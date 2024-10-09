﻿using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader.NativeUtils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Startup;

#pragma warning disable CS8603

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignNewGame))]
    internal class Patch_CampaignNewGame
    {
        internal static bool FixDesignUsage(CampaignNewGame cng)
        {
            if (!Config.ForceNoPredefsInNewGames)
                return false;

            cng.campaignDesignsUsage = 0;
            cng.DesignUsage.text = LocalizeManager.Localize($"$Ui_NewGame_DesignUsage_{(CampaignController.DesignsUsage)cng.campaignDesignsUsage}");
            return true;
        }

        [HarmonyPatch(nameof(CampaignNewGame.Show))]
        [HarmonyPrefix]
        internal static void Prefix_Show(CampaignNewGame __instance)
        {
            FixDesignUsage(__instance);
        }

        [HarmonyPatch(nameof(CampaignNewGame.ChangeDesignUsage))]
        [HarmonyPrefix]
        internal static bool Prefix_ChangeDesignUsage(CampaignNewGame __instance)
        {
            return !FixDesignUsage(__instance);
        }
    }
}
