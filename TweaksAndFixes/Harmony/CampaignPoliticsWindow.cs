using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;
using Il2CppTMPro;
using Il2CppUiExt;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(CampaignPoliticsWindow))]
    internal class Patch_CampaignPoliticsWindow
    {
        public static bool HasPerformedAnyActionThisTurn = false;

        public static bool PlayerHasPerformedActionThisTurn()
        {
            return ActionsManager.lastInteractTurn == CampaignController.Instance.CurrentDate.turn;
        }

        public static void ForceNavalInvasionButtonsActive()
        {
            Player MainPlayer = ExtraGameData.MainPlayer();

            if (MainPlayer == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CampaignPoliticsWindow.UpdateInfo]. Default behavior will be used.");
                return;
            }

            HasPerformedAnyActionThisTurn = PlayerHasPerformedActionThisTurn();
            bool HasNotPerformedNavalInvasionThisTurn = HasPerformedAnyActionThisTurn && ActionsManager.ChoosenAction != ActionsManager.ActionType.NavalInvasion;

            if (!HasPerformedAnyActionThisTurn)
            {
                Patch_CampaignNavalInvasionPopupUi.LastConfirmedChoice = null;
                G.ui.NavalInvasionElement.choosenProvince = null;
            }

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
                if (!element.key.AtWarWith().Contains(MainPlayer) || HasNotPerformedNavalInvasionThisTurn)
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
            // Check params
            if (Config.Param("taf_naval_invasion_tweaks", 0) == 0)
            {
                return;
            }

            ForceNavalInvasionButtonsActive();
        }
    }
}
