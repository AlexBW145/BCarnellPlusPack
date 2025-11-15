using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using UnityEngine;

namespace BCarnellChars.Patches;

[HarmonyPatch]
internal class StickerPatches
{
    internal static Sticker anyportalSticker, stealerSticker, rpsSticker, switcherooSticker;
    internal static Items anyportalItem, profitCard;
    internal static ItemObject mrAnyportal;
    internal static System.Random anyportalRNG, stealerRNG;
    [HarmonyPatch(typeof(Entity), nameof(Entity.Teleport)), HarmonyPostfix]
    private static void AnyportalManSet(Vector3 position, Entity __instance)
    {
        if (__instance.Ec?.Active == true && __instance is PlayerEntity && StickerManager.Instance.StickerValue(anyportalSticker) > 0)
        {
            var player = __instance.GetComponent<PlayerManager>();
            anyportalRNG = new System.Random();
            for (int i = 0; i <= player.itm.maxItem; i++)
            {
                if (player.itm.items[i].itemType != anyportalItem) continue;
                if (anyportalRNG.NextDouble() < (double)(0.1f * StickerManager.Instance.StickerValue(anyportalSticker)))
                    player.itm.SetItem(mrAnyportal, i);
            }
        }
    }

    [HarmonyPatch(typeof(ITM_Teleporter), nameof(ITM_Teleporter.Use)), HarmonyPostfix]
    private static void WhyIsItNotUseThat(PlayerManager pm) => AnyportalManSet(default, pm.plm.Entity);

