using BaldiLevelEditor;
using BCarnellChars;
using BCarnellChars.OtherStuff;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.UI;
using PlusLevelFormat;
using PlusLevelLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace BCarnellEditor
{
    [BepInPlugin("alexbw145.baldiplus.bcarnelleditor", "B. Carnell Chars + Plus Level Editor Compatibility", "1.1.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars", "1.1.0")]
    [BepInDependency("mtm101.rulerp.baldiplus.leveleditor", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "4.2.0.0")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnelleditor");
            harmony.PatchAllConditionals();
            AddSpriteFolderToAssetMan("EditorUI/", 40f, AssetLoader.GetModPath(BasePlugin.Instance), "EditorUI");
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad, true);
        }

        private void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path)
        {
            string[] files = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < files.Length; i++)
            {
                BasePlugin.bcppAssets.Add(prefix + Path.GetFileNameWithoutExtension(files[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(files[i]), pixelsPerUnit));
            }
        }

        public static List<BCPP_RotateAndPlacePrefab> sodaToolcats = new List<BCPP_RotateAndPlacePrefab>();
        private void PostLoad()
        {
            // Dunno why doors are first...
            BaldiLevelEditorPlugin.doorTypes.Add("inflocked", typeof(InfLockedSwingEditorVisual));
            PlusLevelLoaderPlugin.Instance.doorPrefabs.Add("inflocked", ObjectBuilderMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Obstacle>("InfLockedDoor")).value.ReflectionGetVariable("doorPre") as Door);

            GameObject rpsguy = BaldiLevelEditorPlugin.StripAllScripts(NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value.gameObject, true);
            // RPS LOOKS LIKE BEANS, SO UHH... CHANGE SPRITE I GUESS??
            rpsguy.transform.Find("SpriteBase").GetComponentInChildren<SpriteRenderer>().sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(BasePlugin.Instance, "Texture2D", "NPCs", "RPS Guy", "RPSDude.png"), 100);
            BaldiLevelEditorPlugin.characterObjects.Add("rpsguy", rpsguy);
            GameObject errorbotBody = BaldiLevelEditorPlugin.StripAllScripts(NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value.gameObject, true);
            // ERROR-BOT'S BODY IS BEANS, WTF??
            errorbotBody.transform.Find("SpriteBase").GetComponentsInChildren<SpriteRenderer>()[0].sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(BasePlugin.Instance, "Texture2D", "NPCs", "ERRORBOT_ITEMSTEALER", "error-botBody", "error-botBody-1.png"), 100f);
            BaldiLevelEditorPlugin.characterObjects.Add("errorbot", errorbotBody);
            GameObject siegecanoncart = BaldiLevelEditorPlugin.StripAllScripts(NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value.gameObject, true);
            // EVEN SIEGE CANON CART IS A FUCKING BEANS, A LITERAL FUCKING BEANS!!
            siegecanoncart.transform.Find("SpriteBase").GetComponentInChildren<SpriteRenderer>().sprite = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(BasePlugin.Instance, "Texture2D", "NPCs", "Siege Canon Cart", "siegeFront.png"), 39);
            BaldiLevelEditorPlugin.characterObjects.Add("siegecanoncart", siegecanoncart);
            BaldiLevelEditorPlugin.characterObjects.Add("mrportalman", BaldiLevelEditorPlugin.StripAllScripts(NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value.gameObject, true));

            PlusLevelLoaderPlugin.Instance.npcAliases.Add("rpsguy", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).value);
            PlusLevelLoaderPlugin.Instance.npcAliases.Add("errorbot", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("ERRORBOT")).value);
            PlusLevelLoaderPlugin.Instance.npcAliases.Add("siegecanoncart", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")).value);
            PlusLevelLoaderPlugin.Instance.npcAliases.Add("mrportalman", NPCMetaStorage.Instance.Get(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).value);

            foreach (SodaMachine soda in Resources.FindObjectsOfTypeAll<SodaMachine>()) // Might be too flexible I mean...
            {
                if (!BaldiLevelEditorPlugin.editorObjects.Exists(x => x.prefab.gameObject.name.ToLower().Contains(soda.name.ToLower())))
                    continue;
                //Debug.Log("Found SodaMachine: " + soda + ", which is: " + BaldiLevelEditorPlugin.editorObjects.Find(x => x.prefab.gameObject.name.ToLower().Contains(soda.name.ToLower())).name);
                string name = BaldiLevelEditorPlugin.editorObjects.Find(x => x.prefab.gameObject.name.ToLower().Contains(soda.name.ToLower())).name;
                GameObject sodaProfitCard = Instantiate(soda).gameObject;
                sodaProfitCard.name = soda.name + " (ProfitCard)";
                Destroy(sodaProfitCard.GetComponent<ToProfitCardCost>());
                sodaProfitCard.GetComponent<SodaMachine>().ReflectionSetVariable("requiredItem", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                sodaProfitCard.GetComponent<MeshRenderer>().materials = sodaProfitCard.GetComponent<MeshRenderer>().GetMaterialArray().AddToArray(BasePlugin.profitCardInsert);
                if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // Not again...
                    AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                        .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                        .SetValue(sodaProfitCard.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                        .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").itemType });
                sodaProfitCard.ConvertToPrefab(true);
                BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(name + "profit", sodaProfitCard, Vector3.zero));
                PlusLevelLoaderPlugin.Instance.prefabAliases.Add(name + "profit", sodaProfitCard);
                sodaToolcats.Add(new (name + "profit", true,
                BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Object_" + name) != null ? BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Object_" + name) : BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Object_bsodamachine")));
                // Dunno if people are NOT gonna use the assetman from the base level editor itself...
            }

            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bcpp_randomitemmachine", BasePlugin.bcppAssets.Get<GameObject>("Structures/RandomItemMachine"), Vector3.zero));
            PlusLevelLoaderPlugin.Instance.prefabAliases.Add("bcpp_randomitemmachine", BasePlugin.bcppAssets.Get<GameObject>("Structures/RandomItemMachine"));

            BaldiLevelEditorPlugin.itemObjects.Add("profitcard", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
            BaldiLevelEditorPlugin.itemObjects.Add("bhammer", BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"));
            BaldiLevelEditorPlugin.itemObjects.Add("unsecuredyellowkey", BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"));
            BaldiLevelEditorPlugin.itemObjects.Add("securedyellowlock", BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"));
            BaldiLevelEditorPlugin.itemObjects.Add("anyportaloutput", BasePlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"));
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("profitcard", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("bhammer", BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"));
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("unsecuredyellowkey", BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"));
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("securedyellowlock", BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"));
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("anyportaloutput", BasePlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"));


        }
    }

    public class BCPP_NPCPropertyTool : NpcTool
    {
        public override Sprite editorSprite => BasePlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_" + _prefab);
        private string _prefab; // Why is this private on the original script??

        public BCPP_NPCPropertyTool(string prefab) : base(prefab)
        {
            _prefab = prefab;
        }
    }

    public class BCPP_ItemPropertyTool : ItemTool
    {
        public override Sprite editorSprite => BasePlugin.bcppAssets.Get<Sprite>("EditorUI/ITM_" + _item);
        private string _item;

        public BCPP_ItemPropertyTool(string obj) : base(obj)
        {
            _item = obj;
        }
    }

    public class BCPP_SwingingDoorTool : SwingingDoorTool
    {
        public override Sprite editorSprite => BasePlugin.bcppAssets.Get<Sprite>("EditorUI/" + _type + "_SwingDoorED");

        public BCPP_SwingingDoorTool(string type) : base(type)
        {
        }
    }

    public class BCPP_RotateAndPlacePrefab : RotateAndPlacePrefab
    {
        private Sprite icon;
        public override Sprite editorSprite => icon;
        protected bool isProfitCard;
        public bool IsProfitCard => isProfitCard;
        public BCPP_RotateAndPlacePrefab(string obj, bool profitcard = false, Sprite _icon = null) : base(obj)
        {
            isProfitCard = profitcard;
            if (_icon == null) icon = BasePlugin.bcppAssets.Get<Sprite>("EditorUI/Object_" + obj);
            else icon = _icon;
        }
    }

    public class InfLockedSwingEditorVisual : DoorEditorVisual
    {
        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoorMask"));
            renderer.materials[1].SetMainTexture(BasePlugin.securedYellowSwingingDoor.mainTexture);
        }
    }

    [HarmonyPatch(typeof(PlusLevelEditor), "Initialize")]
    class FuckInit
    {
        static void Postfix(PlusLevelEditor __instance)
        {
            __instance.toolCats.Find(x => x.name == "doors").tools.Add(new BCPP_SwingingDoorTool("inflocked"));
            __instance.toolCats.Find(x => x.name == "objects").tools.AddRange([..Plugin.sodaToolcats, new BCPP_RotateAndPlacePrefab("bcpp_randomitemmachine")]);
            __instance.toolCats.Find(x => x.name == "characters").tools.AddRange([
                new BCPP_NPCPropertyTool("rpsguy"),
                new BCPP_NPCPropertyTool("errorbot"),
                new BCPP_NPCPropertyTool("siegecanoncart"),
                new BCPP_NPCPropertyTool("mrportalman")
                ]);
            __instance.toolCats.Find(x => x.name == "items").tools.AddRange([
                new BCPP_ItemPropertyTool("profitcard"),
                new BCPP_ItemPropertyTool("bhammer"),
                new BCPP_ItemPropertyTool("unsecuredyellowkey"),
                new BCPP_ItemPropertyTool("securedyellowlock"),
                new BCPP_ItemPropertyTool("anyportaloutput")
                ]);
        }
    }

    [HarmonyPatch(typeof(PlusLevelEditor), "CreateToolButton")]
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
    }

    // Useless rn...
    [HarmonyPatch(typeof(CustomLevelLoader), "LoadLevel")]
    class VendingMachineStopRNG
    {
        static void Prefix()
        {
            BasePlugin.inLevelEditor = true;
        }
    }
    [HarmonyPatch(typeof(CoreGameManager), "ReturnToMenu")]
    class ResetVariable
    {
        static void Prefix()
        {
            BasePlugin.inLevelEditor = false;
        }
    }
}
