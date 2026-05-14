using HarmonyLib;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(PlayerController))]
    internal class Patch_PlayerController
    {
        public static bool updateTechsForNextClonedShips = false;

        [HarmonyPatch(nameof(PlayerController.CloneShipRaw))]
        [HarmonyPrefix]
        internal static void Prefix_CloneShipRaw(ref bool newTechnologies)
        {
            if (GameManager.IsConstructor && GameManager.IsCampaign && updateTechsForNextClonedShips)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Updating techs for cloned ship.");
                newTechnologies = true;
                updateTechsForNextClonedShips = false;
            }
        }

        [HarmonyPatch(nameof(PlayerController.CloneShipRaw))]
        [HarmonyPostfix]
        internal static void Postfix_CloneShipRaw(Ship from, ref Ship __result)
        {
            __result.TAFData().OnClonePost(from.TAFData());
        }
    }
}
