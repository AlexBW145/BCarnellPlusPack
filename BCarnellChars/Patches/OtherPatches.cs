using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BCarnellChars.Patches
{
    [HarmonyPatch]
    class B1Patches
    {
        [HarmonyPatch(typeof(FloodEvent), nameof(FloodEvent.Initialize)), HarmonyPostfix]
        static void BasementDirtyWaterFill(ref float ___height, ref float ___riseSpeed)
        {
            if (CoreGameManager.Instance.sceneObject == BCPPPlugin.Instance.sBasement
                || CoreGameManager.Instance.sceneObject == BCPPPlugin.Instance.sBoss)
            {
                ___height = 10.1f;
                ___riseSpeed = 1.5f;
            }
        }

        [HarmonyPatch(typeof(Activity), "Completed", [typeof(int)]), HarmonyPrefix]
        static bool BasementGet(int player, ref Notebook ___notebook, Activity __instance, ref bool ___bonusMode, ref bool ___completed)
        {
            if (__instance is not NoActivity && !___bonusMode && BasementGameManager.BasementInstance != null)
            {
                ___completed = true;
                ___notebook.Hide(true);
                BaseGameManager.Instance.CollectNotebook(___notebook);
                CoreGameManager.Instance.GetPlayer(player).plm.AddStamina(CoreGameManager.Instance.GetPlayer(player).plm.staminaMax, true);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Activity), nameof(Activity.Completed), [typeof(int), typeof(bool)]), HarmonyPostfix]
        static void BasementCorrect(int player, bool correct, ref bool ___bonusMode, Activity __instance)
        {
            if (correct && !___bonusMode && BasementGameManager.BasementInstance != null)
                foreach (Cell light in __instance.room.cells)
                    light.SetPower(true);
        }

        [HarmonyPatch(typeof(TapePlayer), "InsertItem"), HarmonyPostfix]
        static void Postfix(PlayerManager player, EnvironmentController ec, ref float ___time)
        {
            if (ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()))
            {
                var proto = ec.Npcs.Find(x => x.GetComponent<PrototypeBot>()).gameObject.GetComponent<PrototypeBot>();
                proto.StopAllCoroutines();
                proto.StartCoroutine(
                    proto.StunHearing(___time));
            }
        }

        private static MethodInfo
            _PrepareToLoad = AccessTools.Method(typeof(BaseGameManager), "PrepareToLoad"),
            _LoadSceneObject = AccessTools.Method(typeof(BaseGameManager), "LoadSceneObject", [typeof(SceneObject)]);
        [HarmonyPatch(typeof(BaseGameManager), nameof(BaseGameManager.LoadNextLevel)), HarmonyPrefix]
        static bool IfBasement(BaseGameManager __instance, ref int ___defaultLives, ref float ___gradeValue, ref int ___correctProblems,
            ref int ___problems, ref int ___levelNo)
        {
            if (SwitchNextFloorBCPP.enabledSwitch && CoreGameManager.Instance.sceneObject.levelNo == 0 && CoreGameManager.Instance.sceneObject.levelTitle == "F1")
            {
                __instance.StopAllCoroutines();
                __instance.Ec.gameObject.SetActive(false);
                _PrepareToLoad.Invoke(__instance, []);
                CoreGameManager.Instance.PrepareForReload();
                if (CoreGameManager.Instance.sceneObject.levelNo > CoreGameManager.Instance.lastLevelNumber)
                    CoreGameManager.Instance.tripPlayed = false;

                if (CoreGameManager.Instance.currentMode == Mode.Main)
                {
                    foreach (NPC item in __instance.Ec.npcsToSpawn)
                        PlayerFileManager.Instance.Find(PlayerFileManager.Instance.foundChars, (int)item.Character);

                    foreach (Obstacle obstacle in __instance.Ec.obstacles)
                        PlayerFileManager.Instance.Find(PlayerFileManager.Instance.foundObstcls, (int)obstacle);

                    PlayerFileManager.Instance.Find(PlayerFileManager.Instance.clearedLevels, ___levelNo);
                }

                SubtitleManager.Instance.DestroyAll();
                _LoadSceneObject.Invoke(__instance, [BCPPPlugin.Instance.sBasement]);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    class MiscPatches
    {
        [HarmonyPatch(typeof(Credits), "Start"), HarmonyPrefix]
        static void CreditsBCPP(Credits __instance)
        {
            IEnumerator WaitUntil() // Objects kept on getting destroyed because they are created upon scene loading, not scene loaded.
            {
                while (SceneManager.GetActiveScene().name != "Credits")
                    yield return null;

                var credits = GameObject.Instantiate(__instance.screens[0]);
                credits.gameObject.name = "BCPP Credits Main";
                credits.GetComponentsInChildren<TextMeshProUGUI>()[0].text = @"<color=red>B. Carnell's</color> <color=yellow>Plus Pack</color>

<size=85%>Mod by AlexBW145
RPS Guy's voice as of BCPP v1.1.2 by M.S.F on Gamebanana
Music from BCPP v1.5 major content is from Innyume (Video Game)
PAC-MAN is owned by and all rights reserved to Bandai Namco Entertainment</size>";
                var credits2 = GameObject.Instantiate(__instance.screens[0]);
                credits2.gameObject.name = "BCPP Credits Tools";
                credits2.GetComponentsInChildren<TextMeshProUGUI>()[0].text = @"Tools Used for <color=red>BC</color><color=yellow>PP</color>

paint.NET, Anim8or, Audacity, Firealpaca, GIMP, Photopea, TTSMP3 (prior to v1.0.0), Visual Studio 2022, Flipnote Studio, & Flipnote Studio 3D.";

                var credits3 = GameObject.Instantiate(__instance.screens[0]);
                credits3.gameObject.name = "BCPP Credits Samples";
                credits3.GetComponentsInChildren<TextMeshProUGUI>()[0].text = @"Audio Samples used for <color=red>BC</color><color=yellow>PP</color>

<size=85%>Pixabay (in most cases), ZERO-G - DATAFILE DISC ONE, Hollywood Edge - Cartoon Trax Sound Effects, 20th Century Fox Sound Effects Library, Scott Cawthon, Xfer Records - Serum (used for FX), the original PAC-MAN, PAC 365, SEGA, Lucasfilm Sound Effects Library, & unknown sources (part of Anyportal Output's sfx is taken from Video Game - The Blockheads).</size>";

                GameObject.Destroy(credits.GetComponentsInChildren<TextMeshProUGUI>()[1]);
                GameObject.Destroy(credits2.GetComponentsInChildren<TextMeshProUGUI>()[1]);
                credits3.GetComponentsInChildren<TextMeshProUGUI>()[1].text = "AlexBW145 is not affilated with these following groups and individuals or actually owns these samples, all rights reserved to Pixabay, ZERO-G, Hollywood Edge, 20th Century Fox, Xfer Records, Bandai Namco, SEGA, and Majic Jungle Software.";
                credits.gameObject.SetActive(false);
                credits2.gameObject.SetActive(false);
                credits3.gameObject.SetActive(false);
                __instance.screens.InsertRange(7, [credits, credits2, credits3]);
                yield break;
            }
            __instance.StartCoroutine(WaitUntil());
        }
#if false
        [HarmonyPatch(typeof(Credits), "Update"), HarmonyPrefix]
        static bool Tester(Credits __instance, ref int ___i)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                ___i++;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                ___i--;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (___i < __instance.screens.Count)
                {
                    GlobalCam.Instance.Transition(UiTransition.SwipeRight, 0.2f);
                    __instance.screens[___i - 1].gameObject.SetActive(value: false);
                    __instance.screens[___i].gameObject.SetActive(value: true);
                    __instance.screens[___i].gameObject.SetActive(value: false);
                    __instance.screens[___i].gameObject.SetActive(value: true);
                }
            }
            return false;
        }
#endif
        [HarmonyPatch(typeof(PitstopGameManager), nameof(PitstopGameManager.Initialize)), HarmonyPostfix]
        static void NegativesRemoveSticker()
        {
            if (CoreGameManager.Instance.nextLevel != null && CoreGameManager.Instance.nextLevel.GetMeta()?.tags.Contains("b-route") == true && CoreGameManager.Instance.nextLevel.levelNo < CoreGameManager.Instance.lastLevelNumber)
                StickerManager.Instance.ClearAppliedStickers();
        }

        internal static StickerPackType bcppStickerPack, securedPacket;
        [HarmonyPatch(typeof(StickerManager), nameof(StickerManager.GiveRandomStickers)), HarmonyPostfix]
        static void GetBCPPStickers(WeightedSticker[] potentialStickers, StickerPackType packType, int amount, bool openNow, StickerManager __instance, ref List<WeightedSticker> ____potentialStickersToAdd)
        {
            if (packType == bcppStickerPack)
                __instance.GiveBCPPRandomStickers(____potentialStickersToAdd, potentialStickers, amount, openNow, false);
            else if (packType == securedPacket)
                __instance.GiveSecuredPacket();
        }

        internal static ItemObject _securedPacket;
        [HarmonyPatch(typeof(StoreRoomFunction), "Restock"), HarmonyPostfix]
        static void ChanceOfBonus(StoreRoomFunction __instance, ref Pickup[] ___stickerPickup, ref PriceTag[] ___stickerTag)
        {
            System.Random rng = new System.Random();
            var bonusPacket = ItemMetaStorage.Instance.FindByEnum(Items.StickerPack).itemObjects.First(x => x.name == "StickerPack_Bonus");
            for (int j = 0; j < ___stickerPickup.Length; j++)
            {
                if (___stickerPickup[j].gameObject.activeSelf && ___stickerPickup[j].item == bonusPacket
                    && rng.NextDouble() < (double)0.35f)
                {
                    if (___stickerPickup[j].GetComponent<Animator>() != null)
                        ___stickerPickup[j].GetComponent<Animator>().enabled = false;
                    ___stickerPickup[j].AssignItem(_securedPacket);
                    ___stickerPickup[j].price = ___stickerPickup[j].item.price;
                    ___stickerTag[j].SetText(___stickerPickup[j].price.ToString());
                    ___stickerPickup[j].gameObject.SetActive(true);
                    ___stickerPickup[j].showDescription = true;
                }
                else if (___stickerPickup[j].item == bonusPacket && ___stickerPickup[j].GetComponent<Animator>() != null)
                    ___stickerPickup[j].GetComponent<Animator>().enabled = true;
            }
        }
    }

    [ConditionalPatchNever, ConditionalPatchNoMod("pixelguy.pixelmodding.baldiplus.moddedmodesapi"), HarmonyPatch(typeof(MainMenu), "Start")]
    class DoneForDay
    {
        static void Postfix()
        {
            if (BCPPSave.Instance.basementCompleted)
            {
                RectTransform canv = RectTransform.FindObjectsOfType<RectTransform>(true).ToList().Find(x => x.name == "PickChallenge");
                StandardMenuButton but = GameObject.Instantiate(canv.gameObject.GetComponentInChildren<StandardMenuButton>().gameObject,
                    canv, true).GetComponent<StandardMenuButton>();
                but.name = "BasementFloor";
                but.transform.SetSiblingIndex(canv.gameObject.GetComponentInChildren<StandardMenuButton>().transform.GetSiblingIndex());
                but.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f) / 2f;
                but.GetComponent<RectTransform>().anchoredPosition = new Vector2(182f, 164f);
                but.GetComponent<TextLocalizer>().key = "But_Basement01";
                but.OnHighlight = new UnityEvent();
                but.OnHighlight.AddListener(() =>
                {
                    but.transform.parent.Find("ModeText").GetComponent<TextLocalizer>().GetLocalizedText("Men_Basement01");
                });
                but.OnPress = new UnityEvent();
                but.OnPress.AddListener(() =>
                {
                    GameLoader gl = Resources.FindObjectsOfTypeAll<GameLoader>().First();
                    gl.gameObject.SetActive(true);
                    but.transform.parent.gameObject.SetActive(false);
                    gl.CheckSeed();
                    gl.Initialize(int.MaxValue);
                    gl.SetMode((int)Mode.Main);
                    ElevatorScreen evl = SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name == "ElevatorScreen").First().GetComponent<ElevatorScreen>();
                    gl.AssignElevatorScreen(evl);
                    evl.gameObject.SetActive(true);
                    gl.LoadLevel(BCPPPlugin.Instance.sBasement);
                    evl.Initialize();
                    gl.SetSave(false);
                });
            }
        }
    }

    [HarmonyPatch]
    class PAC75Patches
    {
        [HarmonyPatch(typeof(GameInitializer), "WaitForGenerator"), HarmonyPostfix]
        static IEnumerator BugfixPacInitialize(IEnumerator result, LevelBuilder lb, BaseGameManager gm)
        {
            if (gm is PacGameManager)
            {
                while (lb.levelInProgress && !lb.levelCreated)
                    yield return null;
                Debug.Log("PAC MAN");
                gm.Initialize();
                yield break;
            }
            while (result.MoveNext())
                yield return result.Current;
        }
        [HarmonyPatch(typeof(Elevator), nameof(Elevator.Initialize)), HarmonyPostfix]
        static void NoExits(EnvironmentController ec, IntVector2 position, Elevator __instance, ref AudioManager ___audMan)
        {
            if (CoreGameManager.Instance.sceneObject != BCPPPlugin.Instance.sPac) return;
            __instance.SetState(ElevatorState.NoPower);
            ___audMan.FlushQueue(true);
            foreach (var gatewall in __instance.transform.Find("Gate").GetComponentsInChildren<MeshRenderer>())
            {
                MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                gatewall.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetTexture("_MainTex", ec.mainHall.wallTex);
                gatewall.SetPropertyBlock(materialPropertyBlock);
            }
            // Insert some attempts trying to make it disappear and make the other walls fill its mesh.
        }
    }
