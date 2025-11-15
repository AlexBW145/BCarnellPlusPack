using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Characters
{
    public class BCarnellBoss : Baldi
    {
        [SerializeField] internal AudioManager audMan;
        [SerializeField] internal GameObject sawbladePre;

        [SerializeField] internal SoundObject stepSnd, boostCharge, boostGo, sawShoot;

        private IEnumerator currentAttack;
        private float attackCooldown = 0f;
        public bool AbleToAttack => attackCooldown <= 0f;
        public bool Attacking { get; private set; }

        [SerializeField]
        internal AnimationCurve speedCurve = new AnimationCurve(), slapCurve = new AnimationCurve();

        public new float Delay => slapCurve.Evaluate(anger + extraAnger);
        public new float Speed => speedCurve.Evaluate(anger) + baseSpeed + extraAnger;
        private bool isThrusting = false;
        internal enum B2Boss
        {
            Phase0,
            Phase1,
            Phase2,
            Phase3,
            Phase4,
            Phase5,
        }
        [SerializeField] internal B2Boss phases { get; private set; } = B2Boss.Phase0;
        [SerializeField] internal SoundObject[] dialIntro;

        private static FieldInfo
            _smoothMove = AccessTools.DeclaredField(typeof(Baldi), "smoothMove"),
            _speedCurve = AccessTools.DeclaredField(typeof(Baldi), "speedCurve"),
            _slapCurve = AccessTools.DeclaredField(typeof(Baldi), "slapCurve");
        [SerializeField, HideInInspector] private Baldi baldiBase = Resources.FindObjectsOfTypeAll<Baldi>().First(x => x.name.Contains("3"));

        public override void CaughtPlayer(PlayerManager player)
        {
            CoreGameManager.Instance.audMan.PlaySingle(BasePlugin.bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/caughtlayer"));
            base.CaughtPlayer(player);
        }

        internal void InitPhase(B2Boss phase)
        {
            phases = phase;
            _smoothMove.SetValue(this, phase != B2Boss.Phase0);
            switch (phase)
            {
                case B2Boss.Phase0:
                    break;
                default:
                    behaviorStateMachine.ChangeState(new BCarnellBoss_ChaseIntense(this, this));
                    break;
            }
        }

        internal void TheDialogue(PlayerManager pm)
        {
            behaviorStateMachine.ChangeState(new BCarnellBoss_Speaking(this, this));
            audMan.FlushQueue(true);
            if (CoreGameManager.Instance.lifeMode == LifeMode.Intense)
                audMan.QueueAudio(dialIntro[4], true);
            else if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.playablecharacters") && ProtagonistStuff.IsMainProtagonist(pm))
                audMan.QueueAudio(dialIntro[5], true);
            else if (PlayerFileManager.Instance.clearedLevels[1] == false && !BasePlugin.Instance.SaveData.death)
                audMan.QueueAudio(dialIntro[1], true);
            else if (CoreGameManager.Instance.Lives <= 0)
                audMan.QueueAudio(dialIntro[3], true);
            else if (ec.SurpassedRealTime < 50f && CoreGameManager.Instance.Lives == 2)
                audMan.QueueAudio(dialIntro[2], true);
            else
                audMan.QueueAudio(dialIntro[0], true);
        }

        internal void ButtonPressed(PlayerManager pm)
        {
            audMan.FlushQueue(true);
            /*if (CoreGameManager.Instance.lifeMode == LifeMode.Intense)
                audMan.QueueAudio(dialIntro[4], true);
            else if (ProtagonistStuff.IsMainProtagonist(pm))
                audMan.QueueAudio(dialIntro[5], true);
            else if (PlayerFileManager.Instance.clearedLevels[0] == false)
                audMan.QueueAudio(dialIntro[1], true);
            else if (CoreGameManager.Instance.Lives <= 0)
                audMan.QueueAudio(dialIntro[3], true);
            else if (ec.SurpassedRealTime < 90f)
                audMan.QueueAudio(dialIntro[2], true);
            else
                audMan.QueueAudio(dialIntro[0], true);*/
        }

        public override void Initialize()
        {
            navigator.Initialize(ec);
            {
                baseAnger = 5.05f;
                baseSpeed = 55f;
                speedMultiplier = 1.85f;
                slapSpeedScale = 15.65f;
                speedCurve = (AnimationCurve)_speedCurve.GetValue(baldiBase);
                slapCurve = (AnimationCurve)_slapCurve.GetValue(baldiBase);
            }
            anger = 5f;
            extraAnger = 2f;
            behaviorStateMachine.ChangeState(new BCarnellBoss_Chase(this, this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 0));
        }

        protected override void VirtualUpdate()
        {
            if (attackCooldown > 0f)
                attackCooldown -= TimeScale * Time.deltaTime;
        }

        public override void Slap()
        {
            /*OnBaldiSlap += (baldi) => {
                if (baldi.GetType().Equals(this))
                    behaviorStateMachine.ChangeState(new BCarnellBoss_Chase(baldi, baldi.GetComponent<BCarnellBoss>()));
            };*/
            slapTotal = 0f;
            slapDistance = nextSlapDistance;
            nextSlapDistance = 0f;
            navigator.SetSpeed(slapDistance / (Delay * MovementPortion));
        }

        public new void SlapNormal()
        {
            audMan.PlaySingle(stepSnd);
            SlapRumble();
        }

        public void Attack()
        {
            var players = ec.Players;
            int attacknum = Mathf.RoundToInt(UnityEngine.Random.Range(0f, 3f));
            switch (phases)
            {
                case B2Boss.Phase0:
                    anger += .5f;
                    if (Vector3.Distance(transform.position, players[0].transform.position) > 390f)
                    {
                        currentAttack = ShootingSaws();
                        break;
                    }
                    else
                        currentAttack = ChargeGo();
                    break;
                case B2Boss.Phase1:
                    looker.Raycast(players[0].transform, Mathf.Min((transform.position - players[0].transform.position).magnitude, looker.distance), out bool playerCaught);
                    if (Vector3.Distance(transform.position, players[0].transform.position) >= 65f && Vector3.Distance(transform.position, players[0].transform.position) < 160f)
                        currentAttack = ShootingSaws(0.3f, 1f);
                    else if (!playerCaught)
                        currentAttack = ChargeGo(0.25f, 4f, false);
                    else
                        return;
                    break;
            }
            Attacking = true;
            StartCoroutine(currentAttack);
        }

        public override float DistanceCheck(float val)
        {
            if (isThrusting)
                return val;
            return base.DistanceCheck(val);
        }

        private IEnumerator ShootingSaws(float actualTimeWillWaste = 5f, float cooldown = 7f)
        {
            var players = ec.Players;
            float time = 0.2f;
            while (actualTimeWillWaste > 0f)
            {
                behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 1));
                actualTimeWillWaste -= TimeScale * Time.deltaTime;
                while (time > 0f)
                {
                    actualTimeWillWaste -= TimeScale * Time.deltaTime;
                    time -= TimeScale * Time.deltaTime;
                    yield return null;
                }
                transform.rotation = Quaternion.LookRotation(players[0].transform.position - transform.position, Vector3.forward);
                var saw = Instantiate(sawbladePre, transform.position + new Vector3(0f, 2f, 0f), transform.rotation).GetComponent<Sawblades>();
                saw.ec = ec;
                saw.GetComponent<Entity>().Initialize(ec, saw.transform.position);
                audMan.PlaySingle(sawShoot);
                time = 0.08f;
                yield return null;
            }
            attackCooldown = cooldown;
            Attacking = false;
            UpdateSoundTarget();
            yield break;
        }

        private IEnumerator ChargeGo(float actualTimeWillWaste = 1.6f, float cooldown = 10f, bool targetPlayer = true)
        {
            var players = ec.Players;
            audMan.pitchModifier = 1f + (1.6f - actualTimeWillWaste);
            audMan.QueueAudio(boostCharge, true);
            while (actualTimeWillWaste > 0f)
            {
                behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 1));
                actualTimeWillWaste -= TimeScale * Time.deltaTime;
                yield return null;
            }
            isThrusting = true;
            audMan.FlushQueue(true);
            audMan.pitchModifier = 1f;
            audMan.PlaySingle(boostGo);
            if (targetPlayer)
            {
                ClearSoundLocations();
                Hear(null, players[0].transform.position, 127, false);
            }
            else
            {
                navigator.WanderRandom();
                Hear(null, navigator.CurrentDestination, 126, false);
            }
            var thrustspeed = 1f;
            while (navigator.HasDestination)
            {
                slapTotal = 0f;
                slapDistance = nextSlapDistance;
                nextSlapDistance = 0f;
                thrustspeed += 5f * (TimeScale * Time.deltaTime);
                navigator.SetSpeed(Speed * MovementPortion * thrustspeed);
                yield return null;
            }
            isThrusting = false;
            navigator.SetSpeed(0f);
            attackCooldown = cooldown;
            Attacking = false;
            UpdateSoundTarget();
            yield break;
        }
    }
}

