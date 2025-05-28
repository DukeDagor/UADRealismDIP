using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using UnityEngine.UI;
using Il2CppCoffee.UIExtensions;
using Il2CppTMPro;
using System.Collections;
using Il2CppUiExt;
using static System.Net.Mime.MediaTypeNames;
using static MelonLoader.MelonLogger;
using static TweaksAndFixes.Config;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(CampaignPoliticsWindow))]
    internal class Patch_CampaignPoliticsWindow
    {
        public static bool HasPerformedActionThisTurn = false;

        public static bool PlayerHasPerformedActionThisTurn()
        {
            Player MainPlayer = ExtraGameData.MainPlayer();

            if (MainPlayer == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CampaignPoliticsWindow.UpdateInfo]. Default behavior will be used.");
                return false;
            }

            // Loop over each countries politics sections
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Player, CampaignPolitics_ElementUI> element in G.ui.PoliticsWindow.createdElements)
            {
                if (element.value == null)
                {
                    continue;
                }

                if (element.key == MainPlayer)
                {
                    continue;
                }

                // Get improve relations text
                GameObject improveRelationsButton = element.value.ImproveRelations.GetChildren()[0];
                TMP_Text improveRelationsText = improveRelationsButton.GetComponent<TMP_Text>();

                // Check if the player already did an action (not including choosing a naval invasion)
                if ((int)(improveRelationsText.color.g * 10) == 7 && G.ui.NavalInvasionElement.choosenProvince == null && !element.key.AtWarWith().Contains(MainPlayer))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ForceNavalInvasionButtonsActive()
        {
            // if (!USER_CONFIG.Naval_Invasions_Per_Turn.Unlimited_Naval_Invasions_Per_Turn && HasLaunchedNavalInvasion())
            // {
            //     return;
            // }

            Player MainPlayer = ExtraGameData.MainPlayer();

            if (MainPlayer == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CampaignPoliticsWindow.UpdateInfo]. Default behavior will be used.");
                return;
            }

            HasPerformedActionThisTurn = PlayerHasPerformedActionThisTurn();

            // Loop over each countries politics sections
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Player, CampaignPolitics_ElementUI> element in G.ui.PoliticsWindow.createdElements)
            {
                if (element.value == null)
                {
                    continue;
                }

                // Get naval invasion text
                GameObject navalInvasionButton = element.value.NavalInvasion.GetChildren()[0];
                TMP_Text navalInvasionText = navalInvasionButton.GetComponent<TMP_Text>();

                // If we aren't at war, set the color to grey
                if (!element.key.AtWarWith().Contains(MainPlayer) || HasPerformedActionThisTurn)
                {
                    navalInvasionText.color = new Color(0.7f, 0.7f, 0.7f, 1);
                    // element.value.NavalInvasion.Interactable(false);
                    continue;
                }

                // If we are at war, set it to interactable and color it white
                if (element.value.NavalInvasion != null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg("CampaignPoliticsWindow: " + element.key.Name(false));
                    element.value.NavalInvasion.Interactable(true);
                    navalInvasionText.color = new Color(1, 1, 1, 1);
                }
            }
        }

        [HarmonyPatch(nameof(CampaignPoliticsWindow.Show))]
        [HarmonyPostfix]
        internal static void Postfix_Show()
        {
            ForceNavalInvasionButtonsActive();
        }
    }
}
