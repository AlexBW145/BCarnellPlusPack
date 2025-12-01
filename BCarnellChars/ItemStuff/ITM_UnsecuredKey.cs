using BCarnellChars.Patches;
using HarmonyLib;
using System.Reflection;

namespace BCarnellChars.ItemStuff;

public class ITM_UnsecuredKey : ITM_Acceptable
{
    private bool usedUpSticker = false;
    private static readonly FieldInfo 
        _audUse = AccessTools.DeclaredField(typeof(ITM_Acceptable), "audUse"),
        _disabled = AccessTools.DeclaredField(typeof(ItemManager), "disabled");
    public override bool Use(PlayerManager pm)
    {
        if (StickerManager.Instance.HasSticker(StickerPatches.securedPacket))
        {
            for (int i = StickerManager.Instance.stickerInventory.Count - 1; i >= 0; i--)
            {
                if (StickerManager.Instance.stickerInventory[i].sticker == StickerPatches.securedPacket)
                {
                    BaseGameManager.Instance.GiveRandomSticker(MiscPatches.bcppStickerPack, 2);
                    StickerManager.Instance.stickerInventory.RemoveAt(i);
                }
            }
            usedUpSticker = true;
            return true;
        }
        return !(bool)_disabled.GetValue(pm.itm) && base.Use(pm);
    }

    public override void PostUse(PlayerManager pm)
    {
        if (usedUpSticker)
        {
            var audUse = (SoundObject)_audUse.GetValue(this);
            if (audUse != null)
                CoreGameManager.Instance.audMan.PlaySingle(audUse);
            Destroy(gameObject);
            return;
        }
        base.PostUse(pm);
    }
}