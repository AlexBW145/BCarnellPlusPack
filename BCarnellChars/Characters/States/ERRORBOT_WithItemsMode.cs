using Rewired;
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
            ChangeNavigationState(new NavigationState_TargetPlayer(erbot, 99, player.transform.position));
            npc.looker.Raycast(player.transform, Mathf.Min(distance, Mathf.Min(npc.looker.distance, npc.ec.MaxRaycast)), Singleton<CoreGameManager>.Instance.GetPlayer(player.playerNumber), erbot.regularMask, out bool _noObstacles);
            if (distance >= 10f && !erbot.looker.IsVisible)
                erbot.Navigator.SetSpeed(25f);
            else if (_noObstacles && distance < 15f && erbot.looker.PlayerInSight() && erbot.looker.IsVisible && !erbot.GetComponent<Entity>().Squished) // _noObstacles is just the usual layer mask for lookers...
                erbot.SayByeByeToYourItems(player);
            else
                erbot.Navigator.SetSpeed(0f);

            // Player's weak point is exposed, they are fucking dead!
            if (!player.itm.HasItem() && distance >= 15f)
            {
                erbot.Navigator.maxSpeed = 0f;
                erbot.Navigator.SetSpeed(0f);
                erbot.BecomeEvil(false, player);
            }

        }
    }
}
