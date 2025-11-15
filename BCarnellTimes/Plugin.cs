using BBTimes.CustomContent.Builders;
using BBTimes.CustomContent.Events;
using BBTimes.CustomContent.Objects;
using BBTimes.CustomContent.CustomItems;
using BBTimes.CustomContent.NPCs;
using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using BCarnellChars;
using BBTimes.CustomComponents;
using BBTimes.Extensions;
using BBTimes;
using BBTimes.Helpers;

namespace BCarnellTimes;

[BepInPlugin("alexbw145.baldiplus.bcarnelltimes", "B. Carnell Chars + Baldi's Basics Times Compatibility", "1.1.0")]
[BepInDependency("alexbw145.baldiplus.bcarnellchars")]
[BepInDependency("pixelguy.pixelmodding.baldiplus.bbextracontent")]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
[BepInProcess("BALDI.exe")]
public class Plugin : BaseUnityPlugin
{
    //public object TimesManager = AccessTools.TypeByName("BBTimes.Manager.BBTimesManager, " + typeof(BBTimes.Plugin.BasePlugin).Assembly.ToString()); //typeof(BBTimes.Plugin.BasePlugin).Assembly.GetType("BBTimes.Manager.BBTimesManager")
    private void Awake()
    {
        Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnelltimes");
        harmony.PatchAllConditionals();
        LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
        {
            GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, ld) =>
            {
                if (floorName == "B1" && floorNum == -1 && ld.levelObject.name == "Basement1")
                {
                    /*List<Type> floorDats = (List<Type>)AccessTools.Field(TimesManager.GetType(), "floorDatas")
                    .GetValue(null);*/
                    ld.levelObject.forcedStructures = [..ld.levelObject.forcedStructures, new StructureWithParameters()
                    {
                        prefab = Resources.FindObjectsOfTypeAll<Structure_Duct>().First()
                    }];
                    ld.levelObject.potentialItems = [..ld.levelObject.potentialItems,
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Hammer>()), weight = 35 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Gum>()), weight = 40 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Bell>()), weight = 35 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GoldenQuarter>()), weight = 25 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SpeedPotion>()), weight = 56 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_EmptyWaterBottle>()), weight = 55 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Beartrap>()), weight = 35 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_TimesYTP>()), weight = 25 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_DivideYTP>()), weight = 45 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Magnet>()), weight = 25 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_PickupGun>()), weight = 25 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SugarFlavoredZestyBar>()), weight = 70 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SoapBubbles>()), weight = 25 }
                    ];
                    ld.levelObject.shopItems = [..ld.levelObject.shopItems,
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Hammer>()), weight = 85 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Gum>()), weight = 70 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Bell>()), weight = 75 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GoldenQuarter>()), weight = 65 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SpeedPotion>()), weight = 56 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_EmptyWaterBottle>()), weight = 55 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Beartrap>()), weight = 35 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Magnet>()), weight = 55 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_PickupGun>()), weight = 35 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SugarFlavoredZestyBar>()), weight = 55 },
                        new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SoapBubbles>()), weight = 40 }
                    ];
                    if (!BCarnellChars.BasePlugin.RadarModExists)
                    {
                        ld.levelObject.potentialItems = ld.levelObject.potentialItems.AddToArray(
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GPS>()), weight = 40 });
                        ld.levelObject.shopItems = ld.levelObject.shopItems.AddToArray(
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GPS>()), weight = 40 });
                    }
                }
            });
            /*Chainloader.PluginInfos.ToList().Find(p => p.Key == "pixelguy.pixelmodding.baldiplus.bbextracontent").Value.Instance.Config.TryGetEntry<bool>("NPC Settings", "Disable replacement feature", out var replacement);
            if (!replacement.Value)
            {
                BCarnellChars.BasePlugin.bcppAssets.Get<NPC>("NPCs/RPS Guy").gameObject.AddComponent<BCPPCharacterReplacer>().SetupPrefab();
                BCarnellChars.BasePlugin.bcppAssets.Get<NPC>("NPCs/ERROR-BOT_ITEM-STEALER").gameObject.AddComponent<BCPPCharacterReplacer>().SetupPrefab();
                BCarnellChars.BasePlugin.bcppAssets.Get<NPC>("NPCs/Siege Cannon Cart").gameObject.AddComponent<BCPPCharacterReplacer>().SetupPrefab();
                BCarnellChars.BasePlugin.bcppAssets.Get<NPC>("NPCs/Mr Portal Man").gameObject.AddComponent<BCPPCharacterReplacer>().SetupPrefab();
                BCarnellChars.BasePlugin.bcppAssets.Get<NPC>("NPCs/Mr Robber").gameObject.AddComponent<BCPPCharacterReplacer>().SetupPrefab();
            }
            GeneratorManagement.Register(this, GenerationModType.Finalizer, (title, num, scene) =>
            {
                if (!replacement.Value)
                {
                    scene.potentialNPCs.RemoveAll(npc => npc.selection.GetComponent<BCPPCharacterReplacer>() != null);
                    foreach (var npc in BCarnellChars.BasePlugin.bcppAssets.GetAll<NPC>().Where(x => x.GetComponent<BCPPCharacterReplacer>() != null))
                    {
                        var replacer = npc.GetComponent<BCPPCharacterReplacer>();
                        if ((replacer.blacklistFloors && replacer.correctingFloors.Contains(scene.levelTitle))
                        || (!replacer.blacklistFloors && !replacer.correctingFloors.Contains(scene.levelTitle)))
                            continue;
                        else if (scene.potentialNPCs.Exists(x => replacer.ReplacesCharacter(x.selection.Character)))
                        {
                            scene.forcedNpcs = [.. scene.forcedNpcs, npc];
                            continue;
                        }
                    }
                }
            });*/
        }, false);
        LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad, true);
    }

    private void PostLoad()
    {
        ERRORBOT.AddLockedInRuleBreak("littering");
        ERRORBOT.AddLockedInRuleBreak("Drinking");

        //HowWasTheFall.audOff = (SoundObject)Resources.FindObjectsOfTypeAll<BlackOut>().First().ReflectionGetVariable("audOff");


        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_Hammer>()), weight = 80});
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_SpeedPotion>()), weight = 80});
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_WaterBottle>()), weight = 60 });
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_Pogostick>()), weight = 60 });
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_Basketball>()), weight = 90 });
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_Beartrap>()), weight = 55 });
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_HeadachePill>()), weight = 60 });
        RandomItemMachine.ModItems.Add(
            new WeightedItemObject { selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.item.GetComponent<ITM_InvisibilityController>()), weight = 60 });
    }
}

