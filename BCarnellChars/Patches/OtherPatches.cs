using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
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
        static void Prefix(ref float ___height, ref float ___volume, ref float ___riseSpeed)
        {
            if (BaseGameManager.Instance.levelObject.name == "Basement1")
            {
                ___height = 10.1f;
                ___volume = 3f;
                ___riseSpeed = 1.5f;
            }
        }
    }

    [HarmonyPatch(typeof(Activity), "Completed", [typeof(int)])]
    class BasementGet
    {
        static bool Prefix(int player, ref Notebook ___notebook, ref bool ___bonusMode)
        {
            if (!___bonusMode && BasementGameManager.BasementInstance != null)
            {
                ___notebook.Hide(true);
                BaseGameManager.Instance.CollectNotebook(___notebook);
                CoreGameManager.Instance.GetPlayer(player).plm.AddStamina(CoreGameManager.Instance.GetPlayer(player).plm.staminaMax, true);
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Activity), "Completed", [typeof(int),typeof(bool),typeof(Activity)])]
    class BasementCorrect
    {
        static void Postfix(int player, bool correct, Activity activity, ref bool ___bonusMode, Activity __instance)
        {
            if (correct && !___bonusMode)
                foreach (Cell light in __instance.room.ec.activities.Find(x => x.room == __instance.room).room.cells)
                    __instance.room.ec.SetLight(true, light);
        }
    }

    [HarmonyPatch(typeof(TapePlayer), "InsertItem")]
    class StunBot
    {
        static void Postfix(PlayerManager player, EnvironmentController ec, ref float ___time)
        {
            if (ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()))
            {
                ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()).gameObject.GetComponent<PrototypeBot>().StopAllCoroutines();
                ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()).gameObject.GetComponent<PrototypeBot>().StartCoroutine(
                    ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()).gameObject.GetComponent<PrototypeBot>().StunHearing(___time));
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager), "LoadNextLevel")]
    class IfBasement
    {
        static bool Prefix(BaseGameManager __instance, ref int ___defaultLives, ref float ___gradeValue, ref int ___correctProblems,
            ref int ___problems, ref int ___levelNo)
        {
            if (SwitchNextFloorBCPP.enabledSwitch && CoreGameManager.Instance.sceneObject.levelNo == 0 && CoreGameManager.Instance.sceneObject.levelTitle == "F1")
            {
                __instance.ReflectionInvoke("PrepareToLoad", []);
                CoreGameManager.Instance.PrepareForReload();
                CoreGameManager.Instance.SetLives(___defaultLives);
                CoreGameManager.Instance.tripPlayed = false;
                if (___problems > 0)
                {
                    CoreGameManager.Instance.GradeVal += -Mathf.RoundToInt(___gradeValue * (___correctProblems / (float)___problems * 2f - 1f));
                }

                if (CoreGameManager.Instance.currentMode == Mode.Main)
                {
                    foreach (NPC item in __instance.Ec.npcsToSpawn)
                    {
                        PlayerFileManager.Instance.Find(PlayerFileManager.Instance.foundChars, (int)item.Character);
                    }

                    foreach (Obstacle obstacle in __instance.Ec.obstacles)
                    {
                        PlayerFileManager.Instance.Find(PlayerFileManager.Instance.foundObstcls, (int)obstacle);
                    }

                    PlayerFileManager.Instance.Find(PlayerFileManager.Instance.clearedLevels, ___levelNo);
                }

                SubtitleManager.Instance.DestroyAll();
                CoreGameManager.Instance.sceneObject = BasePlugin.Instance.sBasement;
                SceneManager.LoadSceneAsync("Game");
                return false;
            }
            return true;
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
                gl.Initialize(1);
                gl.SetMode((int)Mode.Main);
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
