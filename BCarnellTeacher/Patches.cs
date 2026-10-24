using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TeacherAPI;
using UnityEngine;

namespace BCarnellTeacher
{
    [HarmonyPatch]
    class ChangeSchoolMusic
    {
        /*[HarmonyPatch(typeof(MainGameManager), "BeginPlay")]
        [HarmonyPatch(typeof(EndlessGameManager), "BeginPlay")]
        [HarmonyPostfix]
        static void SwapWithMIDI() // Usually, if B. Carnell is the chosen teacher, then play dis instead!
        {
            if (TeacherManager.Instance == null || TeacherManager.Instance?.SpawnedMainTeacher == null)
                return;
            if (TeacherManager.Instance.SpawnedMainTeacher?.GetComponent<BCarnell>() && !TeacherManager.Instance.SpawnedMainTeacher.IsHelping())
            {
                MusicManager.Instance.StopMidi();
                MusicManager.Instance.PlayMidi(Plugin.bcarnelAssets.Get<string>("SchoolHouseCarnell"), true);
            }
        }
        [HarmonyPatch(typeof(MainGameManager), "AllNotebooks")]
        [HarmonyPostfix]
        static void YeahItsTemp()
        {
            if (TeacherManager.Instance.GetTeachersOfType<BCarnell>()[0] == null)
                return;
            TeacherManager.Instance.GetTeachersOfType<BCarnell>().First().OnAllNotebooksCollected();
        }*/
        [HarmonyPatch(typeof(BaseGameManager), "AllNotebooks")]
        [HarmonyPostfix]
        static void CarnellTime()
        {
            if (TeacherManager.Instance == null || TeacherManager.Instance?.SpawnedMainTeacher == null)
                return;
            if (TeacherManager.Instance.SpawnedMainTeacher?.GetComponent<BCarnell>() != null)
                TeacherManager.Instance.SpawnedMainTeacher.GetComponent<BCarnell>().AllNotebooks();
        }
        /*[HarmonyPatch(typeof(MainGameManager), "AllNotebooks", MethodType.Normal)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NoNotebooksNotif(IEnumerable<CodeInstruction> instructions)
        {
            if (!TeacherManager.Instance.SpawnedMainTeacher.GetComponent<BCarnell>())
                return null;
            if (!TeacherManager.Instance.SpawnedMainTeacher.GetComponent<BCarnell>().IsHelping())
                return null;

            return new CodeMatcher(instructions)
            .End()
            .MatchBack(false,
                new(OpCodes.Call),
                new(OpCodes.Ldfld),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(MainGameManager), "MainGameManager:allNotebooksNotification")),
                new(OpCodes.Callvirt, AccessTools.Method("EnvironmentController:SetTileInstantiation")))
            .RemoveInstructions(5)
            .InstructionEnumeration();
        }*/
            
