using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(BattleResultWindow))]
    internal class Patch_BattleResultsWindow
    {
        // For some reason this goes dead during the battle.
        // Sprites unloaded maybe?
        // So let's just recreate it rather than figure out why.
        [HarmonyPatch(nameof(BattleResultWindow.InitCommon))]
        [HarmonyPrefix]
        internal static void Prefix_InitCommon()
        {
            // FlagDatabase.Recreate();
        }
    }
}
