using HarmonyLib;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
#if DEBUG
namespace BCarnellChars.Characters
{
    public class MrRobber : NPC
    {
        public Canvas overlay;
        private MovementModifier playerMod = new MovementModifier(Vector3.zero, 0.1f);

        private AudioManager audMan;
        public AudioManager sfxMan;

        private List<SoundObject> voiceIntro = new List<SoundObject>();
        private List<SoundObject> voiceFlee = new List<SoundObject>();
        private SoundObject sfxBeat;
        private SoundObject[] sfxScreech;

        private Sprite wanderingSprite;
        private Sprite huntingSprite;
        private void Awake()
        {
            audMan = gameObject.GetComponents<AudioManager>()[0];

            voiceIntro = [
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/BlastingGreenades"),
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/Notebooks"),
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/Worse"),
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/Shine")
                ];
            voiceFlee = [
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/Flee"),
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/WasNeverHere"),
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/RegretAlone")
                ];
            sfxBeat = BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/AnnoyingBeat");
            sfxScreech = [
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/ScreechIntro"),
                BasePlugin.bcppAssets.Get<SoundObject>("MrRobber/ScreechLoop")
                ];

            wanderingSprite = BasePlugin.bcppAssets.Get<Sprite>("MrRobber/Idle");
            huntingSprite = BasePlugin.bcppAssets.Get<Sprite>("MrRobber/Targeting");
            spriteRenderer[0].sprite = wanderingSprite;
        }

        public override void Initialize()
        {
            base.Initialize();
            navigator.accel = 1000f;
            behaviorStateMachine.ChangeState(new MrRobber_Hunting(this));
        }

        protected override void VirtualUpdate()
        {
            if (behaviorStateMachine.CurrentNavigationState.GetType() == typeof(NavigationState_PartyEvent))
                behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 1));

