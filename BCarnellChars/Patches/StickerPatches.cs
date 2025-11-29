using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BCarnellChars.Patches;

[HarmonyPatch]
internal class StickerPatches
{
    internal static Sticker anyportalSticker, stealerSticker, rpsSticker, switcherooSticker, securedPacket;
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
    [HarmonyPatch(typeof(ITM_Acceptable), nameof(ITM_Acceptable.Use)), HarmonyPrefix]
    static bool SwitcherooUse(PlayerManager pm, ref bool __result, Items ___item, ref RaycastHit ___hit, ref IItemAcceptor[] ____acceptors, LayerMaskObject ___layerMask)
    {
        if (StickerManager.Instance.StickerValue(switcherooSticker) <= 0
            || (___item != profitCard && ___item != Items.Quarter)) return true;
        if (Physics.Raycast(pm.transform.position, CoreGameManager.Instance.GetCamera(pm.playerNumber).transform.forward, out ___hit, pm.pc.Reach, ___layerMask.mask))
        {
            ____acceptors = ___hit.transform.GetComponents<IItemAcceptor>();
            IItemAcceptor[] acceptors = ____acceptors;
            foreach (IItemAcceptor itemAcceptor in acceptors)
            {
                if (itemAcceptor != null)
                {
                    var instance = ___hit.transform;
                    if (itemAcceptor.ItemFits(profitCard) && ___item == Items.Quarter)
                    {
                        if (instance.GetComponent<DummySwitcherooCheck>() == null)
                            instance.gameObject.AddComponent<DummySwitcherooCheck>();
                        else
                            GameObject.DestroyImmediate(instance.GetComponent<DummySwitcherooCheck>());
                        __result = true;
                        return false;
                    }
                    else if (itemAcceptor.ItemFits(Items.Quarter) && ___item == profitCard)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
        }
        return true;
    }
    [HarmonyPatch(typeof(ITM_Acceptable), nameof(ITM_Acceptable.PostUse)), HarmonyPrefix]
    static bool SwitcherooPostUse(PlayerManager pm, ITM_Acceptable __instance, Items ___item, IItemAcceptor[] ____acceptors, ref RaycastHit ___hit, SoundObject ___audUse)
    {
        if (StickerManager.Instance.StickerValue(switcherooSticker) <= 0
            || (___item != profitCard && ___item != Items.Quarter)) return true;
        IItemAcceptor[] acceptors = ____acceptors;
        foreach (IItemAcceptor itemAcceptor in acceptors)
        {
            if (itemAcceptor != null)
            {
                var instance = ___hit.transform;
                if (itemAcceptor.ItemFits(profitCard) && ___item == Items.Quarter)
                {
                    if (instance.GetComponent<DummySwitcherooCheck>() == null)
                        itemAcceptor.InsertItem(pm, pm.ec);
                    GameObject.Destroy(__instance.gameObject);
                    if (___audUse != null)
                    {
                        CoreGameManager.Instance.audMan.PlaySingle(___audUse);
                    }
                    return false;
                }
                else if (itemAcceptor.ItemFits(Items.Quarter) && ___item == profitCard)
                {
                    var quarter = ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value;
                    int slot = pm.itm.selectedItem;
                    pm.itm.SetItem(quarter, slot);
                    itemAcceptor.InsertItem(pm, pm.ec);
                    if (pm.itm.items[slot] != quarter)
                    {
                        Cell cell = pm.ec.CellFromPosition(instance.transform.position);
                        pm.ec.CreateItem(cell.room, quarter, new(cell.CenterWorldPosition.x, cell.CenterWorldPosition.z));
                    }
                    GameObject.Destroy(__instance.gameObject);
                    if (___audUse != null)
                    {
                        CoreGameManager.Instance.audMan.PlaySingle(___audUse);
                    }
                    return false;
                }
            }
        }
        return true;
    }
}
