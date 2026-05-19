using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using HarmonyLib;
using System.Reflection;

namespace BCarnellChars.OtherStuff
{
    public class PAC_Machine : EnvironmentObject, IClickable<int>
    {
        private bool completed = false;
        [SerializeField] internal LevelObject pacLevel;
        [SerializeField] internal WeightedItemObject[] selection;
        private Vector3[] playerPrevPos;
        private Quaternion[] playerPrevRotat;
        private bool[] playerPrevFlipped;
        private string prevMIDI;
        private double prevMIDIpos;
        private DijkstraMap prevDijkstra;

        private static readonly FieldInfo
            _timeScaleModifiers = AccessTools.DeclaredField(typeof(EnvironmentController), "timeScaleModifiers"),
            _instance = AccessTools.DeclaredField(typeof(Singleton<BaseGameManager>), "m_Instance"),
            _ecPre = AccessTools.DeclaredField(typeof(GameInitializer), "ecPre"),
            _generatorPre = AccessTools.DeclaredField(typeof(GameInitializer), "generatorPre"),
            _loaderPre = AccessTools.DeclaredField(typeof(GameInitializer), "loaderPre"),
            _dijiEC = AccessTools.DeclaredField(typeof(DijkstraMap), "environment");
        private static readonly MethodInfo
            happyInvoke = AccessTools.DeclaredMethod(typeof(HappyBaldi), "Activate");

        public bool ClickableHidden()
        {
            return completed;
        }

        public bool ClickableRequiresNormalHeight()
        {
            return false;
        }

        public void ClickableSighted(int player)
        {
        }

        public void ClickableUnsighted(int player)
        {
        }

        public void Clicked(int player)
        {
            if (completed) return;
            PacGameManager.previousScene = CoreGameManager.Instance.sceneObject;
            prevDijkstra = GameCamera.dijkstraMap;
            completed = true;
            CoreGameManager.Instance.disablePause = true;
            CoreGameManager.Instance.readyToStart = false;
            AudioListener.pause = true;
            PropagatedAudioManager.paused = true;
            ec.PauseEnvironment(true);
            ec.PauseEvents(true);
            List<TimeScaleModifier> timescale = _timeScaleModifiers.GetValue(ec) as List<TimeScaleModifier>;
            timescale.DoIf(t => t.playerTimeScale != 1, t => ec.RemoveTimeScale(t));
            if (MusicManager.Instance.MidiPlaying)
            {
                prevMIDI = MusicManager.Instance.MidiPlayer.MPTK_MidiName;
                prevMIDIpos = MusicManager.Instance.MidiPlayer.MPTK_Position;
                MusicManager.Instance.StopMidi();
            }
            CoreGameManager.Instance.sceneObject = BCPPPlugin.Instance.sPac;
            playerPrevPos = new Vector3[CoreGameManager.Instance.setPlayers];
            playerPrevRotat = new Quaternion[CoreGameManager.Instance.setPlayers];
            playerPrevFlipped = new bool[CoreGameManager.Instance.setPlayers];
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                var pp = CoreGameManager.Instance.GetPlayer(i);
                playerPrevFlipped[i] = pp.plm.Entity.Flipped;
                if (pp.plm.Entity.Flipped)
                    pp.plm.Entity.Flip();
                playerPrevPos[i] = pp.transform.position;
                playerPrevRotat[i] = pp.transform.localRotation;
                pp.plm.Entity.ExternalActivity.moveMods.RemoveAll(m => m != null);
            }
            ec.Active = false;
            foreach (var obj in FindObjectsOfType<GameObject>(true).Where(m => m.gameObject.scene.Equals(ec.gameObject.scene) && m.transform.parent == null))
                obj.SetActive(false);
            var canvas = Instantiate(Resources.FindObjectsOfTypeAll<Canvas>().Last(x => x.name == "FieldTripLoad").gameObject);
            GlobalCam.Instance.FadeIn(UiTransition.Dither, 0.01666667f);
            // I'm doing something wrong...
            Scene pacScene = SceneManager.CreateScene("PacGame");
            SceneManager.SetActiveScene(pacScene);
            //FindObjectOfType<MainGameManager>().gameObject.SetActive(false);
            RenderSettings.skybox = Resources.FindObjectsOfTypeAll<Material>().Last(s => s.name == "Skybox" && s.shader == Shader.Find("Shader Graphs/Skybox"));
            AccessTools.DeclaredField(typeof(Singleton<BaseGameManager>), "m_Instance").SetValue(null, null);
            GameObject prea = new GameObject("GameInitializer");
            prea.gameObject.SetActive(false);
            {
                GameInitializer initaliz = prea.AddComponent<GameInitializer>();
                //initaliz.ReflectionSetVariable("sceneObject", BasePlugin.Instance.sPac);
                // Never find the first prefab, worst mistake of my life.
                _ecPre.SetValue(initaliz, Resources.FindObjectsOfTypeAll<EnvironmentController>().Last(x => x.name == "Environment Controller" && x.GetInstanceID() >= 0));
                _generatorPre.SetValue(initaliz, Resources.FindObjectsOfTypeAll<LevelGenerator>().Last(x => x.name == "LevelGenerator" && x.GetInstanceID() >= 0));
                _loaderPre.SetValue(initaliz, Resources.FindObjectsOfTypeAll<LevelLoader>().Last(x => x.name == "LevelLoader" && x.GetInstanceID() >= 0));
            }
            SceneManager.MoveGameObjectToScene(canvas.gameObject, pacScene);
            SceneManager.MoveGameObjectToScene(prea, pacScene);
            prea.SetActive(true);
            PacGameManager.pacScene = pacScene;
        }

