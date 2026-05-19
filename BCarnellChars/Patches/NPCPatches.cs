using BCarnellChars.Characters;
using BCarnellChars.OtherStuff;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.Patches
{
    [HarmonyPatch(typeof(Jumprope), "Start")]
    class InterruptRPS
    {
        static void Prefix()
        {
            if (GameObject.FindObjectOfType<RockPaperScissors>(false) != null)
            {
                var rps = GameObject.FindObjectsOfType<RockPaperScissors>(false).ToList();
                while (rps.Count > 0)
                {
                    rps[0].rps.EndRPS(false, true);
                    rps.RemoveAt(0);
                }
            }
        }
    }

    // New collision system can make Portal Man ignore squished downsides, useless.
/*[HarmonyPatch(typeof(Entity), nameof(Entity.Squish))]
class PortalManIsInvincible
{
    static bool Prefix(Entity __instance) => !__instance.gameObject.GetComponent<MrPortalMan>();
}*/

// Missed the texture made a better version in the latest API update, useless.
/*[HarmonyPatch(typeof(CullingManager), "LateUpdate")]
class CullingPortal
{
    private static Cell currentCell;
    private static Cell currentCellOutputCam;
    private static int currentChunkId = -1;
    private static int currentChunkIdOutputCam = -1;
    static void Postfix(CullingManager __instance, ref EnvironmentController ___ec, ref bool ___active, ref bool ___manualMode)
    {
        if (Time.timeScale == 0)
            return;
        if (___ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")))
        {
            MrPortalMan poo = ___ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).gameObject.GetComponent<MrPortalMan>();
            if (poo)
            {
                if (poo.portalmanCam != null && ___ec.ContainsCoordinates(IntVector2.GetGridPosition(poo.portalmanCam.transform.position)))
                {
                    currentCell = ___ec.CellFromPosition(poo.portalmanCam.transform.position);
                    if (currentCell.HasChunk && currentCell.Chunk.Id != currentChunkId)
                        currentChunkId = currentCell.Chunk.Id;
                }
                if (poo.OutputCam != null && ___ec.ContainsCoordinates(IntVector2.GetGridPosition(poo.OutputCam.transform.position)))
                {
                    currentCellOutputCam = ___ec.CellFromPosition(poo.OutputCam.transform.position);
                    if (currentCellOutputCam.HasChunk && currentCellOutputCam.Chunk.Id != currentChunkIdOutputCam)
                        currentChunkIdOutputCam = currentCellOutputCam.Chunk.Id;
                }

            }
            if (___active && !___manualMode) {
                for (int i = 0; i < __instance.allChunks[currentChunkId].visibleChunks.Length; i++)
                {
                    if (__instance.allChunks[i].Rendering)
                        continue;
                    __instance.allChunks[i].Render(__instance.allChunks[currentChunkId].visibleChunks[i]);
                }
                if (!__instance.allChunks[currentChunkIdOutputCam].Rendering)
                    __instance.RenderChunk(currentChunkIdOutputCam, true);
            }
        }
        else if (currentCell != null)
        {
            currentChunkId = -1;
            currentChunkIdOutputCam = -1;
            currentCell = null;
        }
    }
}*/
#if !RELEASE
    [HarmonyPatch(typeof(HideableLockerBaldiInteraction), nameof(HideableLockerBaldiInteraction.Trigger))]
    class BCarnellLockerInter
    {
        static bool Prefix(Baldi baldi, HideableLockerBaldiInteraction __instance)
        {
            if (baldi is not BCarnellBoss) return true;
            __instance.Payload(baldi);
            return false;
        }
    }
#endif
}
