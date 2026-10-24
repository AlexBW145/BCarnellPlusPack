using System;
using System.Collections;
using TeacherAPI;
using UnityEngine;

namespace BCarnellTeacher
{
    public class BCarnell_StateBase : TeacherState
    {
        public BCarnell_StateBase(BCarnell carnell) : base(carnell)
        {
            this.carnell = carnell;
        }
        protected virtual void ActivateSlapAnimation() 
        {
            if (carnell.RulerBroken)
                carnell.SlapBroken();
            else
                carnell.SlapNormal();
        }
        public override void DoorHit(StandardDoor door) => door.OpenTimedWithKey(door.DefaultTime, false); // She forgot to make this as default...
        /*public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            if (currentNotebooks >= maxNotebooks)
                carnell.AllNotebooks();
        }*/

        protected BCarnell carnell;
    }
    public class BCarnell_Tutor : BCarnell_StateBase
    {
        public BCarnell_Tutor(BCarnell tutor) : base(tutor) { }
        public override void Enter()
        {
            base.Enter();
            carnell.animator.Play("Wave", 1f);
            carnell.animator.SetDefaultAnimation("Happy", 1f);
            //carnell.audMan.PlaySingle(carnell.audios.Get<SoundObject>("hellothere"));
            carnell.Navigator.SetSpeed(0f);
            ChangeNavigationState(new NavigationState_DoNothing(carnell, 127));
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            base.NotebookCollected(currentNotebooks, maxNotebooks);
#if DEBUG
            Debug.Log("Balanced to " + Mathf.RoundToInt(maxNotebooks / 3));
#endif
            if (currentNotebooks == Math.Max(1, Mathf.RoundToInt(maxNotebooks / 3)))
                carnell.behaviorStateMachine.ChangeState(new BCarnell_GettingAngry(carnell));
        }
    }
    public class BCarnell_GettingAngry : BCarnell_StateBase
    {
        public BCarnell_GettingAngry(BCarnell bcarnell) : base(bcarnell) { }
        public override void Enter()
        {
            base.Enter();
            carnell.animator.SetDefaultAnimation("Stare", 1f);
            carnell.animator.Play("Stare", 1f);
            carnell.AudMan.FlushQueue(true);
        }
        public override void Initialize()
        {
            base.Initialize();
            carnell.TeleportToNearestDoor();
            if (!angryin)
                carnell.StartCoroutine(GetMad());
        }
        private IEnumerator GetMad()
        {
            angryin = true;
            var ogspeed = MusicManager.Instance.MidiPlayer.MPTK_Speed;
            ChangeNavigationState(new NavigationState_DoNothing(carnell, 127));
            while (MusicManager.Instance.MidiPlayer.MPTK_Speed > 0.1666666667f)
            {
                MusicManager.Instance.MidiPlayer.MPTK_Speed -= 0.1f * Time.deltaTime;
                yield return null;
            }
            CoreGameManager.Instance.audMan.PlaySingle(CarnellPlugin.bcarnelAssets.Get<SoundObject>("BCarnell/Angry"));
            carnell.ActivateSpoopMode();
            carnell.behaviorStateMachine.ChangeState(new BCarnell_Chase(carnell));
            MusicManager.Instance.MidiPlayer.MPTK_Speed = ogspeed;
            yield break;
        }
        private bool angryin;
    }
    public class BCarnell_Chase : BCarnell_StateBase
    {
        protected float delayTimer;
        public BCarnell_Chase(BCarnell bcarnell) : base(bcarnell) { }
        public override void OnStateTriggerStay(Entity otherEntity, Collider other, bool isValid)
        {
            base.OnStateTriggerStay(otherEntity, other, isValid);
            if (isValid && carnell.IsTouchingPlayer(other))
            {
                carnell.CaughtPlayer(other.GetComponent<PlayerManager>());
            }
        }
        public override void Enter()
        {
            base.Enter();
            carnell.animator.SetDefaultAnimation("SlapIdle", 1f);
            delayTimer = carnell.Delay;
            carnell.ResetSlapDistance();
            ChangeNavigationState(new NavigationState_WanderRandom(carnell, 0));
        }
        private bool steppedUseless = false;
        public override void Update()
        {
            base.Update();
            carnell.UpdateSlapDistance();
            delayTimer -= Time.deltaTime * npc.TimeScale;

            if (delayTimer <= 0f)
            {
                if (carnell.extraAngerDrain > 0 && this is BCarnell_Chase)
                    carnell.extraAngerDrain -= 1;

                carnell.Slap();
                if (carnell.animator.AnimationFrame == 3 && (carnell.animator.AnimationId == "Slap" || carnell.animator.AnimationId == "SlapBroken"))
                    carnell.animator.SetPause(false);
                else
                    steppedUseless = false;
                ActivateSlapAnimation();
                delayTimer = carnell.Delay;
            }
            else if (!steppedUseless && carnell.animator.AnimationFrame == 3 && (carnell.animator.AnimationId == "Slap" || carnell.animator.AnimationId == "SlapBroken"))
            {
                steppedUseless = true;
                carnell.animator.SetPause(true);
                delayTimer = carnell.Delay / 2f;
            }
        }
        public override void DestinationEmpty()
        {
            carnell.ClearSoundLocations(false, Vector3.zero);
            if (!carnell.looker.PlayerInSight())
                ChangeNavigationState(new NavigationState_WanderRandom(carnell, 0));
        }
        public override void PlayerInSight(PlayerManager player)
        {
            ChangeNavigationState(new NavigationState_TargetPlayer(carnell, 1, player.transform.position));
        }
    }
    public class BCarnell_Praise : BCarnell_StateBase
    {
        public NpcState previousState;
        private float time;

        public BCarnell_Praise(BCarnell bcarnell, NpcState previousState, float delay) : base(bcarnell)
        {
            this.previousState = previousState;
            time = delay * 2f;
        }
        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0) carnell.behaviorStateMachine.ChangeState(previousState);
        }
    }
}
