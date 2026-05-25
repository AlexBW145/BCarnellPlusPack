using BCarnellChars.Characters;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace BCarnellChars.OtherStuff;

internal class CarnellBossManager : BaseGameManager
{
    [SerializeField] private SceneObject pitStop = SceneObjectMetaStorage.Instance.Find(x => x.title == "PIT" && x.tags.Contains("pitstop")).value;
    private static CarnellBossManager singleton;
    private static readonly float lightonChance = 0.2f;
    private static System.Random lightonRNG;
    public static CarnellBossManager BossInstance => singleton;
    private List<SubInitializer> subinit = new List<SubInitializer>();
    private SubInitializer subinitPre => BCPPPlugin.bcppAssets.Get<GameInitializer>("SubInitializer") as SubInitializer;

    [SerializeField] protected List<EnvironmentController> Environments;
    private List<EnvironmentController> LastEnvironments;
    internal List<LevelGenerationParameters> levelObjects;
    private List<LevelGenerationParameters> LastLevelObjects;
    private EnvironmentController endingEC;

    private IEnumerator ItemHallSpawner;
    internal B2Button myman;
    [SerializeField] internal B2BattleObject battle;

    protected override void AwakeFunction()
    {
        CoreGameManager.Instance.SetRandomSeed(); // For the funny...
    }

    private static readonly FieldInfo _seedText = AccessTools.DeclaredField(typeof(ElevatorScreen), "seedText");

    private void UpdateLightmap()
    {
        foreach (var evc in Environments)
            evc.lightingOverride = !evc.gameObject.activeSelf;
        for (int x = 0; x < ec.levelSize.x; x++)
            for (int z = 0; z < ec.levelSize.z; z++)
                ec.UpdateLightingAtCell(ec.cells[x, z]);
        CoreGameManager.Instance.UpdateLightMap();
    }

