using BCarnellChars.Characters;
using BCarnellChars.ItemStuff;
using BCarnellChars.OtherStuff;
using BCarnellChars.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.Registers.Buttons;
using MTM101BaldAPI.SaveSystem;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityCipher;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellchars", "B. Carnell's Plus Pack", BCPPSave.Version)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "11.0.0.0")]
    [BepInDependency("mrsasha5.baldi.basics.plus.advanced", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("io.github.uncertainluei.baldiplus.customposters", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("pixelguy.pixelmodding.baldiplus.moddedmodesapi", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.baldiarcade", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("alexbw145.bbplus.rpsguy")] // This is a bad idea...
    [BepInProcess("BALDI.exe")]
    public class BCPPPlugin : BaseUnityPlugin
    {
        public static BCPPPlugin Instance { get; private set; }
        public BCPPSave SaveData { get; private set; } = new BCPPSave();
        public static AssetManager bcppAssets = new AssetManager();
        // Makin' sure that it'll become colorful!
        internal static bool RadarModExists { get; private set; } = false;
        private const string radarID = "org.aestheticalz.baldi.characterradar";

        internal SceneObject sBasement, sBoss, sPac;
        internal LevelObject lBasement, lBoss, lPac;
#if !RELEASE
        internal LevelObject[] lsubBoss = new LevelObject[5];
#endif

        internal static Material profitCardInsert, freeInsert, securedYellowSwingingDoor;

        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnellchars");
            Instance = this;
            harmony.PatchAllConditionals();

            // Custom Posters Mod
            if (Chainloader.PluginInfos.ContainsKey("io.github.uncertainluei.baldiplus.customposters"))
                Chainloader.PluginInfos["io.github.uncertainluei.baldiplus.customposters"].Instance.GetType()
                    .Assembly.GetType("UncertainLuei.BaldiPlus.CustomPosters.CustomPostersPlugin")
                    .InvokeMember("AddBuiltInPackFromMod", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, [this, "Texture2D", "Posters"]);
            // Custom Modes API
            if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.moddedmodesapi"))
                CustomModeBRoute.Init();

            LoadingEvents.RegisterOnAssetsLoaded(Info, StartLoad(), LoadingEventOrder.Start);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad, LoadingEventOrder.Post);
            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                    LevelStudioAdds.ProfitIsEditorTime();
            }, LoadingEventOrder.Final);
            ModdedSaveGame.AddSaveHandler(Info); // I hate it when the same ol' mistakes happen!
            if (!BCPPSave.Build.IsNullOrWhiteSpace())
                MTM101BaldiDevAPI.AddWarningScreen(@$"<color=red>B. Carnell's</color> <color=yellow>Plus Pack</color> v1.6.0 is currently work in progress right now, anything in this new version are not yet finished!
    Current Build: {BCPPSave.Build}", false);
        }

        private IEnumerator StartLoad()
        {
            yield return 2;
            yield return "Loading...";
            string APIMinimumVer = "11.0.0.0";
            if (MTM101BaldiDevAPI.Version < new Version(APIMinimumVer))
            {
                MTM101BaldiDevAPI.CauseCrash(Info, new Exception("BCPP crashed the mod loading screen because the API version is wrong.\n<color=white>Current API Version: " + MTM101BaldiDevAPI.Version.ToString() + "</color>\n<color=yellow>Required API Version: " + APIMinimumVer + " or later</color>"));
                yield break;
            }
            yield return "Loading Assets";
            ToProfitCardCost.Quarter = ItemMetaStorage.Instance.FindByEnum(Items.Quarter).itemObjects.Last(quart => !quart.name.ToLower().Contains("tutorial")); // Tutorial Quarter Takeover
            if (!Directory.Exists(AssetLoader.GetModPath(this)))
            {
                MTM101BaldiDevAPI.CauseCrash(Info, new Exception("BCPP crashed the mod loading screen because the assets are not installed correctly or is non-existent!"));
                yield break;
            }
            AssetLoader.LocalizationFromMod(this);
#if DEBUG
            AssetLoader.LocalizationFromFunction((lang) => new() { 
                { "Opt_BCPPDataLink", "If you have <color=red>B. Carnell's</color> <color=grey>Monumental</color> <color=blue>Alphabet Class</color> installed,\nyou can link it with\n<color=red>B. Carnell's</color> <color=yellow>Plus Pack</color>\nfor special dialogue and interactions\nfrom the game's characters!" } 
            });
#endif
            float errorbodySpriteSize = 100f;
            int siegeSize = 39;
            // Textures
            bcppAssets.AddRange<Texture2D>([
                AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Player.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_RPSGuy.png"),

                AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "CryingPortal.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "CryingPortalMask.png"),

                AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "RPS Guy", "PRI_rpsguy.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "PRI_errorbot.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "PRI_siegecanon.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "PRI_portalman.png"),

                AssetLoader.TextureFromMod(this, "Texture2D", "ProfitCardMachine.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "FreeItemMachine.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "SwingDoor_SecuredLocked_Transparent.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "AnyportalOutput.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "AnyportalOutput_Mask.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "TASERAttack.png"),
#if !RELEASE
                AssetLoader.TextureFromMod(this, "Texture2D", "SchoolTextures", "BlueBrickWallC.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "SchoolTextures", "CarnellCarpet.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "SchoolTextures", "WoodPlanksC.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "SchoolTextures", "PlasticWallC.png"),
#endif
                AssetLoader.TextureFromMod(this, "Texture2D", "SchoolTextures", "PacWall.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "PAC75Machine.png"),
                AssetLoader.TextureFromMod(this, "Texture2D", "PAC75MachineInactive.png"),
                ],
                [
                    "RPSUI/Player",
                    "RPSUI/OpponentGuy",

                    "MrPortalMan/OutputPortal",
                    "MrPortalMan/OutputPortalMask",

                    "PRI/RPSGuy",
                    "PRI/ERRORBOT",
                    "PRI/SiegeCannonCart",
                    "PRI/MrPortalMan",

                    "ProfitCardMachine",
                    "FreeItemMachine",
                    "Obstacles/InfLockedSwingingDoor",
                    "AnyportalOutput",
                    "AnyportalOutputMask",
                    "TaserLineMat",
#if !RELEASE
                    "Boss/BlueBrickWall",
                    "Boss/Carpet",
                    "Boss/WoodPlanks",
                    "Boss/PlasticWall",
#endif
                    "PAC75/Wall",
                    "PAC75/Machine",
                    "PAC75/MachineInactive"
                ]);
            // Sprites
            bcppAssets.AddRange<Sprite>([
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_border.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Rock.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Paper.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Scissors.png"), 1f),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "RPS Guy", "RPSDude.png"), 100f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "RPS Guy", "RPSDead.png"), 100f),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 100f, "Texture2D", "NPCs", "RPS Guy", "RPSDudeH.png"),
                AssetLoader.SpriteFromMod(this,Vector2.one / 2f, 100f, "Texture2D", "NPCs", "RPS Guy", "RPSDeadH.png"),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-1.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-2.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-3.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-4.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-5.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-6.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-7.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-8.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-9.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-10.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-11.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-12.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-13.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-14.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-15.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-16.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-17.png"), errorbodySpriteSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botHead.png"), errorbodySpriteSize-55f),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeFront.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeLeftFront.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeLeft.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeLeftBehind.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeBehind.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeRightBehind.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeRight.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeRightFront.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "StupidBall.png"), 48f),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "PortalMan.png"), 34f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "PortalManMask.png"), 34f),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Placehold", "StaticIdle.png"), 49f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Placehold", "Jumpscare.png"), 1f),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Hammer_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Hammer_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "ProfitCard_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "ProfitCard_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorKey_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorKey_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorInfLock_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorInfLock_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Anyportal_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Anyportal_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "TASERPulse_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "TASERPulse_Large.png"), 50f),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "Items", "AnyportalMan_Small.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "Texture2D", "Items", "AnyportalMan_Large.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "Texture2D", "Items", "SecuredLockPacket.png"),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "randommachine_bcmac.png"), 25f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "randommachine_out_bcmac.png"), 25f),

                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "Stickers", "Switcherquartprofit.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "Stickers", "RockPaperScissorsSticker.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "Stickers", "ERBOTSticker.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "Stickers", "PortalManSticker.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "Stickers", "StickerPacketSecured.png"),
                ],
                [
                    "RPSUI/Border",
                    "RPSUI/Rock",
                    "RPSUI/Paper",
                    "RPSUI/Scissors",

                    "RPSGuy/Alive",
                    "RPSGuy/Dead",
                    "RPSGuyH/Alive",
                    "RPSGuyH/Dead",

                    "ERRORBOT/Body1",
                    "ERRORBOT/Body2",
                    "ERRORBOT/Body3",
                    "ERRORBOT/Body4",
                    "ERRORBOT/Body5",
                    "ERRORBOT/Body6",
                    "ERRORBOT/Body7",
                    "ERRORBOT/Body8",
                    "ERRORBOT/Body9",
                    "ERRORBOT/Body10",
                    "ERRORBOT/Body11",
                    "ERRORBOT/Body12",
                    "ERRORBOT/Body13",
                    "ERRORBOT/Body14",
                    "ERRORBOT/Body15",
                    "ERRORBOT/Body16",
                    "ERRORBOT/Body17",
                    "ERRORBOT/Head",

                    "SiegeCanonCart/IdleFront",
                    "SiegeCanonCart/IdleLeftFront",
                    "SiegeCanonCart/IdleLeft",
                    "SiegeCanonCart/IdleLeftBehind",
                    "SiegeCanonCart/IdleBehind",
                    "SiegeCanonCart/IdleRightBehind",
                    "SiegeCanonCart/IdleRight",
                    "SiegeCanonCart/IdleRightFront",
                    "SiegeCanonCart/Ball",

                    "MrPortalMan/PortalMan",
                    "MrPortalMan/PortalManMask",

                    "PrototypeBot-01/Idle",
                    "PrototypeBot-01/Jumpscare",

                    "Items/BHammer_Small",
                    "Items/BHammer_Large",
                    "Items/ProfitCard_Small",
                    "Items/ProfitCard_Large",
                    "Items/UnsecuredKey_Small",
                    "Items/UnsecuredKey_Large",
                    "Items/SecuredLock_Small",
                    "Items/SecuredLock_Large",
                    "Items/Anyportal_Small",
                    "Items/Anyportal_Large",
                    "Items/TASERPulse_Small",
                    "Items/TASERPulse_Large",
                    "Items/AnyportalMan_Small",
                    "Items/AnyportalMan_Large",
                    "Items/SecuredPacket",

                    "Structures/RandomMachine",
                    "Structures/RandomMachine_OutOfGoods",

                    "Stickers/Switcherquartprofit",
                    "Stickers/RockPaperScissorsSticker",
                    "Stickers/ERBOTSticker",
                    "Stickers/PortalManSticker",
                    "Stickers/StickerPacketSecured"
                ]);
            for (int frame = 1; frame <= 7; frame++) // Moving Frames
            {
                bcppAssets.AddRange<Sprite>([
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "front", "front_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "leftfront", "leftfront_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "left", "left_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "leftbehind", "leftbehind_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "behind", "behind_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "rightbehind", "rightbehind_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "right", "right_Moving (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "rightfront", "rightfront_Moving (" + frame + ").png"), siegeSize)
                    ],
                    [
                    "SiegeCanonCart/MovingFront_"+frame,
                    "SiegeCanonCart/MovingLeftFront_"+frame,
                    "SiegeCanonCart/MovingLeft_"+frame,
                    "SiegeCanonCart/MovingLeftBehind_"+frame,
                    "SiegeCanonCart/MovingBehind_"+frame,
                    "SiegeCanonCart/MovingRightBehind_"+frame,
                    "SiegeCanonCart/MovingRight_"+frame,
                    "SiegeCanonCart/MovingRightFront_"+frame,
                    ]);
            }
            // Wait, I fucked up my code.
            for (int frame = 1; frame <= 8; frame++) // Shooting Frames
            {
                bcppAssets.AddRange<Sprite>([
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "front", "front_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "leftfront", "leftfront_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "left", "left_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "leftbehind", "leftbehind_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "behind", "behind_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "rightbehind", "rightbehind_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "right", "right_Shoot (" + frame + ").png"), siegeSize),
                    AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "rightfront", "rightfront_Shoot (" + frame + ").png"), siegeSize)
                    ],
                    [
                    "SiegeCanonCart/ShootingFront_"+frame,
                    "SiegeCanonCart/ShootingLeftFront_"+frame,
                    "SiegeCanonCart/ShootingLeft_"+frame,
                    "SiegeCanonCart/ShootingLeftBehind_"+frame,
                    "SiegeCanonCart/ShootingBehind_"+frame,
                    "SiegeCanonCart/ShootingRightBehind_"+frame,
                    "SiegeCanonCart/ShootingRight_"+frame,
                    "SiegeCanonCart/ShootingRightFront_"+frame,
                    ]);
            }
            for (int frame = 0; frame <= 10; frame++) // Jumpscare 1
                bcppAssets.Add<Sprite>("PrototypeBot-01/Jumpscare01_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Jumpscares", "Jumpscare_1" + frame + ".png"), 1f));
            for (int frame = 0; frame <= 13; frame++) // Jumpscare 2
                bcppAssets.Add<Sprite>("PrototypeBot-01/Jumpscare02_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Jumpscares", "Jumpscare_2" + frame + ".png"), 1f));
            float protoSize = 9f;
            for (int frame = 0; frame <= 11; frame++) // Idle
                bcppAssets.Add<Sprite>("PrototypeBot-01/Idle_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Idle", "Idle" + frame + ".png"), protoSize));
            for (int frame = 0; frame <= 7; frame++) // Running
                bcppAssets.Add<Sprite>("PrototypeBot-01/Running_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Running", "Running" + frame + ".png"), protoSize));
            for (int frame = 0; frame <= 17; frame++) // Climbing
                bcppAssets.Add<Sprite>("PrototypeBot-01/Climbing_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "Climbing", "Climbing" + frame + ".png"), protoSize));
            for (int frame = 0; frame <= 11; frame++) // ClimbOut
                bcppAssets.Add<Sprite>("PrototypeBot-01/ClimbOut_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "PrototypeBot-01", "ClimbOut", "ClimbOut" + frame + ".png"), protoSize));

            bcppAssets.AddRange<Sprite>([
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Robber", "Robber.png"), 190f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Robber", "RobberTaxEvasion.png"), 190f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "Icon_Pellet.png"), 16f * 2f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI","Icon_Pellet.png"), 16f)
                ],
                [
                "MrRobber/Idle",
                "MrRobber/Targeting",
                "PAC75/UIPellett",
                "PAC75/UIPowerPellett",
                ]);
            bcppAssets.Add<Cubemap>("PacSkybox", AssetLoader.CubemapFromMod(this, "Texture2D", "PACVirtual.png"));
#if !RELEASE
            // This seems better to use...
            float bcarnellSize = 80f;
            bcppAssets.Add("BCarnellBoss/Walk", AssetLoader.SpritesFromSpritesheet(12, 1, bcarnellSize, Vector2.one / 2f, AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Real B. Carnell", "RealBWalk.png")));
            bcppAssets.Add("BCarnellBoss/SawShoot", AssetLoader.SpritesFromSpritesheet(7, 1, bcarnellSize, Vector2.one / 2f, AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Real B. Carnell", "RealBShoot.png")));
            bcppAssets.Add("BCarnellBoss/BoostPre", AssetLoader.SpritesFromSpritesheet(5, 1, bcarnellSize, Vector2.one / 2f, AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Real B. Carnell", "RealBBoostPre.png")));
            bcppAssets.Add("BCarnellBoss/Boosting", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, bcarnellSize, "Texture2D", "NPCs", "Real B. Carnell", "RealBBoosting.png"));
#endif
            // SoundObjects
            bcppAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 15.wav"), "Nothing", SoundType.Effect, Color.white, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 7.wav"), "Nothing", SoundType.Effect, Color.white, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 14.wav"), "Nothing", SoundType.Effect, Color.white, 0f),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_lose.wav"), "Vfx_RPS_lost", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_win.wav"), "Vfx_RPS_win", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_lose2.wav"), "Vfx_RPS_lost2", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_lostsight.wav"), "Vfx_RPS_lostsight", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_play.wav"), "Vfx_RPS_play", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_dies.wav"), "Sfx_AppleCrunch", SoundType.Effect, Color.white),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "mus_rps.wav"), "Mfx_mus_Playtime", SoundType.Music, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "ERB_static.wav"), "Sfx_Crafters_Loop", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Siege Canon Cart", "WagonMovement.wav"), "Sfx_SiegeCart_Wagon", SoundType.Effect, new Color(1f, 0.6470588f, 0.1529412f)),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "ERB_alarm.wav"), "SFX_Items_AntiHearing", SoundType.Effect, new Color(0.8f, 0.1647059f, 0.2f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "ERB_minenow.wav"), "Vfx_ERRORBOT_MineNow", SoundType.Voice, new Color(0.8f, 0.1647059f, 0.2f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "ERB_byetoneeds.wav"), "Vfx_ERRORBOT_ByeToNeeds", SoundType.Voice, new Color(0.8f, 0.1647059f, 0.2f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "ERB_scanforwants.wav"), "Vfx_ERRORBOT_ScanForWants", SoundType.Voice, new Color(0.8f, 0.1647059f, 0.2f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "grappleImpact.wav"), "Sfx_Bang", SoundType.Effect, new Color(0.8f, 0.1647059f, 0.2f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "bsodaImpact.wav"), "Sfx_Items_NoSquee", SoundType.Effect, new Color(0.8f, 0.1647059f, 0.2f)),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Portal Man", "PTM_Intro.wav"), "Vfx_MrPortalMan_Intro", SoundType.Voice, new Color(1f, 0.6470588f, 0.1529412f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Portal Man", "PTM_Questioning.wav"), "Vfx_MrPortalMan_WhereIs", SoundType.Voice, new Color(1f, 0.6470588f, 0.1529412f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Portal Man", "PTM_End.wav"), "Vfx_MrPortalMan_Fulfilled", SoundType.Voice, new Color(1f, 0.6470588f, 0.1529412f)),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_BlastingGrenades.wav"), "Vfx_MrRobber_BlastingGreenades1", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_Notebooks.wav"), "Vfx_MrRobber_Notebooks", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_Worse.wav"), "Vfx_MrRobber_Worse1", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_Shine.wav"), "Vfx_MrRobber_Shine", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_TimeToFlee.wav"), "Vfx_MrRobber_Flee", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_WasNeverHere.wav"), "Vfx_MrRobber_WasNeverHere", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_RegretAlone.wav"), "Vfx_MrRobber_RegretAlone", SoundType.Voice, Color.grey),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_beat.wav"), "Sfx_MrRobber_Beat", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_essence.wav"), "Sfx_MrRobber_HiddenBeat", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_screechIntro.wav"), "Sfx_MrRobber_Screech", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Mr. Robber", "ROBR_screechLoop.wav"), "Sfx_MrRobber_ScreechLoop", SoundType.Effect, Color.white),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_Intro.wav"), "Nothing", SoundType.Effect, Color.clear, 0),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_IntroLoop.wav"), "Nothing", SoundType.Effect, Color.clear, 0),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_TimeOver.wav"), "Nothing", SoundType.Effect, Color.clear, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_TimeOverLoop.wav"), "Nothing", SoundType.Effect, Color.clear, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill1.wav"), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill2.wav"), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill3.wav"), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill4.wav"), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_FastRun.wav"), "Sfx_CartoonKnock_Trimmed", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_Waiting.wav"), "Sfx_Lose_Buzz", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_AntiHearStun.wav"), "Sfx_ShrinkMachine_Door", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_Ceiling.wav"), "Sfx_Lose_Buzz", SoundType.Effect, Color.white),
