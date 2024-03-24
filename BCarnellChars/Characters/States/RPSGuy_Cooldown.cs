using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class RPSGuy_Cooldown : RPSGuy_StateBase
    {
        private float time;

        public RPSGuy_Cooldown(NPC npc, RPSGuy rpsguy, float time)
            : base(npc, rpsguy)
        {
            this.time = time;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0f)
            {
                rpsGuy.EndCooldown();
            }
        }

        public override void OnStateTriggerEnter(Collider other)
        {
            base.OnStateTriggerEnter(other);
            if (other.CompareTag("NPC"))
            {
                switch (other.GetComponent<NPC>().Character)
                {
                    case Character.Prize:
                        FirstPrize prize = other.GetComponent<FirstPrize>();
                        if (!rpsGuy.Dead && prize.Navigator.speed >= prize.slamSpeed)
                            rpsGuy.fuckingDies();
                        break;
                }
            }
        }
    }
}
