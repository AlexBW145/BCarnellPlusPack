using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.Characters
{
    public class PAC75Ghost : Baldi
    {
        public new float Speed;
        private float increaseSpeed = 0.1f;
        public float NewSpeed => Speed + increaseSpeed;
        [SerializeField] internal AudioManager audman;
        public new AudioManager AudMan => audman;
        public Vector3 home { get; internal set; }

        public override float DistanceCheck(float val)
        {
            return val;
        }

        public static Sprite craftersSprite { private get; set; }

        void Awake()
        {
            loseSounds = Resources.FindObjectsOfTypeAll<Baldi>().Last().loseSounds;
            spriteRenderer[0].sprite = craftersSprite;
        }

        void Start()
        {
            if (ec != null && audman != null)
                BaseGameManager.Instance.Ec.map.AddArrow(navigator.Entity, (Color)audman.ReflectionGetVariable("subtitleColor"));
        }

        public override void Slap()
        {

        }

        public override void Initialize()
        {
            loseSounds = Resources.FindObjectsOfTypeAll<Baldi>().Last().loseSounds;
            navigator.Initialize(ec);
            home = transform.position;
            spriteBase.AddComponent<PickupBob>();
            navigator.maxSpeed = Speed;
            navigator.SetSpeed(Speed);
            PAC75Ghost_StateBase state = new PAC75Ghost_StateBase(this);
            switch (Character.ToStringExtended<Character>())
            {
                case "Blinky":
                    state = new PAC75Ghost_RedGuy(this);
                    break;
                case "Inky":
                    state = new PAC75Ghost_CyanIdiot(this);
                    break;
                case "Cylde":
                    state = new PAC75Ghost_OrangeCurious(this);
                    break;
                case "Pinky":
                    state = new PAC75Ghost_PinkGal(this);
                    break;
            }
            behaviorStateMachine.ChangeState(state);
        }

        public override void SetAnger(float value)
        {
            increaseSpeed = value;
            navigator.maxSpeed = Speed + increaseSpeed;
            navigator.SetSpeed(Speed + increaseSpeed);
        }

        public override void GetAngry(float value)
        {
            increaseSpeed += value;
            if (increaseSpeed <= 0.1f)
                increaseSpeed = 0.1f;
            navigator.maxSpeed = Speed + increaseSpeed;
            navigator.SetSpeed(Speed + increaseSpeed);
        }
    }
}

namespace BCarnellChars.Characters.States
{
    public class PAC75Ghost_StateBase : NpcState
    {
        protected PAC75Ghost ghost;
        public PAC75Ghost_StateBase(PAC75Ghost npc) : base(npc) { ghost = npc; }

        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
            if (!other.CompareTag("Player"))
                return;

            ghost.looker.Raycast(other.transform, Vector3.Magnitude(ghost.transform.position - other.transform.position), out var targetSighted);
            if (!targetSighted)
                return;

            PlayerManager component = other.GetComponent<PlayerManager>();
            if (!component.invincible && isValid)
                PacGameManager.PacInstance.EndGame(other.transform, ghost);
        }

