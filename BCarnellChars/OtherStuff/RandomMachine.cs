using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    [RequireComponent(typeof(SodaMachine))]
    public class ToProfitCardCost : MonoBehaviour, IClickable<int>
    {
        private float profitCardChance = 0.5f;
        private System.Random profitCardRng = FindObjectOfType<LevelBuilder>(false) != null ? FindObjectOfType<LevelBuilder>(false).controlledRNG : new System.Random();

        private float freeChance = 0.05f;
        private System.Random freeRng = FindObjectOfType<LevelBuilder>(false) != null ? FindObjectOfType<LevelBuilder>(false).controlledRNG : new System.Random();
        private SodaMachine vending;

        public void Begin()
        {
            if (BaseGameManager.Instance != null && BaseGameManager.Instance.Ec != null)
            {
                vending = gameObject.GetComponent<SodaMachine>();
                if (vending != null && CoreGameManager.Instance.sceneObject.levelObject != null)
                {
                    // Conflicts begone if any other currency mod exists...
                    if ((ItemObject)vending.ReflectionGetVariable("requiredItem") != Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.Quarter))
                        return;
                    // Now onto this ez fix!
                    if (DateTime.Now.Day == 1 && DateTime.Now.Month == 4)
                    {
                        vending.ReflectionSetVariable("requiredItem", Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.Quarter));
                        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                        MaterialModifier.ChangeMaterial(meshRender, [.. meshRender.GetSharedMaterialArray(), BasePlugin.freeInsert]);
                        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi"))
                            AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                                .SetValue(gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { Items.Quarter });
                        return;
                    }
                    if (profitCardRng.NextDouble() < profitCardChance)
                    {
                        vending.ReflectionSetVariable("requiredItem", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                        MaterialModifier.ChangeMaterial(meshRender, [..meshRender.GetSharedMaterialArray(), BasePlugin.profitCardInsert]);
                        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // Geez, thanks doc...
                            AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                                .SetValue(gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").itemType });
                    }
                    else if (freeRng.NextDouble() < freeChance) // Chances are super low, I hope you don't waste your time finding one...
                    {
                        vending.ReflectionSetVariable("requiredItem", Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.None));
                        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                        MaterialModifier.ChangeMaterial(meshRender, [..meshRender.GetSharedMaterialArray(), BasePlugin.freeInsert]);
                        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // All because of THIS very easy thing??
                            AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                                .SetValue(gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { Items.None });
                    }
                }
            }
        }

        public void Clicked(int player) // FREE!!
        {
            if (vending != null)
            {
                ItemObject itemObject = (ItemObject)vending.ReflectionGetVariable("requiredItem");
                if (itemObject.itemType == Items.None && (int)vending.ReflectionGetVariable("usesLeft") > 0)
                {
                    CoreGameManager.Instance.audMan
                        .PlaySingle((SoundObject)BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").item.GetComponent<ITM_Acceptable>().ReflectionGetVariable("audUse"));
                    vending.ReflectionInvoke("InsertItem", [CoreGameManager.Instance.GetPlayer(player), BaseGameManager.Instance.Ec]);
                }
            }
        }

        public void ClickableSighted(int player)
        {
        }

        public void ClickableUnsighted(int player)
        {
        }

        public bool ClickableHidden()
        {
            ItemObject item = (ItemObject)vending.ReflectionGetVariable("requiredItem");
            return item.itemType != Items.None && (int)vending.ReflectionGetVariable("usesLeft") > 0;
        }

        public bool ClickableRequiresNormalHeight()
        {
            return false;
        }
    }

    public class RandomItemMachine : EnvironmentObject, IItemAcceptor // SodaMachine is only for materials, not sprite renderers.
    {
        public static HashSet<WeightedItemObject> ModItems = new HashSet<WeightedItemObject>();
        [SerializeField] private WeightedItemObject[] potentialItems;
        [SerializeField] private ItemObject requiredItem;

        public SpriteRenderer render;
        public Sprite outOf;

        private int usesLeft = 3;

        private void Start()
        {
            usesLeft = UnityEngine.Random.RandomRangeInt(3, 10);
            render = gameObject.GetComponentInChildren<SpriteRenderer>();
            requiredItem = BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard");
            potentialItems = [
                new WeightedItemObject()
                {
                    selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/BHammer"),
                    weight = 80
                },
                new WeightedItemObject()
                {
                    selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/UnsecuredKey"),
                    weight = 60
                },
                new WeightedItemObject()
                {
                    selection = BasePlugin.bcppAssets.Get<ItemObject>("Items/SecuredLock"),
                    weight = 80
                },

                new WeightedItemObject()
                {
                    selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.NanaPeel), // Ain't no way that ItemMetaStorage.Instance.Get() is useless nowadays...
                    weight = 90
                },
                new WeightedItemObject()
                {
                    selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.Apple),
                    weight = 55
                },
                new WeightedItemObject()
                {
                    selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.PrincipalWhistle),
                    weight = 90
                },
                new WeightedItemObject()
                {
                    selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.PortalPoster),
                    weight = 55
                },
                new WeightedItemObject()
                {
                    selection = Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.Boots),
                    weight = 80
                },
                ..ModItems
            ];
        }

        public void InsertItem(PlayerManager pm, EnvironmentController ec)
        {
            StartCoroutine(Delay(pm));
            usesLeft--;
            if (usesLeft <= 0 && render && outOf)
            {
                render.sprite = outOf;
            }
        }

        public bool ItemFits(Items checkItem)
        {
            if (requiredItem.itemType == checkItem)
            {
                return usesLeft > 0;
            }

            return false;
        }

        private IEnumerator Delay(PlayerManager pm)
        {
            yield return null;
            ItemManager itm = pm.itm;
            WeightedSelection<ItemObject>[] items = potentialItems;
            itm.AddItem(WeightedSelection<ItemObject>.RandomSelection(items));
        }
    }
}
