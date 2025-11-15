using UnityEngine;
using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;

namespace BCarnellChars.Characters
{
    public class SiegeCanonCart : NPC
    {
        [SerializeField] internal CustomSpriteRotatorAnimator animator;

        [SerializeField] internal AudioManager audMan, motorAudMan;
        [SerializeField] internal SoundObject bang;

        [SerializeField] internal float wanderSpeed = 10f, turnSpeed = 22.5f;

        [SerializeField] internal SiegeCartBalls ballPre;

        public override void Initialize()
        {
            this.ChangeRadarColor();
            base.Initialize();
            animator.ec = ec;
            animator.SetDefaultAnimation("Idle", 1f);
            behaviorStateMachine.ChangeState(new SiegeCanonCart_Wander(this));
        }
        protected override void VirtualUpdate()
        {
            motorAudMan.pitchModifier = 1f + navigator.speed / 50f;

            if (navigator.speed > 0 && animator.AnimationId != "Shoot")
            {
                if (animator.AnimationId != "Moving")
                    animator.Play("Moving", navigator.speed, true);
                else
                    animator.ChangeSpeed(navigator.speed);
            }
            else if (animator.AnimationId == "Moving")
                animator.Stop();
        }

        public void Shoot()
        {
            SiegeCartBalls ball = Instantiate(ballPre);
            ball.ec = ec;
            ball.transform.position = transform.position;
            ball.transform.rotation = transform.rotation;
            ball.entity.Initialize(ec, transform.position);
            ball.entity.CopyStatusEffects(navigator.Entity);
            audMan.PlaySingle(bang);
            animator.Play("Shoot", 1f);
        }
    }
}
