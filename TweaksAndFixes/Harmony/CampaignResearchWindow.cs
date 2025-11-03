using HarmonyLib;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignResearchWindow))]
    internal class Patch_CampaignResearchWindow
    {
        [HarmonyPatch(nameof(CampaignResearchWindow.Show))]
        [HarmonyPrefix]
        internal static void Prefix_Show()
        {
            SpriteDatabase.Instance.OverrideResources();
        }
    }
}
