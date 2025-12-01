using BCarnellChars;
using BCarnellChars.OtherStuff;
using BepInEx;
using EndlessFloorsForever;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;

namespace BCarnellEndless
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellendless", "B. Carnell Chars + Arcade Mode Compatibility", "1.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars", "1.5.0")]
    [BepInDependency("alexbw145.baldiplus.arcadeendlessforever", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "8.0.0.0")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        static internal SceneObject Pacman;
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnellendless");
            harmony.PatchAllConditionals();

            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                Pacman = Resources.FindObjectsOfTypeAll<SceneObject>().First(x => x.name == "PacManScene" && x.levelTitle == "PAC");
                EndlessForeverPlugin.AddGeneratorAction(Info, (data) =>
                {
                    data.npcs.AddRange([
                        new WeightedNPC { selection = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/RPS Guy"), weight = 90 },
                        new WeightedNPC { selection = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/ERROR-BOT_ITEM-STEALER"), weight = 77 },
                        new WeightedNPC { selection = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Siege Cannon Cart"), weight = 55 },
                        new WeightedNPC { selection = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Mr Portal Man"), weight = 55 },
                    new WeightedNPC { selection = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Mr Robber"), weight = 5 }]); // Guy with very harsh consequence must not spawn in first floors more often.
                    data.items.AddRange([
                        new WeightedItemObject() { selection = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"), weight = 60 },
                        new WeightedItemObject() { selection = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/BHammer"), weight = 60 },
                        new WeightedItemObject() { selection = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"), weight = 10 },
                        new WeightedItemObject() { selection = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"), weight = 30 },
                        new WeightedItemObject() { selection = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"), weight = 40 },
                        new WeightedItemObject() { selection = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/TASERPulse"), weight = 50 }]);
                    data.potentialObjectBuilders.Add(new WeightedStructureWithParameters()
                    {
                        selection = new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                chance = [.1f],
                                minMax = [new(1, 1)],
                                prefab = [new()
                                {
                                    selection = Resources.FindObjectsOfTypeAll<PAC_Machine>().Last(x => x.name == "PAC-Machine").gameObject,
                                    weight = 75
                                }]
                            },
                            prefab = Resources.FindObjectsOfTypeAll<Structure_EnvironmentObjectPlacer>().ToList().Find(s => s.name == "Structure_EnvironmentObjectBuilder_Individual")
                        },
                        weight = 45
                    });
#if DEBUG
                    data.specialRoomAssets.AddRange([
            new WeightedRoomAsset()
            {
                selection = BasePlugin.bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom1"),
                weight = 70
            },
            new WeightedRoomAsset()
            {
                selection = BasePlugin.bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom2"),
                weight = 70
            },
            new WeightedRoomAsset()
            {
                selection = BasePlugin.bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom3"),
                weight = 60
            }]);
#endif
                });

                }, LoadingEventOrder.Post);
        }
    }

    [HarmonyPatch(typeof(LevelGenerator), nameof(LevelGenerator.StartGenerate))]
    class GeneratePacManEnd
    {
        static void Prefix(LevelGenerator __instance)
        {
            if (__instance.scene == Plugin.Pacman && (bool)EndlessForeverPlugin.Instance.ReflectionGetVariable("gameSave").ReflectionGetVariable("IsInfGamemode"))
                __instance.seedOffset = EndlessForeverPlugin.currentFloorData.FloorID;
        }
    }
}
