using BaldisBasicsPlusAdvanced.Game.Objects.Voting.Topics;
using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Patches
{
    [ConditionalPatchMod("baldi.basics.plus.advanced.mod"), HarmonyPatch]
    class AdvancedPatches
    {
        [HarmonyPatch(typeof(ConvertVendingMachinesTopic), nameof(ConvertVendingMachinesTopic.OnVotingEndedPost)), HarmonyPostfix]
        static void AlsoConvertGamblings(bool isWin)
        {
            if (isWin)
            {
                List<WeightedItemObject> list = new List<WeightedItemObject>();
                ItemMetaData[] array = ItemMetaStorage.Instance.All();
                for (int i = 0; i < array.Length; i++)
                {
                    list.Add(new WeightedItemObject
                    {
                        selection = array[i].value,
                        weight = array[i].tags.Contains("BCPP") ? 111 : 100
                    });
                }

                WeightedItemObject[] setVal = list.ToArray();
                RandomItemMachine[] gambler = GameObject.FindObjectsOfType<RandomItemMachine>(false);
                foreach (RandomItemMachine instance in gambler)
                {
                    instance.ReflectionSetVariable("potentialItems", setVal);
                    instance.ReflectionSetVariable("usesLeft", 10);
                }
            }
        }
        [HarmonyPatch(typeof(GottaSweepTimeTopic), nameof(GottaSweepTimeTopic.OnVotingEndedPost)), HarmonyPostfix]
        static void AlsoAffectPortalMan(bool isWin, ref EnvironmentController ___ec)
        {
            if (isWin)
                foreach (MrPortalMan man in ___ec.Npcs.FindAll(x => x is MrPortalMan))
                {
                    if (man.behaviorStateMachine.currentState is MrPortalMan_Cooldown)
                        man.StartToDevour();
                    MrPortalMan_Wandering state = man.behaviorStateMachine.CurrentState as MrPortalMan_Wandering;
                    state.ReflectionSetVariable("numberOfFoodNeed", int.MaxValue);
                }
        }
    }
}
