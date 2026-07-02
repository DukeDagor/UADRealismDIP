using HarmonyLib;
using UnityEngine;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignMap))]
    internal class Patch_CampaignMap
    {
        [HarmonyPatch(nameof(CampaignMap.CanMove))]
        [HarmonyPrefix]
        internal static bool Prefix_CanMove(CampaignMap __instance, Vector3 desiredPosition, float averageRange, ref bool __result)
        {
            __result = CampaignMapM.CanMove(desiredPosition, averageRange);
            return false;
        }

        internal static bool _SkipNextMapPatch = false;

        [HarmonyPatch(nameof(CampaignMap.PreInit))]
        [HarmonyPrefix]
        internal static void Prefix_PreInit(CampaignMap __instance)
        {
            if (!_SkipNextMapPatch && (Config.OverrideMap != Config.OverrideMapOptions.Disabled))
                MapData.LoadMapData();

            _SkipNextMapPatch = false;
        }
    }

    [HarmonyPatch(typeof(CampaignAreaPopupUI))]
    internal class Patch_CampaignAreaPopupUI
    {
        public static bool allowNextMouseOver = false;

        [HarmonyPatch(nameof(CampaignAreaPopupUI.Show))]
        [HarmonyPrefix]
        internal static bool Prefix_OnMouseOver()
        {
            if (allowNextMouseOver)
            {
                allowNextMouseOver = false;

                return true;
            }

            return false;
        }

        public static bool allowNextMouseExit = false;

        [HarmonyPatch(nameof(CampaignAreaPopupUI.Hide))]
        [HarmonyPrefix]
        internal static bool Prefix_OnMouseExit()
        {
            if (allowNextMouseExit)
            {
                allowNextMouseExit = false;

                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(CampaignProvincePopupUI))]
    internal class Patch_CampaignProvincePopupUI
    {
        public static bool allowNextMouseOver = false;

        [HarmonyPatch(nameof(CampaignProvincePopupUI.Show))]
        [HarmonyPrefix]
        internal static bool Prefix_OnMouseOver()
        {
            if (allowNextMouseOver)
            {
                allowNextMouseOver = false;

                return true;
            }

            return false;
        }

        public static bool allowNextMouseExit = false;

        [HarmonyPatch(nameof(CampaignProvincePopupUI.Hide))]
        [HarmonyPrefix]
        internal static bool Prefix_OnMouseExit()
        {
            if (allowNextMouseExit)
            {
                allowNextMouseExit = false;

                return true;
            }

            return false;
        }
    }
}
