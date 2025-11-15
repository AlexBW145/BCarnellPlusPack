using BCarnellChars.OtherStuff;
using BepInEx.Bootstrap;
using HarmonyLib;
using MonoMod.Utils;
using MTM101BaldAPI;
using PlusStudioLevelLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars;

internal static class LevelLoaderAdds
{
    internal static void AddToLoader()
    {
        LevelLoaderPlugin.Instance.npcAliases.AddRange(new Dictionary<string, NPC>()
        {
            { "rpsguy", BasePlugin.bcppAssets.Get<NPC>("NPCs/RPS Guy") },
            { "errorbot", BasePlugin.bcppAssets.Get<NPC>("NPCs/ERROR-BOT_ITEM-STEALER") },
            { "siegecanoncart", BasePlugin.bcppAssets.Get <NPC>("NPCs/Siege Cannon Cart") },
            { "mrportalman", BasePlugin.bcppAssets.Get <NPC>("NPCs/Mr Portal Man") },
            { "mrrobber", BasePlugin.bcppAssets.Get <NPC>("NPCs/Mr Robber") }
        });
        LevelLoaderPlugin.Instance.itemObjects.AddRange(new Dictionary<string, ItemObject>()
        {
            { "profitcard", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard") },
            { "bhammer", BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer") },
            { "unsecuredyellowkey", BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey") },
            { "securedyellowlock", BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock") },
            { "anyportaloutput", BasePlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput") },
            { "taserpulse", BasePlugin.bcppAssets.Get<ItemObject>("Items/TASERPulse") }
        });
        LevelLoaderPlugin.Instance.tileBasedObjectPrefabs.Add("inflocked", BasePlugin.bcppAssets.Get<SecuredSwingingDoor>("Obstacles/SecuredSwingingDoor").GetComponent<SwingDoor>());
        LevelLoaderPlugin.Instance.basicObjects.Add("bcpp_randomitemmachine", BasePlugin.bcppAssets.Get<GameObject>("Structures/RandomItemMachine"));
        LevelLoaderPlugin.Instance.skyboxAliases.Add("pac75", BasePlugin.bcppAssets.Get<Cubemap>("PacSkybox"));
    }

    private static FieldInfo requiredItem = AccessTools.DeclaredField(typeof(SodaMachine), "requiredItem");
    internal readonly static HashSet<string> profitconverted = new HashSet<string>();
    internal static void ConvertMachineToProfitAcceptable(string name, GameObject soda)
    {
        if (LevelLoaderPlugin.Instance.basicObjects.ContainsKey(name + "_profit"))
            throw new Exception($"{name} already is converted to Profit Card acceptable");
        if (!LevelLoaderPlugin.Instance.basicObjects.ContainsKey(name))
            throw new Exception($"{name} does not exist in the level loader!");
        if (soda.GetComponent<SodaMachine>() == null)
            throw new Exception($"{name} is not a SodaMachine component!");
        GameObject sodaProfitCard = GameObject.Instantiate(soda, MTM101BaldiDevAPI.prefabTransform).gameObject;
        sodaProfitCard.name = soda.name + " (ProfitCard)";
        GameObject.Destroy(sodaProfitCard.GetComponent<ToProfitCardCost>());
        requiredItem.SetValue(sodaProfitCard.GetComponent<SodaMachine>(), BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
        sodaProfitCard.GetComponent<MeshRenderer>().materials = sodaProfitCard.GetComponent<MeshRenderer>().GetMaterialArray().AddToArray(BasePlugin.profitCardInsert);
        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // Not again...
            if (sodaProfitCard.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                        .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")) != null)
                AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                .SetValue(sodaProfitCard.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").itemType });
        LevelLoaderPlugin.Instance.basicObjects.Add(name + "_profit", sodaProfitCard);
        profitconverted.Add(name);
        // Bugfix!! The object always appear for some reason after cloning, but it shouldn't do that, right??
        sodaProfitCard.SetActive(false);
        sodaProfitCard.SetActive(true);
    }

    internal static void PlatformsB2Loader()
    {
        LevelLoaderPlugin.Instance.basicObjects.AddRange(new Dictionary<string, GameObject>()
        {
            { "b2platform_sideways", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Sideways") },
            { "b2platform_corner", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Corner") },
            { "b2platform_oneside", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/OneSide") },
            { "b2platform_open", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Open") }
        });
    }
}
