using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Linq;
using MTM101BaldAPI.Reflection;
using HarmonyLib;
using System.Collections;
using System.Drawing;

namespace BCarnellChars.OtherStuff
{
    public class PAC_Machine : EnvironmentObject, IClickable<int>
    {
        private bool completed = false;
        [SerializeField] internal LevelObject pacLevel;
        [SerializeField] internal WeightedItemObject[] selection;
        private Vector3[] playerPrevPos;
        private Quaternion[] playerPrevRotat;
        private string prevMIDI;
        private double prevMIDIpos;
        private DijkstraMap prevDijkstra;

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
            List<TimeScaleModifier> timescale = ec.ReflectionGetVariable("timeScaleModifiers") as List<TimeScaleModifier>;
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
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                playerPrevPos[i] = CoreGameManager.Instance.GetPlayer(i).transform.position;
                playerPrevRotat[i] = CoreGameManager.Instance.GetPlayer(i).transform.localRotation;
                CoreGameManager.Instance.GetPlayer(i).plm.Entity.ExternalActivity.moveMods.RemoveAll(m => m != null);
            }
            foreach (Entity mob in FindObjectsOfType<Entity>(false).Where(m => m.gameObject.scene.name == "Game"))
                mob.SetActive(false);
            var canvas = Instantiate(Resources.FindObjectsOfTypeAll<Canvas>().ToList().Find(x => x.name == "FieldTripLoad").gameObject);
            GlobalCam.Instance.FadeIn(UiTransition.Dither, 0.01666667f);
            // I'm doing something wrong...
            Scene pacScene = SceneManager.CreateScene("PacGame");
            SceneManager.SetActiveScene(pacScene);
            RenderSettings.skybox = Resources.FindObjectsOfTypeAll<Material>().ToList().Last(s => s.name == "Skybox" && s.shader == Shader.Find("Shader Graphs/Skybox"));
            ec.Active = false;
            ec.gameObject.SetActive(false);
            FindObjectOfType<MainGameManager>().gameObject.SetActive(false);
            AccessTools.DeclaredField(typeof(Singleton<BaseGameManager>), "m_Instance").SetValue(null, null);
            GameObject prea = new GameObject("GameInitializer");
            prea.gameObject.SetActive(false);
            {
                GameInitializer initaliz = prea.AddComponent<GameInitializer>();
                //initaliz.ReflectionSetVariable("sceneObject", BasePlugin.Instance.sPac);
                // Never find the first prefab, worst mistake of my life.
                initaliz.ReflectionSetVariable("ecPre", Resources.FindObjectsOfTypeAll<EnvironmentController>().ToList().Find(x => x.name == "Environment Controller"));
                initaliz.ReflectionSetVariable("generatorPre", Resources.FindObjectsOfTypeAll<LevelGenerator>().ToList().Find(x => x.name == "LevelGenerator"));
                initaliz.ReflectionSetVariable("loaderPre", Resources.FindObjectsOfTypeAll<LevelLoader>().ToList().Find(x => x.name == "LevelLoader"));
            }
            SceneManager.MoveGameObjectToScene(canvas.gameObject, pacScene);
            SceneManager.MoveGameObjectToScene(prea, pacScene);
            prea.SetActive(true);
            PacGameManager.pacScene = pacScene;
        }

        public void Returned(bool win)
        {
            AccessTools.DeclaredField(typeof(Singleton<BaseGameManager>), "m_Instance").SetValue(null, FindObjectsOfType<MainGameManager>(true).ToList().Find(m => m.gameObject.scene.name == "Game"));
            ec.gameObject.SetActive(true);
            FindObjectsOfType<MainGameManager>(true).ToList().Find(m => m.gameObject.scene.name == "Game").gameObject.SetActive(true);
            CoreGameManager.Instance.disablePause = false;
            GameCamera.dijkstraMap = prevDijkstra;
            if (GameCamera.dijkstraMap.EnvironmentController != ec)
                GameCamera.dijkstraMap.ReflectionSetVariable("environment", ec);
            if (GameCamera.dijkstraMap.UpdateIsNeeded())
                GameCamera.dijkstraMap.QueueUpdate();
            //Destroy(FindObjectsOfType<Canvas>().ToList().Find(x => x.name.StartsWith("FieldTripLoad")).gameObject);
            ec.Active = true;
            ec.PauseEnvironment(false);
            ec.PauseEvents(false);
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
            {
                CoreGameManager.Instance.GetPlayer(i).transform.position = playerPrevPos[i];
                CoreGameManager.Instance.GetPlayer(i).transform.localRotation = playerPrevRotat[i];
                CoreGameManager.Instance.GetPlayer(i).ec = ec;
                CoreGameManager.Instance.GetPlayer(i).plm.Entity.Initialize(ec, CoreGameManager.Instance.GetPlayer(i).transform.position);
                CoreGameManager.Instance.GetPlayer(i).itm.Disable(false);
                CoreGameManager.Instance.GetCamera(i).SetControllable(true);
                CoreGameManager.Instance.GetCamera(i).matchTargetRotation = true;
                CoreGameManager.Instance.GetCamera(i).mapCam = ec.map.cams[0];
                CoreGameManager.Instance.GetHud(i).UpdateNotebookText(0, BaseGameManager.Instance.FoundNotebooks + "/" + Mathf.Max(ec.notebookTotal, BaseGameManager.Instance.FoundNotebooks), false);
                CoreGameManager.Instance.GetHud(i).UpdateInventorySize(CoreGameManager.Instance.GetPlayer(i).itm.maxItem + 1);
                CoreGameManager.Instance.GetPlayer(i).itm.UpdateItems();
            }
            foreach (Entity mob in FindObjectsOfType<Entity>(true).Where(m => m.gameObject.scene.name == "Game"))
                mob.SetActive(true);
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
                ec.gameObject.GetComponentInChildren<HappyBaldi>(true).GetComponent<AudioManager>().FlushQueue(true);
                ec.gameObject.GetComponentInChildren<HappyBaldi>(true).ReflectionInvoke("Activate", []);
            }
            AudioListener.pause = false;
            PropagatedAudioManager.paused = false;
            ec.ElevatorManager.SetAllElevators(ElevatorState.Closed);
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
