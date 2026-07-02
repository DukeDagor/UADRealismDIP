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
}