#if !RELEASE
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBOSS_boostCharge.wav"), "Sfx_BCBoss_BoostCharge", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBOSS_boostGo.wav"), "Sfx_BCBoss_BoostGo", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBOSS_sawblade.wav"), "Sfx_ShrinkMachine_Door", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBOSS_sawshoot.wav"), "Sfx_BCBoss_SawShoot", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBOSS_Slap.wav"), "Sfx_BCarnell_Walk", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "LOSEBC_bomb.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "LOSEBC_screech.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "LOSEBC_standard.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "LOSEBC_yello.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_caughtlayer.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f), 0f),
#endif
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "door_break.wav"), "Vfx_Prize_Bang", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "ITM_UnsafeHammerBreak.wav"), "Sfx_Bang", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "ITM_AnyportalOutputSpawning.wav"), "Sfx_Slap", SoundType.Effect, new Color(0.07058824f, 0.6431373f, 1f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "ITM_AnyportalOutputDestroyed.wav"), "Sfx_GlassBreak", SoundType.Effect, new Color(0.07058824f, 0.6431373f, 1f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "ITM_TaserAttack.wav"), "Sfx_TASERAttack", SoundType.Effect, Color.white, 0.336f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "ITM_TaserCaught.wav"), "Sfx_TASERCaught", SoundType.Effect, Color.white, 0.97f)
                ],
                [
                    "RPSUI/HitTie",
                    "RPSUI/HitWin",
                    "RPSUI/HitLose",

                    "RPSGuy/LostGame",
                    "RPSGuy/WonGame",
                    "RPSGuy/LostGameQuote2",
                    "RPSGuy/LostSightOfPlayer",
                    "RPSGuy/WannaPlay",
                    "RPSGuy/Killed",

                    "RPSGuy/AmbientMusic",
                    "ERRORBOT/AmbientStatic",
                    "SiegeCanonCart/Paddle",

                    "ERRORBOT/Alarm",
                    "ERRORBOT/MineNow",
                    "ERRORBOT/ByeToNeeds",
                    "ERRORBOT/Scanning",
                    "ERRORBOT/Jammed",
                    "ERRORBOT/Sprayed",

                    "MrPortalMan/Hungry",
                    "MrPortalMan/WhereIs",
                    "MrPortalMan/Fullfilled",

                    "MrRobber/BlastingGreenades",
                    "MrRobber/Notebooks",
                    "MrRobber/Worse",
                    "MrRobber/Shine",
                    "MrRobber/Flee",
                    "MrRobber/WasNeverHere",
                    "MrRobber/RegretAlone",
                    "MrRobber/AnnoyingBeat",
                    "MrRobber/EssenceBeat",
                    "MrRobber/ScreechIntro",
                    "MrRobber/ScreechLoop",

                    "PrototypeBot-01/AmbientIntro",
                    "PrototypeBot-01/AmbientLoop",
                    "PrototypeBot-01/TimeOut",
                    "PrototypeBot-01/TimeOutLoop",
                    "PrototypeBot-01/Jumpscare_01",
                    "PrototypeBot-01/Jumpscare_02",
                    "PrototypeBot-01/Jumpscare_03",
                    "PrototypeBot-01/Jumpscare_04",
                    "PrototypeBot-01/FastStomp",
                    "PrototypeBot-01/Waiting",
                    "PrototypeBot-01/Stunned",
                    "PrototypeBot-01/Ceiling",
#if !RELEASE
                    "BCarnellBoss/BoostCharge",
                    "BCarnellBoss/BoostGo",
                    "BCarnellBoss/Sawblade",
                    "BCarnellBoss/Sawshoot",
                    "BCarnellBoss/Step",
                    "BCarnellBoss/LOSE/Bomb",
                    "BCarnellBoss/LOSE/Screech",
                    "BCarnellBoss/LOSE/Standard",
                    "BCarnellBoss/LOSE/Yello",
                    "BCarnellBoss/LOSE/caughtlayer",
#endif
                    "Items/UnsafeHammer/DestroyDoor",
                    "Items/UnsafeHammer/DurationBreak",
                    "Items/AnyportalOutput/Spawning",
                    "Items/AnyportalOutput/Destroyed",
                    "Items/TASERPulse/Use",
                    "Items/TASERPulse/Effect"
                ]);
            bcppAssets.Get<SoundObject>("ERRORBOT/Jammed").additionalKeys = [new SubtitleTimedKey() { key = "Sfx_ERRORBOT_Malfunction", time = 0.34f }];
            bcppAssets.Get<SoundObject>("ERRORBOT/Sprayed").additionalKeys = [new SubtitleTimedKey() { key = "Sfx_ERRORBOT_Malfunction", time = 0.34f }];
            bcppAssets.Get<SoundObject>("MrRobber/BlastingGreenades").additionalKeys = [new SubtitleTimedKey() { key = "Vfx_MrRobber_BlastingGreenades2", time = 2.625f }];
            bcppAssets.Get<SoundObject>("MrRobber/Worse").additionalKeys = [new SubtitleTimedKey() { key = "Vfx_MrRobber_Worse2", time = 3.142f }];
            bcppAssets.Get<SoundObject>("MrRobber/ScreechIntro").additionalKeys = [new SubtitleTimedKey() { key = "Sfx_MrRobber_ScreechLoop", time = 1.399f }];
            bcppAssets.Get<SoundObject>("PrototypeBot-01/FastStomp").volumeMultiplier = 2f;

            bcppAssets.Get<SoundObject>("Items/AnyportalOutput/Spawning").additionalKeys = [new SubtitleTimedKey() { key = "Sfx_AnyportalOutput_Spawning", time = 0.5120181f },
            new SubtitleTimedKey() { key = "Sfx_BellGeneric", time = 6.21f }];
#if !RELEASE
            Texture2D LoadTextureFromResources(string name)
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BCarnellChars.bcarnellpluspack.hiddenassets." + name);
                using MemoryStream memoryStream = new();

                stream.CopyTo(memoryStream);
                byte[] bytes = memoryStream.ToArray();

                Texture2D texture2D = new(2, 2, TextureFormat.RGBA32, mipChain: false);
                texture2D.LoadImage(bytes);
                texture2D.filterMode = FilterMode.Point;
                texture2D.name = Path.GetFileNameWithoutExtension("Ref_" + name);
                return texture2D;
            }
            AudioClip LoadAudioFromResources(string name)
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BCarnellChars.bcarnellpluspack.hiddenassets." + name);

                string clippath = Path.Combine(Application.temporaryCachePath, name);
                File.WriteAllBytes(clippath, stream.ToByteArray());
                var audioclip = AssetLoader.AudioClipFromFile(clippath);
                File.Delete(clippath);
                stream.Dispose();
                return audioclip;
            }

            // Boss music
            bcppAssets.Add<string>("mus_BCarnellBossIntro", AssetLoader.MidiFromMod("mus_RealBossIntro", this, "MIDI", "mus_RealBossIntro.mid"));
            bcppAssets.Add<string>("mus_BCarnellBoss01", AssetLoader.MidiFromMod("mus_RealBossFight01", this, "MIDI", "mus_RealBossFight01.mid"));

            // Dogcheck Music
            DogcheckBCPP.music = LoadAudioFromResources("AudioClip.DogcheckBCPP.wav");

            // Boss Meshes
            var b2model = AssetLoader.ModelFromMod(this, "Mesh", "B2PlatformSideways.obj");
            bcppAssets.Add<Mesh>("B2Platform/Sideways", b2model.GetComponentInChildren<MeshFilter>().mesh);
            Destroy(b2model);
            b2model = AssetLoader.ModelFromMod(this, "Mesh", "B2PlatformTurn.obj");
            bcppAssets.Add<Mesh>("B2Platform/Corner", b2model.GetComponentInChildren<MeshFilter>().mesh);
            Destroy(b2model);
            b2model = AssetLoader.ModelFromMod(this, "Mesh", "B2PlatformThreeWay.obj");
            bcppAssets.Add<Mesh>("B2Platform/OneSide", b2model.GetComponentInChildren<MeshFilter>().mesh);
            Destroy(b2model);
            b2model = AssetLoader.ModelFromMod(this, "Mesh", "B2Platform.obj");
            bcppAssets.Add<Mesh>("B2Platform/Open", b2model.GetComponentInChildren<MeshFilter>().mesh);
            Destroy(b2model);
#endif
            // PAC MAN
            bcppAssets.Add("PAC75/MachineModel", AssetLoader.ModelFromMod(this, "PAC75Machine.obj"));
            bcppAssets.Add<SoundObject>("mus_PAC75", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "mus_PAC75.wav"), "Nothing", SoundType.Music, Color.clear, 0));
            bcppAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-waka.wav"), "Nothing", SoundType.Effect, Color.clear, 0),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-superPill.wav"), "Nothing", SoundType.Effect, Color.clear, 0),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-eatGhost.wav"), "Nothing", SoundType.Effect, Color.clear, 0),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-win.wav"), "Nothing", SoundType.Effect, Color.clear, 0),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-ghostLoop.wav"), "Sfx_PAC75Ghost_Wandering", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-ghostblueLoop.wav"), "Sfx_PAC75Ghost_Weakened", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PAC-retreat.wav"), "Sfx_PAC75Ghost_Fleeing", SoundType.Effect, Color.white)],
                [
                "PAC75/Waka",
                "PAC75/WakaSuper",
                "PAC75/WakaGhost",
                "PAC75/Victory",
                "PAC75Ghost/Wandering",
                "PAC75Ghost/Weakened",
                "PAC75Ghost/Fleeing"]);
#if false
            GameObject notfinalnotif = Instantiate(Resources.FindObjectsOfTypeAll<Jumprope>().Last(s => s.name == "Jumprope").gameObject, MTM101BaldiDevAPI.prefabTransform);
            notfinalnotif.name = "BCPP_NotFinalNotif";
            DestroyImmediate(notfinalnotif.GetComponent<Jumprope>());
            DestroyImmediate(notfinalnotif.transform.Find("RopeCanvas").gameObject);
            DestroyImmediate(notfinalnotif.transform.Find("TextCanvas").transform.Find("Count").gameObject);
            var text = notfinalnotif.transform.Find("TextCanvas").gameObject.GetComponentInChildren<TMP_Text>(true);
            text.text = "<color=red>BC</color><color=yellow>PP</color> v1.6.0 Early Access";
            text.alignment = TextAlignmentOptions.Bottom;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = Vector2.right / 2f;
            text.color = Color.white;
            text.alpha = 0.55f;
            text.font = BaldiFonts.ComicSans36.FontAsset();
            text.fontSize = BaldiFonts.ComicSans36.FontSize();
            notfinalnotif.SetActive(false);
            notfinalnotif.transform.SetParent(null);
            DontDestroyOnLoad(notfinalnotif);

            SceneManager.activeSceneChanged += (current, next) =>
            {
                if (next.name == "Game")
                    notfinalnotif.SetActive(true);
                else if (current.name == "Game")
                    notfinalnotif.SetActive(false);
                notfinalnotif.transform.SetAsLastSibling();
            };
