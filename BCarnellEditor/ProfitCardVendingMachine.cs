using HarmonyLib;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using MTM101BaldAPI.Reflection;
using BCarnellChars;

namespace BCarnellEditor
{
    public class ProfitCardVendingMachine : MonoBehaviour
    {
        private SodaMachine sodaMachine;

        private void Start()
        {
            sodaMachine = gameObject.GetComponent<SodaMachine>();
            if (!sodaMachine)
                return;
            sodaMachine.ReflectionSetVariable("requiredItem", ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("ProfitCard")).value);
            var meshRender = sodaMachine.ReflectionGetVariable("meshRenderer") as MeshRenderer;
            meshRender.materials = meshRender.materials.AddToArray(BasePlugin.profitCardInsert);
        }
    }
}
