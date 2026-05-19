using BCarnellChars.Patches;
using MTM101BaldAPI;
using System.Collections.Generic;
using UnityEngine;

namespace BCarnellChars.OtherStuff;

/*
public class RPSGuySticker : ExtendedStickerData
{
    public override bool CanBeApplied() => true;
    public override bool CanBeCovered(StickerStateData data) => !(!BaseGameManager.Instance.InPitstop() && BaseGameManager.Instance.Ec.EventsStarted);
}*/

internal class DummySwitcherooCheck : MonoBehaviour;

public class SecuredPacket : ExtendedStickerData
{
    public override bool CanBeApplied() => false;
    public override BooleanHandshake CanBeCovered(StickerStateData thisSticker, StickerStateData coveringSticker) => BooleanHandshake.AlwaysFalse;
    public override BooleanHandshake CanStackWith(StickerStateData thisSticker, StickerStateData otherSticker) => BooleanHandshake.AlwaysFalse;
    public override BooleanHandshake CanCoverSticker(StickerStateData thisSticker, StickerStateData otherSticker, int heldStickerSlot, int otherStickerSlot) => BooleanHandshake.AlwaysFalse;
}

public static class BCPPStickerStuff
{
    public static void GiveBCPPRandomStickers(this StickerManager __instance, List<WeightedSticker> _potentialStickersToAdd, WeightedSticker[] potentialStickers, int amount, bool openNow, bool sticky)
    {
        _potentialStickersToAdd.Clear();
        var potentialBCPPStickers = new List<WeightedSticker>();
        foreach (WeightedSticker weightedSticker in potentialStickers)
        {
            if (weightedSticker.selection == StickerPatches.rpsSticker
                || weightedSticker.selection == StickerPatches.stealerSticker
                || weightedSticker.selection == StickerPatches.anyportalSticker
                || weightedSticker.selection == StickerPatches.switcherooSticker)
                continue;
            else
                _potentialStickersToAdd.Add(new WeightedSticker(weightedSticker.selection, Mathf.RoundToInt(weightedSticker.weight * __instance.GetStickerOddsMultiplier(weightedSticker.selection))));
        }
        potentialBCPPStickers.AddRange([
            new WeightedSticker(StickerPatches.rpsSticker, Mathf.RoundToInt(100f * __instance.GetStickerOddsMultiplier(StickerPatches.rpsSticker))),
                new WeightedSticker(StickerPatches.stealerSticker, Mathf.RoundToInt(100f * __instance.GetStickerOddsMultiplier(StickerPatches.stealerSticker))),
                new WeightedSticker(StickerPatches.anyportalSticker, Mathf.RoundToInt(100f * __instance.GetStickerOddsMultiplier(StickerPatches.anyportalSticker))),
                new WeightedSticker(StickerPatches.switcherooSticker, Mathf.RoundToInt(100f * __instance.GetStickerOddsMultiplier(StickerPatches.switcherooSticker))),
                ]);

        __instance.AddSticker(potentialBCPPStickers.RandomSelection(), openNow, sticky, openNow);
        for (int j = 0; j < amount; j++)
            __instance.AddSticker(_potentialStickersToAdd.RandomSelection(), openNow, sticky, openNow);
    }

    public static void GiveSecuredPacket(this StickerManager __instance)
    {
        __instance.AddSticker(StickerPatches.securedPacket, true, false, false);
    }
}