        public void Returned(bool win)
        {
            _instance.SetValue(null, FindObjectsOfType<MainGameManager>(true).First(m => m.gameObject.scene.Equals(ec.gameObject.scene)));
            foreach (var obj in FindObjectsOfType<GameObject>(true).Where(m => m.gameObject.scene.Equals(ec.gameObject.scene) && m.transform.parent == null))
                obj.SetActive(true);
            //FindObjectsOfType<MainGameManager>(true).ToList().Find(m => m.gameObject.scene.name == "Game").gameObject.SetActive(true);
            CoreGameManager.Instance.disablePause = false;
            if (prevDijkstra.EnvironmentController != ec)
                _dijiEC.SetValue(prevDijkstra, ec);
            if (prevDijkstra.UpdateIsNeeded())
                prevDijkstra.QueueUpdate();
            GameCamera.dijkstraMap = prevDijkstra;
            //Destroy(FindObjectsOfType<Canvas>().ToList().Find(x => x.name.StartsWith("FieldTripLoad")).gameObject);
            ec.Active = true;
            ec.PauseEnvironment(false);
            ec.PauseEvents(false);
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                var player = CoreGameManager.Instance.GetPlayer(i);
                player.transform.position = playerPrevPos[i];
                player.transform.localRotation = playerPrevRotat[i];
                player.ec = ec;
                player.plm.Entity.Initialize(ec, playerPrevPos[i]);
                player.itm.Disable(false);
                CoreGameManager.Instance.GetCamera(i).SetControllable(true);
                CoreGameManager.Instance.GetCamera(i).matchTargetRotation = true;
                CoreGameManager.Instance.GetCamera(i).mapCam = ec.map.cams[0];
                CoreGameManager.Instance.GetHud(i).UpdateNotebookText(0, BaseGameManager.Instance.FoundNotebooks + "/" + Mathf.Max(ec.notebookTotal, BaseGameManager.Instance.FoundNotebooks), false);
                CoreGameManager.Instance.GetHud(i).UpdateInventorySize(player.itm.maxItem + 1);
                player.itm.UpdateItems();
                if (playerPrevFlipped[i] == true)
                    player.plm.Entity.Flip();
            }
            CoreGameManager.Instance.ResetCameras();
            CoreGameManager.Instance.ResetShaders();
            foreach (Cell cell in ec.AllCells())
                ec.UpdateLightingAtCell(cell);
            gameObject.GetComponent<MeshRenderer>().SetMaterial(BCPPPlugin.bcppAssets.Get<Material>("PAC75/MachineInactive"));
            if (win)
            {
                Cell cell = ec.CellFromPosition(transform.position);
                ItemObject item = WeightedItemObject.ControlledRandomSelection(selection,
                        new(CoreGameManager.Instance.Seed()));
                ec.CreateItem(cell.room, item, new Vector2(cell.CenterWorldPosition.x, cell.CenterWorldPosition.z));
            }
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(prevMIDI))
            {
                MusicManager.Instance.PlayMidi(prevMIDI, true);
                MusicManager.Instance.MidiPlayer.MPTK_Position = prevMIDIpos;
            }
            if (ec.gameObject.GetComponentInChildren<HappyBaldi>(true) != null)
            {
                var happy = ec.gameObject.GetComponentInChildren<HappyBaldi>(true);
                happy.GetComponent<AudioManager>().FlushQueue(true);
                happyInvoke.Invoke(happy, []);
            }
            AudioListener.pause = false;
            PropagatedAudioManager.paused = false;
            foreach (var door in ec.ElevatorManager.Elevators)
            {
                var amopen = door.Door.IsOpen;
                door.Door.Open(false, false);
                if (!amopen)
                    door.Door.Shut();
            }
        }
    }

    public class Pellet : MonoBehaviour
    {
        public bool Powered => gameObject.name.StartsWith("PacSuperPellet");
        private SoundObject waka => Powered ? BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75/WakaSuper") : BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75/Waka");
        public MapIcon icon;
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                gameObject.SetActive(false);
                CoreGameManager.Instance.audMan.PlaySingle(waka);
                icon.gameObject.SetActive(false);
                if (Powered)
                {
                    other.GetComponent<PlayerManager>().plm.AddStamina(10, true);
                    PacGameManager.PacInstance.StartFlee();
                    return;
                }
                BaseGameManager.Instance.CollectNotebooks(1);
                other.GetComponent<PlayerManager>().plm.AddStamina(1, true);
            }
        }
    }
}
