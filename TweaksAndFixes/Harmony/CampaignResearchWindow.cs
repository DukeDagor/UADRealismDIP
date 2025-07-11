using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignResearchWindow))]
    internal class Patch_CampaignResearchWindow
    {
        [HarmonyPatch(nameof(CampaignResearchWindow.Show))]
        [HarmonyPrefix]
        internal static void Prefix_Show()
        {
            // SpriteDatabase.Instance.OverrideResources();
        }
    }
}
