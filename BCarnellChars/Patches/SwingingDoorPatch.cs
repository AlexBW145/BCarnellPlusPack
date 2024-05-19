using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Patches
{
    [HarmonyPatch(typeof(SwingDoor))]
    class SwingingDoorPatches
    {
        static Items _item;
        [HarmonyPatch("ItemFits")]
        [HarmonyPrefix]
        static bool Shitty(Items item, SwingDoor __instance, ref bool __result)
        {
            _item = item;
            if (item == EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock") && !__instance.locked)
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPatch("InsertItem")]
        [HarmonyPrefix]
        static bool Ass(SwingDoor __instance)
        {
            if (_item == EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock"))
            {
                __instance.gameObject.AddComponent<SecuredSwingingDoor>();
                //__instance.gameObject.GetComponent<SecuredSwingingDoor>().originalOverlays = __instance.overlayLocked;
                return false;
            }    
            return true;
        }
    }

    [HarmonyPatch(typeof(ItemManager), "UseItem")]
    class NeverRemoveDat
    {
        static bool Prefix(ItemManager __instance, ref bool ___disabled) 
        {
            // Infinite Uses!!
            if (!___disabled && __instance.items[__instance.selectedItem].itemType == EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey"))
            {
                MonoBehaviour.Instantiate(__instance.items[__instance.selectedItem].item).Use(__instance.pm);
                //__instance.StartCoroutine(delay(__instance.pm));
                return false;
            }
            return true;
        }

        /*static IEnumerator delay(PlayerManager instance)
        {
            float time = 0.1f;
            int slot = instance.itm.selectedItem;
            while (time >= 0f)
            {
                time -= Time.deltaTime * instance.ec.EnvironmentTimeScale;
                yield return null;
            }
            if (instance.itm.items[slot].itemType != EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey"))
                instance.itm.SetItem(ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value, slot);
            yield break;
        }*/
    }
}
