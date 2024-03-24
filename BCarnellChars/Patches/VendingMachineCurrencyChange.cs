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
    [HarmonyPatch(typeof(EnvironmentObject), "LoadingFinished")]
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
                    component.ReflectionSetVariable("requiredItem", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value);
                    var meshRender = __instance.ReflectionGetVariable("meshRenderer") as MeshRenderer;
                    meshRender.materials = meshRender.materials.AddToArray(BasePlugin.profitCardInsert);
                }
            }
        }
    }
}
