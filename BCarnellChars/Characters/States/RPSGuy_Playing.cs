using System;
using System.Collections.Generic;
using System.Text;
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
