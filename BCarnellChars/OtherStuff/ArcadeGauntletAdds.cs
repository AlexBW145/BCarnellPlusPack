using BaldiPlusArcade;
using BaldiPlusArcade.LevelElements;
using BCarnellChars.Patches;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.OtherStuff;

internal static class ArcadeGauntletAdds
{
    internal static void EternityAdds()
    {
        NPC
            rpsguy = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/RPS Guy"),
            errorbot = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/ERROR-BOT_ITEM-STEALER"),
            siegecannoncart = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Siege Cannon Cart"),
            mrportalman = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Mr Portal Man"),
            mrrobber = BCPPPlugin.bcppAssets.Get<NPC>("NPCs/Mr Robber");
        BaldiArcadePlugin.Instance.npcs.AddRange([
            new()
            {
                selection = rpsguy,
                weight = 100
            },
            new()
            {
                selection = errorbot,
                weight = 50
            },
            new()
            {
                selection = siegecannoncart,
                weight = 90
            },
            new()
            {
                selection = mrportalman,
                weight = 90
            },
            new()
            {
                selection = mrrobber,
                weight = 10
            }
            ]);
        BaldiArcadePlugin.Instance.AddElement(new AddCharacterElement("rpsguy", rpsguy, 11, 2000, false), 6);
        BaldiArcadePlugin.Instance.AddElement(new AddCharacterElement("errorbot", errorbot, 9, 3000, true), 10);
        BaldiArcadePlugin.Instance.AddElement(new AddCharacterElement("mrrobber", mrrobber, 10, 4500, false), 6);
        BaldiArcadePlugin.Instance.AddElement(new AddCharacterElement("mrportalman", mrportalman, 9, 4000, true), 10);
        ItemObject
            profitCard = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"),
            unsafeHammer = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/BHammer"),
            unsecuredKey = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
            unsecuredLock = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"),
            anyportal = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/AnyportalOutput"),
            taserPulse = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/TASERPulse");
        BaldiArcadePlugin.Instance.AddElement(new ItemSetLevelElement("bcpp_ilikemoney", [
            new()
            {
                selection = ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value,
                weight = 150
            },
            new()
            {
                selection = profitCard,
                weight = 150
            },
            ], -255), 25);
        BaldiArcadePlugin.Instance.AddElement(new ItemSetLevelElement("bcpp_nsfw", [
            new()
            {
                selection = unsafeHammer,
                weight = 100
            },
            new()
            {
                selection = unsecuredKey,
                weight = 50
            },
            new()
            {
                selection = unsecuredLock,
                weight = 100
            },
            new()
            {
                selection = ItemMetaStorage.Instance.FindByEnum(Items.Scissors).value,
                weight = 25
            },
            new()
            {
                selection = ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
                weight = 25
            }
            ], -300), 15);
        BaldiArcadePlugin.Instance.AddElement(new ItemSetLevelElement("bcpp_crime", [
            new()
            {
                selection = taserPulse,
                weight = 100
            },
            new()
            {
                selection = unsafeHammer,
                weight = 35
            },
            ], -50), 35);
        BaldiArcadePlugin.Instance.AddElement(new ItemSetLevelElement("bcpp_teleporting", [
            new()
            {
                selection = ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value,
                weight = 25
            },
            new()
            {
                selection = anyportal,
                weight = 75
            }
            ], -75), 30);
        BaldiArcadePlugin.Instance.shopItems.AddRange([
            new()
            {
                selection = profitCard,
                weight = 100
            },
            new()
            {
                selection = unsafeHammer,
                weight = 15
            },
            new()
            {
                selection = unsecuredKey,
                weight = 30
            },
            new()
            {
                selection = unsecuredLock,
                weight = 30,
            },
            new()
            {
                selection = anyportal,
                weight = 35
            },
            new()
            {
                selection = taserPulse,
                weight = 25
            }
            ]);
        BaldiArcadePlugin.Instance.stickers.AddRange(
            [
            new(StickerPatches.rpsSticker, 30),
            new(StickerPatches.stealerSticker, 80),
            new(StickerPatches.switcherooSticker, 80),
            new(StickerPatches.anyportalSticker, 55)
            ]);
        StructureBuilder structureBuilder = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<StructureBuilder>().Last(x => x.name == "SwingingDoorConstructor" && x.GetInstanceID() >= 0), MTM101BaldiDevAPI.prefabTransform);
        structureBuilder.name = "SecuredLockedSwingingDoorConstructor";
        BCPPPlugin.bcppAssets.Add(structureBuilder.name, structureBuilder);
        BaldiArcadePlugin.Instance.AddElement(new AddSecuredLockedDoorElement(), 75);
        BaldiArcadePlugin.Instance.AddElement(new AddATonOfSecuredLockedDoors(), 5);
    }
}

public class AddSecuredLockedDoorElement : AddDoorElement
{
    public override string id => "bcpp_inflockedswingdoor";

    protected override int cost => 5000;

    protected override StructureBuilder GetBuilder() => BCPPPlugin.bcppAssets.Get<StructureBuilder>("SecuredLockedSwingingDoorConstructor");

    protected override GameObject GetPrefab() => BCPPPlugin.bcppAssets.Get<SecuredSwingingDoor>("Obstacles/SecuredSwingingDoor").gameObject;

    public override bool CurrentlyValid(EndlessLevelRepresentation rep, int actingFloor) => rep.levelObject.minSize.x >= 75 && rep.levelObject.minSize.z >= 75;
}

public class AddATonOfSecuredLockedDoors : AddSecuredLockedDoorElement
{
    public override ElementType type => base.type | ElementType.Wacky | ElementType.Boss;

    public override bool canStack => false;

    public override string id => "bcpp_tonofinflockeddoors";

    protected override int cost => base.cost / count;

    public AddATonOfSecuredLockedDoors()
    {
        count = 100;
    }

    public override int Apply(EndlessLevelRepresentation rep, int actingFloor)
    {
        int result = base.Apply(rep, actingFloor);
        var itemKey = BCPPPlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey");
        for (int i = 0; i < count / 12; i++)
            rep.levelObject.forcedItems.Add(itemKey);

        return result;
    }
}