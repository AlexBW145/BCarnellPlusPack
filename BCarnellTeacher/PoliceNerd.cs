using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace BCarnellTeacher
{
    public class PoliceNerd : Principal
    {
        private static FieldInfo
            _allKnowing = AccessTools.DeclaredField(typeof(Principal), "allKnowing"),
            _audMan = AccessTools.DeclaredField(typeof(Principal), "audMan"),
            _targetedPlayer = AccessTools.DeclaredField(typeof(Principal), "targetedPlayer"),
            _timeInSight = AccessTools.DeclaredField(typeof(Principal), "timeInSight"),
            _detentionNoise = AccessTools.DeclaredField(typeof(Principal), "detentionNoise"),
            _whistleRng = AccessTools.DeclaredField(typeof(Principal), "whistleRng"),
            _whistleChance = AccessTools.DeclaredField(typeof(Principal), "whistleChance"),
            _detentionLevel = AccessTools.DeclaredField(typeof(Principal), "detentionLevel");

        private List<PlayerManager> players => ec.Players.Where(player => player != null).ToList();
        internal AudioManager audMan { get => (AudioManager)_audMan.GetValue(this); set => _audMan.SetValue(this, value); }
        private PlayerManager targetedPlayer { get => (PlayerManager)_targetedPlayer.GetValue(this); set => _targetedPlayer.SetValue(this, value); }
        private float[] timeInSight { get => (float[])_timeInSight.GetValue(this); set => _timeInSight.SetValue(this, value); }
        private int detentionNoise { get => (int)_detentionNoise.GetValue(this); set => _detentionNoise.SetValue(this, value); }
        private System.Random whistleRng { get => (System.Random)_whistleRng.GetValue(this); set => _whistleRng.SetValue(this, value); }
        private float whistleChance { get => (float)_whistleChance.GetValue(this); set => _whistleChance.SetValue(this, value); }
        private int detentionLevel { get => (int)_detentionLevel.GetValue(this); set => _detentionLevel.SetValue(this, value); }

        [SerializeField] internal Canvas overlay;

        [SerializeField]
        internal SoundObject 
            bang = Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(b => b.name == "Bang"),
            whistle;

        [SerializeField]
        private float
            defaultSpeed = 26f,
            carSpeed = 90f;

        private bool inCar = false;
        public bool PoliceCarMode => inCar;

        private RoomController[] officeThatPlayersIsIn = new RoomController[0];
        public Vector3 targetedOffice { get
            {
                HashSet<RoomController> rooms = new HashSet<RoomController>();
                officeThatPlayersIsIn.DoIf(x => x != null, x => rooms.Add(x));
                if (rooms.Count > 0)
                {
                    DetentionRoomFunction target = null;
                    foreach (var room in rooms)
                    {
                        var controller = room.functionObject.GetComponent<DetentionRoomFunction>();
                        if (controller == null) continue;
                        if (target == null || (float)detentionTime.GetValue(controller) > (float)detentionTime.GetValue(target))
                            target = controller;
                    }
                    return ec.RealRoomMid(target.Room);
                }
                return transform.position;
            } }
        private string[] savedRulebreak = new string[0];
        private FieldInfo activeoffice = AccessTools.DeclaredField(typeof(DetentionRoomFunction), "active"), detentionTime = AccessTools.DeclaredField(typeof(DetentionRoomFunction), "time");

        private bool allKnowing { get => (bool)_allKnowing.GetValue(this); set => _allKnowing.SetValue(this, value); }
        public bool AllKnowing => allKnowing;

        public override void Initialize()
        {
            base.Initialize();
            savedRulebreak = new string[CoreGameManager.Instance.TotalPlayers];
            officeThatPlayersIsIn = new RoomController[CoreGameManager.Instance.TotalPlayers];
            behaviorStateMachine.ChangeState(new PoliceNerd_Wandering(this));
        }

        public new void ObservePlayer(PlayerManager player)
        {
            if (!player.Disobeying || player.Tagged)
                return;

            timeInSight[player.playerNumber] += Time.deltaTime * TimeScale;
            if (timeInSight[player.playerNumber] >= player.GuiltySensitivity)
            {
                behaviorStateMachine.ChangeState(new PoliceNerd_ChasingPlayer(this, player));

                targetedPlayer = player;
                Scold(player.ruleBreak);
            }
        }

        public new void LoseTrackOfPlayer(PlayerManager player)
        {
            timeInSight[player.playerNumber] = 0f;
        }

        protected override void VirtualUpdate()
        {
            for (int i = 0; i < officeThatPlayersIsIn.Length; i++)
            {
                if (officeThatPlayersIsIn[i] != null && !(bool)activeoffice.GetValue(officeThatPlayersIsIn[i].functionObject.GetComponent<DetentionRoomFunction>()))
                    officeThatPlayersIsIn[i] = null;
            }
        }

        public new void SendToDetention(bool validCollision) // Just makin' sure...
        {
            if (ec.offices.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, ec.offices.Count);
                SendToDetention(ec.offices[index]);
            }
        }

        public void SendToDetention(RoomController office)
        {
            CarTransform(false);
            targetedPlayer.ClearGuilt();
            targetedPlayer.plm.Entity.Teleport(ec.RealRoomMid(office));
            float time; //= detentionInit + detentionInc * (float)detentionLevel;
            /*if (detentionLevel >= audTimes.Length - 1)
            {
                time = 99f;
            }*/
            detentionLevel++;
            if (detentionLevel > 14)
                detentionLevel = 14;
            switch (savedRulebreak[targetedPlayer.playerNumber].ToLower())
            {
                default:
                    time = UnityEngine.Random.Range(15, 30);
                    break;
                case "running" or "lockers" or "littering":
                    time = 15f;
                    break;
                case "faculty" or "drinking" or "breakingproperty" or "stabbing":
                    time = 15f * detentionLevel;
                    break;
                case "escaping" or "fraud":
                    time = 20f * detentionLevel;
                    break;
            }
            savedRulebreak[targetedPlayer.playerNumber] = null;
            //base.transform.position = targetedPlayer.transform.position + targetedPlayer.transform.forward * 10f;

            office.functionObject.GetComponent<DetentionRoomFunction>().Activate(time, ec);
            //audMan.QueueAudio(audTimes[detentionLevel]);
            //audMan.QueueAudio(audDetention);
            //audMan.QueueAudio(audScolds[UnityEngine.Random.Range(0, audScolds.Length)]);
            timeInSight[targetedPlayer.playerNumber] = 0f;
            //detentionLevel = Mathf.Min(detentionLevel + 1, audTimes.Length - 1);
            ec.MakeNoise(targetedPlayer.transform.position, detentionNoise);
            officeThatPlayersIsIn[targetedPlayer.playerNumber] = office;
        }

        public void TravelToDetention(Entity victim, bool isValid)
        {
            if (ec.offices.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, ec.offices.Count);
                CarTransform(true);
                behaviorStateMachine.ChangeState(new PoliceNerd_Carry(this, new PoliceNerd_Wandering(this), victim, ec.offices[index]));
                if (isValid && victim.CompareTag("Player"))
                {
                    overlay.gameObject.SetActive(true);
                    overlay.worldCamera = CoreGameManager.Instance.GetCamera(victim.GetComponent<PlayerManager>().playerNumber).canvasCam;
                }
            }
        }

        public new void Scold(string brokenRule)
        {
            audMan.FlushQueue(true);
            if (brokenRule != null)
            {
                if (targetedPlayer != null && !behaviorStateMachine.currentState.GetType().Equals(typeof(PoliceNerd_TargetingNPC))) savedRulebreak[targetedPlayer.playerNumber] = brokenRule;
                audMan.QueueAudio(whistle);
            }
        }

        public new void WhistleReact(Vector3 target)
        {
            audMan.FlushQueue(true);
            if (!inCar)
                CarTransform(true);
            behaviorStateMachine.ChangeState(new PoliceNerd_WhistleApproach(this, behaviorStateMachine.CurrentState, target));
            //audMan.PlaySingle(audComing);
        }

        public new void WhistleReached()
        {
            if (inCar)
                CarTransform(false);
        }

        public new void WhistleChance()
        {
            if (whistleRng.NextDouble() < (double)whistleChance && !audMan.QueuedAudioIsPlaying)
            {
                audMan.PlaySingle(CarnellPlugin.bcarnelAssets.Get<SoundObject>("PoliceNerd/GoofyHorn"));
            }
        }

        public void CarTransform(bool mode)
        {
            audMan.PlaySingle(mode ? CarnellPlugin.bcarnelAssets.Get<SoundObject>("PoliceNerd/CarEnabled") : CarnellPlugin.bcarnelAssets.Get<SoundObject>("PoliceNerd/CarOut"));
            inCar = mode;
            spriteRenderer[1].gameObject.SetActive(mode);
            AccessTools.DeclaredField(typeof(Navigator), "decelerate").SetValue(navigator, mode);
            navigator.accel = mode ? carSpeed + 50f : 15f;
            navigator.maxSpeed = mode ? carSpeed + 50f : defaultSpeed;
            navigator.SetSpeed(mode ? carSpeed : defaultSpeed);
            CapsuleCollider again = AccessTools.DeclaredField(typeof(Entity), "trigger").GetValue(gameObject.GetComponent<Entity>()) as CapsuleCollider;
            again.radius = mode ? 4.5f : 2f;
            if (mode && !navigator.passableObstacles.Contains(PassableObstacle.Bully))
                navigator.passableObstacles.Add(PassableObstacle.Bully);
            else if (!mode && navigator.passableObstacles.Contains(PassableObstacle.Bully))
                navigator.passableObstacles.Remove(PassableObstacle.Bully);
        }

        private IEnumerator delaySight()
        {
            float time = 0.2f;
            while (time > 0)
            {
                time -= Time.deltaTime * TimeScale;
                yield return null;
            }
            if (!looker.PlayerInSight() && players.Exists(x => x.ruleBreak.ToLower() == "escaping") && players.Exists(x => x.disobeying == true))
            {
                var player = players.Find(x =>
                {
                    if (x.ruleBreak.ToLower() == "escaping" &&
                    x.disobeying == true)
                        return x;
                    return players[0];
                });

                targetedPlayer = player;
                Scold(player.ruleBreak);
                if (!inCar)
                    CarTransform(true);
                behaviorStateMachine.ChangeState(new PoliceNerd_ChasingPlayer(this, player));
            }
        }
        public void BeforeRelization()
        {
            StartCoroutine(delaySight());
        }

        [Obsolete("Obsoleted by BCarnellTeacher.NavigationState_Arrested", true)]
        public static IEnumerator NPCsStuckInDetent(NPC npc, RoomController office)
        {
            var nav = npc.behaviorStateMachine.CurrentNavigationState;
            npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_PartyEvent(npc, 0, office));
            yield return new WaitForSecondsNPCTimescale(npc, UnityEngine.Random.Range(15, 30));
            npc.behaviorStateMachine.ChangeNavigationState(nav);
        }
    }
}