using BCarnellChars.Characters;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    public class BasementGameManager : BaseGameManager
    {
        private static BasementGameManager singleton;
        static MethodInfo EndSequence = AccessTools.DeclaredMethod(typeof(CoreGameManager), "EndSequence");
        public static BasementGameManager BasementInstance => singleton;

        private Ambience ambience;
        private Fog fog = new Fog()
        {
            color = Color.black,
            startDist = 5f,
            maxDist = 250f,
            priority = 0,
            strength = 0
        };
        private AudioManager audMan;
        public Canvas overlay;

        private static float lightonChance = 0.2f;
        private static System.Random lightonRNG;

        public CustomImageAnimator overlayAnimator;
        public static List<List<Sprite>> jumpscareFrames = new List<List<Sprite>>();

        public override void Initialize()
        {
            singleton = this;
            lightonRNG = new System.Random(CoreGameManager.Instance.Seed());
            if (levelNo > CoreGameManager.Instance.lastLevelNumber)
            {
                CoreGameManager.Instance.SetLives(defaultLives);
                CoreGameManager.Instance.lastLevelNumber = levelNo;
            }
            base.Initialize();
            ambience = gameObject.GetComponentInChildren<Ambience>();
            audMan = gameObject.AddComponent<AudioManager>();
            audMan.audioDevice = gameObject.AddComponent<AudioSource>();
            audMan.useUnscaledPitch = true;
            audMan.ReflectionSetVariable("disableSubtitles", true);
            audMan.audioDevice.dopplerLevel = 0f;

            overlayAnimator.animations.Add("Jump1", new CustomAnimation<Sprite>(jumpscareFrames[0].ToArray(), .6f));
            overlayAnimator.animations.Add("Jump2", new CustomAnimation<Sprite>(jumpscareFrames[1].ToArray(), .8f));
        }

        private void OnDestroy()
        {
            singleton = null;
        }

        public override void BeginPlay()
        {
            base.BeginPlay();
            ambience.Initialize(ec);
            foreach (Activity activity in ec.activities)
                if (activity.Corrupted)
                    activity.Corrupt(false);
        }

        protected override void ExitedSpawn()
        {
            base.ExitedSpawn();
            if (CoreGameManager.Instance.currentMode != Mode.Free)
            {
                audMan.QueueAudio(BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientIntro"), true);
                audMan.QueueAudio(BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientLoop"));
                audMan.SetLoop(true);
            }
            BeginSpoopMode();
        }

        protected override void VirtualUpdate()
        {
            /*if (audMan & ec && !audMan.loop && !audMan.maintainLoop && audMan.soundQueue[0] == BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientLoop"))
            {
                audMan.SetLoop(true);
                audMan.maintainLoop = true;
            }*/
        }

        public override void BeginSpoopMode()
        {
            if (CoreGameManager.Instance.currentMode == Mode.Free)
            {
                ec.Npcs.Find(x => x.gameObject.GetComponent<PrototypeBot>()).Despawn();
                return;
            }
            foreach (Cell light in ec.lights)
            {
                if (!(lightonRNG.NextDouble() < lightonChance))
                    light.SetPower(false);
            }

            ec.MaxRaycast = 250f;
            StartCoroutine(FadeOnFog());
        }

        private IEnumerator FadeOnFog()
        {
            ec.AddFog(fog);
            fog.strength = 0f;
            float fogStrength2 = 0f;
            while (fogStrength2 < 1f)
            {
                fogStrength2 += 0.25f * Time.deltaTime;
                fog.strength = fogStrength2;
                ec.UpdateFog();
                yield return null;
            }

            fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
        }

        private IEnumerator FadeOffFog()
        {
            float fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
            while (fogStrength2 > 0f)
            {
                fogStrength2 -= 0.25f * Time.deltaTime;
                fog.strength = fogStrength2;
                ec.UpdateFog();
                yield return null;
            }

            fogStrength2 = 0f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
        }

        public override void LoadNextLevel()
        {
            CoreGameManager.Instance.saveMapAvailable = false;
            for (int i = 0; i < CoreGameManager.Instance.Lives; i++)
            {
                CoreGameManager.Instance.AddPoints(CoreGameManager.Instance.GetPointsThisLevel(0), 0, playAnimation: false, includeInLevelTotal: false);
            }

            PrepareToLoad();
            elevatorScreen = Instantiate(elevatorScreenPre);
            elevatorScreen.OnLoadReady += base.LoadNextLevel;
            elevatorScreen.Initialize();
            int num = 0;
            if (time <= levelObject.timeBonusLimit)
            {
                num = levelObject.timeBonusVal;
            }

            CoreGameManager.Instance.AddPoints(num, 0, playAnimation: false, includeInLevelTotal: false);
            CoreGameManager.Instance.AwardGradeBonus();
            elevatorScreen.ShowResults(time, num);
            if (CoreGameManager.Instance.SaveEnabled && MTM101BaldiDevAPI.SaveGamesEnabled && levelObject.name == "Basement1"
                && CoreGameManager.Instance.currentMode != Mode.Free && levelObject.finalLevel)
            {
                BCPPSave.Instance.basementCompleted = true;
                BCPPSave.Instance.Save();
            }
        }

        protected override void LoadSceneObject(SceneObject sceneObject, bool restarting)
        {
            CoreGameManager.Instance.nextLevel = sceneObject;
            if (!levelObject.finalLevel || restarting)
            {
                base.LoadSceneObject(Resources.FindObjectsOfTypeAll<SceneObject>().First(x => x.levelTitle == "PIT"), restarting);
                return;
            }
            
            base.LoadSceneObject(sceneObject, restarting);
        }

        public override void RestartLevel()
        {
            CoreGameManager.Instance.saveMapAvailable = true;
            PrepareToLoad();
            elevatorScreen = Instantiate(elevatorScreenPre);
            elevatorScreen.OnLoadReady += base.RestartLevel;
            elevatorScreen.Initialize();
        }

        public void EndGame(Transform player, PrototypeBot bot)
        {
            bot.spriteBase.SetActive(false);
            overlay.gameObject.SetActive(true);
            overlayAnimator.Play("Jump" + Mathf.RoundToInt(UnityEngine.Random.Range(1f, 2f)), 1f);
            overlay.worldCamera = CoreGameManager.Instance.GetCamera(player.gameObject.GetComponent<PlayerManager>().playerNumber).canvasCam;
            Time.timeScale = 0f;
            audMan.FlushQueue(true);
            MusicManager.Instance.StopMidi();
            CoreGameManager.Instance.disablePause = true;
            CoreGameManager.Instance.GetCamera(player.gameObject.GetComponent<PlayerManager>().playerNumber).UpdateTargets(bot.transform, 0);
            CoreGameManager.Instance.GetCamera(player.gameObject.GetComponent<PlayerManager>().playerNumber).offestPos = (player.position - bot.transform.position).normalized * 2f + Vector3.up;
            CoreGameManager.Instance.GetCamera(player.gameObject.GetComponent<PlayerManager>().playerNumber).SetControllable(value: false);
            CoreGameManager.Instance.GetCamera(player.gameObject.GetComponent<PlayerManager>().playerNumber).matchTargetRotation = false;
            audMan.volumeModifier = 0.6f;
            AudioManager audioManager = audMan;
            WeightedSelection<SoundObject>[] loseSounds = bot.loseSounds;
            audioManager.PlaySingle(WeightedSelection<SoundObject>.RandomSelection(loseSounds));
            CoreGameManager.Instance.StartCoroutine((IEnumerator)EndSequence.Invoke(CoreGameManager.Instance, []));
            InputManager.Instance.Rumble(1f, 2f);
        }

        public override void ActivityCompleted(bool correct, Activity activity)
        {
            base.ActivityCompleted(correct, activity);
            if (correct && ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()))
                ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()).gameObject.GetComponent<PrototypeBot>().MechFunctInvoke();
        }
    }

    public class SwitchNextFloorBCPP : MonoBehaviour, IButtonReceiver
    {
        private static bool but = false;
        public static bool enabledSwitch => but;
        public void ButtonPressed(bool val)
        {
            if (!but && BaseGameManager.Instance != null && CoreGameManager.Instance != null)
            {
                CoreGameManager.Instance.audMan.PlaySingle(Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "LAt_Sighted"));
                BaseGameManager.Instance.Ec.FlickerLights(true);

                but = true;
            }
        }

        void OnDestroy()
        {
            but = false;
        }
        void Start()
        {
            but = false;
        }
    }

    public class FacultyButtonBuilderBCPP : ObjectBuilder
    {
        public GameButton buttonPre;

        public override void Build(EnvironmentController ec, LevelBuilder builder, RoomController room, System.Random cRng)
        {
            List<RoomController> facultyRoom = ec.rooms.FindAll(x => x.category == RoomCategory.Faculty);
            //Debug.Log("Faculties found:" + facultyRoom.Count);
            if (facultyRoom.Count <= 0)
            {
                Debug.LogWarning("WTF NOTHING???");
                return;
            }
            List<Cell> tilesOfShape = facultyRoom[Mathf.RoundToInt(UnityEngine.Random.Range(0, facultyRoom.Count - 1))].AllTilesNoGarbage(false, false);
            for (int i = 0; i < tilesOfShape.Count; i++)
                if (!tilesOfShape[i].HasFreeWall || (tilesOfShape[i].shape != TileShape.Corner && tilesOfShape[i].shape != TileShape.Single) || tilesOfShape[i].room.category != RoomCategory.Faculty
                    || tilesOfShape[i].offLimits)
                    tilesOfShape.RemoveAt(i--);
            int index;
            Direction direct;
            while (tilesOfShape.Count > 0)
            {
                index = cRng.Next(0, tilesOfShape.Count);
                direct = tilesOfShape[index].RandomUncoveredDirection(cRng);
                if (direct != Direction.Null && !tilesOfShape[index].Hidden)
                {
                    if (GameButton.BuildInArea(ec, tilesOfShape[index].position, tilesOfShape[index].position, 1, gameObject.AddComponent<SwitchNextFloorBCPP>().gameObject, buttonPre, cRng) == null)
                        Debug.LogWarning("WTF FAILURE???");
                    else break;
                    tilesOfShape.RemoveAt(index);
                }
                else
                    tilesOfShape.RemoveAt(index);
            }
            Debug.LogWarning("WTF FAILURE???");
        }
    }
}

public class CustomImageAnimatorUnscaledTime : CustomImageAnimator
{
    protected override void VirtualUpdate()
    {
        if (!(currentAnim == ""))
        {
            float num = Time.unscaledDeltaTime * Speed;
            currentAnimTime += num;
            currentFrameTime += num;
            if (currentFrameTime >= currentFrame.frameTime)
            {
                _currentFrameIndex++;
                currentFrameTime = 0f;
                UpdateFrame();
            }

            if (currentAnimTime >= currentAnimation.animationLength)
            {
                Stop();
            }
        }
    }
}