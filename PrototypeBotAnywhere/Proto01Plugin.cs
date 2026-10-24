using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TeacherAPI;
using UnityEngine;
using UnityEngine.UI;

namespace PrototypeBotAnywhere
{
    [BepInPlugin("alexbw145.baldiplus.prototypebot", "PrototypeBot01 chases you down in every levels and kills every npc whose existence exists in those levels", "1.0.2")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "10.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.teacherapi", "0.3.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("BALDI.exe")]
    public class Proto01Plugin : BaseUnityPlugin
    {
        private ConfigEntry<int> chance;
        public static AssetManager assets01 = new AssetManager([..AssetManager.defaultIgnoreTypes,
            typeof(RenderTexture),
            typeof(Item),
            Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.bcarnellchars") ?
            AccessTools.TypeByName("BCarnellChars.Characters.PrototypeBot, " + Chainloader.PluginInfos["alexbw145.baldiplus.bcarnellchars"].Instance.GetType().Assembly.ToString())
            : typeof(object)]);
        private void Awake()
        {
            chance = Config.Bind("Settings", "PrototypeBot01Weight", 11, "Sets the selection weight for PrototypeBot-01 (Restart required!)");
            Harmony harmony = new Harmony("alexbw145.baldiplus.prototypebot");
            harmony.PatchAll();
            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                float protoSize = 9f;
                if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.bcarnellchars")) // Take assets from the existing BCPP asset manager
                {
                    AssetManager bcppAssets = Chainloader.PluginInfos["alexbw145.baldiplus.bcarnellchars"].Instance.GetType().GetField("bcppAssets", BindingFlags.Static | BindingFlags.Public).GetValue(null) as AssetManager;
                    for (int frame = 0; frame <= 10; frame++) // Jumpscare 1
                        assets01.Add<Sprite>("PrototypeBot-01/Jumpscare01_" + frame, bcppAssets.Get<Sprite>("PrototypeBot-01/Jumpscare01_" + frame));
                    for (int frame = 0; frame <= 13; frame++) // Jumpscare 2
                        assets01.Add<Sprite>("PrototypeBot-01/Jumpscare02_" + frame, bcppAssets.Get<Sprite>("PrototypeBot-01/Jumpscare02_" + frame));

                    for (int frame = 0; frame <= 11; frame++) // Idle
                        assets01.Add<Sprite>("PrototypeBot-01/Idle_" + frame, bcppAssets.Get<Sprite>("PrototypeBot-01/Idle_" + frame));
                    for (int frame = 0; frame <= 7; frame++) // Running
                        assets01.Add<Sprite>("PrototypeBot-01/Running_" + frame, bcppAssets.Get<Sprite>("PrototypeBot-01/Running_" + frame));
                    for (int frame = 0; frame <= 17; frame++) // Climbing
                        assets01.Add<Sprite>("PrototypeBot-01/Climbing_" + frame, bcppAssets.Get<Sprite>("PrototypeBot-01/Climbing_" + frame));
                    for (int frame = 0; frame <= 11; frame++) // ClimbOut
                        assets01.Add<Sprite>("PrototypeBot-01/ClimbOut_" + frame, bcppAssets.Get<Sprite>("PrototypeBot-01/ClimbOut_" + frame));
                    assets01.AddRange<SoundObject>([
                bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientIntro"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/AmbientLoop"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/TimeOut"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/TimeOutLoop"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_01"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_02"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_03"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_04"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/FastStomp"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Waiting"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Stunned"),
                bcppAssets.Get<SoundObject>("PrototypeBot-01/Ceiling")
                        ],
                        [
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
                        ]);
                }
                else // Create assets if BCPP plugin isn't loaded
                {
                    string bcppPath = Path.Combine(Application.streamingAssetsPath, "Modded", "alexbw145.baldiplus.bcarnellchars");
                    if (!Directory.Exists(bcppPath))
                    {
                        MTM101BaldiDevAPI.CauseCrash(Info, new System.Exception("Unable to load TeacherAPI character of PrototypeBot01: \n<color=white>BCPP assets directory does not exist! Please install it, put it into \"BALDI_Data/StreamingAssets/Modded/\", and boot up BBP again."));
                        return;
                    }
                    AssetLoader.LoadLocalizationFolder(Path.Combine(bcppPath, "Language", Language.English.ToString()), Language.English);
                    for (int frame = 0; frame <= 10; frame++) // Jumpscare 1
                        assets01.Add<Sprite>("PrototypeBot-01/Jumpscare01_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(bcppPath, "Texture2D", "NPCs", "PrototypeBot-01", "Jumpscares", "Jumpscare_1" + frame + ".png")), 1f));
                    for (int frame = 0; frame <= 13; frame++) // Jumpscare 2
                        assets01.Add<Sprite>("PrototypeBot-01/Jumpscare02_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(bcppPath, "Texture2D", "NPCs", "PrototypeBot-01", "Jumpscares", "Jumpscare_2" + frame + ".png")), 1f));

                    for (int frame = 0; frame <= 11; frame++) // Idle
                        assets01.Add<Sprite>("PrototypeBot-01/Idle_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(bcppPath, "Texture2D", "NPCs", "PrototypeBot-01", "Idle", "Idle" + frame + ".png")), protoSize));
                    for (int frame = 0; frame <= 7; frame++) // Running
                        assets01.Add<Sprite>("PrototypeBot-01/Running_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(bcppPath, "Texture2D", "NPCs", "PrototypeBot-01", "Running", "Running" + frame + ".png")), protoSize));
                    for (int frame = 0; frame <= 17; frame++) // Climbing
                        assets01.Add<Sprite>("PrototypeBot-01/Climbing_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(bcppPath, "Texture2D", "NPCs", "PrototypeBot-01", "Climbing", "Climbing" + frame + ".png")), protoSize));
                    for (int frame = 0; frame <= 11; frame++) // ClimbOut
                        assets01.Add<Sprite>("PrototypeBot-01/ClimbOut_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(bcppPath, "Texture2D", "NPCs", "PrototypeBot-01", "ClimbOut", "ClimbOut" + frame + ".png")), protoSize));
                    assets01.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_Intro.wav")), "Nothing", SoundType.Effect, Color.clear, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_IntroLoop.wav")), "Nothing", SoundType.Effect, Color.clear, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_TimeOver.wav")), "Nothing", SoundType.Effect, Color.clear, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_TimeOverLoop.wav")), "Nothing", SoundType.Effect, Color.clear, 0f),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill1.wav")), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill2.wav")), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill3.wav")), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "Jumpscares", "PB01_Kill4.wav")), "Sfx_PrototypeBot01_Jumpscare", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_FastRun.wav")), "Sfx_CartoonKnock_Trimmed", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_Waiting.wav")), "Sfx_Lose_Buzz", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_AntiHearStun.wav")), "Sfx_ShrinkMachine_Door", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(bcppPath, "AudioClip", "NPCs", "PrototypeBot-01", "PB01_Ceiling.wav")), "Sfx_Lose_Buzz", SoundType.Effect, Color.white)
                        ],
                        [
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
                        ]);;
                }
                var prototypeBot = new NPCBuilder<PrototypeBot01_Teacher>(Info)
                .SetName("PrototypeBot-01 (TeacherAPI)")
                .SetEnum("PrototypeBot")
                .AddLooker()
                .AddTrigger()
                //.IgnoreBelts()
                .SetWanderEnterRooms()
                .IgnorePlayerOnSpawn()
                .SetForcedSubtitleColor(new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f))
                .AddSpawnableRoomCategories(RoomCategory.Hall, RoomCategory.Office)
                .SetPoster(ObjectCreators.CreatePosterObject(Resources.FindObjectsOfTypeAll<Texture2D>().ToList().Last(x => x.name == "Transparent" && x.isReadable), []))
                .SetMinMaxAudioDistance(50f, 300f)
                .SetAudioTimescaleType(TimeScaleType.Npc)
                .IgnorePlayerVisibility()
                .SetMetaTags(["BCPP", "lethal", "adv_exclusion_hammer_immunity"]) // Why do you not check the LevelObject from the SceneObject from the CoreGameManager??
                .Build();
                prototypeBot.audMan = prototypeBot.gameObject.GetComponent<AudioManager>();
                prototypeBot.spriteRenderer[0].transform.localPosition = new Vector3(0f, 0f, 0f);
                prototypeBot.animator = prototypeBot.gameObject.AddComponent<CustomSpriteRendererAnimator>();
                prototypeBot.animator.renderer = prototypeBot.spriteRenderer[0];
                prototypeBot.animator.timeScale = TimeScaleType.Npc;
                prototypeBot.smoothMove = true;
                prototypeBot.loseSounds = [
                    new WeightedSoundObject()
                {
                    selection = assets01.Get<SoundObject>("PrototypeBot-01/Jumpscare_01"),
                    weight = 90,
                },
                new WeightedSoundObject()
                {
                    selection = assets01.Get<SoundObject>("PrototypeBot-01/Jumpscare_02"),
                    weight = 90,
                },
                new WeightedSoundObject()
                {
                    selection = assets01.Get<SoundObject>("PrototypeBot-01/Jumpscare_03"),
                    weight = 90,
                },
                new WeightedSoundObject()
                {
                    selection = assets01.Get<SoundObject>("PrototypeBot-01/Jumpscare_04"),
                    weight = 90,
                }
                ];
                prototypeBot.faststompSnd = assets01.Get<SoundObject>("PrototypeBot-01/FastStomp");
                prototypeBot.impatientSnd = assets01.Get<SoundObject>("PrototypeBot-01/Waiting");
                prototypeBot.ceilingclimbSnd = assets01.Get<SoundObject>("PrototypeBot-01/Ceiling");
                prototypeBot.stunnedSnd = assets01.Get<SoundObject>("PrototypeBot-01/Stunned");
                prototypeBot.slap = prototypeBot.faststompSnd;
                List<Sprite> idle01 = new List<Sprite>();
                for (int frame = 0; frame <= 11; frame++) // Idle
                    idle01.Add(assets01.Get<Sprite>("PrototypeBot-01/Idle_" + frame));
                List<Sprite> run01 = new List<Sprite>();
                for (int frame = 0; frame <= 7; frame++) // Running
                    run01.Add(assets01.Get<Sprite>("PrototypeBot-01/Running_" + frame));
                List<Sprite> climb01 = new List<Sprite>();
                for (int frame = 0; frame <= 17; frame++) // Climbing
                    climb01.Add(assets01.Get<Sprite>("PrototypeBot-01/Climbing_" + frame));
                List<Sprite> out01 = new List<Sprite>();
                for (int frame = 0; frame <= 11; frame++) // ClimbOut
                    out01.Add(assets01.Get<Sprite>("PrototypeBot-01/ClimbOut_" + frame));
                prototypeBot.animator.AddAnimation("Idle", new SpriteAnimation(8, idle01.ToArray()));
                prototypeBot.animator.AddAnimation("Running", new SpriteAnimation(34, run01.ToArray()));
                prototypeBot.animator.AddAnimation("Climbing", new SpriteAnimation(14, climb01.ToArray()));
                prototypeBot.animator.AddAnimation("ClimbOut", new SpriteAnimation(18, out01.ToArray()));
                prototypeBot.spriteRenderer[0].sprite = idle01[0];
                prototypeBot.overlay = Instantiate(Resources.FindObjectsOfTypeAll<Gum>().ToList().First().transform.Find("GumOverlay").gameObject, prototypeBot.transform).GetComponent<Canvas>();
                prototypeBot.overlay.transform.Find("Image").GetComponent<Image>().sprite = assets01.Get<Sprite>("PrototypeBot-01/Jumpscare");
                prototypeBot.overlayAnimator = prototypeBot.overlay.gameObject.AddComponent<CustomImageAnimator>();
                prototypeBot.overlayAnimator.image = prototypeBot.overlay.transform.Find("Image").GetComponent<Image>();
                prototypeBot.overlayAnimator.useScaledTime = false;
                List<Sprite> jump1 = new List<Sprite>();
                for (int frame = 0; frame <= 10; frame++) // Jumpscare 1
                    jump1.Add(assets01.Get<Sprite>("PrototypeBot-01/Jumpscare01_" + frame));
                List<Sprite> jump2 = new List<Sprite>();
                for (int frame = 0; frame <= 13; frame++) // Jumpscare 2
                    jump2.Add(assets01.Get<Sprite>("PrototypeBot-01/Jumpscare02_" + frame));
                prototypeBot.overlayAnimator.AddAnimation("Jump1", new SpriteAnimation(18, jump1.ToArray()));
                prototypeBot.overlayAnimator.AddAnimation("Jump2", new SpriteAnimation(18, jump2.ToArray()));
                prototypeBot.overlayAnimator.image.sprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Last(x => x.name == "Transparent");
                prototypeBot.ReplacementMusic = "mute";
                prototypeBot.ambientaudMan = prototypeBot.gameObject.AddComponent<AudioManager>();
                prototypeBot.ambientaudMan.audioDevice = prototypeBot.gameObject.AddComponent<AudioSource>();
                prototypeBot.ambientaudMan.useUnscaledPitch = true;
                var _disableSubtitles = AccessTools.DeclaredField(typeof(AudioManager), "disableSubtitles");
                _disableSubtitles.SetValue(prototypeBot.ambientaudMan, true);
                prototypeBot.ambientaudMan.audioDevice.dopplerLevel = 0f;
                prototypeBot.ambientaudMan.audioDevice.volume = 1f;
                TeacherPlugin.RegisterTeacher(prototypeBot);

                GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, ld) =>
                {
                    var meta = ld.GetMeta();
                    if (meta == null) return;
                    foreach (var levelObject in ld.GetCustomLevelObjects())
                    {
                        if (levelObject.IsModifiedByMod(Info)) continue;
                        if ((floorName != "B1" && floorNum != -1) || ld.name != "Basement1")
                            levelObject.AddPotentialTeacher(prototypeBot, chance.Value);
                        levelObject.MarkAsModifiedByMod(Info);
                    }
                });
            }, LoadingEventOrder.Pre);
        }
    }
}
