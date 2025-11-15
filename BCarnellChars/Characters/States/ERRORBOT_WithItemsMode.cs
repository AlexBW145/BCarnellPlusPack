using Rewired;
using System;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_WithItemsMode : ERRORBOT_StateBase
    {
        private PlayerManager player;
        private NavigationState targetState;
        private NavigationState_DoNothing noMovementState;
        public ERRORBOT_WithItemsMode(NPC npc, ERRORBOT errbot, PlayerManager _player)
            : base(npc, errbot)
        {
            player = _player;
        }

        public override void Enter()
        {
            base.Enter();
            erbot.Navigator.maxSpeed = 0f;
            erbot.Navigator.SetSpeed(25f);
            targetState = new NavigationState_TargetPlayer(erbot, 99, player.transform.position);
            noMovementState = new NavigationState_DoNothing(erbot, 99);
            ChangeNavigationState(targetState);
        }

        public override void Update()
        {
            base.Update();
            float distance = (erbot.transform.position - player.transform.position).magnitude;
            targetState.UpdatePosition(player.transform.position);
            npc.looker.Raycast(player.transform, Mathf.Min(distance, Mathf.Min(npc.looker.distance, npc.ec.MaxRaycast)), Singleton<CoreGameManager>.Instance.GetPlayer(player.playerNumber), erbot.regularMask, out bool _noObstacles);
            if (distance > 10f && !erbot.looker.IsVisible && !player.Invisible)
                ChangeNavigationState(targetState);
            else if (_noObstacles && distance < 10f && erbot.looker.PlayerInSight() && erbot.looker.IsVisible) // _noObstacles is just the usual layer mask for lookers...
                erbot.SayByeByeToYourItems(player);
            else
                ChangeNavigationState(noMovementState);

            // Player's weak point is exposed, they are fucking dead!
            if (!player.itm.HasItem() && distance >= 10f)
            {
                erbot.Navigator.maxSpeed = 0f;
                erbot.Navigator.SetSpeed(0f);
                erbot.BecomeEvil(false, player);
            }
        }
    }
}