    private static bool usedUp = false; // __state is that confusing why is it not doing anything in two prefixes no postfixes??
    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.UseItem)), HarmonyPrefix]
    private static void WellItDoesNotCount(ItemManager __instance, bool ___disabled)
    {
        usedUp = false;
        if (!(___disabled && (!__instance.items[__instance.selectedItem].overrideDisabled || __instance.maxItem < 0)))
            usedUp = true;
    }
    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.UpdateSelect)), HarmonyPostfix]
    private static void ResetCondition() => usedUp = false;

    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.RemoveItem)), HarmonyPrefix]
    private static bool DoesItCountAsThievery()
    {
        if (usedUp) return true;
        if (StickerManager.Instance.StickerValue(stealerSticker) > 0)
        {
            stealerRNG = new System.Random();
            if (stealerRNG.NextDouble() < (double)(0.099f * StickerManager.Instance.StickerValue(stealerSticker)))
                return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(WeightedSelection<NPC>), nameof(WeightedSelection<NPC>.ControlledRandomIndexList)), HarmonyPrefix]
    private static void ChancePreserve(ref List<WeightedSelection<NPC>> items, System.Random rng, out System.Random __state)
    {
        __state = JsonUtility.FromJson<System.Random>(JsonUtility.ToJson(rng));
    }
    [HarmonyPatch(typeof(WeightedSelection<NPC>), nameof(WeightedSelection<NPC>.ControlledRandomIndexList)), HarmonyPostfix]
    private static void RPSGuyChances(ref List<WeightedSelection<NPC>> items, System.Random rng, ref int __result, System.Random __state)
    {
        if (StickerManager.Instance.StickerValue(rpsSticker) <= 0) return;
        int iFoundHim = -1;
        for (int i = 0; i < items.Count; i++)
            if (items[i].selection is RPSGuy)
            {
                iFoundHim = i;
                break;
            }
        if (iFoundHim >= 0)
        {
            if (__result == iFoundHim) return;
            List<WeightedSelection<NPC>> guys = new(items);
            for (int i = 0; i < guys.Count; i++)
            {
                if (guys[i].selection is RPSGuy)
                    guys[i] = new()
                    {
                        selection = guys[i].selection,
                        weight = guys[i].weight + (Mathf.RoundToInt(guys[i].weight / 2f) * StickerManager.Instance.StickerValue(rpsSticker))
                    };
            }
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            foreach (WeightedSelection<NPC> item in guys)
                num3 += item.weight;

            int num4 = __state.Next(0, num3);
            for (num = 0; num < guys.Count; num++)
            {
                num2 += guys[num].weight;
                if (num2 > num4)
                {
                    break;
                }
            }

            if (num < guys.Count)
            {
                __result = num;
                return;
            }

            Debug.Log("No valid selection found. Returning index 0");
            __result = 0;
        }
    }

    //\n\nEvery stack increases 0.1 value of the conversion\nwhere profit cards will return more quarters and profit cards will accept less quarters
    // Unused desc part ^^^
    private static bool isPostuse = false;
    [HarmonyPatch(typeof(ITM_Acceptable), nameof(ITM_Acceptable.PostUse)), HarmonyPrefix]
    private static void IsPostuse() => isPostuse = true;
    [HarmonyPatch(typeof(ITM_Acceptable), nameof(ITM_Acceptable.Use)), HarmonyPrefix]
    private static void IsUse() => isPostuse = false;

    [HarmonyPatch(typeof(SodaMachine), nameof(SodaMachine.ItemFits)), HarmonyPostfix]
    private static void SwitcherooAcceptor(Items checkItem, SodaMachine __instance, ItemObject ___requiredItem, int ___usesLeft, ref bool __result)
    {
        if (StickerManager.Instance.StickerValue(switcherooSticker) > 0)
        {
            if (___requiredItem.itemType == Items.Quarter && checkItem == profitCard)
            {
                __result = ___usesLeft > 0;
                if (___usesLeft > 0 && isPostuse)
                {
                    Cell cell = __instance.Ec.CellFromPosition(__instance.transform.position);
                    __instance.Ec.CreateItem(cell.room, ___requiredItem, new(cell.CenterWorldPosition.x, cell.CenterWorldPosition.z));
                }
            }
            else if (___requiredItem.itemType == profitCard && checkItem == Items.Quarter)
            {
                if (___usesLeft > 0 && !isPostuse)
                {
                    if (__instance.GetComponent<DummySwitcherooCheck>() == null)
                        __instance.gameObject.AddComponent<DummySwitcherooCheck>();
                    else
                        GameObject.DestroyImmediate(__instance.GetComponent<DummySwitcherooCheck>());
                    __result = true;
                }
                else if (___usesLeft > 0 && isPostuse)
                    __result = true;
                else
                    __result = false;
            }
        }
        if (!isPostuse && checkItem != Items.Quarter && ___requiredItem.itemType == checkItem && __instance.GetComponent<DummySwitcherooCheck>() != null)
            GameObject.DestroyImmediate(__instance.GetComponent<DummySwitcherooCheck>());
    }
    [HarmonyPatch(typeof(SodaMachine), nameof(SodaMachine.InsertItem)), HarmonyPrefix]
    private static bool SwitcherooDoNot(SodaMachine __instance) => __instance.GetComponent<DummySwitcherooCheck>() == null;

    [HarmonyPatch(typeof(TapePlayer), nameof(TapePlayer.ItemFits)), HarmonyPostfix]
    static void SwitcherooPayphone(Items checkItem, TapePlayer __instance, Items ___requiredItem, bool ___active, ref bool __result)
    {
        if (StickerManager.Instance.StickerValue(switcherooSticker) > 0)
        {
            if (___requiredItem == Items.Quarter && checkItem == profitCard)
            {
                __result = !___active;
                if (!___active && isPostuse)
                    __instance.gameObject.AddComponent<DummySwitcherooCheck>();
            }
        }
    }
    [HarmonyPatch(typeof(TapePlayer), nameof(TapePlayer.InsertItem)), HarmonyPostfix]
    static void PayphoneBack(PlayerManager player, EnvironmentController ec, TapePlayer __instance, Items ___requiredItem)
    {
        if (__instance.GetComponent<DummySwitcherooCheck>() != null && ___requiredItem == Items.Quarter)
        {
            player.itm.SetItem(ItemMetaStorage.Instance.FindByEnum(___requiredItem).value, player.itm.selectedItem);
            GameObject.DestroyImmediate(__instance.GetComponent<DummySwitcherooCheck>());
        }
    }
    [HarmonyPatch(typeof(RandomItemMachine), nameof(RandomItemMachine.ItemFits)), HarmonyPostfix]
    static void GamblerSwitcheroo(Items checkItem, RandomItemMachine __instance, ItemObject ___requiredItem, int ___usesLeft, ref bool __result)
    {
        if (StickerManager.Instance.StickerValue(switcherooSticker) > 0)
        {
            if (___requiredItem.itemType == profitCard && checkItem == Items.Quarter)
            {
                if (___usesLeft > 0 && !isPostuse)
                {
                    if (__instance.GetComponent<DummySwitcherooCheck>() == null)
                        __instance.gameObject.AddComponent<DummySwitcherooCheck>();
                    else
                        GameObject.DestroyImmediate(__instance.GetComponent<DummySwitcherooCheck>());
                    __result = true;
                }
                else if (___usesLeft > 0 && isPostuse)
                    __result = true;
                else
                    __result = false;
            }
        }
        if (!isPostuse && checkItem != Items.Quarter && ___requiredItem.itemType == checkItem && __instance.GetComponent<DummySwitcherooCheck>() != null)
            GameObject.DestroyImmediate(__instance.GetComponent<DummySwitcherooCheck>());
    }
    [HarmonyPatch(typeof(RandomItemMachine), nameof(RandomItemMachine.InsertItem)), HarmonyPrefix]
    private static bool GamblerSwitcherooDoNot(RandomItemMachine __instance) => __instance.GetComponent<DummySwitcherooCheck>() == null;
}
