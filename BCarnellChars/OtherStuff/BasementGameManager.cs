using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    public class BasementGameManager : BaseGameManager
    {
        [SerializeField] private SceneObject pitStop = Resources.FindObjectsOfTypeAll<SceneObject>().Last(x => x.levelTitle == "PIT");
        private static BasementGameManager singleton;
        private static MethodInfo EndSequence = AccessTools.DeclaredMethod(typeof(CoreGameManager), "EndSequence");
        private static FieldInfo _disableSubtitles = AccessTools.DeclaredField(typeof(AudioManager), "disableSubtitles");
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

        [SerializeField] internal CustomImageAnimator overlayAnimator;

        public override void Initialize()
        {
            singleton = this;
            lightonRNG = new System.Random(CoreGameManager.Instance.Seed());
            if (levelNo < CoreGameManager.Instance.lastLevelNumber)
            {
                CoreGameManager.Instance.SetLives(defaultLives, false);
                CoreGameManager.Instance.lastLevelNumber = levelNo;
            }
            base.Initialize();
            ambience = gameObject.GetComponentInChildren<Ambience>();
            audMan = gameObject.AddComponent<AudioManager>();
            audMan.audioDevice = gameObject.AddComponent<AudioSource>();
            audMan.useUnscaledPitch = true;
            _disableSubtitles.SetValue(audMan, true);
            audMan.audioDevice.dopplerLevel = 0f;
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

        public void ScaryMode()
        {
            audMan.FlushQueue(true);
            audMan.QueueAudio(BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/TimeOut"), true);
            audMan.QueueAudio(BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/TimeOutLoop"));
            audMan.SetLoop(true);
            StartCoroutine(FadeOffFog());
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

            ec.StartEventTimers();
            ec.SetTimeLimit(ec.SurpassedGameTime + levelObject.timeLimit);
            //ec.MaxRaycast = 250f; // Isn't the bot only supposed to have night vision?
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
            int stickerBonuses = CoreGameManager.Instance.GetStickerBonuses(ec.RemainingTime, (ec.GetBaldi() != null) ? ec.NavigableDistance(ec.CellFromPosition(CoreGameManager.Instance.GetPlayer(0).transform.position), ec.CellFromPosition(ec.GetBaldi().transform.position), PathType.Nav) : 0, ec.map.PlayerDiscoveredCells);
            CoreGameManager.Instance.AddPoints(stickerBonuses, 0, false, false, true);
            CoreGameManager.Instance.saveMapAvailable = false;
            CoreGameManager.Instance.saveMapPurchased = false;
            for (int i = 0; i < 2 - CoreGameManager.Instance.Attempts; i++)
                CoreGameManager.Instance.AddPoints(CoreGameManager.Instance.GetPointsThisLevel(0), 0, false, false, false);

            PrepareToLoad();
            elevatorScreen = Instantiate(elevatorScreenPre);
            elevatorScreen.OnLoadReady += base.LoadNextLevel;
            elevatorScreen.Initialize();
            if (time <= levelObject.timeBonusLimit)
                _ = levelObject.timeBonusVal;

            if (problems > 0)
                CoreGameManager.Instance.GradeVal += -Mathf.RoundToInt(gradeValue * ((float)correctProblems / (float)problems * 2f - 1f));

            elevatorScreen.ShowResults(time, Mathf.RoundToInt((float)stickerBonuses * CoreGameManager.Instance.YtpMultiplier));
            if (CoreGameManager.Instance.GetPoints(0) > 0)
                _ = levelObject.finalLevel;
            if (CoreGameManager.Instance.SaveEnabled && MTM101BaldiDevAPI.SaveGamesEnabled && levelObject.name == "Basement1"
                && CoreGameManager.Instance.currentMode != Mode.Free && levelObject.finalLevel)
            {
                BCPPSave.Instance.basementCompleted = true;
                BCPPSave.Instance.Save();
            }
        }

        public override int CurrentStickerLevel() => 1;

        protected override void LoadSceneObject(SceneObject sceneObject, bool restarting)
        {
            CoreGameManager.Instance.nextLevel = sceneObject;
            if (!levelObject.finalLevel || restarting)
            {
                base.LoadSceneObject(pitStop, restarting);
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
            Time.timeScale = 0f;
            overlay.gameObject.SetActive(true);
            overlayAnimator.Play("Jump" + Mathf.RoundToInt(UnityEngine.Random.Range(1f, 2f)), 1f);
            overlay.worldCamera = CoreGameManager.Instance.GetCamera(player.gameObject.GetComponent<PlayerManager>().playerNumber).canvasCam;
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
        private EnvironmentController ec;
        private static bool but = false;
        internal static bool enabledSwitch => but;
        internal TimeScaleModifier itshard = new TimeScaleModifier();
        public void ButtonPressed(bool val)
        {
            if (!but && BaseGameManager.Instance != null && CoreGameManager.Instance != null)
            {
                CoreGameManager.Instance.audMan.PlaySingle(Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "LAt_Sighted"));
                ec.FlickerLights(true);
                ec.AddTimeScale(itshard);

                but = true;
            }
        }

        private void Update()
        {
            if (but)
            {
                itshard.npcTimeScale += 0.01f * (Time.deltaTime * itshard.npcTimeScale);
                itshard.environmentTimeScale += 0.01f * (Time.deltaTime * itshard.environmentTimeScale);
            }
        }

        public void ConnectButton(GameButtonBase button)
        {
        }

        private void OnDestroy()
        {
            ec.RemoveTimeScale(itshard);
            but = false;
        }
        private void Start()
        {
            ec = BaseGameManager.Instance.Ec;
            but = false;
        }
    }

    public class FacultyButtonBuilderBCPP : StructureBuilder
    {
        public GameButton buttonPre;

        public override void OnGenerationFinished(LevelBuilder lg)
        {
            System.Random rng = lg.controlledRNG;
            List<RoomController> facultyRoom = ec.rooms.FindAll(x => x.category == RoomCategory.Faculty);
            Debug.Log("Faculties found:" + facultyRoom.Count);
            if (facultyRoom.Count <= 0)
            {
                Debug.LogWarning("WTF NOTHING???");
                return;
            }
            List<Cell> tilesOfShape = new List<Cell>();
            facultyRoom.Do(f => tilesOfShape.AddRange(f.AllTilesNoGarbage(false, false)));
            for (int i = 0; i < tilesOfShape.Count; i++)
                if (!tilesOfShape[i].HasHardFreeWall || (tilesOfShape[i].shape != TileShapeMask.Corner && tilesOfShape[i].shape != TileShapeMask.Single) || tilesOfShape[i].room.category != RoomCategory.Faculty
                    || tilesOfShape[i].offLimits)
                    tilesOfShape.RemoveAt(i--);
            Debug.Log("Tiles found:" + tilesOfShape.Count);
            int index;
            Direction direct;
            while (tilesOfShape.Count > 0)
            {
                index = rng.Next(0, tilesOfShape.Count);
                direct = tilesOfShape[index].RandomUncoveredDirection(rng);
                if (direct != Direction.Null && !tilesOfShape[index].Hidden)
                {
                    if (GameButton.BuildInArea(ec, tilesOfShape[index].position, 1, gameObject.AddComponent<SwitchNextFloorBCPP>().gameObject, buttonPre, rng) == null)
                        Debug.LogWarning("WTF NULL???");
                    else break;
                    tilesOfShape.RemoveAt(index);
                }
                else
                    tilesOfShape.RemoveAt(index);
            }
            Debug.LogWarning(tilesOfShape.Count <= 0 ? "WTF FAILURE???" : "ItDone!");
        }

        protected override void Finished()
        {
        }
    }

    public class B1TimeOut : RandomEvent
    {
        private List<Cell> lightsToTurnOn = new List<Cell>();

        private float baldiAngerRate = 5f, baldiAngerAmount = 0f, lightOffRate = 0.1f, timeToNextAnger, timeToNextLight;

        private void Update()
        {
            if (!active)
                return;

            timeToNextAnger -= Time.deltaTime * ec.NpcTimeScale;
            timeToNextLight -= Time.deltaTime * ec.EnvironmentTimeScale;
            if (timeToNextAnger <= 0f)
            {
                timeToNextAnger = baldiAngerRate + timeToNextAnger;
                baldiAngerAmount++;
            }
            ec.Npcs.DoIf(npc => npc.GetComponent<PrototypeBot>() != null, (npc) =>
            {
                if (!npc.behaviorStateMachine.currentState.GetType().Equals(typeof(PrototypeBot_Stunned)))
                {
                    npc.Navigator.maxSpeed = 40f + baldiAngerAmount;
                    npc.Navigator.SetSpeed(40f + baldiAngerAmount);
                }
                else
                {
                    npc.Navigator.maxSpeed = 0f;
                    npc.Navigator.SetSpeed(0f);
                }
            });

            if (timeToNextLight <= 0f)
            {
                timeToNextLight = lightOffRate + timeToNextLight;
                if (lightsToTurnOn.Count > 0)
                {
                    ec.QueueLightSourceForRegenerate(lightsToTurnOn[0]);
                    lightsToTurnOn[0].SetPower(true);
                    lightsToTurnOn.RemoveAt(0);
                }
            }
        }

        public override void Begin()
        {
            base.Begin();
            BasementGameManager.BasementInstance.ScaryMode();
            ec.lights.Do(l =>
            {
                l.lightColor = Color.red;
                ec.QueueLightSourceForRegenerate(l);
            });
            lightsToTurnOn.AddRange(ec.lights);
            lightsToTurnOn.Shuffle();
            ec.CloseSchool();
        }

        public override void End()
        {
        }
    }
}