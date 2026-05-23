using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.OtherStuff;

public class BasementGameManager : BaseGameManager
{
    [SerializeField] private SceneObject pitStop = SceneObjectMetaStorage.Instance.Find(x => x.title == "PIT" && x.tags.Contains("pitstop")).value;
    private static BasementGameManager singleton;
    private static readonly MethodInfo EndSequence = AccessTools.DeclaredMethod(typeof(CoreGameManager), "EndSequence");
    private static readonly FieldInfo _disableSubtitles = AccessTools.DeclaredField(typeof(AudioManager), "disableSubtitles");
    public static BasementGameManager BasementInstance => singleton;

    [SerializeField] internal Ambience ambience;
    private Fog fog = new Fog()
    {
        color = Color.black,
        startDist = 5f,
        maxDist = 250f,
        priority = 0,
        strength = 0
    };
    [SerializeField] internal AudioManager audMan;
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
    }

    private void OnDestroy()
    {
        singleton = null;
    }

    public override void BeginPlay()
    {
        base.BeginPlay();
        MusicManager.Instance.StopMidi();
        ModdedFileManager.Instance.saveData.saveAvailable = false;
        PlayerFileManager.Instance.savedGameData.saveAvailable = false;
        PlayerFileManager.Instance.Save();
        ambience.Initialize(ec);
        foreach (Activity activity in ec.activities)
            if (activity.Corrupted)
                activity.Corrupt(false);
    }

    public override void ExitedSpawn()
    {
        base.ExitedSpawn();
        if (CoreGameManager.Instance.currentMode != Mode.Free)
        {
            audMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientIntro"), true);
            audMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientLoop"));
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
        audMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/TimeOut"), true);
        audMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/TimeOutLoop"));
        audMan.SetLoop(true);
        StartCoroutine(FadeOffFog());
    }

    public override void BeginSpoopMode()
    {
        if (CoreGameManager.Instance.currentMode == Mode.Free)
        {
            ec.Npcs.Find(x => x is PrototypeBot).Despawn();
            return;
        }
        foreach (Cell light in ec.lights)
        {
            if (!(lightonRNG.NextDouble() < (double)lightonChance))
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
        HighlightManager.Instance.Highlight("steam_completed", LocalizationManager.Instance.GetLocalizedText("Steam_Highlight_Win"), string.Format(LocalizationManager.Instance.GetLocalizedText("Steam_Highlight_Win_Desc"), CurrentLevel + 1), 2u, 0f, 0f, TimelineEventClipPriority.Standard);
        int num = 0;
        var robot = ec.Npcs.Find(x => x is PrototypeBot);
        if (robot != null)
        {
            num = ec.NavigableDistance(ec.CellFromPosition(CoreGameManager.Instance.GetPlayer(0).transform.position), ec.CellFromPosition(robot.transform.position), PathType.Nav);
            if (num < 0)
            {
                num = ec.NavigableDistance(ec.CellFromPosition(CoreGameManager.Instance.GetPlayer(0).transform.position), ec.CellFromPosition(robot.transform.position), PathType.Const) * 2;
                if (num < 0)
                    num = 100;
            }
        }

        int stickerBonuses = CoreGameManager.Instance.GetStickerBonuses(ec.RemainingTime, num, ec.map.PlayerDiscoveredCells);
        CoreGameManager.Instance.AddPoints(stickerBonuses, 0, false, false, true);
        CoreGameManager.Instance.saveMapAvailable = false;
        CoreGameManager.Instance.saveMapPurchased = false;
        for (int i = 0; i < 2 - CoreGameManager.Instance.Attempts; i++)
            CoreGameManager.Instance.AddPoints(CoreGameManager.Instance.GetPointsThisLevel(0), 0, false, false, false);

        PrepareToLoad();
        elevatorScreen = Instantiate(elevatorScreenPre);
        elevatorScreen.OnLoadReady += base.LoadNextLevel;
        elevatorScreen.Initialize();

        if (problems > 0)
        {
            CoreGameManager.Instance.GradeVal += -Mathf.RoundToInt(gradeValue * ((float)correctProblems / (float)problems * 2f - 1f));
        }

        elevatorScreen.ShowResults(time, Mathf.RoundToInt((float)stickerBonuses * CoreGameManager.Instance.YtpMultiplier));
        if (CoreGameManager.Instance.SaveEnabled && MTM101BaldiDevAPI.SaveGamesEnabled && CoreGameManager.Instance.sceneObject == BCPPPlugin.Instance.sBasement
            && CoreGameManager.Instance.currentMode != Mode.Free)
        {
            BCPPSave.Instance.basementCompleted = true;
            BCPPSave.Instance.Save();
        }
    }

    public override int CurrentStickerLevel() => -1;

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
        if (correct && ec.Npcs.Exists(x => x is PrototypeBot))
            ec.Npcs.Find(x => x is PrototypeBot).GetComponent<PrototypeBot>().MechFunctInvoke();
    }
}