    protected new void Start()
    {
        SubInitializer subin;
        Environments = [ec];
        levelObjects = [levelObject];
        lightonRNG = new System.Random(CoreGameManager.Instance.Seed());
        for (int i = 0; i < 4; i++)
        {
            subin = Instantiate(subinitPre, null, false);
            subin.subLevelObject = BCPPPlugin.Instance.lsubBoss[i];
            subin.baseGameManager = this;
            subin.onReady = (lb, gm) =>
            {
                Environments.Add(lb.Ec);
                /*lb.Ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().AddFunction(lb.Ec.mainHall.functionObject.AddComponent<B2HallRoomFunction>());
                lb.Ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().Initialize(lb.Ec.mainHall);
                lb.Ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().OnGenerationFinished();*/
                lb.Ec.PauseEnvironment(false);
                lb.Ec.PauseEvents(false);
                //GC.Collect();
                /*if (lb.Ec.elevators.Count > 0)
                {
                    foreach (Elevator elevator in lb.Ec.elevators)
                    {
                        elevator.Door.Shut();
                        elevator.ColliderGroup.Enable(false);
                        elevator.Close();
                    }
                }*/
                lb.Ec.SetAllLights(false);
                foreach (Cell light in lb.Ec.lights)
                {
                    if (light.room.category == RoomCategory.Special || lightonRNG.NextDouble() < (double)lightonChance)
                        light.SetLight(true);
                }
                lb.Ec.gameObject.SetActive(false);

                /*if (!CoreGameManager.Instance.levelGenError && subinit.Count >= 4
                    && subinit.TrueForAll(s => s.Ready) && !CoreGameManager.Instance.readyToStart)
                {
                    generatorFinished = true;
                    CoreGameManager.Instance.readyToStart = true;
                    foreach (Cell cell in ec.AllCells())
                        ec.UpdateLightingAtCell(cell);
                    ec.UpdateQueuedLightChanges();
                    foreach (var profit in GameObject.FindObjectsOfType<ToProfitCardCost>(true).Where(profit => profit.gameObject.scene == ec.gameObject.scene))
                        profit.Begin();
                    foreach (var banan in FindObjectsOfType<ITM_NanaPeel>(false))
                        Destroy(banan.gameObject);
                }*/
            };
            subinit.Add(subin);
            subin.gameObject.SetActive(true);
        }
        subin = Instantiate(subinitPre, null, false);
        subin.subLevelAsset = new(BCPPPlugin.Instance.sendBoss.levelAsset, BCPPPlugin.Instance.sendBoss.extraAsset);
        subin.baseGameManager = this;
        subin.onReady = (lb, gm) =>
        {
            Environments.Add(lb.Ec);
            endingEC = lb.Ec;
            lb.Ec.PauseEnvironment(false);
            lb.Ec.PauseEvents(false);
            lb.Ec.gameObject.SetActive(false);
        };
        subinit.Add(subin);
        subin.gameObject.SetActive(true);
        IEnumerator WaitForGenerator()
        {
            while (!(subinit.TrueForAll(s => s.Ready) && singleton != null))
                yield return null;
            if (CoreGameManager.Instance.levelGenError)
                yield break;

            if (levelNo < CoreGameManager.Instance.lastLevelNumber)
            {
                CoreGameManager.Instance.SetLives(defaultLives, false);
                CoreGameManager.Instance.lastLevelNumber = levelNo;
            }
            HighlightManager.Instance.SetTimelineGameMode(TimelineGameMode.Staging);
            CoreGameManager.Instance.SpawnPlayers(ec);
            ec.AssignPlayers();
            ApplyMap(ec.map);
            if (completeMapOnReady)
                ec.map.CompleteMap();
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
                ec.map.AddArrow(CoreGameManager.Instance.GetPlayer(i).plm.Entity, Color.white);
            foreach (var subec in Environments)
            {
                if (subec == ec) continue;
                if (completeMapOnReady)
                    subec.map.CompleteMap();
                for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
                {
                    subec.map.targets.Add(CoreGameManager.Instance.GetPlayer(i).plm.Entity);
                    //subec.map.cams.Add(Instantiate(CoreGameManager.Instance.GetCamera(i).mapCamPre, subec.map.transform));
                    subec.map.cams.Add(CoreGameManager.Instance.GetCamera(i).mapCam);
                    subec.map.AddArrow(CoreGameManager.Instance.GetPlayer(i).plm.Entity, Color.white);
                    //CameraExtensions.GetUniversalAdditionalCameraData(CoreGameManager.Instance.GetCamera(i).camCom).cameraStack.Insert(2, subec.map.cams[i]);
                }
            }
            UpdateLightmap();

            generatorFinished = true;
            CollectNotebooks(0);
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                var hud = CoreGameManager.Instance.GetHud(i);
                hud.SetNotebookDisplay(false);
            }
            foreach (Cell cell in ec.AllCells())
                ec.QueueLightSourceForRegenerate(cell);
            /*foreach (var profit in GameObject.FindObjectsOfType<ToProfitCardCost>(true).Where(profit => profit.gameObject.scene == ec.gameObject.scene))
                profit.Begin();*/
            foreach (var banan in FindObjectsOfType<ITM_NanaPeel>(false))
                Destroy(banan.gameObject);

            GC.Collect();
            CoreGameManager.Instance.ResetCameras();
            CoreGameManager.Instance.ResetShaders();
            CoreGameManager.Instance.readyToStart = true;
            if (beginPlayImmediately)
                BeginPlay();
            if (spawnNpcsOnInit)
            {
                ec.SpawnNPCs();
                ec.StartEventTimers();
            }
            yield break;
        }
        StartCoroutine(WaitForGenerator());
    }

    public override void Initialize()
    {
#if !DEBUG
        if (CoreGameManager.Instance.currentMode == Mode.Free || CoreGameManager.Instance.lifeMode == LifeMode.Explorer || !BCPPSave.Instance.basementCompleted
            || Chainloader.PluginInfos.ContainsKey("com.exmagikguy.baldi.loguscheats") || Chainloader.PluginInfos.ContainsKey("com.sinai.unityexplorer") || Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.pinedebug"))
        {
            DogcheckBCPP.SendToDogcheckScreen();
            Destroy(gameObject);
            return;
        }
#endif
        /*ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().AddFunction(ec.mainHall.functionObject.AddComponent<B2HallRoomFunction>());
        ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().Initialize(ec.mainHall);
        ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().OnGenerationFinished();*/
        singleton = this;
        ec.ElevatorManager.PrepareToLeaveSpawn();
        var text = _seedText.GetValue(ElevatorScreen.Instance) as TMP_Text;
        text.text = CoreGameManager.Instance.Seed().ToString();
        ec.SetAllLights(false);
        foreach (Cell light in ec.lights)
        {
            if (light.room.category == RoomCategory.Special || lightonRNG.NextDouble() < (double)lightonChance)
                light.SetLight(true);
        }
    }

    private void OnDestroy()
    {
        singleton = null;
    }

    public override int CurrentStickerLevel() => -2;

    public override void ExitedSpawn()
    {
        base.ExitedSpawn();
        BeginSpoopMode();
    }

    public override void BeginPlay()
    {
        base.BeginPlay();
        MusicManager.Instance.StopMidi();
        ModdedFileManager.Instance.saveData.saveAvailable = false;
        PlayerFileManager.Instance.savedGameData.saveAvailable = false;
        PlayerFileManager.Instance.Save();
        LastEnvironments = new(Environments);
        LastEnvironments.Remove(endingEC);
        LastLevelObjects = new(levelObjects);
    }

    public override void BeginSpoopMode()
    {
        ec.ElevatorManager.SetAllElevators(ElevatorState.OutOfOrder);
    }

    protected override void AllNotebooks()
    {
    }

    public override void CallSpecialManagerFunction(int val, GameObject source)
    {
        base.CallSpecialManagerFunction(val, source);
        switch (val)
        {
            case 0:
                if (!allNotebooksFound && !MusicManager.Instance.MidiPlaying)
                    myman.waitingtogetcloser = true;
                break;
            case 1:
                if (!allNotebooksFound)
                    ec.Npcs.DoIf(x => x is BCarnellBoss, x => x.GetComponent<BCarnellBoss>().IAmComing());
                break;
            case 2:
                if (!allNotebooksFound && MusicManager.Instance.MidiPlaying && MusicManager.Instance.MidiPlayer.MPTK_MidiName == BCPPPlugin.bcppAssets.Get<string>("mus_BCarnellBossIntro"))
                    THEBUTTONISPRESSED();
                else if (allNotebooksFound)
                {
                    IEnumerator Transition()
                    {
                        float elaspedTime = 0f;
                        var timescale = new TimeScaleModifier();
                        ec.AddTimeScale(timescale);
                        while (elaspedTime <= 2f)
                        {
                            timescale.npcTimeScale -= 0.35f * Time.deltaTime;
                            timescale.npcTimeScale = Mathf.Max(0f, timescale.npcTimeScale);
                            timescale.environmentTimeScale = timescale.playerTimeScale = timescale.npcTimeScale;
                            if (!MusicManager.Instance.MidiPlayer.MPTK_IsPaused)
                                MusicManager.Instance.MidiPlayer.MPTK_Speed -= (MusicManager.Instance.MidiPlayer.MPTK_Speed / 0.2500000001f) * Time.deltaTime;
                            elaspedTime += Time.deltaTime;
                            yield return null;
                        }
                        BCarnellBoss.B2Boss phase = BCarnellBoss.B2Boss.Phase1;
                        ec.Npcs.DoIf(x => x is BCarnellBoss, x =>
                        {
                            x.GetComponent<AudioManager>().FlushQueue(true);
                            phase = x.GetComponent<BCarnellBoss>().phases;
                        });
                        if (phase == BCarnellBoss.B2Boss.Phase4)
                        {
                            BCarnellBoss.MidiHang(MusicManager.Instance.MidiPlayer, false, true);
                            MusicManager.Instance.StopMidi();
                            LastEnvironments = [endingEC];
                            SwapEnvironment(0, true);
                            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
                            {
                                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().modifiers["walkSpeed"].RemoveAll(x => x.multiplier == 2f);
                                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().modifiers["runSpeed"].RemoveAll(x => x.multiplier == 2f);
                                CoreGameManager.Instance.GetPlayer(i).Teleport(endingEC.spawnPoint);
                                CoreGameManager.Instance.GetPlayer(i).transform.rotation = endingEC.spawnRotation;
                                CoreGameManager.Instance.GetPlayer(i).itm.Disable(true);
                            }
                            if (ItemHallSpawner != null)
                                StopCoroutine(ItemHallSpawner);
                            endingEC.ElevatorManager.SetAllElevators(ElevatorState.Closed);
                            CoreGameManager.Instance.disablePause = true;
                            CoreGameManager.Instance.sceneObject = BCPPPlugin.Instance.sendBoss;
                            yield break;
                        }
                        MusicManager.Instance.MidiPlayer.MPTK_Speed = 1f + ((0.2500000001f * (float)(phase - 1)) + ((float)BCarnellBoss.initHP / 60f));
                        LastEnvironments = [.. LastEnvironments.Where(e => e != ec)];
                        LastLevelObjects = [.. LastLevelObjects.Where(ld => ld != levelObject)];
                        SwapEnvironment(Mathf.RoundToInt(UnityEngine.Random.Range(0f, LastEnvironments.Count - 1)), true);
                        Teleporttt();
                        ec.Npcs.DoIf(x => x is BCarnellBoss, x => x.GetComponent<BCarnellBoss>().InitPhase(++phase));
                        ec.flickerSpeed = 0.05f;
                        ec.mainHall.lights.Do(x => x.SetLight(true));
                        ec.FlickerLights(true);
                        StartItemSpawnInHall();
                        yield break;
                    }
                    StartCoroutine(Transition());
                }
                break;
            case 3:
                if (endingEC.gameObject.activeSelf)
                {
                    foreach (var door in FindObjectsOfType<SecuredSwingingDoor>(false))
                        door.UnlockFully();
                    var carnell = (BCarnellBoss)ec.SpawnNPC(BCPPPlugin.Instance.sBoss.baldiPrefab, IntVector2.GetGridPosition(ec.spawnPoint));
                    carnell.InitPhase(BCarnellBoss.B2Boss.Phase5);
                }
                break;
        }
    }

    private void StartItemSpawnInHall()
    {
        if (ItemHallSpawner != null)
            StopCoroutine(ItemHallSpawner);
        float distanceFromPlayer = 70f, distanceFromBattle = 125f;
        HashSet<B2BattleObject> objects = new HashSet<B2BattleObject>();
        List<Cell> cells = new(ec.mainHall.cells);
        cells.ControlledShuffle(new System.Random(CoreGameManager.Instance.Seed()));
        foreach (var cell in cells)
        {
            if (Vector3.Distance(cell.CenterWorldPosition, ec.Players[0].transform.position) <= distanceFromPlayer
                || objects.Any(c => Vector3.Distance(cell.CenterWorldPosition, c.transform.position) <= distanceFromBattle))
                continue;
            objects.Add(Instantiate(battle, cell.CenterWorldPosition, default, ec.transform).Initialize(ec));
        }
        IEnumerator Spawner()
        {
            while (ec != null)
            {
                float time = 7f;
                while (time > 0f)
                {
                    time -= Time.deltaTime * ec.EnvironmentTimeScale;
                    yield return null;
                }
                var items = levelObject.forcedItems;
                var tiles = ec.mainHall.AllTilesNoGarbage(false, false);
                var cell = tiles[UnityEngine.Random.Range(0, tiles.Count)];
                var pickup = ec.CreateItem(ec.mainHall, items[UnityEngine.Random.Range(0, items.Count - 1)], new(cell.CenterWorldPosition.x, cell.CenterWorldPosition.z));
                pickup.icon = ec.map.AddIcon(pickup.iconPre, pickup.transform, Color.white);
                yield return null;
            }
        }
        ItemHallSpawner = Spawner();
        StartCoroutine(ItemHallSpawner);
    }

    public void CutsceneIntro()
    {
        MusicManager.Instance.MidiPlayer.MPTK_EnablePresetDrum = true;
        if (allNotebooksFound) return;
        IEnumerator cutscene()
        {
            ec.Npcs.DoIf(x => x is BCarnellBoss, x => x.GetComponent<BCarnellBoss>().TheDialogue(ec.Players[0]));
            ec.AddTimeScale(new TimeScaleModifier(0f, 1f, 1f));
            MusicManager.Instance.PlayMidi(BCPPPlugin.bcppAssets.Get<string>("mus_BCarnellBossIntro"), false);
            yield return null;
            while (MusicManager.Instance.MidiPlaying)
                yield return null;
            ec.Npcs.DoIf(x => x is BCarnellBoss, x => x.GetComponent<AudioManager>().FlushQueue(true));
            allNotebooksFound = true;
            LastEnvironments = [.. LastEnvironments.Where(e => e != ec)];
            LastLevelObjects = [.. LastLevelObjects.Where(ld => ld != levelObject)];
            yield return null;
            SwapEnvironment(Mathf.RoundToInt(UnityEngine.Random.Range(0f, LastEnvironments.Count - 1)), true);
            MusicManager.Instance.PlayMidi(BCPPPlugin.bcppAssets.Get<string>("mus_BCarnellBoss01"), true);
            yield return null;
            Teleporttt();
            var speedMod = new ValueModifier(2f, 0f);
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().modifiers["walkSpeed"].Add(speedMod);
                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().modifiers["runSpeed"].Add(speedMod);
            }
            ec.Npcs.DoIf(x => x is BCarnellBoss, x => x.GetComponent<BCarnellBoss>().InitPhase(BCarnellBoss.B2Boss.Phase1));
            ec.flickerSpeed = 0.05f;
            ec.mainHall.lights.Do(x => x.SetLight(true));
            ec.FlickerLights(true);
            StartItemSpawnInHall();
            yield break;
        }
        StartCoroutine(cutscene());
    }

    internal void THEBUTTONISPRESSED()
    {
        if (MusicManager.Instance.MidiPlaying && MusicManager.Instance.MidiPlayer.MPTK_MidiName == BCPPPlugin.bcppAssets.Get<string>("mus_BCarnellBossIntro")
            && MusicManager.Instance.MidiPlayer.MPTK_TickCurrent < 6912)
            MusicManager.Instance.MidiPlayer.MPTK_TickCurrent = 6912;
        ec.Npcs.DoIf(x => x is BCarnellBoss, x => x.GetComponent<BCarnellBoss>().ButtonPressed());
    }

    private void Teleporttt()
    {
        bool flag = false;
        List<Cell> path = new List<Cell>();
        IntVector2 position = ec.Elevators[UnityEngine.Random.Range(0, ec.Elevators.Count)].Door.position;
        int num = 0, carnellpos = 64;
        while (!flag && num++ < 32)
        {
            ec.FindPath(ec.CellFromPosition(position), ec.mainHall.TileAtIndex(UnityEngine.Random.Range(0, ec.mainHall.TileCount)), PathType.Nav, out path, out var success);
            if (success && path.Count > carnellpos)
            {
                flag = true;
                path = new List<Cell>(path);
                break;
            }
        }
        if (flag)
        {
            ec.Npcs.DoIf(x => x is BCarnellBoss, x =>
            {
                var bald = x as BCarnellBoss;
                bald.ClearSoundLocations();
                x.transform.position = path[carnellpos].CenterWorldPosition;
            });
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
                CoreGameManager.Instance.GetPlayer(i).Teleport(path[10].CenterWorldPosition);
            ec.MakeNoise(path[10].CenterWorldPosition, 127);
        }
    }

    private void SwapEnvironment(int ecIndex, bool limited = false)
    {
        if ((!limited && Environments[ecIndex] == null) || (limited && LastEnvironments[ecIndex] == null)) return;
        PropagatedAudioManager.paused = true;
        ec.PauseEnvironment(true);
        ec.PauseEvents(true);
        ec.Active = false;
        ec.gameObject.SetActive(false);
        foreach (var banan in FindObjectsOfType<Entity>(true).Where(x => x.transform.parent == null && x.Ec == ec && x.gameObject.scene.Equals(gameObject.scene)))
            Destroy(banan.gameObject);
        var thepropaganda = ec.soundPropagationTransform;
        ec = limited ? LastEnvironments[ecIndex] : Environments[ecIndex];
        levelObject = limited ? LastLevelObjects[ecIndex] : levelObjects[ecIndex];
        thepropaganda.SetParent(ec.transform, false);
        ec.soundPropagationTransform = thepropaganda;
        ec.gameObject.SetActive(true);
        PropagatedAudioManager.paused = false;
        ec.AssignPlayers();
        ApplyMap(ec.map);
        UpdateLightmap();
        for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
        {
            CoreGameManager.Instance.GetPlayer(i).plm.Entity.Initialize(ec, CoreGameManager.Instance.GetPlayer(i).transform.position);
            CoreGameManager.Instance.GetCamera(i).mapCam.transform.SetParent(ec.map.transform, true);
        }
        GameCamera.dijkstraMap = new DijkstraMap(ec, PathType.Sound, int.MaxValue, CoreGameManager.Instance.GetCamera(0).listenerTra);
        GC.Collect();
        ec.Active = true;
        ec.BeginPlay();
        ec.PauseEnvironment(false);
        ec.PauseEvents(false);
        foreach (Cell cell in ec.AllCells())
            ec.UpdateLightingAtCell(cell);
        ec.UpdateQueuedLightChanges();
        if (ec.Npcs.Count <= 0 && ec.npcsToSpawn.Count > 0)
        {
            ec.SpawnNPCs();
            ec.StartEventTimers();
        }
        ec.ElevatorManager.SetAllElevators(ElevatorState.OutOfOrder);
    }

    public override void LoadNextLevel()
    {
        /*int stickerBonuses = CoreGameManager.Instance.GetStickerBonuses(ec.RemainingTime, (ec.GetBaldi() != null) ? ec.NavigableDistance(ec.CellFromPosition(CoreGameManager.Instance.GetPlayer(0).transform.position), ec.CellFromPosition(ec.GetBaldi().transform.position), PathType.Nav) : 0, ec.map.PlayerDiscoveredCells);
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
            _ = levelObject.finalLevel;*/

        elevatorScreen = Instantiate(elevatorScreenPre);
        elevatorScreen.OnLoadReady += () =>
        {
            Destroy(ElevatorScreen.Instance.gameObject);
            CoreGameManager.Instance.Quit();
            Destroy(gameObject);
        };
        elevatorScreen.Initialize();
    }

    protected override void LoadSceneObject(SceneObject sceneObject, bool restarting)
    {
        CoreGameManager.Instance.nextLevel = sceneObject;
        if (!levelObject.finalLevel || restarting)
            base.LoadSceneObject(pitStop, restarting);
        else
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
}

