using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_WithItemsMode : ERRORBOT_StateBase
    {
        private PlayerManager player;
        public ERRORBOT_WithItemsMode(NPC npc, ERRORBOT errbot, PlayerManager _player)
            : base(npc, errbot)
        {
            player = _player;
        }

        public override void Update()
        {
            base.Update();
            float distance = (erbot.transform.position - player.transform.position).magnitude;
            erbot.navigationStateMachine.ChangeState(new NavigationState_TargetPlayer(erbot, 99, player.transform.position));
            if (distance >= 10f && !erbot.looker.IsVisible)
                erbot.Navigator.SetSpeed(25f);
            else if (distance < 15f && erbot.looker.IsVisible && !erbot.GetComponent<Entity>().Squished)
                erbot.SayByeByeToYourItems(player);
            else
                erbot.Navigator.SetSpeed(0f);

            // Player's weak point is exposed, they are fucking dead.
            if (!player.itm.HasItem() && distance >= 15f)
            {
                erbot.Navigator.maxSpeed = 0f;
                erbot.Navigator.SetSpeed(0f);
                erbot.BecomeEvil(false, player);
            }

        }

        public override void OnStateTriggerStay(Collider other)
        {
            base.OnStateTriggerStay(other);
            erbot.Jammed(other);
        }
    }
}
