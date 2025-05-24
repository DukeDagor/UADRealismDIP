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
        public static bool HasLaunchedNavalInvasionThisTurn = false;

        public static bool HasLaunchedNavalInvasion()
        {
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Player, CampaignPolitics_ElementUI> element in G.ui.PoliticsWindow.createdElements)
            {
                if (element.value == null)
                {
                    continue;
                }

                GameObject child = element.value.NavalInvasion.GetChildren()[0];
                TMP_Text text = child.GetComponent<TMP_Text>();

                if (text.color.r == 1.0 && text.color.g == 0.0 && text.color.b == 0.0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void ForceNavalInvasionButtonsActive()
        {
            HasLaunchedNavalInvasionThisTurn = HasLaunchedNavalInvasion();

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

            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Player, CampaignPolitics_ElementUI> element in G.ui.PoliticsWindow.createdElements)
            {
                if (element.value == null)
                {
                    continue;
                }

                GameObject child = element.value.NavalInvasion.GetChildren()[0];
                TMP_Text text = child.GetComponent<TMP_Text>();

                // if ((text.color.r == 1.0 && text.color.g == 0.0 && text.color.b == 0.0))
                // {
                //     continue;
                // }

                if (!element.key.AtWarWith().Contains(MainPlayer))
                {
                    text.color = new Color(0.7f, 0.7f, 0.7f, 1);
                    // element.value.NavalInvasion.Interactable(false);
                    continue;
                }

                if (element.value.NavalInvasion != null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg("CampaignPoliticsWindow: " + element.key.Name(false));
                    element.value.NavalInvasion.Interactable(true);
                    text.color = new Color(1, 1, 1, 1);
                }
            }
        }

        [HarmonyPatch(nameof(CampaignPoliticsWindow.UpdateInfo))]
        [HarmonyPostfix]
        internal static void Postfix_UpdateInfo()
        {
            ForceNavalInvasionButtonsActive();
        }
    }
}
