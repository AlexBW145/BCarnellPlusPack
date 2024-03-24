using BCarnellChars.Characters;
using BCarnellChars.Characters.States;
using HarmonyLib;
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
                    rps.EndRPS(false);
            }
        }
    }
}
