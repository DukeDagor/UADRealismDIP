using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(PoliticsRelationshipElement))]
    internal class Patch_PoliticsRelationshipElement
    {
        [HarmonyPatch(nameof(PoliticsRelationshipElement.Init))]
        [HarmonyPostfix]
        internal static void Postfix_Init(PoliticsRelationshipElement __instance)
        { 
            bool rotateFlags = Config.Param("taf_politics_window_relation_matrix_rotate_flags", 0) == 1;

            if (!rotateFlags) return;

            __instance.Flag.gameObject.transform.eulerAngles = new Vector3(0f, 0f, 270f);

            __instance.TopText.gameObject.transform.eulerAngles = new Vector3(0f, 0f, 315f);
            __instance.BottomText.gameObject.transform.eulerAngles = new Vector3(0f, 0f, 315f);

            __instance.TopText.gameObject.transform.position -= new Vector3(5f, 0f, 0f);
            __instance.BottomText.gameObject.transform.position += new Vector3(5f, 0f, 0f);

            __instance.TopBar.gameObject.transform.position += new Vector3(0f, 14f, 0f);
            __instance.BottomBar.gameObject.transform.position -= new Vector3(0f, 14f, 0f);
        }

        [HarmonyPatch(nameof(PoliticsRelationshipElement.InitBar))]
        [HarmonyPostfix]
        internal static void Postfix_InitBar(PoliticsRelationshipElement __instance)
        {
        }
    }

    [HarmonyPatch(typeof(CampaignPolitics_ElementUI))]
    internal class Patch_CampaignPolitics_ElementUI
    {
        [HarmonyPatch(nameof(CampaignPolitics_ElementUI.Init))]
        [HarmonyPostfix]
        internal static void Postfix_Refresh(CampaignPolitics_ElementUI __instance)
        {
            float layoutSpacing = Config.Param("taf_politics_window_relation_matrix_spacing", 30);

            HorizontalLayoutGroup layout = __instance.gameObject.GetChild("Relations").GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.spacing = layoutSpacing;
        }
    }
}