namespace BCarnellChars.Characters.States
{
    public class BCarnellBoss_StateBase : NpcState
    {
        protected BCarnellBoss bcarnell;

        public BCarnellBoss_StateBase(NPC npc, BCarnellBoss carnell)
            : base(npc)
        {
            bcarnell = carnell;
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
            bcarnell.Hear(source, position, value, true);
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (!IsActuallyHidden(player))
            {
                bcarnell.ClearSoundLocations();
                bcarnell.Hear(null, player.transform.position, 127, false);
            }
        }

        public override void DestinationEmpty()
        {
            if (bcarnell.CurrentDestinationInteraction != null && bcarnell.CurrentDestinationInteraction.Check(bcarnell))
            {
                bcarnell.CurrentDestinationInteraction.Trigger(bcarnell);
                bcarnell.ClearDestinationInteraction();
            }
            if (!bcarnell.Attacking)
                bcarnell.UpdateSoundTarget();
        }

        protected static FieldInfo
            _hiddenByLight = AccessTools.DeclaredField(typeof(PlayerManager), "hiddenByLight"),
            _hides = AccessTools.DeclaredField(typeof(Entity), "hides");
        protected bool IsActuallyHidden(PlayerManager player)
        {
            if ((bool)_hiddenByLight.GetValue(player) && (int)_hides.GetValue(player.plm.Entity) > 2)
                return true;
            return !(bool)_hiddenByLight.GetValue(player) && player.Invisible;
        }

