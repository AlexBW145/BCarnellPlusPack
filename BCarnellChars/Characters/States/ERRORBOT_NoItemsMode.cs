using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_NoItemsMode : ERRORBOT_StateBase
    {
        private PlayerManager player;
        public ERRORBOT_NoItemsMode(NPC npc, ERRORBOT errbot, PlayerManager _player)
            : base(npc, errbot)
        {
            player = _player;
        }

        public override void Update()
        {
            base.Update();
            float distance = (erbot.transform.position - player.transform.position).magnitude;
            if (!erbot.looker.PlayerInSight() && distance >= 45f)
                erbot.Alert(player);

        }

        public override void OnStateTriggerStay(Collider other)
        {
            base.OnStateTriggerStay(other);
            erbot.Jammed(other);
        }
    }
}
