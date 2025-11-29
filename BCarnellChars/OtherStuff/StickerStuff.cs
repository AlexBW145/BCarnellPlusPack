using MTM101BaldAPI;
using UnityEngine;

namespace BCarnellChars.OtherStuff;

public class RPSGuySticker : ExtendedStickerData
{
    public override bool CanBeApplied() => true;
    public override bool CanBeCovered(StickerStateData data) => !(!BaseGameManager.Instance.InPitstop() && BaseGameManager.Instance.Ec.EventsStarted);
}

internal class DummySwitcherooCheck : MonoBehaviour
{
}

public class SecuredPacket : ExtendedStickerData
{
    public override bool CanBeApplied() => false;
    public override bool CanBeCovered(StickerStateData data) => false;
}