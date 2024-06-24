using BaldiEndless;
using BCarnellChars;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using UnityEngine;

namespace BCarnellEndless
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellendless", "B. Carnell Chars + Arcade Mode Compatibility", "1.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars", "1.1.0")]
    [BepInDependency("mtm101.rulerp.baldiplus.endlessfloors", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "4.2.0.0")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnellendless");
            harmony.PatchAllConditionals();

            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                EndlessFloorsPlugin.AddGeneratorAction(Info, (data) =>
                {
                    data.npcs.AddRange([
                        new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value, weight = 90 },
                        new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value, weight = 77 },
                        new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value, weight = 55 },
                        new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value, weight = 55 }]);
                    data.items.AddRange([
                        new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"), weight = 60 },
                        new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"), weight = 60 },
                        new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"), weight = 10 },
                        new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"), weight = 30 },
                        new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"), weight = 40 }]);
                    data.objectBuilders.AddRange([
                        new WeightedObjectBuilder() { selection = ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor")).value, weight = 80 },
                        new WeightedObjectBuilder() { selection = BasePlugin.bcppAssets.Get<ObjectBuilder>("ObjectBuilder/RandomItemMachine"), weight = 60 }]);
                });
                
            }, true);
        }
    }
}