public class SubInitializer : GameInitializer
{
    [SerializeField] public SceneObject subSceneObject;
    [SerializeField] public LevelObject subLevelObject;
    [SerializeField] public Tuple<LevelAsset, ExtraLevelDataAsset> subLevelAsset;
    [SerializeField] public Tuple<LevelDataContainer, ExtraLevelDataAsset> subLevelContainer;
    [SerializeField] internal EnvironmentController ecPre;
    [SerializeField] internal LevelBuilder generatorPre, loaderPre;
    [SerializeField] internal Vector3 ecPos = Vector3.zero;
    private IEnumerator waitForGenerator;
    public Action<LevelBuilder, BaseGameManager> onReady;
    public BaseGameManager baseGameManager = BaseGameManager.Instance;
    public bool Ready { get; private set; } = false;

    public override void Initialize()
    {
        if (CoreGameManager.Instance == null || (baseGameManager == null && subSceneObject == null)) return;
        Time.timeScale = 0;
        EnvironmentController ec = Instantiate(ecPre, ecPos, default);
        LevelBuilder levelBuilder = null;
        if (baseGameManager == null && subSceneObject != null)
        {
            baseGameManager = Instantiate(subSceneObject.manager);
            baseGameManager.Ec = ec;
            baseGameManager.CurrentLevel = subSceneObject.levelNo;
            if (CoreGameManager.Instance.sceneObject == CoreGameManager.Instance.levelMapHasBeenPurchasedFor)
                baseGameManager.CompleteMapOnReady();
        }
        if (subLevelObject != null)
        {
            levelBuilder = Instantiate(generatorPre);
            LevelGenerationParameters levelGenerationParameters = new LevelGenerationParameters();
            levelGenerationParameters.AssignData(subLevelObject, baseGameManager.levelGenerationModifier);
            levelBuilder.ld = levelGenerationParameters;
            if (baseGameManager is CarnellBossManager)
                ((CarnellBossManager)baseGameManager).levelObjects.Add(levelGenerationParameters);
            else
            {
                baseGameManager.levelObject = levelGenerationParameters;
                baseGameManager.PrepareLevelGenerationData();
            }
        }
        else if (subLevelAsset != null)
        {
            levelBuilder = Instantiate(loaderPre);
            levelBuilder.levelAsset = subLevelAsset.Item1;
            levelBuilder.extraAsset = subLevelAsset.Item2;
        }
        else if (subLevelContainer != null)
        {
            levelBuilder = Instantiate(loaderPre);
            levelBuilder.levelContainer = subLevelContainer.Item1;
            levelBuilder.extraAsset = subLevelContainer.Item2;
        }

        /*baseGameManager.levelObject = subLevelObject;
        baseGameManager.Ec = ec;
        baseGameManager.CurrentLevel = CoreGameManager.Instance.sceneObject.levelNo;*/

        levelBuilder.scene = subSceneObject == null ? CoreGameManager.Instance.sceneObject : subSceneObject;
        levelBuilder.Ec = ec;
        levelBuilder.seedOffset = baseGameManager.CurrentLevel;
        levelBuilder.StartGenerate();
        waitForGenerator = WaitForGenerator(levelBuilder, baseGameManager);
        StartCoroutine(waitForGenerator);
    }

