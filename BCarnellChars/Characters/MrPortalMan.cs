using BCarnellChars.Characters.States;
using BCarnellChars.GeneratorStuff;
using BCarnellChars.Patches;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters
{
    public class MrPortalMan : NPC
    {
        private AudioManager audMan;
        private SoundObject audHungry;
        private SoundObject audWhere;
        private SoundObject audFulfilled;
        private SoundObject teleport;

        public CryingPortal portalPre;
        private List<Cell> _spawnedPortals = new List<Cell>();
        private List<Transform> portals = new List<Transform>();

        private List<TileShape> tileShapes = new List<TileShape>()
        {
            TileShape.Corner,
            TileShape.Single
        };

        public Camera portalmanCam;
        public Camera outputCamPre;
        private Camera outputCam;
        private Transform currentOutput;

        private void Awake()
        {
            audMan = gameObject.GetComponents<AudioManager>()[0];

            audHungry = BasePlugin.bcppAssets.Get<SoundObject>("MrPortalMan/Hungry");
            audWhere = BasePlugin.bcppAssets.Get<SoundObject>("MrPortalMan/WhereIs");
            audFulfilled = BasePlugin.bcppAssets.Get<SoundObject>("MrPortalMan/Fullfilled");
            teleport = Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(b => b.name == "Teleport");

            spriteRenderer[1].sprite = AssetLoader.SpriteFromTexture2D(Render(outputCamPre.targetTexture), 34f);
        }

        private void Start()
        {
            if (BasePlugin.RadarModExists && ec != null)
                Singleton<BaseGameManager>.Instance.Ec.map.AddArrow(transform, RadarSupport.MrPortalManColor.Value);
        }

        public override void Initialize()
        {
            base.Initialize();
            portalmanCam.enabled = true;
            outputCam = Instantiate(outputCamPre, transform, false);
            outputCam.gameObject.SetActive(true);
            outputCam.enabled = true;
            spriteRenderer[1].sprite = AssetLoader.SpriteFromTexture2D(Render(outputCam.targetTexture), 34f);
            behaviorStateMachine.ChangeState(new MrPortalMan_Cooldown(this, UnityEngine.Random.RandomRangeInt(40, 80)));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
            foreach (SpriteRenderer render in spriteRenderer)
                render.gameObject.SetActive(false);
            baseTrigger[0].enabled = false;

            List<RoomController> list = new List<RoomController>();
            foreach (RoomController room in ec.rooms)
                if (room.category == RoomCategory.Faculty || room.category == RoomCategory.Special)
                    list.Add(room);
            int num = Mathf.RoundToInt(list.Count * (70f / 100f));
            for (int i = 0; i < num; i++)
            {
                if (list.Count <= 0)
                {
                    break;
                }
                int index = UnityEngine.Random.Range(0, list.Count);
                SpawnBoard(list[index]);
                list.RemoveAt(index);
            }
        }

        private void LateUpdate()
        {
            if (!gameObject.activeSelf || Time.timeScale == 0)
                return;
            Render(portalmanCam.targetTexture);
            Render(outputCam.targetTexture);
        }

        private void SpawnBoard(RoomController room)
        {
            _spawnedPortals = room.GetTilesOfShape(tileShapes, true);
            Cell cell = null;
            bool flag = false;
            while (!flag && _spawnedPortals.Count > 0)
            {
                cell = _spawnedPortals[UnityEngine.Random.Range(0, _spawnedPortals.Count)];
                if (cell.HasFreeWall)
                {
                    var port = Instantiate(portalPre, cell.TileTransform).Spawn(cell);
                    port.gameObject.SetActive(true);
                    portals.Add(port);
                    flag = true;
                }
                else
                {
                    _spawnedPortals.Remove(cell);
                }
            }
        }

        public void StartToDevour()
        {
            SwitchOutput();
            transform.position = currentOutput.position + Vector3.back * 5f;
            transform.rotation = currentOutput.rotation;

            audMan.PlaySingle(audHungry);
            behaviorStateMachine.ChangeState(new MrPortalMan_Wandering(this));
            navigator.maxSpeed = 18;
            navigator.SetSpeed(18);
            foreach (SpriteRenderer render in spriteRenderer)
                render.gameObject.SetActive(true);
            baseTrigger[0].enabled = true;
            StartCoroutine(showFade());
        }

        public void Questioning()
        {
            int callChance = UnityEngine.Random.RandomRangeInt(0, 2);
            if (callChance < 1 && !audMan.AnyAudioIsPlaying)
                audMan.PlaySingle(audWhere);
        }

        public void TeleportAnIdiot(Entity idiot)
        {
            if (idiot.CompareTag("GrapplingHook"))
                idiot.GetComponent<ITM_GrapplingHook>().pressure = 9999f;
            if (idiot.CompareTag("GrapplingHook") || idiot.GetComponent<Balloon>())
                return;
            if (!idiot.CompareTag("Player"))
            {
                audMan.PlaySingle(teleport);
                if (idiot.GetComponent<AudioManager>() != null) idiot.GetComponent<AudioManager>().PlaySingle(teleport);
            }
            else
            {
                CoreGameManager.Instance.audMan.PlaySingle(teleport);
                if (FindObjectOfType<ITM_GrapplingHook>())
                {
                    ITM_GrapplingHook grapple = FindObjectOfType<ITM_GrapplingHook>();
                    if (grapple.pressure < 9999f)
                        grapple.pressure = 9999f;
                }
            }
            idiot.transform.position = currentOutput.position + Vector3.back * 5f;
            idiot.transform.rotation = currentOutput.rotation;
            SwitchOutput();
        }

        public void Rest()
        {
            //masking.enabled = false;
            audMan.FlushQueue(true);
            audMan.PlaySingle(teleport);
            audMan.PlaySingle(audFulfilled);
            StartCoroutine(hideFade());
            baseTrigger[0].enabled = false;
            behaviorStateMachine.ChangeState(new MrPortalMan_Cooldown(this, UnityEngine.Random.RandomRangeInt(40, 80)));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
        }

        private IEnumerator hideFade()
        {
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
            foreach (Transform portal in portals)
                Destroy(portal.gameObject);
            portals.Clear();
        }

        private void SwitchOutput()
        {
            int random = UnityEngine.Random.Range(0, portals.Count);
            outputCam.transform.SetParent(portals[random].transform, false);
            outputCam.transform.rotation.eulerAngles.Set(0f, 180f, 0f);
            currentOutput = portals[random];
        }

        private Texture2D Render(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
            var old_rt = RenderTexture.active;
            RenderTexture.active = rTex;

            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            RenderTexture.active = old_rt;
            return tex;
        }
    }
}
