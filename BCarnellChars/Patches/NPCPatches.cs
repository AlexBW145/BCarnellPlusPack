using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Patches
{
    [HarmonyPatch(typeof(Playtime), "StartJumprope")]
    class InterruptRPS
    {
        static void Prefix(Playtime __instance)
        {
            if (GameObject.FindObjectOfType<RPSGuy>())
            {
                RPSGuy rps = GameObject.FindObjectOfType<RPSGuy>();
                if (rps.Playing && !(Jumprope)AccessTools.DeclaredField(typeof(Playtime), "currentJumprope").GetValue(__instance))
                    rps.EndRPS(false, true);
            }
        }
    }

    [HarmonyPatch(typeof(CullingManager), "LateUpdate")]
    class CullingPortal
    {
        private static Cell currentCell;
        private static int currentChunkId = -1;
        static void Postfix(CullingManager __instance, ref EnvironmentController ___ec, ref bool ___active, ref bool ___manualMode)
        {
            if (___ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")))
            {
                MrPortalMan poo = ___ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("MrPortalMan")).gameObject.GetComponent<MrPortalMan>();
                if (poo && poo.portalmanCam != null && ___ec.ContainsCoordinates(IntVector2.GetGridPosition(poo.portalmanCam.transform.position)))
                {
                    currentCell = ___ec.CellFromPosition(poo.portalmanCam.transform.position);
                    if (currentCell.HasChunk && currentCell.Chunk.Id != currentChunkId && ___active && !___manualMode)
                    {
                        __instance.RenderChunk(currentCell.Chunk.Id, true);
                    }
                }
            }
            else if (currentCell != null)
            {
                currentChunkId = -1;
                currentCell = null;
            }
        }
    }
}
