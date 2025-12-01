using BaldiBasicsPlusDiscordRPC;

namespace BCarnellChars
{
    internal static class DiscordRPCStuff
    {
        internal static void Init()
        {
#if DEBUG
            BaldiPlusRPCPlugin.AddRPCData([
                "BCPP_BRoute1",
                "BCPP_Pac75",
                "BCPP_BRoute2"
                ],
                [
                    new RichPressenceData()
                    {
                        sceneObject = BasePlugin.Instance.sBasement,
                        stateText = "Dwelling through the restricted basement",
                        largeImageURL = "https://images.gamebanana.com/img/ss/mods/66fa0f387213c.jpg"
                    },
                    new RichPressenceData()
                    {
                        sceneObject = BasePlugin.Instance.sPac,
                        stateText = "Playing PAC-75",
                        largeImageURL = "https://images.gamebanana.com/img/ss/mods/66fa0f387213c.jpg"
                    },
                    new RichPressenceData()
                    {
                        sceneObject = BasePlugin.Instance.sBoss,
                        stateText = "WERE YOU LOOKING FOR SOMETHING?",
                        largeImageURL = "https://images.gamebanana.com/img/ss/mods/66fa0f387213c.jpg"
                    }
                    ]);
#endif
        }
    }
}
