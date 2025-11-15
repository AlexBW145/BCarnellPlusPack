using BBP_Playables.Core;
using BepInEx.Bootstrap;

namespace BCarnellChars
{
    internal class ProtagonistStuff
    {
        internal static bool IsMainProtagonist(PlayerManager pm) => Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.playablecharacters") && pm?.GetPlayable()?.GetCurrentPlayable()?.name.ToLower().Replace(" ", "") == "themainprotagonist";
    }
}
