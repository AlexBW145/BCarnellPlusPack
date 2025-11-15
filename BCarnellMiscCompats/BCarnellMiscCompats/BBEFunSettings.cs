using System;
using System.Collections.Generic;
using System.Text;
using BBE.CustomClasses;
using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using HarmonyLib;
using MTM101BaldAPI;
using BBE.Extensions;

namespace BCarnellChars.Patches
{
    [ConditionalPatchMod("rost.moment.baldiplus.extramod"), HarmonyPatch]
    class BBEFunSettings
    {
        [HarmonyPatch(typeof(MrPortalMan_Cooldown), MethodType.Constructor, [typeof(MrPortalMan), typeof(float)]), HarmonyPostfix]
        static void IsEvilHungerless(ref float ___cooldown)
        {
            if (FunSettingsType.QuantumSweep.IsActive())
                ___cooldown = 2f;
        }
        [HarmonyPatch(typeof(MrPortalMan_Wandering), nameof(MrPortalMan_Wandering.Initialize)), HarmonyPostfix]
        static void DoHungerLess(ref int ___numberOfFoodNeed)
        {
            if (FunSettingsType.QuantumSweep.IsActive())
                ___numberOfFoodNeed = int.MaxValue;
        }
    }
}
