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
    // Redoing this, useless.
    /*[HarmonyPatch(typeof(EnvironmentObject), "GiveController")]
    class SodaCostPatch
    {
        private static float profitCardChance = 0.5f;
        private static System.Random profitCardRng = new System.Random();
        static void Postfix(EnvironmentObject __instance)
        {
            SodaMachine component = __instance.GetComponent<SodaMachine>();
            if (component != null && !BasePlugin.inLevelEditor)
            {
                if (profitCardRng.NextDouble() < (double)profitCardChance)
                {
                    component.ReflectionSetVariable("requiredItem", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                    MeshRenderer meshRender = __instance.GetComponent<MeshRenderer>();
                    meshRender.materials = meshRender.materials.AddToArray(BasePlugin.profitCardInsert);
                }
            }
        }
    }*/
}