#if !EARLYBIRD
    [HarmonyPatch]
    class B2Patches
    {
        [HarmonyPatch(typeof(CoreGameManager), nameof(CoreGameManager.BackupMap)), HarmonyPrefix]
        static bool StopBackingUpMaps() => !(CarnellBossManager.BossInstance != null || CoreGameManager.Instance.sceneObject == BCPPPlugin.Instance.sBoss);
        [HarmonyPatch(typeof(ExtendedStickerData), nameof(ExtendedStickerData.CanBeApplied)), HarmonyPostfix]
        static void AffectGeneratorRegardless(ref bool __result, ExtendedStickerData __instance)
        {
            if (__instance.affectsLevelGeneration && !__result &&
                (CarnellBossManager.BossInstance != null || CoreGameManager.Instance.nextLevel == BCPPPlugin.Instance.sBoss))
                __result = true;
        }

        [HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.CreateCell), [typeof(int), typeof(Transform), typeof(IntVector2), typeof(RoomController), typeof(bool), typeof(bool)]), HarmonyPostfix]
        static void B2Refresh(int tileBin, Transform parent, IntVector2 position, RoomController room, bool destroyExisting, bool lockCell, EnvironmentController __instance)
        {
            if (!__instance.Active) return;
            if (!__instance.cells[position.x, position.z].Null && room.functionObject?.GetComponent<B2HallRoomFunction>() != null)
                room.functionObject.GetComponent<B2HallRoomFunction>().Refresh();
        }
        [HarmonyPatch(typeof(PropagatedAudioManager), "VirtualUpdate"), HarmonyPrefix]
        static void DoNotFuckUpThemAll(PropagatedAudioManager __instance, ref bool ___environmentSet, ref EnvironmentController ___environment)
        {
            if (!___environmentSet && CarnellBossManager.BossInstance != null)
            {
                Transform parent = __instance.transform;
                while (parent != null && parent.GetComponent<EnvironmentController>() == null)
                    parent = parent.parent;
                if (parent?.GetComponent<EnvironmentController>() != null)
                {
                    ___environment = parent.GetComponent<EnvironmentController>();
                    __instance.audioSourceManager.transform.SetParent(___environment.soundPropagationTransform);
                    ___environmentSet = true;
                }
            }
        }
    }
#endif
#if DEBUG
    [HarmonyPatch(typeof(LevelGenerator), "StartGenerate")]
    class EveryoneIsHere // This is outdated since v0.8 released.
    {
        static void Prefix(LevelGenerator __instance)
        {
            //__instance.ld.additionalNPCs = __instance.ld.potentialNPCs.Count;
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
                GameLoader gl = Resources.FindObjectsOfTypeAll<GameLoader>().First(x => x.gameObject.scene.name == "MainMenu");
                gl.gameObject.SetActive(true);
                __instance.gameObject.SetActive(false);
                gl.CheckSeed();
                gl.Initialize(1);
                gl.SetMode((int)Mode.Main);
                ElevatorScreen evl = SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name == "ElevatorScreen").First().GetComponent<ElevatorScreen>();
                gl.AssignElevatorScreen(evl);
                evl.gameObject.SetActive(true);
                //gl.LoadLevel(Resources.FindObjectsOfTypeAll<SceneObject>().ToList().Find(x => x.name == "MainLevel_3"));
                gl.LoadLevel(BCPPPlugin.Instance.sBoss);
                evl.Initialize();
                gl.SetSave(false);
            });
        }
    }
#endif
}