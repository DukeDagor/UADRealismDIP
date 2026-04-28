using HarmonyLib;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(MinesFieldManager))]
    internal class Patch_MinesFieldManager
    {
        [HarmonyPatch(nameof(MinesFieldManager.FromStore))]
        [HarmonyPrefix]
        internal static void Prefix_FromStore(MinesFieldManager.Store store, out Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            __state = Patch_CampaignController.BeginCampaignLoadMethodTiming(nameof(MinesFieldManager.FromStore), $"storeNull={store == null}");
            Patch_CampaignController.EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(MinesFieldManager.FromStore))]
        [HarmonyPostfix]
        internal static void Postfix_FromStore(Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            Patch_CampaignController.EndCampaignLoadMethodTiming(__state);
        }

        [HarmonyPatch(nameof(MinesFieldManager.DamageTaskForce))]
        [HarmonyPrefix]
        internal static bool Prefix_DamageTaskForce(MinesFieldManager __instance, CampaignController.TaskForce taskForce, Player mineFieldOwner, float minefieldRadiusKm, float damageMultiplier, ref float __result)
        {
            __result = MinesFieldManagerM.DamageTaskForce(__instance, taskForce, mineFieldOwner, minefieldRadiusKm, damageMultiplier);
            return false;
        }
    }
}
