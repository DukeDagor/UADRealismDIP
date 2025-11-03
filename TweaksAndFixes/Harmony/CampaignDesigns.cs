using HarmonyLib;
using Il2Cpp;

#pragma warning disable CS8625

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignDesigns))]
    internal class Patch_CampaignDesigns
    {
        [HarmonyPatch(nameof(CampaignDesigns.RandomShip))]
        [HarmonyPrefix]
        internal static bool Prefix_RandomShip(CampaignDesigns __instance, Player player, ShipType type, int desiredYear, ref Ship.Store __result)
        {
            // use the fast version if we don't have any overlaying of predef sets
            if (!PredefinedDesignsData.Instance.NeedUseNewFindCode)
                return true;

            __result = PredefinedDesignsData.Instance.GetRandomShip(player, type, desiredYear);
            return false;
        }
    }
}
