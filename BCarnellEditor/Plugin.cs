using BaldiLevelEditor;
using BCarnellChars;
using BCarnellChars.OtherStuff;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using PlusLevelFormat;
using PlusLevelLoader;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.TextCore;

namespace BCarnellEditor
{
    [BepInPlugin("alexbw145.baldiplus.bcarnelleditor", "B. Carnell Chars + Plus Level Editor Compatibility", "1.0.0.0")]
    [BepInDependency("alexbw145.baldiplus.bcarnellchars")]
    [BepInDependency("mtm101.rulerp.baldiplus.leveleditor")]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("alexbw145.baldiplus.bcarnelleditor");
            harmony.PatchAllConditionals();
            LoadingEvents.RegisterOnAssetsLoaded(PostLoad, true);
        }

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

            GameObject sodaMachineCard = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().Find(x => x.name == "SodaMachine")).gameObject;
            sodaMachineCard.AddComponent<ProfitCardVendingMachine>();
            GameObject zestyMachineCard = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().Find(x => x.name == "ZestyMachine")).gameObject;
            zestyMachineCard.AddComponent<ProfitCardVendingMachine>();
            GameObject crazyMachineCard1 = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().Find(x => x.name == "CrazyVendingMachineBSODA")).gameObject;
            crazyMachineCard1.AddComponent<ProfitCardVendingMachine>();
            GameObject crazyMachineCard2 = Instantiate(Resources.FindObjectsOfTypeAll<SodaMachine>().ToList().Find(x => x.name == "CrazyVendingMachineZesty")).gameObject;
            crazyMachineCard2.AddComponent<ProfitCardVendingMachine>();

            DontDestroyOnLoad(sodaMachineCard);
            //sodaMachineCard.SetActive(false);
            DontDestroyOnLoad(zestyMachineCard);
            //zestyMachineCard.SetActive(false);
            DontDestroyOnLoad(crazyMachineCard1);
            //crazyMachineCard1.SetActive(false);
            DontDestroyOnLoad(crazyMachineCard2);
            //crazyMachineCard2.SetActive(false);

            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bsodamachineprofit", sodaMachineCard, Vector3.zero));
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("zestymachineprofit", zestyMachineCard, Vector3.zero));
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("crazymachine_bsodaprofit", crazyMachineCard1, Vector3.zero));
            BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("crazymachine_zestyprofit", crazyMachineCard2, Vector3.zero));
            PlusLevelLoaderPlugin.Instance.prefabAliases.Add("bsodamachineprofit", sodaMachineCard);
            PlusLevelLoaderPlugin.Instance.prefabAliases.Add("zestymachineprofit", zestyMachineCard);
            PlusLevelLoaderPlugin.Instance.prefabAliases.Add("crazymachine_bsodaprofit", crazyMachineCard1);
            PlusLevelLoaderPlugin.Instance.prefabAliases.Add("crazymachine_zestyprofit", crazyMachineCard2);

            BaldiLevelEditorPlugin.itemObjects.Add("profitcard", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value);
            BaldiLevelEditorPlugin.itemObjects.Add("bhammer", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value);
            BaldiLevelEditorPlugin.itemObjects.Add("unsecuredyellowkey", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value);
            BaldiLevelEditorPlugin.itemObjects.Add("securedyellowlock", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock")).value);
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("profitcard", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value);
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("bhammer", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("BHammer")).value);
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("unsecuredyellowkey", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value);
            PlusLevelLoaderPlugin.Instance.itemObjects.Add("securedyellowlock", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("SecuredYellowLock")).value);


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
            __instance.toolCats.Find(x => x.name == "doors").tools.Add(new SwingingDoorTool("inflocked"));
            __instance.toolCats.Find(x => x.name == "objects").tools.AddRange([
                new RotateAndPlacePrefab("bsodamachineprofit"), new RotateAndPlacePrefab("zestymachineprofit"), new RotateAndPlacePrefab("crazymachine_bsodaprofit"), new RotateAndPlacePrefab("crazymachine_zestyprofit")
                ]);
            __instance.toolCats.Find(x => x.name == "characters").tools.AddRange([
                new NpcTool("rpsguy"),
                new NpcTool("errorbot"),
                new NpcTool("siegecanoncart"),
                new NpcTool("mrportalman")
                ]);
            __instance.toolCats.Find(x => x.name == "items").tools.AddRange([
                new ItemTool("profitcard"),
                new ItemTool("bhammer"),
                new ItemTool("unsecuredyellowkey"),
                new ItemTool("securedyellowlock")
                ]);
        }
    }

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
