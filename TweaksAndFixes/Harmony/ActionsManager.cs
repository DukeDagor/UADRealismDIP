using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using static Il2Cpp.ActionsManager;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignMap))]
    internal class Patch_ActionsManager
    {
        // [HarmonyPatch(nameof(ActionsManager.ReportAction))]
        // [HarmonyPrefix]
        // internal static void Prefix_ReportAction(bool success, ActionType action, Player player, Player choosenPlayer, bool isAi, params int[] param)
        // {
        //     Melon<TweaksAndFixes>.Logger.Msg("Report Action: " + player.Name(false) + " " + (success ? "SUCCEDED" : "FAILED") + " to " + action + " for " + choosenPlayer.Name(false));
        // }
    }
}
