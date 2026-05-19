using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Characters
{
    public class BCarnellBoss : Baldi
    {
        internal AudioManager audMan
        {
            get => AudMan;
            set => _audMan.SetValue(this, value);
        }
        [SerializeField] internal GameObject sawbladePre;

        [SerializeField] internal SoundObject stepSnd, boostCharge, boostGo, sawShoot;

        private IEnumerator currentAttack;
        private float attackCooldown = 0f;
        public bool AbleToAttack => attackCooldown <= 0f;
        public bool Attacking { get; private set; }

        internal AnimationCurve speedCurve
        {
            get => (AnimationCurve)_speedCurve.GetValue(this);
            set => _speedCurve.SetValue(this, value);
        }
        internal AnimationCurve slapCurve
        {
            get => (AnimationCurve)_slapCurve.GetValue(this);
            set => _slapCurve.SetValue(this, value);
        }

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

        private static readonly FieldInfo
            _audMan = AccessTools.DeclaredField(typeof(Baldi), "audMan"),
            _smoothMove = AccessTools.DeclaredField(typeof(Baldi), "smoothMove"),
            _speedCurve = AccessTools.DeclaredField(typeof(Baldi), "speedCurve"),
            _slapCurve = AccessTools.DeclaredField(typeof(Baldi), "slapCurve");
        [SerializeField, HideInInspector] private Baldi baldiBase = Resources.FindObjectsOfTypeAll<Baldi>().First(x => x.name.Contains("3"));
        [SerializeField] internal CustomSpriteRendererAnimator animator;

        public override void CaughtPlayer(PlayerManager player)
        {
            base.CaughtPlayer(player);
            CoreGameManager.Instance.audMan.PlaySingle(BCPPPlugin.bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/caughtlayer"));
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
                    behaviorStateMachine.ChangeState(new BCarnellBoss_ChaseIntense(this));
                    break;
            }
        }

        internal void TheDialogue(PlayerManager pm)
        {
            behaviorStateMachine.ChangeState(new BCarnellBoss_Speaking(this));
            audMan.FlushQueue(true);
            if (CoreGameManager.Instance.lifeMode == LifeMode.Intense)
                audMan.QueueAudio(dialIntro[4], true);
            else if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.playablecharacters") && ProtagonistStuff.IsMainProtagonist(pm))
                audMan.QueueAudio(dialIntro[5], true);
            else if (PlayerFileManager.Instance.clearedLevels[1] == false && !BCPPPlugin.Instance.SaveData.death)
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
            navigator.Entity.SetHeight(6.5f);
            animator.ec = ec;
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
            behaviorStateMachine.ChangeState(new BCarnellBoss_Chase(this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 0));
            animator.SetDefaultAnimation("SlapIdle", 1f, true);
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
            animator.Play("Slap", 1.5f);
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

        internal void SpawnSaw()
        {
            var saw = Instantiate(sawbladePre, transform.position + new Vector3(0f, 2f, 0f), transform.rotation).GetComponent<Sawblades>();
            saw.Initialize(this);
            audMan.PlaySingle(sawShoot);
            animator.Play("Shoot", 1f);
        }

        private IEnumerator ShootingSaws(float actualTimeWillWaste = 5f, float cooldown = 7f)
        {
            var players = ec.Players;
            float time = 0.2f;
            while (actualTimeWillWaste > 0f)
            {
                behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 127));
                actualTimeWillWaste -= TimeScale * Time.deltaTime;
                while (time > 0f)
                {
                    actualTimeWillWaste -= TimeScale * Time.deltaTime;
                    time -= TimeScale * Time.deltaTime;
                    yield return null;
                }
                transform.rotation = Quaternion.LookRotation(players[0].transform.position - transform.position, Vector3.forward);
                SpawnSaw();
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
            float initial = actualTimeWillWaste;
            audMan.pitchModifier = 1f + (1.6f - actualTimeWillWaste);
            audMan.QueueAudio(boostCharge, true);
            animator.Play("BoostPre", 1f, true);
            var speed = 1f;
            while (actualTimeWillWaste > 0f)
            {
                behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 127));
                actualTimeWillWaste -= TimeScale * Time.deltaTime;
                speed = 1f + (-actualTimeWillWaste + initial);
                animator.ChangeSpeed(speed);
                yield return null;
            }
            isThrusting = true;
            audMan.FlushQueue(true);
            audMan.pitchModifier = 1f;
            audMan.PlaySingle(boostGo);
            animator.Play("Boosting", 1f, true);
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
            animator.Play("BoostOut", 1f, false);
            yield break;
        }
    }
}