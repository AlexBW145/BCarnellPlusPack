using BBTimes;
using BBTimes.CustomComponents;
using BBTimes.CustomContent.CustomItems;
using BBTimes.ModPatches.GeneratorPatches;
using BBTimes.Plugin;
using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using BCarnellChars.ItemStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BCarnellTimes
{
    [HarmonyPatch(typeof(BBTimes.CustomContent.CustomItems.ITM_Hammer), "Use")]
    class ActLikeBHammer
    {
        static bool Prefix(BBTimes.CustomContent.CustomItems.ITM_Hammer __instance, PlayerManager pm, ref bool __result)
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

    [HarmonyPatch(typeof(BCarnellChars.ItemStuff.ITM_Hammer), "Use")]
    class ActLikeTimesHammer
    {
        static bool Prefix(BCarnellChars.ItemStuff.ITM_Hammer __instance, PlayerManager pm, ref bool __result)
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

    [HarmonyPatch(typeof(PostRoomCreation), "ExecutePostRoomTasks")]
    class WelcomeToTheUnderground
    {
        static bool Prefix()
        {
            return PostRoomCreation.i.ld.name != "Basement1";
        }
    }

    // Useless
    /*[HarmonyPatch(typeof(ERRORBOT), "Jammed")]
    class Gummed
    {
        static void Postfix(ERRORBOT __instance, Collider other, ref AudioManager ___audMan, ref SoundObject ___splat)
        {
            if (other.GetComponent<ITM_Gum>())
            {
                __instance.looker.ReflectionSetVariable("layerMask", __instance.regularMask);
                __instance.spriteRenderer[1].color = Color.magenta;
                __instance.behaviorStateMachine.ChangeState(new ERRORBOT_Cooldown(__instance, __instance, UnityEngine.Random.RandomRangeInt(60, 120)));
                __instance.behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(__instance, 99));
                __instance.Navigator.maxSpeed = 0;
                __instance.Navigator.SetSpeed(0);
                ___audMan.PlaySingle(___splat);
            }
        }
    }*/
}
