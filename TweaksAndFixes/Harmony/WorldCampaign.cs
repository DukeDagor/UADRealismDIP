using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;

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

                rightBoarder.transform.eulerAngles = new(rightBoarder.transform.eulerAngles.x, 90, rightBoarder.transform.eulerAngles.z);
                rightBoarder.transform.SetScale(new(1f, 1f, 0.01f));
                rightBoarder.transform.localPosition = new(-7.6125f, 1f, 0f);

                leftBoarder.transform.eulerAngles = new(leftBoarder.transform.eulerAngles.x, 270, leftBoarder.transform.eulerAngles.z);
                leftBoarder.transform.SetScale(new(1f, 1f, 0.01f));
                leftBoarder.transform.localPosition = new(7.6125f, 1f, 0f);
            }
            else
            {
                // Expand size of vignettes to cover empty space to either side
                GameObject rightBoarder = WorldCampaign.instance.worldEx.GetChild("2DMap").GetChild("BorderRight");
                GameObject leftBoarder = WorldCampaign.instance.worldEx.GetChild("2DMap").GetChild("BorderLeft");

                rightBoarder.transform.eulerAngles = new(rightBoarder.transform.eulerAngles.x, 270, rightBoarder.transform.eulerAngles.z);
                rightBoarder.transform.SetScale(new(1f, 1f, 0.01f));
                rightBoarder.transform.localPosition = new(-4.95f, 1f, 0f);

                leftBoarder.transform.eulerAngles = new(leftBoarder.transform.eulerAngles.x, 90, leftBoarder.transform.eulerAngles.z);
                leftBoarder.transform.SetScale(new(1f, 1f, 0.01f));
                leftBoarder.transform.localPosition = new(4.95f, 1f, 0f);
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

            GameObject mapImage = ModUtils.GetChildAtPath("2DMap/Map", WorldCampaign.instance.worldEx);
            var mapRenderer = mapImage.GetComponent<MeshRenderer>();
            mapRenderer.enabled = UiM.TAF_Settings.settings.showMapImage;
        }
    }
}
