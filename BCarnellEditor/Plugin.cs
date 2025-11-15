using BCarnellChars;
using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.UI;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.TextCore;
using UnityEngine.UI;
using static UnityEngine.ParticleSystemJobs.NativeParticleData;

namespace BCarnellEditor
{
    [BepInPlugin("alexbw145.baldiplus.bcarnelleditor", "B. Carnell Chars + Plus Level Editor Compatibility", "2.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars", "1.5.2")]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "8.1.0.0")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnelleditor");
            harmony.PatchAllConditionals();
            AddSpriteFolderToAssetMan("EditorUI/", 40f, AssetLoader.GetModPath(BasePlugin.Instance), "EditorUI");
            LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
            {
                AssetLoader.LocalizationFromFunction((lang) => new Dictionary<string, string>()
                {
                    { "Ed_Tool_item_profitcard_Desc", "An alternative quarter that's only used for alternative quarter inputs." },
                    { "Ed_Tool_item_bhammer_Desc", "Not safe for windows (and rock guys)" },
                    { "Ed_Tool_item_unsecuredyellowkey_Desc", "Don't lose it!" },
                    { "Ed_Tool_item_securedyellowlock_Desc", "Don't place it!" },
                    { "Ed_Tool_item_anyportaloutput_Desc", "Can only be placed if Mr. Portal Man is present, but its usable for planned escape routes!" },
                    { "Ed_Tool_item_taserpulse_Desc", "ZAP! This bad boy chains through three bad criminals! But can lethally get you into trouble!" },

                    { "Ed_Tool_door_inflocked_Title", "Secured Locked Swinging Door" },
                    { "Ed_Tool_door_inflocked_Desc", "It's forever to be locked..." },
                    { "Ed_Tool_object_bcpp_randomitemmachine_Title", "Mystery Profit Item Machine"},
                    { "Ed_Tool_object_bcpp_randomitemmachine_Desc", "<i>What will it give you?</i>"},

                    { "Ed_BCPP_RPSGuy_Title", "RPS Guy" },
                    { "Ed_BCPP_RPSGuy_Desc", "This kid loves playing RPS with someone! He has won atleast an 8 winstreak for his career!" },
                    { "Ed_BCPP_ERRORBOT_Title", "ERROR-BOT_ITEM-STEALER" },
                    { "Ed_BCPP_ERRORBOT_Desc", "Due to its unstable behavior, it somewhat steals intruder's items." },
                    { "Ed_BCPP_SiegeCanonCart_Title", "Siege Canon Cart" },
                    { "Ed_BCPP_SiegeCanonCart_Desc", "This bad boy has some big balls that can shoot out!\n<b>Watch out for its firepower!</b>" },
                    { "Ed_BCPP_MrPortalMan_Title", "Mr. Portal Man" },
                    { "Ed_BCPP_MrPortalMan_Desc", "So lemmie say this person has swallowed an object object and later teleported that object into one of its counterparts. <i>Yeah that's a portal man...</i>" },
                    { "Ed_BCPP_MrRobber_Title", "Mr. Robber" },
                    { "Ed_BCPP_MrRobber_Desc", "This thief is off onto collecting notebooks, but you shouldn't be alone by now..." },
                });
            }, LoadingEventOrder.Start);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad, LoadingEventOrder.Post);
        }

        private void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path)
        {
            string[] files = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < files.Length; i++)
            {
                BasePlugin.bcppAssets.Add(prefix + Path.GetFileNameWithoutExtension(files[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(files[i]), pixelsPerUnit));
            }
        }

        public static List<ObjectToolProfitCard> sodaToolcats = new List<ObjectToolProfitCard>();
        private void PostLoad()
        {
            // Dunno why doors are first...
            var infdoorlocked = AccessTools.Field(typeof(BasePlugin), "securedYellowSwingingDoor").GetValue(null) as Material;
            EditorInterface.AddDoor<DoorDisplay>("inflocked", DoorIngameStatus.AlwaysObject, Resources.FindObjectsOfTypeAll<SwingDoor>().Last().mask.First(), [infdoorlocked, infdoorlocked]);
            LevelLoaderPlugin.Instance.tileBasedObjectPrefabs.Add("inflocked", BasePlugin.bcppAssets.Get<SecuredSwingingDoor>("Obstacles/SecuredSwingingDoor").GetComponent<SwingDoor>());

            NPC rpsguy = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value;
            // RPS LOOKS LIKE BEANS, SO UHH... CHANGE SPRITE I GUESS??
            rpsguy.transform.Find("SpriteBase").GetComponentInChildren<SpriteRenderer>().sprite = BCarnellChars.BasePlugin.bcppAssets.Get<Sprite>("RPSGuy/Alive");
            EditorInterface.AddNPCVisual("rpsguy", rpsguy);
            NPC errorbotBody = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value;
            // ERROR-BOT'S BODY IS BEANS, WTF??
            errorbotBody.transform.Find("SpriteBase").GetComponentsInChildren<SpriteRenderer>()[0].sprite = BCarnellChars.BasePlugin.bcppAssets.Get<Sprite>("ERRORBOT/Body3");
            EditorInterface.AddNPCVisual("errorbot", errorbotBody);
            NPC siegecanoncart = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value;
            // EVEN SIEGE CANON CART IS A FUCKING BEANS, A LITERAL FUCKING BEANS!!
            siegecanoncart.transform.Find("SpriteBase").GetComponentInChildren<SpriteRenderer>().sprite = BCarnellChars.BasePlugin.bcppAssets.Get<Sprite>("SiegeCanonCart/IdleFront");
            EditorInterface.AddNPCVisual("siegecanoncart", siegecanoncart);
            var mrportalman = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value;
            EditorInterface.AddNPCVisual("mrportalman", mrportalman);
            NPC mrrobber = NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrRobber")).value;
            mrrobber.transform.Find("SpriteBase").GetComponentInChildren<SpriteRenderer>().sprite = BCarnellChars.BasePlugin.bcppAssets.Get<Sprite>("MrRobber/Targeting");
            EditorInterface.AddNPCVisual("mrrobber", mrrobber);

            LevelLoaderPlugin.Instance.npcAliases.Add("rpsguy", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value);
            LevelLoaderPlugin.Instance.npcAliases.Add("errorbot", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value);
            LevelLoaderPlugin.Instance.npcAliases.Add("siegecanoncart", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value);
            LevelLoaderPlugin.Instance.npcAliases.Add("mrportalman", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value);
            LevelLoaderPlugin.Instance.npcAliases.Add("mrrobber", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrRobber")).value);

            /*foreach (SodaMachine soda in Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().FindAll(x => x.gameObject.GetComponent<ToProfitCardCost>())) // Might be too flexible I mean...
            {
                if (!LevelStudioPlugin.Instance.basicObjectDisplays.Values.Where(x => x.gameObject.name.ToLower().Contains(soda.name.ToLower())
                    || LevelLoaderPlugin.Instance.basicObjects.ContainsKey(LevelStudioPlugin.Instance.basicObjectDisplays.Find(x => x.prefab.gameObject.name.ToLower().Contains("_" + soda.name.ToLower())).name + "profit"))
                    continue;
                Debug.Log("Found SodaMachine: " + soda + ", which is: " + BaldiLevelEditorPlugin.editorObjects.Find(x => x.prefab.gameObject.name.ToLower().Contains("_" + soda.name.ToLower())).name);
                string name = BaldiLevelEditorPlugin.editorObjects.Find(x => x.prefab.gameObject.name.ToLower().Contains("_" + soda.name.ToLower())).name;
                GameObject sodaProfitCard = Instantiate(soda).gameObject;
                sodaProfitCard.name = soda.name + " (ProfitCard)";
                Destroy(sodaProfitCard.GetComponent<ToProfitCardCost>());
                sodaProfitCard.GetComponent<SodaMachine>().ReflectionSetVariable("requiredItem", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                sodaProfitCard.GetComponent<MeshRenderer>().materials = sodaProfitCard.GetComponent<MeshRenderer>().GetMaterialArray().AddToArray(BasePlugin.profitCardInsert);
                if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // Not again...
                    if (gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")) != null)
                        AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                        .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                        .SetValue(sodaProfitCard.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                        .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").itemType });
                sodaProfitCard.ConvertToPrefab(true);
                BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(name + "profit", sodaProfitCard, Vector3.zero));
                PlusLevelLoaderPlugin.Instance.prefabAliases.Add(name + "profit", sodaProfitCard);
                if (Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(s => s.name.ToLower().EndsWith("_" + name.ToLower())) == null)
                    Debug.LogWarning(name.ToLower() + " has a missing icon.");
                sodaToolcats.Add(new (name + "profit", true,
                Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(s => s.name.ToLower().EndsWith("_" + name.ToLower())) ?? BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Object_bsodamachine")));
            }*/

            EditorInterface.AddObjectVisual("bcpp_randomitemmachine", BasePlugin.bcppAssets.Get<GameObject>("Structures/RandomItemMachine"), true);
            LevelLoaderPlugin.Instance.basicObjects.Add("bcpp_randomitemmachine", BasePlugin.bcppAssets.Get<GameObject>("Structures/RandomItemMachine"));

            /*BaldiLevelEditorPlugin.itemObjects.Add("profitcard", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
            BaldiLevelEditorPlugin.itemObjects.Add("bhammer", BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"));
            BaldiLevelEditorPlugin.itemObjects.Add("unsecuredyellowkey", BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"));
            BaldiLevelEditorPlugin.itemObjects.Add("securedyellowlock", BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"));
            BaldiLevelEditorPlugin.itemObjects.Add("anyportaloutput", BasePlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"));
            BaldiLevelEditorPlugin.itemObjects.Add("taserpulse", BasePlugin.bcppAssets.Get<ItemObject>("Items/TASERPulse"));*/
            LevelLoaderPlugin.Instance.itemObjects.Add("profitcard", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
            LevelLoaderPlugin.Instance.itemObjects.Add("bhammer", BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"));
            LevelLoaderPlugin.Instance.itemObjects.Add("unsecuredyellowkey", BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"));
            LevelLoaderPlugin.Instance.itemObjects.Add("securedyellowlock", BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"));
            LevelLoaderPlugin.Instance.itemObjects.Add("anyportaloutput", BasePlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"));
            LevelLoaderPlugin.Instance.itemObjects.Add("taserpulse", BasePlugin.bcppAssets.Get<ItemObject>("Items/TASERPulse"));

#if DEBUG
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("B2Platform_Sideways".ToLower(), BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Sideways"), Vector3.zero, true));
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("B2Platform_Corner".ToLower(), BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Corner"), Vector3.zero, true));
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("B2Platform_OneSide".ToLower(), BasePlugin.bcppAssets.Get<GameObject>("B2Platform/OneSide"), Vector3.zero, true));
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("B2Platform_Open".ToLower(), BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Open"), Vector3.zero, true));
#endif
        }
    }

    public class ObjectToolProfitCard : ObjectTool
    {
        protected bool isProfitCard;
        public bool IsProfitCard => isProfitCard;
        public ObjectToolProfitCard(string type, Sprite sprite, float offset, bool profitcard) : base(type, sprite, offset)
        {
            isProfitCard = profitcard;
            if (sprite == null) this.sprite = BasePlugin.bcppAssets.Get<Sprite>("EditorUI/Object_" + type);
        }
    }

    public class ObjectToolNoRotationProfitCard : ObjectToolNoRotation
    {
        protected bool isProfitCard;
        public bool IsProfitCard => isProfitCard;
        public ObjectToolNoRotationProfitCard(string type, Sprite sprite, float offset, bool profitcard) : base(type, sprite, offset)
        {
            isProfitCard = profitcard;
            if (sprite == null) this.sprite = BasePlugin.bcppAssets.Get<Sprite>("EditorUI/Object_" + type);
        }
    }

    public class NPCToolBCPP : NPCTool
    {
        private string _titleKey;
        private string _descKey;
        public NPCToolBCPP(string npc, Sprite sprite, string titleKey, string descKey) : base (npc, sprite)
        {
            _titleKey = titleKey;
            _descKey = descKey;
        }

        public override string titleKey => _titleKey;
        public override string descKey => _descKey;
    }

    [HarmonyPatch(typeof(EditorController), nameof(EditorController.EditorModeAssigned))]
    class FuckInit
    {
        static void Prefix(EditorController __instance)
        {

            __instance.currentMode.availableTools["doors"].Add(new DoorTool("inflocked", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/inflocked_SwingDoorED")));
            __instance.currentMode.availableTools["objects"].AddRange([..Plugin.sodaToolcats, new ObjectToolNoRotation("bcpp_randomitemmachine", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/Object_bcpp_randomitemmachine"))]);
#if DEBUG
            __instance.toolCats.Find(x => x.name == "objects").tools.AddRange([
                new BCPP_RotateAndPlacePrefab("B2Platform_Sideways".ToLower()),
                new BCPP_RotateAndPlacePrefab("B2Platform_Corner".ToLower()),
                new BCPP_RotateAndPlacePrefab("B2Platform_OneSide".ToLower()),
                new BCPP_RotateAndPlacePrefab("B2Platform_Open".ToLower())
                ]);
#endif
            __instance.currentMode.availableTools["npcs"].AddRange([
                new NPCToolBCPP("rpsguy", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_rpsguy"), "Ed_BCPP_RPSGuy_Title", "Ed_BCPP_RPSGuy_Desc"),
                new NPCToolBCPP("errorbot", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_errorbot"), "Ed_BCPP_ERRORBOT_Title", "Ed_BCPP_ERRORBOT_Desc"),
                new NPCToolBCPP("siegecanoncart", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_siegecanoncart"), "Ed_BCPP_SiegeCanonCart_Title", "Ed_BCPP_SiegeCanonCart_Desc"),
                new NPCToolBCPP("mrportalman", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_mrportalman"), "Ed_BCPP_MrPortalMan_Title", "Ed_BCPP_MrPortalMan_Desc"),
                new NPCToolBCPP("mrrobber", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_mrrobber"), "Ed_BCPP_MrRobber_Title", "Ed_BCPP_MrRobber_Desc"),
                ]);
            __instance.currentMode.availableTools["items"].AddRange([
                new ItemTool("profitcard", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_profitcard")),
                new ItemTool("bhammer", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_bhammer")),
                new ItemTool("unsecuredyellowkey", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_unsecuredyellowkey")),
                new ItemTool("securedyellowlock", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_securedyellowlock")),
                new ItemTool("anyportaloutput", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_anyportaloutput")),
                new ItemTool("taserpulse", BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_taserpulse")),
                ]);
        }
    }

    /*[HarmonyPatch(typeof(PlusLevelEditor), "CreateToolButton")]
    class ProfitIcon
    {
        static void Postfix(Transform parent, EditorTool tool, ref ToolIconManager __result)
        {
            if (tool.GetType().Equals(typeof(BCPP_RotateAndPlacePrefab))) // More flexibility if mod support
            {
                BCPP_RotateAndPlacePrefab type = tool as BCPP_RotateAndPlacePrefab;
                if (type.IsProfitCard)
                {
                    Image icon = UIHelpers.CreateImage(BasePlugin.bcppAssets.Get<Sprite>("EditorUI/Icon_Profit"), __result.transform, new Vector2(0f, 0f), false);
                    icon.raycastTarget = false;
                    icon.transform.SetParent(__result.transform, false);
                }
            }
        }
    }*/
}
