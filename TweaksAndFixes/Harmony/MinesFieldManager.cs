﻿using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(MinesFieldManager))]
    internal class Patch_MinesFieldManager
    {

        [HarmonyPatch(nameof(MinesFieldManager.DamageTaskForce))]
        [HarmonyPrefix]
        internal static bool Prefix_DamageTaskForce(MinesFieldManager __instance, CampaignController.TaskForce taskForce, Player mineFieldOwner, float minefieldRadiusKm, float damageMultiplier, ref float __result)
        {
            __result = MinesFieldManagerM.DamageTaskForce(__instance, taskForce, mineFieldOwner, minefieldRadiusKm, damageMultiplier);
            return false;
        }
    }
}
