using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class RPSGuy_Playing : RPSGuy_StateBase
    {
        public RPSGuy_Playing(NPC npc, RPSGuy rpsguy)
            : base(npc, rpsguy)
        {
        }

        public override void Enter()
        {
        }

        public override void PlayerLost(PlayerManager player)
        {
            base.PlayerLost(player);
            rpsGuy.EndRPS(false, true);
        }
    }
}
