using BCarnellChars.Characters.States;
using BCarnellChars.GeneratorStuff;
using BCarnellChars.ItemStuff;
using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Characters
{
    public class MrPortalMan : NPC
    {
        [SerializeField] internal AudioManager audMan;
        [SerializeField] internal SoundObject audHungry, audWhere, audFulfilled, teleport;

        [SerializeField] internal CryingPortal portalPre;
        private List<Cell> _spawnedPortals = new List<Cell>();
        private List<CryingPortal> portals = new List<CryingPortal>();

        private TileShapeMask tileShapes = TileShapeMask.Corner | TileShapeMask.Single;
        [SerializeField] private float hallspawnPercent = 15f;

        public Camera portalmanCam;
        [SerializeField] internal Camera outputCamPre;
        private Camera outputCam;
        public Camera OutputCam => outputCam;
        private CryingPortal currentOutput;

        public override void Initialize()
        {
            BasePlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/ManCamRender").Release();
            BasePlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/OutputCamRender").Release();
            this.ChangeRadarColor();
            base.Initialize();
            if (!gameObject.GetComponent<CullAffector>())
                gameObject.AddComponent<CullAffector>();
            behaviorStateMachine.ChangeState(new MrPortalMan_Cooldown(this, UnityEngine.Random.RandomRangeInt(40, 80)));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
            foreach (SpriteRenderer render in spriteRenderer)
                render.gameObject.SetActive(false);
            Navigator.Entity.SetTrigger(false);
            Navigator.Entity.SetInteractionState(false);

            List<RoomController> list = new List<RoomController>();
            foreach (RoomController room in ec.rooms)
                if (room.category == RoomCategory.Faculty || room.category == RoomCategory.Special)
                    list.Add(room);
            int num = Mathf.RoundToInt(list.Count * (70f / 100f));
            List<Cell> list2 = new List<Cell>();
            foreach (List<Cell> subcell in ec.FindHallways())
                foreach (Cell cell in subcell)
                {
                    if (cell.HasHardFreeWall && !cell.offLimits && cell.shape == TileShapeMask.Straight && cell.room.type == RoomType.Hall)
                        list2.Add(cell);
                }    
            int num2 = Mathf.RoundToInt(list2.Count * (hallspawnPercent / 100f));
            for (int i = 0; i < num; i++)
            {
                if (list.Count <= 0)
                    break;

                int index = UnityEngine.Random.Range(0, list.Count);
                SpawnBoard(list[index]);
                list.RemoveAt(index);
            }
            for (int i = 0; i < num2; i++)
            {
                if (list2.Count <= 0)
                    break;

                int index = UnityEngine.Random.Range(0, list2.Count);
                SpawnBoard(list2[index].room);
                list2.RemoveAt(index);
            }
            portalmanCam.enabled = true;
            outputCam = outputCamPre; // It was already cloned wdym??
            outputCam.gameObject.SetActive(true);
            outputCam.enabled = true;
            spriteRenderer[1].sprite = AssetLoader.SpriteFromTexture2D(Render(outputCam.targetTexture), 34f);
        }

        private void SpawnBoard(RoomController room)
        {
            _spawnedPortals = room.GetTilesOfShape(room.type == RoomType.Hall ? TileShapeMask.Straight : tileShapes, true);
            Cell cell = null;
            bool flag = false;
            while (!flag && _spawnedPortals.Count > 0)
            {
                cell = _spawnedPortals[UnityEngine.Random.Range(0, _spawnedPortals.Count)];
                if (cell.HasHardFreeWall & portals.TrueForAll(x => (cell.CenterWorldPosition - x.CellPos).magnitude > 200f)) // Side note: I can't decide on magnitudes without experimenting with them in-game.
                {
                    if (!ec.TrapCheck(cell) && cell.RandomUncoveredDirection(new System.Random(CoreGameManager.Instance.Seed())) != Direction.Null) {
                        portals.Add(Instantiate(portalPre, cell.TileTransform).Spawn(cell));
                        flag = true;
                    }
                }
                if (!flag) {
                    _spawnedPortals.Remove(cell);
                    flag = true;
                }
            }
        }

        protected override void VirtualUpdate()
        {
            if (Navigator.Entity.ExternalActivity.moveMods.Count > 0)
                Navigator.Entity.ExternalActivity.moveMods = [];
        }

        public void StartToDevour()
        {
            SwitchOutput();
            Navigator.Entity.Teleport(currentOutput.CellPos);
            transform.rotation = currentOutput.PortalRotat;

            audMan.PlaySingle(audHungry);
            behaviorStateMachine.ChangeState(new MrPortalMan_Wandering(this));
            navigator.maxSpeed = 18;
            navigator.SetSpeed(18);
            foreach (SpriteRenderer render in spriteRenderer)
                render.gameObject.SetActive(true);
            Navigator.Entity.SetTrigger(true);
            Navigator.Entity.SetInteractionState(true);
            StartCoroutine(showFade());
        }

        public void Questioning()
        {
            int callChance = UnityEngine.Random.RandomRangeInt(0, 2);
            if (callChance < 1 && !audMan.AnyAudioIsPlaying)
                audMan.PlaySingle(audWhere);
        }

        private static FieldInfo
            _maxPressure = AccessTools.DeclaredField(typeof(ITM_GrapplingHook), "maxPressure"),
            _pm = AccessTools.DeclaredField(typeof(Item), "pm");
        public void TeleportAnIdiot(Entity idiot)
        {
            if (idiot.CompareTag("NPC") && !idiot.GetComponent<Navigator>().isActiveAndEnabled)
                return;
            if (idiot.CompareTag("GrapplingHook"))
                idiot.GetComponent<ITM_GrapplingHook>().pressure = 9999f;
            if (idiot.CompareTag("GrapplingHook") || idiot.GetComponent<Balloon>())
                return;
            if (!idiot.CompareTag("Player"))
            {
                audMan.PlaySingle(teleport);
                if (idiot.GetComponent<AudioManager>() != null) idiot.GetComponent<AudioManager>().PlaySingle(teleport);
                if (idiot.gameObject.GetComponent<ITM_GrapplingHook>()) _maxPressure.SetValue(idiot.GetComponent<ITM_GrapplingHook>(), 0f);
            }
            else
            {
                CoreGameManager.Instance.audMan.PlaySingle(teleport);
                var grapplings = FindObjectsOfType<ITM_GrapplingHook>(false).Where(x => ((PlayerManager)_pm.GetValue(x)).plm.Entity == idiot).ToList();
                while (grapplings.Count > 0)
                {
                    _maxPressure.SetValue(grapplings[0], 0f);
                    grapplings.RemoveAt(0);
                }
            }
            if (idiot.CompareTag("Player") && FindObjectsOfType<ITM_AnyportalOutput>().Count(x => x.Ready && x.Owner == idiot.gameObject?.GetComponent<PlayerManager>()) > 0)
            {
                List<ITM_AnyportalOutput> placedOutputs = FindObjectsOfType<ITM_AnyportalOutput>().ToList().FindAll(x => x.Ready && x.Owner == idiot.gameObject?.GetComponent<PlayerManager>());
                int index = Mathf.RoundToInt(UnityEngine.Random.Range(0f, placedOutputs.Count - 1));
                idiot.Teleport(ec.CellFromPosition(placedOutputs[index].transform.position).CenterWorldPosition);
                idiot.transform.rotation = placedOutputs[index].transform.rotation;
                placedOutputs[index].Break();
            }
            else
            {
                idiot.Teleport(currentOutput.CellPos);
                idiot.transform.rotation = currentOutput.PortalRotat;
            }
            if (idiot.CompareTag("Player") | idiot.CompareTag("NPC"))
                SwitchOutput();
        }

        public void Rest()
        {
            //masking.enabled = false;
            audMan.FlushQueue(true);
            audMan.PlaySingle(teleport);
            audMan.PlaySingle(audFulfilled);
            StartCoroutine(hideFade());
            Navigator.Entity.SetTrigger(false);
            Navigator.Entity.SetInteractionState(false);
            behaviorStateMachine.ChangeState(new MrPortalMan_Cooldown(this, UnityEngine.Random.RandomRangeInt(40, 80)));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
        }

        private IEnumerator hideFade()
        {
            StopCoroutine(showFade());
            float alpha = 1f;
            while (spriteRenderer[0].color.a > 0)
            {
                alpha -= 0.2f * (Time.deltaTime * TimeScale);
                foreach (SpriteRenderer render in spriteRenderer)
                    render.color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, alpha);
                yield return null;
            }
            foreach (SpriteRenderer render in spriteRenderer)
                render.color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, 0f);
            yield break;
        }

        private IEnumerator showFade()
        {
            StopCoroutine(hideFade());
            float alpha = 0f;
            while (spriteRenderer[0].color.a < 1)
            {
                alpha += 0.2f * (Time.deltaTime * TimeScale);
                foreach (SpriteRenderer render in spriteRenderer)
                    render.color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, alpha);
                yield return null;
            }
            foreach (SpriteRenderer render in spriteRenderer)
                render.color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, 1f);
            yield break;
        }

        public override void Despawn()
        {
            base.Despawn();
            _spawnedPortals.Clear();
            foreach (CryingPortal portal in portals)
                Destroy(portal.gameObject);
            portals.Clear();
            foreach (ITM_AnyportalOutput plac in FindObjectsOfType<ITM_AnyportalOutput>()) // No purpose of existing if Mr. Portal Man has despawned
                if (plac.isActiveAndEnabled) plac.Break(true);
        }

        private void OnDestroy()
        {
            if (spriteRenderer[1].sprite != null) // Sprite has Texture2D, both must be eliminated at once.
            {
                Destroy(spriteRenderer[1].sprite.texture);
                Destroy(spriteRenderer[1].sprite);
            }
            BasePlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/ManCamRender").Release();
            BasePlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/OutputCamRender").Release();
        }

        private void SwitchOutput()
        {
            portalmanCam.targetTexture.Release();
            //outputCam.targetTexture.Release();
            int random = UnityEngine.Random.Range(0, portals.Count);
            outputCam.transform.SetParent(portals[random].transform, false);
            outputCam.transform.position = portals[random].CellPos;
            outputCam.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            /*if (currentOutput != null && currentOutput.gameObject.GetComponent<CullAffector>())
                Destroy(currentOutput.GetComponent<CullAffector>());*/
            currentOutput = portals[random];
            /*if (!currentOutput.gameObject.GetComponent<CullAffector>())
                currentOutput.gameObject.AddComponent<CullAffector>();*/
            if (spriteRenderer[1].sprite != null)
            {
                Destroy(spriteRenderer[1].sprite.texture);
                Destroy(spriteRenderer[1].sprite); // I don't want to fill up de assets!
            }
            spriteRenderer[1].sprite = AssetLoader.SpriteFromTexture2D(Render(outputCam.targetTexture), 34f);
            spriteRenderer[1].sprite.name = "Spr_PortalOutputRenderTextSpr";
        }

        private Texture2D Render(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);
            var old_rt = RenderTexture.active;
            RenderTexture.active = rTex;

            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            tex.name = "PortalOutputRenderTextShot";

            RenderTexture.active = old_rt;
            return tex;
        }
    }
}
