using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace BCarnellChars.Characters.States
{
    public class MrPortalMan_Wandering : MrPortalMan_StateBase
    {
        public MrPortalMan_Wandering(MrPortalMan mrportal)
            : base(mrportal)
        {
        }
        private Entity currentTarget;
        private int numberOfFoodNeed;
        private int currentAteFood = 0;

        public override void Initialize()
        {
            base.Initialize();
            numberOfFoodNeed = Mathf.RoundToInt(portalMan.ec.Npcs.Count - (portalMan.ec.Npcs.Count / 1.5f));
        }

        public override void Update()
        {
            base.Update();
            if (currentAteFood >= numberOfFoodNeed)
                portalMan.Rest();

            foreach (NPC npc in portalMan.ec.Npcs)
            {
                if (npc != portalMan)
                {
                    portalMan.looker.Raycast(npc.transform, Mathf.Min((portalMan.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, portalMan.looker.distance, portalMan.ec.MaxRaycast), out bool _sighted);
                    if (_sighted && currentTarget == null)
                        currentTarget = npc.GetComponent<Entity>();
                    break;
                }
            }

            if (!portalMan.Navigator.HasDestination && currentTarget == null)
                portalMan.Questioning();

            if (currentTarget != null)
                portalMan.navigationStateMachine.ChangeState(new NavigationState_TargetPosition(portalMan, 63, currentTarget.transform.position));
            else if (currentTarget == null && !portalMan.Navigator.HasDestination)
                portalMan.navigationStateMachine.ChangeState(new NavigationState_WanderRandom(portalMan, 0));
        }

        public override void PlayerSighted(PlayerManager player)
        {
            base.PlayerSighted(player);
            if (currentTarget == null)
                currentTarget = player.GetComponent<PlayerEntity>();
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            if (currentTarget != null) {
                portalMan.looker.Raycast(currentTarget.transform, Mathf.Min((portalMan.transform.position - currentTarget.transform.position).magnitude + currentTarget.GetComponent<NPC>().Navigator.Velocity.magnitude, portalMan.looker.distance, portalMan.ec.MaxRaycast), out bool _sighted);
                if (!_sighted)
                    currentTarget = null;
            }
        }

        public override void OnStateTriggerStay(Collider other)
        {
            base.OnStateTriggerEnter(other);
            if (other.CompareTag("NPC") || other.tag == "Player") {
                portalMan.TeleportAnIdiot(other.GetComponent<Entity>());
                if (other.GetComponent<Entity>() == currentTarget)
                    currentTarget = null;
                currentAteFood++;
            }
        }
    }
}