            if (behaviorStateMachine.CurrentState.GetType() == typeof(MrRobber_Hunting))
            {
                if (spriteRenderer[0].color == Color.clear)
                {
                    if (sfxMan.volumeModifier > 0f)
                        sfxMan.volumeModifier -= .1f * ec.NpcTimeScale;
                    else if (sfxMan.volumeModifier <= 0f)
                        sfxMan.volumeModifier = 0f;
                }
                else
                {
                    if (sfxMan.volumeModifier < 1f)
                        sfxMan.volumeModifier += .1f * Time.deltaTime;
                    else if (sfxMan.volumeModifier >= 1f)
                        sfxMan.volumeModifier = 1f;
                }
            }
            else if (sfxMan.volumeModifier < 1f)
                sfxMan.volumeModifier = 1f;
        }

        public IEnumerator GoNuts()
        {
            if (behaviorStateMachine.CurrentState.GetType() == typeof(MrRobber_Crazy))
                yield break;
            navigator.maxSpeed = 0f;
            navigator.SetSpeed(0f);
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 1));
            behaviorStateMachine.ChangeState(new MrRobber_Crazy(this));

            audMan.QueueRandomAudio(voiceIntro.ToArray());
            audMan.QueueAudio(sfxScreech[0]);
            audMan.QueueAudio(sfxScreech[1]);
            sfxMan.FlushQueue(false);
            spriteRenderer[0].sprite = huntingSprite;

            while (sfxMan.QueuedAudioIsPlaying)
                yield return null;

            sfxMan.QueueAudio(sfxBeat);
            sfxMan.SetLoop(true);

            while (audMan.audioDevice.clip != sfxScreech[0].soundClip)
                yield return null;

            sfxMan.audioDevice.dopplerLevel = 1f;
            navigator.maxSpeed = 30f;
            navigator.SetSpeed(30f);
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 1));

            while (audMan.audioDevice.clip != sfxScreech[1].soundClip)
                yield return null;
            audMan.SetLoop(true);

            yield break;
        }

        public void DoneFor(PlayerManager player)
        {
            if (behaviorStateMachine.CurrentState.GetType() != typeof(MrRobber_Crazy))
                return;
            AudioMixer mixer = Resources.FindObjectsOfTypeAll<AudioMixer>().ToList().Find(x => x.name.ToLower() == "master");

            player.plm.am.moveMods.Add(playerMod);
            overlay.gameObject.SetActive(true);
            overlay.worldCamera = Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).canvasCam;

            sfxMan.FlushQueue(true);
            sfxMan.audioDevice.dopplerLevel = 0f;
            spriteRenderer[0].color = Color.clear;

            player.itm.RemoveRandomItem();
            player.itm.RemoveRandomItem();
            player.itm.RemoveRandomItem();
            player.plm.stamina = -10f;

            AudioListener.volume = 0f;
            StartCoroutine(volumeListenerRise(player, mixer));

            audMan.FlushQueue(true);
            audMan.PlayRandomAudio(voiceFlee.ToArray());
            behaviorStateMachine.ChangeState(new MrRobber_DoNothing(this));
        }

        private IEnumerator volumeListenerRise(PlayerManager player, AudioMixer mixer)
        {
            var audioJungle = 0f;
            var idkhow = 1f;
            Image whiteBang = overlay.transform.Find("Image").GetComponent<Image>();
            while (AudioListener.volume < 1f)
            {
                audioJungle += .06f * (Time.deltaTime * ec.NpcTimeScale);
                AudioListener.volume = audioJungle;
                if (idkhow > 0f)
                {
                    idkhow -= .1f * (Time.deltaTime * ec.NpcTimeScale);
                    whiteBang.color = new Color(1f, 1f, 1f, idkhow);
                }
                else
                {
                    idkhow = 0f;
                    overlay.gameObject.SetActive(false);
                }
                yield return null;
            }
            AudioListener.volume = 1f;
            player.plm.am.moveMods.Remove(playerMod);
            overlay.gameObject.SetActive(false);
            whiteBang.color = Color.white;
            yield break;
        }
    }

    public class MrRobber_StateBase : NpcState
    {
        protected MrRobber robber;

        public MrRobber_StateBase(MrRobber npc)
            : base(npc)
        {
            robber = npc;
        }
    }

    public class MrRobber_Hunting : MrRobber_StateBase
    {
        public MrRobber_Hunting(MrRobber npc)
            : base(npc)
        {
        }

        private List<GameObject> sightedNPCs = new List<GameObject>();
        private float timeInSight = 0f;

        public override void Initialize()
        {
            base.Initialize();
            robber.Navigator.maxSpeed = 15f;
            robber.Navigator.SetSpeed(15f);
            robber.Navigator.SetRoomAvoidance(true);
            ChangeNavigationState(new NavigationState_WanderRandom(robber, 1));
        }

        // Never open doors, ever.
        public override void DoorHit(StandardDoor door)
        {
        }

        public override void Update()
        {
            foreach (NPC npc in robber.ec.Npcs)
            {
                if (npc.gameObject != robber.gameObject)
                {
                    robber.looker.Raycast(npc.transform, Mathf.Min((robber.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, robber.ec.MaxRaycast), LayerMask.GetMask("Default", "Block Raycast", "NPCs", "Windows"), out bool _sighted);
                    if ((_sighted | (robber.transform.position - npc.transform.position).magnitude <= 80f) && !sightedNPCs.Contains(npc.gameObject))
                        sightedNPCs.Add(npc.gameObject);
                    else if (!_sighted & (robber.transform.position - npc.transform.position).magnitude > 80f && sightedNPCs.Contains(npc.gameObject))
                        sightedNPCs.Remove(npc.gameObject);
                    break;
                }
            }

            if (sightedNPCs.Count > 0)
                robber.spriteRenderer[0].color = Color.clear;
            else
                robber.spriteRenderer[0].color = Color.white;
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (robber.spriteRenderer[0].color != Color.clear)
            {
                if (timeInSight < 2)
                    timeInSight += .1f * robber.ec.NpcTimeScale;
                else
                    robber.StartCoroutine(robber.GoNuts());
            }
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
        }
    }

    public class MrRobber_Crazy : MrRobber_StateBase
    {
        public MrRobber_Crazy(MrRobber npc)
            : base(npc)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            robber.Navigator.maxSpeed = 0f;
            robber.Navigator.SetSpeed(0f);
            robber.Navigator.SetRoomAvoidance(false);
        }

        public override void Update()
        {
            if (robber.Navigator.maxSpeed == 0f)
                return;
            robber.Navigator.maxSpeed = robber.Navigator.speed + 4f;
            robber.Navigator.SetSpeed(robber.Navigator.maxSpeed);
        }

        public override void PlayerInSight(PlayerManager player)
        {
            ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
        }

        public override void OnStateTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || robber.Navigator.maxSpeed == 0f)
                return;

            robber.DoneFor(other.GetComponent<PlayerManager>());
        }
    }

    public class MrRobber_DoNothing : MrRobber_StateBase
    {
        public MrRobber_DoNothing(MrRobber npc)
            : base(npc)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            robber.Navigator.maxSpeed = 15f;
            robber.Navigator.SetSpeed(15f);
            robber.Navigator.SetRoomAvoidance(true);
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
        }
    }
}
#endif