        [HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
        [HarmonyPostfix]
        static void StopPlayingFinaleMIDI()
        {
            if (BaseGameManager.Instance == null || !(bool)BaseGameManager.Instance?.ReflectionGetVariable("playStarted")
                || TeacherManager.Instance == null || TeacherManager.Instance?.SpawnedMainTeacher == null)
                return;
            if (TeacherManager.Instance.SpawnedMainTeacher?.GetComponent<BCarnell>() != null)
                TeacherManager.Instance.SpawnedMainTeacher.GetComponent<BCarnell>().ReflectionSetVariable("startedFinale", false);
        }
    }
    [HarmonyPatch]
    class MiscCarnellPatches
    {
        [HarmonyPatch(typeof(TapePlayer), "InsertItem"), HarmonyPostfix]
        static void WhenYouAttractedWithNoise(TapePlayer __instance, ref List<NavigationState_WanderFleeOverride> ___fleeStates) // One problem is that the dijistrka map makes this happen after the tape player ends.
        {
            IEnumerator Huh(BCarnell bcarnell, List<NavigationState_WanderFleeOverride> fleeStates)
            {
                while (!fleeStates.Exists(f => f.npc == bcarnell))
                    yield return null;
                NavigationState_WanderFleeOverride state = bcarnell.navigationStateMachine.ReflectionGetVariable("currentState") as NavigationState_WanderFleeOverride;
                state.End();
            }
            if (BaseGameManager.Instance.Ec.Npcs.Exists(x => x.GetComponent<BCarnell>()))
            {
                var bcarnell = BaseGameManager.Instance.Ec.Npcs.Find(x => x.GetComponent<BCarnell>()).GetComponent<BCarnell>();
                bcarnell.StartCoroutine(Huh(bcarnell, ___fleeStates));
                
                bcarnell.Attract(__instance.transform.position);
            }
        }
        static MethodInfo SetGuilt = AccessTools.DeclaredMethod(typeof(NPC), "SetGuilt", [typeof(float), typeof(string)]);
        [HarmonyPatch(typeof(TimeOut), "Update"), HarmonyPostfix]
        static void LaterStudents(TimeOut __instance, ref EnvironmentController ___ec, ref List<Cell> ___lightsToTurnOff)
        {
            if (!__instance.Active) return;
            if (BaseGameManager.Instance.Ec.Npcs.Exists(x => x.Character == CarnellPlugin.Nerd)
                && ___lightsToTurnOff.Count() <= Mathf.RoundToInt(___ec.hallLights.Count() / 1.5f))
            {
                foreach (NPC npc in ___ec.Npcs)
                {
                    if (npc.Character == Character.Null && npc is not Student) continue;
                    bool 
                        isStudent = npc.GetMeta()?.tags.Contains("student") == true || npc is Student,
                        hasTrigger = npc.GetMeta()?.flags.HasFlag(NPCFlags.HasTrigger) == true || npc is Student;
                    if (!MakeStudentsFaulty.facultyWhitelist.Contains(npc.Character)
                        && hasTrigger && isStudent && npc.Navigator.Entity.CurrentRoom?.functionObject.GetComponent<RuleFreeZone>() == null)
                        SetGuilt.Invoke(npc, [1000000f, "AfterHours"]);
                    else if (npc.BrokenRule == "AfterHours" && npc.Navigator.Entity.CurrentRoom?.functionObject.GetComponent<RuleFreeZone>() != null
                        && !MakeStudentsFaulty.facultyWhitelist.Contains(npc.Character)
                        && hasTrigger && isStudent)
                        npc.ClearGuilt();
                }
            }
        }
    }
    [HarmonyPatch(typeof(DetentionRoomFunction))]
    class DetentionForYouAgain
    {
        [HarmonyPatch("OnNpcEnter")]
        [HarmonyPatch("OnNpcExit")]
        [HarmonyPostfix]
        static void LockDoorsAsUsual(NPC npc, DetentionRoomFunction __instance, ref bool ___active)
        {
            if (npc.Character == CarnellPlugin.Nerd && ___active)
                __instance.ReflectionInvoke("LockDoors", []);
        }

        [HarmonyPatch("OnNpcExit")]
        [HarmonyPostfix]
        static void DoubleCheck(NPC npc, ref bool ___active)
        {
            if (npc.Character == CarnellPlugin.Nerd && ___active)
                npc.GetComponent<PoliceNerd>().BeforeRelization();
        }
    }
    [HarmonyPatch(typeof(ITM_PrincipalWhistle), "Use")]
    class WhistleNerd
    {
        static void Postfix(PlayerManager pm)
        {
            foreach (NPC npc in pm.ec.Npcs)
            {
                if (npc.Character == CarnellPlugin.Nerd)
                    npc.GetComponent<PoliceNerd>().WhistleReact(pm.transform.position);
            }
        }
    }

