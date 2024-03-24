using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class RPSGuy_Wandering : RPSGuy_StateBase
    {
        private float calloutTime = 3f;

        public RPSGuy_Wandering(NPC npc, RPSGuy rpsguy)
            : base(npc, rpsguy)
        {
        }

        public override void Enter()
        {
            base.Enter();
            npc.looker.Blink();
            if (!npc.Navigator.HasDestination)
            {
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
            }
        }

        public override void Update()
        {
            base.Update();
            calloutTime -= Time.deltaTime * npc.TimeScale;
            if (calloutTime <= 0f)
            {
                rpsGuy.CalloutChance();
                calloutTime = 3f;
            }
        }

        public override void OnStateTriggerEnter(Collider other)
        {
            base.OnStateTriggerEnter(other);
            if (other.CompareTag("Player"))
            {
                PlayerManager component = other.GetComponent<PlayerManager>();
                if (!component.Tagged)
                {
                    rpsGuy.StartRPS(component);
                }
            }
            else if (other.CompareTag("NPC"))
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

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void PlayerSighted(PlayerManager player)
        {
            base.PlayerSighted(player);
            if (!player.Tagged)
            {
                rpsGuy.StartPersuingPlayer(player);
            }
        }

        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);
            if (!player.Tagged)
            {
                rpsGuy.PersuePlayer(player);
            }
        }

        public override void PlayerLost(PlayerManager player)
        {
            base.PlayerLost(player);
            rpsGuy.PlayerTurnAround(player);
        }
    }

}
