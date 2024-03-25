using BBTimes.CreatorHelpers;
using BBTimes.Helpers;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using UnityEngine;

namespace BCarnellTimes
{
    [BepInPlugin("alexbw145.baldiplus.bcarnelltimes", "B. Carnell Chars + Baldi's Basics Times Compatibility", "1.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars")]
    [BepInDependency("pixelguy.pixelmodding.baldiplus.bbextracontent")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnelltimes");
            harmony.PatchAllConditionals();
            LoadingEvents.RegisterOnAssetsLoaded(PostLoad, true);
            GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, ld) =>
            {
                if (floorName == "B1" && floorNum == -1 && ld.name == "Basement1")
                {
                    ld.forcedSpecialHallBuilders = [..ld.forcedSpecialHallBuilders, ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("Vent")).value];
                }
            });
        }

        private void PostLoad()
        {

        }
    }
}
