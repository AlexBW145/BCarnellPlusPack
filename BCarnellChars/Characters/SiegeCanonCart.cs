using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using BCarnellChars.Characters.States;
using HarmonyLib;
using BCarnellChars.OtherStuff;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;

namespace BCarnellChars.Characters
{
    public class SiegeCanonCart : NPC
    {
        public CustomSpriteAnimator animator;
        // This is stupidly unoptimized.
        public Sprite idleFrame;
        public List<Sprite> movingFrames;
        public List<Sprite> shootingFrames;

        private AudioManager audMan;
        private SoundObject bang;
        private AudioManager motorAudMan;

        public float wanderSpeed = 10f;
        public float turnSpeed = 22.5f;

        public SiegeCartBalls ballPre;

        private void Awake()
        {
            audMan = gameObject.GetComponents<AudioManager>()[0];
            bang = Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(b => b.name == "Bang");

            motorAudMan = gameObject.GetComponents<AudioManager>()[1];
        }

        private void Start()
        {
            if (BasePlugin.RadarModExists && ec != null)
                Singleton<BaseGameManager>.Instance.Ec.map.AddArrow(transform, RadarSupport.SiegeCartColor.Value);
        }

        public override void Initialize()
        {
            base.Initialize();
            animator.animations.Add("Idle", new CustomAnimation<Sprite>([idleFrame], 1f));
            animator.animations.Add("Moving", new CustomAnimation<Sprite>(movingFrames.ToArray(), 0.3f));
            animator.animations.Add("Shoot", new CustomAnimation<Sprite>(shootingFrames.ToArray(), 0.3f));
            animator.SetDefaultAnimation("Idle", 1f);
            behaviorStateMachine.ChangeState(new SiegeCanonCart_Wander(this));
        }
        protected override void VirtualUpdate()
        {
            motorAudMan.pitchModifier = 1f + navigator.speed / 50f;
            spriteBase.GetComponent<AnimatedSpriteRotator>().targetSprite = spriteRenderer[1].sprite;

            if (navigator.speed > 0)
                animator.SetDefaultAnimation("Moving", navigator.speed - 0.3f);
            else
                animator.SetDefaultAnimation("Idle", 1f);
        }

        public void Shoot()
        {
            SiegeCartBalls ball = Instantiate(ballPre);
            ball.ec = ec;
            ball.transform.position = transform.position;
            ball.transform.rotation = transform.rotation;
            ball.GetComponent<Entity>().Initialize(ec, transform.position);
            ball.GetComponent<Entity>().SetActive(true);
            audMan.PlaySingle(bang);
            animator.Play("Shoot", 1f);
        }
    }
}
