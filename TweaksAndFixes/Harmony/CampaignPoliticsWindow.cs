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

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(CampaignPoliticsWindow))]
    internal class Patch_CampaignPoliticsWindow
    {

        [HarmonyPatch(nameof(CampaignPoliticsWindow.UpdateInfo))]
        [HarmonyPostfix]
        internal static void Postfix_UpdateInfo(CampaignPoliticsWindow __instance)
        {
            // Melon<TweaksAndFixes>.Logger.Msg("CampaignPoliticsWindow.UpdateInfo");

            Player MainPlayer = ExtraGameData.MainPlayer();

            if (MainPlayer == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CampaignPoliticsWindow.UpdateInfo]. Default behavior will be used.");
                return;
            }

            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Player, CampaignPolitics_ElementUI> element in __instance.createdElements)
            {
                if (element.value == null)
                {
                    continue;
                }

                GameObject child = element.value.NavalInvasion.GetChildren()[0];
                TMP_Text text = child.GetComponent<TMP_Text>();

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
    }
}