public class SwitchNextFloorBCPP : MonoBehaviour, IButtonReceiver
{
    private EnvironmentController ec;
    private static bool but = false;
    internal static bool enabledSwitch => but;
    internal TimeScaleModifier itshard = new TimeScaleModifier();
    [SerializeField] private SoundObject uhoh = Resources.FindObjectsOfTypeAll<SoundObject>().Last(x => x.name == "LAt_Sighted" && x.GetInstanceID() >= 0);
    public void ButtonPressed(bool val)
    {
        if (!but && BaseGameManager.Instance != null && CoreGameManager.Instance != null)
        {
            CoreGameManager.Instance.audMan.PlaySingle(uhoh);
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
    private void Start() => but = false;
    internal void Initialize(EnvironmentController ec)
    {
        this.ec = ec;
        but = false;
    }
}

public class FacultyButtonBuilderBCPP : StructureBuilder
{
    [SerializeField] internal GameButton buttonPre;

    public override void OnGenerationFinished(LevelBuilder lg)
    {
        System.Random rng = lg.controlledRNG;
        List<RoomController> facultyRoom = ec.rooms.FindAll(x => x.category == RoomCategory.Faculty);
        //BCPPSave.Instance.logger.LogInfo("Faculties found:" + facultyRoom.Count);
        if (facultyRoom.Count <= 0)
        {
            //BCPPSave.Instance.logger.LogWarning("WTF NOTHING???");
            return;
        }
        List<Cell> tilesOfShape = new List<Cell>();
        facultyRoom.Do(f => tilesOfShape.AddRange(f.AllTilesNoGarbage(false, false)));
        for (int i = 0; i < tilesOfShape.Count; i++)
            if (!tilesOfShape[i].HasHardFreeWall || (tilesOfShape[i].shape != TileShapeMask.Corner && tilesOfShape[i].shape != TileShapeMask.Single) || tilesOfShape[i].room.category != RoomCategory.Faculty
                || tilesOfShape[i].offLimits)
                tilesOfShape.RemoveAt(i--);
        //BCPPSave.Instance.logger.LogInfo("Tiles found:" + tilesOfShape.Count);
        int index;
        Direction direct;
        var switcher = gameObject.AddComponent<SwitchNextFloorBCPP>();
        while (tilesOfShape.Count > 0)
        {
            index = rng.Next(0, tilesOfShape.Count);
            direct = tilesOfShape[index].RandomUncoveredDirection(rng);
            if (direct != Direction.Null && !tilesOfShape[index].Hidden)
            {
                if (GameButton.BuildInArea(ec, tilesOfShape[index].position, 1, gameObject, buttonPre, rng) == null) { }
                    //BCPPSave.Instance.logger.LogWarning("WTF NULL???");
                else
                {
                    switcher.Initialize(ec);
                    break;
                }
                tilesOfShape.RemoveAt(index);
            }
            else
                tilesOfShape.RemoveAt(index);
        }
        //BCPPSave.Instance.logger.LogWarning(tilesOfShape.Count <= 0 ? "WTF FAILURE???" : "ItDone!");
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