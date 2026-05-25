using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using BepInEx.Bootstrap;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] internal PropagatedAudioManager battleAud;
        [SerializeField] internal Sawblades sawbladePre;

        [SerializeField] internal SoundObject stepSnd, boostCharge, boostGo, sawShoot, postbattle, death, inbattle;

        private Queue<IEnumerator> enqueuedAttacks = new Queue<IEnumerator>();
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
        internal const int initHP = 15;
        [Range(0, int.MaxValue)] private int hp;
        internal int HP { get => hp;
            set {
                if (phases == B2Boss.Phase0 || phases >= B2Boss.Phase5) return;
                hp = value;
                SetAnger(baseAnger + ((float)(initHP - hp) / 10f));
                MusicManager.Instance.MidiPlayer.MPTK_Speed = 1f + ((0.2500000001f * (float)(phases - 1)) + ((float)(initHP - hp) / 60f));
                if (phases == B2Boss.Phase4 && hp == 1) // Plus does not have yet ported the hang function from BBCR.
                    MidiEvent(MusicManager.Instance.MidiPlayer);
                else if (hp <= 0)
                {
                    Entity.SetFrozen(true);
                    Entity.SetTrigger(false);
                    CarnellBossManager.Instance.CallSpecialManagerFunction(2, null);
                }
            }
        }

        private void MidiEvent(MidiSynth player) => MidiHang(player, hp == 1, true);

        internal static void MidiHang(MidiSynth player, bool stop, bool keepDrums)
        {
            for (int i = 0; i < 16; i++)
            {
                if (i != 9)
                    player.MPTK_ChannelEnableSet(i, !stop);
                else
                    player.MPTK_ChannelEnableSet(i, !stop || keepDrums);
            }
            if (_ActiveVoices.GetValue(player) != null)
            {
                foreach (fluid_voice fluid_voice2 in (List<fluid_voice>)_ActiveVoices.GetValue(player))
                    fluid_voice2.DurationTick = stop ? -1L : 0L;
            }
        }

        private void OnDestroy() => MidiHang(MusicManager.Instance.MidiPlayer, false, true);

        private static readonly FieldInfo
            _audMan = AccessTools.DeclaredField(typeof(Baldi), "audMan"),
            _smoothMove = AccessTools.DeclaredField(typeof(Baldi), "smoothMove"),
            _speedCurve = AccessTools.DeclaredField(typeof(Baldi), "speedCurve"),
            _slapCurve = AccessTools.DeclaredField(typeof(Baldi), "slapCurve"),
            _ActiveVoices = AccessTools.DeclaredField(typeof(MidiSynth), "ActiveVoices");
        [SerializeField, HideInInspector] private Baldi baldiBase = Resources.FindObjectsOfTypeAll<Baldi>().First(x => x.name.Contains("3") && x.GetInstanceID() >= 0);
        [SerializeField] internal CustomSpriteRendererAnimator animator;
        private Vector3 spawnPos;

        private void Start() => spawnPos = transform.position;

        public override void CaughtPlayer(PlayerManager player)
        {
            base.CaughtPlayer(player);
            CoreGameManager.Instance.audMan.PlaySingle(BCPPPlugin.bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/caughtlayer"));
            MidiHang(MusicManager.Instance.MidiPlayer, false, true);
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
                    baseAnger = 5f + (.5f * (float)phase);
                    SetAnger(baseAnger);
                    baseSpeed = 25f;
                    hp = initHP;
                    behaviorStateMachine.ChangeState(new BCarnellBoss_ChaseIntense(this));
                    battleAud.QueueAudio(inbattle, true);
                    battleAud.SetLoop(true);
                    break;
                case B2Boss.Phase5:
                    isThrusting = true;
                    behaviorStateMachine.ChangeState(new BCarnellBoss_GoingTo(this, ec.CellFromPosition(6, 38).CenterWorldPosition, new BCarnellBoss_PostBattle(this)));
                    navigator.SetSpeed(99f);
                    navigator.maxSpeed = 99f;
                    break;
            }
        }

        internal void IAmComing()
        {
            _smoothMove.SetValue(this, true);
            Entity.Release();
            Cell position = ec.CellFromPosition(spawnPos);
            int distance = 16;
            ec.FindPath(ec.CellFromPosition(transform.position), position, PathType.Nav, out var path, out var success);
            path.Reverse();
            if (success && path.Count > distance)
                Entity.Teleport(path[distance].CenterWorldPosition);
            isThrusting = true;
            behaviorStateMachine.ChangeState(new BCarnellBoss_PreBattle(this, spawnPos));
            navigator.SetSpeed(99f);
            navigator.maxSpeed = 99f;
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
            else if (ec.SurpassedGameTime < 50f && CoreGameManager.Instance.Lives == 2)
                audMan.QueueAudio(dialIntro[2], true);
            else
                audMan.QueueAudio(dialIntro[0], true);
        }

        internal void ButtonPressed()
        {
            audMan.FlushQueue(true);
        }

        public override void Initialize()
        {
            navigator.Initialize(ec);
            Entity.SetHeight(6.5f);
            animator.ec = ec;

            baseAnger = 0f;
            /*baseAnger = 5.05f;
            baseSpeed = 55f;
            speedMultiplier = 1.85f;
            slapSpeedScale = 15.65f;*/
            baseSpeed = 15f;
            speedCurve = (AnimationCurve)_speedCurve.GetValue(baldiBase);
            slapCurve = (AnimationCurve)_slapCurve.GetValue(baldiBase);

            SetAnger(5f);
            extraAnger = 4f; //initial ex anger = 2f
            behaviorStateMachine.ChangeState(new BCarnellBoss_Chase(this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 0));
            animator.SetDefaultAnimation("SlapIdle", 1f, true);
        }

        protected override void VirtualUpdate()
        {
            if (attackCooldown > 0f)
                attackCooldown -= TimeScale * Time.deltaTime;
            if (!Attacking && enqueuedAttacks.Count > 0)
            {
                Attacking = true;
                var attack = enqueuedAttacks.Dequeue();
                StartCoroutine(attack);
            }
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
            var dist = Vector3.Distance(transform.position, players[0].transform.position);
            looker.Raycast(players[0].transform, Mathf.Min((transform.position - players[0].transform.position).magnitude, looker.distance), out bool playerCaught);
            switch (phases)
            {
                case B2Boss.Phase0:
                    if (dist > 290f)
                        enqueuedAttacks.Enqueue(ShootingSaws());
                    else if (dist >= 120f)
                        enqueuedAttacks.Enqueue(ChargeGo());
                    else
                    {
                        attackCooldown = 0.5f;
                        return;
                    }
                    GetAngry(.5f);
                    break;
                case B2Boss.Phase1:
                    if (dist >= 125f && dist < 525f)
                        enqueuedAttacks.Enqueue(ShootingSaws(0.3f, 1f));
                    else if (!playerCaught)
                        enqueuedAttacks.Enqueue(ChargeGo(0.25f, 4f, false));
                    else
                        attackCooldown = 0.15f;
                    break;
                case B2Boss.Phase2:
                    if (!playerCaught && dist >= 200f)
                    {
                        enqueuedAttacks.Enqueue(ChargeGo(0.55f, 2f, false));
                        enqueuedAttacks.Enqueue(ShootingSaws(0.48f, 2f));
                    }
                    else if (!playerCaught)
                    {
                        for (var i = 0; i < 2; i++)
                        {
                            enqueuedAttacks.Enqueue(ShootingSaws(0.2f, 0.5f));
                            enqueuedAttacks.Enqueue(DelayAttack(0.28f));
                        }
                    }
                    else
                        enqueuedAttacks.Enqueue(ShootingSaws(10f));
                    break;
                case B2Boss.Phase3:
                    if (dist >= 130f && !playerCaught)
                    {
                        for (var i = 0; i < 12; i++)
                        {
                            transform.Rotate((Vector3.up * i) * 30f);
                            SpawnSaw();
                        }
                        attackCooldown = 1f;
                    }
                    else if (!playerCaught)
                    {
                        var tiles = ec.mainHall.AllTilesNoGarbage(false, false);
                        var cell = tiles[UnityEngine.Random.Range(0, tiles.Count)];
                        behaviorStateMachine.CurrentNavigationState.UpdatePosition(cell.CenterWorldPosition);
                        enqueuedAttacks.Enqueue(ChargeGo(4f, 9f, false));
                    }
                    else if (playerCaught && dist <= 105f)
                    {
                        for (var i = -1; i <= 1; i++)
                        {
                            transform.Rotate((Vector3.up * (i == 0 ? 1 : i)) * 20f);
                            SpawnSaw();
                        }
                        enqueuedAttacks.Enqueue(DelayAttack(0.3f));
                        enqueuedAttacks.Enqueue(ShootingSaws(0.48f, 1f, 0.04f));
                    }
                    else if (dist > 105f)
                        attackCooldown = 2f;
                    break;
                case B2Boss.Phase4:
                    if (!playerCaught && dist >= 430f)
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            enqueuedAttacks.Enqueue(ShootingSaws(0.2f, initialtimer: 0.5f));
                            enqueuedAttacks.Enqueue(DelayAttack(0.5f, true));
                        }
                    }
                    else if (playerCaught && dist >= 125f)
                    {
                        enqueuedAttacks.Enqueue(ChargeGo(0.5f, 4f, true));
                        enqueuedAttacks.Enqueue(DelayAttack(1f));
                        enqueuedAttacks.Enqueue(ShootingSaws(0.3f, 4f));
                    }
                    else
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            enqueuedAttacks.Enqueue(ShootingSaws(0.2f, initialtimer: 2f, cooldown: 4f));
                            enqueuedAttacks.Enqueue(DelayAttack(1f, true));
                        }
                    }
                    break;
            }
        }

        public override float DistanceCheck(float val)
        {
            if (isThrusting)
                return val;
            return base.DistanceCheck(val);
        }

        internal void SpawnSaw()
        {
            var saw = Instantiate(sawbladePre, transform.position + new Vector3(0f, 2f, 0f), default);
            saw.transform.forward = transform.forward;
            saw.Initialize(this);
            audMan.PlaySingle(sawShoot);
            animator.Play("Shoot", 1f);
        }

        private IEnumerator ShootingSaws(float actualTimeWillWaste = 5f, float cooldown = 7f, float initialtimer = 0.08f)
        {
            var players = ec.Players;
            float time = 0.2f;
            var state = new NavigationState_DoNothing(this, 127);
            while (actualTimeWillWaste > 0f)
            {
                behaviorStateMachine.ChangeNavigationState(state);
                actualTimeWillWaste -= TimeScale * Time.deltaTime;
                while (time > 0f)
                {
                    actualTimeWillWaste -= TimeScale * Time.deltaTime;
                    time -= TimeScale * Time.deltaTime;
                    yield return null;
                }
                transform.rotation = Quaternion.LookRotation(players[0].transform.position - transform.position, Vector3.forward);
                SpawnSaw();
                time = initialtimer;
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
            var state = new NavigationState_DoNothing(this, 127);
            while (actualTimeWillWaste > 0f)
            {
                behaviorStateMachine.ChangeNavigationState(state);
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

        private IEnumerator DelayAttack(float actualTimeWillWaste, bool standstill = false)
        {
            var state = new NavigationState_DoNothing(this, 127);
            while (actualTimeWillWaste > 0f)
            {
                if (standstill)
                    behaviorStateMachine.ChangeNavigationState(state);
                actualTimeWillWaste -= TimeScale * Time.deltaTime;
                yield return null;
            }
            Attacking = false;
            yield break;
        }
    }
}