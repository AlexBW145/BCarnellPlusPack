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
        private System.Random profitCardRng = new System.Random();

        private float freeChance = 0.05f;
        private System.Random freeRng = new System.Random();

        private void Start()
        {
            if (BaseGameManager.Instance != null && BaseGameManager.Instance.Ec != null)
            {
                SodaMachine component = gameObject.GetComponent<SodaMachine>();
                if (component != null && CoreGameManager.Instance.sceneObject.levelObject != null)
                {
                    // Conflicts begone if any other currency mod exists...
                    if ((ItemObject)component.ReflectionGetVariable("requiredItem") != Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.Quarter))
                        return;
                    // Now onto this ez fix!
                    if (profitCardRng.NextDouble() < profitCardChance)
                    {
                        component.ReflectionSetVariable("requiredItem", BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard"));
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
                        component.ReflectionSetVariable("requiredItem", Resources.FindObjectsOfTypeAll<ItemObject>().ToList().Find(x => x.itemType == Items.None));
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
            SodaMachine component = gameObject.GetComponent<SodaMachine>();
            if (component != null)
            {
                ItemObject itemObject = (ItemObject)component.ReflectionGetVariable("requiredItem");
                if (itemObject.itemType == Items.None && (int)component.ReflectionGetVariable("usesLeft") > 0)
                {
                    CoreGameManager.Instance.audMan
                        .PlaySingle((SoundObject)BasePlugin.bcppAssets.Get<ItemObject>("Items/ProfitCard").item.GetComponent<ITM_Acceptable>().ReflectionGetVariable("audUse"));
                    component.ReflectionInvoke("InsertItem", [CoreGameManager.Instance.GetPlayer(player), BaseGameManager.Instance.Ec]);
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
            return true;
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
