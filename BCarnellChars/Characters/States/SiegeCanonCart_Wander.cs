using BepInEx.Logging;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace BCarnellChars.Characters.States
{
    // The entire First Prize code modified because Siege Canon Cart replaces First Prize on the decompile
    //  Don't ask why I'm doing this.
    public class SiegeCanonCart_Wander : SiegeCanonCart_StateBase
    {
        public SiegeCanonCart_Wander(SiegeCanonCart siegecart)
            : base(siegecart)
        {
        }

        private NavigationState targetState;

        private Vector3 nextTarget;
        private Vector3 _rotation;
        private IntVector2 currentStandardTargetPos;
        //private IntVector2 standardTargetPos;

        private float _angleDiff;

        private float shootyFireTime;
        private float shootyCooldown;

        private float unseenDelay;
        //private float previousFrameSpeed;
        //private bool playerInSight;

        public override void Enter()
        {
            base.Enter();
            targetState = new NavigationState_TargetPosition(npc, 63, npc.transform.position);
        }

        public override void Update()
        {
            //playerInSight = unseenDelay > 0f;
            /*if (nextTarget != Vector3.zero)
                TargetPosition(nextTarget);*/
            if (!npc.Navigator.HasDestination && npc.Navigator.Speed <= siegeCart.wanderSpeed + 1f)
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
            if (npc.Navigator.NextPoint != npc.transform.position)
                _angleDiff = Mathf.DeltaAngle(npc.transform.eulerAngles.y, Mathf.Atan2(npc.Navigator.NextPoint.x - npc.transform.position.x, npc.Navigator.NextPoint.z - npc.transform.position.z) * 57.29578f);
            else
                _angleDiff = 0f;
            if (Mathf.Abs(_angleDiff) > 5f)
            {
                npc.Navigator.maxSpeed = 0f;
                npc.Navigator.SetSpeed(0f);
            }
            else if (unseenDelay > 0f && (nextTarget == Vector3.zero || IntVector2.GetGridPosition(nextTarget) == IntVector2.GetGridPosition(npc.Navigator.CurrentDestination))) {
                TargetPosition(npc.ec.CellFromPosition(npc.transform.position).FloorWorldPosition);
                npc.Navigator.maxSpeed = siegeCart.wanderSpeed + 2f;
            }
            else
                npc.Navigator.maxSpeed = siegeCart.wanderSpeed;
            if (npc.Navigator.Speed < 2f || Mathf.Abs(_angleDiff) <= 5f)
            {
                _rotation = npc.transform.eulerAngles;
                _rotation.y += Mathf.Min(siegeCart.turnSpeed * Time.deltaTime * npc.TimeScale, Mathf.Abs(_angleDiff)) * Mathf.Sign(_angleDiff);
                npc.transform.eulerAngles = _rotation;
            }
            if (!npc.looker.PlayerInSight() && unseenDelay > 0f)
            {
                unseenDelay -= Time.deltaTime * npc.TimeScale;
                if (unseenDelay <= 0f)
                    LostPlayer();
            }


            //previousFrameSpeed = npc.Navigator.Speed;

            if (shootyCooldown > 0f)
                shootyCooldown -= Time.deltaTime * npc.TimeScale;
            if (shootyFireTime > 0f && unseenDelay <= 0f)
                shootyFireTime -= Time.deltaTime * npc.TimeScale;
        }

        public override void DestinationEmpty()
        {
            if (!npc.Navigator.Wandering)
            {
                npc.Navigator.maxSpeed = 0f;
                npc.Navigator.SetSpeed(0f);
            }
            base.DestinationEmpty();
            currentStandardTargetPos.x = 0;
            currentStandardTargetPos.z = 0;
            if (npc.Navigator.Speed <= siegeCart.wanderSpeed + 1f)
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public void TargetPosition(Vector3 target)
        {
            if (npc.Navigator.Speed > siegeCart.wanderSpeed + 1f)
            {
                nextTarget = target;
                return;
            }
            ChangeNavigationState(targetState);
            targetState.UpdatePosition(target);
            npc.Navigator.SkipCurrentDestinationPoint();
            nextTarget = Vector3.zero;
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (!player.Tagged)
            {
                // This shitty code took me awhile to figure out the math...
                Vector3 vector = player.transform.position - npc.transform.position;
                if (npc.Navigator.speed < 0.01f && IntVector2.GetGridPosition(npc.transform.position) == IntVector2.GetGridPosition(npc.ec.CellFromPosition(npc.transform.position).FloorWorldPosition))
                    TargetPosition(player.transform.position);
                else
                    TargetPosition(npc.ec.CellFromPosition(npc.transform.position).FloorWorldPosition);
                if (shootyCooldown <= 0f)
                {
                    unseenDelay = 1f;
                    if (shootyFireTime < 4f && npc.Navigator.speed <= 1f)
                        shootyFireTime += Time.deltaTime * npc.TimeScale;
                    else if (npc.Navigator.speed < 0.01f
                        && IntVector2.GetGridPosition(npc.transform.position) == IntVector2.GetGridPosition(npc.ec.CellFromPosition(npc.transform.position).FloorWorldPosition) &&
                        Vector3.Angle(npc.transform.forward, vector) <= 11.25f && vector != Vector3.zero)
                    {
                        npc.transform.LookAt(npc.ec.CellFromPosition(player.transform.position).CenterWorldPosition); // In case of grid snapping...
                        shootyFireTime = 0;
                        shootyCooldown = 7f;
                        siegeCart.Shoot();
                    }
                }
            }
            else if (unseenDelay > 0f)
                unseenDelay = 0f;
        }

        private void LostPlayer()
        {
            currentStandardTargetPos.x = 0;
            currentStandardTargetPos.z = 0;
            if (npc.Navigator.speed <= siegeCart.wanderSpeed + 1f)
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
            npc.Navigator.maxSpeed = siegeCart.wanderSpeed;
        }
    }
}