    protected new IEnumerator WaitForGenerator(LevelBuilder lb, BaseGameManager gm)
    {
        while (lb.levelInProgress && !lb.levelCreated)
            yield return null;
        Ready = true;
        onReady?.Invoke(lb, gm);
    }
}

public class B2HallRoomFunction : RoomFunction
{
    [SerializeField] internal Skybox skybox;
    [Range(1, int.MaxValue)] private int range = 9;
    private static readonly FieldInfo
        _skyboxTileH = AccessTools.DeclaredField(typeof(Skybox), "modifiedMeshHeight"),
        _tileH = AccessTools.DeclaredField(typeof(Skybox), "horizontalMesh"),
        _tileV = AccessTools.DeclaredField(typeof(Skybox), "verticalMesh");
    private bool NotHall => Room?.category != RoomCategory.Hall;
    [SerializeField] private Texture2D trans = Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent" && x.isReadable && x.GetInstanceID() >= 0);
    [Obsolete("Classic Reimplemented Code", true)]
    private MeshRenderer CreateCellMesh(int typeBin, Vector3 position, Material mat)
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "Cell" + typeBin.ToString();
        gameObject.AddComponent<MeshFilter>().mesh = room.ec.TileMesh(typeBin);
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.SetMaterial(mat);
        gameObject.transform.SetParent(room.transform);
        gameObject.transform.position = position;
        return meshRenderer;
    }

    public override void Initialize(RoomController room)
    {
        base.Initialize(room);
        room.florTex = room.ceilTex = trans;
        float num = room.ec.RealRoomMax(room).x - room.ec.RealRoomMin(room).x;
        float num2 = room.ec.RealRoomMax(room).z - room.ec.RealRoomMin(room).z;
        Vector3 vector = new Vector3(room.ec.RealRoomMax(room).x - num / 2f, 5f, room.ec.RealRoomMax(room).z - num2 / 2f);
        skybox.transform.position = vector;
        Vector3 vector2 = room.ec.RealRoomSize(room);
        vector2.x /= 10f;
        vector2.z /= 10f;
        vector2.y = 1;
        skybox.transform.localScale = vector2;
        _skyboxTileH.SetValue(skybox, range);
        foreach (var rend in (MeshRenderer[])_tileH.GetValue(skybox))
        {
            rend.transform.localPosition = new(rend.transform.localPosition.x, rend.transform.localPosition.y < 0f ? -10f : 10f, rend.transform.localPosition.z);
            rend.transform.localPosition += (rend.transform.localPosition.y < 0f ? Vector3.down : Vector3.up) * ((range - 1) * 5f);
            rend.transform.localScale += Vector3.up * ((range - 1) * 10f);
        }
        foreach (var rend in (MeshRenderer[])_tileV.GetValue(skybox))
        {
            rend.transform.localPosition = new(rend.transform.localPosition.x, rend.transform.localPosition.y < 0f ? -10f : 10f, rend.transform.localPosition.z);
            rend.transform.localPosition += (rend.transform.localPosition.y < 0f ? Vector3.down : Vector3.up) * ((range - 1) * 5f);
            rend.transform.localScale += Vector3.up * ((range - 1) * 10f);
        }
        skybox.InitializeMaterials(room.wallTex, room.size.x, room.size.z);
        room.cells[0].renderers.AddRange(skybox.GetComponent<RendererContainer>().renderers);
    }

    public override void OnGenerationFinished()
    {
        var entitysafe = room.AllEntitySafeCellsNoGarbage();
        Dictionary<Direction, List<MeshRenderer>> renderers = new Dictionary<Direction, List<MeshRenderer>>();
        foreach (var dir in Directions.All())
            renderers.Add(dir, new List<MeshRenderer>());
        for (var ch = 0; ch < skybox.transform.childCount; ch++)
        {
            var child = skybox.transform.GetChild(ch);
            foreach (var dir in Directions.All())
            {
                if (child.localRotation.eulerAngles == dir.ToRotation().eulerAngles)
                    renderers[dir].Add(child.GetComponent<MeshRenderer>());
            }
        }
        for (int c = 0; c < room.cells.Count; c++)
        {
            var cell = room.cells[c];
            if (cell.TileTransform.Find("FluorescentLight(Clone)") != null)
                Destroy(cell.TileTransform.Find("FluorescentLight(Clone)").gameObject);
            if (entitysafe.Contains(cell))
            {
                int bin = 0;
                foreach (var dir in Directions.All())
                {
                    if (room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir)) != null &&
                            !entitysafe.Contains(room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir))))
                        bin |= Directions.ToBinary(dir);
                }
                for (int i = 0; i < 4; i++)
                {
                    if (bin.ContainsDirection((Direction)i))
                    {
                        cell.Block((Direction)i, true);
                        cell.Tile.Collider((Direction)i).gameObject.SetActive(true);
                        cell.Tile.Collider((Direction)i).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast B");
                    }
                }
            }
            else
            {
                cell.TileTransform.gameObject.AddComponent<NavMeshObstacle>().size = Vector3.one * 10f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().radius = 7f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().height = 10f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().carving = true;
                for (int i = 0; i < 4; i++)
                    cell.Block((Direction)i, true);
            }
            foreach (var dir in Directions.All())
            {
                if (!cell.hideFromMap && (/*room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir)) == null
                    ||*/ room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir))?.hideFromMap == true))
                {
                    foreach (var rend in renderers[dir])
                    {
                        var newrend = Instantiate(rend, skybox.transform, true);
                        newrend.transform.SetParent(null, true);
                        newrend.transform.localScale = new(10f, newrend.transform.localScale.y, 10f);
                        newrend.transform.SetParent(skybox.transform, true);
                        newrend.transform.position = new(cell.TileTransform.position.x + (dir.ToVector3().x * 5f), newrend.transform.position.y, cell.TileTransform.position.z + (dir.ToVector3().z * 5f));
                        cell.AddRenderer(newrend);
                        var property = new MaterialPropertyBlock();
                        newrend.GetPropertyBlock(property);
                        property.SetVector("_Tiling", new Vector2(1, range));
                        newrend.SetPropertyBlock(property);
                    }
                }
            }
        }
        /*new Material(room.baseMat);
        Texture2D florTex = room.florTex;
        Texture2D wallTex = room.wallTex;
        Texture2D ceilTex = room.ceilTex;
        room.wallTex = wallTex;
        room.florTex = trans;
        room.ceilTex = trans;
        room.GenerateTextureAtlas();
        Material endlessMat = room.baseMat;
        room.wallTex = wallTex;
        room.florTex = florTex;
        room.ceilTex = ceilTex;
        room.GenerateTextureAtlas();
        var entitysafe = room.AllEntitySafeCellsNoGarbage();*/
        /*foreach (var cell in room.cells)
        {
            if ((!NotHall && cell.ConstBin == 0 && !room.ec.CellsFromPerimeter(cell.position, 2).Exists(x => x.ConstBin != 0))
                || NotHall)
            {
                cell.SetBase(endlessMat);
                if (cell.TileTransform.Find("FluorescentLight(Clone)") != null)
                    Destroy(cell.TileTransform.Find("FluorescentLight(Clone)").gameObject);
            }
            if (entitysafe.Contains(cell))
            {
                int bin = 0;
                using (List<Direction>.Enumerator enumerator2 = Directions.All().GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        Direction dir = enumerator2.Current;
                        if (room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir)) != null &&
                            !entitysafe.Contains(room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir))))
                            bin |= Directions.ToBinary(dir);
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    if (bin.ContainsDirection((Direction)i))
                    {
                        cell.Block((Direction)i, true);
                        cell.Tile.Collider((Direction)i).gameObject.SetActive(true);
                        cell.Tile.Collider((Direction)i).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast B");
                    }
                }
            }
            else if (NotHall)
            {
                cell.TileTransform.gameObject.AddComponent<NavMeshObstacle>().size = Vector3.one * 10f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().radius = 7f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().height = 10f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().carving = true;
                for (int i = 0; i < 4; i++)
                    cell.Block((Direction)i, true);
            }
        }
        using (List<Cell>.Enumerator enumerator = room.cells.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                Cell cell = enumerator.Current;
                int bin = 0;
                using (List<Direction>.Enumerator enumerator2 = Directions.All().GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        Direction dir = enumerator2.Current;
                        if (room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir)) == null
                            || room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir))?.hideFromMap == true)
                            bin |= Directions.ToBinary(dir);
                    }
                }
                if ((!NotHall && cell.ConstBin == 0 && !room.ec.CellsFromPerimeter(cell.position, 2).Exists(x => x.ConstBin != 0))
                || NotHall)
                    for (int i = -range; i <= range; i++)
                        if (i != 0)
                            cell.AddRenderer(CreateCellMesh(bin, cell.FloorWorldPosition + Vector3.up * (10f * i), endlessMat));
            }
        }*/
    }

    public void Refresh()
    {
        var entitysafe = room.AllEntitySafeCellsNoGarbage();
        foreach (var cell in room.cells)
        {
            if (entitysafe.Contains(cell))
            {
                int bin = 0;
                foreach (var dir in Directions.All())
                {
                    if (room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir)) != null &&
                        !entitysafe.Contains(room.cells.Find(x => x.position == cell.position + Directions.ToIntVector2(dir))))
                        bin |= Directions.ToBinary(dir);
                }
                for (int i = 0; i < 4; i++)
                {
                    if (bin.ContainsDirection((Direction)i))
                    {
                        cell.Block((Direction)i, true);
                        cell.Tile.Collider((Direction)i).gameObject.SetActive(true);
                        cell.Tile.Collider((Direction)i).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast B");
                    }
                }
            }
            else if (NotHall)
            {
                cell.TileTransform.gameObject.GetOrAddComponent<NavMeshObstacle>().size = Vector3.one * 10f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().radius = 7f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().height = 10f;
                cell.TileTransform.gameObject.GetComponent<NavMeshObstacle>().carving = true;
                for (int i = 0; i < 4; i++)
                    cell.Block((Direction)i, true);
            }
        }
    }
}

