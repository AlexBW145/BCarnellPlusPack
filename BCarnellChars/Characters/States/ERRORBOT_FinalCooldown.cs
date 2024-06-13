using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_FinalCooldown : ERRORBOT_StateBase
    {
        private float time;

        public ERRORBOT_FinalCooldown(NPC npc, ERRORBOT errbot, float time)
            : base(npc, errbot)
        {
            this.time = time;
        }

        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0f)
            {
                erbot.StartCooldown();
            }
        }

        public override void OnStateTriggerStay(Collider other)
        {
        }
    }
}
