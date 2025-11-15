using BaldisBasicsPlusAdvanced.Game.Objects.Voting.Topics;
using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Patches
{
    [ConditionalPatchMod("baldi.basics.plus.advanced.mod"), HarmonyPatch]
    class AdvancedPatches
    {
        private static FieldInfo 
            _usesLeft = AccessTools.DeclaredField(typeof(RandomItemMachine), "usesLeft"),
            _numberOfFoodNeed = AccessTools.DeclaredField(typeof(MrPortalMan_Wandering), "numberOfFoodNeed");
        [HarmonyPatch(typeof(ConvertVendingMachinesTopic), nameof(ConvertVendingMachinesTopic.OnVotingEndedPre)), HarmonyPostfix]
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
                var replenishSprite = BasePlugin.bcppAssets.Get<Sprite>("Structures/RandomMachine");
                foreach (RandomItemMachine instance in gambler)
                {
                    instance.potentialItems = setVal;
                    instance.render.sprite = replenishSprite;
                    _usesLeft.SetValue(instance, 10);
                }
            }
        }
        [HarmonyPatch(typeof(GottaSweepTimeTopic), nameof(GottaSweepTimeTopic.OnVotingEndedPre)), HarmonyPostfix]
        static void AlsoAffectPortalMan(bool isWin, ref EnvironmentController ___ec)
        {
            if (isWin)
                foreach (MrPortalMan man in ___ec.Npcs.FindAll(x => x is MrPortalMan))
                {
                    if (man.behaviorStateMachine.currentState is MrPortalMan_Cooldown)
                        man.StartToDevour();
                    MrPortalMan_Wandering state = man.behaviorStateMachine.CurrentState as MrPortalMan_Wandering;
                    _numberOfFoodNeed.SetValue(state, int.MaxValue);
                }
        }
        [HarmonyPatch(typeof(GottaSweepTimeTopic), nameof(GottaSweepTimeTopic.IsAvailable)), HarmonyPostfix]
        static void DoesHeExist(ref bool __result, ref EnvironmentController ___ec)
        {
            if (!__result)
                __result = ___ec.npcsToSpawn.Exists(x => x is MrPortalMan);
        }
    }
}