    [HarmonyPatch(typeof(RoomFunction))]
    class MakeStudentsFaulty
    {
        static MethodInfo SetGuilt = AccessTools.DeclaredMethod(typeof(NPC), "SetGuilt", [typeof(float), typeof(string)]);
        public static HashSet<Character> facultyWhitelist = new HashSet<Character>();
        public static void GetMore()
        {
            facultyWhitelist = new HashSet<Character>();
            NPCMetaStorage.Instance.All().DoIf(npc => !npc.tags.Contains("student") && (npc.tags.Contains("faculty") || npc.tags.Contains("teacher")), (npc) => facultyWhitelist.Add(npc.value.Character));
            if (NPCMetaStorage.Instance.All().ToList().Exists(x => x.character.ToStringExtended() == "MrPortalMan")) // Just a fix...
                facultyWhitelist.Add(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan"));
            /*facultyWhitelist = new HashSet<Character>()
            {
                Character.Baldi,
                Character.Principal,
                Character.Prize,
                Character.Pomp,
                Character.Sweep, // When the sweep got arrested for entering faculty only rooms!
                Character.LookAt,
                Character.Chalkles,
                Character.DrReflex,
                EnumExtensions.GetFromExtendedName<Character>("BCarnell"),
                EnumExtensions.GetFromExtendedName<Character>("PoliceNerd"),
            };
            Dictionary<Character, NPC> whoare = TeacherPlugin.Instance.ReflectionGetVariable("whoAreTeachers") as Dictionary<Character, NPC>;
            foreach (Teacher teacher in whoare.Values)
                facultyWhitelist.Add(teacher.Character);
            if (NPCMetaStorage.Instance.All().ToList().Exists(x => x.character.ToStringExtended() == "MrPortalMan"))
                facultyWhitelist.Add(EnumExtensions.GetFromExtendedName<Character>("MrPortalMan"));
            if (NPCMetaStorage.Instance.All().ToList().Exists(x => x.character.ToStringExtended() == "VADOK")) // This is just a test whitelist.
                facultyWhitelist.Add(EnumExtensions.GetFromExtendedName<Character>("VADOK"));*/
        }
        [HarmonyPatch(nameof(RoomFunction.OnNpcStay))] // Seems like FacultyTrigger does not have that value...
        [HarmonyPatch(nameof(RoomFunction.OnNpcExit))]
        static void Postfix(RoomFunction __instance, NPC npc)
        {
            if (__instance is not FacultyTrigger) return;
            if (BaseGameManager.Instance == null || BaseGameManager.Instance?.Ec == null) return;
            if (npc.Character == Character.Null && npc is not Student) return;
            if (BaseGameManager.Instance.Ec.Npcs.Exists(x => x.Character == CarnellPlugin.Nerd)
                && (npc.GetMeta()?.flags.HasFlag(NPCFlags.HasTrigger) == true || npc is Student)
                && !facultyWhitelist.Contains(npc.Character) && CoreGameManager.Instance.sceneObject.baldiPrefab?.Character == npc.Character)
            {
                SetGuilt.Invoke(npc, [1f, "Faculty"]);
            }
        }
    }

    [HarmonyPatch(typeof(RulerEvent), nameof(RulerEvent.Begin))] // Nerf the broken ruler event time if B. Carnell exists.
    class RulerLess
    {
        static void Prefix(ref float ___eventTime)
        {
            if (TeacherManager.Instance == null) return;
            if (TeacherManager.Instance.SpawnedMainTeacher?.GetComponent<BCarnell>())
                ___eventTime = 22.1f;
        }
    }

    /*[HarmonyPatch(typeof(CharacterPostersRoomFunction), nameof(CharacterPostersRoomFunction.Build))]
    class PosterFix
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).Start().MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NPC), nameof(NPC.Poster))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<PosterObject>), nameof(List<PosterObject>.Add))))
                .Insert(new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldloc_3),
                Transpilers.EmitDelegate<Func<NPC, bool>>((npc) =>
                {
                    if (TeacherManager.Instance == null) return false;
                    var mainteach = AccessTools.DeclaredField(typeof(TeacherManager), "<MainTeacherPrefab>k__BackingField").GetValue(TeacherManager.Instance) as Teacher; // I took it from UnityExplorer
                    return npc.Character == Character.Principal && mainteach?.GetComponent<BCarnell>() != null;
                }),
                new CodeInstruction(OpCodes.Brtrue, OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt, Transpilers.EmitDelegate<Func<NPC>>(() => Plugin.bcarnelAssets.Get<NPC>("NPCs/PoliceNerd"))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<PosterObject>), nameof(List<PosterObject>.Add))),
                new CodeInstruction(OpCodes.Br, OpCodes.Ldloc_1))
                .InstructionEnumeration();
        }
    }*/

    [HarmonyPatch(typeof(LevelGenerator))]
    class SwapPrincipalWithNerd
    {
        static MethodInfo prefab = AccessTools.DeclaredPropertyGetter(typeof(TeacherManager), "MainTeacherPrefab");
        static FieldInfo assisting = AccessTools.DeclaredField(typeof(TeacherManager), "assistingTeachersPrefabs");
        [HarmonyPatch(nameof(LevelGenerator.StartGenerate)), HarmonyPostfix]
        static void AddInNerd(LevelGenerator __instance)
        {
            if (TeacherManager.Instance != null && ((Teacher)prefab.Invoke(TeacherManager.Instance, []))?.Character == Character.Baldi
                && ((List<Teacher>)assisting.GetValue(TeacherManager.Instance))?.Contains(CarnellPlugin.bcarnelAssets.Get<NPC>("NPCs/BCarnell")) == true)
                __instance.ld.forcedNpcs = __instance.ld.forcedNpcs.AddToArray(CarnellPlugin.bcarnelAssets.Get<NPC>("NPCs/PoliceNerd"));
        }