public class B2Button : EnvironmentObject, IClickable<int>
{
    public bool ClickableHidden() => false;

    public bool ClickableRequiresNormalHeight() => true;

    public void ClickableSighted(int player)
    {
    }

    public void ClickableUnsighted(int player)
    {
    }

    public void Clicked(int player)
    {
        BaseGameManager.Instance.CallSpecialManagerFunction(2, gameObject);
    }

    public override void LoadingFinished()
    {
        base.LoadingFinished();
        transform.position += Vector3.up * 5f;
        ec.OnEnvironmentBeginPlay += () => CarnellBossManager.BossInstance.myman = this;
    }
    internal bool waitingtogetcloser = false;

    private void Update()
    {
        if (waitingtogetcloser)
        {
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                if (Vector3.Distance(transform.position, CoreGameManager.Instance.GetPlayer(0).transform.position) <= 45f)
                {
                    waitingtogetcloser = false;
                    BaseGameManager.Instance.CallSpecialManagerFunction(1, null);
                }
            }
        }
    }
}

public class B2BaseRoomFunction : RoomFunction
{
    [SerializeField] private List<Animator> exteriorSigns = new List<Animator>();
    [SerializeField] internal Animator exteriorSignPrefab;
    protected override void OnFirstPlayerEnter(PlayerManager player)
    {
        base.OnFirstPlayerEnter(player);
        BaseGameManager.Instance.CallSpecialManagerFunction(0, null);
    }

