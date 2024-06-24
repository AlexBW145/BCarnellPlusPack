using BBTimes.CustomContent.Builders;
using BBTimes.CustomContent.Events;
using BBTimes.CustomContent.Objects;
using BBTimes.CustomContent.CustomItems;
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

namespace BCarnellTimes
{
    [BepInPlugin("alexbw145.baldiplus.bcarnelltimes", "B. Carnell Chars + Baldi's Basics Times Compatibility", "1.0.0")]
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
                    if (floorName == "B1" && floorNum == -1 && ld.name == "Basement1")
                    {
                        /*List<Type> floorDats = (List<Type>)AccessTools.Field(TimesManager.GetType(), "floorDatas")
                        .GetValue(null);*/
                        ld.forcedSpecialHallBuilders = [..ld.forcedSpecialHallBuilders, Resources.FindObjectsOfTypeAll<VentBuilder>().First()];
                        ld.potentialItems = [..ld.potentialItems,
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Hammer>()), weight = 35 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Gum>()), weight = 40 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Bell>()), weight = 35 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GoldenQuarter>()), weight = 25 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SpeedPotion>()), weight = 56 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_EmptyWaterBottle>()), weight = 55 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Beartrap>()), weight = 35 }
                        ];
                        ld.shopItems = [..ld.shopItems,
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Hammer>()), weight = 85 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Gum>()), weight = 70 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Bell>()), weight = 75 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GoldenQuarter>()), weight = 65 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_SpeedPotion>()), weight = 56 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_EmptyWaterBottle>()), weight = 55 },
                            new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_Beartrap>()), weight = 35 }];
                        if (!BasePlugin.RadarModExists)
                        {
                            ld.potentialItems = ld.potentialItems.AddToArray(
                                new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GPS>()), weight = 40 });
                            ld.shopItems = ld.shopItems.AddToArray(
                                new() { selection = Resources.FindObjectsOfTypeAll<ItemObject>().First(x => x.item.GetComponent<ITM_GPS>()), weight = 40 });
                        }
                    }
                });
            }, false);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad, true);
        }

        private void PostLoad()
        {
            ERRORBOT.AddLockedInRuleBreak("littering");
            ERRORBOT.AddLockedInRuleBreak("Drinking");

            HowWasTheFall.audOff = (SoundObject)Resources.FindObjectsOfTypeAll<BlackOut>().First().ReflectionGetVariable("audOff");

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
}
