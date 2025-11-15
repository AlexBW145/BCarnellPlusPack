using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    [RequireComponent(typeof(SodaMachine))]
    public class ToProfitCardCost : EnvironmentObject, IClickable<int>
    {
        internal static ItemObject Quarter;
        private System.Random 
            profitCardRng = FindObjectOfType<LevelBuilder>(false) != null ? FindObjectOfType<LevelBuilder>(false).controlledRNG : new System.Random(),
            freeRng = FindObjectOfType<LevelBuilder>(false) != null ? FindObjectOfType<LevelBuilder>(false).controlledRNG : new System.Random();

        private float profitCardChance = 0.5f, freeChance = 0.05f;
        private SodaMachine vending;

        private static FieldInfo
            _requiredItem = AccessTools.DeclaredField(typeof(SodaMachine), "requiredItem"),
            _usesLeft = AccessTools.DeclaredField(typeof(SodaMachine), "usesLeft"),
            _audUse = AccessTools.DeclaredField(typeof(ITM_Acceptable), "audUse");
        private static MethodInfo _InsertItem = AccessTools.Method(typeof(SodaMachine), "InsertItem");

        public override void LoadingFinished() => Begin();

        public void Begin()
        {
            if (ec != null)
            {
                vending = gameObject.GetComponent<SodaMachine>();
                if (vending != null && BaseGameManager.Instance.levelObject != null)
                {
                    // Conflicts begone if any other currency mod exists...
                    if ((ItemObject)_requiredItem.GetValue(vending) != Quarter)
                        return;
                    if (DateTime.Now.Day == 1 && DateTime.Now.Month == 4)
                    {
                        _requiredItem.SetValue(vending, Quarter);
                        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                        MaterialModifier.ChangeMaterial(meshRender, [.. meshRender.GetSharedMaterialArray(), BasePlugin.freeInsert]);
                        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi"))
                            if (gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")) != null)
                                AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                                .SetValue(gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { Items.Quarter });
                        return;
                    }
                    // Now onto this ez fix!
                    if (profitCardRng.NextDouble() < (double)profitCardChance)
                    {
                        _requiredItem.SetValue(vending, BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
                        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                        MaterialModifier.ChangeMaterial(meshRender, [..meshRender.GetSharedMaterialArray(), BasePlugin.profitCardInsert]);
                        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // Geez, thanks doc...
                            if (gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")) != null)
                                AccessTools.Field(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent"), "requiredItems")
                                .SetValue(gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")), new List<Items> { BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").itemType });
                    }
                    else if (freeRng.NextDouble() < (double)freeChance) // Chances are super low, I hope you don't waste your time finding one...
                    {
                        _requiredItem.SetValue(vending, ItemMetaStorage.Instance.FindByEnum(Items.None).value);
                        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
                        MaterialModifier.ChangeMaterial(meshRender, [..meshRender.GetSharedMaterialArray(), BasePlugin.freeInsert]);
                        if (Chainloader.PluginInfos.ContainsKey("pixelguy.pixelmodding.baldiplus.pixelinternalapi")) // All because of THIS very easy thing??
                            if (gameObject.GetComponent(Chainloader.PluginInfos["pixelguy.pixelmodding.baldiplus.pixelinternalapi"].Instance.GetType().Assembly
                                .GetType("PixelInternalAPI.Components.SodaMachineCustomComponent")) != null)
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
                ItemObject itemObject = (ItemObject)_requiredItem.GetValue(vending);
                if (itemObject.itemType == Items.None && (int)_usesLeft.GetValue(vending) > 0)
                {
                    CoreGameManager.Instance.audMan
                        .PlaySingle((SoundObject)_audUse.GetValue(BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").item.GetComponent<ITM_Acceptable>()));
                    _InsertItem.Invoke(vending, [CoreGameManager.Instance.GetPlayer(player), ec]);
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
            if (vending == null) return true;
            ItemObject item = (ItemObject)_requiredItem.GetValue(vending);
            return item.itemType != Items.None || (int)_usesLeft.GetValue(vending) <= 0;
        }

        public bool ClickableRequiresNormalHeight() => true;
    }

    public class RandomItemMachine : EnvironmentObject, IItemAcceptor // SodaMachine is only for materials, not sprite renderers.
    {
        public static HashSet<WeightedItemObject> ModItems = new HashSet<WeightedItemObject>();
        [SerializeField] internal WeightedItemObject[] potentialItems;
        [SerializeField] internal ItemObject requiredItem;

        public SpriteRenderer render;
        public Sprite outOf;

        private int usesLeft = 3;

        private void Start()
        {
            usesLeft = UnityEngine.Random.RandomRangeInt(3, 10);
            render = gameObject.GetComponentInChildren<SpriteRenderer>();
            potentialItems = potentialItems.AddRangeToArray(ModItems.ToArray());
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