        public override void Update()
        {
            if (PacGameManager.PacInstance.Powered)
                ghost.behaviorStateMachine.ChangeState(new PAC75Ghost_InDanger(ghost, this));
        }
    }

    public class PAC75Ghost_RedGuy : PAC75Ghost_StateBase
    {
        public PAC75Ghost_RedGuy(PAC75Ghost npc) : base(npc) { }

        public override void Enter()
        {
            base.Enter();
            ghost.AudMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75Ghost/Wandering"), true);
            ghost.AudMan.SetLoop(true);
            foreach (SpriteRenderer spriterend in ghost.spriteRenderer)
                spriterend.color = Color.red;
            ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }

        public override void DestinationEmpty()
        {
            ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }

        public override void PlayerSighted(PlayerManager player)
        {
            ChangeNavigationState(new NavigationState_TargetPlayer(ghost, 32, player.transform.position));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            currentNavigationState.UpdatePosition(player.transform.position);
        }
    }

    public class PAC75Ghost_CyanIdiot : PAC75Ghost_StateBase
    {
        public PAC75Ghost_CyanIdiot(PAC75Ghost npc) : base(npc) { }

        public override void Enter()
        {
            base.Enter();
            ghost.AudMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75Ghost/Wandering"), true);
            ghost.AudMan.SetLoop(true);
            foreach (SpriteRenderer spriterend in ghost.spriteRenderer)
                spriterend.color = Color.cyan;
            ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }

        public override void DestinationEmpty()
        {
            ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }

        public override void PlayerSighted(PlayerManager player)
        {
            float distance = Vector3.Distance(npc.transform.position, player.transform.position);
            if (distance >= 45 && distance < 400)
                ChangeNavigationState(new NavigationState_TargetPlayer(ghost, 32, player.transform.position));
            else
                ChangeNavigationState(new NavigationState_WanderFlee(ghost, 32, GameCamera.dijkstraMap));
        }
    }

    public class PAC75Ghost_OrangeCurious : PAC75Ghost_StateBase
    {
        public PAC75Ghost_OrangeCurious(PAC75Ghost npc) : base(npc) { }

        public override void Enter()
        {
            base.Enter();
            ghost.AudMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75Ghost/Wandering"), true);
            ghost.AudMan.SetLoop(true);
            foreach (SpriteRenderer spriterend in ghost.spriteRenderer)
                spriterend.color = new Color(1f, 0.5f, 0f);
            ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }

        public override void DestinationEmpty()
        {
            ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }
    }

    public class PAC75Ghost_PinkGal : PAC75Ghost_StateBase
    {
        public PAC75Ghost_PinkGal(PAC75Ghost npc) : base(npc) { }

        public override void Enter()
        {
            base.Enter();
            ghost.AudMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75Ghost/Wandering"), true);
            ghost.AudMan.SetLoop(true);
            foreach (SpriteRenderer spriterend in ghost.spriteRenderer)
                spriterend.color = new Color(1f, 0.5f, 1f);
            ChangeNavigationState(new NavigationState_TargetPlayer(ghost, 32, ghost.ec.Players[0].transform.position));
        }

        public override void DestinationEmpty()
        {
            int selectedPlayer = 0;
            float distance = Vector3.Distance(npc.transform.position, ghost.ec.Players[selectedPlayer].transform.position);
            if (distance < 155)
                ChangeNavigationState(new NavigationState_TargetPlayer(ghost, 32, ghost.ec.Players[selectedPlayer].transform.position));
            else
                ChangeNavigationState(new NavigationState_WanderRandom(ghost, 32));
        }

        public override void PlayerSighted(PlayerManager player)
        {
            ChangeNavigationState(new NavigationState_TargetPlayer(ghost, 32, player.transform.position));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            currentNavigationState.UpdatePosition(player.transform.position);
        }
    }

    public class PAC75Ghost_InDanger : PAC75Ghost_StateBase
    {
        protected PAC75Ghost_StateBase previousState;
        public PAC75Ghost_InDanger(PAC75Ghost npc, PAC75Ghost_StateBase previousState) : base(npc)
        {
            this.previousState = previousState;
        }

        public override void Enter()
        {
            base.Enter();
            ghost.AudMan.FlushQueue(true);
            ghost.AudMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75Ghost/Weakened"), true);
            ghost.AudMan.SetLoop(true);
            foreach (SpriteRenderer spriterend in ghost.spriteRenderer)
                spriterend.color = Color.blue;
            ChangeNavigationState(new NavigationState_WanderFlee(ghost, 32, GameCamera.dijkstraMap));
        }

        public override void DestinationEmpty()
        {
            ChangeNavigationState(new NavigationState_WanderFlee(ghost, 32, GameCamera.dijkstraMap));
        }

        public override void Update()
        {
            if (!PacGameManager.PacInstance.Powered)
                ghost.behaviorStateMachine.ChangeState(previousState);
        }

        public override void Exit()
        {
            ghost.AudMan.FlushQueue(true);
            ghost.Navigator.SetSpeed(ghost.NewSpeed);
        }

        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
            if (!other.CompareTag("Player"))
                return;

            ghost.looker.Raycast(other.transform, Vector3.Magnitude(ghost.transform.position - other.transform.position), out var targetSighted);
            if (!targetSighted || !isValid)
                return;

            CoreGameManager.Instance.audMan.PlaySingle(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75/WakaGhost"));
            ghost.behaviorStateMachine.ChangeState(new PAC75Ghost_Fleeing(ghost, previousState));
        }
    }

    public class PAC75Ghost_Fleeing : PAC75Ghost_StateBase
    {
        protected PAC75Ghost_StateBase previousState;
        public PAC75Ghost_Fleeing(PAC75Ghost npc, PAC75Ghost_StateBase previousState) : base(npc)
        {
            this.previousState = previousState;
        }

        public override void OnStateTriggerStay(Collider other, bool isValid)
        {

        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_TargetPosition(ghost, 32, ghost.home));
            ghost.Navigator.SetSpeed(ghost.Speed + 99f);
            ghost.AudMan.FlushQueue(true);
            ghost.AudMan.QueueAudio(BCPPPlugin.bcppAssets.Get<SoundObject>("PAC75Ghost/Fleeing"), true);
            ghost.AudMan.SetLoop(true);
            foreach (SpriteRenderer spriterend in ghost.spriteRenderer)
                spriterend.color = Color.clear;
        }

        public override void Exit()
        {
            ghost.Navigator.SetSpeed(ghost.NewSpeed);
        }

        public override void Update()
        {
        }

        public override void DestinationEmpty()
        {
            ghost.AudMan.FlushQueue(true);
            if (!PacGameManager.PacInstance.Powered)
                ghost.behaviorStateMachine.ChangeState(previousState);
            else
                ghost.behaviorStateMachine.ChangeState(new PAC75Ghost_InDanger(ghost, previousState));
        }
    }
}