#endif
        }

        private IEnumerator PreLoad()
        {
            yield return 24;
            yield return "Preparing...";
            RadarModExists = Chainloader.PluginInfos.ContainsKey(radarID);
            if (RadarModExists)
            {
                ConfigFile radarConfig = Chainloader.PluginInfos.Last(p => p.Key == radarID).Value.Instance.Config;
                RadarSupport.RPSGuyColor = radarConfig.Bind("CharacterColors", "RPSGuyColor", new Color(0.7176471f, 0.6941177f, 0.6235294f), "The color of RPS Guy's arrow.");
                RadarSupport.ERRORBOTColor = radarConfig.Bind("CharacterColors", "ERRORBOTColor", new Color(0.8f, 0.1647059f, 0.2f), "The color of ERROR-BOT_ITEM-STEALER's arrow.");
                RadarSupport.SiegeCartColor = radarConfig.Bind("CharacterColors", "SiegeCartColor", new Color(0.4196078f, 0.2901961f, 0.2117647f), "The color of Siege Canon Cart's arrow.");
                RadarSupport.MrPortalManColor = radarConfig.Bind("CharacterColors", "MrPortalManColor", new Color(1f, 0.6470588f, 0.1529412f), "The color of Mr. Portal Man's arrow.");

                if (RadarSupport.RPSGuyColor.Value == Color.clear)
                    Debug.LogError("RPSGuyColor is (0, 0, 0, 0). The map arrow will be fully clear.");
                if (RadarSupport.ERRORBOTColor.Value == Color.clear)
                    Debug.LogError("ERRORBOTColor is (0, 0, 0, 0). The map arrow will be fully clear.");
                if (RadarSupport.SiegeCartColor.Value == Color.clear)
                    Debug.LogError("SiegeCartColor is (0, 0, 0, 0). The map arrow will be fully clear.");
                if (RadarSupport.MrPortalManColor.Value == Color.clear)
                    Debug.LogError("MrPortalManColor is (0, 0, 0, 0). The map arrow will be fully clear.");
            }
#if !RELEASE
            // B2 Ending
            /*bcppAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "dial_underscore.wav"), "_Talk", SoundType.Voice, Color.white, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "dial_angryunderscore.wav"), "_Talk", SoundType.Voice, Color.white, 0f),
                ],
                [
                    "_/Talk",
                    "_/AngryTalk"
                    ]);
            var loop = ScriptableObject.CreateInstance<LoopingSoundObject>();
            loop.clips = [AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "01.wav")];
            loop.mixer = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().Last(x => x.name.ToLower() == "Music".ToLower());
            bcppAssets.Add<LoopingSoundObject>("_", loop);*/
#endif
            BinaryReader reader;
            var PacModel = bcppAssets.Get<GameObject>("PAC75/MachineModel");
            bcppAssets.Remove<GameObject>("PAC75/MachineModel");

            yield return "Initializing RPS Guy";
            FieldInfo 
                _audOnStart = AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart"),
                _loopAudOnStart = AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart");
            // do dis
            var rpsguy = new NPCBuilder<RPSGuy>(Info)
                .SetName("RPS Guy")
                .SetEnum("RPSGuy")
                .AddLooker()
                .AddTrigger()
                .SetMaxSightDistance(99f)
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .AddSpawnableRoomCategories(RoomCategory.Class, RoomCategory.Office, RoomCategory.Special)
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/RPSGuy"), []))
                .SetMetaTags(["BCPP", "student"])
                .Build(); //ObjectCreators.CreateNPC<RPSGuy>("RPS Guy", EnumExtensions.ExtendEnum<Character>("RPSGuy"), ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/RPSGuy"), []), spawnableRooms: [RoomCategory.Class, RoomCategory.Office, RoomCategory.Special]);
            //AccessTools.DeclaredField(typeof(Navigator), "avoidRooms").SetValue(rpsguy.Navigator, true);
            rpsguy.Navigator.SetRoomAvoidance(true);
            rpsguy.spriteRenderer[0].transform.localPosition = new Vector3(0f, -2.3f, 0);
            //AccessTools.DeclaredField(typeof(Looker), "distance").SetValue(rpsguy.looker, 99f);
            PropagatedAudioManager annoying = rpsguy.gameObject.AddComponent<PropagatedAudioManager>();
            annoying.audioDevice = rpsguy.gameObject.AddComponent<AudioSource>();
            _audOnStart.SetValue(annoying, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("RPSGuy/AmbientMusic")
            });
            _loopAudOnStart.SetValue(annoying, true);
            annoying.audioDevice.spatialBlend = 1;
            annoying.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            annoying.audioDevice.maxDistance = 150;
            annoying.audioDevice.dopplerLevel = 0;
            annoying.audioDevice.spread = 0;
            yield return "Initializing RPSUI";
            GameObject rockpaperscissors = Instantiate(Resources.FindObjectsOfTypeAll<Jumprope>().Last(s => s.name == "Jumprope").gameObject, MTM101BaldiDevAPI.prefabTransform);
            rockpaperscissors.name = "Rock Paper Scissors";
            Destroy(rockpaperscissors.GetComponent<Jumprope>());
            Destroy(rockpaperscissors.transform.Find("RopeCanvas").gameObject);
            Destroy(rockpaperscissors.transform.Find("TextCanvas").transform.Find("Count").gameObject);
            RawImage raw = rockpaperscissors.transform.Find("TextCanvas").gameObject.AddComponent<RawImage>();
            raw.color = new Color(0f, 0f, 0f, 0.50f); // Initial Color
            Image uhf = new GameObject("Border", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            uhf.gameObject.layer = LayerMask.NameToLayer("UI");
            uhf.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform, false);
            uhf.transform.localScale = Vector3.one;
            uhf.sprite = bcppAssets.Get<Sprite>("RPSUI/Border");
            uhf.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            uhf.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            uhf.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            uhf.rectTransform.anchoredPosition = Vector2.zero;
            uhf.transform.SetAsFirstSibling(); // IT COVERS, SHIT IT INTO THE FIRST SIBLING!!
            uhf.rectTransform.sizeDelta += Vector2.up * 40f;
            GameObject me = new GameObject("Player", typeof(RawImage));
            me.layer = LayerMask.NameToLayer("UI");
            me.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            me.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            me.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            me.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            me.transform.localScale = Vector3.one * 0.5f;
            me.GetComponent<RectTransform>().anchoredPosition = new Vector3(40, 0, 0); // Pro tip: anchoredPosition helps.
            me.GetComponent<RawImage>().texture = bcppAssets.Get<Texture2D>("RPSUI/Player");
            GameObject them = new GameObject("Opponent", typeof(RawImage));
            them.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            them.layer = LayerMask.NameToLayer("UI");
            them.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0.5f);
            them.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
            them.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            them.transform.localScale = Vector3.one * 0.5f;
            them.GetComponent<RectTransform>().anchoredPosition = new Vector3(-40, 0, 0); // Pro tip: anchoredPosition helps.
            them.GetComponent<RawImage>().texture = bcppAssets.Get<Texture2D>("RPSUI/OpponentGuy");
            GameObject iChose = new GameObject("Player_Chose", typeof(Image));
            iChose.layer = LayerMask.NameToLayer("UI");
            iChose.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            iChose.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            iChose.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            iChose.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            iChose.transform.localScale = Vector3.one * 0.8f;
            iChose.GetComponent<RectTransform>().anchoredPosition = new Vector3(110, 0, 0); // Pro tip: anchoredPosition helps.
            iChose.GetComponent<Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().Last(x => x.name == "Transparent");
            GameObject theyChoose = new GameObject("Opponent_Chose", typeof(Image));
            theyChoose.layer = LayerMask.NameToLayer("UI");
            theyChoose.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            theyChoose.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0.5f);
            theyChoose.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
            theyChoose.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            theyChoose.transform.localScale = Vector3.one * 0.8f;
            theyChoose.GetComponent<RectTransform>().anchoredPosition = new Vector3(-110, 0, 0); // Pro tip: anchoredPosition helps.
            theyChoose.GetComponent<Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().Last(x => x.name == "Transparent");
            rockpaperscissors.transform.Find("TextCanvas").gameObject.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Center;
            yield return "Finalizing RPS Guy";
            rpsguy.rpsPre = rockpaperscissors.AddComponent<RockPaperScissors>();
            rpsguy.rpsPre.hitSounds.AddRange([
                bcppAssets.Get<SoundObject>("RPSUI/HitTie"),
                bcppAssets.Get<SoundObject>("RPSUI/HitWin"),
                bcppAssets.Get<SoundObject>("RPSUI/HitLose")
                ]);
            rpsguy.rpsPre.chosenSprites.AddRange([
                bcppAssets.Get<Sprite>("RPSUI/Rock"),
                bcppAssets.Get<Sprite>("RPSUI/Paper"),
                bcppAssets.Get<Sprite>("RPSUI/Scissors")
            ]);

            rpsguy.audMan = rpsguy.gameObject.GetComponents<PropagatedAudioManager>()[0];
            rpsguy.alive = bcppAssets.Get<Sprite>("RPSGuy/Alive");
            rpsguy.dead = bcppAssets.Get<Sprite>("RPSGuy/Dead");
            rpsguy.aliveH = bcppAssets.Get<Sprite>("RPSGuyH/Alive");
            rpsguy.deadH = bcppAssets.Get<Sprite>("RPSGuyH/Dead");

            rpsguy.audCongrats = [bcppAssets.Get<SoundObject>("RPSGuy/LostGame"), bcppAssets.Get<SoundObject>("RPSGuy/LostGameQuote2")];
            rpsguy.audYouLose = bcppAssets.Get<SoundObject>("RPSGuy/WonGame");
            rpsguy.audDestroyed = bcppAssets.Get<SoundObject>("RPSGuy/Killed");
            rpsguy.audWannaPlay = bcppAssets.Get<SoundObject>("RPSGuy/WannaPlay");
            rpsguy.audFine = bcppAssets.Get<SoundObject>("RPSGuy/LostSightOfPlayer");

            rpsguy.spriteRenderer[0].sprite = rpsguy.alive;

            yield return "Initializing ERROR-BOT_ITEM-STEALER";
            FieldInfo
                _spriteSheet = AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet");
            var errorbot = new NPCBuilder<ERRORBOT>(Info)
                .SetName("ERROR-BOT_ITEM-STEALER")
                .SetEnum("ERRORBOT")
                .AddLooker()
                .AddTrigger()
                .AddSpawnableRoomCategories(RoomCategory.Hall)
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/ERRORBOT"), []))
                .SetMetaTags(["BCPP"])
                .Build(); //ObjectCreators.CreateNPC<ERRORBOT>("ERROR-BOT_ITEM-STEALER", EnumExtensions.ExtendEnum<Character>("ERRORBOT"), ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/ERRORBOT"), []), usesHeatMap: true, spawnableRooms: [RoomCategory.Hall]);
            errorbot.Navigator.SetRoomAvoidance(true);
            //AccessTools.DeclaredField(typeof(Navigator), "avoidRooms").SetValue(errorbot.Navigator, true);
            SpriteRotator threesixty = errorbot.spriteBase.AddComponent<SpriteRotator>();
            AccessTools.DeclaredField(typeof(SpriteRotator), "spriteRenderer").SetValue(threesixty, errorbot.spriteRenderer[0]);
            AccessTools.DeclaredField(typeof(SpriteRotator), "sprites").SetValue(threesixty, new Sprite[]
            {
                bcppAssets.Get<Sprite>("ERRORBOT/Body1"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body2"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body3"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body4"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body5"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body6"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body7"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body8"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body9"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body10"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body11"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body12"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body13"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body14"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body15"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body16"),
                bcppAssets.Get<Sprite>("ERRORBOT/Body17"),
            });
            errorbot.spriteRenderer[0].transform.position = new Vector3(0f, -3.35f, 0f);
            errorbot.spriteRenderer[0].sprite = bcppAssets.Get<Sprite>("ERRORBOT/Body3");
            errorbot.spriteRenderer = errorbot.spriteRenderer.AddToArray(new GameObject("Head", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>());
            errorbot.spriteRenderer[1].transform.SetParent(errorbot.spriteBase.transform);
            errorbot.spriteRenderer[1].gameObject.layer = errorbot.spriteRenderer[0].gameObject.layer;
            errorbot.spriteRenderer[1].transform.position = new Vector3(0f, 0.27f, 0f);
            errorbot.spriteRenderer[1].material = Resources.FindObjectsOfTypeAll<Material>().Last(m => m.name == "SpriteStandard_Billboard");
            errorbot.spriteRenderer[1].sprite = bcppAssets.Get<Sprite>("ERRORBOT/Head");
            PropagatedAudioManager staticbot = errorbot.gameObject.AddComponent<PropagatedAudioManager>();
            staticbot.audioDevice = errorbot.gameObject.AddComponent<AudioSource>();
            _audOnStart.SetValue(staticbot, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("ERRORBOT/AmbientStatic")
            });
            _loopAudOnStart.SetValue(staticbot, true);
            staticbot.audioDevice.spatialBlend = 1;
            staticbot.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            staticbot.audioDevice.maxDistance = 150;
            staticbot.audioDevice.dopplerLevel = 0;
            staticbot.audioDevice.spread = 0;

            errorbot.audMan = errorbot.gameObject.GetComponents<AudioManager>()[0];
            errorbot.staticMan = staticbot;

            errorbot.audAlarm = bcppAssets.Get<SoundObject>("ERRORBOT/Alarm");
            errorbot.audItemGet = [
                bcppAssets.Get<SoundObject>("ERRORBOT/MineNow"), bcppAssets.Get<SoundObject>("ERRORBOT/ByeToNeeds")
                ];
            errorbot.audScan = bcppAssets.Get<SoundObject>("ERRORBOT/Scanning");
            errorbot.bang = bcppAssets.Get<SoundObject>("ERRORBOT/Jammed");
            errorbot.splat = bcppAssets.Get<SoundObject>("ERRORBOT/Sprayed");

            yield return "Initializing Siege Cannon Cart";
            // This part is the reason why I almost gave up
            var siegecart = new NPCBuilder<SiegeCannonCart>(Info)
                .SetName("Siege Cannon Cart")
                .SetEnum("SiegeCannonCart")
                .AddLooker()
                .AddTrigger()
                //.IgnoreBelts()
                .AddSpawnableRoomCategories(RoomCategory.Hall)
                .DisableNavigationPrecision()
                .DisableAutoRotation()
                .EnableAcceleration()
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/SiegeCannonCart"), []))
                .SetMetaTags(["BCPP", "faculty"])
                .Build(); //ObjectCreators.CreateNPC<SiegeCanonCart>("Siege Canon Cart", EnumExtensions.ExtendEnum<Character>("SiegeCanonCart"), ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/SiegeCannonCart"), []), spawnableRooms: [RoomCategory.Hall]);
            siegecart.Navigator.SetRoomAvoidance(true);
            //AccessTools.DeclaredField(typeof(Navigator), "preciseTarget").SetValue(siegecart.Navigator, false);
            //AccessTools.DeclaredField(typeof(Navigator), "autoRotate").SetValue(siegecart.Navigator, false);
            //AccessTools.DeclaredField(typeof(Navigator), "decelerate").SetValue(siegecart.Navigator, true);
            //AccessTools.DeclaredField(typeof(Looker), "hasFov").SetValue(siegecart.looker, true);
            siegecart.Navigator.maxSpeed = 15f;
            siegecart.Navigator.accel = 15f;
            //AccessTools.DeclaredField(typeof(NPC), "ignoreBelts").SetValue(siegecart, true);
            AudioManager motorAudMan = siegecart.gameObject.AddComponent<PropagatedAudioManager>();
            motorAudMan.audioDevice = siegecart.gameObject.AddComponent<AudioSource>();
            motorAudMan.audioDevice.spatialBlend = 1;
            motorAudMan.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            motorAudMan.audioDevice.maxDistance = 150;
            motorAudMan.audioDevice.dopplerLevel = 0;
            motorAudMan.audioDevice.spread = 0;
            _audOnStart.SetValue(motorAudMan, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("SiegeCanonCart/Paddle")
            });
            _loopAudOnStart.SetValue(motorAudMan, true);
            AnimatedSpriteRotator hell = siegecart.spriteBase.AddComponent<AnimatedSpriteRotator>();
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "renderer").SetValue(hell, siegecart.spriteRenderer[0]);
            SpriteRotationMap idleRotationMap = new SpriteRotationMap()
            {
                angleCount = 8
            };
            Sprite[] idleStaticFrames = new Sprite[]
            {
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleFront"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleLeftFront"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleLeft"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleLeftBehind"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleBehind"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleRightBehind"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleRight"),
                bcppAssets.Get<Sprite>("SiegeCanonCart/IdleRightFront"),
            };
            // I fucking hate this. Not because of the entire Dr. Reflex reference, it's because of the character usually being an animated 3d model on the decompile.
            _spriteSheet.SetValue(idleRotationMap, idleStaticFrames);
            SpriteRotationMap moveRotationMap = new SpriteRotationMap()
            {
                angleCount = 8
            };

            List<Sprite> movingFrames = new List<Sprite>();
            for (int frame = 1; frame <= 7; frame++)
            {
                movingFrames.AddRange([
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingFront_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingLeftFront_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingLeft_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingLeftBehind_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingBehind_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingRightBehind_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingRight_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/MovingRightFront_"+frame)
                    ]);
            }
            _spriteSheet.SetValue(moveRotationMap, movingFrames.ToArray());
            SpriteRotationMap shootRotationMap = new SpriteRotationMap()
            {
                angleCount = 8
            };

            List<Sprite> shootingFrames = new List<Sprite>();
            for (int frame = 1; frame <= 8; frame++)
            {
                shootingFrames.AddRange([
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingFront_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingLeftFront_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingLeft_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingLeftBehind_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingBehind_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingRightBehind_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingRight_"+frame),
                    bcppAssets.Get<Sprite>("SiegeCanonCart/ShootingRightFront_"+frame)
                    ]);
            }
            _spriteSheet.SetValue(shootRotationMap, shootingFrames.ToArray());
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "spriteMap").SetValue(hell, new SpriteRotationMap[]
            {
                idleRotationMap,
                moveRotationMap,
                shootRotationMap
            });
            siegecart.spriteRenderer[0].transform.position = new Vector3(0, 0.75f, 0);
            siegecart.spriteRenderer[0].sprite = bcppAssets.Get<Sprite>("SiegeCanonCart/IdleFront");
            CustomSpriteRotatorAnimator animator = siegecart.gameObject.AddComponent<CustomSpriteRotatorAnimator>();
            animator.renderer = hell;
            animator.timeScale = TimeScaleType.Npc; 
            siegecart.animator = animator;
            animator.AddAngledAnimation("Idle", 8, idleStaticFrames.ToList(), 1f);
            animator.AddAngledAnimation("Moving", 8, movingFrames.ToList(), 2);
            animator.AddAngledAnimation("Shoot", 8, shootingFrames.ToList(), 6);

            siegecart.audMan = siegecart.gameObject.GetComponents<AudioManager>()[0];
            siegecart.bang = Resources.FindObjectsOfTypeAll<SoundObject>().Last(b => b.name == "Bang");

            siegecart.motorAudMan = motorAudMan;
            // The balls
            GameObject ball = Instantiate(Resources.FindObjectsOfTypeAll<ITM_BSODA>().Last(x => x.name == "ITM_BSODA"), MTM101BaldiDevAPI.prefabTransform).gameObject;
            ball.name = "CanonBall";
            Destroy(ball.GetComponent<ITM_BSODA>());
            siegecart.ballPre = ball.AddComponent<SiegeCartBalls>();
            ball.ConvertToPrefab(true);
            siegecart.ballPre.entity = ball.GetComponent<Entity>();
            ball.layer = LayerMask.NameToLayer("StandardEntities"); // No wonder why it keeps setting to default...
            Destroy(ball.transform.Find("RendereBase").Find("Particles").gameObject);
            ball.transform.Find("RendereBase").GetComponentInChildren<SpriteRenderer>().sprite = bcppAssets.Get<Sprite>("SiegeCanonCart/Ball");
            bcppAssets.Add<Entity>("SiegeCannonBall", ball.GetComponent<Entity>());

            yield return "Initializing Mr. Portal Man";
            FieldInfo _layerMaskLooker = AccessTools.DeclaredField(typeof(Looker), "layerMask");
            var portalman = new NPCBuilder<MrPortalMan>(Info)
                .SetName("Mr Portal Man")
                .SetEnum("MrPortalMan")
                .AddLooker()
                .AddTrigger()
                //.IgnoreBelts()
                .SetAirborne()
                .DisableNavigationPrecision()
                .IgnorePlayerOnSpawn()
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .AddSpawnableRoomCategories(RoomCategory.Faculty)
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/MrPortalMan"), []))
                .SetMetaTags(["BCPP"])
                .Build(); //ObjectCreators.CreateNPC<MrPortalMan>("Mr Portal Man", EnumExtensions.ExtendEnum<Character>("MrPortalMan"), ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/MrPortalMan"), []), usesHeatMap: true, spawnableRooms: [RoomCategory.Hall]);
            //AccessTools.DeclaredField(typeof(Looker), "hasFov").SetValue(portalman.looker, false);
            LayerMask layer = (LayerMask)_layerMaskLooker.GetValue(portalman.looker);
            layer |= 1 << LayerMask.NameToLayer("NPCs");
            _layerMaskLooker.SetValue(portalman.looker, layer);
            //AccessTools.DeclaredField(typeof(NPC), "ignorePlayerOnSpawn").SetValue(portalman, true);
            portalman.spriteRenderer[0].sprite = bcppAssets.Get<Sprite>("MrPortalMan/PortalMan");
            portalman.spriteRenderer[0].transform.position = new Vector3(0f, 0.5f, 0f);
            portalman.spriteRenderer = portalman.spriteRenderer.AddToArray(Instantiate(portalman.spriteRenderer[0].gameObject, portalman.spriteBase.transform, true).GetComponent<SpriteRenderer>());
            portalman.spriteRenderer[1].maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            portalman.Navigator.Entity.SetTrigger(false);
            SpriteMask mask = portalman.spriteRenderer[0].gameObject.AddComponent<SpriteMask>();
            mask.sprite = bcppAssets.Get<Sprite>("MrPortalMan/PortalManMask");
            mask.alphaCutoff = 1f;
            portalman.spriteRenderer[0].gameObject.AddComponent<BillboardUpdater>();
            CapsuleCollider trigger = portalman.baseTrigger[0] as CapsuleCollider;
            trigger.radius = 3;
            trigger.height = 12;
            yield return "Initializing Mr. Portal Man (RenderTextures)";
            // Welp, I was never here!
            RenderTexture baseRendererTexture = Resources.FindObjectsOfTypeAll<RenderTexture>().Last(x => x.name == "AuthenticGameTexture");
            Camera mainCam = Resources.FindObjectsOfTypeAll<GameCamera>().First().transform.Find("MainCamera").GetComponent<Camera>();
            RenderTexture portalmanCamText = Instantiate(baseRendererTexture);
            portalmanCamText.name = "MrPortalManRenderCam";
            portalmanCamText.width = 256;
            portalmanCamText.height = 256;
            portalmanCamText.isPowerOfTwo = false;
            portalmanCamText.memorylessMode = RenderTextureMemoryless.Depth | RenderTextureMemoryless.MSAA;
            portalmanCamText.Release();
            bcppAssets.Add<RenderTexture>("MrPortalMan/ManCamRender", portalmanCamText);
            RenderTexture outputportalCamText = Instantiate(baseRendererTexture);
            outputportalCamText.name = "OutputPortalRenderCam";
            outputportalCamText.width = 277;
            outputportalCamText.height = 356;
            outputportalCamText.isPowerOfTwo = false;
            outputportalCamText.memorylessMode = RenderTextureMemoryless.Depth | RenderTextureMemoryless.MSAA;
            outputportalCamText.Release();
            bcppAssets.Add<RenderTexture>("MrPortalMan/OutputCamRender", outputportalCamText);
            portalman.portalmanCam = Instantiate(mainCam, portalman.transform, false);
            portalman.portalmanCam.name = "MrPortalManCamera";
            portalman.portalmanCam.targetTexture = portalmanCamText;
            portalman.portalmanCam.cullingMask |= LayerMask.GetMask("Billboard");
            //portalman.portalmanCam.nearClipPlane = 0.01f;
            portalman.portalmanCam.useOcclusionCulling = false;

            Destroy(portalman.portalmanCam.gameObject.GetComponent<VA_AudioListener>());
            portalman.portalmanCam.enabled = false;
            portalman.outputCamPre = Instantiate(mainCam, portalman.transform, false);
            portalman.outputCamPre.name = "OutputPortalCamera";
            portalman.outputCamPre.targetTexture = outputportalCamText;
            portalman.outputCamPre.gameObject.AddComponent<CullAffector>();
            //portalman.outputCamPre.cullingMask |= LayerMask.GetMask("Billboard");
            //portalman.outputCamPre.nearClipPlane = 0.01f;
            portalman.outputCamPre.useOcclusionCulling = false;
            Destroy(portalman.outputCamPre.gameObject.GetComponent<VA_AudioListener>());
            portalman.outputCamPre.enabled = false;
            portalman.outputCamPre.gameObject.SetActive(false);

            portalman.audMan = portalman.gameObject.GetComponents<AudioManager>()[0];
            portalman.audHungry = bcppAssets.Get<SoundObject>("MrPortalMan/Hungry");
            portalman.audWhere = bcppAssets.Get<SoundObject>("MrPortalMan/WhereIs");
            portalman.audFulfilled = bcppAssets.Get<SoundObject>("MrPortalMan/Fullfilled");
            portalman.teleport = Resources.FindObjectsOfTypeAll<SoundObject>().Last(b => b.name == "Teleport");
            yield return "Initializing Mr. Portal Man (Portal Outputs)";
            // This is a room poster
            Material cryingportalMat = Instantiate(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "BlankChalk"));
            cryingportalMat.name = "CryingPortalMat";
            cryingportalMat.SetMainTexture(bcppAssets.Get<Texture2D>("MrPortalMan/OutputPortal"));
            Material cryingportalMask = Instantiate(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "PortalMask"));
            cryingportalMask.name = "CryingPortalMask";
            cryingportalMask.SetMaskTexture(bcppAssets.Get<Texture2D>("MrPortalMan/OutputPortalMask"));
            GameObject cryingPortal = Instantiate(Resources.FindObjectsOfTypeAll<Chalkboard>().Last(), MTM101BaldiDevAPI.prefabTransform).gameObject;
            cryingPortal.name = "CryingPortalOutput";
            cryingPortal.transform.Find("Chalkbaord").Find("Quad").GetComponent<MeshRenderer>().materials = [cryingportalMask, cryingportalMat];
            cryingPortal.transform.Find("Chalkbaord").Find("Quad").GetComponent<MeshCollider>().enabled = false;
            Destroy(cryingPortal.GetComponent<Chalkboard>());
            CryingPortal cry = cryingPortal.AddComponent<CryingPortal>();
            cry.mask = cryingportalMask;
            cryingPortal.ConvertToPrefab(true);
            portalman.portalPre = cryingPortal.GetComponent<CryingPortal>();

            yield return "Initializing Mr. Robber"; // People do hate him for some reason...
            var robber = new NPCBuilder<MrRobber>(Info)
                .SetName("Mr Robber")
                .SetEnum("MrRobber")
                .AddLooker()
                .AddTrigger()
                //.IgnoreBelts()
                .EnableAcceleration()
                //.SetFOV(100f)
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .AddSpawnableRoomCategories(RoomCategory.Hall, RoomCategory.Special)
                .SetPoster(ObjectCreators.CreatePosterObject(Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent" && x.isReadable), []))
                .SetMinMaxAudioDistance(1f, 500f)
                .SetMetaTags(["BCPP"])
                .Build();

            //ObjectCreators.CreateNPC<MrRobber>("Mr Robber", EnumExtensions.ExtendEnum<Character>("MrRobber"), ObjectCreators.CreatePosterObject(Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent"), []), spawnableRooms: [RoomCategory.Hall, RoomCategory.Special], minAudioDistance: 1f, maxAudioDistance: 500);
            robber.Navigator.SetRoomAvoidance(false);
            // I FORGOT ABOUT THIS
            layer = (LayerMask)_layerMaskLooker.GetValue(robber.looker);
            layer |= 1 << LayerMask.NameToLayer("NPCs");
            _layerMaskLooker.SetValue(robber.looker, layer);
            // Returning to normal I suppose...
            robber.sfxMan = robber.gameObject.AddComponent<AudioManager>();
            robber.sfxMan.audioDevice = robber.gameObject.AddComponent<AudioSource>();
            robber.sfxMan.audioDevice.playOnAwake = false;
            robber.sfxMan.audioDevice.spatialBlend = 1f;
            robber.sfxMan.audioDevice.spatialBlend = 1;
            robber.sfxMan.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            robber.sfxMan.audioDevice.maxDistance = 150;
            robber.sfxMan.audioDevice.dopplerLevel = 0;
            robber.sfxMan.audioDevice.spread = 0;
            _audOnStart.SetValue(robber.sfxMan, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("MrRobber/EssenceBeat")
            });
            _loopAudOnStart.SetValue(robber.sfxMan, true);
            robber.overlay = Instantiate(Resources.FindObjectsOfTypeAll<Gum>().Last().transform.Find("GumOverlay").gameObject, robber.transform).GetComponent<Canvas>();
            robber.overlay.transform.Find("Image").GetComponent<Image>().sprite = null;
            robber.overlay.gameObject.SetActive(false);

            robber.audMan = robber.gameObject.GetComponents<AudioManager>()[0];

            robber.voiceIntro = [
                bcppAssets.Get<SoundObject>("MrRobber/BlastingGreenades"),
                bcppAssets.Get<SoundObject>("MrRobber/Notebooks"),
                bcppAssets.Get<SoundObject>("MrRobber/Worse"),
                bcppAssets.Get<SoundObject>("MrRobber/Shine")
                ];
            robber.voiceFlee = [
                bcppAssets.Get<SoundObject>("MrRobber/Flee"),
                bcppAssets.Get<SoundObject>("MrRobber/WasNeverHere"),
                bcppAssets.Get<SoundObject>("MrRobber/RegretAlone")
                ];
            robber.sfxBeat = BCPPPlugin.bcppAssets.Get<SoundObject>("MrRobber/AnnoyingBeat");
            robber.sfxScreech = [
                bcppAssets.Get<SoundObject>("MrRobber/ScreechIntro"),
                bcppAssets.Get<SoundObject>("MrRobber/ScreechLoop")
                ];

            robber.wanderingSprite = bcppAssets.Get<Sprite>("MrRobber/Idle");
            robber.huntingSprite = bcppAssets.Get<Sprite>("MrRobber/Targeting");
            robber.spriteRenderer[0].sprite = robber.wanderingSprite;

            /*NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [rpsguy], "RPS Guy", NPCFlags.Standard));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [errorbot], "ERROR-BOT_ITEM-STEALER", NPCFlags.Standard | NPCFlags.MakeNoise));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [siegecart], "Siege Canon Cart", NPCFlags.StandardNoCollide));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [portalman], "Mr Portal Man", NPCFlags.Standard));
            //NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [robber], "Mr Robber", NPCFlags.Standard));*/
            yield return "Finalizing NPC Initialization";
            bcppAssets.AddRange<NPC>([
                rpsguy,
                errorbot,
                siegecart,
                portalman,
                robber
                ],
                [
                "NPCs/RPS Guy",
                "NPCs/ERROR-BOT_ITEM-STEALER",
                "NPCs/Siege Cannon Cart",
                "NPCs/Mr Portal Man",
                "NPCs/Mr Robber"
                ]);

            yield return "Initializing Unsafe Hammer";
            // Pretty sure this hammer came in before BBT, but I didn't really know how to add items to arrays back then...
            Items bhammerEnum = EnumExtensions.ExtendEnum<Items>("BHammer");
            ItemObject hammer = new ItemBuilder(Info)
                .SetItemComponent<ITM_Hammer>()
                .SetNameAndDescription("Itm_BHammer", "Desc_BHammer")
                .SetSprites(bcppAssets.Get<Sprite>("Items/BHammer_Small"), bcppAssets.Get<Sprite>("Items/BHammer_Large"))
                .SetEnum(bhammerEnum)
                .SetShopPrice(411) // No longer blends into BBT's hammer cost
                .SetGeneratorCost(30)
                .SetMeta(ItemFlags.MultipleUse, ["BCPP", "crmp_contraband"]) // Reason of contraband: "Unsafe"
                .Build(); //ObjectCreators.CreateItemObject("Itm_BHammer", "Desc_BHammer", bcppAssets.Get<Sprite>("Items/BHammer_Small"), bcppAssets.Get<Sprite>("Items/BHammer_Large"), EnumExtensions.ExtendEnum<Items>("BHammer"), 25, 30);
            hammer.name = "Itm_BHammer";
            hammer.item.name = "Obj_BHammer";
            ItemObject hammer2 = new ItemBuilder(Info) // And here goes the stupid grapple ways...
                .SetItemComponent<ITM_Hammer>()
                .SetNameAndDescription("Itm_BHammer", "Desc_BHammer")
                .SetSprites(bcppAssets.Get<Sprite>("Items/BHammer_Small"), bcppAssets.Get<Sprite>("Items/BHammer_Large"))
                .SetEnum(bhammerEnum)
                .SetShopPrice(int.MaxValue)
                .SetGeneratorCost(int.MaxValue)
                .SetMeta(hammer.GetMeta())
                //.SetMeta(ItemFlags.MultipleUse, ["BCPP"])
                .Build();
            hammer2.name = "Itm_BHammerMid";
            hammer2.item.name = "Obj_BHammerMid";
            hammer2.item.GetComponent<ITM_Hammer>().duration = 2;
            ItemObject hammer3 = new ItemBuilder(Info) // And here goes the stupid grapple ways...
                .SetItemComponent<ITM_Hammer>()
                .SetNameAndDescription("Itm_BHammer", "Desc_BHammer")
                .SetSprites(bcppAssets.Get<Sprite>("Items/BHammer_Small"), bcppAssets.Get<Sprite>("Items/BHammer_Large"))
                .SetEnum(bhammerEnum)
                .SetShopPrice(int.MaxValue)
                .SetGeneratorCost(int.MaxValue)
                .SetMeta(hammer.GetMeta())
                //.SetMeta(ItemFlags.MultipleUse, ["BCPP"])
                .Build();
            hammer3.name = "Itm_BHammerLow";
            hammer3.item.name = "Obj_BHammerLow";
            hammer3.item.GetComponent<ITM_Hammer>().duration = 1;
            hammer.GetMeta().itemObjects = [hammer3, hammer2, hammer];
            yield return "Initializing Profit Card and its assets";
            var playerclicklayer = Resources.FindObjectsOfTypeAll<LayerMaskObject>().Last(x => x.name == "PlayerClickLayerMask");
            // Annnddd, this is something stupidly uncool and sometimes creative...
            ItemObject profitCard = new ItemBuilder(Info)
                .SetItemComponent<ITM_Acceptable>()
                .SetNameAndDescription("Itm_ProfitCard", "Desc_ProfitCard")
                .SetSprites(bcppAssets.Get<Sprite>("Items/ProfitCard_Small"), bcppAssets.Get<Sprite>("Items/ProfitCard_Large"))
                .SetEnum("ProfitCard")
                .SetShopPrice(285)
                .SetGeneratorCost(25)
                .SetMeta(ItemFlags.None, ["BCPP", "currency"])
                .Build(); //ObjectCreators.CreateItemObject("Itm_ProfitCard", "Desc_ProfitCard", bcppAssets.Get<Sprite>("Items/ProfitCard_Small"), bcppAssets.Get<Sprite>("Items/ProfitCard_Large"), EnumExtensions.ExtendEnum<Items>("ProfitCard"), 85, 25);
            profitCard.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("ProfitCard"));
            profitCard.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().Last(x => x.name == "CoinDrop"));
            profitCard.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("layerMask", playerclicklayer);
            profitCardInsert = Instantiate(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "BSODAMachine"));
            profitCardInsert.name = "ProfitCardMachine";
            profitCardInsert.SetMainTexture(bcppAssets.Get<Texture2D>("ProfitCardMachine"));
            profitCardInsert.renderQueue += 10;
            // You'll need a free one!
            freeInsert = Instantiate(profitCardInsert);
            freeInsert.name = "FreeItemMachine";
            freeInsert.SetMainTexture(bcppAssets.Get<Texture2D>("FreeItemMachine"));
            yield return "Initializing Unsecured Key";
            // Something evil is brewing inside!
            ItemObject swingingdoorInfKey = new ItemBuilder(Info)
                .SetItemComponent<ITM_UnsecuredKey>()
                .SetNameAndDescription("Itm_UnsecuredKey", "Desc_UnsecuredKey")
                .SetSprites(bcppAssets.Get<Sprite>("Items/UnsecuredKey_Small"), bcppAssets.Get<Sprite>("Items/UnsecuredKey_Large"))
                .SetEnum("UnsecuredYellowKey")
                .SetShopPrice(1000)
                .SetGeneratorCost(50)
                .SetAsNotOverridable()
                .SetMeta(ItemFlags.MultipleUse, ["BCPP", "technology"])
                .Build(); //ObjectCreators.CreateItemObject("Itm_UnsecuredKey", "Desc_UnsecuredKey", bcppAssets.Get<Sprite>("Items/UnsecuredKey_Small"), bcppAssets.Get<Sprite>("Items/UnsecuredKey_Large"), EnumExtensions.ExtendEnum<Items>("UnsecuredYellowKey"), 110, 50);
            swingingdoorInfKey.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey"));
            swingingdoorInfKey.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().Last(x => x.name == "Doors_StandardUnlock"));
            swingingdoorInfKey.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("layerMask", playerclicklayer);
            yield return "Initializing Secured Lock and Secured Locked Door builder";
            ItemObject swingingdoorInfLock = new ItemBuilder(Info)
                .SetItemComponent<ITM_Acceptable>()
                .SetNameAndDescription("Itm_SecuredLock", "Desc_SecuredLock")
                .SetSprites(bcppAssets.Get<Sprite>("Items/SecuredLock_Small"), bcppAssets.Get<Sprite>("Items/SecuredLock_Large"))
                .SetEnum(EnumExtensions.ExtendEnum<Items>("SecuredYellowLock"))
                .SetShopPrice(795)
                .SetGeneratorCost(75)
                .SetMeta(ItemFlags.None, ["BCPP", "technology", "crmp_contraband"]) // Reason of contraband: they last too long.
                .Build(); //ObjectCreators.CreateItemObject("Itm_SecuredLock", "Desc_SecuredLock", bcppAssets.Get<Sprite>("Items/SecuredLock_Small"), bcppAssets.Get<Sprite>("Items/SecuredLock_Large"), EnumExtensions.ExtendEnum<Items>("SecuredYellowLock"), 95, 75);
            swingingdoorInfLock.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock"));
            swingingdoorInfLock.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().Last(x => x.name == "Slap"));
            swingingdoorInfLock.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("layerMask", playerclicklayer);
            GameObject inflockedSwingDoor = Instantiate(Resources.FindObjectsOfTypeAll<SwingDoor>().Last(p => p.name == "Door_Swinging"), MTM101BaldiDevAPI.prefabTransform).gameObject;
            inflockedSwingDoor.name = "Door_SwingingSecuredLock";
            var inflockeddoor = inflockedSwingDoor.SwapComponent<SwingDoor, SwingDoor_Secured>();
            inflockeddoor.securedDoor = inflockedSwingDoor.AddComponent<SecuredSwingingDoor>();
            inflockeddoor.securedDoor.swingDoor = inflockeddoor;
            securedYellowSwingingDoor = Instantiate(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "CoinDoor"));
            securedYellowSwingingDoor.name = "SecuredLockDoor";
            securedYellowSwingingDoor.SetMainTexture(bcppAssets.Get<Texture2D>("Obstacles/InfLockedSwingingDoor"));
            //inflockedSwingDoor.GetComponent<SecuredSwingingDoor>().doorOverlay = securedYellowSwingingDoor;
            //inflockedSwingDoor.SetActive(false);
            inflockedSwingDoor.GetComponent<AudioManager>().enabled = false;
            //inflockedSwingDoor.ConvertToPrefab(true);
            CoinDoorBuilder builder = Instantiate(Resources.FindObjectsOfTypeAll<CoinDoorBuilder>().Last(x => x.name == "CoinDoorBuilder"), MTM101BaldiDevAPI.prefabTransform);
            builder.gameObject.name = "Inf Secured Swinging Door Builder";
            builder.ReflectionSetVariable("doorPre", inflockedSwingDoor.GetComponent<SwingDoor>());
            builder.obstacle = EnumExtensions.ExtendEnum<Obstacle>("InfLockedDoor");
            //builder.gameObject.ConvertToPrefab(true);
            //ObjectBuilderMetaStorage.Instance.Add(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor"), new ObjectBuilderMeta(Info, builder));
            bcppAssets.Add<SecuredSwingingDoor>("Obstacles/SecuredSwingingDoor", inflockedSwingDoor.GetComponent<SecuredSwingingDoor>());
            bcppAssets.Add<ObjectBuilder>("ObjectBuilder/SecuredSwingingDoor", builder);
            yield return "Initializing Anyportal Output";
            ItemObject anyportal = new ItemBuilder(Info)
                .SetItemComponent<ITM_AnyportalOutput>()
                .SetNameAndDescription("Itm_Anyportal", "Desc_Anyportal")
                .SetSprites(bcppAssets.Get<Sprite>("Items/Anyportal_Small"), bcppAssets.Get<Sprite>("Items/Anyportal_Large"))
                .SetEnum("AnyportalOutput")
                .SetShopPrice(500)
                .SetGeneratorCost(105)
                .SetMeta(ItemFlags.Persists, ["BCPP", "technology"])
                .Build();
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().wrongPlacement = Resources.FindObjectsOfTypeAll<SoundObject>().Last(x => x.name == "ErrorMaybe");
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().outputGeneration = bcppAssets.Get<SoundObject>("Items/AnyportalOutput/Spawning");
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().destroySnd = bcppAssets.Get<SoundObject>("Items/AnyportalOutput/Destroyed");

            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan = anyportal.item.gameObject.AddComponent<PropagatedAudioManager>();
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice = anyportal.item.gameObject.AddComponent<AudioSource>();
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice.playOnAwake = false;
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice.spatialBlend = 1f;
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice.dopplerLevel = 0f;
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice.maxDistance = 100f;
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().audMan.audioDevice.SetCustomCurve(AudioSourceCurveType.CustomRolloff, NPCMetaStorage.Instance.Get(Character.Beans).value.GetComponent<AudioManager>().audioDevice.GetCustomCurve(AudioSourceCurveType.CustomRolloff));

            GameObject quad = Instantiate(Resources.FindObjectsOfTypeAll<Chalkboard>().ToList().First(), anyportal.item.transform, false).gameObject;
            quad.transform.Find("Chalkbaord").Find("Quad").SetParent(anyportal.item.transform, false);
            Destroy(quad.gameObject);
            anyportal.item.transform.Find("Quad").rotation = Quaternion.Euler(90f, 0f, 0f);
            anyportal.item.transform.Find("Quad").localPosition = new Vector3(0f, -4.9f, 0f);
            anyportal.item.transform.Find("Quad").localScale = new Vector3(10f, 10f, 10f);
            Material outputmat = Instantiate(cryingportalMat);
            outputmat.name = "OutputPortalMat";
            outputmat.SetMainTexture(bcppAssets.Get<Texture2D>("AnyportalOutput"));
            Material outputmask = Instantiate(cryingportalMask);
            outputmask.name = "OutputPortalMask";
            outputmask.SetMaskTexture(bcppAssets.Get<Texture2D>("AnyportalOutputMask"));
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().mask = outputmask;
            anyportal.item.transform.Find("Quad").GetComponent<MeshRenderer>().materials = [outputmask, outputmat];
            anyportal.item.transform.Find("Quad").GetComponent<MeshCollider>().enabled = false;

            ParticleSystem particle = anyportal.item.transform.Find("Quad").gameObject.AddComponent<ParticleSystem>();
            var main = particle.main;

            main.duration = 5f;
            main.startLifetime = 0.75f;
            main.startSpeed = -10f;
            main.startSize = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.playOnAwake = false;
            main.startColor = new Color(0.07058824f, 0.6431373f, 1f);

            var emission = particle.emission;
            emission.rateOverTime = 50f;


            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 10f;

            var render = anyportal.item.transform.Find("Quad").gameObject.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Billboard;
            render.material = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UI_AsSprite");
            render.sortMode = ParticleSystemSortMode.OldestInFront;
            render.minParticleSize = 0.01f;
            render.maxParticleSize = 0.01f;
            yield return "Initializing TASER Pulse";
            ItemObject taserpulse = new ItemBuilder(Info)
                .SetItemComponent<ITM_TaserPulse>()
                .SetNameAndDescription("Itm_TASERPulse", "Desc_TASERPulse")
                .SetSprites(bcppAssets.Get<Sprite>("Items/TASERPulse_Small"), bcppAssets.Get<Sprite>("Items/TASERPulse_Large"))
                .SetEnum("TASERPulse")
                .SetShopPrice(455)
                .SetGeneratorCost(140)
                .SetMeta(ItemFlags.Persists, ["BCPP", "technology", "firearm", "crmp_contraband"]) // Reason of contraband: illegal use inside of the schoolhouse.
                .Build();
            taserpulse.item.GetComponent<ITM_TaserPulse>().mask = LayerMask.GetMask("Default", "Ignore Raycast", "Ignore Raycast B", "NPCs", "Windows");
            var tasermat = Instantiate(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "Bookshelf"));
            tasermat.SetMainTexture(bcppAssets.Get<Texture2D>("TaserLineMat"));
            taserpulse.item.GetComponent<ITM_TaserPulse>().mat = tasermat;
            taserpulse.item.GetComponent<ITM_TaserPulse>().use = bcppAssets.Get<SoundObject>("Items/TASERPulse/Use");

            /*ItemMetaStorage.Instance.Add(hammer, new ItemMetaData(Info, hammer));
            ItemMetaStorage.Instance.Add(profitCard, new ItemMetaData(Info, profitCard));
            ItemMetaStorage.Instance.Add(swingingdoorInfKey, new ItemMetaData(Info, swingingdoorInfKey));
            ItemMetaStorage.Instance.Add(swingingdoorInfLock, new ItemMetaData(Info, swingingdoorInfLock));*/
            yield return "Finalizing Item Initialization";
            bcppAssets.AddRange<ItemObject>([
                hammer,
                hammer2,
                hammer3,
                profitCard,
                swingingdoorInfKey,
                swingingdoorInfLock,
                anyportal,
                taserpulse
                ],
                [
                "Items/BHammer",
                "Items/BHammerMid",
                "Items/BHammerLow",
                "Items/ProfitCard",
                "Items/UnsecuredKey",
                "Items/SecuredLock",
                "Items/AnyportalOutput",
                "Items/TASERPulse"
                ]);

            yield return "Initializing structures";
            GameObject randommachine = Instantiate(Resources.FindObjectsOfTypeAll<TapePlayer>().Last(x => x.name == "PayPhone" && x.GetInstanceID() >= 0), MTM101BaldiDevAPI.prefabTransform).gameObject;
            DestroyImmediate(randommachine.GetComponent<AudioManager>());
            DestroyImmediate(randommachine.GetComponent<AudioSource>());
            randommachine.name = "Random Machine";
            randommachine.ConvertToPrefab(true);

            RandomItemMachine gambling = randommachine.AddComponent<RandomItemMachine>();
            gambling.coverage = randommachine.GetComponent<TapePlayer>().coverage;
            gambling.tileShapeMask = randommachine.GetComponent<TapePlayer>().tileShapeMask;
            DestroyImmediate(randommachine.GetComponent<TapePlayer>());
            gambling.requiredItem = profitCard;
            var itemmeta = ItemMetaStorage.Instance;
            gambling.potentialItems = [
                new WeightedItemObject()
                {
                    selection = hammer,
                    weight = 80
                },
                new WeightedItemObject()
                {
                    selection = swingingdoorInfKey,
                    weight = 60
                },
                new WeightedItemObject()
                {
                    selection = anyportal,
                    weight = 80
                },
                new WeightedItemObject()
                {
                    selection = taserpulse,
                    weight = 60
                },

                new WeightedItemObject()
                {
                    selection = itemmeta.FindByEnum(Items.NanaPeel).value,
                    weight = 90
                },
                new WeightedItemObject()
                {
                    selection = itemmeta.FindByEnum(Items.Apple).value,
                    weight = 55
                },
                new WeightedItemObject()
                {
                    selection = itemmeta.FindByEnum(Items.PrincipalWhistle).value,
                    weight = 90
                },
                new WeightedItemObject()
                {
                    selection = itemmeta.FindByEnum(Items.PortalPoster).value,
                    weight = 55
                },
                new WeightedItemObject()
                {
                    selection = itemmeta.FindByEnum(Items.Boots).value,
                    weight = 80
                }
            ];
            gambling.render = gambling.GetComponentInChildren<SpriteRenderer>(true);
            gambling.render.sprite = bcppAssets.Get<Sprite>("Structures/RandomMachine");
            gambling.render.transform.position = new Vector3(gambling.render.transform.position.x, 4.2f, gambling.render.transform.position.z);
            gambling.outOf = bcppAssets.Get<Sprite>("Structures/RandomMachine_OutOfGoods");

            GenericHallBuilder gambleBuilder = Instantiate(Resources.FindObjectsOfTypeAll<GenericHallBuilder>().Last(x => x.name == "PayphoneBuilder" && x.GetInstanceID() >= 0), MTM101BaldiDevAPI.prefabTransform);
            gambleBuilder.gameObject.name = "Random Machine Builder";
            gambleBuilder.gameObject.ConvertToPrefab(true);
            ObjectPlacer yesIhateThat = gambleBuilder.ReflectionGetVariable("objectPlacer") as ObjectPlacer;
            yesIhateThat.ReflectionSetVariable("prefab", gambling.gameObject);
            gambleBuilder.ReflectionSetVariable("objectPlacer", yesIhateThat);
            bcppAssets.Add<ObjectBuilder>("ObjectBuilder/RandomItemMachine", gambleBuilder);

            bcppAssets.AddRange<GameObject>([
                    gambling.gameObject,
                ],
                [
                    "Structures/RandomItemMachine",
                ]);

            yield return "Initializing Stickers";
            MiscPatches.bcppStickerPack = EnumExtensions.ExtendEnum<StickerPackType>("BCPP");
            StickerPatches.switcherooSticker = new StickerBuilder<ExtendedStickerData>(Info)
                .SetSprite(bcppAssets.Get<Sprite>("Stickers/Switcherquartprofit"))
                .SetEnum("Switcherquartprofit")
                .SetDuplicateOddsMultiplier(0.5f)
                .SetValueCap(1)
                .Build().sticker;
            StickerPatches.profitCard = profitCard.itemType;
            StickerPatches.rpsSticker = new StickerBuilder<ExtendedStickerData>(Info)
                .SetSprite(bcppAssets.Get<Sprite>("Stickers/RockPaperScissorsSticker")) // I have invented one of the most useless stickers that only can benefit gambling on an NPC.
                .SetEnum("RockPaperScissors")
                .SetAsAffectingGenerator()
                .Build().sticker;
            StickerPatches.stealerSticker = new StickerBuilder<ExtendedStickerData>(Info)
                .SetSprite(bcppAssets.Get<Sprite>("Stickers/ERBOTSticker"))
                .SetEnum("ERRORBOT")
                .SetDuplicateOddsMultiplier(0.65f)
                .Build().sticker;
            var anyportalSticker = new StickerBuilder<ExtendedStickerData>(Info)
                .SetSprite(bcppAssets.Get<Sprite>("Stickers/PortalManSticker"))
                .SetEnum("Anyportal")
                .SetDuplicateOddsMultiplier(0.65f)
                .Build();
            anyportal.item.gameObject.GetComponent<ITM_AnyportalOutput>().anyportalSticker = anyportalSticker.sticker;
            StickerPatches.anyportalSticker = anyportalSticker.sticker;
            StickerPatches.anyportalItem = anyportal.itemType;
            var anyportalGuy = new ItemBuilder(Info)
                .SetNameAndDescription("Itm_AnyportalMan", "Desc_AnyportalMan")
                .SetEnum("AnyportalMan")
                .SetSprites(bcppAssets.Get<Sprite>("Items/AnyportalMan_Small"), bcppAssets.Get<Sprite>("Items/AnyportalMan_Large"))
                .SetShopPrice(980)
                .SetGeneratorCost(105)
                .SetItemComponent<ITM_AnyportalGuy>()
                .SetMeta(ItemFlags.None, ["BCPP", "technology"])
                .Build();
            StickerPatches.mrAnyportal = anyportalGuy;
            anyportalGuy.item.GetComponent<ITM_AnyportalGuy>().destroySnd = portalman.teleport;
            anyportalGuy.item.GetComponent<ITM_AnyportalGuy>().wrongPlacement = anyportal.item.GetComponent<ITM_AnyportalOutput>().wrongPlacement;
            bcppAssets.Add<ItemObject>("Items/AnyportalGuy", anyportalGuy);
            StickerPatches.securedPacket = new StickerBuilder<SecuredPacket>(Info)
                .SetSprite(bcppAssets.Get<Sprite>("Stickers/StickerPacketSecured"))
                .SetEnum("SecuredPacket")
                .SetDuplicateOddsMultiplier(0)
                .Build().sticker;
            var stickerPacket = new ItemBuilder(Info)
                .SetNameAndDescription("StickerTitle_SecuredPacket", "Desc_SecuredPacket")
                .SetSprites(bcppAssets.Get<Sprite>("Items/SecuredPacket"), bcppAssets.Get<Sprite>("Items/SecuredPacket"))
                .SetShopPrice(150)
                .SetAsInstantUse()
                .SetEnum(Items.StickerPack)
                .SetItemComponent<ITM_StickerPack>()
                .Build();
            MiscPatches._securedPacket = stickerPacket;
            MiscPatches.securedPacket = EnumExtensions.ExtendEnum<StickerPackType>("SecuredBCPP");
            ((ITM_StickerPack)stickerPacket.item).ReflectionSetVariable("type", MiscPatches.securedPacket);

            yield return "Initializing v1.1 new stuff";
            // The Basement
            // But before we pity ourselves, we must create a new killer!!
            var prototypeBot = new NPCBuilder<PrototypeBot>(Info)
                .SetName("PrototypeBot-01")
                .SetEnum("PrototypeBot")
                .AddLooker()
                .AddTrigger()
                //.IgnoreBelts()
                .SetWanderEnterRooms()
                .IgnorePlayerOnSpawn()
                .AddSpawnableRoomCategories(RoomCategory.Hall, RoomCategory.Office)
                .SetPoster(ObjectCreators.CreatePosterObject(Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent" && x.isReadable), []))
                .SetMinMaxAudioDistance(50f, 300f)
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .IgnorePlayerVisibility()
                .SetForcedSubtitleColor(new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f))
                .SetMetaTags(["BCPP", "adv_exclusion_hammer_immunity"]) // Why do you not check the LevelObject from the SceneObject from the CoreGameManager??
                .Build(); //ObjectCreators.CreateNPC<PrototypeBot>("PrototypeBot-01", EnumExtensions.ExtendEnum<Character>("PrototypeBot"), ObjectCreators.CreatePosterObject(Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent"), []), spawnableRooms: [RoomCategory.Hall, RoomCategory.Office], maxAudioDistance: 400f);
            prototypeBot.audMan = prototypeBot.gameObject.GetComponent<AudioManager>();
            prototypeBot.spriteRenderer[0].sprite = bcppAssets.Get<Sprite>("PrototypeBot-01/Idle");
            prototypeBot.spriteRenderer[0].transform.localPosition = new Vector3(0f, 0f, 0f);
            prototypeBot.animator = prototypeBot.gameObject.AddComponent<CustomSpriteRendererAnimator>();
            prototypeBot.animator.renderer = prototypeBot.spriteRenderer[0];
            prototypeBot.animator.timeScale = TimeScaleType.Npc;
            prototypeBot.jumpscares = [
                new WeightedSoundObject()
                {
                    selection = bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_01"),
                    weight = 90,
                },
                new WeightedSoundObject()
                {
                    selection = bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_02"),
                    weight = 90,
                },
                new WeightedSoundObject()
                {
                    selection = bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_03"),
                    weight = 90,
                },
                new WeightedSoundObject()
                {
                    selection = bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_04"),
                    weight = 90,
                }
            ];
            prototypeBot.faststompSnd = bcppAssets.Get<SoundObject>("PrototypeBot-01/FastStomp");
            prototypeBot.impatientSnd = bcppAssets.Get<SoundObject>("PrototypeBot-01/Waiting");
            prototypeBot.ceilingclimbSnd = bcppAssets.Get<SoundObject>("PrototypeBot-01/Ceiling");
            prototypeBot.stunnedSnd = bcppAssets.Get<SoundObject>("PrototypeBot-01/Stunned");
            List<Sprite> idle01 = new List<Sprite>();
            for (int frame = 0; frame <= 11; frame++) // Idle
                idle01.Add(bcppAssets.Get<Sprite>("PrototypeBot-01/Idle_" + frame));
            List<Sprite> run01 = new List<Sprite>();
            for (int frame = 0; frame <= 7; frame++) // Running
                run01.Add(bcppAssets.Get<Sprite>("PrototypeBot-01/Running_" + frame));
            List<Sprite> climb01 = new List<Sprite>();
            for (int frame = 0; frame <= 17; frame++) // Climbing
                climb01.Add(bcppAssets.Get<Sprite>("PrototypeBot-01/Climbing_" + frame));
            List<Sprite> out01 = new List<Sprite>();
            for (int frame = 0; frame <= 11; frame++) // ClimbOut
                out01.Add(bcppAssets.Get<Sprite>("PrototypeBot-01/ClimbOut_" + frame));
            prototypeBot.animator.AddAnimation("Idle", new SpriteAnimation(8, idle01.ToArray()));
            prototypeBot.animator.AddAnimation("Running", new SpriteAnimation(34, run01.ToArray()));
            prototypeBot.animator.AddAnimation("Climbing", new SpriteAnimation(14, climb01.ToArray()));
            prototypeBot.animator.AddAnimation("ClimbOut", new SpriteAnimation(18, out01.ToArray()));
            // And a brand new game manager!!
            MainGameManager managRef = Resources.FindObjectsOfTypeAll<MainGameManager>().First();
            /* newManager = new GameObject("BCPP-Basement01_Manager");
            newManager.SetActive(false);*/
            BasementGameManager basementManager = new BaseGameManagerBuilder<BasementGameManager>()
                .SetNameKey("Mode_HideSeek")
                .SetObjectName("BCPP-Basement01_Manager")
                .SetLevelNumber(1)
                .SetNPCSpawnMode(GameManagerNPCAutomaticSpawn.OnSpawnExit)
                .SetGradeValue(2f)
                .Build();
            Ambience ambObj = Instantiate(managRef.GetComponentInChildren<Ambience>(), basementManager.transform);
            basementManager.ambience = ambObj;
            basementManager.overlay = Instantiate(Resources.FindObjectsOfTypeAll<Gum>().Last().transform.Find("GumOverlay").gameObject, basementManager.transform).GetComponent<Canvas>();
            basementManager.overlayAnimator = basementManager.overlay.gameObject.AddComponent<CustomImageAnimator>();
            basementManager.overlayAnimator.image = basementManager.overlay.transform.Find("Image").GetComponent<Image>();
            basementManager.overlayAnimator.useScaledTime = false;
            basementManager.overlayAnimator.image.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Last(x => x.name == "Transparent");
            List<Sprite> jump1 = new List<Sprite>();
            for (int frame = 0; frame <= 10; frame++) // Jumpscare 1
                jump1.Add(bcppAssets.Get<Sprite>("PrototypeBot-01/Jumpscare01_" + frame));
            List<Sprite> jump2 = new List<Sprite>();
            for (int frame = 0; frame <= 13; frame++) // Jumpscare 2
                jump2.Add(bcppAssets.Get<Sprite>("PrototypeBot-01/Jumpscare02_" + frame));
            basementManager.overlayAnimator.AddAnimation("Jump1", new SpriteAnimation(18, jump1.ToArray()));
            basementManager.overlayAnimator.AddAnimation("Jump2", new SpriteAnimation(18, jump2.ToArray()));
            basementManager.audMan = Instantiate(Resources.FindObjectsOfTypeAll<CoreGameManager>().Last(x => x.GetInstanceID() >= 0).musicMan, basementManager.transform, false);
            basementManager.audMan.useUnscaledPitch = true;
            //_disableSubtitles.SetValue(audMan, true);
            //audMan.audioSourceManager.dopplerLevel = 0f;

            // Also the time out event...
            B1TimeOut timeoutBasement = new RandomEventBuilder<B1TimeOut>(Info)
                .SetName("Event_B1TimeOut")
                .SetEnum("B1TimeOut")
                .SetSound(RandomEventMetaStorage.Instance.Get(RandomEventType.TimeOut).value.EventIntro)
                .SetJingle(RandomEventMetaStorage.Instance.Get(RandomEventType.TimeOut).value.EventJingleOverride)
                .SetMeta(RandomEventFlags.Special, "BCPP")
                .Build();

            lBasement = ScriptableObject.CreateInstance<CustomLevelObject>();
            // Make sure to not modify the ones you're unsure about!
            lBasement.name = "Basement1";
            //lBasement.previousLevels = [Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main1")];

            lBasement.minSize = new IntVector2(40, 45);
            lBasement.maxSize = new IntVector2(50, 55);
            lBasement.minPlots = 4;
            lBasement.maxPlots = 11;
            lBasement.minPlotSize = 3;
            lBasement.outerEdgeBuffer = 7;

            lBasement.minHallsToRemove = 3;
            lBasement.maxHallsToRemove = 6;
            lBasement.minSideHallsToRemove = 2;
            lBasement.maxSideHallsToRemove = 3;
            lBasement.minReplacementHalls = 5;
            lBasement.maxReplacementHalls = 7;
            lBasement.bridgeTurnChance = 4;
            lBasement.additionTurnChance = 9;
            lBasement.maxHallAttempts = 9; // THIS HAS HELPED ME, THANK GOD!
            lBasement.deadEndBuffer = 6;
            lBasement.includeBuffers = true;

            lBasement.hallWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").hallWallTexs.Clone() as WeightedTexture2D[];
            lBasement.hallFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").hallFloorTexs.Clone() as WeightedTexture2D[];
            lBasement.hallCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").hallCeilingTexs.Clone() as WeightedTexture2D[];
            lBasement.hallLights = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").hallLights.Clone() as WeightedTransform[];
            lBasement.maxLightDistance = 9;

            lBasement.minPrePlotSpecialHalls = 1;
            lBasement.minPostPlotSpecialHalls = 0;
            lBasement.maxPrePlotSpecialHalls = 4;
            lBasement.maxPostPlotSpecialHalls = 2;
            lBasement.prePlotSpecialHallChance = 0.5f;
            lBasement.postPlotSpecialHallChance = 0.25f;
            lBasement.potentialPrePlotSpecialHalls = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialPrePlotSpecialHalls;
            lBasement.potentialPostPlotSpecialHalls = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialPostPlotSpecialHalls;
            lBasement.standardHallBuilders = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").standardHallBuilders;

            lBasement.minSpecialBuilders = 2;
            lBasement.maxSpecialBuilders = 3;
            lBasement.forcedStructures = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").forcedStructures.Clone() as StructureWithParameters[];
            lBasement.forcedStructures = lBasement.forcedStructures.Where(x => x.prefab is not Structure_StudentSpawner).ToArray();
            lBasement.potentialStructures = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialStructures.Clone() as WeightedStructureWithParameters[];
            var vent = lBasement.potentialStructures[lBasement.potentialStructures.ToList().FindIndex(x => x.selection.prefab is Structure_Vent)];
            vent.selection = new StructureWithParameters()
            {
                parameters = new StructureParameters()
                {
                    minMax = [
                        new(7,11),
                        new(7,14),
                        new(25,30)
                        ]
                },
                prefab = vent.selection.prefab
            };
            /*lBasement.forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>())).parameters.prefab =
            lBasement.forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>())).parameters.prefab.AddToArray(new WeightedGameObject()
            {
                selection = inflockedSwingDoor.gameObject,
                weight = 10
            });*/
            //lBasement.forcedSpecialHallBuilders = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").forcedSpecialHallBuilders;
            var roomg = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").roomGroup.Where(x => x.name == "Class" || x.name == "Faculty" || x.name == "LockedRoom" || x.name == "Office").ToArray();
            lBasement.roomGroup = new RoomGroup[roomg.Length];
            for (int i = 0; i < roomg.Length; i++)
            {
                lBasement.roomGroup[i] = new RoomGroup()
                {
                    ceilingTexture = roomg[i].ceilingTexture.Clone() as WeightedTexture2D[],
                    wallTexture = roomg[i].wallTexture.Clone() as WeightedTexture2D[],
                    floorTexture = roomg[i].floorTexture.Clone() as WeightedTexture2D[],
                    light = roomg[i].light,
                    maxRooms = roomg[i].maxRooms,
                    minRooms = roomg[i].minRooms,
                    name = roomg[i].name,
                    potentialRooms = roomg[i].potentialRooms.Clone() as WeightedRoomAsset[],
                    stickToHallChance = roomg[i].stickToHallChance
                };
            }
            lBasement.roomGroup.First(x => x.name == "Class").potentialRooms = lBasement.roomGroup.First(x => x.name == "Class").potentialRooms.Where(x => x.selection.activity.prefab is MathMachine).ToArray();
            lBasement.roomGroup.First(x => x.name == "Class").stickToHallChance = 1f;
            lBasement.roomGroup.First(x => x.name == "Faculty").minRooms = 10;
            lBasement.roomGroup.First(x => x.name == "Faculty").maxRooms = 14;
            lBasement.roomGroup.First(x => x.name == "Faculty").stickToHallChance = 1f;
            lBasement.hallWallTexs = lBasement.hallWallTexs.AddToArray(new()
            {
                weight = 100,
                selection = lBasement.roomGroup.First(x => x.name.ToLower() == "faculty").wallTexture[0].selection
            });
            lBasement.roomGroup.First(x => x.name == "Office").minRooms = 3;
            lBasement.roomGroup.First(x => x.name == "Office").maxRooms = 6;
            lBasement.roomGroup.First(x => x.name == "LockedRoom").minRooms = lBasement.roomGroup.First(x => x.name == "LockedRoom").maxRooms = 3;
            /*lBasement.minClassRooms = 7;
            lBasement.maxClassRooms = 7;
            lBasement.classStickToHallChance = 1;
            lBasement.potentialClassRooms = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").potentialClassRooms;
            lBasement.minFacultyRooms = 10;
            lBasement.maxFacultyRooms = 14;
            lBasement.facultyStickToHallChance = 0.75f;
            lBasement.potentialFacultyRooms = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").potentialFacultyRooms;
            lBasement.minExtraRooms = 0;
            lBasement.maxExtraRooms = 0;
            lBasement.extraStickToHallChance = 0.75f;
            lBasement.potentialExtraRooms = [];
            lBasement.minOffices = 2;
            lBasement.maxOffices = 6;
            lBasement.officeStickToHallChance = 1;
            lBasement.potentialOffices = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").potentialOffices;*/
            lBasement.minRoomSize = new IntVector2(3, 5);

            lBasement.centerWeightMultiplier = 25;
            lBasement.perimeterBase = 4;
            lBasement.dijkstraWeightValueMultiplier = 0.3f;
            lBasement.dijkstraWeightPower = 1.6f;
            lBasement.extraDoorChance = 0.15f;
            lBasement.additionalHallDoorRequirementMultiplier = 4;
            lBasement.hallPriorityDampening = 3;

            /*lBasement.classWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").classWallTexs;
            lBasement.classFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").classFloorTexs;
            lBasement.classCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").classCeilingTexs;
            lBasement.facultyWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").facultyWallTexs;
            lBasement.facultyFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").facultyFloorTexs;
            lBasement.facultyCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").facultyCeilingTexs;
            lBasement.classLights = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").classLights;
            lBasement.facultyLights = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").facultyLights;
            lBasement.officeLights = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").officeLights;*/

            lBasement.standardDoorMat = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").standardDoorMat;
            lBasement.minSpecialRooms = 2;
            lBasement.maxSpecialRooms = 2;
            lBasement.specialRoomsStickToEdge = true;
            lBasement.potentialSpecialRooms = ((WeightedRoomAsset[])Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialSpecialRooms.Clone()).Where(x => x.selection.name.ToLower().Contains("cafet")).ToArray();
            //lBasement.windowChance = 0.5f;

            lBasement.lightMode = LightMode.Greatest;
            lBasement.standardLightStrength = 5;
            lBasement.standardLightColor = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").standardLightColor;
            lBasement.standardDarkLevel = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").standardDarkLevel;
            lBasement.forcedNpcs = [
            //NPCMetaStorage.Instance.Get(Character.LookAt).value,
                ];
            lBasement.posterChance = 2;
            lBasement.posters = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").posters;
            lBasement.potentialItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialItems,
                            new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfLock,
                                weight = 75
                            }
                        ];
            lBasement.forcedItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").forcedItems, profitCard,
                Resources.FindObjectsOfTypeAll<ItemObject>().Last(x => x.itemType == Items.Tape)];
            lBasement.maxItemValue *= 2;
            lBasement.singleEntranceItemVal = 10;
            lBasement.noHallItemVal = 15;
            lBasement.minEvents = 0;
            lBasement.maxEvents = 0;
            lBasement.initialEventGap = 0;
            lBasement.minEventGap = 60;
            lBasement.maxEventGap = 150;
            lBasement.randomEvents = [
                    /*new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().Last(x => x.name == "Event_Flood"),
                        weight = 150
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().Last(x => x.name == "Event_TestProcedure"),
                        weight = 100
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().Last(x => x.name == "Event_Fog"),
                        weight = 75
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().Last(x => x.name == "Event_BrokenRuler"),
                        weight = 100
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().Last(x => x.name == "Event_Party"),
                        weight = 75
                    }*/
            ];

            /*lBasement.fieldTrip = false;
            lBasement.fieldTrips = [];
            lBasement.tripEntrancePre = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").tripEntrancePre;
            lBasement.tripEntranceRoom = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Main3").tripEntranceRoom;
            lBasement.fieldTripItems = [];*/

            lBasement.exitCount = 4;
            lBasement.elevatorPre = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").elevatorPre;
            lBasement.elevatorRoom = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").elevatorRoom;
            lBasement.hallBuffer = 4;
            lBasement.edgeBuffer = 3;

            lBasement.mapPrice = 1000;
            lBasement.shopItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl2").shopItems,
                            new WeightedItemObject()
                            {
                                selection = profitCard,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            }];
            lBasement.totalShopItems = 6;
            lBasement.finalLevel = false;
            lBasement.timeBonusLimit = 780;
            lBasement.timeBonusVal = 1000;
            lBasement.timeLimit = 671;
            lBasement.timeOutEvent = timeoutBasement;
            sBasement = ScriptableObject.CreateInstance<SceneObject>();
            sBasement.name = "BasementLevel_1";
            sBasement.manager = basementManager; //Resources.FindObjectsOfTypeAll<MainGameManager>().Last(x => x.name.Contains("Lvl3"));
            sBasement.levelObject = lBasement;
            sBasement.skybox = Resources.FindObjectsOfTypeAll<Cubemap>().Last(x => x.name.Contains("DayStandard"));
            sBasement.skyboxColor = Color.black;
            sBasement.levelTitle = "B1";
            sBasement.levelNo = -1;
            sBasement.mapPrice = 1000;
            sBasement.shopItems = lBasement.shopItems;
            sBasement.storeUsesNextLevelData = false;
            sBasement.usesMap = true;
            sBasement.totalShopItems = 8;
            sBasement.nameKey = "Level_BasementBCPP";
            sBasement.potentialNPCs = new();
            sBasement.forcedNpcs = [prototypeBot];
            lBasement.type = LevelType.Schoolhouse;
            sBasement.potentialStickers = SceneObjectMetaStorage.Instance.Find(x => x.title == "F3" && x.number == 2).value.potentialStickers.Clone() as WeightedSticker[];
            GeneratorManagement.EnqueueGeneratorChanges(sBasement);
            sBasement.AddMeta(this, ["b-route"]);

            // And the button... You'll get what I'm doing.
            var buttemp = Resources.FindObjectsOfTypeAll<LockdownDoorBuilder>().Last().ReflectionGetVariable("buttonPre") as GameButton;
            GameButton button = Instantiate(buttemp, MTM101BaldiDevAPI.prefabTransform, false);
            button.gameObject.name = "Basement01Button";
            button.transform.localPosition = Vector3.zero;
            ButtonColorManager.CreateButtonColor("Proto01", new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f));
            button.ChangeColor("Proto01");
            FacultyButtonBuilderBCPP butbuilder = new GameObject("FacultyButtonBuilder_BCPP", typeof(FacultyButtonBuilderBCPP)).GetComponent<FacultyButtonBuilderBCPP>();
            butbuilder.gameObject.ConvertToPrefab(true);
            butbuilder.buttonPre = button;
            bcppAssets.Add<StructureBuilder>("ObjectBuilder/BasementButtonBuilder", butbuilder);

            yield return "Initializing v1.5 stuff...";
            // Aw shit it's pac man!
            GameObject bsodaClone = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().Last(x => x.name == "SodaMachine"), MTM101BaldiDevAPI.prefabTransform).gameObject;
            DestroyImmediate(bsodaClone.GetComponent<SodaMachine>());
            bsodaClone.name = "PAC-Machine";
            PAC_Machine pacmanGameMachine = bsodaClone.AddComponent<PAC_Machine>();
            pacmanGameMachine.gameObject.ConvertToPrefab(true);
            bsodaClone.GetComponent<MeshFilter>().mesh = PacModel.GetComponentInChildren<MeshFilter>().mesh;
            Destroy(PacModel);
            pacmanGameMachine.selection = (WeightedItemObject[])Resources.FindObjectsOfTypeAll<MysteryRoom>().Last().ReflectionGetVariable("items");
            var PacMat = Instantiate(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "Bookshelf"));
            PacMat.SetMainTexture(bcppAssets.Get<Texture2D>("PAC75/Machine"));
            bcppAssets.Add<Material>("PAC75/Machine", PacMat);
            PacMat = Instantiate(bcppAssets.Get<Material>("PAC75/Machine"));
            PacMat.SetMainTexture(bcppAssets.Get<Texture2D>("PAC75/MachineInactive"));
            bcppAssets.Add<Material>("PAC75/MachineInactive", PacMat);
            bsodaClone.GetComponent<MeshRenderer>().SetMaterialArray([bcppAssets.Get<Material>("PAC75/Machine")]);
            GenericHallBuilder arcadeBuilder = new GameObject("PacManHallBuilder", typeof(GenericHallBuilder)).GetComponent<GenericHallBuilder>();
            arcadeBuilder.obstacle = Obstacle.Null;
            ObjectPlacer wha = new ObjectPlacer()
            {
                eligibleShapes = TileShapeMask.Closed |
                TileShapeMask.Single |
                TileShapeMask.Straight |
                TileShapeMask.Corner |
                TileShapeMask.End,
                coverage = CellCoverage.North | CellCoverage.Down,
            };
            pacmanGameMachine.coverage = CellCoverage.North | CellCoverage.Down;
            pacmanGameMachine.tileShapeMask = TileShapeMask.Closed |
                TileShapeMask.Single |
                TileShapeMask.Straight |
                TileShapeMask.Corner |
                TileShapeMask.End;
            wha.ReflectionSetVariable("prefab", bsodaClone);
            wha.ReflectionSetVariable("max", 1);
            wha.ReflectionSetVariable("useWallDir", true);
            arcadeBuilder.ReflectionSetVariable("objectPlacer", wha);
            arcadeBuilder.gameObject.ConvertToPrefab(true);
            bcppAssets.Add<ObjectBuilder>("ObjectBuilder/PAC75EntranceBuilder", arcadeBuilder);
            var pellet = new GameObject("PacPellet", typeof(Pellet), typeof(SphereCollider)).GetComponent<Pellet>();
            pellet.GetComponent<SphereCollider>().radius = 4.8f;
            pellet.GetComponent<SphereCollider>().isTrigger = true;
            SpriteRenderer spriterenPel = new GameObject("Sprite", typeof(SpriteRenderer), typeof(PickupBob)).GetComponent<SpriteRenderer>();
            spriterenPel.transform.SetParent(pellet.transform, false);
            spriterenPel.material = ObjectCreators.SpriteMaterial;
            spriterenPel.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Last(x => x.name == "NotebookSheet_6");
            spriterenPel.gameObject.layer = LayerMask.NameToLayer("Billboard");
            pellet.gameObject.ConvertToPrefab(true);
            bcppAssets.Add("PAC75/Pellet", pellet);
            var superpellet = Instantiate(pellet);
            superpellet.gameObject.name = "PacSuperPellet";
            superpellet.gameObject.ConvertToPrefab(true);
            superpellet.GetComponentInChildren<SpriteRenderer>(true).sprite = Resources.FindObjectsOfTypeAll<Sprite>().Last(x => x.name == "AppleIcon_Large");
            superpellet.GetComponentInChildren<SpriteRenderer>(true).color = Color.yellow;
            bcppAssets.Add("PAC75/SuperPellet", superpellet);
            // Pac Manager
            /*newManager = new GameObject("BCPP-PAC75_Manager");
            newManager.SetActive(false);*/
            PacGameManager PacMan = new BaseGameManagerBuilder<PacGameManager>()
                .SetNPCSpawnMode(GameManagerNPCAutomaticSpawn.OnInitialize)
                .SetNameKey("Mode_PAC75")
                .SetObjectName("BCPP-PAC75_Manager")
                .Build();
            PacMan.beginPlayImmediately = true;
            //PacMan.CompleteMapOnReady();
            ambObj = Instantiate(managRef.GetComponentInChildren<Ambience>(), PacMan.transform);
            PacMan.ambience = ambObj;

            Texture2D darkestFloor = new Texture2D(1, 1);
            darkestFloor.SetPixel(1, 1, Color.black);
            GameObject ghostcontainer = new GameObject("ConnectedElevatorContainer", typeof(RoomFunctionContainer), typeof(PAC75Ghost_RoomContainer));
            ghostcontainer.ConvertToPrefab(true);
            {
                ghostcontainer.layer = LayerMask.NameToLayer("Ignore Raycast");
                ghostcontainer.GetComponent<RoomFunctionContainer>().ReflectionSetVariable("functions", new List<RoomFunction>()); // Had to manually fix it.
                ghostcontainer.GetComponent<RoomFunctionContainer>().AddFunction(ghostcontainer.GetComponent<PAC75Ghost_RoomContainer>());
            }
            //RoomFactory.CreateAssetsFromPath(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "PAC75_GhostSpawn.cbld"), 0, false, ghostcontainer.GetComponent<RoomFunctionContainer>(), keepTextures: false).First();

            reader = new BinaryReader(File.OpenRead(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "pac75_ghostspawn.rbpl")));
            var ghostroom = LevelImporter.CreateVanillaRoomAsset(BaldiRoomAsset.Read(reader));
            reader.Close();
            ghostroom.name = "PAC75_GhostSpawnroom";
            ((UnityEngine.Object)ghostroom).name = "pac75_ghostspawn";
            ghostroom.keepTextures = false;
            ghostroom.type = RoomType.Room;
            ghostroom.category = RoomCategory.Null;
            ghostroom.color = Color.white;
            ghostroom.windowChance = 0f;
            ghostroom.posterChance = 0f;
            ghostroom.minItemValue = 0;
            ghostroom.maxItemValue = 0;
            ghostroom.wallTex = bcppAssets.Get<Texture2D>("PAC75/Wall");
            ghostroom.ceilTex = Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent" && x.isReadable);
            ghostroom.florTex = darkestFloor;
            ghostroom.doorMats = Resources.FindObjectsOfTypeAll<StandardDoorMats>().ToList().Last(d => d.name == "ClassDoorSet");
            ghostroom.roomFunctionContainer = ghostcontainer.GetComponent<RoomFunctionContainer>();
            bcppAssets.Add<RoomAsset>("PAC75/GhostSpawnRoom", ghostroom);

            PAC75Ghost.craftersSprite = NPCMetaStorage.Instance.Get(Character.Crafters).value.spriteRenderer[0].sprite;

            var Red = new NPCBuilder<PAC75Ghost>(Info)
                .SetName("PAC75_Red")
                .SetEnum("Blinky")
                .AddLooker()
                .AddTrigger()
                .IgnorePlayerOnSpawn()
                .SetAirborne()
                .SetForcedSubtitleColor(Color.red)
                .SetMaxSightDistance(int.MaxValue)
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetMetaTags(["PAC75"])
                .Build();
            Red.audman = Red.GetComponent<AudioManager>();
            Red.Speed = 19f;
            Red.potentialRoomAssets = [new() { selection = ghostroom, weight = int.MaxValue }];

            var Cyan = new NPCBuilder<PAC75Ghost>(Info)
                .SetName("PAC75_Cyan")
                .SetEnum("Inky")
                .AddLooker()
                .AddTrigger()
                .IgnorePlayerOnSpawn()
                .SetAirborne()
                .SetForcedSubtitleColor(Color.cyan)
                .SetMaxSightDistance(int.MaxValue)
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetMetaTags(["PAC75"])
                .Build();
            Cyan.audman = Cyan.GetComponent<AudioManager>();
            Cyan.Speed = 15f;

            var Orange = new NPCBuilder<PAC75Ghost>(Info)
                .SetName("PAC75_Orange")
                .SetEnum("Cylde")
                .AddLooker()
                .AddTrigger()
                .IgnorePlayerOnSpawn()
                .SetAirborne()
                .SetForcedSubtitleColor(new Color(1f, 0.5f, 0f))
                .SetMaxSightDistance(int.MaxValue)
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetMetaTags(["PAC75"])
                .Build();
            Orange.audman = Orange.GetComponent<AudioManager>();
            Orange.Speed = 14f;

            var Pink = new NPCBuilder<PAC75Ghost>(Info)
                .SetName("PAC75_Pink")
                .SetEnum("Pinky")
                .AddLooker()
                .AddTrigger()
                .IgnorePlayerOnSpawn()
                .SetAirborne()
                .SetForcedSubtitleColor(new Color(1f, 0.5f, 1f))
                .SetMaxSightDistance(int.MaxValue)
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetMetaTags(["PAC75"])
                .Build();
            Pink.audman = Pink.GetComponent<AudioManager>();
            Pink.Speed = 19f;

            /*RoomAsset emptyRoom = ScriptableObject.CreateInstance<RoomAsset>(); // This is very stupid.
            emptyRoom.category = RoomCategory.Null;
            emptyRoom.type = RoomType.Null;
            emptyRoom.offLimits = true;
            emptyRoom.keepTextures = false;
            emptyRoom.posterChance = 0f;
            emptyRoom.maxItemValue = 0;
            emptyRoom.name = "Elevator_Null";*/

            // Todo: attempt to make it as a maze.
            lPac = Instantiate(Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3"));
            lPac.name = "PacManLevel";
            lPac.exitCount = 1;
            lPac.type = EnumExtensions.ExtendEnum<LevelType>("Pac75");
            //lPac.maxHallAttempts = 75;
            lPac.centerWeightMultiplier = 250;
            //lPac.elevatorRoom = emptyRoom;
            lPac.roomGroup = [];
            lPac.previousLevels = [];
            lPac.finalLevel = false;
            lPac.forcedNpcs = [];
            lPac.potentialSpecialRooms = [];
            lPac.minSpecialRooms = 0;
            lPac.maxSpecialRooms = 0;
            lPac.minEvents = 0;
            lPac.maxEvents = 0;
            lPac.minSize = new IntVector2(55, 55);
            lPac.maxSize = lPac.minSize;
            lPac.posters = [];
            lPac.posterChance = 0f;
            lPac.postPlotSpecialHallChance = 0f;
            lPac.minPostPlotSpecialHalls = 0;
            lPac.maxPostPlotSpecialHalls = 0;
            lPac.minPrePlotSpecialHalls = 0;
            lPac.maxPrePlotSpecialHalls = 0;
            lPac.prePlotSpecialHallChance = 0f;
            lPac.potentialPrePlotSpecialHalls = [];
            lPac.potentialPostPlotSpecialHalls = [];
            lPac.forcedSpecialHallBuilders = [];
            lPac.forcedStructures = [];
            lPac.potentialStructures = [];
            lPac.minSpecialBuilders = 0;
            lPac.maxSpecialBuilders = 0;
            lPac.specialHallBuilders = [];
            lPac.standardHallBuilders = [];
            lPac.minPlots = 5;
            lPac.maxPlots = 5;
            lPac.minPlotSize = 1;
            lPac.minRoomSize = new IntVector2(1, 1);
            lPac.minReplacementHalls = 5;
            lPac.maxReplacementHalls = 7;
            lPac.minHallsToRemove = 0;
            lPac.maxHallsToRemove = 0;
            lPac.minSideHallsToRemove = 0;
            lPac.maxSideHallsToRemove = 0;
            lPac.additionTurnChance = 1;
            lPac.bridgeTurnChance = 75;
            lPac.deadEndBuffer = 0;
            lPac.edgeBuffer = 0;
            lPac.outerEdgeBuffer = 2;
            lPac.hallBuffer = 5;
            lPac.includeBuffers = true;
            lPac.specialRoomsStickToEdge = false;
            lPac.hallCeilingTexs = [new() { selection = Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent" && x.isReadable), weight = int.MaxValue }];
            lPac.hallWallTexs = [new() { selection = bcppAssets.Get<Texture2D>("PAC75/Wall"), weight = int.MaxValue }];
            lPac.hallFloorTexs = [new() { selection = darkestFloor, weight = int.MaxValue }];
            lPac.standardLightColor = new Color(0f, 0.2862745f, 0.9098039f);
            lPac.standardLightStrength = 8;
            lPac.lightMode = LightMode.Greatest;
            lPac.timeLimit = 0;
            lPac.timeBonusLimit = 0;
            lPac.randomEvents = [];
            lPac.timeOutEvent = null;
            sPac = ScriptableObject.CreateInstance<SceneObject>();
            sPac.name = "PacManScene";
            sPac.skippable = true;
            sPac.levelNo = -3;
            sPac.usesMap = true;
            sPac.levelTitle = "PAC";
            sPac.levelObject = lPac;
            sPac.skybox = bcppAssets.Get<Cubemap>("PacSkybox");
            sPac.skyboxColor = new Color(0f, 0.2862745f, 0.9098039f);
            sPac.manager = PacMan;
            sPac.mapPrice = 0;
            sPac.nameKey = "PAC_75";
            sPac.totalShopItems = 0;
            sPac.storeUsesNextLevelData = false;
            sPac.potentialNPCs = [];
            sPac.forcedNpcs = [Red, Cyan, Orange, Pink];
            sPac.shopItems = [];
            sPac.baldiPrefab = null;
            sPac.AddMeta(this, ["secondary", "PAC75"]);
#if RELEASE
            yield return "Initializing Early Release Stuff...";
            sBoss = ScriptableObject.CreateInstance<SceneObject>();
            sBoss.name = "PlaceholderB";
            sBoss.skippable = false;
            sBoss.levelNo = -2;
            sBoss.usesMap = false;
            sBoss.levelTitle = "B2";
            sBoss.nameKey = "Level_BasementBCPP";
            sBoss.additionalNPCs = 0;
            sBoss.potentialNPCs = [];
            sBoss.shopItems = [];
            var placeholderending = Resources.FindObjectsOfTypeAll<SceneObject>().Last(x => x.name.Contains("Placeholder") && x.GetInstanceID() >= 0);
            sBoss.levelAsset = placeholderending.levelAsset;
            sBoss.extraAsset = placeholderending.extraAsset;
            sBoss.levelContainer = placeholderending.levelContainer;
            var placeholderman = new BaseGameManagerBuilder<BRouteEarlyEndingManager>()
                .SetObjectName("PlaceholderB")
                .SetNameKey(placeholderending.nameKey)
                .SetLevelNumber(2)
                .SetGradeValue(0f)
                .SetNPCSpawnMode(GameManagerNPCAutomaticSpawn.Never)
                .Build();
            sBoss.manager = placeholderman;
            sBasement.nextLevel = sBoss;
            sBoss.AddMeta(this, ["ending"]);
#else
            yield return "Initializing v1.6 stuff...";
            var platform = new GameObject("B2PlatformSideways", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            platform.GetComponent<MeshFilter>().mesh = bcppAssets.Get<Mesh>("B2Platform/Sideways");
            platform.GetComponent<MeshCollider>().sharedMesh = platform.GetComponent<MeshFilter>().mesh;
            platform.GetComponent<MeshRenderer>().SetMaterial(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "Vent"));
            platform.ConvertToPrefab(true);
            bcppAssets.Add<GameObject>("B2Platform/Sideways", platform);

            platform = new GameObject("B2PlatformCorner", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            platform.GetComponent<MeshFilter>().mesh = bcppAssets.Get<Mesh>("B2Platform/Corner");
            platform.GetComponent<MeshCollider>().sharedMesh = platform.GetComponent<MeshFilter>().mesh;
            platform.GetComponent<MeshRenderer>().SetMaterial(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "Vent"));
            platform.ConvertToPrefab(true);
            bcppAssets.Add<GameObject>("B2Platform/Corner", platform);

            platform = new GameObject("B2PlatformOneSide", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            platform.GetComponent<MeshFilter>().mesh = bcppAssets.Get<Mesh>("B2Platform/OneSide");
            platform.GetComponent<MeshCollider>().sharedMesh = platform.GetComponent<MeshFilter>().mesh;
            platform.GetComponent<MeshRenderer>().SetMaterial(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "Vent"));
            platform.ConvertToPrefab(true);
            bcppAssets.Add<GameObject>("B2Platform/OneSide", platform);

            platform = new GameObject("B2PlatformOpen", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            platform.GetComponent<MeshFilter>().mesh = bcppAssets.Get<Mesh>("B2Platform/Open");
            platform.GetComponent<MeshCollider>().sharedMesh = platform.GetComponent<MeshFilter>().mesh;
            platform.GetComponent<MeshRenderer>().SetMaterial(Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "Vent"));
            platform.ConvertToPrefab(true);
            bcppAssets.Add<GameObject>("B2Platform/Open", platform);

            LevelLoaderAdds.PlatformsB2Loader();

            GameObject deepspecialroomfunc = new GameObject("BCPP_DeepSpecialRoomFunction", typeof(RoomFunctionContainer), typeof(B2HallRoomFunction), typeof(SpecialRoomSwingingDoorsBuilder));
            deepspecialroomfunc.ConvertToPrefab(true);
            {
                deepspecialroomfunc.GetComponent<RoomFunctionContainer>().ReflectionSetVariable("functions", new List<RoomFunction>()); // Had to manually fix it.
                deepspecialroomfunc.GetComponent<RoomFunctionContainer>().AddFunction(deepspecialroomfunc.GetComponent<B2HallRoomFunction>());
                deepspecialroomfunc.GetComponent<RoomFunctionContainer>().AddFunction(deepspecialroomfunc.GetComponent<SpecialRoomSwingingDoorsBuilder>());
            }
            deepspecialroomfunc.GetComponent<SpecialRoomSwingingDoorsBuilder>().ReflectionSetVariable("swingDoorPre", Resources.FindObjectsOfTypeAll<SwingDoor>().Last(d => d.name == "Door_Swinging"));

            // Reasons why this mod now is dependent to the custom rooms mod.
            List<RoomAsset> specialrooms = new List<RoomAsset>();
            string[] roomPaths = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset"), "b2_special_*.rbpl");
            for (int i = 0; i < roomPaths.Length; i++)
            {
                reader = new BinaryReader(File.OpenRead(roomPaths[i]));
                var room = LevelImporter.CreateVanillaRoomAsset(BaldiRoomAsset.Read(reader), true);
                room.name = Path.GetFileNameWithoutExtension(roomPaths[i]);
                ((UnityEngine.Object)ghostroom).name = room.name;
                specialrooms.Add(room);
                reader.Close();
            }
            /*specialrooms.AddRange([RoomFactory.CreateAssetsFromPath(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "Special_B2Deep1.cbld"), 0, false, deepspecialroomfunc.GetComponent<RoomFunctionContainer>(), squaredShape: true).First(),
            RoomFactory.CreateAssetsFromPath(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "Special_B2Deep2.cbld"), 0, false, deepspecialroomfunc.GetComponent<RoomFunctionContainer>(), squaredShape: true).First(),
            RoomFactory.CreateAssetsFromPath(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "Special_B2Deep3.cbld"), 0, false, deepspecialroomfunc.GetComponent<RoomFunctionContainer>(), squaredShape: true).First()]);*/
            var thatlight = Resources.FindObjectsOfTypeAll<GameObject>().Last(x => x.name == "CordedHangingLight");
            foreach (var room in specialrooms)
            {
                room.category = RoomCategory.Special;
                room.wallTex = bcppAssets.Get<Texture2D>("Boss/BlueBrickWall");
                room.florTex = bcppAssets.Get<Texture2D>("Boss/Carpet");
                room.ceilTex = bcppAssets.Get<Texture2D>("Boss/Carpet");
                /*if (room.potentialDoorPositions.Count <= 0)
                    room.potentialDoorPositions.AddRange(room.forcedDoorPositions);*/
                room.cells.DoIf(cell => !room.entitySafeCells.Contains(cell.pos), cell => room.blockedWallCells.Add(cell.pos));
                room.cells.DoIf(cell => !room.entitySafeCells.Contains(cell.pos), cell => room.eventSafeCells.Remove(cell.pos));
                room.roomFunctionContainer = deepspecialroomfunc.GetComponent<RoomFunctionContainer>();
                room.lightPre = thatlight.transform;
            }
            GameObject DaButtonBase = new GameObject("ButtonBase");
            DaButtonBase.ConvertToPrefab(true);
            //var thebase = RoomFactory.CreateAssetsFromPath(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "Room_CarnellBase.cbld"), 0, false, null, false, false, null, false, true).First();
            reader = new BinaryReader(File.OpenRead(Path.Combine(AssetLoader.GetModPath(this), "RoomAsset", "b2_secretbase.rbpl")));
            var thebase = LevelImporter.CreateVanillaRoomAsset(BaldiRoomAsset.Read(reader));
            reader.Close();
            thebase.name = "SecretCarnellBase";
            ((UnityEngine.Object)thebase).name = "b2_secretbase";
            thebase.basicObjects.DoIf(x => x.prefab.GetComponent<TapePlayer>(), x => x.prefab = DaButtonBase.transform);
            thebase.category = RoomCategory.Null;
            thebase.type = RoomType.Room;
            thebase.doorMats = Resources.FindObjectsOfTypeAll<StandardDoorMats>().Last(x => x.name == "DoctorDoorSet");

            bcppAssets.AddRange([.. specialrooms, thebase],
                ["Special/B2SpecialRoom1",
                "Special/B2SpecialRoom2",
                "Special/B2SpecialRoom3",
                "Special/B2SpecialRoom4",
                "Room/BCarnellBase"]);

            // BOSS TIME!
            var bossleveltype = EnumExtensions.ExtendEnum<LevelType>("BCarnell");
            /*newManager = new GameObject("BCPP-BasementBoss_Manager");
            newManager.SetActive(false);*/
            CarnellBossManager bossManager = new BaseGameManagerBuilder<CarnellBossManager>()
                .SetObjectName("BCPP-BasementBoss_Manager")
                .SetGradeValue(5f)
                .SetLevelNumber(2)
                .SetNPCSpawnMode(GameManagerNPCAutomaticSpawn.OnSpawnExit)
                .Build();
            bossManager.CurrentLevel = 2;

            var realbcarnell = new NPCBuilder<BCarnellBoss>(Info)
                .SetName("Real B Carnell")
                .SetEnum("BCarnellBoss")
                .AddLooker()
                .AddTrigger()
                //.IgnoreBelts()
                .IgnorePlayerOnSpawn()
                .DisableNavigationPrecision()
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetPoster(ObjectCreators.CreatePosterObject(Resources.FindObjectsOfTypeAll<Texture2D>().Last(x => x.name == "Transparent" && x.isReadable), []))
                .SetMinMaxAudioDistance(10f, 300f)
                .SetWanderEnterRooms()
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .IgnorePlayerVisibility()
                .SetMetaTags(["BCPP", "adv_exclusion_hammer_immunity", "boss"])
                .Build();
            realbcarnell.animator = realbcarnell.gameObject.AddComponent<CustomSpriteRendererAnimator>();
            realbcarnell.animator.renderer = realbcarnell.spriteRenderer[0];
            realbcarnell.animator.timeScale = TimeScaleType.Npc;
            realbcarnell.animator.AddAnimation("Slap", new SpriteAnimation(bcppAssets.Get<Sprite[]>("BCarnellBoss/Walk"), 0.75f));
            realbcarnell.animator.AddAnimation("SlapIdle", new SpriteAnimation([bcppAssets.Get<Sprite[]>("BCarnellBoss/Walk").Last()], 1f));
            realbcarnell.animator.AddAnimation("Shoot", new SpriteAnimation(9 * 2, bcppAssets.Get<Sprite[]>("BCarnellBoss/SawShoot")));
            realbcarnell.animator.AddAnimation("BoostPre", new SpriteAnimation(bcppAssets.Get<Sprite[]>("BCarnellBoss/BoostPre"), 0.5f));
            realbcarnell.animator.AddAnimation("Boosting", new SpriteAnimation([bcppAssets.Get<Sprite>("BCarnellBoss/Boosting")], 1f));
            realbcarnell.animator.AddAnimation("BoostOut", new SpriteAnimation(bcppAssets.Get<Sprite[]>("BCarnellBoss/Walk"), 0.5f));
            realbcarnell.dialIntro = [
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_Intro1.wav"), "Vfx_BCarnell_Dial", SoundType.Voice, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_Intro2.wav"), "Vfx_BCarnell_Dial", SoundType.Voice, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_Intro3.wav"), "Vfx_BCarnell_Dial", SoundType.Voice, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_Intro4.wav"), "Vfx_BCarnell_Dial", SoundType.Voice, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_Intro5.wav"), "Vfx_BCarnell_Dial", SoundType.Voice, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Real B. Carnell", "BCBoss_Intro6.wav"), "Vfx_BCarnell_Dial", SoundType.Voice, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ];
            GameObject sawblade = Instantiate(Resources.FindObjectsOfTypeAll<ITM_BSODA>().Last(x => x.name == "ITM_BSODA"), MTM101BaldiDevAPI.prefabTransform).gameObject;
            sawblade.name = "Sawblade";
            Destroy(sawblade.GetComponent<ITM_BSODA>());
            sawblade.ConvertToPrefab(true);
            var sawbladeActual = sawblade.AddComponent<Sawblades>();
            sawbladeActual.spriteRenderer = sawblade.transform.Find("RendereBase").GetComponentInChildren<SpriteRenderer>();
            sawbladeActual.entity = sawblade.GetComponent<Entity>();
            sawblade.layer = LayerMask.NameToLayer("CollidableEntities"); // No wonder why it keeps setting to default...
            Destroy(sawblade.transform.Find("RendereBase").Find("Particles")?.gameObject);
            AudioManager brrr = sawblade.gameObject.AddComponent<AudioManager>();
            brrr.audioDevice = sawblade.gameObject.AddComponent<AudioSource>();
            _audOnStart.SetValue(brrr, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("BCarnellBoss/Sawblade")
            });
            _loopAudOnStart.SetValue(brrr, true);
            brrr.audioDevice.spatialBlend = 1;
            brrr.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            brrr.audioDevice.maxDistance = 150;
            brrr.audioDevice.dopplerLevel = 0;
            brrr.audioDevice.spread = 0;
            //sawblade.transform.Find("RendereBase").GetComponentInChildren<SpriteRenderer>().sprite = bcppAssets.Get<Sprite>("SiegeCanonCart/Ball");
            bcppAssets.Add<Entity>("Sawblade", sawblade.GetComponent<Entity>());
            realbcarnell.sawbladePre = sawblade;
            realbcarnell.stepSnd = bcppAssets.Get<SoundObject>("BCarnellBoss/Step");
            realbcarnell.boostCharge = bcppAssets.Get<SoundObject>("BCarnellBoss/BoostCharge");
            realbcarnell.boostGo = bcppAssets.Get<SoundObject>("BCarnellBoss/BoostGo");
            realbcarnell.sawShoot = bcppAssets.Get<SoundObject>("BCarnellBoss/Sawshoot");
            realbcarnell.loseSounds = [new WeightedSoundObject() { selection = bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/Bomb"), weight = 500 },
            new WeightedSoundObject() { selection = bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/Screech"), weight = 500 },
            new WeightedSoundObject() { selection = bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/Standard"), weight = 550 },
            new WeightedSoundObject() { selection = bcppAssets.Get<SoundObject>("BCarnellBoss/LOSE/Yello"), weight = 500}];
            realbcarnell.audMan = realbcarnell.gameObject.GetComponent<AudioManager>();

            SubInitializer subinit = new GameObject("SubGame Initializer", typeof(SubInitializer)).GetComponent<SubInitializer>();
            subinit.ecPre = Resources.FindObjectsOfTypeAll<EnvironmentController>().Last(x => x.name == "Environment Controller");
            subinit.generatorPre = Resources.FindObjectsOfTypeAll<LevelGenerator>().Last(x => x.name == "LevelGenerator");
            subinit.loaderPre = Resources.FindObjectsOfTypeAll<LevelLoader>().Last(x => x.name == "LevelLoader");
            subinit.gameObject.ConvertToPrefab(false);
            bcppAssets.Add<GameInitializer>("SubInitializer", subinit);

            lBoss = Instantiate(lBasement);
            lBoss.name = "BasementBoss";
            lBoss.type = bossleveltype;
            lBoss.potentialItems = lBoss.potentialItems.AddToArray(new()
            {
                selection = taserpulse,
                weight = 75
            });
            roomg = lBasement.roomGroup.Where(x => x.name.ToLower().Contains("facult") || x.name.ToLower().Contains("offic") || x.name == "LockedRoom").ToArray();
            lBoss.roomGroup = new RoomGroup[roomg.Length];
            for (int i = 0; i < roomg.Length; i++)
            {
                lBoss.roomGroup[i] = new RoomGroup()
                {
                    ceilingTexture = roomg[i].ceilingTexture.Clone() as WeightedTexture2D[],
                    wallTexture = roomg[i].wallTexture.Clone() as WeightedTexture2D[],
                    floorTexture = roomg[i].floorTexture.Clone() as WeightedTexture2D[],
                    light = roomg[i].light,
                    maxRooms = roomg[i].maxRooms,
                    minRooms = roomg[i].minRooms,
                    name = roomg[i].name,
                    potentialRooms = roomg[i].potentialRooms.Clone() as WeightedRoomAsset[],
                    stickToHallChance = roomg[i].stickToHallChance
                };
            }
            lBoss.roomGroup.First(x => x.name.ToLower().Contains("facult")).wallTexture = [new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/PlasticWall"),
                weight = 100
            }];
            lBoss.roomGroup.First(x => x.name.ToLower().Contains("facult")).floorTexture = [new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/Carpet"),
                weight = 100
            }];
            lBoss.roomGroup.First(x => x.name.ToLower().Contains("facult")).minRooms = 44;
            lBoss.roomGroup.First(x => x.name.ToLower().Contains("facult")).maxRooms = 67;
            //lBoss.windowChance = 0.2f;
            lBoss.roomGroup.First(x => x.name.ToLower().Contains("offic")).minRooms = lBoss.roomGroup.First(x => x.name.ToLower().Contains("offic")).maxRooms = 0;
            lBoss.roomGroup.First(x => x.name == "LockedRoom").minRooms = lBoss.roomGroup.First(x => x.name == "LockedRoom").maxRooms = 6;
            lBoss.minSpecialRooms = 3;
            lBoss.maxSpecialRooms = 4;
            lBoss.specialRoomsStickToEdge = false;
            lBoss.potentialSpecialRooms = lBoss.potentialSpecialRooms.AddRangeToArray([
            new WeightedRoomAsset()
            {
                selection = bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom1"),
                weight = 90
            },
            new WeightedRoomAsset()
            {
                selection = bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom2"),
                weight = 100
            },
            new WeightedRoomAsset()
            {
                selection = bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom3"),
                weight = 100
            },
            new WeightedRoomAsset()
            {
                selection = bcppAssets.Get<RoomAsset>("Special/B2SpecialRoom4"),
                weight = 90
            }]);
            lBoss.hallWallTexs = [new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/BlueBrickWall"),
                weight = 100
            }];
            lBoss.hallFloorTexs = [new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/Carpet"),
                weight = 100
            },
            new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/WoodPlanks"),
                weight = 100
            }];
            /*lBoss.facultyFloorTexs = [new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/WoodPlanks"),
                weight = 100
            }];
            lBoss.facultyWallTexs = [new WeightedTexture2D()
            {
                selection = bcppAssets.Get<Texture2D>("Boss/PlasticWall"),
                weight = 100
            }];
            lBoss.potentialPrePlotSpecialHalls = lBoss.potentialPrePlotSpecialHalls.AddRangeToArray([
                new WeightedRoomAsset()
                {
                    selection = bcppAssets.Get<RoomAsset>("Hall/ExposedSquaredView"),
                    weight = 90
                }
            ]);*/
            lBoss.additionTurnChance += 9;
            lBoss.bridgeTurnChance += 9;
            lBoss.minReplacementHalls += 8;
            lBoss.maxReplacementHalls += 11;
            lBoss.forcedStructures.First(x => x.parameters.prefab.ToList().Exists(x => x.selection.name.ToLower() == "DietSodaMachine".ToLower())).parameters.prefab = [.. lBoss.forcedStructures.First(x => x.parameters.prefab.ToList().Exists(x => x.selection.name.ToLower() == "DietSodaMachine".ToLower())).parameters.prefab.Where(x => x.selection.name.ToLower() != "DietSodaMachine".ToLower())];
            lBoss.potentialStructures = [.. lBoss.potentialStructures.Where(x => x.selection.prefab is not Structure_PowerLever)];
            lBoss.minPlotSize = 2;
            lBoss.minPlots = 14;
            lBoss.maxPlots = 28;
            lBoss.minPrePlotSpecialHalls = 10;
            lBoss.minPostPlotSpecialHalls = 7;
            lBoss.maxPrePlotSpecialHalls = 17;
            lBoss.maxPostPlotSpecialHalls = 15;
            lBoss.minSize = new IntVector2(60, 75);
            lBoss.maxSize = new IntVector2(90, 95);
            lBoss.finalLevel = true;
            lBoss.timeOutEvent = null;
            lBoss.maxItemValue = 900 * 4;
            lBoss.duplicateItemWeightReduction = 0.50f;

            sBoss = ScriptableObject.CreateInstance<SceneObject>();
            sBoss.name = "BasementLevel_Boss";
            sBoss.manager = bossManager;
            sBoss.levelObject = lBoss;
            sBoss.skybox = Resources.FindObjectsOfTypeAll<Cubemap>().Last(x => x.name.Contains("DayStandard"));
            sBoss.skyboxColor = Color.black;
            sBoss.nextLevel = Resources.FindObjectsOfTypeAll<SceneObject>().Last(x => x.name.Contains("Placeholder"));
            sBoss.levelTitle = "B2";
            sBoss.levelNo = -2;
            sBoss.mapPrice = 2000;
            sBoss.shopItems = lBoss.shopItems;
            sBoss.storeUsesNextLevelData = false;
            sBoss.usesMap = true;
            sBoss.totalShopItems = 8;
            sBasement.nextLevel = sBoss;
            sBoss.nameKey = "Level_BossBCPP";
            sBoss.potentialNPCs = [];
            sBoss.potentialStickers = sBasement.potentialStickers;
            sBoss.baldiPrefab = realbcarnell;
            sBoss.AddMeta(this, ["b-route"]);

            var partWhereJustHalls = Instantiate(lBoss);
            partWhereJustHalls.name = "BasementBoss_RushingHall";
            partWhereJustHalls.roomGroup = [];
            partWhereJustHalls.minSpecialRooms = 2;
            partWhereJustHalls.maxSpecialRooms = 4;
            partWhereJustHalls.minSize = new IntVector2(70, 95);
            partWhereJustHalls.minPlotSize = 0;
            partWhereJustHalls.minRoomSize = new IntVector2(0, 0);
            partWhereJustHalls.hallBuffer = 9;
            partWhereJustHalls.deadEndBuffer = 0;
            partWhereJustHalls.edgeBuffer = 0;
            partWhereJustHalls.additionTurnChance = 9;
            partWhereJustHalls.bridgeTurnChance = 9;
            partWhereJustHalls.exitCount = 1;
            //partWhereJustHalls.elevatorRoom = emptyRoom;
            partWhereJustHalls.potentialStructures = [];
            partWhereJustHalls.forcedStructures[partWhereJustHalls.forcedStructures.LastIndex(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>()))] = new StructureWithParameters()
            {
                prefab = Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>())).prefab,
                parameters = new StructureParameters()
                {
                    chance = [0.4f],
                    minMax = [Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>())).parameters.minMax[0]],
                    prefab = [Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>())).parameters.prefab[0]]
                }
            };
            partWhereJustHalls.minSpecialBuilders = 0;
            partWhereJustHalls.maxSpecialBuilders = 0;
            partWhereJustHalls.forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SodaMachine>())).parameters.minMax = [new(55, 71)];
            partWhereJustHalls.minPrePlotSpecialHalls = 17;
            partWhereJustHalls.minPostPlotSpecialHalls = 17;
            partWhereJustHalls.maxPrePlotSpecialHalls = 20;
            partWhereJustHalls.maxPostPlotSpecialHalls = 21;
            partWhereJustHalls.forcedItems = [ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard,
            ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard,
            ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard,
            ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard,
            ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard];
            partWhereJustHalls.potentialItems = [.. Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialItems.Where(i => i.selection.itemType != Items.NanaPeel && i.selection.itemType != Items.DoorLock)];

            var roomsDuh = Instantiate(lBoss);
            roomsDuh.name = "BasementBoss_Stealth";
            roomsDuh.roomGroup = [.. roomsDuh.roomGroup.Where(rg => !rg.name.ToLower().Contains("offic"))];
            foreach (var roomgroup in roomsDuh.roomGroup)
            {
                roomgroup.stickToHallChance = 0.5f;
                roomgroup.minRooms = 33;
                roomgroup.maxRooms = 77;
            }
            roomsDuh.centerWeightMultiplier = 150f;
            roomsDuh.minSize = new IntVector2(110, 110);
            roomsDuh.maxSize = roomsDuh.minSize;
            roomsDuh.minPlotSize = 3;
            roomsDuh.minRoomSize = new IntVector2(0, 0);
            roomsDuh.potentialStructures = [.. roomsDuh.potentialStructures.Where(s => s.selection.prefab.GetComponent<Structure_Vent>() || s.selection.prefab.GetComponent<Structure_HallDoor>())];
            roomsDuh.forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SodaMachine>())).parameters.minMax = [new(21,42)];
            roomsDuh.minSpecialBuilders = 2;
            roomsDuh.maxSpecialBuilders = 2;
            roomsDuh.minPlots = 44;
            roomsDuh.maxPlots = 88;
            roomsDuh.hallBuffer = 2;
            roomsDuh.deadEndBuffer = 9;
            roomsDuh.edgeBuffer = 9;
            roomsDuh.outerEdgeBuffer = 2;
            roomsDuh.additionTurnChance = 9;
            roomsDuh.bridgeTurnChance = 9;
            roomsDuh.exitCount = 1;
            roomsDuh.minSpecialRooms = 0;
            roomsDuh.maxSpecialRooms = 0;
            roomsDuh.minPrePlotSpecialHalls = 0;
            roomsDuh.minPostPlotSpecialHalls = 0;
            roomsDuh.maxPrePlotSpecialHalls = 0;
            roomsDuh.maxPostPlotSpecialHalls = 0;
            //roomsDuh.elevatorRoom = emptyRoom;
            roomsDuh.forcedItems = [ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard, ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value,
            ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard, ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value,
            ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, profitCard, ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value];
            roomsDuh.potentialItems = [.. Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialItems.Where(i => i.selection.itemType != Items.ChalkEraser && i.selection.itemType != Items.DietBsoda)];

            var thesilent = Instantiate(lBoss);
            thesilent.name = "BasementBoss_SuperLibrary";
            thesilent.potentialSpecialRooms = [.. Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialSpecialRooms.Where(rm => rm.selection.name.ToLower().Contains("libra"))];
            thesilent.specialRoomsStickToEdge = false;
            thesilent.minSpecialRooms = 10;
            thesilent.maxSpecialRooms = 20;
            thesilent.roomGroup = [.. thesilent.roomGroup.Where(rg => !rg.name.ToLower().Contains("offic"))];
            foreach (var roomgroup in thesilent.roomGroup)
            {
                roomgroup.stickToHallChance = 1f;
                roomgroup.minRooms = 15;
                roomgroup.maxRooms = 30;
            }
            thesilent.forcedStructures = [.. thesilent.forcedStructures.Where(s => s.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SodaMachine>()))];
            thesilent.hallBuffer = 10;
            thesilent.deadEndBuffer = 0;
            thesilent.edgeBuffer = 10;
            thesilent.outerEdgeBuffer = 6;
            thesilent.bridgeTurnChance = 10;
            thesilent.additionTurnChance = 5;
            thesilent.forcedItems = [ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value, ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value, ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value, ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value, ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value, ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value, ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value, ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value, ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value, ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value, ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value, ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value,];
            thesilent.potentialItems = [.. Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialItems.Where(i => i.selection.itemType != Items.AlarmClock && i.selection.itemType != Items.DietBsoda)];

            var deadends = Instantiate(lBoss);
            deadends.name = "BasementBoss_SuperF";
            deadends.roomGroup = [.. deadends.roomGroup.Where(rg => !rg.name.ToLower().Contains("offic"))];
            foreach (var roomgroup in deadends.roomGroup)
            {
                roomgroup.stickToHallChance = 1f;
                roomgroup.minRooms = 25;
                roomgroup.maxRooms = 30;
            }
            foreach (var objectbuilder in deadends.potentialStructures)
                objectbuilder.selection.parameters.minMax = [new(19, 19), new(19, 19), new(19, 19)];
            deadends.forcedStructures.First(struc => struc.parameters.prefab.ToList().Exists(x => x.selection.GetComponent<SodaMachine>())).parameters.minMax = [new(30, 42)];
            deadends.minSpecialBuilders = deadends.potentialStructures.Length;
            deadends.maxSpecialBuilders = deadends.potentialStructures.Length;
            deadends.hallBuffer = 10;
            deadends.deadEndBuffer = 10;
            deadends.edgeBuffer = 0;
            deadends.outerEdgeBuffer = 4;
            deadends.minSpecialRooms = 0;
            deadends.maxSpecialRooms = 0;
            deadends.additionTurnChance = 10;
            deadends.bridgeTurnChance = 10;
            deadends.forcedItems = [ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value,ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value,ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value,ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value,ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value,ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
            ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value,ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value, ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value];
            deadends.potentialItems = [.. Resources.FindObjectsOfTypeAll<LevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").potentialItems.Where(i => i.selection.itemType != Items.InvisibilityElixir && i.selection.itemType != Items.Teleporter && i.selection.itemType != Items.DietBsoda)];

            lsubBoss = [partWhereJustHalls, roomsDuh, thesilent, deadends];
            lBoss.roomGroup = lBoss.roomGroup.AddToArray(new RoomGroup()
            {
                potentialRooms = [new()
                {
                    selection = thebase,
                    weight = 99,
                }],
                light = lBoss.hallLights,
                wallTexture = lBoss.hallWallTexs,
                ceilingTexture = lBoss.hallCeilingTexs,
                floorTexture = lBoss.hallFloorTexs,
                maxRooms = 1,
                minRooms = 1,
                name = "BCarnellBase",
                stickToHallChance = 9f

            });
#endif
            yield return "Finalizing...";
            GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, ld) =>
            {
                switch (floorName)
                {
                    case "F1":
                        ld.potentialNPCs.AddRange([
                                new WeightedNPC()
                            {
                                selection = rpsguy,
                                weight = 80
                            }
                            ]);
                        ld.potentialStickers = ld.potentialStickers.AddRangeToArray([
                            new(StickerPatches.rpsSticker, 80),
                            new(StickerPatches.switcherooSticker, 25)
                            ]);
                        break;
                    case "F2" or "F4":
                        ld.potentialNPCs.AddRange([
                                new WeightedNPC()
                            {
                                selection = rpsguy,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = errorbot,
                                weight = 80
                            },
                            new WeightedNPC()
                            {
                                selection = siegecart,
                                weight = 140
                            }
                            ]);
                        ld.additionalNPCs += 1;
                        ld.shopItems = ld.shopItems.AddRangeToArray([
                                new WeightedItemObject()
                            {
                                selection = profitCard,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            }
                                ]);
                        ld.potentialStickers = ld.potentialStickers.AddRangeToArray([
                            new(StickerPatches.rpsSticker, 80),
                            new(StickerPatches.switcherooSticker, 25),
                            new(StickerPatches.stealerSticker, 100),
                            new(StickerPatches.anyportalSticker, 80)
                            ]);
                        break;
                    case "F3" or "F5":
                        ld.shopItems = ld.shopItems.AddRangeToArray([
                                new WeightedItemObject()
                            {
                                selection = profitCard,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = anyportal,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = taserpulse,
                                weight = 77
                            }
                                ]);
                        ld.potentialNPCs.AddRange([
                            new WeightedNPC()
                            {
                                selection = rpsguy,
                                weight = 120
                            },
                            new WeightedNPC()
                            {
                                selection = errorbot,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = siegecart,
                                weight = 140
                            },
                            new WeightedNPC()
                            {
                                selection = portalman,
                                weight = 150
                            },
                            new WeightedNPC()
                            {
                                selection = robber,
                                weight = 10
                            }
                        ]);
                        ld.additionalNPCs += 1;
                        ld.potentialStickers = ld.potentialStickers.AddRangeToArray([
                            new(StickerPatches.rpsSticker, 80),
                            new(StickerPatches.switcherooSticker, 30),
                            new(StickerPatches.stealerSticker, 95),
                            new(StickerPatches.anyportalSticker, 100)
                            ]);
                        break;
                    case "END":
                        ld.potentialNPCs.AddRange([
                                new WeightedNPC()
                            {
                                selection = rpsguy,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = errorbot,
                                weight = 90
                            },
                            new WeightedNPC()
                            {
                                selection = siegecart,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = portalman,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = robber,
                                weight = 30
                            }
                            ]);
                        ld.additionalNPCs += 2;
                        ld.potentialStickers = ld.potentialStickers.AddRangeToArray([
                            new(StickerPatches.rpsSticker, 90),
                            new(StickerPatches.switcherooSticker, 50),
                            new(StickerPatches.stealerSticker, 95),
                            new(StickerPatches.anyportalSticker, 150)
                            ]);
                        break;
                }
                foreach (var level in ld.GetCustomLevelObjects())
                {
                    if (level.IsModifiedByMod(Info)) continue;
                    if (level.forcedStructures.ToList().Exists(struc => struc.parameters?.prefab.ToList().Exists(x => x.selection.name.ToLower() == "PayPhone".ToLower()) == true))
                    {
                        var payphone = level.forcedStructures.First(struc => struc.parameters?.prefab.ToList().Exists(x => x.selection.name.ToLower() == "PayPhone".ToLower()) == true);
                        payphone.parameters.prefab = payphone.parameters.prefab.AddToArray(
                        new WeightedGameObject()
                        {
                            selection = gambling.gameObject,
                            weight = 75
                        });
                    }
                    switch (floorName)
                    {
                        case "F1":
                            level.forcedItems.Add(profitCard);
                            level.potentialItems = level.potentialItems.AddRangeToArray([
                                new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            }
                                ]);
#if DEBUG
                        level.forcedStructures = level.forcedStructures.AddToArray(new StructureWithParameters()
                        {
                            parameters = new()
                            {
                                chance = [],
                                minMax = [],
                                prefab = []
                            },
                            prefab = butbuilder
                        });
#else
                            level.potentialStructures = level.potentialStructures.AddToArray(new WeightedStructureWithParameters()
                            {
                                selection = new StructureWithParameters()
                                {
                                    parameters = new()
                                    {
                                        chance = [],
                                        minMax = [],
                                        prefab = []
                                    },
                                    prefab = butbuilder
                                },
                                weight = 111
                            });
                            level.maxSpecialBuilders++;
                            level.minSpecialBuilders = Mathf.Max(1, level.minSpecialBuilders);
#endif
                            break;
                        case "F2" or "F4":
                            level.forcedItems.Add(profitCard);
                            level.potentialItems = level.potentialItems.AddRangeToArray([
                                new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfLock,
                                weight = 75
                            }
                                ]);
                            break;
                        case "F3" or "F5":
                            level.forcedItems.Add(profitCard);
                            level.potentialItems = level.potentialItems.AddRangeToArray([
                                new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfLock,
                                weight = 75
                            },
                            new WeightedItemObject()
                            {
                                selection = anyportal,
                                weight = 90
                            },
                            new WeightedItemObject()
                            {
                                selection = taserpulse,
                                weight = 50
                            }
                                ]);
                            if (level.forcedStructures.ToList().Exists(struc => struc.parameters?.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>()) == true))
                            {
                                var halldoor = level.forcedStructures.First(struc => struc.parameters?.prefab.ToList().Exists(x => x.selection.GetComponent<SwingDoor>()) == true);
                                halldoor.parameters.prefab =
                            halldoor.parameters.prefab.AddToArray(new WeightedGameObject()
                            {
                                selection = inflockedSwingDoor,
                                weight = 10
                            });
                            }
                            break;
                        case "END":
                            level.forcedItems.Add(profitCard);
                            level.potentialItems = level.potentialItems.AddRangeToArray([
                                new WeightedItemObject()
                            {
                                selection = hammer,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfKey,
                                weight = 85
                            },
                            new WeightedItemObject()
                            {
                                selection = swingingdoorInfLock,
                                weight = 75
                            },
                            new WeightedItemObject()
                            {
                                selection = anyportal,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = taserpulse,
                                weight = 75
                            }
                                ]);
                            break;
                        default: // For floors that are from different mods
                            if (!level.forcedItems.Contains(profitCard) && floorName != "INF") level.forcedItems.Add(profitCard);
                            if (ld.potentialNPCs.ToList().Exists(x => x.selection.GetComponent<MrPortalMan>()) && floorName != "INF")
                            {
                                level.potentialItems = level.potentialItems.AddToArray(
                                new WeightedItemObject()
                                {
                                    selection = anyportal,
                                    weight = 90
                                });
                                ld.shopItems = ld.shopItems.AddToArray(
                                new WeightedItemObject()
                                {
                                    selection = anyportal,
                                    weight = 100
                                });
                            }
                            break;
                    }
                    if (floorName == "F2" || floorName == "F3")
                        level.forcedStructures = level.forcedStructures.AddToArray(new StructureWithParameters()
                        {
                            prefab = Resources.FindObjectsOfTypeAll<Structure_EnvironmentObjectPlacer>().Last(s => s.name == "Structure_EnvironmentObjectBuilder_Individual"),
                            parameters = new StructureParameters()
                            {
                                chance = [.1f],
                                minMax = [new(1, 1)],
                                prefab = [new()
                            {
                                selection = pacmanGameMachine.gameObject,
                                weight = 75
                            }]
                            }
                        });
                    level.MarkAsModifiedByMod(Info);
                }

            });
            GeneratorManagement.Register(this, GenerationModType.Finalizer, (name, num, ld) =>
            {
                if (ld == sBasement)
                {
                    var levelobject = ld.levelObject as CustomLevelObject;
                    ld.additionalNPCs = 0;
                    ld.potentialNPCs = [];
                    ld.forcedNpcs = ld == sBasement ? [prototypeBot] : [];
                    ld.levelObject.minEvents = 0;
                    ld.levelObject.maxEvents = 0;
                    if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbextracontent"))
                    {
                        levelobject.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_ExtraWindowsToSpawn", Resources.FindObjectsOfTypeAll<CustomLevelObject>().Last(x => x.name == "Schoolhouse_Lvl3").GetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_ExtraWindowsToSpawn"));
                        levelobject.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_GenConfig_DisableOutside", true);
                        levelobject.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_MathMachineNumballsMinMax", new IntVector2(12, 18));
                    }
                    if (ld == sBasement)
                    {
                        levelobject.roomGroup.First(x => x.name == "Class").potentialRooms = levelobject.roomGroup.First(x => x.name == "Class").potentialRooms.Where(x => x.selection.activity.prefab is MathMachine).ToArray();
                        levelobject.potentialSpecialRooms = levelobject.potentialSpecialRooms.Where(x => x.selection.name.ToLower().Contains("cafet")).ToArray();
                    }
                }
                else if (ld == sPac)
                {
                    var levelobject = ld.levelObject as CustomLevelObject;
                    ld.additionalNPCs = 0;
                    ld.potentialNPCs = [];
                    ld.forcedNpcs = [Red, Cyan, Orange, Pink];
                    levelobject.minEvents = 0;
                    levelobject.maxEvents = 0;
                    levelobject.forcedStructures = [];
                    levelobject.potentialStructures = [];
                    levelobject.minSpecialBuilders = 0;
                    levelobject.maxSpecialBuilders = 0;
                    levelobject.minSize = new IntVector2(55, 55);
                    levelobject.maxSize = levelobject.minSize;
                    if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbextracontent"))
                    {
                        levelobject.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_ExtraWindowsToSpawn", new List<object>());
                        levelobject.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_GenConfig_DisableOutside", true);
                        levelobject.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_MathMachineNumballsMinMax", new IntVector2(1, 1));
                    }
                }
            });
            ModdedSaveSystem.AddSaveLoadAction(this, (isSave, path) =>
            {
#if DEBUG
                if (BCPPSave.Instance.firstTime)
                {
                    Debug.LogWarning("FIRST TIME IN BCPP, NO PROBLEM!!");
                    BCPPSave.Instance.firstTime = false;
                }
#endif
                if (!isSave)
                    BCPPSave.Instance.Load();
                else
                    BCPPSave.Instance.Save();
            });

            if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.discordrpc"))
                DiscordRPCStuff.Init();

            if (Chainloader.PluginInfos.ContainsKey("mrsasha5.baldi.basics.plus.advanced"))
                AdvancedEditionAdds.AdvancedAdds();

            LevelLoaderAdds.AddToLoader();
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                LevelStudioAdds.EditorAdds();

            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.baldiarcade"))
                ArcadeGauntletAdds.EternityAdds();