    public override void OnGenerationFinished()
    {
        base.OnGenerationFinished();
        foreach (Door door in room.doors)
        {
            if (door.CellOnOtherSide(room).room.type == RoomType.Hall)
            {
                exteriorSigns.Add(Instantiate(exteriorSignPrefab, door.transform));
                DirectedIntVector2 directedIntVector = door.OtherSideDirectedIntVector2(room);
                exteriorSigns[exteriorSigns.Count - 1].transform.position = room.ec.CellFromPosition(directedIntVector.position).FloorWorldPosition;
                exteriorSigns[exteriorSigns.Count - 1].transform.rotation = directedIntVector.direction.ToRotation();
            }
        }
    }
}

public class B2BattleObject : MonoBehaviour, IEntityTrigger
{
    [SerializeField] internal Entity entity;
    [SerializeField] internal SpriteRenderer spriteRenderer;
    [SerializeField] internal SoundObject buzz, collected, hurt;
    [SerializeField] internal AudioManager audMan;
    private static readonly Sprite[] randoms = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.GetInstanceID() >= 0 && x.pixelsPerUnit >= 50f).ToArray();

    internal B2BattleObject Initialize(EnvironmentController ec)
    {
        entity.Initialize(ec, transform.position);
        spriteRenderer.sprite = randoms[UnityEngine.Random.RandomRangeInt(0, randoms.Length)];
        spriteRenderer.gameObject.AddComponent<PickupBob>();
        var propertyBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetInt("_SpriteColorGlitching", 1);
        propertyBlock.SetFloat("_SpriteColorGlitchPercent", 0.5f);
        propertyBlock.SetFloat("_SpriteColorGlitchVal", UnityEngine.Random.Range(0f, 4096f));
        spriteRenderer.SetPropertyBlock(propertyBlock);
        return this;
    }

    public void EntityTriggerEnter(Entity otherEntity, Collider other, bool validCollision)
    {
        var player = other.GetComponent<PlayerManager>();
        if (other.CompareTag("Player") && validCollision && otherEntity != null && player != null)
        {
            entity.SetTrigger(false);
            StartCoroutine(HeadToEvilGuy(entity.Ec.Npcs.First(x => x is BCarnellBoss).Entity));
            player.plm.AddStamina(player.plm.StaminaMax / 4f, true);
            audMan.PlaySingle(collected);
        }
    }

    private IEnumerator HeadToEvilGuy(Entity evilPerson)
    {
        var oldpos = transform.position;
        float elasped = 25f;
        while (Vector3.Distance(transform.position, evilPerson.transform.position) > 5f)
        {
            elasped += elasped * (Time.deltaTime * entity.Ec.EnvironmentTimeScale);
            transform.position = Vector3.MoveTowards(transform.position, evilPerson.transform.position, elasped * (Time.deltaTime * entity.Ec.EnvironmentTimeScale));
            yield return null;
        }
        var movemod = new MovementModifier(default, 0f);
        evilPerson.ExternalActivity.moveMods.Add(movemod);
        evilPerson.GetComponent<AudioManager>()?.PlaySingle(hurt, 3f);
        evilPerson.GetComponent<NPC>().Hear(null, oldpos, 126);
        evilPerson.GetComponent<NPC>().behaviorStateMachine.CurrentState.Hear(null, oldpos, 125);
        evilPerson.GetComponent<BCarnellBoss>().HP--;
        entity.SetFrozen(true);
        spriteRenderer.gameObject.SetActive(false);
        audMan.FlushQueue(true);
        audMan.audioSourceManager.spatialBlend = 0f;
        audMan.volumeModifier = 0.4f;
        audMan.QueueAudio(buzz, true);
        audMan.audioSourceManager.time = 0.735f;
        elasped = 0.25f * (1f + StickerManager.Instance.StickerValue(Sticker.BaldiPraise));
        Shader.SetGlobalInt("_ColorGlitching", 1);
        Shader.SetGlobalInt("_SpriteColorGlitching", 1);
        if (!PlayerFileManager.Instance.reduceFlashing)
        {
            Shader.SetGlobalFloat("_ColorGlitchPercent", 0.5f);
            Shader.SetGlobalFloat("_SpriteColorGlitchPercent", 0.5f);
        }
        else
        {
            Shader.SetGlobalFloat("_ColorGlitchPercent", 0.025f);
            Shader.SetGlobalFloat("_SpriteColorGlitchPercent", 0.025f);
        }
        while (elasped > 0f)
        {
            elasped -= Time.deltaTime;
            float power = elasped * 10f;
            Shader.SetGlobalFloat("_VertexGlitchSeed", UnityEngine.Random.Range(0f, 1000f));
            Shader.SetGlobalFloat("_TileVertexGlitchSeed", UnityEngine.Random.Range(0f, 1000f));
            if (!PlayerFileManager.Instance.reduceFlashing)
            {
                Shader.SetGlobalFloat("_VertexGlitchIntensity", Mathf.Pow(power, 2f));
                Shader.SetGlobalFloat("_TileVertexGlitchIntensity", Mathf.Pow(power, 2f));
                
                Shader.SetGlobalInt("_ColorGlitchVal", UnityEngine.Random.Range(0, 4096));
                Shader.SetGlobalInt("_SpriteColorGlitchVal", UnityEngine.Random.Range(0, 4096));
            }
            else
            {
                Shader.SetGlobalFloat("_VertexGlitchIntensity", power * 2f);
                Shader.SetGlobalFloat("_TileVertexGlitchIntensity", power * 2f);
            }
            yield return null;
        }
        evilPerson.ExternalActivity.moveMods.Remove(movemod);
        audMan.FlushQueue(true);
        CoreGameManager.Instance.ResetShaders();
        Destroy(gameObject);
        yield break;
    }

    public void EntityTriggerExit(Entity otherEntity, Collider other, bool validCollision)
    {
    }

    public void EntityTriggerStay(Entity otherEntity, Collider other, bool validCollision)
    {
    }
}

