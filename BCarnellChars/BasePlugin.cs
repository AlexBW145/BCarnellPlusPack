using BepInEx;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.AssetTools;
using BCarnellChars.Characters;
using MTM101BaldAPI.Components;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Experimental.GlobalIllumination;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BCarnellChars.Patches;
using BCarnellChars.GeneratorStuff;
using System.Threading.Tasks;
using UnityEngine.Experimental.AI;
using BCarnellChars.OtherStuff;
using MonoMod.Utils;
using BCarnellChars.ItemStuff;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.SaveSystem;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.IO;
using MTM101BaldAPI.ObjectCreation;
using System.Collections;

namespace BCarnellChars
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellchars", "B. Carnell Chars", "1.0.4")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInIncompatibility("alexbw145.bbplus.rpsguy")] // This is a bad idea...
    [BepInProcess("BALDI.exe")]
    public class BasePlugin : BaseUnityPlugin
    {
        public static BasePlugin Instance;
        public static AssetManager bcppAssets = new AssetManager();
        // Makin' sure that it'll become colorful!
        public static bool RadarModExists = false;
        public const string radarID = "org.aestheticalz.baldi.characterradar";
        public static bool inLevelEditor = false;

        public LevelObject lBasement;
        public SceneObject sBasement;

        public static Material profitCardInsert;
        public static Material securedYellowSwingingDoor;

        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnellchars");
            Instance = this;
            harmony.PatchAllConditionals();

            // Custom Posters Mod
            if (Chainloader.PluginInfos.ContainsKey("io.github.luisrandomness.bbp_custom_posters"))
                Chainloader.PluginInfos["io.github.luisrandomness.bbp_custom_posters"].Instance.GetType().Assembly.GetType("LuisRandomness.BBPCustomPosters.CustomPostersPlugin").InvokeMember("AddBuiltInPackFromMod", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, [this, "Texture2D", "Posters"]);

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
                AssetLoader.TextureFromMod(this, "Texture2D", "SwingDoor_SecuredLocked.png")
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
                    "Obstacles/InfLockedSwingingDoor"
                ]);
            // Sprites
            bcppAssets.AddRange<Sprite>([
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_border.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Rock.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Paper.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Scissors.png"), 1f),

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "RPS Guy", "RPSDude.png"), 100),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "RPS Guy", "RPSDead.png"), 100),

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

                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Hammer_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Hammer_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "ProfitCard_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "ProfitCard_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorKey_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorKey_Large.png"), 50f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorInfLock_Small.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorInfLock_Large.png"), 50f)
                ],
                [
                    "RPSUI/Border",
                    "RPSUI/Rock",
                    "RPSUI/Paper",
                    "RPSUI/Scissors",

                    "RPSGuy/Alive",
                    "RPSGuy/Dead",

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

                    "Items/BHammer_Small",
                    "Items/BHammer_Large",
                    "Items/ProfitCard_Small",
                    "Items/ProfitCard_Large",
                    "Items/UnsecuredKey_Small",
                    "Items/UnsecuredKey_Large",
                    "Items/SecuredLock_Small",
                    "Items/SecuredLock_Large"
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
            // SoundObjects
            bcppAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 15.wav"), "Nothing", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 7.wav"), "Nothing", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 14.wav"), "Nothing", SoundType.Effect, Color.white),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_lose.wav"), "Vfx_RPS_lost", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "RPS_win.wav"), "Vfx_RPS_win", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f)),
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
                ],
                [
                    "RPSUI/HitTie",
                    "RPSUI/HitWin",
                    "RPSUI/HitLose",

                    "RPSGuy/LostGame",
                    "RPSGuy/WonGame",
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
                ]);
            bcppAssets.Get<SoundObject>("RPSUI/HitTie").subtitle = false;
            bcppAssets.Get<SoundObject>("RPSUI/HitWin").subtitle = false;
            bcppAssets.Get<SoundObject>("RPSUI/HitLose").subtitle = false;
            bcppAssets.Get<SoundObject>("ERRORBOT/Jammed").additionalKeys = [new SubtitleTimedKey() { key = "Sfx_ERRORBOT_Malfunction", time = 0.34f }];
            bcppAssets.Get<SoundObject>("ERRORBOT/Sprayed").additionalKeys = [new SubtitleTimedKey() { key = "Sfx_ERRORBOT_Malfunction", time = 0.34f }];

            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), false);
            ModdedSaveGame.AddSaveHandler(Info); // I hate it when the same ol' mistakes happen!
        }

        private IEnumerator PreLoad()
        {
            yield return 1;
            yield return "Loading...";
            if (MTM101BaldiDevAPI.Version < new Version("4.2.0.0"))
            {
                MTM101BaldiDevAPI.CauseCrash(Info, new Exception("BCPP crashed the mod loading screen because the API version is wrong.\n<color=white>Current API Version: " + MTM101BaldiDevAPI.Version.ToString() + "</color>\n<color=yellow>Required API Version: 4.2.0.0 or later</color>"));
                yield break;
            }
            RadarModExists = Chainloader.PluginInfos.ContainsKey(radarID);
            if (RadarModExists)
            {
                ConfigFile radarConfig = Chainloader.PluginInfos.ToList().Find(p => p.Key == radarID).Value.Instance.Config;
                RadarSupport.RPSGuyColor = radarConfig.Bind("CharacterColors", "RPSGuyColor", new Color(0.7176471f, 0.6941177f, 0.6235294f), "The color of RPS Guy's arrow.");
                RadarSupport.ERRORBOTColor = radarConfig.Bind("CharacterColors", "ERRORBOTColor", new Color(0.8f, 0.1647059f, 0.2f), "The color of ERROR-BOT_ITEM-STEALER's arrow.");
                RadarSupport.SiegeCartColor = radarConfig.Bind("CharacterColors", "SiegeCartColor", new Color(0.4196078f, 0.2901961f, 0.2117647f), "The color of Siege Canon Cart's arrow.");
                RadarSupport.MrPortalManColor = radarConfig.Bind("CharacterColors", "MrPortalManColor", new Color(1f, 0.6470588f, 0.1529412f), "The color of Mr. Portal Man's arrow.");

                if (RadarSupport.RPSGuyColor.Value == Color.clear)
                    Debug.LogError("RPSGuyColor is (0, 0, 0, 0). The map arrow will be fully clear.");
                if (RadarSupport.ERRORBOTColor.Value == Color.clear)
                    Debug.LogError("ERRORBOTColor is (0, 0, 0, 0). The map arrow will be fully clear.");
                if (RadarSupport.SiegeCartColor.Value == Color.clear)
                    Debug.LogError("MrPortalManColor is (0, 0, 0, 0). The map arrow will be fully clear.");
                if (RadarSupport.SiegeCartColor.Value == Color.clear)
                    Debug.LogError("MrPortalManColor is (0, 0, 0, 0). The map arrow will be fully clear.");
            }

            yield return "Initializing RPS Guy";
            // do dis
            var rpsguy = new NPCBuilder<RPSGuy>(Info)
                .SetName("RPS Guy")
                .SetEnum(EnumExtensions.ExtendEnum<Character>("RPSGuy"))
                .AddLooker()
                .AddTrigger()
                .SetMaxSightDistance(99f)
                .AddSpawnableRoomCategories(RoomCategory.Class, RoomCategory.Office, RoomCategory.Special)
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/RPSGuy"), []))
                .SetMetaTags(["BCPP"])
                .Build(); //ObjectCreators.CreateNPC<RPSGuy>("RPS Guy", EnumExtensions.ExtendEnum<Character>("RPSGuy"), ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/RPSGuy"), []), spawnableRooms: [RoomCategory.Class, RoomCategory.Office, RoomCategory.Special]);
            //AccessTools.DeclaredField(typeof(Navigator), "avoidRooms").SetValue(rpsguy.Navigator, true);
            rpsguy.Navigator.SetRoomAvoidance(true);
            //AccessTools.DeclaredField(typeof(Looker), "distance").SetValue(rpsguy.looker, 99f);
            PropagatedAudioManager annoying = rpsguy.gameObject.AddComponent<PropagatedAudioManager>();
            annoying.audioDevice = rpsguy.gameObject.AddComponent<AudioSource>();
            AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart").SetValue(annoying, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("RPSGuy/AmbientMusic")
            });
            AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart").SetValue(annoying, true);
            annoying.audioDevice.spatialBlend = 1;
            annoying.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            annoying.audioDevice.maxDistance = 150;
            annoying.audioDevice.dopplerLevel = 0;
            annoying.audioDevice.spread = 0;
            yield return "Initializing RPSUI";
            GameObject rockpaperscissors = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Jumprope>().ToList().Find(s => s.name == "Jumprope").gameObject);
            rockpaperscissors.name = "Rock Paper Scissors";
            Destroy(rockpaperscissors.GetComponent<Jumprope>());
            Destroy(rockpaperscissors.transform.Find("RopeCanvas").gameObject);
            Destroy(rockpaperscissors.transform.Find("TextCanvas").transform.Find("Count").gameObject);
            RawImage raw = rockpaperscissors.transform.Find("TextCanvas").gameObject.AddComponent<RawImage>();
            raw.color = new Color(0f, 0f, 0f, 0.50f); // Initial Color
            rockpaperscissors.ConvertToPrefab(false);
            Image uhf = new GameObject("Border", typeof(Image)).GetComponent<Image>();
            uhf.gameObject.layer = LayerMask.NameToLayer("UI");
            uhf.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            uhf.transform.localPosition = Vector3.zero;
            uhf.sprite = bcppAssets.Get<Sprite>("RPSUI/Border");
            uhf.transform.SetAsFirstSibling(); // IT COVERS, SHIT IT INTO THE FIRST SIBLING!!
            uhf.rectTransform.sizeDelta = new Vector2(rockpaperscissors.transform.Find("TextCanvas").GetComponent<RectTransform>().sizeDelta.x, uhf.rectTransform.sizeDelta.y + 40);
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
            iChose.GetComponent<Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name == "Transparent");
            GameObject theyChoose = new GameObject("Opponent_Chose", typeof(Image));
            theyChoose.layer = LayerMask.NameToLayer("UI");
            theyChoose.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            theyChoose.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0.5f);
            theyChoose.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
            theyChoose.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            theyChoose.transform.localScale = Vector3.one * 0.8f;
            theyChoose.GetComponent<RectTransform>().anchoredPosition = new Vector3(-110, 0, 0); // Pro tip: anchoredPosition helps.
            theyChoose.GetComponent<Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name == "Transparent");
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

            yield return "Initializing ERROR-BOT_ITEM-STEALER";
            var errorbot = new NPCBuilder<ERRORBOT>(Info)
                .SetName("ERROR-BOT_ITEM-STEALER")
                .SetEnum(EnumExtensions.ExtendEnum<Character>("ERRORBOT"))
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
            errorbot.spriteRenderer = errorbot.spriteRenderer.AddItem(new GameObject("Head", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>()).ToArray();
            errorbot.spriteRenderer[1].transform.SetParent(errorbot.spriteBase.transform);
            errorbot.spriteRenderer[1].gameObject.layer = errorbot.spriteRenderer[0].gameObject.layer;
            errorbot.spriteRenderer[1].transform.position = new Vector3(0f, 0.27f, 0f);
            errorbot.spriteRenderer[1].material = Resources.FindObjectsOfTypeAll<Material>().ToList().Find(m => m.name == "SpriteStandard_Billboard");
            errorbot.spriteRenderer[1].sprite = bcppAssets.Get<Sprite>("ERRORBOT/Head");
            PropagatedAudioManager staticbot = errorbot.gameObject.AddComponent<PropagatedAudioManager>();
            staticbot.audioDevice = errorbot.gameObject.AddComponent<AudioSource>();
            AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart").SetValue(staticbot, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("ERRORBOT/AmbientStatic")
            });
            AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart").SetValue(staticbot, true);
            staticbot.audioDevice.spatialBlend = 1;
            staticbot.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            staticbot.audioDevice.maxDistance = 150;
            staticbot.audioDevice.dopplerLevel = 0;
            staticbot.audioDevice.spread = 0;

            yield return "Initializing Siege Canon Cart";
            // This part is the reason why I almost gave up
            var siegecart = new NPCBuilder<SiegeCanonCart>(Info)
                .SetName("Siege Canon Cart")
                .SetEnum(EnumExtensions.ExtendEnum<Character>("SiegeCanonCart"))
                .AddLooker()
                .AddTrigger()
                .IgnoreBelts()
                .AddSpawnableRoomCategories(RoomCategory.Hall)
                .DisableNavigationPrecision()
                .DisableAutoRotation()
                .EnableAcceleration()
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/SiegeCannonCart"), []))
                .SetMetaTags(["BCPP"])
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
            AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart").SetValue(motorAudMan, new SoundObject[]
            {
                bcppAssets.Get<SoundObject>("SiegeCanonCart/Paddle")
            });
            AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart").SetValue(motorAudMan, true);
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
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(idleRotationMap, idleStaticFrames);
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
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(moveRotationMap, movingFrames.ToArray());
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
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(shootRotationMap, shootingFrames.ToArray());
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "spriteMap").SetValue(hell, new SpriteRotationMap[]
            {
                idleRotationMap,
                moveRotationMap,
                shootRotationMap
            });
            siegecart.spriteRenderer[0].transform.position = new Vector3(0, 0.75f, 0);
            SpriteRenderer nonvisible = Instantiate(siegecart.spriteRenderer[0]); // I copy this from the existing one to make it animated.
            nonvisible.transform.SetParent(siegecart.spriteBase.transform);
            nonvisible.gameObject.SetActive(false); // And then I later hide it!
            siegecart.spriteRenderer = siegecart.spriteRenderer.AddToArray(nonvisible);
            CustomSpriteAnimator animator = siegecart.gameObject.AddComponent<CustomSpriteAnimator>();
            siegecart.idleFrame = idleStaticFrames[0];
            siegecart.movingFrames = [movingFrames[0], movingFrames[8], movingFrames[8*2], movingFrames[8*3],
            movingFrames[8*4],movingFrames[8*5],movingFrames[8*6]];
            siegecart.shootingFrames = [shootingFrames[0], shootingFrames[8], shootingFrames[8*2], shootingFrames[8*3],
            shootingFrames[8*4],shootingFrames[8*5],shootingFrames[8*6],shootingFrames[8*7]];
            animator.spriteRenderer = nonvisible;
            siegecart.animator = animator;
            // The balls
            GameObject ball = Instantiate(Resources.FindObjectsOfTypeAll<ITM_BSODA>().ToList().Find(x => x.name == "ITM_BSODA")).gameObject;
            ball.name = "CanonBall";
            Destroy(ball.GetComponent<ITM_BSODA>());
            siegecart.ballPre = ball.AddComponent<SiegeCartBalls>();
            ball.ConvertToPrefab(true);
            ball.layer = LayerMask.NameToLayer("StandardEntities"); // No wonder why it keeps setting to default...
            Destroy(ball.transform.Find("RendereBase").Find("Particles").gameObject);
            ball.transform.Find("RendereBase").GetComponentInChildren<SpriteRenderer>().sprite = bcppAssets.Get<Sprite>("SiegeCanonCart/Ball");
            bcppAssets.Add<Entity>("SiegeCannonBall", ball.GetComponent<Entity>());

            yield return "Initializing Mr. Portal Man";
            var portalman = new NPCBuilder<MrPortalMan>(Info)
                .SetName("Mr Portal Man")
                .SetEnum(EnumExtensions.ExtendEnum<Character>("MrPortalMan"))
                .AddLooker()
                .AddTrigger()
                .IgnoreBelts()
                .SetAirborne()
                .DisableNavigationPrecision()
                .IgnorePlayerOnSpawn()
                .AddSpawnableRoomCategories(RoomCategory.Faculty)
                .SetPoster(ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/MrPortalMan"), []))
                .SetMetaTags(["BCPP"])
                .Build(); //ObjectCreators.CreateNPC<MrPortalMan>("Mr Portal Man", EnumExtensions.ExtendEnum<Character>("MrPortalMan"), ObjectCreators.CreatePosterObject(bcppAssets.Get<Texture2D>("PRI/MrPortalMan"), []), usesHeatMap: true, spawnableRooms: [RoomCategory.Hall]);
            //AccessTools.DeclaredField(typeof(Looker), "hasFov").SetValue(portalman.looker, false);
            AccessTools.DeclaredField(typeof(Looker), "layerMask").SetValue(portalman.looker, (LayerMask)AccessTools.DeclaredField(typeof(Looker), "layerMask").GetValue(NPCMetaStorage.Instance.Get(Character.Principal).value.looker));
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
            RenderTexture baseRendererTexture = Resources.FindObjectsOfTypeAll<RenderTexture>().ToList().Find(x => x.name == "AuthenticGameTexture");
            Camera mainCam = Resources.FindObjectsOfTypeAll<GameCamera>().First().transform.Find("MainCamera").GetComponent<Camera>();
            RenderTexture portalmanCamText = Instantiate(baseRendererTexture);
            portalmanCamText.name = "MrPortalManRenderCam";
            portalmanCamText.width = 256;
            portalmanCamText.height = 256;
            bcppAssets.Add<RenderTexture>("MrPortalMan/ManCamRender", portalmanCamText);
            RenderTexture outputportalCamText = Instantiate(baseRendererTexture);
            outputportalCamText.name = "OutputPortalRenderCam";
            outputportalCamText.width = 277;
            outputportalCamText.height = 356;
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
            //portalman.outputCamPre.cullingMask |= LayerMask.GetMask("Billboard");
            //portalman.outputCamPre.nearClipPlane = 0.01f;
            portalman.outputCamPre.useOcclusionCulling = false;
            Destroy(portalman.outputCamPre.gameObject.GetComponent<VA_AudioListener>());
            portalman.outputCamPre.enabled = false;
            portalman.outputCamPre.gameObject.SetActive(false);
            yield return "Initializing Mr. Portal Man (Portal Outputs)";
            // This is a room poster
            Material cryingportalMat = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "BlankChalk"));
            cryingportalMat.name = "CryingPortalMat";
            cryingportalMat.SetMainTexture(bcppAssets.Get<Texture2D>("MrPortalMan/OutputPortal"));
            Material cryingportalMask = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "PortalMask"));
            cryingportalMask.name = "CryingPortalMask";
            cryingportalMask.SetMaskTexture(bcppAssets.Get<Texture2D>("MrPortalMan/OutputPortalMask"));
            GameObject cryingPortal = Instantiate(Resources.FindObjectsOfTypeAll<Chalkboard>().ToList().First()).gameObject;
            cryingPortal.name = "CryingPortalOutput";
            cryingPortal.transform.Find("Chalkbaord").Find("Quad").GetComponent<MeshRenderer>().materials = [cryingportalMask, cryingportalMat];
            cryingPortal.transform.Find("Chalkbaord").Find("Quad").GetComponent<MeshCollider>().enabled = false;
            Destroy(cryingPortal.GetComponent<Chalkboard>());
            CryingPortal cry = cryingPortal.AddComponent<CryingPortal>();
            cry.mask = cryingportalMask;
            cryingPortal.ConvertToPrefab(true);
            portalman.portalPre = cryingPortal.GetComponent<CryingPortal>();

            /*NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [rpsguy], "RPS Guy", NPCFlags.Standard));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [errorbot], "ERROR-BOT_ITEM-STEALER", NPCFlags.Standard | NPCFlags.MakeNoise));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [siegecart], "Siege Canon Cart", NPCFlags.StandardNoCollide));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [portalman], "Mr Portal Man", NPCFlags.Standard));*/
            yield return "Finalizing NPC Initialization";
            bcppAssets.AddRange<NPC>([
                rpsguy,
                errorbot,
                siegecart,
                portalman,
                ],
                [
                "NPCs/RPS Guy",
                "NPCs/ERROR-BOT_ITEM-STEALER",
                "NPCs/Siege Cannon Cart",
                "NPCs/Mr Portal Man",
                ]);

            yield return "Initializing Unsafe Hammer";
            // Pretty sure this hammer came in before BBT, but I didn't really know how to add items to arrays back then...
            ItemObject hammer = new ItemBuilder(Info)
                .SetItemComponent<ITM_Hammer>()
                .SetNameAndDescription("Itm_BHammer", "Desc_BHammer")
                .SetSprites(bcppAssets.Get<Sprite>("Items/BHammer_Small"), bcppAssets.Get<Sprite>("Items/BHammer_Large"))
                .SetEnum(EnumExtensions.ExtendEnum<Items>("BHammer"))
                .SetShopPrice(203) // Makin' sure it blends into Time's hammer.
                .SetGeneratorCost(30)
                .SetMeta(ItemFlags.None, ["BCPP"])
                .Build(); //ObjectCreators.CreateItemObject("Itm_BHammer", "Desc_BHammer", bcppAssets.Get<Sprite>("Items/BHammer_Small"), bcppAssets.Get<Sprite>("Items/BHammer_Large"), EnumExtensions.ExtendEnum<Items>("BHammer"), 25, 30);
            yield return "Initializing Profit Card and its assets";
            // Annnddd, this is something stupidly uncool and sometimes creative...
            ItemObject profitCard = new ItemBuilder(Info)
                .SetItemComponent<ITM_Acceptable>()
                .SetNameAndDescription("Itm_ProfitCard", "Desc_ProfitCard")
                .SetSprites(bcppAssets.Get<Sprite>("Items/ProfitCard_Small"), bcppAssets.Get<Sprite>("Items/ProfitCard_Large"))
                .SetEnum(EnumExtensions.ExtendEnum<Items>("ProfitCard"))
                .SetShopPrice(285)
                .SetGeneratorCost(25)
                .SetMeta(ItemFlags.None, ["BCPP"])
                .Build(); //ObjectCreators.CreateItemObject("Itm_ProfitCard", "Desc_ProfitCard", bcppAssets.Get<Sprite>("Items/ProfitCard_Small"), bcppAssets.Get<Sprite>("Items/ProfitCard_Large"), EnumExtensions.ExtendEnum<Items>("ProfitCard"), 85, 25);
            profitCard.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("ProfitCard"));
            profitCard.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "CoinDrop"));
            profitCardInsert = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "BSODAMachine"));
            profitCardInsert.name = "ProfitCardMachine";
            profitCardInsert.SetMainTexture(bcppAssets.Get<Texture2D>("ProfitCardMachine"));
            yield return "Initializing Unsecured Key";
            // Something evil is brewing inside!
            ItemObject swingingdoorInfKey = new ItemBuilder(Info)
                .SetItemComponent<ITM_Acceptable>()
                .SetNameAndDescription("Itm_UnsecuredKey", "Desc_UnsecuredKey")
                .SetSprites(bcppAssets.Get<Sprite>("Items/UnsecuredKey_Small"), bcppAssets.Get<Sprite>("Items/UnsecuredKey_Large"))
                .SetEnum(EnumExtensions.ExtendEnum<Items>("UnsecuredYellowKey"))
                .SetShopPrice(1110)
                .SetGeneratorCost(50)
                .SetMeta(ItemFlags.MultipleUse | ItemFlags.Persists, ["BCPP"])
                .Build(); //ObjectCreators.CreateItemObject("Itm_UnsecuredKey", "Desc_UnsecuredKey", bcppAssets.Get<Sprite>("Items/UnsecuredKey_Small"), bcppAssets.Get<Sprite>("Items/UnsecuredKey_Large"), EnumExtensions.ExtendEnum<Items>("UnsecuredYellowKey"), 110, 50);
            swingingdoorInfKey.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey"));
            swingingdoorInfKey.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "Doors_StandardUnlock"));
            yield return "Initializing Secured Lock and Secured Locked Door builder";
            ItemObject swingingdoorInfLock = new ItemBuilder(Info)
                .SetItemComponent<ITM_Acceptable>()
                .SetNameAndDescription("Itm_SecuredLock", "Desc_SecuredLock")
                .SetSprites(bcppAssets.Get<Sprite>("Items/SecuredLock_Small"), bcppAssets.Get<Sprite>("Items/SecuredLock_Large"))
                .SetEnum(EnumExtensions.ExtendEnum<Items>("SecuredYellowLock"))
                .SetShopPrice(795)
                .SetGeneratorCost(75)
                .SetMeta(ItemFlags.None, ["BCPP"])
                .Build(); //ObjectCreators.CreateItemObject("Itm_SecuredLock", "Desc_SecuredLock", bcppAssets.Get<Sprite>("Items/SecuredLock_Small"), bcppAssets.Get<Sprite>("Items/SecuredLock_Large"), EnumExtensions.ExtendEnum<Items>("SecuredYellowLock"), 95, 75);
            swingingdoorInfLock.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock"));
            swingingdoorInfLock.item.GetComponent<ITM_Acceptable>().ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "Slap"));
            GameObject inflockedSwingDoor = Instantiate(Resources.FindObjectsOfTypeAll<CoinDoor>().ToList().Find(p => p.name == "Door_SwingingCoin")).gameObject;
            inflockedSwingDoor.name = "Door_SwingingSecuredLock";
            Destroy(inflockedSwingDoor.GetComponent<CoinDoor>());
            inflockedSwingDoor.AddComponent<SecuredSwingingDoor>();
            securedYellowSwingingDoor = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "CoinDoor"));
            securedYellowSwingingDoor.name = "SecuredLockDoor";
            securedYellowSwingingDoor.SetMainTexture(bcppAssets.Get<Texture2D>("Obstacles/InfLockedSwingingDoor"));
            //inflockedSwingDoor.GetComponent<SecuredSwingingDoor>().doorOverlay = securedYellowSwingingDoor;
            //inflockedSwingDoor.SetActive(false);
            inflockedSwingDoor.GetComponent<AudioManager>().enabled = false;
            inflockedSwingDoor.ConvertToPrefab(true);
            CoinDoorBuilder builder = Instantiate(Resources.FindObjectsOfTypeAll<CoinDoorBuilder>().ToList().Find(x => x.name == "CoinDoorBuilder"));
            builder.gameObject.name = "Inf Secured Swinging Door Builder";
            builder.ReflectionSetVariable("doorPre", inflockedSwingDoor.GetComponent<SwingDoor>());
            builder.obstacle = EnumExtensions.ExtendEnum<Obstacle>("InfLockedDoor");
            builder.gameObject.ConvertToPrefab(true);
            ObjectBuilderMetaStorage.Instance.Add(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor"), new ObjectBuilderMeta(Info, builder));
            bcppAssets.Add<SecuredSwingingDoor>("Obstacles/SecuredSwingingDoor", inflockedSwingDoor.GetComponent<SecuredSwingingDoor>());
            bcppAssets.Add<ObjectBuilder>("ObjectBuilder/SecuredSwingingDoor", builder);

            /*ItemMetaStorage.Instance.Add(hammer, new ItemMetaData(Info, hammer));
            ItemMetaStorage.Instance.Add(profitCard, new ItemMetaData(Info, profitCard));
            ItemMetaStorage.Instance.Add(swingingdoorInfKey, new ItemMetaData(Info, swingingdoorInfKey));
            ItemMetaStorage.Instance.Add(swingingdoorInfLock, new ItemMetaData(Info, swingingdoorInfLock));*/
            yield return "Finalizing Item Initialization";
            bcppAssets.AddRange<ItemObject>([
                hammer,
                profitCard,
                swingingdoorInfKey,
                swingingdoorInfLock
                ],
                [
                "Items/BHammer",
                "Items/ProfitCard",
                "Items/UnsecuredKey",
                "Items/SecuredLock"
                ]);

            yield return "";
            // The Basement
            lBasement = ScriptableObject.CreateInstance<LevelObject>();
            // Make sure to not modify the ones you're unsure about!
            lBasement.name = "Basement1";
            lBasement.previousLevels = [Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main1")];

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

            lBasement.hallWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallWallTexs;
            lBasement.hallFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallFloorTexs;
            lBasement.hallCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallCeilingTexs;
            lBasement.hallLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallLights;
            lBasement.maxLightDistance = 9;

            lBasement.minPrePlotSpecialHalls = 1;
            lBasement.minPostPlotSpecialHalls = 0;
            lBasement.maxPrePlotSpecialHalls = 4;
            lBasement.maxPostPlotSpecialHalls = 2;
            lBasement.prePlotSpecialHallChance = 0.5f;
            lBasement.postPlotSpecialHallChance = 0.25f;
            lBasement.potentialPrePlotSpecialHalls = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialPrePlotSpecialHalls;
            lBasement.potentialPostPlotSpecialHalls = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialPostPlotSpecialHalls;
            lBasement.standardHallBuilders = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardHallBuilders;

            lBasement.minSpecialBuilders = 4;
            lBasement.maxSpecialBuilders = 5;
            lBasement.forcedSpecialHallBuilders = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").forcedSpecialHallBuilders;
            lBasement.specialHallBuilders = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").specialHallBuilders,
                            new WeightedObjectBuilder()
                            {
                                selection = ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor")).value,
                                weight = 75
                            }];

            lBasement.minClassRooms = 7;
            lBasement.maxClassRooms = 7;
            lBasement.classStickToHallChance = 1;
            lBasement.potentialClassRooms = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialClassRooms;
            lBasement.minFacultyRooms = 10;
            lBasement.maxFacultyRooms = 14;
            lBasement.facultyStickToHallChance = 0.75f;
            lBasement.potentialFacultyRooms = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialFacultyRooms;
            lBasement.minExtraRooms = 0;
            lBasement.maxExtraRooms = 0;
            lBasement.extraStickToHallChance = 0.75f;
            lBasement.potentialExtraRooms = [];
            lBasement.minOffices = 1;
            lBasement.maxOffices = 1;
            lBasement.officeStickToHallChance = 1;
            lBasement.potentialOffices = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialOffices;
            lBasement.minRoomSize = new IntVector2(3, 5);

            lBasement.centerWeightMultiplier = 25;
            lBasement.perimeterBase = 4;
            lBasement.dijkstraWeightValueMultiplier = 0.3f;
            lBasement.dijkstraWeightPower = 1.6f;
            lBasement.extraDoorChance = 0.15f;
            lBasement.additionalHallDoorRequirementMultiplier = 4;
            lBasement.hallPriorityDampening = 3;

            lBasement.classWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classWallTexs;
            lBasement.classFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classFloorTexs;
            lBasement.classCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classCeilingTexs;
            lBasement.facultyWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyWallTexs;
            lBasement.facultyFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyFloorTexs;
            lBasement.facultyCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyCeilingTexs;
            lBasement.classLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classLights;
            lBasement.facultyLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyLights;
            lBasement.officeLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").officeLights;

            lBasement.standardDoorMat = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardDoorMat;
            lBasement.minSpecialRooms = 2;
            lBasement.maxSpecialRooms = 2;
            lBasement.specialRoomsStickToEdge = true;
            lBasement.potentialSpecialRooms = [
            new WeightedRoomAsset()
                        {
                            selection = Resources.FindObjectsOfTypeAll<RoomAsset>().ToList().Find(x => x.name.Contains("Cafeteria")),
                            weight = 100
                        }
        ];
            lBasement.windowChance = 0.5f;

            lBasement.lightMode = LightMode.Greatest;
            lBasement.standardLightStrength = 5;
            lBasement.standardLightColor = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardLightColor;
            lBasement.standardDarkLevel = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardDarkLevel;
            lBasement.potentialBaldis = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialBaldis;
            lBasement.additionalNPCs = 1;
            lBasement.potentialNPCs = [
            new WeightedNPC()
                {
                    selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value,
                    weight = 100
                },
                new WeightedNPC()
                {
                    selection = NPCMetaStorage.Instance.Get(Character.Sweep).value,
                    weight = 170
                },
                new WeightedNPC()
                {
                    selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value,
                    weight = 100
                },
                new WeightedNPC()
                {
                    selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value,
                    weight = 150
                }];
            lBasement.forcedNpcs = [
            NPCMetaStorage.Instance.Get(Character.LookAt).value,
                ];
            lBasement.posterChance = 2;
            lBasement.posters = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").posters;
            lBasement.potentialItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialItems,
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/SecuredLock"),
                                weight = 75
                            }
                        ];
            lBasement.forcedItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").forcedItems, bcppAssets.Get<ItemObject>("Items/ProfitCard")];
            lBasement.maxItemValue *= 2;
            lBasement.singleEntranceItemVal = 10;
            lBasement.noHallItemVal = 15;
            lBasement.minEvents = 2;
            lBasement.maxEvents = 5;
            lBasement.initialEventGap = 0;
            lBasement.minEventGap = 60;
            lBasement.maxEventGap = 150;
            lBasement.randomEvents = [
                new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().ToList().Find(x => x.name == "Event_Flood"),
                        weight = 150
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().ToList().Find(x => x.name == "Event_TestProcedure"),
                        weight = 100
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().ToList().Find(x => x.name == "Event_Fog"),
                        weight = 75
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().ToList().Find(x => x.name == "Event_BrokenRuler"),
                        weight = 100
                    },
                    new WeightedRandomEvent()
                    {
                        selection = Resources.FindObjectsOfTypeAll<RandomEvent>().ToList().Find(x => x.name == "Event_Party"),
                        weight = 75
                    }
            ];

            lBasement.fieldTrip = false;
            lBasement.fieldTrips = [];
            lBasement.tripEntrancePre = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").tripEntrancePre;
            lBasement.tripEntranceRoom = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").tripEntranceRoom;
            lBasement.fieldTripItems = [];

            lBasement.exitCount = 4;
            lBasement.elevatorPre = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").elevatorPre;
            lBasement.elevatorRoom = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").elevatorRoom;
            lBasement.hallBuffer = 4;
            lBasement.edgeBuffer = 3;

            lBasement.mapPrice = 1000;
            lBasement.shopItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main2").shopItems,
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/ProfitCard"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            }];
            lBasement.totalShopItems = 6;
            lBasement.finalLevel = true;
            lBasement.timeBonusLimit = 780;
            lBasement.timeBonusVal = 200;
            sBasement = ScriptableObject.CreateInstance<SceneObject>();
            sBasement.name = "BasementLevel_1";
            sBasement.manager = Resources.FindObjectsOfTypeAll<MainGameManager>().ToList().Find(x => x.name.Contains("Lvl3"));
            sBasement.levelObject = lBasement;
            sBasement.skybox = Resources.FindObjectsOfTypeAll<Cubemap>().ToList().Find(x => x.name.Contains("DayStandard"));
            sBasement.skyboxColor = Color.white;
            sBasement.nextLevel = Resources.FindObjectsOfTypeAll<SceneObject>().ToList().Find(x => x.name.Contains("Placeholder"));
            sBasement.levelTitle = "B1";
            sBasement.levelNo = -1;

            //Resources.FindObjectsOfTypeAll<SceneObject>().ToList().Find(x => x.name == "MainLevel_1").nextLevel = sBasement;