#if DEBUG
            // I know my mistake that FasGuy made this for one of Missed the Texture's mods during the v0.3.X era, but I am using this for the "Link Data" menu.
            var logo = new Texture2D(1,1,TextureFormat.RGBA32,false);
            ImageConversion.LoadImage(logo, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAV0AAABFCAMAAADepMXHAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAFrUExURQAAAI8AAKUAALYAAMMAAMsAANAAANMAANQAANIAAL0AAMUAAKAAAKwAALUAALgAAH4AAJgAAK8AAMIAANoAAOAAAOQAAOcAAOgAAOkAAOYAAOUAAOEAALAAAMAAAM0AANYAAN0AAL8AAMgAAM4AAGIAAH0AAN8AAO4AAPIAAPQAAPUAAPYAAPEAAPMAAPAAAMkAAO8AAOoAANcAAPcAAPkAAKQAALsAANsAAOwAAO0AAIMAAJsAAM8AANEAAPgAAJ0AALcAANwAAN4AAJcAANUAAOIAAJIAAMcAANkAAMQAAIwAAKgAALIAALwAAP0AAPwAAPoAAKoAALMAAMYAAI0AAKsAANgAAP8AAP4AAMoAAJUAAMwAAPsAAK0AAHoAAJoAAKIAAJ4AAH8AAJ8AAMEAAOMAAOsAALoAAIsAAKEAALkAALEAAJQAAIgAAKcAAIoAALQAAHcAAJMAAK4AAP/HAMqcAJJwAAAAAKxzHvwAAAB5dFJOU////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////wClVIrsAAAACXBIWXMAAA7DAAAOwwHHb6hkAAAL5ElEQVR4Xu2Yi38U1RXHxxryMCBVSdg86CZAJU9CwqIJlUSTQgGBPEQhCrS1Cm2xtS+VP7/n8ZuZe889d3cTQ2X78RuZued3fufcuTezsxOLFy8Knxc5kBcgHTXo/rLa/88ocptLwBKBVA30owW9e393mdd+8Xrfsf6BQWGAGRIdngCRizeGj/UPkZPHSBwtRXH8xJsn/z9295dvvf1O/6mR0dONsbHG6XFhYtLZOTYXZ37VnJqeJHOjMTp9FBsgXZl6VBRnz52nIxw9Cy3h1+++daZv4EJjZnZufmF+bnZxcXb24sWlRrI4WfbJS1Mj40WxvMSelZe2u5enL9ARjp5F12JYaF250lqyi+PMe+83V0d5sHZ1Zmlx+Sh2l5r95oMzZ89x16J5/to6P3AmRyfoCEfvImtKmG9ttBad3f3wo+Ym3bjM6vSFycmto9nd354Zvk6tiNHRifHTdD49cYOOSGPUg+C6aQHrk1fnNngdNL44vzA3Y9ZE8u/evnlL7lzm9s1jfEJWkQSBsCuK4uM770zxrVoUcwwPRnmzNauItefAZRfF3UubKxd5d3ktF+fn6dZNdu74vSH+siOdjwqyAqQD7QW5Pzx57cay1G1v7+zsyojRbLH3iY57EL1suvwT90+tXOTFEHQDzfJZcoDCTz+7f6t8HHIawxoS9l4/O3ygvaCaE30PGjr1w/39HfoNK5oszpz9nF4f1Nxr6GXTIj4bXl2RDyV9qS3My2IlVULxF588Kj+wFewLeOPc0C0MPUyFhnv3ro1clfjhw4f7j6ELND5xtn+VXh8g9Bh62fQHxZNz02sLvMTdKxuteR5IpqIofv+Hk/0jsc62iMsXxtcwdLEVHJ9trjbko8K7u32F1ZKi+OPJR9P8NQehx5DLpqv/4pPmJD/79unJx7dPsh76o+PL9wfomxwxI76+5tD1KeLyrdWiGFueo7e5jbmllYmRTc5eZ2Sgf9ox9y81+4cG9C89+rd+amJGfq+8uzutaHOLJ32Xx+n7tcd39/i9wXG+gfjDGd8/oCj+9NW9wWh3yVU8+Xp982ljbGXl2bM13oWN3R36atrdmJ9d4nBF0PMY/X1CbNKr3Oh4g/4qpNcP6rE6Mj4zxzPy3A93aJ/RXvr/mZ4a8hoBrbfA7v5lb3j1mTwP+NMpL2YGvXefhuuk8V+fN6fH6LyxsXFFdmh7n4/0CXgs3/2kb1A3OenX1fLy8iL9Glut1oI+5lfWlvhNkODf7A6d0Z77n7l5qjHbovu5d3eXrr18MPAS98P7p0Keu+v8jh+s/oNvePVSRWi5HquQB3JU4fHu7uPHO8U+hfvbLBRz860r+ixird5dVva+Hhxdau3u0u9dxR4Du0sfenkw8BL3d+mNQWVGbBL97e6jLb5R6/XfHZ6aoL+YsX2cecj37v4+716ll0heBzgRvNt6w7PEu1s2BwuPd/gtTdUeQ7fx79/ePkVPR4LuKL5TSJaQUB+H/7jzzuXR6JXg88tPZ9hdbdY233u0Yzv1hnFGj5UiI7BNiDdKyXzvfcR/De5u0yWJ2Hs7rOv4eO/2LX7vKTboabjE96fI3z75pl4SxW/dPffgaUO/nISprfGrfMtXj1V9UZ5Zmp2TtwCW+alJT9mWPlsrZ0kt1COd/qsT985PN+T/ecg3Qs/u7muf3Z56+mx5cXFpeU02jzPFnbvvB0ti+ct/Dg+d2trafDDY3zx2rLlerF54Ot6Q9wJmTH5D9FqwJW8FqrHQaDTGNMRbRJax06Nbg9jdvefnpkZOr83MXF0bG50e6tXd/de/T/QNbE2MlS9Nurrizps6BBwUxfO+S81m89ilm/fv36SQXlxpM+Sttn6j7RvmN9pB0URYHyIbxxywXFFHOhocWH9EHpnt+H/uXRrcnLxxY3JrdeqaqD2GrsMgCbpTn2AMJNeJLm0d8LvgOnqHdB1lonj30zoqUUsbHE/nogR/KlZ7i+wl9+yKXiU67C6CnzkcP+/fy6S3dlc/UAKUV5vDXSVW6AADA4WBYkGWgaJAY6AQEEKQeXU5zBVibTlck4oRyJRAzegIEjT7o+jYJJrqQFMe4vIwWRs8k9YGQK9po2Pko2UB0GOQc2ibjJppqLqdxdEoFvVAoLYdjgnFNdBr8joGRfFdDRQBlSVQLcha2qSIOssjeXfX0M7iaBSLehC4Dit0kLYKFIZDlJfkHI6uQAhBJulNmEQcRWQTRJzjKBIcJZayjfNwMZbnIt0ZxO7mmjaBI9EFxAmaRWmI1W1ckU1oCkNBrKmCMQilTN92SDkW5yIGJoxRXCOq45BxqDOIXTiP2gApw1hIBEVkRyfSROJNBJEwojHOB4E7YmU+bGCCEKUhOYfRGYQ+bEBpgNRhLCSCIrJ3eZLCqMJ6OTamMPb7toc7YmUZZNKj211EBAQroTJEXBgrqUKQ4uqEb49FG0vDGq9tJ7hjsLQAqGZXeIjSkJzD6HVkZgtEVIaIBWMlVQhSXJ1wVePlMHKZCOeDwB2DpYVAhh4EKA3JOYxumwaUIgojxICxkioEKaKnPVzRqhzGMQaK06Aj3FGWm1lwpQcBSkNyDqPn5yKyzZN1ewprckgTqmIYEqtsCmJb4TXoBHfMrTjSgwClITmH0fNzEbne2iXOpAprckgTvkjEauxKKrwGneCO333/Pf9nm4e6b6rJOYxuAgfUxaSZVCFJj2lGRAxjIjmuTJvgfBC4Iy+YfqR7SKQHAUpDcg6jV8H3GiWgzJCmUoUknJKMaBjGRHJUmRb4HdrDHelmYqR7SKQHAUpDcg6jExqIBiUAVZYklwgEBCflSB6hzfF30SGBO8p608VChh4EKA3JOYwuEXY7AKnsFiS5RGCpGthcqrgENs/fRYcE7mjXqECtd6WKUBqScxidQWjRJMoMNmVjphLSZKq41LZ6FNBFhwTug/VlkKmOcHe96X7ggyRRFxNnJLLGWkjTieBT2WSQzoDzQeA2tDj8pP9VmwILD1EaknNYnQ8EpGqaYCoURkgiAokaM2dkSASf0iZnQtWKLjokcBtZGlZarRo/Mg8DjYcoDck5Ep2PCmmaqH5+YBmFEeKvgRph5ow8ieADm/7TcUgXHRK4yw8ErU3+CdVA1qt3XCCgNCTnSHU+RSAts3KshRHiw9gnTIs7tNs4g9rUKeO4posOCdwk2M1oi2UGnoOPSKiQkHM4Op8tYiB4LHUx4sHYJ0pbu40ziK00BkPQRYcEbhIszSE1aWVEzpHofKwevzXwZNqLBWMXMcQgw9g4g9hKXxQIXXRI4B7BylIckxTG5BxW5wOfkaoJTQniwNhFDAakCBPmiG0ShVVddEjgFsHKLK5JxRoScg6r84HPAaqEpgRxYOxistafLw91U2XCI99dWBITVMCK4DmMzugAeUHU2gQ1gFVHrvAvCQFhwoA2LgnDGOeDwB3arowxpgyhIyj0d5eAJzFBDhAHxh5J0hSYsKatS+JacBt0gBu0XRljTBngsBaopVyPQtSSuwaxYOyQ5mxBrj4xRa5YcBt0gBu0XRljTJZg4xyLUaOgRESCx5gzRDwYO6Q5KQhUE1ZEYmISoVK8+k5wffulEcZkqERYYg+0SjShAE1VzBkiJowdnJyt8BukntgkSik59R3h8vZLIwKTC+djywGUQOIIU0aIDeMUL2crbKzEkuMJpbS8M1zeYW2RyYXzDEIPONq2OfTuYhBiS7wORmKL8YgELSnvjFQHa3NbSAImFzEwiFOQP9zmeuuu8XNJSWqzAjtsJ9FUtKku4Fqsrf2tA4uPFPIhZ5McLrWtx19Cm1Qmp2okOzFGgA22kWiq2lRnuBKLY8pGMcbkoHV8dJ2aqC6VQCIACUxZA70CMoCoQDMlEJkgDoaMhDWeFnXqEhTWQI9AKkf1W5GIwY4JkKQxhgryDBRvdiQqIAOICjRTAlGApEATIJV4mp26G1BYAz0CqSzl7rYzSqMOreB5qWCqw2wVzgcBk5VANSDZjvZOZBVoFmRfWQ53gVgcA8UBhjzwuU5kApAIQOIV5qe5RGdzRAKQUpBnoLza/FRX2Ru78+N48eK/NpbcdsPszcUAAAAASUVORK5CYII="));
            logo.filterMode = FilterMode.Point;
            bcppAssets.Add<Sprite>("BCPPLogo", AssetLoader.SpriteFromTexture2D(logo, 1f));
            CustomOptionsCore.OnMenuInitialize += (instance, handler) => handler.AddCategory<DataLinkCategory>("BCPP Data Link");
#endif
        }
