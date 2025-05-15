using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Reflection;
using Il2CppSystem.Runtime;
using UnityEngine.UI;
using TweaksAndFixes.Harmony;
using static MelonLoader.MelonLogger;
using Il2CppSystem.Linq;
using System.Xml.Linq;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignPolitics_ElementUI))]
    internal class Patch_CampaignPolitics_ElementUI
    {
        [HarmonyPatch(nameof(CampaignPolitics_ElementUI.Init))]
        [HarmonyPostfix]
        internal static void Postfix_Init(CampaignPolitics_ElementUI __instance)
        {
            __instance.NavalInvasion.OnEnter(new System.Action(() =>
            {
                // Dont ask why this is neccesary, because I've got no god-damn clue
                int max_depth = 100;
                int current_depth = 0;

                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Player, CampaignPolitics_ElementUI> entry in __instance.campaignPoliticsWindow.createdElements)
                {
                    if (current_depth >= max_depth) break;

                    // After matching the value, cache the key for use in the Naval Incasion Popup UI
                    if (entry.value == __instance)
                    {
                        Patch_CampaignNavalInvasionPopupUi.NavalInvasionUiNation = entry.key;

                        break;
                    }

                    current_depth++;
                }
            }));

            var rt = __instance.RelationsRoot.GetComponent<RectTransform>();
            MelonCoroutines.Start(FixAnchor(rt));
        }

        internal static System.Collections.IEnumerator FixAnchor(RectTransform rt)
        {
            // For some reason we have to wait 2 frames.
            // Presumably the anchor is getting reset after
            // Init, somewhere.
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (rt == null)
                yield break;
            rt.anchorMin = new Vector2(-0.03f, 1f);
        }
    }
}
