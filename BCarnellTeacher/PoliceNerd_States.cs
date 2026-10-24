using System.Collections;
using UnityEngine;
namespace BCarnellTeacher
{
    public class PoliceNerd_StateBase : NpcState
    {
        public PoliceNerd_StateBase(PoliceNerd nerd) : base(nerd)
        {
            policenerd = nerd;
        }
        protected PoliceNerd policenerd;

        public override void OnRoomExit(RoomController room)
        {
            base.OnRoomExit(room);
            if (room.ec.timeOut && room.type == RoomType.Room && !room.HasIncompleteActivity)
                room.SetPower(false);
        }

        public override void DoorHit(StandardDoor door) => door.OpenTimedWithKey(door.DefaultTime + (policenerd.PoliceCarMode ? 2f : 0f), false);

        protected virtual void GetOutOfMyWay(Entity other, bool isValid)
        {
            if (isValid && policenerd.PoliceCarMode && (other.CompareTag("NPC") | other.CompareTag("Player")))
            {
                if (!other.Squished)
                    if (other.CompareTag("Player"))
                        CoreGameManager.Instance.audMan.PlaySingle(policenerd.bang);
                    else
                        other.GetComponent<AudioManager>()?.PlaySingle(policenerd.bang);
                other.Squish(10f);
            }
        }
    }
    public class PoliceNerd_SubState : PoliceNerd_StateBase
    {
        protected NpcState previousState;