#if DEBUG
            lBasement.potentialNPCs.Add(
                new WeightedNPC()
                {
                    selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value,
                    weight = 150
                });
            /*var hi = 0;
            while (hi < 99)
            {
                lBasement.forcedNpcs = lBasement.forcedNpcs.AddRangeToArray([
                    NPCMetaStorage.Instance.Get(Character.Sweep).value
                    ]);
                hi++;
            }*/
#endif

            GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, ld) =>
            {
                switch (floorName)
                {
                    case "F1":
                        ld.forcedItems.Add(bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        ld.potentialItems = ld.potentialItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            }
                            ]);
                        ld.potentialNPCs.AddRange([
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value,
                                weight = 150
                            }
                        ]);
                        break;
                    case "F2":
                        ld.forcedItems.Add(bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        ld.potentialItems = ld.potentialItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/SecuredLock"),
                                weight = 75
                            }
                            ]);
                        ld.shopItems = ld.shopItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/ProfitCard"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            }
                            ]);
                        ld.potentialNPCs.AddRange([
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value,
                                weight = 200
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value,
                                weight = 140
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value,
                                weight = 140
                            }
                        ]);
                        ld.additionalNPCs += 1;
                        break;
                    case "F3":
                        ld.forcedItems.Add(bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        ld.potentialItems = ld.potentialItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/SecuredLock"),
                                weight = 75
                            }
                            ]);
                        ld.shopItems = ld.shopItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/ProfitCard"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 100
                            }
                            ]);
                        ld.potentialNPCs.AddRange([
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value,
                                weight = 200
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value,
                                weight = 140
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value,
                                weight = 140
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value,
                                weight = 150
                            }
                        ]);
                        ld.additionalNPCs += 1;
                        ld.specialHallBuilders = ld.specialHallBuilders.AddRangeToArray([
                            new WeightedObjectBuilder()
                            {
                                selection = ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor")).value,
                                weight = 75
                            }
                            ]);
                        break;
                    case "END":
                        ld.forcedItems.Add(bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        ld.potentialItems = ld.potentialItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/BHammer"),
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                                weight = 85
                            },
                            new WeightedItemObject()
                            {
                                selection = bcppAssets.Get<ItemObject>("Items/SecuredLock"),
                                weight = 75
                            }
                            ]);
                        ld.potentialNPCs.AddRange([
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value,
                                weight = 100
                            },
                            new WeightedNPC()
                            {
                                selection = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value,
                                weight = 100
                            }
                        ]);
                        ld.additionalNPCs += 2;
                        break;
                    default: // For floors that are from different mods
                        ld.forcedItems.Add(bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        break;
                }

            });
        }
    }

    class RadarSupport
    {
        public static ConfigEntry<Color> RPSGuyColor;

        public static ConfigEntry<Color> ERRORBOTColor;

        public static ConfigEntry<Color> SiegeCartColor;

        public static ConfigEntry<Color> MrPortalManColor;
    }
}
