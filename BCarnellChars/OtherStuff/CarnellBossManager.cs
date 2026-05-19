using BCarnellChars.Characters;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.Registers;
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

    private IEnumerator ItemHallSpawner;

    protected override void AwakeFunction()
    {
        CoreGameManager.Instance.SetRandomSeed(); // For the funny...
    }

    private static readonly FieldInfo _seedText = AccessTools.DeclaredField(typeof(ElevatorScreen), "seedText");

    protected new void Start()
    {
        Environments = [ec];
        levelObjects = [levelObject];
        lightonRNG = new System.Random(CoreGameManager.Instance.Seed());
        for (int i = 0; i < 4; i++)
        {
            var subin = Instantiate(subinitPre, null, false);
            subin.subLevelObject = BCPPPlugin.Instance.lsubBoss[i];
            subin.baseGameManager = this;
            subin.onReady = (lb, gm) =>
            {
                Environments.Add(lb.Ec);
                lb.Ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().AddFunction(lb.Ec.mainHall.functionObject.AddComponent<B2HallRoomFunction>());
                lb.Ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().Initialize(lb.Ec.mainHall);
                lb.Ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().OnGenerationFinished();
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
                    if (lightonRNG.NextDouble() < lightonChance)
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
        if (CoreGameManager.Instance.currentMode == Mode.Free || CoreGameManager.Instance.lifeMode == LifeMode.Explorer || !BCPPSave.Instance.basementCompleted
            || Chainloader.PluginInfos.ContainsKey("com.exmagikguy.baldi.loguscheats") || Chainloader.PluginInfos.ContainsKey("com.sinai.unityexplorer") || Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.pinedebug"))
        {
            DogcheckBCPP.SendToDogcheckScreen();
            Destroy(gameObject);
            return;
        }
        ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().AddFunction(ec.mainHall.functionObject.AddComponent<B2HallRoomFunction>());
        ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().Initialize(ec.mainHall);
        ec.mainHall.functionObject.GetComponent<RoomFunctionContainer>().OnGenerationFinished();
        singleton = this;
        ec.ElevatorManager.PrepareToLeaveSpawn();
        var text = _seedText.GetValue(ElevatorScreen.Instance) as TMP_Text;
        text.text = CoreGameManager.Instance.Seed().ToString();
        ec.SetAllLights(false);
        foreach (Cell light in ec.lights)
        {
            if (lightonRNG.NextDouble() < lightonChance)
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
        LastEnvironments = Environments;
        LastLevelObjects = levelObjects;
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
            case 1:
                break;
        }
    }

    private void StartItemSpawnInHall()
    {
        if (ItemHallSpawner != null)
            StopCoroutine(ItemHallSpawner);
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
                var cell = ec.mainHall.AllTilesNoGarbage(false, false)[UnityEngine.Random.Range(0, ec.mainHall.AllTilesNoGarbage(false, false).Count)];
                ec.CreateItem(ec.mainHall, items[UnityEngine.Random.Range(0, items.Count - 1)], new(cell.CenterWorldPosition.x, cell.CenterWorldPosition.z));
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
            ec.Npcs.DoIf(x => x.Character.ToStringExtended().ToLower() == "BCarnellBoss".ToLower(), x => x.GetComponent<BCarnellBoss>().TheDialogue(ec.Players[0]));
            ec.AddTimeScale(new TimeScaleModifier(0f, 1f, 1f));
            MusicManager.Instance.PlayMidi(BCPPPlugin.bcppAssets.Get<string>("mus_BCarnellBossIntro"), false);
            yield return null;
            while (MusicManager.Instance.MidiPlaying)
                yield return null;
            ec.Npcs.DoIf(x => x.Character.ToStringExtended().ToLower() == "BCarnellBoss".ToLower(), x => x.GetComponent<AudioManager>().FlushQueue(true));
            allNotebooksFound = true;
            LastEnvironments = [.. Environments.Where(e => e != ec)];
            LastLevelObjects = [.. levelObjects.Where(ld => ld != levelObject)];
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
            ec.Npcs.DoIf(x => x.Character.ToStringExtended().ToLower() == "BCarnellBoss".ToLower(), x => x.GetComponent<BCarnellBoss>().InitPhase(BCarnellBoss.B2Boss.Phase1));
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
        ec.Npcs.DoIf(x => x.Character.ToStringExtended().ToLower() == "BCarnellBoss".ToLower(), x => x.GetComponent<BCarnellBoss>().ButtonPressed(ec.Players[0]));
    }

    private void Teleporttt()
    {
        bool flag = false;
        List<Cell> path = new List<Cell>();
        IntVector2 position = ec.Elevators[UnityEngine.Random.Range(0, ec.Elevators.Count)].Door.position;
        int num = 0;
        int carnellpos = 64;
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
            ec.Npcs.DoIf(x => x.Character.ToStringExtended().ToLower() == "BCarnellBoss".ToLower(), x =>
            {
                var bald = x as BCarnellBoss;
                bald.ClearSoundLocations();
                x.transform.position = path[carnellpos].FloorWorldPosition + Vector3.up * 5f;
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
        foreach (var banan in FindObjectsOfType<ITM_NanaPeel>(false))
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
        for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
        {
            CoreGameManager.Instance.GetPlayer(i).plm.Entity.Initialize(ec, CoreGameManager.Instance.GetPlayer(i).transform.position);
            CoreGameManager.Instance.GetCamera(i).mapCam.transform.SetParent(ec.map.transform, true);
            GameCamera.dijkstraMap = new DijkstraMap(ec, PathType.Sound, int.MaxValue, CoreGameManager.Instance.GetCamera(i).listenerTra);
        }
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
    private int range = 9;
    private bool NotHall => Room?.category != RoomCategory.Hall;
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

    public override void OnGenerationFinished()
    {
        new Material(room.baseMat);
        Texture2D florTex = room.florTex;
        Texture2D wallTex = room.wallTex;
        Texture2D ceilTex = room.ceilTex;
        Texture2D trans = Resources.FindObjectsOfTypeAll<Texture2D>().ToList().Find(x => x.name == "Transparent" && x.isReadable);
        room.wallTex = wallTex;
        room.florTex = trans;
        room.ceilTex = trans;
        room.GenerateTextureAtlas();
        Material endlessMat = room.baseMat;
        room.wallTex = wallTex;
        room.florTex = florTex;
        room.ceilTex = ceilTex;
        room.GenerateTextureAtlas();
        var entitysafe = room.AllEntitySafeCellsNoGarbage();
        foreach (var cell in room.cells)
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
        }
    }

    public void Refresh()
    {
        var entitysafe = room.AllEntitySafeCellsNoGarbage();
        foreach (var cell in room.cells)
        {
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