using BCarnellChars.Characters.States;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars.Characters
{
    public class MrRobber : NPC
    {
        [SerializeField] internal Canvas overlay;
        private MovementModifier playerMod = new MovementModifier(Vector3.zero, 0.1f);

        [SerializeField] internal AudioManager audMan, sfxMan;

        [SerializeField] internal List<SoundObject> voiceIntro = new List<SoundObject>(), voiceFlee = new List<SoundObject>();
        [SerializeField] internal SoundObject sfxBeat;
        [SerializeField] internal SoundObject[] sfxScreech;

        [SerializeField] internal Sprite wanderingSprite, huntingSprite;

        public override void Initialize()
        {
            this.ChangeRadarColor();
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
                        sfxMan.volumeModifier -= .1f * (Time.deltaTime * TimeScale);
                    else if (sfxMan.volumeModifier <= 0f)
                        sfxMan.volumeModifier = 0f;
                }
                else
                {
                    if (sfxMan.volumeModifier < 1f)
                        sfxMan.volumeModifier += .1f * (Time.deltaTime * TimeScale);
                    else if (sfxMan.volumeModifier >= 1f)
                        sfxMan.volumeModifier = 1f;
                }
                return;
            }
            else if (sfxMan.volumeModifier < 1f)
                sfxMan.volumeModifier = 1f;
            sfxMan.pitchModifier += .01f * (Time.deltaTime * TimeScale);
        }

        public IEnumerator GoNuts()
        {
            if (behaviorStateMachine.CurrentState.GetType() == typeof(MrRobber_Crazy))
                yield break;
            navigator.SetRoomAvoidance(false);
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
            overlay.worldCamera = CoreGameManager.Instance.GetCamera(player.playerNumber).canvasCam;

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
                audioJungle += .06f * (Time.deltaTime * TimeScale);
                AudioListener.volume = audioJungle;
                if (idkhow > 0f)
                {
                    idkhow -= .1f * (Time.deltaTime * TimeScale);
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
}

namespace BCarnellChars.Characters.States
{
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
            bool sighted = false;
            foreach (NPC npc in robber.ec.Npcs.Where(x => x.gameObject != robber.gameObject))
            {
                robber.looker.Raycast(npc.transform, float.PositiveInfinity, out var _sighted);
                if (_sighted || Vector3.Distance(robber.transform.position, npc.transform.position) <= 180f)
                    sighted = true;
            }
            if (npc.ec.timeOut && sighted)
                sighted = !(npc.ec.ContainsCoordinates(IntVector2.GetGridPosition(npc.transform.position)) && npc.ec.LightLevel(npc.transform.position) <= 0.1f);

            robber.spriteRenderer[0].color = sighted ? Color.clear : Color.white;
            if (robber.Navigator.Entity.Visible && sighted)
                robber.Navigator.Entity.SetVisible(false);
            else if (!robber.Navigator.Entity.Visible && !sighted)
                robber.Navigator.Entity.SetVisible(true);
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (robber.spriteRenderer[0].color != Color.clear)
            {
                if (timeInSight < 1)
                    timeInSight += .5f * (Time.deltaTime * robber.TimeScale);
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

        public override void OnStateTriggerEnter(Collider other, bool isValid)
        {
            if (!other.CompareTag("Player") || robber.Navigator.maxSpeed == 0f || !isValid)
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