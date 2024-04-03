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

namespace BCarnellChars
{
    [BepInPlugin("alexbw145.baldiplus.bcarnellchars", "B. Carnell Chars", "1.0.0.0")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInIncompatibility("alexbw145.bbplus.rpsguy")] // This is a bad idea...
    [BepInProcess("BALDI.exe")]
    public class BasePlugin : BaseUnityPlugin
    {
        public static BasePlugin Instance;
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

            LoadingEvents.RegisterOnAssetsLoaded(PreLoad, false);
            GeneratorManagement.Register(this, GenerationModType.Addend, (floorName, floorNum, ld) =>
            {
                switch (floorName)
                {
                    case "F1":
                        ld.items = ld.items.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 75
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
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
                        ld.items = ld.items.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 75
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock")).value,
                                weight = 75
                            }
                            ]);
                        ld.shopItems = ld.shopItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
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
                        ld.items = ld.items.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 75
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock")).value,
                                weight = 75
                            }
                            ]);
                        ld.shopItems = ld.shopItems.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
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
                        ld.items = ld.items.AddRangeToArray([
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
                                weight = 85
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock")).value,
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
                }

            });
        }

        private void PreLoad()
        {
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

            // do dis
            var rpsguy = ObjectCreators.CreateNPC<RPSGuy>("RPS Guy", EnumExtensions.ExtendEnum<Character>("RPSGuy"), ObjectCreators.CreatePosterObject(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "RPS Guy", "PRI_rpsguy.png"), []), spawnableRooms: [RoomCategory.Class, RoomCategory.Office, RoomCategory.Special]);
            AccessTools.DeclaredField(typeof(Navigator), "avoidRooms").SetValue(rpsguy.Navigator, true);
            AccessTools.DeclaredField(typeof(Looker), "distance").SetValue(rpsguy.looker, 99f);
            PropagatedAudioManager annoying = rpsguy.gameObject.AddComponent<PropagatedAudioManager>();
            annoying.audioDevice = rpsguy.gameObject.AddComponent<AudioSource>();
            AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart").SetValue(annoying, new SoundObject[]
            {
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "RPS Guy", "mus_rps.wav"), "Mfx_mus_Playtime", SoundType.Music, new Color(0.7176471f, 0.6941177f, 0.6235294f))
            });
            AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart").SetValue(annoying, true);
            annoying.audioDevice.spatialBlend = 1;
            annoying.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            annoying.audioDevice.maxDistance = 150;
            annoying.audioDevice.dopplerLevel = 0;
            annoying.audioDevice.spread = 0;
            GameObject rockpaperscissors = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Jumprope>().ToList().Find(s => s.name == "Jumprope").gameObject);
            rockpaperscissors.name = "Rock Paper Scissors";
            Destroy(rockpaperscissors.GetComponent<Jumprope>());
            Destroy(rockpaperscissors.transform.Find("RopeCanvas").gameObject);
            Destroy(rockpaperscissors.transform.Find("TextCanvas").transform.Find("Count").gameObject);
            RawImage raw = rockpaperscissors.transform.Find("TextCanvas").gameObject.AddComponent<RawImage>();
            raw.color = new Color(0f,0f,0f,0.50f); // Initial Color
            DontDestroyOnLoad(rockpaperscissors);
            rockpaperscissors.SetActive(false);
            Image uhf = new GameObject("Border", typeof(Image)).GetComponent<Image>();
            uhf.gameObject.layer = LayerMask.NameToLayer("UI");
            uhf.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            uhf.transform.localPosition = Vector3.zero;
            uhf.sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_border.png"), 1f);
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
            me.GetComponent<RawImage>().texture = AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Player.png");
            GameObject them = new GameObject("Opponent", typeof(RawImage));
            them.transform.SetParent(rockpaperscissors.transform.Find("TextCanvas").transform);
            them.layer = LayerMask.NameToLayer("UI");
            them.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0.5f);
            them.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
            them.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            them.transform.localScale = Vector3.one * 0.5f;
            them.GetComponent<RectTransform>().anchoredPosition = new Vector3(-40, 0, 0); // Pro tip: anchoredPosition helps.
            them.GetComponent<RawImage>().texture = AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_RPSGuy.png");
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
            rpsguy.rpsPre = rockpaperscissors.AddComponent<RockPaperScissors>();
            rpsguy.rpsPre.hitTie = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 15.wav"), "Nothing", SoundType.Effect, Color.white);
            rpsguy.rpsPre.hitTie.subtitle = false;
            rpsguy.rpsPre.hitWin = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 7.wav"), "Nothing", SoundType.Effect, Color.white);
            rpsguy.rpsPre.hitWin.subtitle = false;
            rpsguy.rpsPre.hitLose = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "Hit 14.wav"), "Nothing", SoundType.Effect, Color.white);
            rpsguy.rpsPre.hitLose.subtitle = false;
            rpsguy.rpsPre.chosenSprites.AddRange([
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Rock.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Paper.png"), 1f),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "UI", "RPS_Scissors.png"), 1f)
            ]);

            var errorbot = ObjectCreators.CreateNPC<ERRORBOT>("ERROR-BOT_ITEM-STEALER", EnumExtensions.ExtendEnum<Character>("ERRORBOT"), ObjectCreators.CreatePosterObject(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "PRI_errorbot.png"), []), usesHeatMap: true, spawnableRooms: [RoomCategory.Hall]);
            AccessTools.DeclaredField(typeof(Navigator), "avoidRooms").SetValue(errorbot.Navigator, true);
            AnimatedSpriteRotator threesixty = errorbot.spriteBase.AddComponent<AnimatedSpriteRotator>();
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "renderer").SetValue(threesixty, errorbot.spriteRenderer[0]);
            float errorbodySpriteSize = 100f;
            Sprite target = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-1.png"), errorbodySpriteSize);
            threesixty.targetSprite = target;
            SpriteRotationMap rotationMap = new SpriteRotationMap()
            {
                angleCount = 17
            };
            // It uses many, likely a 1st Prize reference but not a Dr. Reflex reference.
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(rotationMap, new Sprite[]
            {
                target,
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
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-17.png"), errorbodySpriteSize)
            });
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "spriteMap").SetValue(threesixty, new SpriteRotationMap[]
            {
                rotationMap
            });
            errorbot.spriteRenderer[0].transform.position = new Vector3(0f, -3.35f, 0f);
            errorbot.spriteRenderer = errorbot.spriteRenderer.AddItem(new GameObject("Head", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>()).ToArray();
            errorbot.spriteRenderer[1].transform.SetParent(errorbot.spriteBase.transform);
            errorbot.spriteRenderer[1].gameObject.layer = errorbot.spriteRenderer[0].gameObject.layer;
            errorbot.spriteRenderer[1].transform.position = new Vector3(0f, 0.27f, 0f);
            errorbot.spriteRenderer[1].material = Resources.FindObjectsOfTypeAll<Material>().ToList().Find(m => m.name == "SpriteStandard_Billboard");
            errorbot.spriteRenderer[1].sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botHead.png"), errorbodySpriteSize-55f);
            PropagatedAudioManager staticbot = errorbot.gameObject.AddComponent<PropagatedAudioManager>();
            staticbot.audioDevice = errorbot.gameObject.AddComponent<AudioSource>();
            AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart").SetValue(staticbot, new SoundObject[]
            {
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "ERRORBOT_ITEMSTEALER", "ERB_static.wav"), "Sfx_Crafters_Loop", SoundType.Effect, Color.white)
            });
            AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart").SetValue(staticbot, true);
            staticbot.audioDevice.spatialBlend = 1;
            staticbot.audioDevice.rolloffMode = AudioRolloffMode.Custom;
            staticbot.audioDevice.maxDistance = 150;
            staticbot.audioDevice.dopplerLevel = 0;
            staticbot.audioDevice.spread = 0;

            // This part is the reason why I almost gave up
            var siegecart = ObjectCreators.CreateNPC<SiegeCanonCart>("Siege Canon Cart", EnumExtensions.ExtendEnum<Character>("SiegeCanonCart"), ObjectCreators.CreatePosterObject(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "PRI_siegecanon.png"), []), usesHeatMap: true, spawnableRooms: [RoomCategory.Hall]);
            AccessTools.DeclaredField(typeof(Navigator), "avoidRooms").SetValue(siegecart.Navigator, true);
            AccessTools.DeclaredField(typeof(Navigator), "preciseTarget").SetValue(siegecart.Navigator, false);
            AccessTools.DeclaredField(typeof(Navigator), "autoRotate").SetValue(siegecart.Navigator, false);
            AccessTools.DeclaredField(typeof(Navigator), "decelerate").SetValue(siegecart.Navigator, true);
            //AccessTools.DeclaredField(typeof(Looker), "hasFov").SetValue(siegecart.looker, true);
            siegecart.Navigator.maxSpeed = 15f;
            siegecart.Navigator.accel = 15f;
            AccessTools.DeclaredField(typeof(NPC), "ignoreBelts").SetValue(siegecart, true);
            AudioManager motorAudMan = siegecart.gameObject.AddComponent<PropagatedAudioManager>();
            motorAudMan.audioDevice = siegecart.gameObject.AddComponent<AudioSource>();
            AccessTools.DeclaredField(typeof(AudioManager), "soundOnStart").SetValue(motorAudMan, new SoundObject[]
            {
                ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "AudioClip", "NPCs", "Siege Canon Cart", "WagonMovement.wav"), "Sfx_SiegeCart_Wagon", SoundType.Effect, new Color(1f, 0.6470588f, 0.1529412f))
            });
            AccessTools.DeclaredField(typeof(AudioManager), "loopOnStart").SetValue(motorAudMan, true);
            AnimatedSpriteRotator hell = siegecart.spriteBase.AddComponent<AnimatedSpriteRotator>();
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "renderer").SetValue(hell, siegecart.spriteRenderer[0]);
            int siegeSize = 39;
            SpriteRotationMap idleRotationMap = new SpriteRotationMap()
            {
                angleCount = 8
            };
            Sprite[] idleStaticFrames = new Sprite[]
            {
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeFront.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeLeftFront.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeLeft.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeLeftBehind.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeBehind.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeRightBehind.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeRight.png"), siegeSize),
                AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "siegeRightFront.png"), siegeSize),
            };
            // I fucking hate this. Not because of the entire Dr. Reflex reference, it's because of the character usually being an animated 3d model on the decompile.
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(idleRotationMap, idleStaticFrames);
            SpriteRotationMap moveRotationMap = new SpriteRotationMap()
            {
                angleCount = 8
            };
            List<Sprite> movingFrames = new List<Sprite>();
            // THEY ARE IN ORDER FOR EASINESS, NOT GONNA BE AddToArray(new Sprite[]) EVERY TIME...
            for (int frame = 1; frame <= 7; frame++)
            {
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "front", "front_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "leftfront", "leftfront_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "left", "left_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "leftbehind", "leftbehind_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "behind", "behind_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "rightbehind", "rightbehind_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "right", "right_Moving (" + frame + ").png"), siegeSize));
                movingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "moving", "rightfront", "rightfront_Moving (" + frame + ").png"), siegeSize));
            }
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(moveRotationMap, movingFrames.ToArray());
            SpriteRotationMap shootRotationMap = new SpriteRotationMap()
            {
                angleCount = 8
            };
            List<Sprite> shootingFrames = new List<Sprite>();
            // Wait, I fucked up my code.
            for (int frame = 1; frame <= 8; frame++)
            {
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "front", "front_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "leftfront", "leftfront_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "left", "left_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "leftbehind", "leftbehind_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "behind", "behind_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "rightbehind", "rightbehind_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "right", "right_Shoot (" + frame + ").png"), siegeSize));
                shootingFrames.Add(AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Siege Canon Cart", "shooting", "rightfront", "rightfront_Shoot (" + frame + ").png"), siegeSize));
            }
            AccessTools.DeclaredField(typeof(SpriteRotationMap), "spriteSheet").SetValue(shootRotationMap, shootingFrames.ToArray());
            AccessTools.DeclaredField(typeof(AnimatedSpriteRotator), "spriteMap").SetValue(hell, new SpriteRotationMap[]
            {
                idleRotationMap,
                moveRotationMap,
                shootRotationMap
            });
            siegecart.spriteRenderer[0].transform.position = new Vector3(0,0.75f,0);
            SpriteRenderer nonvisible = Instantiate(siegecart.spriteRenderer[0]); // I copy this from the existing one to make it animated.
            nonvisible.transform.SetParent(siegecart.spriteBase.transform);
            nonvisible.gameObject.SetActive(false); // And then I later hide it!
            siegecart.spriteRenderer = siegecart.spriteRenderer.AddToArray(nonvisible);
            CustomSpriteAnimator animator = siegecart.gameObject.AddComponent<CustomSpriteAnimator>();
            animator.spriteRenderer = nonvisible;
            siegecart.animator = animator;
            siegecart.idleFrame = idleStaticFrames[0];
            siegecart.movingFrames = [movingFrames[0], movingFrames[8], movingFrames[8*2], movingFrames[8*3],
            movingFrames[8*4],movingFrames[8*5],movingFrames[8*6]];
            siegecart.shootingFrames = [shootingFrames[0], shootingFrames[8], shootingFrames[8*2], shootingFrames[8*3],
            shootingFrames[8*4],shootingFrames[8*5],shootingFrames[8*6],shootingFrames[8*7]];
            // The balls
            GameObject ball = Instantiate(Resources.FindObjectsOfTypeAll<ITM_BSODA>().ToList().Find(x => x.name == "ITM_BSODA")).gameObject;
            ball.name = "CanonBall";
            Destroy(ball.GetComponent<ITM_BSODA>());
            siegecart.ballPre = ball.AddComponent<SiegeCartBalls>();
            DontDestroyOnLoad(ball);
            ball.SetActive(false);
            ball.GetComponent<Entity>().SetActive(false);
            ball.layer = LayerMask.NameToLayer("StandardEntities"); // No wonder why it keeps setting to default...
            Destroy(ball.transform.Find("RendereBase").Find("Particles").gameObject);
            ball.transform.Find("RendereBase").GetComponentInChildren<SpriteRenderer>().sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "StupidBall.png"), 48f);

            var portalman = ObjectCreators.CreateNPC<MrPortalMan>("Mr. Portal Man", EnumExtensions.ExtendEnum<Character>("MrPortalMan"), ObjectCreators.CreatePosterObject(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "PRI_portalman.png"), []), usesHeatMap: true, spawnableRooms: [RoomCategory.Hall]);
            AccessTools.DeclaredField(typeof(Looker), "hasFov").SetValue(portalman.looker, false);
            AccessTools.DeclaredField(typeof(Looker), "layerMask").SetValue(portalman.looker, (LayerMask)AccessTools.DeclaredField(typeof(Looker), "layerMask").GetValue(NPCMetaStorage.Instance.Get(Character.Principal).value.looker));
            AccessTools.DeclaredField(typeof(NPC), "ignorePlayerOnSpawn").SetValue(portalman, true);
            portalman.spriteRenderer[0].sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "PortalMan.png"), 34f);
            portalman.spriteRenderer[0].transform.position = new Vector3(0f, 0.5f, 0f);
            /*Transform billboard = new GameObject("BillboardBase", typeof(BillboardUpdater)).transform;
            billboard.SetParent(portalman.spriteBase.transform);
            GameObject mask = new GameObject("Mask", typeof(MeshFilter), typeof(MeshRenderer));
            mask.transform.SetParent(billboard);
            mask.transform.localScale = portalman.spriteRenderer[0].transform.localScale;
            mask.transform.position = portalman.spriteRenderer[0].transform.position;
            MeshFilter mFilter = mask.GetComponent<MeshFilter>();
            GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mFilter.mesh = prim.GetComponent<MeshFilter>().sharedMesh;
            Destroy(prim);
            MeshRenderer mRender = mask.GetComponent<MeshRenderer>();
            Material mrportalMask = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "PortalMask"));
            mrportalMask.name = "MrPortalMask";
            mrportalMask.SetMaskTexture(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "PortalManMask.png"));
            mRender.materials = [mrportalMask, null];
            MaterialModifier.ChangeHole(mRender, mrportalMask, mRender.materials[1]);
            MaterialModifier.SetBase(mRender, Resources.FindObjectsOfTypeAll<Texture2D>().ToList().Find(x => x.name == "StoreWJohnny_Blank"));
            portalman.masking = mRender;*/
            CapsuleCollider trigger = portalman.baseTrigger[0] as CapsuleCollider;
            trigger.radius = 3;
            trigger.height = 12;
            // This is a room poster
            Material cryingportalMat = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "BlankChalk"));
            cryingportalMat.name = "CryingPortalMat";
            cryingportalMat.SetMainTexture(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "CryingPortal.png"));
            Material cryingportalMask = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "PortalMask"));
            cryingportalMask.name = "CryingPortalMask";
            cryingportalMask.SetMaskTexture(AssetLoader.TextureFromMod(this, "Texture2D", "NPCs", "Mr. Portal Man", "CryingPortalMask.png"));
            GameObject cryingPortal = Instantiate(Resources.FindObjectsOfTypeAll<Chalkboard>().ToList().First()).gameObject;
            cryingPortal.name = "CryingPortalOutput";
            cryingPortal.transform.Find("Chalkbaord").Find("Quad").GetComponent<MeshRenderer>().materials = [cryingportalMask, cryingportalMat];
            cryingPortal.transform.Find("Chalkbaord").Find("Quad").GetComponent<MeshCollider>().enabled = false;
            Destroy(cryingPortal.GetComponent<Chalkboard>());
            CryingPortal cry = cryingPortal.AddComponent<CryingPortal>();
            cry.mask = cryingportalMask;
            DontDestroyOnLoad(cryingPortal);
            portalman.portalPre = cryingPortal.GetComponent<CryingPortal>();
            cryingPortal.SetActive(false);

            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [rpsguy], "RPS Guy", NPCFlags.Standard));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [errorbot], "ERROR-BOT_ITEM-STEALER", NPCFlags.Standard | NPCFlags.MakeNoise));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [siegecart], "Siege Canon Cart", NPCFlags.StandardNoCollide));
            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, [portalman], "Mr. Portal Man", NPCFlags.Standard));

            // Pretty sure this hammer came in before BBT, but I didn't really know how to add items to arrays back then...
            ItemObject hammer = ObjectCreators.CreateItemObject("Itm_BHammer", "Desc_BHammer", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Hammer_Small.png"), 1f), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "Hammer_Large.png"), 50f), EnumExtensions.ExtendEnum<Items>("BHammer"), 25, 30);
            ITM_Hammer hammerFunc = new GameObject("BHammer", typeof(ITM_Hammer)).GetComponent<ITM_Hammer>();
            hammer.item = hammerFunc;
            DontDestroyOnLoad(hammerFunc.gameObject);
            // Annnddd, this is something stupidly uncool and sometimes creative...
            ItemObject profitCard = ObjectCreators.CreateItemObject("Itm_ProfitCard", "Desc_ProfitCard", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "ProfitCard_Small.png"), 1f), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "ProfitCard_Large.png"), 50f), EnumExtensions.ExtendEnum<Items>("ProfitCard"), 85, 25);
            ITM_Acceptable profitCardFunc = Instantiate(Resources.FindObjectsOfTypeAll<ITM_Acceptable>().ToList().Find(p => p.name == "Quarter"));
            profitCardFunc.name = "ProfitCard";
            profitCardFunc.ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("ProfitCard"));
            profitCard.item = profitCardFunc;
            DontDestroyOnLoad(profitCardFunc.gameObject);
            profitCardInsert = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "BSODAMachine"));
            profitCardInsert.name = "ProfitCardMachine";
            profitCardInsert.SetMainTexture(AssetLoader.TextureFromMod(this, "Texture2D", "ProfitCardMachine.png"));
            // Something evil is brewing inside!
            ItemObject swingingdoorInfKey = ObjectCreators.CreateItemObject("Itm_UnsecuredKey", "Desc_UnsecuredKey", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorKey_Small.png"), 1f), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorKey_Large.png"), 50f), EnumExtensions.ExtendEnum<Items>("UnsecuredYellowKey"), 110, 50);
            ITM_Acceptable infKeyFunc = Instantiate(Resources.FindObjectsOfTypeAll<ITM_Acceptable>().ToList().Find(p => p.name == "DetentionKey"));
            infKeyFunc.name = "UnsecuredLock";
            infKeyFunc.ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey"));
            infKeyFunc.ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "Doors_StandardUnlock"));
            swingingdoorInfKey.item = infKeyFunc;
            DontDestroyOnLoad(infKeyFunc.gameObject);
            ItemObject swingingdoorInfLock = ObjectCreators.CreateItemObject("Itm_SecuredLock", "Desc_SecuredLock", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorInfLock_Small.png"), 1f), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Texture2D", "Items", "YellowDoorInfLock_Large.png"), 50f), EnumExtensions.ExtendEnum<Items>("SecuredYellowLock"), 95, 75);
            ITM_Acceptable infswingdoorlockFunc = Instantiate(Resources.FindObjectsOfTypeAll<ITM_Acceptable>().ToList().Find(p => p.name == "SwingDoorLock"));
            infswingdoorlockFunc.name = "SecuredLock";
            infswingdoorlockFunc.ReflectionSetVariable("item", EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock"));
            infswingdoorlockFunc.ReflectionSetVariable("audUse", Resources.FindObjectsOfTypeAll<SoundObject>().ToList().Find(x => x.name == "Slap"));
            swingingdoorInfLock.item = infswingdoorlockFunc;
            DontDestroyOnLoad(infswingdoorlockFunc.gameObject);
            GameObject inflockedSwingDoor = Instantiate(Resources.FindObjectsOfTypeAll<CoinDoor>().ToList().Find(p => p.name == "Door_SwingingCoin")).gameObject;
            inflockedSwingDoor.name = "Door_SwingingSecuredLock";
            Destroy(inflockedSwingDoor.GetComponent<CoinDoor>());
            inflockedSwingDoor.AddComponent<SecuredSwingingDoor>();
            securedYellowSwingingDoor = Instantiate(Resources.FindObjectsOfTypeAll<Material>().ToList().Find(x => x.name == "CoinDoor"));
            securedYellowSwingingDoor.name = "SecuredLockDoor";
            securedYellowSwingingDoor.SetMainTexture(AssetLoader.TextureFromMod(this, "Texture2D", "SwingDoor_SecuredLocked.png"));
            //inflockedSwingDoor.GetComponent<SecuredSwingingDoor>().doorOverlay = securedYellowSwingingDoor;
            //inflockedSwingDoor.SetActive(false);
            inflockedSwingDoor.GetComponent<AudioManager>().enabled = false;
            DontDestroyOnLoad(inflockedSwingDoor);
            CoinDoorBuilder builder = Instantiate(Resources.FindObjectsOfTypeAll<CoinDoorBuilder>().ToList().Find(x => x.name == "CoinDoorBuilder"));
            builder.gameObject.name = "Inf Secured Swinging Door Builder";
            builder.ReflectionSetVariable("doorPre", inflockedSwingDoor.GetComponent<SwingDoor>());
            builder.obstacle = EnumExtensions.ExtendEnum<Obstacle>("InfLockedDoor");
            DontDestroyOnLoad(builder.gameObject);
            ObjectBuilderMetaStorage.Instance.Add(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor"), new ObjectBuilderMeta(Info, builder));

            ItemMetaStorage.Instance.Add(hammer, new ItemMetaData(Info, hammer));
            ItemMetaStorage.Instance.Add(profitCard, new ItemMetaData(Info, profitCard));
            ItemMetaStorage.Instance.Add(swingingdoorInfKey, new ItemMetaData(Info, swingingdoorInfKey));
            ItemMetaStorage.Instance.Add(swingingdoorInfLock, new ItemMetaData(Info, swingingdoorInfLock));

            // The Basement
            lBasement = new LevelObject()
            {
                name = "Basement1",
                previousLevels = [Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main1")],

                minSize = new IntVector2(40, 45),
                maxSize = new IntVector2(50, 55),
                minPlots = 4,
                maxPlots = 11,
                minPlotSize = 3,
                outerEdgeBuffer = 7,

                minHallsToRemove = 3,
                maxHallsToRemove = 6,
                minSideHallsToRemove = 2,
                maxSideHallsToRemove = 3,
                minReplacementHalls = 5,
                maxReplacementHalls = 7,
                bridgeTurnChance = 4,
                additionTurnChance = 9,
                maxHallAttempts = 9, // THIS HAS HELPED ME, THANK GOD!
                deadEndBuffer = 6,
                includeBuffers = true,

                hallWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallWallTexs,
                hallFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallFloorTexs,
                hallCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallCeilingTexs,
                hallLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").hallLights,
                maxLightDistance = 9,

                minPrePlotSpecialHalls = 1,
                minPostPlotSpecialHalls = 0,
                maxPrePlotSpecialHalls = 4,
                maxPostPlotSpecialHalls = 2,
                prePlotSpecialHallChance = 0.5f,
                postPlotSpecialHallChance = 0.25f,
                potentialPrePlotSpecialHalls = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialPrePlotSpecialHalls,
                potentialPostPlotSpecialHalls = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialPostPlotSpecialHalls,
                standardHallBuilders = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardHallBuilders,

                minSpecialBuilders = 4,
                maxSpecialBuilders = 5,
                forcedSpecialHallBuilders = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").forcedSpecialHallBuilders,
                specialHallBuilders = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").specialHallBuilders,
                            new WeightedObjectBuilder()
                            {
                                selection = ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor")).value,
                                weight = 75
                            }],

                minClassRooms = 7,
                maxClassRooms = 7,
                classStickToHallChance = 1,
                potentialClassRooms = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialClassRooms,
                minFacultyRooms = 10,
                maxFacultyRooms = 14,
                facultyStickToHallChance = 0.75f,
                potentialFacultyRooms = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialFacultyRooms,
                minExtraRooms = 0,
                maxExtraRooms = 0,
                extraStickToHallChance = 0.75f,
                potentialExtraRooms = [],
                minOffices = 1,
                maxOffices = 1,
                officeStickToHallChance = 1,
                potentialOffices = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialOffices,
                minRoomSize = new IntVector2(3, 5),

                centerWeightMultiplier = 25,
                perimeterBase = 4,
                dijkstraWeightValueMultiplier = 0.3f,
                dijkstraWeightPower = 1.6f,
                extraDoorChance = 0.15f,
                additionalHallDoorRequirementMultiplier = 4,
                hallPriorityDampening = 3,

                classWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classWallTexs,
                classFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classFloorTexs,
                classCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classCeilingTexs,
                facultyWallTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyWallTexs,
                facultyFloorTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyFloorTexs,
                facultyCeilingTexs = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyCeilingTexs,
                classLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").classLights,
                facultyLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").facultyLights,
                officeLights = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").officeLights,

                standardDoorMat = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardDoorMat,
                minSpecialRooms = 2,
                maxSpecialRooms = 2,
                specialRoomsStickToEdge = true,
                potentialSpecialRooms = [
                new WeightedRoomAsset()
                        {
                            selection = Resources.FindObjectsOfTypeAll<RoomAsset>().ToList().Find(x => x.name.Contains("Cafeteria")),
                            weight = 100
                        }
            ],
                windowChance = 0.5f,

                lightMode = LightMode.Greatest,
                standardLightStrength = 5,
                standardLightColor = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardLightColor,
                standardDarkLevel = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").standardDarkLevel,
                potentialBaldis = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").potentialBaldis,
                additionalNPCs = 1,
                potentialNPCs = [
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
                }],
                forcedNpcs = [
                NPCMetaStorage.Instance.Get(Character.LookAt).value,
                ],
                posterChance = 2,
                posters = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").posters,
                items = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").items,
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 75
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
                                weight = 100
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock")).value,
                                weight = 75
                            }
                            ],
                singleEntranceItemVal = 10,
                noHallItemVal = 15,
                minEvents = 2,
                maxEvents = 5,
                initialEventGap = 0,
                minEventGap = 60,
                maxEventGap = 150,
                randomEvents = [
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
                ],

                fieldTrip = false,
                fieldTrips = [],
                tripEntrancePre = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").tripEntrancePre,
                tripEntranceRoom = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").tripEntranceRoom,
                fieldTripItems = [],

                exitCount = 4,
                elevatorPre = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").elevatorPre,
                elevatorRoom = Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main3").elevatorRoom,
                hallBuffer = 4,
                edgeBuffer = 3,

                mapPrice = 150,
                shopItems = [..Resources.FindObjectsOfTypeAll<LevelObject>().ToList().Find(x => x.name == "Main2").shopItems,
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value,
                                weight = 80
                            },
                            new WeightedItemObject()
                            {
                                selection = ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value,
                                weight = 100
                            }],
                totalShopItems = 6,
                finalLevel = true,
                timeBonusLimit = 780,
                timeBonusVal = 200
            };
            sBasement = new SceneObject()
            {
                name = "BasementLevel_1",
                manager = Resources.FindObjectsOfTypeAll<MainGameManager>().ToList().Find(x => x.name.Contains("Lvl3")),
                levelObject = lBasement,
                skybox = Resources.FindObjectsOfTypeAll<Cubemap>().ToList().Find(x => x.name.Contains("DayStandard")),
                skyboxColor = Color.white,
                nextLevel = Resources.FindObjectsOfTypeAll<SceneObject>().ToList().Find(x => x.name.Contains("Placeholder")),
                levelTitle = "B1",
                levelNo = -1
            };

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
