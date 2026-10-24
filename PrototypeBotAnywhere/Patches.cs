using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using TeacherAPI;
using UnityEngine;

namespace PrototypeBotAnywhere
{
    [HarmonyPatch(typeof(TapePlayer), "InsertItem")]
    class StunBotTeacher
    {
        static void Postfix(PlayerManager player, EnvironmentController ec, ref float ___time)
        {
            if (ec.Npcs.Find(x => x.GetComponent<PrototypeBot01_Teacher>()))
            {
                var proto = ec.Npcs.Find(x => x.GetComponent<PrototypeBot01_Teacher>()).gameObject.GetComponent<PrototypeBot01_Teacher>();
                proto.StopAllCoroutines();
                proto.StartCoroutine(
                    proto.StunHearing(___time));
            }
        }
    }

    [HarmonyPatch]
    class ProtoBotPatches
    {
        [HarmonyPatch(typeof(Activity), "Completed", [typeof(int)]), HarmonyPrefix]
        static bool BasementGet(int player, ref Notebook ___notebook, Activity __instance, ref bool ___bonusMode, ref bool ___completed)
        {
            if (TeacherManager.Instance == null || TeacherManager.DefaultBaldiEnabled) return true;
            if (__instance is not NoActivity && !___bonusMode && TeacherManager.Instance.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is PrototypeBot01_Teacher)
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
            if (TeacherManager.Instance == null || TeacherManager.DefaultBaldiEnabled) return;
            if (correct && !___bonusMode && TeacherManager.Instance.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is PrototypeBot01_Teacher)
                foreach (Cell light in __instance.room.cells)
                    light.SetPower(true);
        }

        [HarmonyPatch(typeof(TimeOut), nameof(TimeOut.Begin)), HarmonyPrefix]
        static bool B1TimeOut(TimeOut __instance, EnvironmentController ___ec, ref List<Cell> ___lightsToTurnOff, ref float ___baldiAngerRate, ref float ___baldiAngerAmount, ref float ___lightOffRate, ref bool ___active)
        {
            if (TeacherManager.Instance == null || TeacherManager.DefaultBaldiEnabled) return true;
            if (TeacherManager.Instance.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is PrototypeBot01_Teacher)
            {
                ___active = true;
                ___baldiAngerRate = 5f;
                ___baldiAngerAmount = 0f;
                ___lightOffRate = 0.1f;
                var proto = TeacherManager.Instance.SpawnedMainTeacher as PrototypeBot01_Teacher;
                proto.ScaryMode();
                if (FogAndSndGoAway.fogFade != null)
                    BaseGameManager.Instance.StopCoroutine(FogAndSndGoAway.fogFade);
                BaseGameManager.Instance.StartCoroutine(FadeOffFog(proto, proto.ec, proto.fog));
                ___ec.lights.Do(l =>
                {
                    l.lightColor = Color.red;
                    ___ec.QueueLightSourceForRegenerate(l);
                });
                ___lightsToTurnOff.AddRange(___ec.lights);
                ___lightsToTurnOff.Shuffle();
                ___ec.CloseSchool();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(TimeOut), "Update"), HarmonyPrefix]
        static bool B1TimeOutUpdate(TimeOut __instance, EnvironmentController ___ec, ref List<Cell> ___lightsToTurnOff, ref float ___baldiAngerRate, ref float ___baldiAngerAmount, ref float ___lightOffRate,
            ref float ___timeToNextAnger, ref float ___timeToNextLight)
        {
            if (TeacherManager.Instance == null || TeacherManager.DefaultBaldiEnabled) return true;
            if (TeacherManager.Instance.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is PrototypeBot01_Teacher)
            {
                if (!__instance.Active) return false;
                var proto = TeacherManager.Instance.SpawnedMainTeacher as PrototypeBot01_Teacher;
                ___timeToNextAnger -= Time.deltaTime * ___ec.NpcTimeScale;
                ___timeToNextLight -= Time.deltaTime * ___ec.EnvironmentTimeScale;
                if (___timeToNextAnger <= 0f)
                {
                    ___timeToNextAnger = ___baldiAngerRate + ___timeToNextAnger;
                    ___baldiAngerAmount++;
                }
                if (proto.behaviorStateMachine.currentState is not PrototypeBot_Teacher_Stunned)
                {
                    proto.Navigator.maxSpeed = 40f + ___baldiAngerAmount;
                    proto.Navigator.SetSpeed(40f + ___baldiAngerAmount);
                }
                else
                {
                    proto.Navigator.maxSpeed = 0f;
                    proto.Navigator.SetSpeed(0f);
                }

                if (___timeToNextLight <= 0f)
                {
                    ___timeToNextLight = ___lightOffRate + ___timeToNextLight;
                    if (___lightsToTurnOff.Count > 0)
                    {
                        ___ec.QueueLightSourceForRegenerate(___lightsToTurnOff[0]);
                        ___lightsToTurnOff[0].SetPower(true);
                        ___lightsToTurnOff.RemoveAt(0);
                    }
                }
                return false;
            }
            return true;
        }

