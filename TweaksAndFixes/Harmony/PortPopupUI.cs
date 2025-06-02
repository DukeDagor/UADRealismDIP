using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using static Il2Cpp.ActionsManager;
using System.Runtime.InteropServices;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(PortPopupUI))]
    internal class Patch_PortPopupUI
    {
        [HarmonyPatch(nameof(PortPopupUI.Show))]
        [HarmonyPostfix]
        internal static void Postfix_Show(PortPopupUI __instance, PortElement port)
        {
            // Check params
            if (Config.Param("taf_hide_submarine_managment_buttons", 0) == 0)
            {
                return;
            }

            // Called multiple times. Check if the button was already destroyed.
            if (__instance == null)
            {
                return;
            }

            if (__instance.MoveSubmarines == null)
            {
                return;
            }

            if (__instance.MoveSubmarines.transform == null)
            {
                return;
            }

            __instance.MoveSubmarines.transform.SetParent(null);
            __instance.MoveSubmarines.SetActive(false);
        }
    }
}
