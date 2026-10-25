using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Utils;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components.Animation;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeacherAPI;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellTeacher
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellteacher", "B. Carnell in Plus", "1.1.0")]
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

            LoadingEvents.RegisterOnAssetsLoaded(Info, AssetsLoad, LoadingEventOrder.Start);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad, LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, MakeStudentsFaulty.GetMore, LoadingEventOrder.Final);
            ModdedSaveGame.AddSaveHandler(Info);
        }

        private void AssetsLoad()
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
            Color 
                bcarnellSub = new Color(0.01960784f, 0.3607843f, 0.7568628f),
                nerdSub = new Color(126f / 255f, 131f / 255f, 176f / 255f);
            bcarnelAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Hello.wav"), "Vfx_BCarnell_Hello", SoundType.Voice, bcarnellSub),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Slap.wav"), "Sfx_BCarnell_Walk", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Broken.wav"), "Sfx_BrokenDagger", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_Reequip.wav"), "Sfx_RegenDagger", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_angry.wav"), "Vfx_BCarnell_Angry", SoundType.Effect, bcarnellSub),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "BC_FinalFloor.wav"), "Vfx_BCarnell_FinalFloor", SoundType.Effect, bcarnellSub),

                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_bomb.wav"), "Sfx_Lose_Buzz", SoundType.Effect, bcarnellSub),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_screech.wav"), "Sfx_Lose_Buzz", SoundType.Effect, bcarnellSub),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_standard.wav"), "Sfx_Lose_Buzz", SoundType.Effect, bcarnellSub),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "LOSEBC_yello.wav"), "Sfx_Lose_Buzz", SoundType.Effect, bcarnellSub)
                ],
                [
                    "BCarnell/Hello",

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
            bcarnelAssets.Get<SoundObject>("BCarnell/Hello").additionalKeys = [
                new(){
                    key = "Vfx_BCarnell_Hello1",
                    time = 3.21f
                }
                ];
            float NerdSize = 62f; // Came from a stock image
            bcarnelAssets.AddRange<Sprite>([
                AssetLoader.SpriteFromMod(this, new(0.6f, 0.5f), NerdSize, "Texture2D", "NerdMan.png"),
                AssetLoader.SpriteFromMod(this, new(0.635f, 0.5f), NerdSize, "Texture2D", "NerdMan_Netural.png"),
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
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Car", "PN_CarDisabled.wav"), "Sfx_BCarnell_CarOut", SoundType.Effect, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Car", "PN_CarEnabled.wav"), "Sfx_PoliceNerd_CarEnabled", SoundType.Effect, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PN_Horn.wav"), "Sfx_PoliceNerd_GoofyHorn", SoundType.Effect, nerdSub),

                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "PN_JailForYou.wav"), "Vfx_PoliceNerd_JailForYou", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoDefault.wav"), "Sfx_PoliceNerd_NoBreakingDefault", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoRunning.wav"), "Sfx_PoliceNerd_NoRunning", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoFaculty.wav"), "Sfx_PoliceNerd_NoFaculty", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoDrinking.wav"), "Sfx_PoliceNerd_NoDrinking", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoEscapeDetention.wav"), "Sfx_PoliceNerd_NoEscapingDetention", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoEscapeJail.wav"), "Sfx_PoliceNerd_NoEscapingJail", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoLockers.wav"), "Sfx_PoliceNerd_NoLockers", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoBullying.wav"), "Sfx_PoliceNerd_NoBullying", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoRobbing.wav"), "Sfx_PoliceNerd_NoRobbery", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoAfterHours.wav"), "Sfx_PoliceNerd_NoAfterHours", SoundType.Voice, nerdSub),
                    ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NerdScolds", "PN_NoFraud.wav"), "Sfx_PoliceNerd_NoFraud", SoundType.Voice, nerdSub)
                    ],
                    [
                        "PoliceNerd/CarOut",
                        "PoliceNerd/CarEnabled",
                        "PoliceNerd/GoofyHorn",

                        "PoliceNerd/JailForU",
                        "PoliceNerd/RuleScold/Default",
                        "PoliceNerd/RuleScold/Running",
                        "PoliceNerd/RuleScold/Faculty",
                        "PoliceNerd/RuleScold/Drinking",
                        "PoliceNerd/RuleScold/EscapingDetention",
                        "PoliceNerd/RuleScold/EscapingJail",
                        "PoliceNerd/RuleScold/Lockers",
                        "PoliceNerd/RuleScold/Bullying",
                        "PoliceNerd/RuleScold/Robbery",
                        "PoliceNerd/RuleScold/AfterHours",
                        "PoliceNerd/RuleScold/Fraud"
                    ]);
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Bullying").soundKey = "Sfx_PoliceNerd_Hey";
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Bullying").additionalKeys = [new()
            {
                key = "Sfx_PoliceNerd_NoBullying",
                time = 0.635f
            }];
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Robbery").soundKey = "Sfx_PoliceNerd_You";
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Robbery").additionalKeys = [new()
            {
                key = "Sfx_PoliceNerd_NoRobbery",
                time = 1.105f
            }];
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/AfterHours").soundKey = "Sfx_PoliceNerd_Hey";
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/AfterHours").additionalKeys = [new()
            {
                key = "Sfx_PoliceNerd_NoAfterHours",
                time = 0.65f
            }];
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Fraud").soundKey = "Sfx_PoliceNerd_Hey";
            bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Fraud").additionalKeys = [new()
            {
                key = "Sfx_PoliceNerd_NoFraud",
                time = 0.735f
            }];
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

            bcarnelAssets.AddRange<SoundObject>([
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "CloneDestabilizer_Use.wav"), "Vfx_CarnellDestabilizer_Shoot", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "CloneDestabilizer_Idle.wav"), "Vfx_CarnellDestabilizer_Idle", SoundType.Effect, Color.white),
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "CloneDestabilizer_Boom.wav"), "Vfx_CarnellDestabilizer_Boom", SoundType.Effect, Color.white),
                ], [
                    "Destabilizer/Shoot",
                    "Destabilizer/Idle",
                    "Destabilizer/Boom"
                    ]);
            bcarnelAssets.AddRange<Sprite>([
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Texture2D", "CarnellDestabilizer_Small.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "Texture2D", "CarnellDestabilizer_Large.png")
                ], [
                    "Destabilizer/SmallIcon",
                    "Destabilizer/LargeIcon"
                    ]);

            var windowTex = AssetLoader.TextureFromMod(this, "Texture2D", "JailBar.png");
            WindowObject jailbar = ObjectCreators.CreateWindowObject("PoliceNerdJailBar", windowTex, windowTex, AssetLoader.TextureFromMod(this, "Texture2D", "JailBarMask.png"));
            jailbar.breakable = false;
            jailbar.allowSoundThrough = true;
            StandardDoorMats jaildoor = ObjectCreators.CreateDoorDataObject("PoliceNerdJailDoor", AssetLoader.TextureFromMod(this, "Texture2D", "JailDoor_80.png"), AssetLoader.TextureFromMod(this, "Texture2D", "JailDoor_0.png"));
            var jaildetention = Instantiate(AssetFinder.FindOfTypeWithName<RoomFunctionContainer>("OfficeRoomFunction", true), MTM101BaldiDevAPI.prefabTransform, false);
            jaildetention.gameObject.name = "JailRoomFunction";
            FieldInfo _functions = AccessTools.DeclaredField(typeof(RoomFunctionContainer), "functions");
            var contains = (List<RoomFunction>)_functions.GetValue(jaildetention);
            contains.Remove(jaildetention.GetComponent<CharacterPostersRoomFunction>());
            DestroyImmediate(jaildetention.GetComponent<CharacterPostersRoomFunction>());
            contains.Add(jaildetention.gameObject.AddComponent<JailWindowRoomFunction>());
            _functions.SetValue(jaildetention, contains);

            // Be aware that I am not using the Level Loader plugin for this.
            // Smaller Sized Jail
            RoomAsset jail = ScriptableObject.CreateInstance<RoomAsset>();
            jail.name = "SmallerJail";
            ((UnityEngine.Object)jail).name = "Room_PoliceNerd_SmallerJail";
            jail.cells = new()
            {
                new CellData()
                {
                    pos = new(0, 0),
                    roomId = 0,
                    type = 13,
                },
                new CellData()
                {
                    pos = new(1, 0),
                    roomId = 0,
                    type = 5,
                },
                new CellData()
                {
                    pos = new(2, 0),
                    roomId = 0,
                    type = 7,
                }
            };
            jail.entitySafeCells = new() { new(0, 0), new(1, 0), new(2, 0) };
            jail.eventSafeCells = new() { new(0, 0), new(1, 0), new(2, 0) };
            jail.potentialDoorPositions = new() { new(1, 0) };
            jail.standardLightCells = new() { new(0, 0), new(2, 0) };
            jail.lightPre = AssetFinder.FindOfTypeWithName<GameObject>("FluorescentLight", true).transform;
            jail.windowObject = jailbar;
            jail.windowChance = 0.4f;
            jail.keepTextures = true;
            jail.wallTex = AssetLoader.TextureFromMod(this, "Texture2D", "RoomWallJail.png");
            jail.florTex = AssetLoader.TextureFromMod(this, "Texture2D", "RoomFloorJail.png");
            jail.ceilTex = AssetFinder.FindOfTypeWithName<Texture2D>("CeilingNoLight", true);
            jail.roomFunctionContainer = jaildetention;
            jail.type = RoomType.Room;
            jail.category = RoomCategory.Office;
            jail.color = new(1f, 1f, 0f);
            jail.hasActivity = false;
            jail.doorMats = jaildoor;
            jail.posterChance = 0f;
            bcarnelAssets.Add("PoliceNerdJail/Smaller", jail);
            // Regular Sized Jail (from BCMAC)
            jail = Instantiate(jail);
            jail.name = "RegularJail";
            ((UnityEngine.Object)jail).name = "Room_PoliceNerd_RegularJail";
            jail.cells = new()
            {
                new CellData()
                {
                    pos = new(0, 0),
                    roomId = 0,
                    type = 12,
                },
                new CellData()
                {
                    pos = new(1, 0),
                    roomId = 0,
                    type = 4,
                },
                new CellData()
                {
                    pos = new(2, 0),
                    roomId = 0,
                    type = 4,
                },
                new CellData()
                {
                    pos = new(3, 0),
                    roomId = 0,
                    type = 4,
                },
                new CellData()
                {
                    pos = new(4, 0),
                    roomId = 0,
                    type = 6,
                },

                new CellData()
                {
                    pos = new(0, 1),
                    roomId = 0,
                    type = 9,
                },
                new CellData()
                {
                    pos = new(1, 1),
                    roomId = 0,
                    type = 1,
                },
                new CellData()
                {
                    pos = new(2, 1),
                    roomId = 0,
                    type = 1,
                },
                new CellData()
                {
                    pos = new(3, 1),
                    roomId = 0,
                    type = 1,
                },
                new CellData()
                {
                    pos = new(4, 1),
                    roomId = 0,
                    type = 3,
                }
            };
            jail.entitySafeCells = new() {
            new(1, 0), new(2, 0), new(3, 0), new(4, 0),
            new(1, 1), new(2, 1), new(3, 1)
            };
            jail.eventSafeCells = new() {
            new(1, 0), new(2, 0), new(3, 0), new(4, 0),
            new(1, 1), new(2, 1), new(3, 1)
            };
            jail.potentialDoorPositions = new() { new(2, 0) };
            jail.standardLightCells = new() { 
                new(0, 0), new(4, 0),
                new(1, 1), new(3, 1),
            };
            jail.basicObjects.AddRange([
                new(){
                    position = new(45f, 0f, 5f),
                    rotation = Direction.West.ToRotation(),
                    prefab = AssetFinder.FindOfTypeWithName<GameObject>("Chair_Test", true).transform,
                    replaceable = false
                },
                new(){
                    position = new(5f, 0f, 10f),
                    rotation = Direction.East.ToRotation(),
                    prefab = AssetFinder.FindOfTypeWithName<GameObject>("BigDesk_NoSpawn", true).transform,
                    replaceable = false
                },
                new(){
                    position = new(5f, 3.75f, 13f),
                    rotation = default,
                    prefab = AssetFinder.FindOfTypeWithName<GameObject>("Decor_Lunch", true).transform,
                    replaceable = true,
                }
                ]);
            jail.basicSwaps.AddRange([
                new()
                {
                    prefabToSwap = jail.basicObjects[2].prefab,
                    potentialReplacements = [
                        new()
                        {
                            selection = AssetFinder.FindOfTypeWithName<GameObject>("Decor_Papers", true).transform,
                            weight = 50
                        },
                        new()
                        {
                            selection = AssetFinder.FindOfTypeWithName<GameObject>("Decor_Globe", true).transform,
                            weight = 50
                        },
                        new()
                        {
                            selection = AssetFinder.FindOfTypeWithName<GameObject>("Decor_Notebooks", true).transform,
                            weight = 50
                        }
                        ],
                    chance = 0.35f
                }
                ]);
            bcarnelAssets.Add("PoliceNerdJail/Regular", jail);
        }

        private void PreLoad()
        {
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
            _BCarnell.Navigator.accel = 0f;

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
            _BCarnell.hello = bcarnelAssets.Get<SoundObject>("BCarnell/Hello");
            _BCarnell.rulerBreak = bcarnelAssets.Get<SoundObject>("BCarnell/BrokenDagger");
            _BCarnell.regenDagger = bcarnelAssets.Get<SoundObject>("BCarnell/RegenDagger");
            _BCarnell.screamsInAgony = bcarnelAssets.Get<SoundObject>("BCarnell/FinalFloor");
            _BCarnell.loseSounds = [
                new WeightedSoundObject()
                {
                    selection = bcarnelAssets.Get<SoundObject>("BCarnell/Lose/Bomb"),
                    weight = 500
                },
                new WeightedSoundObject()
                {
                    selection = bcarnelAssets.Get<SoundObject>("BCarnell/Lose/Screech"),
                    weight = 500
                },
                new WeightedSoundObject()
                {
                    selection = bcarnelAssets.Get<SoundObject>("BCarnell/Lose/Standard"),
                    weight = 550
                },
                new WeightedSoundObject()
                {
                    selection = bcarnelAssets.Get<SoundObject>("BCarnell/Lose/Yello"),
                    weight = 500
                }
                ];
            _BCarnell.binders = bcarnelAssets.Get<Sprite[]>("BCarnell/Binders");

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
                .SetForcedSubtitleColor(new Color(126f / 255f, 131f / 255f, 176f / 255f))
                .SetMetaTags(["BCPP", "faculty"])
                .AddPotentialRoomAssets(
                new WeightedRoomAsset()
                {
                    selection = bcarnelAssets.Get<RoomAsset>("PoliceNerdJail/Smaller"),
                    weight = 50
                },
                new WeightedRoomAsset()
                {
                    selection = bcarnelAssets.Get<RoomAsset>("PoliceNerdJail/Regular"),
                    weight = 150
                })
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
            _PoliceNerd.spriteRenderer[0].sprite = bcarnelAssets.Get<Sprite>("PoliceNerd/Idle");
            _PoliceNerd.spriteRenderer[0].transform.localPosition = new Vector3(0f, -0.7f, 0f);
            _PoliceNerd.spriteRenderer = _PoliceNerd.spriteRenderer.AddToArray(Instantiate(_PoliceNerd.spriteRenderer[0], _PoliceNerd.spriteRenderer[0].transform, false));
            _PoliceNerd.spriteRenderer[1].sprite = bcarnelAssets.Get<Sprite>("PoliceNerd/CarManslaughter");
            //_PoliceNerd.spriteRenderer[1].sortingOrder++;
            _PoliceNerd.spriteRenderer[1].transform.localPosition = new Vector3(0f, 0.5f, -1.1f);
            _PoliceNerd.spriteRenderer[1].transform.localScale = new Vector3(4f, 4f, 4f);
            _PoliceNerd.spriteRenderer[1].gameObject.SetActive(false);
            _PoliceNerd.spriteRenderer[0].gameObject.AddComponent<BillboardUpdater>();
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
            _PoliceNerd.jailForYou = bcarnelAssets.Get<SoundObject>("PoliceNerd/JailForU");
            _PoliceNerd.noDefault = bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Default");
            _PoliceNerd.noJailbreak = bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/EscapingJail");
            _PoliceNerd.Scolds.AddRange(new Dictionary<string, SoundObject>()
            {
                { "running", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Running") },
                { "faculty", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Faculty") },
                { "drinking", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Drinking") },
                { "escaping", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/EscapingDetention") },
                { "lockers", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Lockers") },
                { "afterhours", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/AfterHours") },
                { "bullying", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Bullying") },
                { "fraud", bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Fraud") }
            });
            _PoliceNerd.noRobbery = bcarnelAssets.Get<SoundObject>("PoliceNerd/RuleScold/Robbery");
            _PoliceNerd.whistle = bcarnelAssets.Get<SoundObject>("PoliceNerd/GoofyHorn");
            _PoliceNerd.carOn = bcarnelAssets.Get<SoundObject>("PoliceNerd/CarEnabled");
            _PoliceNerd.carOff = bcarnelAssets.Get<SoundObject>("PoliceNerd/CarOut");

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

            var counter = new ItemBuilder(Info)
                .SetNameAndDescription("Itm_CarnellDestabilizer", "Desc_CarnellDestabilizer")
                .SetSprites(bcarnelAssets.Get<Sprite>("Destabilizer/SmallIcon"), bcarnelAssets.Get<Sprite>("Destabilizer/LargeIcon"))
                .SetItemComponent<ITM_CarnellDestabilizer>()
                .SetEnum("CarnellDestabilizer")
                .SetMeta(ItemFlags.CreatesEntity | ItemFlags.Persists, ["BCPP"])
                .SetGeneratorCost(1111)
                .SetShopPrice(1111)
                .Build();
            counter.item.gameObject.layer = LayerMask.NameToLayer("CollidableEntities");
            var itemcomponent = counter.item as ITM_CarnellDestabilizer;
            itemcomponent.blastSnd = bcarnelAssets.Get<SoundObject>("Destabilizer/Shoot");
            itemcomponent.boomSnd = bcarnelAssets.Get<SoundObject>("Destabilizer/Boom");
            itemcomponent.audMan = itemcomponent.gameObject.AddComponent<PropagatedAudioManager>();
            itemcomponent.audMan.audioDevice = itemcomponent.gameObject.AddComponent<AudioSource>();
            itemcomponent.audMan.audioDevice.dopplerLevel = 1f;
            itemcomponent.audMan.audioDevice.spatialBlend = 1f;
            itemcomponent.audMan.audioDevice.playOnAwake = false;
            itemcomponent.audMan.ReflectionSetVariable("soundOnStart", new SoundObject[] { bcarnelAssets.Get<SoundObject>("Destabilizer/Idle") });
            var transformRend = new GameObject("RendererBase").transform;
            transformRend.SetParent(itemcomponent.transform, false);
            transformRend.gameObject.layer = LayerMask.NameToLayer("Billboard");
            var trigger = itemcomponent.gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2f;
            var collider = itemcomponent.gameObject.AddComponent<CapsuleCollider>();
            collider.radius = 1f;
            collider.height = 2.5f;
            ParticleSystem particle = transformRend.gameObject.AddComponent<ParticleSystem>();
            var main = particle.main;

            main.duration = 5f;
            main.startLifetime = 1f;
            main.startSpeed = 10f;
            main.startSize = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
            main.playOnAwake = true;
            main.startColor = Color.magenta;

            var emission = particle.emission;
            emission.rateOverTime = 50f;

            var sizeOverLifetime = particle.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.5f, 0f);

            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.arc = 0f;

            var render = transformRend.gameObject.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Billboard;
            render.material = Resources.FindObjectsOfTypeAll<Material>().Last(x => x.name == "UI_AsSprite" && x.IsPrefab());
            render.sortMode = ParticleSystemSortMode.OldestInFront;

            itemcomponent.entity = itemcomponent.gameObject.AddComponent<Entity>();
            itemcomponent.entity.ReflectionSetVariable("trigger", trigger);
            itemcomponent.entity.ReflectionSetVariable("collider", collider);
            itemcomponent.entity.ReflectionSetVariable("rendererBase", transformRend);
            ActivityModifier externalActivity = itemcomponent.gameObject.AddComponent<ActivityModifier>();
            itemcomponent.entity.ReflectionSetVariable("externalActivity", externalActivity);
            FieldInfo _CollisionMask = AccessTools.DeclaredField(typeof(Entity), "collisionLayerMask");
            LayerMask collisionLayerMask = (LayerMask)_CollisionMask.GetValue(itemcomponent.entity);
            collisionLayerMask |= LayerMask.NameToLayer("NPCs");
            _CollisionMask.SetValue(itemcomponent.entity, collisionLayerMask);
            itemcomponent.entity.ReflectionSetVariable("grounded", false);

            bcarnelAssets.Add("Items/CarnellDestabilizer", counter);

            GeneratorManagement.Register(this, GenerationModType.Addend, (name, num, ld) =>
            {
                var meta = ld.GetMeta();
                if (meta == null) return; // Not checking for tags, but some levels such as Notebook Frenzy does not use "main" and "found_on_main" tags.
                string[] defaultFloors = ["F1", "F2", "F3", "F4", "F5", "END"];
                if (meta.tags.Contains("main") || meta.tags.Contains("endless"))
                    ld.shopItems = ld.shopItems.AddToArray(new WeightedItemObject()
                    {
                        selection = counter,
                        weight = 11
                    });
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
                            if (meta.tags.Contains("main") || meta.tags.Contains("endless"))
                                levelObject.potentialItems = levelObject.potentialItems.AddToArray(new()
                                {
                                    selection = counter,
                                    weight = 1,
                                });
                            break;
                    }
                    levelObject.MarkAsModifiedByMod(Info);
                }
            });
            GeneratorManagement.RegisterFieldTripLootChange(this, (trip, loot) =>
            {
                loot.potentialItems.Add(new WeightedItemObject()
                {
                    selection = counter,
                    weight = 111
                });
            });
        }
    }
}