public class DeveloperNoteABW : Item
{
    [SerializeField] internal TextMeshProUGUI textt;
    [SerializeField] internal Canvas canvas;
    public override bool Use(PlayerManager pm)
    {
        CoreGameManager.Instance.disablePause = true;
        canvas.worldCamera = CoreGameManager.Instance.GetCamera(pm.playerNumber).canvasCam;
        pm.plm.Entity.SetFrozen(true);
        StartCoroutine(Hi(pm));
        GlobalCam.Instance.Transition(UiTransition.Dither, 0.01666667f);
        return true;
    }
    private IEnumerator Hi(PlayerManager pm)
    {
        var input = InputManager.Instance;
        string[] text = [ // Some bullshit that you have to like, reach to the end yourself.
            "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"
            ];
        for (int t = 0; t < text.Length; t++)
            text[t] = UnityCipher.RijndaelEncryption.Decrypt(text[t], "_ABW");
        int i = 0;
        while (i < text.Length)
        {
            textt.text = text[i];
            yield return null;
            while (!input.GetDigitalInput("Interact", true))
                yield return null;
            i++;
            yield return null;
        }
        GlobalCam.Instance.Transition(UiTransition.Dither, 0.01666667f);
        Destroy(gameObject);
        pm.plm.Entity.SetFrozen(false);
        pm.plm.am.moveMods.Add(new MovementModifier(default, 0f));
        CarnellBossManager.Instance.CallSpecialManagerFunction(3, null);
        yield break;
    }
}