        public PoliceNerd_SubState(PoliceNerd nerd, NpcState previousState)
            : base(nerd)
        {
            this.previousState = previousState;
        }
    }
    public class PoliceNerd_Wandering : PoliceNerd_StateBase
    {
        public PoliceNerd_Wandering(PoliceNerd nerd) : base(nerd) { }
        private float whislteTime = 1f;
        private System.Random checkDetentionRoomRNG = new System.Random();
        private float checkDetentionRoomChance = 2f;
        private int wandersUntilChance = 0;

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_WanderRounds(npc, 0));
            policenerd.SwitchToNormalSprite();
        }

        public override void Update()
        {
            foreach (NPC npc in policenerd.ec.Npcs)
            {
                if (((npc.Disobeying && npc.Navigator?.Entity.Visible == true) || (npc.gameObject?.name?.StartsWith("Mr Robber") == true && npc.spriteRenderer[0].color != Color.clear && npc.spriteRenderer[0].gameObject.activeSelf)) && npc.gameObject != policenerd.gameObject)
                {
                    policenerd.looker.Raycast(npc.transform, Mathf.Min((policenerd.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, policenerd.looker.distance, npc.ec.MaxRaycast), out var targetSighted);
                    if (targetSighted)
                    {
                        policenerd.behaviorStateMachine.ChangeState(new PoliceNerd_TargetingNPC(policenerd, npc));
                        policenerd.Scold(npc.BrokenRule);
                    }

                    break;
                }
            }

            whislteTime -= Time.deltaTime * npc.TimeScale;
            if (whislteTime <= 0f)
            {
                policenerd.WhistleChance();
                whislteTime = 1f;
            }
        }

        public override void PlayerInSight(PlayerManager player) => policenerd.ObservePlayer(player);

        public override void PlayerLost(PlayerManager player) => policenerd.LoseTrackOfPlayer(player);

        public override void DestinationEmpty()
        {
            wandersUntilChance++;
            var target = policenerd.targetedOffice;
            if (checkDetentionRoomRNG.NextDouble() < (double)checkDetentionRoomChance && wandersUntilChance >= 3 && target != npc.transform.position) {
                wandersUntilChance = 0;
                ChangeNavigationState(new NavigationState_TargetPosition(policenerd, 62, target));
            }
            else
                ChangeNavigationState(new NavigationState_WanderRounds(policenerd, 0));
        }

        public override void DoorHit(StandardDoor door)
        {
            if (!door.IsOpen && !policenerd.PoliceCarMode)
            {
                if (npc.ec.CellFromPosition(npc.transform.position) == door.aTile)
                {
                    if (door.bTile.room.category == RoomCategory.Faculty)
                    {
                        policenerd.FacultyDoorHit(door, door.bTile);
                        return;
                    }
                }
                else if (door.aTile.room.category == RoomCategory.Faculty)
                {
                    policenerd.FacultyDoorHit(door, door.aTile);
                    return;
                }
            }

            base.DoorHit(door);
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid) => GetOutOfMyWay(otherentity, isValid);
    }

    public class PoliceNerd_TargetingNPC : PoliceNerd_StateBase
    {
        protected NavigationState_TargetPosition targetState;

        protected NPC targetedNpc;

        public PoliceNerd_TargetingNPC(PoliceNerd nerd, NPC targetedNpc)
            : base(nerd)
        {
            this.targetedNpc = targetedNpc;
            targetState = new NavigationState_TargetPosition(policenerd, 63, targetedNpc.transform.position);
        }

        public override void Enter()
        {
            base.Enter();
            if (targetedNpc?.Character == Character.Bully && !npc.Navigator.passableObstacles.Contains(PassableObstacle.Bully))
                npc.Navigator.passableObstacles.Add(PassableObstacle.Bully);

            ChangeNavigationState(targetState);
            policenerd.SwitchToChaseSprite();
        }

        public override void Update()
        {
            if (targetedNpc == null || targetedNpc.Navigator?.Entity.Visible == false)
                return;
            targetState.UpdatePosition(targetedNpc.transform.position);
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
            if (other.transform == targetedNpc?.transform)
            {
                /*int index = UnityEngine.Random.Range(0, policenerd.ec.offices.Count);
                targetedNpc.transform.position = policenerd.ec.offices[index].RandomEntitySafeCellNoGarbage().FloorWorldPosition + Vector3.up * 5f;*/
                if (targetedNpc.Character != Character.Bully)
                    policenerd.TravelToDetention(targetedNpc.gameObject.GetComponent<Entity>(), isValid);
                else
                {
                    policenerd.behaviorStateMachine.ChangeState(new PoliceNerd_Wandering(policenerd));
                    if (isValid)
                        targetedNpc.SentToDetention();
                }
            }
            else if (other.gameObject != targetedNpc?.gameObject)
                GetOutOfMyWay(otherentity, isValid);
        }

        public override void DestinationEmpty()
        {
            if (targetedNpc == null || targetedNpc.Navigator?.Entity.Visible == false || !targetedNpc.spriteBase.activeSelf || targetedNpc.Navigator?.Entity.CurrentRoom?.category == RoomCategory.Office
                || targetedNpc.Character == Character.Bully)
                policenerd.behaviorStateMachine.ChangeState(new PoliceNerd_Wandering(policenerd));
        }

        public override void Exit()
        {
            if (targetedNpc?.Character == Character.Bully)
            {
                targetedNpc.ClearGuilt();
                npc.Navigator.passableObstacles.Remove(PassableObstacle.Bully);
            }
        }
    }

    public class PoliceNerd_ChasingPlayer : PoliceNerd_StateBase
    {
        protected NavigationState_TargetPlayer targetState;

        protected PlayerManager player;

        public PoliceNerd_ChasingPlayer(PoliceNerd nerd, PlayerManager player)
            : base(nerd)
        {
            this.player = player;
        }

        public override void Enter()
        {
            base.Enter();
            targetState = new NavigationState_TargetPlayer(npc, 63, player.transform.position);
            ChangeNavigationState(targetState);
            policenerd.SwitchToChaseSprite();
        }

        public override void DestinationEmpty()
        {
            ChangeNavigationState(new NavigationState_WanderRounds(npc, 0));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (this.player == player)
            {
                ChangeNavigationState(targetState);
                targetState.UpdatePosition(player.transform.position);
            }
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
            if (other.CompareTag("Player") && other.GetComponent<PlayerManager>() == player)
                policenerd.TravelToDetention(other.gameObject.GetComponent<Entity>(), isValid);
            else if (other.gameObject != player.gameObject)
                GetOutOfMyWay(otherentity, isValid);
        }

        public override void Update()
        {
            base.Update();
            if (policenerd.AllKnowing)
                targetState.UpdatePosition(player.transform.position);
        }
    }

    public class PoliceNerd_WhistleApproach : PoliceNerd_SubState
    {
        protected Vector3 destination;
        public PoliceNerd_WhistleApproach(PoliceNerd nerd, NpcState previousState, Vector3 destination)
            : base(nerd, previousState)
        {
            this.destination = destination;
        }

        public override void Initialize()
        {
            base.Initialize();
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 63, destination));
        }

        public override void Resume()
        {
            npc.behaviorStateMachine.ChangeState(previousState);
        }

        public override void DestinationEmpty()
        {
            npc.behaviorStateMachine.ChangeState(previousState);
        }

        public override void Exit()
        {
            if (!previousState.GetType().Equals(typeof(PoliceNerd_Carry)))
                policenerd.WhistleReached();
        }

        public override void OnStateTriggerStay(Entity otherentity,Collider other, bool isValid) => GetOutOfMyWay(otherentity, isValid);
    }

    public class PoliceNerd_Carry : PoliceNerd_SubState
    {
        protected Entity victim;
        protected RoomController office;

        private MovementModifier moveMod = new MovementModifier(Vector3.zero, 0f);

        public PoliceNerd_Carry(PoliceNerd nerd, NpcState previousState, Entity victim, RoomController office)
            : base(nerd, previousState)
        {
            this.victim = victim;
            this.office = office;
        }
        private bool iWasSquished = false;

        public override void Enter()
        {
            moveMod.forceTrigger = true;
            base.Enter();
            iWasSquished = policenerd.Navigator.Entity.Squished;
            if (!iWasSquished)
            {
                victim.ExternalActivity.moveMods.Add(moveMod);
                victim.Teleport(npc.transform.position);
                ChangeNavigationState(new NavigationState_TargetPosition(npc, 63, npc.ec.RealRoomMid(office)));
                victim.SetTrigger(false);
            }
            else
                ChangeNavigationState(new NavigationState_TargetPosition(npc, 63, npc.ec.RealRoomMid(office)));
        }

        public override void Update()
        {
            if (!iWasSquished && victim?.CurrentRoom == office) {
                npc.behaviorStateMachine.ChangeState(previousState);
                return;
            }
            moveMod.movementAddend = policenerd.Navigator.Velocity.normalized * policenerd.Navigator.speed * policenerd.Navigator.Am.Multiplier;
        }
        public override void DestinationEmpty()
        {
            var pathAccessible = npc.ec.CheckPath(npc.ec.CellFromPosition(policenerd.transform.position), npc.ec.CellFromPosition(npc.ec.RealRoomMid(office)), PathType.Nav);
            if ((!iWasSquished && victim?.CurrentRoom == office && pathAccessible) || victim == null)
                npc.behaviorStateMachine.ChangeState(previousState);
            else if (!pathAccessible) // Like Detention Bot, Police Nerd sometimes softlocks himself in certain situations.
            {
                if (office.HasDoorPositions)
                    policenerd.Navigator.Entity.Teleport(office.doors[0].aTile.CenterWorldPosition);
                else
                    policenerd.Navigator.Entity.Teleport(npc.ec.RealRoomMid(office));
                CurrentNavigationState.UpdatePosition(npc.ec.RealRoomMid(office));
            }
            else
                policenerd.behaviorStateMachine.ChangeState(victim?.CompareTag("Player") == true ? new PoliceNerd_ChasingPlayer(policenerd, victim?.gameObject.GetComponent<PlayerManager>()) : new PoliceNerd_TargetingNPC(policenerd, victim?.gameObject.GetComponent<NPC>()));
        }
        public override void Exit()
        {
            victim?.ExternalActivity.moveMods.Remove(moveMod);
            if (!iWasSquished && victim?.CurrentRoom == office)
            {
                if (victim.transform.CompareTag("Player"))
                    policenerd.SendToDetention(office);
                else if (victim.transform.CompareTag("NPC"))
                {
                    if (victim.gameObject?.name?.StartsWith("Mr Robber") == true)
                    {
                        victim.SetActive(false);
                        return;
                    }
                    victim.GetComponent<NPC>().SentToDetention();
                    victim.GetComponent<NPC>().ClearGuilt();
                    victim.Teleport(npc.ec.RealRoomMid(office));
                    victim.GetComponent<NPC>().navigationStateMachine.ChangeState(new NavigationState_Arrested(victim.GetComponent<NPC>(), 127, office));
                }
                if (!victim.transform.CompareTag("Player")) policenerd.CarTransform(false);
            }
            if (policenerd.overlay.isActiveAndEnabled)
            {
                policenerd.overlay.gameObject.SetActive(false);
                policenerd.overlay.worldCamera = null;
            }
            if (!iWasSquished)
                victim?.SetTrigger(true);
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
            if (other.gameObject != victim?.gameObject)
                GetOutOfMyWay(otherentity, isValid);
        }

        public override void OnStateTriggerExit(Entity otherentity, Collider other, bool isValid)
        {
            if (!iWasSquished && other.GetComponent<Entity>() == victim)
                policenerd.behaviorStateMachine.ChangeState(victim.CompareTag("Player") ? new PoliceNerd_ChasingPlayer(policenerd, victim.gameObject.GetComponent<PlayerManager>()) : new PoliceNerd_TargetingNPC(policenerd, victim.gameObject.GetComponent<NPC>()));
        }
    }

    public class NavigationState_Arrested : NavigationState
    {
        private RoomController office;
        private float time = 1f;

        public NavigationState_Arrested(NPC npc, int priority, RoomController office)
            : base(npc, priority)
        {
            this.office = office;
            time = UnityEngine.Random.Range(15, 30);
        }

        public override void Enter()
        {
            base.Enter();
            npc.Navigator.FindPath(office.RandomEventSafeCellNoGarbage().CenterWorldPosition);
            npc.StartCoroutine(Timer());
        }

        private IEnumerator Timer()
        {
            while (time > 0f)
            {
                time -= Time.deltaTime * npc.TimeScale;
                yield return null;
            }
            End();
            yield break;
        }

        public override void DestinationEmpty()
        {
            npc.Navigator.FindPath(office.RandomEventSafeCellNoGarbage().CenterWorldPosition);
        }

        public void End()
        {
            priority = 0;
            npc.behaviorStateMachine.RestoreNavigationState();
        }
    }
}