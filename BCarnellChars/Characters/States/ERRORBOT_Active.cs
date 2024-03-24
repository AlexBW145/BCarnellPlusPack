using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_Active : ERRORBOT_StateBase
    {
        public ERRORBOT_Active(NPC npc, ERRORBOT errbot)
            : base(npc, errbot)
        {

        }

        private float timer = 60f;
        public override void Update()
        {
            base.Update();
            timer -= Time.deltaTime * npc.TimeScale;
            if (timer <= 0f)
                erbot.EndCooldown();
        }

        public override void PlayerSighted(PlayerManager player)
        {
            base.PlayerSighted(player);
            if (!player.Tagged)
            {
                erbot.BecomeEvil(player.itm.HasItem(), player);
            }
        }

        public override void OnStateTriggerStay(Collider other)
        {
            base.OnStateTriggerStay(other);
            erbot.Jammed(other);
        }
    }
}
