using ModdedModesAPI.ModesAPI;

namespace BCarnellChars
{
    internal class CustomModeBRoute
    {
        public static void Init()
        {
            CustomModesHandler.OnMainMenuInitialize += () =>
            {
                var BRoutePage = ModeObject.CreateModeObjectOverExistingScreen(SelectionScreen.ChallengesScreen);
                //BRoutePage.StandardButtonBuilder.CreateSeedInput(out var seedinput);
                var B1Button = BRoutePage.StandardButtonBuilder.CreateModeButton(BCPPPlugin.Instance.sBasement, false, int.MaxValue, Mode.Main);
                //B1Button.gameObject.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                if (BCPPSave.Instance.basementCompleted)
                {
                    B1Button.AddTextVisual("But_Basement01", out var text);
                    text.fontSize = 24 - 2;
                    BRoutePage.StandardButtonBuilder.AddDescriptionText(B1Button, "Men_Basement01");
                    //B1Button.gameObject.GetComponent<RectTransform>().anchoredPosition = Vector2.left * 190f;
#if !EARLYBIRD
                    var B2Button = BRoutePage.StandardButtonBuilder.CreateModeButton(BCPPPlugin.Instance.sBoss, false, int.MaxValue, Mode.Main);
                    //B2Button.gameObject.GetComponent<RectTransform>().anchoredPosition = Vector2.right * 190f;
                    if (BCPPSave.Instance.death)
                    {
                        B2Button.AddTextVisual("The Other Side", out var text2);
                        text2.fontSize = 24 - 4;
                        BRoutePage.StandardButtonBuilder.AddDescriptionText(B2Button, "Fight against someone's soul to redo the progress!\n<color=red>THIS IS UNPLAYABLE WITH SAVES DISABLED!!");
                        return;
                    }
                    B2Button.OnPress.RemoveAllListeners();
                    B2Button.AddTextVisual("<color=grey>???", out var _text2);
                    B2Button.underlineOnHigh = false;
                    BRoutePage.StandardButtonBuilder.AddDescriptionText(B2Button, "Desc_Suspend");
                    BRoutePage.StandardButtonBuilder.AddTooltipAnimation(B2Button, "BCPP: Foil some intruder's plans.");
#endif
                    return;
                }
                B1Button.OnPress.RemoveAllListeners();
                B1Button.AddTextVisual("<color=grey>???", out var _text);
                B1Button.underlineOnHigh = false;
                BRoutePage.StandardButtonBuilder.AddDescriptionText(B1Button, "Desc_Suspend");
                BRoutePage.StandardButtonBuilder.AddTooltipAnimation(B1Button, "BCPP: Hunt for the button hidden in faculty rooms.");

            };
        }
    }
}