[RequireComponent(typeof(NPC))]
internal class BCPPCharacterReplacer : MonoBehaviour, INPCPrefab
{
    public void SetupPrefabPost() { }
    public void SetupPrefab() {
        Name = gameObject.name;
        switch (Npc = gameObject.GetComponent<NPC>())
        {
            case RPSGuy:
                Npc.MarkAsReplacement(50, Character.Playtime);
                blacklistFloors = true;
                break;
            case ERRORBOT:
                Npc.MarkAsReplacement(33, Character.Bully);
                correctingFloors.Add("F1");
                blacklistFloors = true;
                break;
            case SiegeCanonCart:
                Npc.MarkAsReplacement(50, Character.Prize);
                correctingFloors.Add("F1");
                blacklistFloors = true;
                break;
            case MrPortalMan:
                Npc.MarkAsReplacement(45, Character.Sweep);
                correctingFloors.Add("F1");
                blacklistFloors = true;
                break;
            case MrRobber:
                Npc.MarkAsReplacement(5, Character.Crafters);
                correctingFloors.AddRange(["F1", "F2"]);
                blacklistFloors = true;
                break;
        }
    }
    public string Name { get; set; }
    public string TexturePath => "";
    public string SoundPath => "";
    public NPC Npc { get; set; }
    [SerializeField] Character[] replacementNPCs; public Character[] GetReplacementNPCs() => replacementNPCs; public void SetReplacementNPCs(params Character[] chars) => replacementNPCs = chars;
    public int ReplacementWeight { get; set; }

    public string Category => "";

    internal List<string> correctingFloors = new List<string>();
    internal bool blacklistFloors = false;
}