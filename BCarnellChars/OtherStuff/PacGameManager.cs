using BCarnellChars.Characters;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BCarnellChars.OtherStuff
{
    public class PacGameManager : BaseGameManager
    {
        private static PacGameManager singleton;
        public static PacGameManager PacInstance => singleton;

        public static SceneObject previousScene { get; internal set; }
        [SerializeField] private Ambience ambience;
        private ValueModifier speedMod = new ValueModifier(1, 1);

        private static float powerChance = 0.1f;
        private static System.Random powerRNG;

        private float fleeTime = 0;
        public bool Powered => fleeTime > 0;
        internal static Scene? pacScene;

        protected override void AwakeFunction()
        {
            if (Instance.gameObject.activeSelf) return;
            AccessTools.DeclaredField(typeof(Singleton<BaseGameManager>), "m_Instance").SetValue(null, this);
        }

        public override void RestartLevel() => LoadSceneObject(previousScene, true);

        protected override void LoadSceneObject(SceneObject sceneObject, bool restarting)
        {
            ec.Active = false;
            Time.timeScale = 0f;
            CoreGameManager.Instance.disablePause = true;
            AudioListener.pause = true;
            PropagatedAudioManager.paused = true;
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++) // Sometimes, it does not remove the modifier.
            {
                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().modifiers["walkSpeed"].Remove(speedMod);
                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().modifiers["runSpeed"].Remove(speedMod);
            }
            CoreGameManager.Instance.musicMan.FlushQueue(true);
            CoreGameManager.Instance.sceneObject = sceneObject;
            Shader.SetGlobalTexture("_Skybox", sceneObject.skybox);
            Shader.SetGlobalColor("_SkyboxColor", sceneObject.skyboxColor);
            GlobalCam.Instance.FadeIn(UiTransition.Dither, 0.01666667f);
            OnDestroy();
            FindObjectsOfType<PAC_Machine>(true).ToList().Find(x => x.gameObject.scene.name == "Game").Returned(!restarting);
            AudioListener.pause = false;
            PropagatedAudioManager.paused = false;
        }

        public override void Initialize()
        {
            singleton = this;
            powerRNG = new System.Random(CoreGameManager.Instance.Seed());
            completeMapOnReady = true;
            ec.AssignPlayers();
            var ghostroom = ec.rooms.Find(x => x.functionObject.GetComponent<PAC75Ghost_RoomContainer>() != null);
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                CoreGameManager.Instance.GetPlayer(i).plm.Entity.Initialize(ec, CoreGameManager.Instance.GetPlayer(i).transform.position);
                bool flag = false;
                while (!flag)
                {
                    var spawn = ec.RandomCell(false, true, true).CenterWorldPosition;
                    flag = true;
                    if (!flag)
                        break;

                    if (Vector3.Distance(ec.RealRoomMid(ghostroom), spawn) <= 450f)
                        flag = false;
                    CoreGameManager.Instance.GetPlayer(i).transform.position = spawn;
                }
                CoreGameManager.Instance.GetPlayer(i).itm.Disable(true);
                CoreGameManager.Instance.GetPlayer(i).plm.AddStamina(float.MaxValue, true);
                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().AddModifier("walkSpeed", speedMod);
                CoreGameManager.Instance.GetPlayer(i).GetMovementStatModifier().AddModifier("runSpeed", speedMod);
                CoreGameManager.Instance.GetHud(i).UpdateInventorySize(0);
            }
            ec.notebookTotal = 0;
            var mapicon = Resources.FindObjectsOfTypeAll<MapIcon>().ToList().Last(x => x.name == "Icon_Notebook");
            foreach (Cell cell in ec.AllCells())
                if (cell.TileTransform.Find("FluorescentLight(Clone)")?.GetComponentInChildren<MeshRenderer>() != null)
                    cell.TileTransform.Find("FluorescentLight(Clone)").GetComponentInChildren<MeshRenderer>().enabled = false;
            foreach (Cell cell in ec.AllTilesNoGarbage(false, false))
            {
                if (ec.TrapCheck(cell) || cell.room.type != RoomType.Hall) continue;
                if (!(powerRNG.NextDouble() >= (double)powerChance) && (cell.WallCount <= 0 || (cell.AllOpenNavDirections.Count >= 3 && cell.WallCount <= 1)))
                {
                    var powerpellet = Instantiate(BCPPPlugin.bcppAssets.Get<Pellet>("PAC75/SuperPellet").gameObject, cell.CenterWorldPosition, default);
                    powerpellet.GetComponent<Pellet>().icon = ec.map.AddIcon(mapicon, powerpellet.transform, Color.white);
                    powerpellet.GetComponent<Pellet>().icon.spriteRenderer.sprite = BCPPPlugin.bcppAssets.Get<Sprite>("PAC75/UIPowerPellett");
                    continue;
                }
                var pellet = Instantiate(BCPPPlugin.bcppAssets.Get<Pellet>("PAC75/Pellet").gameObject, cell.CenterWorldPosition, default);
                pellet.GetComponent<Pellet>().icon = ec.map.AddIcon(mapicon, pellet.transform, Color.white);
                pellet.GetComponent<Pellet>().icon.spriteRenderer.sprite = BCPPPlugin.bcppAssets.Get<Sprite>("PAC75/UIPellett");
                ec.notebookTotal++;
            }
            base.Initialize();
            GlobalCam.Instance.Transition(UiTransition.Dither, 0.01666667f);
            CoreGameManager.Instance.musicMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("mus_PAC75"), true);
            CoreGameManager.Instance.musicMan.SetLoop(true);
            Destroy(FindObjectsOfType<Canvas>().ToList().Find(x => x.name.StartsWith("FieldTripLoad")).gameObject);
            AudioListener.pause = false;
            PropagatedAudioManager.paused = false;
        }

        protected override void VirtualUpdate()
        {
            if (fleeTime > 0)
                fleeTime -= Time.deltaTime * ec.NpcTimeScale;
            if (fleeTime <= 0)
                fleeTime = 0;
        }

        public void StartFlee()
        {
            fleeTime = 25f;
        }

        public override void BeginPlay()
        {
            Time.timeScale = 1f;
            CoreGameManager.Instance.disablePause = false;
            InputManager.Instance.ActivateActionSet("InGame");
            CoreGameManager.Instance.PlayBegins();
            ec.Active = true;
            ec.BeginPlay();
            playStarted = true;
            AudioListener.pause = false;
            PropagatedAudioManager.paused = false;
            ambience.Initialize(ec);
        }

        public override void EndGame(Transform player, Baldi baldi)
        {
            Time.timeScale = 0f;
            MusicManager.Instance.StopMidi();
            CoreGameManager.Instance.disablePause = true;
            CoreGameManager.Instance.GetCamera(0).UpdateTargets(baldi.transform, 0);
            CoreGameManager.Instance.GetCamera(0).offestPos = (player.position - baldi.transform.position).normalized * 2f + Vector3.up;
            CoreGameManager.Instance.GetCamera(0).SetControllable(value: false);
            CoreGameManager.Instance.GetCamera(0).matchTargetRotation = false;
            CoreGameManager.Instance.audMan.volumeModifier = 0.6f;
            AudioManager audioManager = CoreGameManager.Instance.audMan;
            WeightedSelection<SoundObject>[] loseSounds = baldi.loseSounds;
            audioManager.PlaySingle(WeightedSelection<SoundObject>.RandomSelection(loseSounds));
            IEnumerator EndSequence()
            {
                float time2 = 1f;
                Shader.SetGlobalColor("_SkyboxColor", Color.black);
                while (time2 > 0f)
                {
                    time2 -= Time.unscaledDeltaTime;
                    CoreGameManager.Instance.GetCamera(0).camCom.farClipPlane = 500f * time2;
                    CoreGameManager.Instance.GetCamera(0).billboardCam.farClipPlane = 500f * time2;
                    yield return null;
                }

                CoreGameManager.Instance.GetCamera(0).camCom.farClipPlane = 1000f;
                CoreGameManager.Instance.GetCamera(0).billboardCam.farClipPlane = 1000f;
                CoreGameManager.Instance.GetCamera(0).StopRendering(true);
                CoreGameManager.Instance.audMan.FlushQueue(true);
                AudioListener.pause = true;
                time2 = 2f;
                while (time2 > 0f)
                {
                    time2 -= Time.unscaledDeltaTime;
                    yield return null;
                }

                RestartLevel();
            }
            CoreGameManager.Instance.StartCoroutine(EndSequence());
            InputManager.Instance.Rumble(1f, 2f);
        }

        public override void CollectNotebooks(int count)
        {
            base.CollectNotebooks(count);
            speedMod.addend = (foundNotebooks / (NotebookTotal / 38.652f)) + 2f;
            foreach (Baldi bald in ec.Npcs.FindAll(b => b.GetComponent<Baldi>() != null))
                bald.SetAnger(foundNotebooks / (NotebookTotal / 40.652f));
        }

        protected override void AllNotebooks()
        {
            if (allNotebooksFound || ec.notebookTotal <= 0) return;
            allNotebooksFound = true;
            CoreGameManager.Instance.disablePause = true;
            CoreGameManager.Instance.musicMan.FlushQueue(true);
            TimeScaleModifier pauseScale = new TimeScaleModifier(0f, 0f, 0f);
            ec.AddTimeScale(pauseScale);
            IEnumerator WaitUntilEnd()
            {
                yield return new WaitForSeconds(1.260f);
                ec.RemoveTimeScale(pauseScale);
                yield return null;
                LoadSceneObject(previousScene, false);
                yield break;
            }
            CoreGameManager.Instance.audMan.PlaySingle(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75/Victory"));
            StartCoroutine(WaitUntilEnd());
        }

        private void OnDestroy()
        {
            if (pacScene != null)
            {
                SceneManager.UnloadSceneAsync((Scene)pacScene, UnloadSceneOptions.None);
                pacScene = null;
            }
        }
    }

    public class PAC75Ghost_RoomContainer : RoomFunction
    {
        float time = UnityEngine.Random.Range(60, 75);
        public override void OnGenerationFinished()
        {
            room.doors.Do(d =>
            {
                d.lockBlocks = true; d.Lock(true);
            });
            Invoke("TeleportGhosties", 0.1f);
        }

        void TeleportGhosties()
        {
            foreach (var npc in room.ec.Npcs)
            {
                if (npc.Character.ToStringExtended() == "Blinky")
                {
                    npc.Navigator.Entity.Teleport(room.doors[0].bTile.CenterWorldPosition);
                    npc.GetComponent<PAC75Ghost>().home = room.doors[0].bTile.CenterWorldPosition;
                }
                else if (npc.Character.ToStringExtended() == "Inky")
                    npc.Navigator.Entity.Teleport(room.cells[0].CenterWorldPosition);
                else if (npc.Character.ToStringExtended() == "Cylde")
                    npc.Navigator.Entity.Teleport(room.cells[1].CenterWorldPosition);
                else if (npc.Character.ToStringExtended() == "Pinky")
                    npc.Navigator.Entity.Teleport(room.cells[2].CenterWorldPosition);
            }
        }

        void Update()
        {
            if (room == null) return;
            if (time > 0)
                time -= Time.deltaTime * room.ec.EnvironmentTimeScale;
            else
            {
                room.doors.Do(d =>
                {
                    d.Unlock();
                    d.Open(true, false);
                });
            }
        }
    }
}
