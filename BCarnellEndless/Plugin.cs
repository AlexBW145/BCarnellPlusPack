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
    [BepInDependency("alexbw145.baldiplus.bcarnellchars")]
    [BepInDependency("mtm101.rulerp.baldiplus.endlessfloors")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
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
                    data.npcs.Add(new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value, weight = 90 });
                    data.npcs.Add(new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value, weight = 77 });
                    data.npcs.Add(new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value, weight = 55 });
                    data.npcs.Add(new WeightedNPC { selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value, weight = 55 });
                    data.items.Add(new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"), weight = 60 });
                    data.items.Add(new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"), weight = 60 });
                    data.items.Add(new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"), weight = 10 });
                    data.items.Add(new WeightedItemObject() { selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"), weight = 30 });
                    data.objectBuilders.Add(new WeightedObjectBuilder() { selection = ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor")).value, weight = 85 });
                });
                
            }, true);
        }
    }
}
