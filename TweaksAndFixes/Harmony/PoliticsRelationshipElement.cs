using HarmonyLib;
using UnityEngine;
using Il2Cpp;
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
    }

    [HarmonyPatch(typeof(CampaignPolitics_ElementUI))]
    internal class Patch_CampaignPolitics_ElementUI
    {
        [HarmonyPatch(nameof(CampaignPolitics_ElementUI.Init))]
        [HarmonyPostfix]
        internal static void Postfix_Refresh(CampaignPolitics_ElementUI __instance)
        {
            __instance.gameObject.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            float layoutSpacing = Config.Param("taf_politics_window_relation_matrix_spacing", 30);

            HorizontalLayoutGroup layout = __instance.gameObject.GetChild("Relations").GetComponent<HorizontalLayoutGroup>();
            if (layout == null) return;
            layout.spacing = layoutSpacing;

            LayoutElement element = __instance.gameObject.GetChild("Relations").GetComponent<LayoutElement>();
            element.preferredWidth = 400;
            element.flexibleWidth = -1;

            __instance.gameObject.GetChild("FlagAndName").GetComponent<LayoutElement>().preferredWidth = 200;

            __instance.gameObject.GetChild("GeneralInfo").GetComponent<LayoutElement>().preferredWidth = 270;

            __instance.gameObject.GetChild("Financial").GetComponent<LayoutElement>().preferredWidth = 310;

            __instance.gameObject.GetChild("Naval").GetComponent<LayoutElement>().preferredWidth = 250;

            __instance.gameObject.GetChild("Minor Allies").GetComponent<LayoutElement>().preferredWidth = 200;

            __instance.gameObject.GetChild("Minor Allies").GetComponent<LayoutElement>().flexibleWidth = -1;

            __instance.gameObject.GetChild("Actions").GetComponent<LayoutElement>().preferredWidth = 180;

            // FlagAndName: 200
            // GeneralInfo: 270
            // Financial: 310
            // Naval: 250
            // Relations: 400 | Flexable: -1
            // Actions: 150

        }

    }
}