        private static IEnumerator FadeOffFog(PrototypeBot01_Teacher proto, EnvironmentController ec, Fog fog)
        {
            float fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
            while (fogStrength2 > 0f)
            {
                if (proto == null || TeacherManager.Instance.SpawnedMainTeacher is not PrototypeBot01_Teacher)
                {
                    fog.strength = 0f;
                    ec.UpdateFog();
                    yield break;
                }
                fogStrength2 -= 1f * Time.deltaTime;
                fog.strength = fogStrength2;
                if (proto?.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume < 1f)
                    proto.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume += 0.25f * Time.deltaTime;
                else if (proto?.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume > 1f)
                    proto.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume = 1f;
                ec.UpdateFog();
                yield return null;
            }
            proto.Ambience.audioSourceManager.volume = 1f;
            fogStrength2 = 0f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
        }
    }

    [HarmonyPatch(typeof(RoomFunction))]
    internal class FogAndSndGoAway
    {
        [HarmonyPatch("OnPlayerEnter"), HarmonyPostfix]
        private static void GoAway(RoomFunction __instance, RoomController ___room)
        {
            if (__instance is SunlightRoomFunction && !___room.ec.timeOut && TeacherManager.Instance?.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is PrototypeBot01_Teacher)
            {
                if (fogFade != null)
                    BaseGameManager.Instance.StopCoroutine(fogFade);
                fogFade = FadeOffFog(TeacherManager.Instance.SpawnedMainTeacher as PrototypeBot01_Teacher, ___room.ec, TeacherManager.Instance.SpawnedMainTeacher.GetComponent<PrototypeBot01_Teacher>().fog);
                BaseGameManager.Instance.StartCoroutine(fogFade);
            }
        }

        [HarmonyPatch("OnPlayerExit"), HarmonyPostfix]
        private static void GoIn(RoomFunction __instance, RoomController ___room)
        {
            if (__instance is SunlightRoomFunction && !___room.ec.timeOut && TeacherManager.Instance?.SpawnedMainTeacher != null && TeacherManager.Instance.SpawnedMainTeacher is PrototypeBot01_Teacher)
            {
                if (fogFade != null)
                    BaseGameManager.Instance.StopCoroutine(fogFade);
                fogFade = FadeOnFog(TeacherManager.Instance.SpawnedMainTeacher as PrototypeBot01_Teacher, ___room.ec, TeacherManager.Instance.SpawnedMainTeacher.GetComponent<PrototypeBot01_Teacher>().fog);
                BaseGameManager.Instance.StartCoroutine(fogFade);
            }
        }

        internal static IEnumerator fogFade;

        private static IEnumerator FadeOnFog(PrototypeBot01_Teacher proto, EnvironmentController ec, Fog fog)
        {
            fog.strength = 0f;
            float fogStrength2 = 0f;
            while (fogStrength2 < 1f)
            {
                if (proto == null || TeacherManager.Instance.SpawnedMainTeacher is not PrototypeBot01_Teacher)
                {
                    fog.strength = 1f;
                    ec.UpdateFog();
                    yield break;
                }
                fogStrength2 += 1f * Time.deltaTime;
                if (proto?.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume < 1f)
                    proto.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume += 0.25f * Time.deltaTime;
                else if (proto?.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume > 1f)
                    proto.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume = 1f;
                fog.strength = fogStrength2;
                ec.UpdateFog();
                yield return null;
            }
            proto.Ambience.audioSourceManager.volume = 1f;
            fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
        }

        private static IEnumerator FadeOffFog(PrototypeBot01_Teacher proto, EnvironmentController ec, Fog fog)
        {
            float fogStrength2 = 1f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
            while (fogStrength2 > 0f)
            {
                if (proto == null || TeacherManager.Instance.SpawnedMainTeacher is not PrototypeBot01_Teacher)
                {
                    fog.strength = 0f;
                    ec.UpdateFog();
                    yield break;
                }
                fogStrength2 -= 1f * Time.deltaTime;
                if (proto?.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume > 0f)
                    proto.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume -= 0.25f * Time.deltaTime;
                else if (proto?.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume < 0f)
                    proto.Ambience.audioSourceManager.GetAudioSource(SoundType.Effect).volume = 0f;
                fog.strength = fogStrength2;
                ec.UpdateFog();
                yield return null;
            }
            proto.Ambience.audioSourceManager.volume = 0f;
            fogStrength2 = 0f;
            fog.strength = fogStrength2;
            ec.UpdateFog();
        }
    }
    
}
