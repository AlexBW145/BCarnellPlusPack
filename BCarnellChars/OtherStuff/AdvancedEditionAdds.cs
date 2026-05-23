using BaldisBasicsPlusAdvanced.API;
using BaldisBasicsPlusAdvanced.Game.Objects.Plates.KitchenStove;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.Linq;

namespace BCarnellChars.OtherStuff;

internal static class AdvancedEditionAdds
{
    internal static void AdvancedAdds()
    {
        var profitCard = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard");
        var quarter = ToProfitCardCost.Quarter;
        var profitCardRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(quarter, quarter)
            .SetCookedFood(profitCard)
            .OverrideCookingTime(4)
            .OverrideCoolingTime(7)
            .RegisterRecipe();
        var quarterRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(profitCard)
            .SetCookedFood(quarter, quarter)
            .OverrideCookingTime(2)
            .OverrideCoolingTime(7)
            .RegisterRecipe();
        var infKey = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey");
        var regularKey = ItemMetaStorage.Instance.FindByEnum(Items.DetentionKey).value;
        var infKeyRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(regularKey, regularKey, regularKey, regularKey)
            .SetCookedFood(infKey)
            .OverrideCookingTime(11)
            .OverrideCoolingTime(11)
            .RegisterRecipe();
        var regularKeyRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(infKey)
            .SetCookedFood(regularKey, regularKey, regularKey, regularKey)
            .OverrideCookingTime(7)
            .OverrideCoolingTime(4)
            .RegisterRecipe();
        var infLock = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock");
        var regularLock = ItemMetaStorage.Instance.FindByEnum(Items.DoorLock).value;
        var infLockRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(regularLock, regularLock, regularLock, regularLock)
            .SetCookedFood(infLock)
            .OverrideCookingTime(11)
            .OverrideCoolingTime(11)
            .RegisterRecipe();
        var regularLockRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(infLock)
            .SetCookedFood(regularLock, regularLock, regularLock, regularLock)
            .OverrideCookingTime(7)
            .OverrideCoolingTime(4)
            .RegisterRecipe();
        var taserPulse = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/TASERPulse");
        var bsoda = ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value;
        var windBlower = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("WindBlower")).value;
        var taserPulseRecipe = new FoodRecipeData(BCPPPlugin.Instance.Info)
            .SetRawFood(bsoda, windBlower)
            .SetCookedFood(taserPulse)
            .RegisterRecipe();
        Dictionary<string, string> localization = new()
        {
            { "Adv_Elv_Tip_BCPP_RPSGuyFact", "RPS Guy has no way of a punishment that will hinder your progress!" },
            { "Adv_Elv_Tip_BCPP_RPSGuyAdvantage", "As long as your rock papers scissors streak is that high, RPS Guy will reward you more YTPs than usual!" },
            { "Adv_Elv_Tip_BCPP_ERRORBOTAdvantage1", "ERROR-BOT_ITEM-STEALER will malfunction if gummed, BSODA'd, balled, or grappled." },
            { "Adv_Elv_Tip_BCPP_ERRORBOTAdvantage2", "ERROR-BOT_ITEM-STEALER will fail a steal if it's squished!" },
            { "Adv_Elv_Tip_BCPP_SiegeCannonCartFact", "Siege Cannon Cart is based on a CoC troop." },
            { "Adv_Elv_Tip_BCPP_SiegeCannonCartAdvantage1", "Siege Cannon Cart is capable of destroying windows!" },
            { "Adv_Elv_Tip_BCPP_SiegeCannonCartAdvantage2", "Siege Cannon Cart's balls can be avoided by many ways, and more situationally the Techno Boots." },
            { "Adv_Elv_Tip_BCPP_SiegeCannonCartAFECLegacyFact", "Siege Cannon Cart was gonna appear in AFEC's Hardmode game mode but the creator lacks imagining to possibility of creating a brand new schoolhouse layout this large plus he was in fact working on a halloween campaign special for the mod." },
            { "Adv_Elv_Tip_BCPP_MrPortalManFact", "Mr. Portal Man will bring in teleportation chaos in your run." },
            { "Adv_Elv_Tip_BCPP_MrPortalManAdv", "If Gotta Sweep is given overtime for its job, then Mr. Portal Man will not be satified with his hunger." },
            { "Adv_Elv_Tip_BCPP_MrPortalManAdvantage", "Prediction is required for Mr. Portal Man, know where you are teleporting to and also be on a lookout for Anyportal Outputs!" },
            { "Adv_Elv_Tip_BCPP_MrRobberFact", "Mr. Robber's creation came from the fact that he is created to be a joke of a Flipnote Studio sample." },
            { "Adv_Elv_Tip_BCPP_MrRobberAdvantage", "You should NEVER EVER be alone when he arrives! Doubting that during after school hours will make it worse!" },
            { "Adv_Elv_Tip_BCPP_ProfitCard", "Profit Cards are an competiting alternative to the infamous shiny quarters! Look out for vending machines that require one!" },
            { "Adv_Elv_Tip_BCPP_BHammer", "Unsafe Hammer lasts for three usages and can break down doors too!" },
            { "Adv_Elv_Tip_BCPP_InfLocks", "Look out for the swinging doors that are locked forever! You'll need an Unsecured Key for unlocking them which this item has infinite uses!" },
            { "Adv_Elv_Tip_BCPP_AnyportalOutput", "Anyportal Outputs can only be placed when Mr. Portal Man is present! Use them for rerouting your escape routes when he is present!" },
            { "Adv_Elv_Tip_BCPP_TASERPulse", "TASER™ Pulse can chain through 3 bad guys and lasts longer than the Diet BSODA!" },
            { "Adv_Elv_Tip_BCPP_RandomMachine", "There's this machine that requires a profit card and is stocked with a random amount of items..." },
            { "Adv_Elv_Tip_BCPP_B1", "A route to the basement requires more preparation than usual!" },
#if !EARLYBIRD
            { "Adv_Elv_Tip_BCPP_B2", "Are you forgetting something?" },
#endif
        };
        AssetLoader.LocalizationFromFunction((lang) => localization);
        ApiManager.AddNewTips(BCPPPlugin.Instance.Info, localization.Keys.ToArray());

        ApiManager.AddNewSymbolMachineWords(BCPPPlugin.Instance.Info,
            "BCMAC",
            "BCPP",
            "RPS",
            "Siege",
            "Rob",
            "TASER",
            "pretz",
            "Nerd",
            "Carnl",
            "monum",
            "expan",
            "RoutB",
            "depth",
            "forgt");
    }
}