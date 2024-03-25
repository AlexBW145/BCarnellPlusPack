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

        //public MeshRenderer masking;

        private void Awake()
        {
            audMan = gameObject.GetComponents<AudioManager>()[0];

            audHungry = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(BasePlugin.Instance, "AudioClip", "NPCs", "Mr. Portal Man", "PTM_Intro.wav"), "Vfx_MrPortalMan_Intro", SoundType.Voice, new Color(1f, 0.6470588f, 0.1529412f));
            audWhere = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(BasePlugin.Instance, "AudioClip", "NPCs", "Mr. Portal Man", "PTM_Questioning.wav"), "Vfx_MrPortalMan_WhereIs", SoundType.Voice, new Color(1f, 0.6470588f, 0.1529412f));
            audFulfilled = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(BasePlugin.Instance, "AudioClip", "NPCs", "Mr. Portal Man", "PTM_End.wav"), "Vfx_MrPortalMan_Fulfilled", SoundType.Voice, new Color(1f, 0.6470588f, 0.1529412f));
            teleport = Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(b => b.name == "Teleport");
        }

        private void Start()
        {
            if (BasePlugin.RadarModExists && ec != null)
                Singleton<BaseGameManager>.Instance.Ec.map.AddArrow(transform, RadarSupport.MrPortalManColor.Value);
        }

        public override void Initialize()
        {
            base.Initialize();
            //masking.enabled = false;
            behaviorStateMachine.ChangeState(new MrPortalMan_Cooldown(this, UnityEngine.Random.RandomRangeInt(40, 80)));
            navigationStateMachine.ChangeState(new NavigationState_DoNothing(this, 0));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
            spriteRenderer[0].gameObject.SetActive(false);
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
            //masking.enabled = true;
            int random = UnityEngine.Random.Range(0, portals.Count);
            transform.position = portals[random].position + Vector3.back * 5f;
            transform.rotation = portals[random].rotation;

            audMan.PlaySingle(audHungry);
            behaviorStateMachine.ChangeState(new MrPortalMan_Wandering(this));
            navigator.maxSpeed = 18;
            navigator.SetSpeed(18);
            spriteRenderer[0].gameObject.SetActive(true);
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
            if (!idiot.CompareTag("Player")) {
                audMan.PlaySingle(teleport);
                if (idiot.GetComponent<AudioManager>() != null) idiot.GetComponent<AudioManager>().PlaySingle(teleport);
            }
            else
                CoreGameManager.Instance.audMan.PlaySingle(teleport);
            int random = UnityEngine.Random.Range(0, portals.Count);
            idiot.transform.position = portals[random].position + Vector3.back * 5f;
            idiot.transform.rotation = portals[random].rotation;
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
            navigationStateMachine.ChangeState(new NavigationState_DoNothing(this, 0));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
        }

        private IEnumerator hideFade()
        {
            float alpha = 1f;
            while (spriteRenderer[0].color.a > 0)
            {
                alpha -= 0.2f * (Time.deltaTime * TimeScale);
                spriteRenderer[0].color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, alpha);
                yield return null;
            }
            spriteRenderer[0].color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, 0);
            yield break;
        }

        private IEnumerator showFade()
        {
            float alpha = 0f;
            while (spriteRenderer[0].color.a < 1)
            {
                alpha += 0.2f * (Time.deltaTime * TimeScale);
                spriteRenderer[0].color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, alpha);
                yield return null;
            }
            spriteRenderer[0].color = new Color(spriteRenderer[0].color.r, spriteRenderer[0].color.g, spriteRenderer[0].color.b, 1);
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
    }
}
