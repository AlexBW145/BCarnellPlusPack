using BBTimes;
using BBTimes.CustomComponents;
using BBTimes.CustomContent.CustomItems;
using BBTimes.Plugin;
using BCarnellChars.Characters;
using BCarnellChars.ItemStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellTimes
{
    // ITM_Hammer & Hammer doesn't conflict names, phew!
    [HarmonyPatch(typeof(Hammer), "Use")]
    class ActLikeBHammer
    {
        static bool Prefix(Hammer __instance, PlayerManager pm, ref bool __result)
        {
            if (pm.ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("RPSGuy")))
            {
                RPSGuy component2 = pm.ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).gameObject.GetComponent<RPSGuy>();
                if (component2 != null && component2.Playing)
                {
                    component2.fuckingDies();
                    MonoBehaviour.Destroy(__instance.gameObject);
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ITM_Hammer), "Use")]
    class ActLikeTimesHammer
    {
        static bool Prefix(ITM_Hammer __instance, PlayerManager pm, ref bool __result)
        {
            if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out var raycastHit, pm.pc.reach, LayerMask.GetMask("Default", "Windows")))
            {
                CustomWindowComponent component = raycastHit.transform.GetComponent<CustomWindowComponent>();
                if (component != null)
                {
                    raycastHit.transform.GetComponent<Window>().Break(true);
                    MonoBehaviour.Destroy(__instance.gameObject);
                    if (!component.unbreakable)
                        pm.RuleBreak("breakingproperty", 3f, 0.15f);
                    //Debug.LogWarning("That's a " + (component.unbreakable ? "metal window, dumbass!" : "window..."));
                    __result = !component.unbreakable;
                    return false;
                }
            }
            return true;
        }
    }
}