        [HarmonyPatch(nameof(LevelGenerator.Generate), MethodType.Enumerator), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceWithNerd(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
            .End()
            .MatchBack(true,
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Singleton<CoreGameManager>), nameof(Singleton<CoreGameManager>.Instance))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CoreGameManager), nameof(CoreGameManager.Seed))),
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LevelBuilder), nameof(LevelBuilder.seedOffset))),
            new CodeMatch(OpCodes.Add),
            new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(System.Random), [typeof(int)])),
            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(LevelBuilder), nameof(LevelBuilder.controlledRNG)))
            ).ThrowIfInvalid("WTF").Advance(1)
            .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldloc_2), //this
            CodeInstruction.LoadField(typeof(LevelBuilder), "ec"), //ec
            Transpilers.EmitDelegate<Action<EnvironmentController>>((__instance) =>
            {
                if (TeacherManager.Instance != null && prefab.Invoke(TeacherManager.Instance, []) is BCarnell)
                    for (int i = 0; i < __instance.npcsToSpawn.Count; i++)
                    {
                        if (__instance.npcsToSpawn[i].Character == Character.Principal)
                            __instance.npcsToSpawn[i] = CarnellPlugin.bcarnelAssets.Get<NPC>("NPCs/PoliceNerd");
                    }
            })
            )
            .InstructionEnumeration();
    }

    [HarmonyPatch]
    class UhhItsAStateMachineBugfix // Long thing, it doesn't let me use multiple referenced private vars in a different subclass.
    {
        [HarmonyPatch(typeof(Principal), nameof(Principal.Scold)), HarmonyPrefix]
        static bool NotErroring(string brokenRule, Principal __instance) // I hate Testerly's error
        {
            if (__instance is PoliceNerd)
            {
                PoliceNerd hi = __instance as PoliceNerd;
                hi.Scold(brokenRule);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Principal_Wandering), nameof(Principal_Wandering.Enter)), HarmonyPrefix]
        static bool Wan(Principal_Wandering __instance)
        {
            if (__instance.Npc is PoliceNerd)
            {
                __instance.Npc.behaviorStateMachine.ChangeState(new PoliceNerd_Wandering(__instance.Npc as PoliceNerd));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Principal_ChasingPlayer), nameof(Principal_ChasingPlayer.Enter)),
            HarmonyPatch(typeof(Principal_ChasingPlayer), nameof(Principal_ChasingPlayer_AllKnowing.Enter)), 
            HarmonyPrefix]
        static bool Pla(Principal_ChasingPlayer __instance, ref PlayerManager ___player)
        {
            if (__instance.Npc is PoliceNerd)
            {
                __instance.Npc.behaviorStateMachine.ChangeState(new PoliceNerd_ChasingPlayer(__instance.Npc as PoliceNerd, ___player));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Principal_ChasingNpc), nameof(Principal_ChasingNpc.Enter)), HarmonyPrefix]
        static bool NC(Principal_ChasingNpc __instance, ref NPC ___targetedNpc)
        {
            if (__instance.Npc is PoliceNerd)
            {
                __instance.Npc.behaviorStateMachine.ChangeState(new PoliceNerd_TargetingNPC(__instance.Npc as PoliceNerd, ___targetedNpc));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Principal_WhistleApproach), nameof(Principal_WhistleApproach.Initialize)), HarmonyPrefix]
        static bool Wh(Principal_StateBase __instance, NpcState ___previousState, ref Vector3 ___destination)
        {
            if (__instance.Npc is PoliceNerd)
            {
                __instance.Npc.behaviorStateMachine.ChangeState(new PoliceNerd_WhistleApproach(__instance.Npc as PoliceNerd, ___previousState, ___destination));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Principal), nameof(Principal.WhistleReact)), HarmonyPrefix]
        static bool HeyKid(Vector3 target, Principal __instance)
        {
            if (__instance is PoliceNerd)
            {
                PoliceNerd hi = __instance as PoliceNerd;
                hi.WhistleReact(target);
                return false;
            }
            return true;
        }
    }
}