#if DEBUG
        class DataLinkCategory : CustomOptionsCategory
        {
            StandardMenuButton button;
            public override void Build()
            {
                var logo = bcppAssets.Get<Sprite>("BCPPLogo");
                CreateImage(logo, "Logo", new(0f, 65f), logo.rect.size / 2f);
                button = CreateTextButton(LinkData, "Apply", "Link Data!", new(0f, -155f), BaldiFonts.ComicSans24, TextAlignmentOptions.Bottom, new(132f, 32f), Color.black);
                CreateText("Info", "Opt_BCPPDataLink", new(0f, -25f), BaldiFonts.ComicSans12, TextAlignmentOptions.Center, new(196f, 32f), Color.black, true);
            }

            private void LinkData()
            {
                BCPPSave.Instance.LinkData();
                button.text.raycastTarget = false;
                button.text.text = "Linked!";
                MusicManager.Instance.PlaySoundEffect(Resources.FindObjectsOfTypeAll<SoundObject>().Last(x => x.name == "NotebookCollect"));
            }
        }
#endif
        private void PostLoad()
        {
            foreach(SodaMachine soda in Resources.FindObjectsOfTypeAll<SodaMachine>())
            {
                var profit = soda.gameObject.AddComponent<ToProfitCardCost>();
                profit.coverage = soda.coverage;
                profit.tileShapeMask = soda.tileShapeMask;
            }
            var basicobjects = LevelLoaderPlugin.Instance.basicObjects.Where(_object => _object.Value.GetComponent<SodaMachine>() != null).ToArray();
            foreach (var soda in basicobjects)
                LevelLoaderAdds.ConvertMachineToProfitAcceptable(soda.Key, soda.Value);
            // Sugaku characters should not appear for whatever reasons...
            sBasement.additionalNPCs = 0;
            lBasement.minEvents = 0;
            lBasement.maxEvents = 0;
            sPac.additionalNPCs = 0;
            sPac.potentialNPCs = [];
            lPac.minEvents = 0;
            lPac.maxEvents = 0;
            lPac.forcedStructures = [];
            lPac.potentialStructures = [];
            lPac.minSpecialBuilders = 0;
            lPac.maxSpecialBuilders = 0;
            lPac.minSize = new IntVector2(55, 55);
            lPac.maxSize = lPac.minSize;
            if (lPac is CustomLevelObject && Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.bbextracontent"))
            {
                var lpac = lPac as CustomLevelObject;
                lpac.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_ExtraWindowsToSpawn", new List<object>());
                lpac.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_GenConfig_DisableOutside", true);
                lpac.SetCustomModValue("pixelguy.pixelmodding.baldiplus.bbextracontent", "Times_EnvConfig_MathMachineNumballsMinMax", new IntVector2(1, 1));
            }
        }
    }

    static class RadarSupport
    {
        internal static ConfigEntry<Color> RPSGuyColor, ERRORBOTColor, SiegeCartColor, MrPortalManColor;

        internal static Color ProtoColor = new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f);

        internal static void ChangeRadarColor(this NPC me)
        {
            if (BCPPPlugin.RadarModExists && me.ec != null)
            {
                var color = 
                    me is RPSGuy ? RPSGuyColor.Value : 
                    me is ERRORBOT ? ERRORBOTColor.Value : 
                    me is SiegeCannonCart ? SiegeCartColor.Value : 
                    me is MrPortalMan ? MrPortalManColor.Value :
                    me is PrototypeBot ? ProtoColor : 
                    Color.clear;
                if (!me.ec.map.arrowTargets.Contains(me.Navigator.Entity))
                    me.ec.map.AddArrow(me.Navigator.Entity, color);
                else
                {
                    var arrows = me.ec.map.ReflectionGetVariable("arrows") as List<MapIcon>;
                    arrows[me.ec.map.arrowTargets.LastIndexOf(me.Navigator.Entity)].spriteRenderer.color = color;
                }
            }
        }
    }

    [Serializable]
    public class BCPPSave // Redo...
    {
        public static BCPPSave Instance => BCPPPlugin.Instance.SaveData;
        [NonSerialized] public ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("BCPPSave");
        [NonSerialized] public const string Version = "1.6.0", Build = "earlybird"; // BepinEx 5 does not support semantic versioning, which they have lied in their tutorial documentation until BepInEx 6 comes out.
        internal static Version typeVersion => new Version(Version);

        internal string version = Version, build = Build;
        internal bool basementCompleted, firstTime = true, death;

        public bool BasementCompleted => basementCompleted;
        public bool B2 => death;


        internal bool Load()
        {
            string path = Path.Combine(ModdedSaveSystem.GetCurrentSaveFolder(BCPPPlugin.Instance), "BCPP.dat");
            if (!File.Exists(path))
            {
                logger.LogWarning("Save data for B. Carnell Plus Pack does not exist, creating file now!");
                SetDefaults();
                Save();
                return false;
            }
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        string ver = reader.ReadString(); // Was too stupid to write scrambled integers inside the file...
                        if (new Version(ver) < new Version("1.5.1"))
                        {
                            logger.LogWarning("Save data for B. Carnell Plus Pack is in legacy format, it will now load without easy failure!");
                            /*if (new Version(ver) < typeVersion)
                            {
                                logger.LogWarning("Save data's mod version for B. Carnell Plus Pack is lower than the current mod version, setting to defaults!");
                                SetDefaults();
                                return false;
                            }*/
                            basementCompleted = reader.ReadBoolean();
                            firstTime = reader.ReadBoolean();
                            return true;
                        }
                    }
                }
            }
            catch
            {
                try
                {
                    var save = JsonUtility.FromJson<BCPPSave>(RijndaelEncryption.Decrypt(File.ReadAllText(path), "BCPP_" + PlayerFileManager.Instance.fileName));
                    basementCompleted = save.basementCompleted;
                    if (new Version(save.version) >= new Version("1.6.0"))
                        death = save.death;
                }
                catch
                {
                    logger.LogWarning("Save data for B. Carnell Plus Pack failed to load or is corrupted, setting to defaults!");
                    File.Copy(path, Path.Combine(ModdedSaveSystem.GetCurrentSaveFolder(BCPPPlugin.Instance), "BCPP_CorruptDat_" + DateTime.Now.ToString("s").Replace(":", "-")));
                    SetDefaults();
                    Save();
                    return false;
                }
            }
            return true;
        }

        internal void Save()
        {
            version = Version;
            build = Build;
            string path = Path.Combine(ModdedSaveSystem.GetCurrentSaveFolder(BCPPPlugin.Instance), "BCPP.dat");
            File.WriteAllText(path, RijndaelEncryption.Encrypt(JsonUtility.ToJson(this), "BCPP_" + PlayerFileManager.Instance.fileName));
        }

        internal void SetDefaults()
        {
            version = Version;
            build = Build;
            if (typeVersion >= new Version("1.1.0"))
                basementCompleted = false;
            if (typeVersion >= new Version("1.6.0"))
                death = false;
            firstTime = true;
        }
#if DEBUG
        internal void LinkData()
        {
            string path = Path.Combine(Directory.GetParent(Directory.GetParent(Application.persistentDataPath).ToString()).ToString(), "AlexBW145", "BCLink.dat");
            BCarnellDataLink.BCLinkedData data;
            if (File.Exists(path))
                data = JsonUtility.FromJson<BCarnellDataLink.BCLinkedData>(RijndaelEncryption.Decrypt(File.ReadAllText(path), "BCarnellLink"));
            else
                data = new BCarnellDataLink.BCLinkedData();
#if false
            logger.LogInfo(data.BCMACLinked);
            logger.LogInfo(data.BCPPLinked);
#endif
            data.BCPPLinked = true;
            File.WriteAllText(path, RijndaelEncryption.Encrypt(JsonUtility.ToJson(data), "BCarnellLink"));
        }
#endif
    }
}
#if DEBUG
namespace BCarnellDataLink
{
    [Serializable]
    internal class BCLinkedData
    {
        public bool BCMACLinked = false;
        public bool BCPPLinked = false;
    }
}
#endif