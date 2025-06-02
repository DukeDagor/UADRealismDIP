﻿using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;
using UnityEngine.UI;
using Il2CppCoffee.UIExtensions;
using Il2CppTMPro;
using Il2CppUiExt;
using static TweaksAndFixes.Config;
using static MelonLoader.MelonLogger;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(MoveShipsWindow))]
    internal class Patch_MoveShipsWindow
    {
        public static void SetOnHover(MoveShipsWindow window)
        {
            foreach (MoveShip_Element element in window.allElements)
            {
                element.GetComponent<Button>().OnEnter(new System.Action(() =>
                {
                    window.SetShipInfoAndImage((Ship)element.CurrentVessel);
                }));
            }
        }

        [HarmonyPatch(nameof(MoveShipsWindow.ShowBaseFromMap))]
        [HarmonyPostfix]
        internal static void Postfix_ShowBaseFromMap(MoveShipsWindow __instance)
        {
            SetOnHover(__instance);
        }

        [HarmonyPatch(nameof(MoveShipsWindow.ShowBaseFromPort))]
        [HarmonyPostfix]
        internal static void Postfix_ShowBaseFromPort(MoveShipsWindow __instance)
        {
            SetOnHover(__instance);
        }
    }
}
