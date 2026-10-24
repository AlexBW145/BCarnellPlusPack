using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections.Generic;
using System.Linq;
using TeacherAPI;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellTeacher
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellteacher", "B. Carnell in Plus", "1.0.8")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "11.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.teacherapi", "0.3.0")]
    [BepInProcess("BALDI.exe")]
    public class CarnellPlugin : BaseUnityPlugin
    {
        public static AssetManager bcarnelAssets = new AssetManager();
        public BCarnell _BCarnell { get; private set; }
        public PoliceNerd _PoliceNerd { get; private set; }
        public static Character Nerd { get; private set; }

        internal static ConfigEntry<int> chance;

        private void Awake()
        {
            chance = Config.Bind("Settings", "BCarnellWeight", 100, "Sets the selection weight for B. Carnell (Restart required!)");
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnellteacher");
            harmony.PatchAllConditionals();
            TeacherPlugin.RequiresAssetsFolder(this);
            AssetLoader.LocalizationFromMod(this);

            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad, LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, MakeStudentsFaulty.GetMore, LoadingEventOrder.Final);
            ModdedSaveGame.AddSaveHandler(Info);
        }

        private void PreLoad()
        {
            // Assets
            float SpriteSize = 120f;
            for (int frame = 1; frame <= 6; frame++) // Walk Frames
            {
                bcarnelAssets.Add<Sprite>("BCarnell/Walk_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Walk", "Walk" + frame + ".png"), SpriteSize));
                bcarnelAssets.Add<Sprite>("BCarnell/WalkBrokenRuler_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "WalkBrokenRuler", "WalkBrokenRuler" + frame + ".png"), SpriteSize));
            }
            for (int frame = 0; frame <= 22; frame++) // Wave Frames
                bcarnelAssets.Add<Sprite>("BCarnell/Wave_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Wave", "Wave" + frame + ".png"), SpriteSize));
            for (int frame = 0; frame <= 24; frame++) // Jumpscare Frames
                bcarnelAssets.Add<Sprite>("BCarnell/JumpScare_" + frame, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Jumpscare", "BCarnellJumpScare" + frame + ".png"), 1f));
            bcarnelAssets.Add<Sprite>("BCarnell/GettingAngry", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "angry.png"), SpriteSize));
            bcarnelAssets.Add<Texture2D>("PRI_BCarnell", AssetLoader.TextureFromMod(this, "Texture2D", "PRI_BCarnell.png"));
            bcarnelAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Slap.wav"), "Sfx_BCarnell_Walk", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Broken.wav"), "Sfx_BrokenDagger", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Reequip.wav"), "Sfx_RegenDagger", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_angry.wav"), "Vfx_BCarnell_Angry", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_FinalFloor.wav"), "Vfx_BCarnell_FinalFloor", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_bomb.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_screech.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_standard.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f)),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_yello.wav"), "Sfx_Lose_Buzz", SoundType.Effect, new Color(0.01960784f, 0.3607843f, 0.7568628f))
                ],
                [
                    "BCarnell/Slap",
                    "BCarnell/BrokenDagger",
                    "BCarnell/RegenDagger",
                    "BCarnell/Angry",
                    "BCarnell/FinalFloor",

                    "BCarnell/Lose/Bomb",
                    "BCarnell/Lose/Screech",
                    "BCarnell/Lose/Standard",
                    "BCarnell/Lose/Yello"
                ]);
            float NerdSize = 62f; // Came from a stock image
            bcarnelAssets.AddRange<Sprite>([
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NerdMan.png"), NerdSize),
                AssetLoader.SpriteFromMod(this, Vector3.one/2f, NerdSize, "Texture2D", "NerdMan_Netural.png"),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NerdCar.png"), NerdSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "InCar.png"), 1f)
                ],
                [
                    "PoliceNerd/Scolding",
                    "PoliceNerd/Idle",
                    "PoliceNerd/CarManslaughter",
                    "PoliceNerd/HUDCar"
                ]);
            bcarnelAssets.AddRange<SoundObject>([
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Car", "PN_CarDisabled.wav"), "Sfx_BCarnell_CarOut", SoundType.Effect, Color.white),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Car", "PN_CarEnabled.wav"), "Sfx_PoliceNerd_CarEnabled", SoundType.Effect, Color.white),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PN_Horn.wav"), "Sfx_PoliceNerd_GoofyHorn", SoundType.Effect, Color.white)
                    ],
                    [
                        "PoliceNerd/CarOut",
                        "PoliceNerd/CarEnabled",
                        "PoliceNerd/GoofyHorn"
                    ]);
            bcarnelAssets.Add<Texture2D>("PRI_PoliceNerd", AssetLoader.TextureFromMod(this, "Texture2D", "PRI_PoliceNerd.png"));
            float notebookSize = 100f; // How do I color the sprite render?
            bcarnelAssets.Add<Sprite[]>("BCarnell/Binders",
                [
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder1.png"), notebookSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder2.png"), notebookSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder3.png"), notebookSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder4.png"), notebookSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder5.png"), notebookSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder6.png"), notebookSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Binders", "Binder7.png"), notebookSize)
                ]);
            bcarnelAssets.AddRange<string>([
                AssetLoader.MidiFromMod("mus_bcarnellschool", this, "MIDI", "mus_schoolbcarnell.mid"),
                AssetLoader.MidiFromMod("mus_alphabetfleeStart", this, "MIDI", "mus_alphabet-fleeStart.mid"),
                AssetLoader.MidiFromMod("mus_alphabetfleePhase1", this, "MIDI", "mus_alphabet-flee01.mid"),
                AssetLoader.MidiFromMod("mus_alphabetfleePhase2", this, "MIDI", "mus_alphabet-flee02.mid"),
                AssetLoader.MidiFromMod("mus_alphabetfleePhase3", this, "MIDI", "mus_alphabet-flee03.mid"),
                AssetLoader.MidiFromMod("mus_alphabetfleePhase4", this, "MIDI", "mus_alphabet-flee04.mid")
                ],
                [
                    "SchoolHouseCarnell",
                    "AlphabetFleeStart",
                    "AlphabetFlee01",
                    "AlphabetFlee02",
                    "AlphabetFlee03",
                    "AlphabetFlee04"
                ]);

            // B. Carnell
            _BCarnell = new NPCBuilder<BCarnell>(Info)
                .SetName("B Carnell")
                .SetEnum("BCarnell")
                .AddLooker()
                .AddTrigger()
                .IgnorePlayerOnSpawn()
                .SetWanderEnterRooms()
                .AddSpawnableRoomCategories(RoomCategory.Null)
                .SetPoster(ObjectCreators.CreatePosterObject(bcarnelAssets.Get<Texture2D>("PRI_BCarnell"), []))
                .SetForcedSubtitleColor(new Color(0.01960784f, 0.3607843f, 0.7568628f))
                .SetMetaTags(["BCPP", "lethal", "teacher", "faculty"])
                .Build();
            
            /*ObjectCreators.CreateNPC<BCarnell>(
                "B Carnell",
                EnumExtensions.ExtendEnum<Character>("BCarnell"),
                ObjectCreators.CreateCharacterPoster(bcarnelAssets.Get<Texture2D>("PRI_BCarnell"), "", ""));*/
            _BCarnell.audMan = _BCarnell.GetComponent<AudioManager>();
            /*_BCarnell.audMan.ReflectionSetVariable("subtitleColor", new Color(0.01960784f, 0.3607843f, 0.7568628f));
            _BCarnell.audMan.ReflectionSetVariable("overrideSubtitleColor", true);*/
            _BCarnell.spriteRenderer[0].transform.position = new Vector3(0f, -2f, 0f);
            _BCarnell.Navigator.passableObstacles.Add(PassableObstacle.LockedDoor);
            _BCarnell.slap = bcarnelAssets.Get<SoundObject>("BCarnell/Slap");

            // Adds a custom animator
            CustomSpriteRendererAnimator animator = _BCarnell.gameObject.AddComponent<CustomSpriteRendererAnimator>();
            animator.renderer = _BCarnell.spriteRenderer[0];
            animator.timeScale = TimeScaleType.Npc;
            _BCarnell.animator = animator;
            List<Sprite> 
                slapSprites = new List<Sprite>(),
                slapbrokenSprite = new List<Sprite>(),
                waveSprites = new List<Sprite>(),
                jumpscareFrames = new List<Sprite>();
            for (int frame = 1; frame <= 6; frame++) // Walk Frames
            {
                slapSprites.Add(bcarnelAssets.Get<Sprite>("BCarnell/Walk_" + frame));
                slapbrokenSprite.Add(bcarnelAssets.Get<Sprite>("BCarnell/WalkBrokenRuler_" + frame));
            }
            for (int frame = 0; frame <= 22; frame++) // Wave Frames
                waveSprites.Add(bcarnelAssets.Get<Sprite>("BCarnell/Wave_" + frame));
            for (int frame = 0; frame <= 24; frame++) // Jumpscare Frames
                jumpscareFrames.Add(bcarnelAssets.Get<Sprite>("BCarnell/JumpScare_" + frame));
            animator.AddAnimation("Wave", new SpriteAnimation(waveSprites.ToArray(), 3f));
            animator.AddAnimation("Happy", new SpriteAnimation([waveSprites.Last()], 1f));
            animator.AddAnimation("Stare", new SpriteAnimation([bcarnelAssets.Get<Sprite>("BCarnell/GettingAngry")], 1f));

            animator.AddAnimation("Slap", new SpriteAnimation(12, slapSprites.ToArray()));
            animator.AddAnimation("SlapIdle", new SpriteAnimation([slapSprites.Last()], 1f));
            animator.AddAnimation("SlapIdleBroken", new SpriteAnimation([slapbrokenSprite.Last()], 1f));
            animator.AddAnimation("SlapBroken", new SpriteAnimation(12, slapbrokenSprite.ToArray()));

            _BCarnell.overlay = Instantiate(Resources.FindObjectsOfTypeAll<Gum>().Last().transform.Find("GumOverlay").gameObject, _BCarnell.transform).GetComponent<Canvas>();
            _BCarnell.overlay.transform.Find("Image").GetComponent<Image>().sprite = bcarnelAssets.Get<Sprite>("BCarnell/JumpScare_0");
            _BCarnell.overlayAnimator = _BCarnell.overlay.gameObject.AddComponent<CustomImageAnimator>();
            _BCarnell.overlayAnimator.useScaledTime = false;
            _BCarnell.overlayAnimator.image = _BCarnell.overlay.transform.Find("Image").GetComponent<Image>();
            _BCarnell.overlayAnimator.AddAnimation("Jumpscarey", new SpriteAnimation(jumpscareFrames.ToArray(), 0.785f));

            // Police Nerd
            _PoliceNerd = new NPCBuilder<PoliceNerd>(Info)
                .SetName("Police Nerd")
                .SetEnum("PoliceNerd")
                .AddLooker()
                .AddTrigger()
                .AddHeatmap()
                .SetWanderEnterRooms()
                .SetMinMaxAudioDistance(50f, 250f)
                .AddSpawnableRoomCategories(RoomCategory.Office)
                .SetPoster(ObjectCreators.CreatePosterObject(bcarnelAssets.Get<Texture2D>("PRI_PoliceNerd"), []))
                .SetMetaTags(["BCPP", "faculty"])
                .Build();

            /*ObjectCreators.CreateNPC<PoliceNerd>(
            "Police Nerd",
            EnumExtensions.ExtendEnum<Character>("PoliceNerd"),
            ObjectCreators.CreateCharacterPoster(bcarnelAssets.Get<Texture2D>("PRI_PoliceNerd"), "", ""),
            usesHeatMap: true,
            minAudioDistance: 50f,
            spawnableRooms: [RoomCategory.Office]
        );*/
            _PoliceNerd.audMan = _PoliceNerd.GetComponent<AudioManager>();
            _PoliceNerd.spriteBase.AddComponent<BillboardUpdater>();
            _PoliceNerd.spriteRenderer[0].sprite = bcarnelAssets.Get<Sprite>("PoliceNerd/Idle");
            _PoliceNerd.spriteRenderer[0].transform.localPosition = new Vector3(0f, -0.7f, 0f);
            _PoliceNerd.spriteRenderer = _PoliceNerd.spriteRenderer.AddToArray(Instantiate(_PoliceNerd.spriteRenderer[0], _PoliceNerd.spriteBase.transform, true));
            _PoliceNerd.spriteRenderer[1].sprite = bcarnelAssets.Get<Sprite>("PoliceNerd/CarManslaughter");
            //_PoliceNerd.spriteRenderer[1].sortingOrder++;
            _PoliceNerd.spriteRenderer[1].transform.localPosition -= new Vector3(0f, 0f, _PoliceNerd.spriteRenderer[0].transform.forward.z) * 0.1f;
            _PoliceNerd.spriteRenderer[1].transform.localScale = new Vector3(4f, 4f, 4f);
            _PoliceNerd.spriteRenderer[1].gameObject.SetActive(false);
            _PoliceNerd.Navigator.maxSpeed = 26f;
            _PoliceNerd.Navigator.SetSpeed(26f);
            _PoliceNerd.Navigator.passableObstacles.Add(PassableObstacle.LockedDoor);
            LayerMask layer = (LayerMask)_PoliceNerd.looker.ReflectionGetVariable("layerMask");
            layer |= 1 << LayerMask.NameToLayer("NPCs");
            _PoliceNerd.looker.ReflectionSetVariable("layerMask", layer);
            var pri = NPCMetaStorage.Instance.Get(Character.Principal).value as Principal;
            //_PoliceNerd.detentionUiPre = (DetentionUi)pri.ReflectionGetVariable("detentionUiPre");
            _PoliceNerd.overlay = Instantiate(Resources.FindObjectsOfTypeAll<Gum>().Last().transform.Find("GumOverlay").gameObject, _PoliceNerd.transform).GetComponent<Canvas>(); // Another one...
            _PoliceNerd.overlay.transform.Find("Image").GetComponent<Image>().sprite = bcarnelAssets.Get<Sprite>("PoliceNerd/HUDCar");
            _PoliceNerd.whistle = bcarnelAssets.Get<SoundObject>("PoliceNerd/GoofyHorn");
            _PoliceNerd.ReflectionSetVariable("normalSprite", bcarnelAssets.Get<Sprite>("PoliceNerd/Idle"));
            _PoliceNerd.ReflectionSetVariable("chasingSprite", bcarnelAssets.Get<Sprite>("PoliceNerd/Scolding"));
            _PoliceNerd.ReflectionSetVariable("whistleChance", 0.02f);

            //NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [_BCarnell], "B Carnell", NPCFlags.Standard));
            //NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [_PoliceNerd], "Police Nerd", NPCFlags.Standard));
            bcarnelAssets.AddRange<NPC>([
                _BCarnell,
                _PoliceNerd
                ],
                [
                    "NPCs/BCarnell",
                    "NPCs/PoliceNerd"
                ]);
            Nerd = _PoliceNerd.Character;
            TeacherPlugin.RegisterTeacher(_BCarnell);

            GeneratorManagement.Register(this, GenerationModType.Addend, (name, num, ld) =>
            {
                var meta = ld.GetMeta();
                if (meta == null) return; // Not checking for tags, but some levels such as Notebook Frenzy does not use "main" and "found_on_main" tags.
                string[] defaultFloors = ["F1", "F2", "F3", "F4", "F5", "END"];
                foreach (var levelObject in ld.GetCustomLevelObjects())
                {
                    if (levelObject.IsModifiedByMod(Info)) continue;
                    switch (name)
                    {
                        default:
                            if (defaultFloors.Contains(name) || name == "INF")
                                levelObject.AddPotentialTeacher(_BCarnell, chance.Value);
                            else
                                levelObject.AddPotentialTeacher(_BCarnell, 40);
                            levelObject.AddPotentialAssistingTeacher(_BCarnell, chance.Value);
                            break;
                    }
                    levelObject.MarkAsModifiedByMod(Info);
                }
            });
        }
    }
}