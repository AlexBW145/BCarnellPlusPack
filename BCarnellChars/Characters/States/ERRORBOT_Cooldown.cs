using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_Cooldown : ERRORBOT_StateBase
    {
        private float time;

        public ERRORBOT_Cooldown(NPC npc, ERRORBOT errbot, float time)
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
                erbot.EndCooldown();
            }
        }
    }
}
