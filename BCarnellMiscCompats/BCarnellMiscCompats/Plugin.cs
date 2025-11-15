using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections;
using UnityEngine;

namespace BCarnellChars
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "6.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars", "1.5.2")]
    [BepInDependency("baldi.basics.plus.advanced.mod", BepInDependency.DependencyFlags.SoftDependency)] // Voting Stuff
    [BepInDependency("rost.moment.baldiplus.extramod", BepInDependency.DependencyFlags.SoftDependency)] // Quantum Sweep = Restless Hungry Portal Man
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAllConditionals();
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "alexbw145.baldiplus.bcarnellmisccompats";
        public const string PLUGIN_NAME = "B. Carnell Plus Pack - Misc Compats";
        public const string PLUGIN_VERSION = "1.0";
    }
}
