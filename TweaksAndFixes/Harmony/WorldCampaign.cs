using HarmonyLib;
using UnityEngine;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(WorldCampaign))]
    internal class Patch_WorldCampaign
    {
        private static bool HasDestroyedSubmarineButton = false;

        [HarmonyPatch(nameof(WorldCampaign.CreateWorld))]
        [HarmonyPostfix]
        internal static void Postfix_CreateWorld(WorldCampaign __instance)
        {
            // Check params
            if (Config.Param("taf_hide_map_vignettes", 0) == 1)
            {
                // Hide the left and right vignettes.
                GameObject rightBoarder = WorldCampaign.instance.worldEx.GetChild("2DMap").GetChild("BorderRight");
                GameObject leftBoarder = WorldCampaign.instance.worldEx.GetChild("2DMap").GetChild("BorderLeft");
            
                leftBoarder.TryDestroy();
                rightBoarder.TryDestroy();
            }

            if (Config.Param("taf_hide_submarine_managment_buttons", 0) == 1 && !HasDestroyedSubmarineButton)
            {
                GameObject submarines = G.ui.GetChild("WorldEx").GetChild("TopPanel").GetChild("Tabs").GetChild("Buttons").GetChild("Submarines");

                if (submarines != null)
                {
                    submarines.transform.SetParent(null);
                    submarines.SetActive(false);
                }

                HasDestroyedSubmarineButton = true;
            }
        }
    }
}