        protected virtual void ActivateSlapAnimation()
        {
            bcarnell.SlapNormal();
        }

        public override void DoorHit(StandardDoor door)
        {
            if (door.locked)
            {
                door.Unlock();
                door.OpenTimed(5f, makeNoise: false);
            }
            else
            {
                base.DoorHit(door);
            }
        }
    }

    public class BCarnellBoss_Speaking : BCarnellBoss_StateBase
    {
        public BCarnellBoss_Speaking(NPC npc, BCarnellBoss carnell) : base(npc, carnell) { }

        public override void Enter()
        {
            base.Enter();
            bcarnell.Navigator.Entity.SetTrigger(false);
            bcarnell.Navigator.Entity.SetFrozen(true);
        }

        public override void Exit()
        {
            base.Exit();
            bcarnell.Navigator.Entity.SetTrigger(true);
            bcarnell.Navigator.Entity.SetFrozen(false);
        }

        public override void PlayerInSight(PlayerManager player)
        {
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }

        public override void DestinationEmpty()
        {
        }
    }

    public class BCarnellBoss_Chase : BCarnellBoss_StateBase
    {
        protected float delayTimer;

        public BCarnellBoss_Chase(NPC npc, BCarnellBoss carnell)
            : base(npc, carnell)
        {
        }

        public override void Enter()
        {
            base.Enter();
            delayTimer = bcarnell.Delay;
            bcarnell.ResetSlapDistance();
        }

        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
            base.OnStateTriggerStay(other, isValid);
            if (!other.CompareTag("Player"))
                return;

            bcarnell.looker.Raycast(other.transform, Vector3.Magnitude(bcarnell.transform.position - other.transform.position), out var targetSighted);
            if (!targetSighted)
                return;

            PlayerManager component = other.GetComponent<PlayerManager>();
            ItemManager itm = component.itm;
            if (!component.invincible && isValid)
                bcarnell.CaughtPlayer(component);
        }

        public override void Update()
        {
            bcarnell.UpdateSlapDistance();
            if (!bcarnell.Attacking)
            {
                delayTimer -= Time.deltaTime * npc.TimeScale;
                if (delayTimer <= 0f)
                {
                    bcarnell.Slap();
                    ActivateSlapAnimation();
                    delayTimer = bcarnell.Delay;
                }
            }
        }

        public override void PlayerSighted(PlayerManager player)
        {
            if (!IsActuallyHidden(player) && !bcarnell.Attacking && bcarnell.AbleToAttack)
                bcarnell.Attack();
        }
    }

    public class BCarnellBoss_ChaseIntense : BCarnellBoss_Chase
    {
        public BCarnellBoss_ChaseIntense(NPC npc, BCarnellBoss bcarnell) : base(npc, bcarnell) { }

        public override void PlayerSighted(PlayerManager player)
        {
            if (!bcarnell.Attacking && bcarnell.AbleToAttack && !IsActuallyHidden(player))
                bcarnell.Attack();
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (!bcarnell.Attacking && !IsActuallyHidden(player))
                base.PlayerInSight(player);
        }

        public override void DestinationEmpty()
        {
            var players = bcarnell.ec.Players;
            if (Vector3.Distance(bcarnell.transform.position, players[0].transform.position) >= 65 && bcarnell.AbleToAttack)
                bcarnell.Attack();
            else
                base.DestinationEmpty();
        }

        protected override void ActivateSlapAnimation()
        {
        }
    }
}