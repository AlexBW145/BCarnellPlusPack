using BCarnellChars.Characters;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BCarnellChars.Patches
{
    [HarmonyPatch(typeof(FloodEvent), "Initialize")]
    class BasementDirtyWaterFill
    {
        static void Prefix(ref float ___height, ref float ___volume)
        {
            if (BaseGameManager.Instance.levelObject.name == "Basement1")
            {
                ___height = 10.1f;
                ___volume = 3f;
            }
        }
    }

#if DEBUG
    [HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
    class EveryoneIsHere
    {
        static void Prefix(LevelGenerator __instance)
        {
            __instance.ld.additionalNPCs = __instance.ld.potentialNPCs.Count;
            /*__instance.ld.minSize = new IntVector2(99, 99);
            __instance.ld.maxSize = new IntVector2(99, 99);
            __instance.ld.minSpecialRooms = 9;
            __instance.ld.maxSpecialRooms = 9;*/
        }
    }

    [HarmonyPatch(typeof(MainMenu), "Start")]
    class TestingBasement
    {
        static void Prefix(MainMenu __instance)
        {
            var test = __instance.transform.Find("StartTest").GetComponent<StandardMenuButton>();
            test.gameObject.SetActive(true);
            test.OnPress = new UnityEvent();
            test.OnPress.AddListener(() =>
            {
                GameLoader gl = Resources.FindObjectsOfTypeAll<GameLoader>().First();
                gl.gameObject.SetActive(true);
                __instance.gameObject.SetActive(false);
                gl.CheckSeed();
                gl.Initialize(1);                gl.SetMode((int)Mode.Free);
                ElevatorScreen evl = SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name == "ElevatorScreen").First().GetComponent<ElevatorScreen>();
                gl.AssignElevatorScreen(evl);
                evl.gameObject.SetActive(true);
                //gl.LoadLevel(Resources.FindObjectsOfTypeAll<SceneObject>().ToList().Find(x => x.name == "MainLevel_3"));
                gl.LoadLevel(BasePlugin.Instance.sBasement);
                evl.Initialize();
                gl.SetSave(false);
            });
        }
    }
#endif
}
