using HarmonyLib;
using MonoMod.Utils;
using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusLevelStudio.UI;
using PlusStudioLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars;

internal static class LevelStudioAdds
{
    private static void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 1f, params string[] path)
    {
        string[] files = Directory.GetFiles(Path.Combine(path));
        for (int i = 0; i < files.Length; i++)
        {
            BCPPPlugin.bcppAssets.Add(prefix + Path.GetFileNameWithoutExtension(files[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(files[i]), pixelsPerUnit));
        }
    }

    internal static void EditorAdds()
    {
        AddSpriteFolderToAssetMan("EditorUI/", 40f, AssetLoader.GetModPath(BCPPPlugin.Instance), "EditorUI");
        AssetLoader.LocalizationFromFunction((lang) => new Dictionary<string, string>()
                {
                    { "Ed_Tool_item_profitcard_Desc", "An alternative quarter that's only used for alternative quarter inputs." },
                    { "Ed_Tool_item_bhammer_Desc", "Not safe for windows (and rock guys)" },
                    { "Ed_Tool_item_unsecuredyellowkey_Desc", "Don't lose it!" },
                    { "Ed_Tool_item_securedyellowlock_Desc", "Don't place it!" },
                    { "Ed_Tool_item_anyportaloutput_Desc", "Can only be placed if Mr. Portal Man is present, but it's usable for planned escape routes!" },
                    { "Ed_Tool_item_taserpulse_Desc", "ZAP! This bad boy chains through three bad criminals! But can lethally get you into trouble!" },
                    { "Ed_Tool_item_stickerpack_securedlocked_Title", "Secured Locked Sticker Packet" },
                    { "Ed_Tool_item_stickerpack_securedlocked_Desc", "Contains 1 BCPP-related sticker and 2 randomly selected stickers. This packet is not yet opened, you must search for a Unsecured Key in order to open!" },

                    { "Ed_Tool_door_inflocked_Title", "Secured Locked Swinging Door" },
                    { "Ed_Tool_door_inflocked_Desc", "It's forever to be locked..." },
                    { "Ed_Tool_object_bcpp_randomitemmachine_Title", "Mystery Profit Item Machine"},
                    { "Ed_Tool_object_bcpp_randomitemmachine_Desc", "<i>What will it give you?</i>"},
                    { "Ed_Tool_object_profitmachine_Title", " (Profit Card)"},
                    { "Ed_Tool_object_profitmachine_Desc", "It now accepts an alternative quarter..."},

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
        var studioslot = AssetLoader.SpriteFromMod(BCPPPlugin.Instance, Vector2.one / 2f, 1f, "Texture2D", "UI", "StudioSlotBCPP.png");
        BCPPPlugin.bcppAssets.Add("StudioSlot", studioslot);
        NPC 
            rpsguy = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/RPS Guy"),
            errorbot = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/ERROR-BOT_ITEM-STEALER"),
            siegecannoncart = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Siege Cannon Cart"),
            mrportalman = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Mr Portal Man"),
            mrrobber = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Mr Robber");
        EditorInterface.AddNPCVisual("rpsguy", rpsguy);
        EditorInterface.AddNPCVisual("errorbot", errorbot);
        EditorInterface.AddNPCVisual("siegecanoncart", siegecannoncart);
        EditorInterface.AddNPCVisual("mrportalman", mrportalman);
        EditorInterface.AddNPCVisual("mrrobber", mrrobber);

        EditorInterface.AddObjectVisual("bcpp_randomitemmachine", BCPPPlugin.bcppAssets.Get<GameObject>("Structures/RandomItemMachine"), true);
        var infdoorlocked = BCPPPlugin.securedYellowSwingingDoor;
        EditorInterface.AddDoor<DoorDisplay>("inflocked", DoorIngameStatus.AlwaysObject, Resources.FindObjectsOfTypeAll<SwingDoor>().Last().mask[0], [infdoorlocked, infdoorlocked]);

#if DEBUG
        var basicobject = EditorInterface.AddObjectVisual("b2platform_sideways", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Sideways"), true);
        foreach (var collid in basicobject.ingameColliders)
            GameObject.DestroyImmediate(collid);
        basicobject.ingameColliders.Clear();
        basicobject = EditorInterface.AddObjectVisual("b2platform_corner", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Corner"), true);
        foreach (var collid in basicobject.ingameColliders)
            GameObject.DestroyImmediate(collid);
        basicobject.ingameColliders.Clear();
        basicobject = EditorInterface.AddObjectVisual("b2platform_oneside", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/OneSide"), true);
        foreach (var collid in basicobject.ingameColliders)
            GameObject.DestroyImmediate(collid);
        basicobject.ingameColliders.Clear();
        basicobject = EditorInterface.AddObjectVisual("b2platform_open", BasePlugin.bcppAssets.Get<GameObject>("B2Platform/Open"), true);
        foreach (var collid in basicobject.ingameColliders)
            GameObject.DestroyImmediate(collid);
        basicobject.ingameColliders.Clear();
        Sprite man = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name == "BaldiChalkSheet_0");
        EditorInterfaceModes.AddModeCallback((mode, isComplaint) =>
        {
            EditorInterfaceModes.AddToolsToCategory(mode, "objects",
                [
                new ObjectTool("b2platform_sideways", man),
                new ObjectTool("b2platform_corner", man),
                new ObjectTool("b2platform_oneside".ToLower(), man),
                new ObjectTool("b2platform_open".ToLower(), man)
                ]);
        });
#endif
        LevelStudioPlugin.Instance.skyboxSprites.Add("pac75", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/pac75skybox"));
        LevelStudioPlugin.Instance.selectableSkyboxes.Add("pac75");
        LevelStudioPlugin.Instance.selectableStickers.AddRange(new List<string>()
        {
            "switcherquartprofit",
            "rock_paper_scissors",
            "errorbot",
            "anyportalman",
        });
        LevelStudioPlugin.Instance.stickerSprites.AddRange(new()
        {
            { "switcherquartprofit", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/Sticker_switcherooprofit") },
            { "rock_paper_scissors", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/Sticker_rps") },
            { "errorbot", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/Sticker_errorbot") },
            { "anyportalman", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/Sticker_anyportalman") }
        });

        EditorInterfaceModes.AddModeCallback((mode, isComplaint) =>
        {
            EditorInterfaceModes.AddToolsToCategory(mode, "npcs", [
                new NPCToolBCPP("rpsguy", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_rpsguy"), "Ed_BCPP_RPSGuy_Title", "Ed_BCPP_RPSGuy_Desc"),
                new NPCToolBCPP("errorbot", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_errorbot"), "Ed_BCPP_ERRORBOT_Title", "Ed_BCPP_ERRORBOT_Desc"),
                new NPCToolBCPP("siegecanoncart", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_siegecanoncart"), "Ed_BCPP_SiegeCanonCart_Title", "Ed_BCPP_SiegeCanonCart_Desc"),
                new NPCToolBCPP("mrportalman", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_mrportalman"), "Ed_BCPP_MrPortalMan_Title", "Ed_BCPP_MrPortalMan_Desc"),
                new NPCToolBCPP("mrrobber", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/NPC_mrrobber"), "Ed_BCPP_MrRobber_Title", "Ed_BCPP_MrRobber_Desc"),
                ]);
            EditorInterfaceModes.AddToolsToCategory(mode, "items", [
                new ItemTool("profitcard") { frameOverride = studioslot },
                new ItemTool("bhammer") { frameOverride = studioslot },
                new ItemTool("unsecuredyellowkey") { frameOverride = studioslot },
                new ItemTool("securedyellowlock") { frameOverride = studioslot },
                new ItemTool("anyportaloutput") { frameOverride = studioslot },
                new ItemTool("taserpulse") { frameOverride = studioslot },
                new ItemTool("stickerpack_securedlocked", false) { frameOverride = studioslot }
                ]);
            EditorInterfaceModes.AddToolToCategory(mode, "doors", new DoorTool("inflocked", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/inflocked_SwingDoorED")) { frameOverride = studioslot });
            EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectToolNoRotation("bcpp_randomitemmachine", BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/Object_bcpp_randomitemmachine")) { frameOverride = studioslot });
        });
    }

    internal static void ProfitIsEditorTime()
    {
        foreach (var soda in LevelLoaderAdds.profitconverted)
            EditorInterface.AddObjectVisualWithMeshCollider(soda + "_profit", LevelLoaderPlugin.Instance.basicObjects[soda + "_profit"], true);
        EditorInterfaceModes.AddModeCallback((mode, isComplaint) =>
        {
            if (mode.id != "rooms")
                foreach (var soda in LevelLoaderAdds.profitconverted)
                {
                    var ogtool = mode.availableTools["objects"].Find(x => x is ObjectTool && ((ObjectTool)x).type == soda);
                    if (ogtool == null) continue;
                    EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectToolProfitCard(soda + "_profit", ogtool.sprite)
                    {
                        frameOverride = ogtool.frameOverride
                    });
                }
        });
    }
}

public class NPCToolBCPP : NPCTool
{
    private string _titleKey;
    private string _descKey;
    public NPCToolBCPP(string npc, Sprite sprite, string titleKey, string descKey) : base(npc, sprite)
    {
        _titleKey = titleKey;
        _descKey = descKey;
        frameOverride = BCPPPlugin.bcppAssets.Get<Sprite>("StudioSlot");
    }

    public override string titleKey => _titleKey;
    public override string descKey => _descKey;
}

public class ObjectToolProfitCard(string type, Sprite sprite) : ObjectTool(type, sprite)
{
    public override string titleKey => LocalizationManager.Instance.GetLocalizedText(base.titleKey.Replace("_profit", "")) + LocalizationManager.Instance.GetLocalizedText("Ed_Tool_object_profitmachine_Title");
    public override string descKey => "Ed_Tool_object_profitmachine_Desc";
}

[HarmonyPatch(typeof(HotSlotScript))]
class ProfitIcon // Hacky stuff before frame overwrites were a thing.
{
    [HarmonyPatch("Start"), HarmonyPostfix]
    static void InsertNewww(HotSlotScript __instance) => __instance.StartCoroutine(waitforMe(__instance));
    [HarmonyPatch(nameof(HotSlotScript.currentTool), MethodType.Setter), HarmonyPostfix]
    static void IsProfit(HotSlotScript __instance)
    {
        if (__instance.currentTool != null)
            __instance.iconImage?.transform.parent.Find("ProfitCardIcon")?.gameObject.SetActive(__instance.currentTool is ObjectToolProfitCard);
        else
            __instance.iconImage?.transform.parent.Find("ProfitCardIcon")?.gameObject.SetActive(false);
    }

    private static IEnumerator waitforMe(HotSlotScript __instance)
    {
        while (__instance.iconImage == null)
            yield return null;
        Image icon = GameObject.Instantiate(__instance.iconImage, __instance.iconImage.transform.parent, true);
        icon.gameObject.name = "ProfitCardIcon";
        icon.sprite = BCPPPlugin.bcppAssets.Get<Sprite>("EditorUI/Icon_Profit");
        icon.color = Color.white;
        icon.raycastTarget = false;
        icon.gameObject.SetActive(false);
        yield break;
    }
}