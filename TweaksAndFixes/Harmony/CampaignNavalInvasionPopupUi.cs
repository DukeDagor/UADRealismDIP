using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;
using UnityEngine.UI;
using Il2CppCoffee.UIExtensions;
using Il2CppTMPro;
using Il2CppUiExt;
using static TweaksAndFixes.Config;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(CampaignNavalInvasionPopupUi))]
    internal class Patch_CampaignNavalInvasionPopupUi
    {
        // Track the last "Naval Invasion" button owner we last hovered over (yes, I needed to make it that scuffed)
        public static Player NavalInvasionUiNation;

        public static Province LastConfirmedChoice = null;

        public static Il2CppSystem.Collections.Generic.Dictionary<int, string> TitleIndexes = new Il2CppSystem.Collections.Generic.Dictionary<int, string>();

        // Provinces that can be invaded by the player
        public static Il2CppSystem.Collections.Generic.List<Province> invadable = new Il2CppSystem.Collections.Generic.List<Province>();

        // Provinces are currently being invaded by the player
        public static Il2CppSystem.Collections.Generic.Dictionary<Province, CampaignConquestEvent> invading = new Il2CppSystem.Collections.Generic.Dictionary<Province, CampaignConquestEvent>();

        // Provinces that have insufficient tonnage nearby
        public static Il2CppSystem.Collections.Generic.List<Province> uninvadable = new Il2CppSystem.Collections.Generic.List<Province>();

        // Find all current naval invasions
        public static Il2CppSystem.Collections.Generic.Dictionary<Province, CampaignConquestEvent> GetProvinceListOfCurrentNavalInvasions(Player Attacker, Player Defender)
        {
            Il2CppSystem.Collections.Generic.Dictionary<Province, CampaignConquestEvent> provinces = new Il2CppSystem.Collections.Generic.Dictionary<Province, CampaignConquestEvent>();

            // Melon<TweaksAndFixes>.Logger.Msg("Provinces Under Attack:");

            // Get the list of provinces with naval invasions
            foreach (BaseCampaignSpecialEvent specialEvent in CampaignController.Instance.CampaignData.SpecialEvents)
            {
                if (specialEvent.EventType == BaseCampaignSpecialEvent.SpecialEventType.NavalInvasion)
                {
                    CampaignConquestEvent campaignConquestEvent = new CampaignConquestEvent(specialEvent.Pointer);

                    if (campaignConquestEvent.Attacker == Attacker.data && campaignConquestEvent.Defender == Defender.data)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg("  " + campaignConquestEvent.Id + " : " + campaignConquestEvent.Name + " -> " + campaignConquestEvent.EnemyProvince.Name + " (" + campaignConquestEvent.EnemyPort.Name + ")");

                        provinces.Add(campaignConquestEvent.EnemyProvince, campaignConquestEvent);
                    }
                }
            }
            return provinces;
        }

        // Find all valid naval invasion targets
        public static Il2CppSystem.Collections.Generic.List<Province> GetProvinceListForNavalInvasion(Player Attacker, Player Defender)
        {
            Il2CppSystem.Collections.Generic.List<Province> provinces = new Il2CppSystem.Collections.Generic.List<Province>();

            Il2CppSystem.Collections.Generic.List<Province> provincesUnderAttack = new Il2CppSystem.Collections.Generic.List<Province>();

            // Melon<TweaksAndFixes>.Logger.Msg("Provinces Under Attack:");

            // Get the list of provinces with naval invasions
            foreach (BaseCampaignSpecialEvent specialEvent in CampaignController.Instance.CampaignData.SpecialEvents)
            {
                if (specialEvent.EventType == BaseCampaignSpecialEvent.SpecialEventType.NavalInvasion)
                {
                    CampaignConquestEvent campaignConquestEvent = new CampaignConquestEvent(specialEvent.Pointer);

                    // Melon<TweaksAndFixes>.Logger.Msg("  " + campaignConquestEvent.Id + " : " + campaignConquestEvent.Name + " -> " + campaignConquestEvent.EnemyProvince.Name + " (" + campaignConquestEvent.EnemyPort.Name + ")");

                    provincesUnderAttack.Add(campaignConquestEvent.EnemyProvince);
                }
            }

            // Melon<TweaksAndFixes>.Logger.Msg("");
            float minimumAreaTonnage = Config.Param("taf_naval_invasion_minimum_area_tonnage", 25000);

            foreach (Area area in CampaignMap.Instance.Areas.Areas)
            {
                float areaTonnage = CampaignController.Instance.AreaCurrentTonnage(area, Attacker);

                // Check for minimum tonnage in the sea region
                if (areaTonnage >= minimumAreaTonnage)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg(area.Name + " : " + areaTonnage);

                    foreach (Province province in area.Provinces)
                    {
                        // Exclude provinces with no port
                        if (!province.HavePort) continue;

                        // Check if province is under attack
                        if (provincesUnderAttack.Contains(province)) continue;

                        // Check if the province is owned by the enemy
                        if (province.ControllerPlayer != Defender) continue;

                        // Check we are already invading them
                        if (G.ui.NavalInvasionElement.choosenProvince == province) continue;

                        // Sanity check to ensure we are still at war
                        if (province.ControllerPlayer.AtWarWith().Contains(Attacker))
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg("  " + area.Name + " -> " + province.Name);

                            provinces.Add(province);
                        }
                    }
                }
            }

            // Melon<TweaksAndFixes>.Logger.Msg("");

            return provinces;
        }

        // Find all invalid naval invasion targets
        public static Il2CppSystem.Collections.Generic.List<Province> GetListOfBlockedNavalInvasionProvinces(Player Attacker, Player Defender)
        {
            Il2CppSystem.Collections.Generic.List<Province> provinces = new Il2CppSystem.Collections.Generic.List<Province>();

            Il2CppSystem.Collections.Generic.List<Province> provincesUnderAttack = new Il2CppSystem.Collections.Generic.List<Province>();

            // Melon<TweaksAndFixes>.Logger.Msg("\nProvinces Under Attack:");

            // Get the list of provinces with naval invasions
            foreach (BaseCampaignSpecialEvent specialEvent in CampaignController.Instance.CampaignData.SpecialEvents)
            {
                if (specialEvent.EventType == BaseCampaignSpecialEvent.SpecialEventType.NavalInvasion)
                {
                    CampaignConquestEvent campaignConquestEvent = new CampaignConquestEvent(specialEvent.Pointer);

                    // Melon<TweaksAndFixes>.Logger.Msg("  " + campaignConquestEvent.Id + " : " + campaignConquestEvent.Name + " -> " + campaignConquestEvent.EnemyProvince.Name + " (" + campaignConquestEvent.EnemyPort.Name + ")");

                    provincesUnderAttack.Add(campaignConquestEvent.EnemyProvince);
                }
            }


            // Melon<TweaksAndFixes>.Logger.Msg("");
            float minimumAreaTonnage = Config.Param("taf_naval_invasion_minimum_area_tonnage", 25000);

            foreach (Area area in CampaignMap.Instance.Areas.Areas)
            {
                float areaTonnage = CampaignController.Instance.AreaCurrentTonnage(area, Attacker);

                // Check for FAILIER to meet minimum tonnage in the sea region
                if (areaTonnage < minimumAreaTonnage)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg(area.Name + " : " + areaTonnage);

                    foreach (Province province in area.Provinces)
                    {
                        // Exclude provinces with no port
                        if (!province.HavePort) continue;

                        // Check if province is under attack
                        if (provincesUnderAttack.Contains(province)) continue;

                        // Check if the province is owned by the enemy
                        if (province.ControllerPlayer != Defender) continue;

                        // Check we are already invading them
                        if (G.ui.NavalInvasionElement.choosenProvince == province) continue;

                        // Sanity check to ensure we are still at war
                        if (province.ControllerPlayer.AtWarWith().Contains(Attacker))
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg(area.Name + " -> " + province.Name);

                            provinces.Add(province);
                        }
                    }
                }
            }

            // Melon<TweaksAndFixes>.Logger.Msg("");

            return provinces;
        }

        public static void SetButtonUninteractable(Button button, TMP_Text provinceText, TMP_Text tonnageText, Color provinceColor, Color tonnageColor = default)
        {
            if (tonnageColor == default)
            {
                tonnageColor = provinceColor;
            }

            button.onClick.RemoveAllListeners();
            button.OnEnter(new System.Action(() =>
            {
                provinceText.color = provinceColor;
                tonnageText.color = tonnageColor;
            }));
            button.OnLeave(new System.Action(() =>
            {
                provinceText.color = provinceColor;
                tonnageText.color = tonnageColor;
            }));
            provinceText.color = provinceColor;
            tonnageText.color = tonnageColor;
        }

        [HarmonyPatch(nameof(CampaignNavalInvasionPopupUi.Init))]
        [HarmonyPrefix]
        internal static void Prefix_Init(CampaignNavalInvasionPopupUi __instance, ref Il2CppSystem.Collections.Generic.List<Province> provinces, Il2CppSystem.Action onConfirm)
        {
            // Check params
            if (Config.Param("taf_naval_invasion_tweaks", 0) == 0)
            {
                return;
            }

            // Sanity check
            if (NavalInvasionUiNation == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Failed to catch parent Ui in [CampaignNavalInvasionPopupUi.Prefix_Init]. Default behavior will be used");
                return;
            }

            Player MainPlayer = ExtraGameData.MainPlayer();

            // Sanity check
            if (MainPlayer == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CampaignNavalInvasionPopupUi.Prefix_Init]. Default behavior will be used.");
                return;
            }

            // Clear lists
            provinces.Clear();
            TitleIndexes.Clear();
            
            // Get valid provinces
            invadable = GetProvinceListForNavalInvasion(MainPlayer, NavalInvasionUiNation);

            // Get current invasions
            invading = GetProvinceListOfCurrentNavalInvasions(MainPlayer, NavalInvasionUiNation);

            // Get invalid provinces
            uninvadable = GetListOfBlockedNavalInvasionProvinces(MainPlayer, NavalInvasionUiNation);

            // Title placeholder
            Province TitleProvince = CampaignMap.Instance.Provinces.Provinces[0];
            
            // Get new invasions
            if (__instance.choosenProvince != null && __instance.choosenProvince.ControllerPlayer == NavalInvasionUiNation && !invading.ContainsKey(__instance.choosenProvince))
            {
                TitleIndexes.Add(0, LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ProvinceListCatagories_New"));
                provinces.Add(TitleProvince);
                provinces.Add(__instance.choosenProvince);
            }

            if (invading.Count > 0)
            {
                TitleIndexes.Add(provinces.Count, LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ProvinceListCatagories_Ongoing"));
                provinces.Add(TitleProvince);

                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Province, CampaignConquestEvent> province in invading)
                {
                    provinces.Add(province.Key);
                }
            }

            if (invadable.Count > 0)
            {
                TitleIndexes.Add(provinces.Count, LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ProvinceListCatagories_Options"));
                provinces.Add(TitleProvince);

                foreach (Province province in invadable)
                {
                    provinces.Add(province);
                }
            }

            if (uninvadable.Count > 0)
            {
                TitleIndexes.Add(provinces.Count, LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ProvinceListCatagories_Insufficient_Tonnage"));
                provinces.Add(TitleProvince);

                foreach (Province province in uninvadable)
                {
                    provinces.Add(province);
                }
            }

            // Melon<TweaksAndFixes>.Logger.Msg(ModUtils.DumpHierarchy(__instance.gameObject));

        }

        [HarmonyPatch(nameof(CampaignNavalInvasionPopupUi.Init))]
        [HarmonyPostfix]
        internal static void Postfix_Init(CampaignNavalInvasionPopupUi __instance, Il2CppSystem.Collections.Generic.List<Province> provinces, Il2CppSystem.Action onConfirm)
        {
            // Check params
            if (Config.Param("taf_naval_invasion_tweaks", 0) == 0)
            {
                return;
            }

            // Get the explanation text box and update the text based on the number of valid invasion targets
            GameObject DescObject = __instance.gameObject.Get("Window").Get("Text");
            TMP_Text DescText = DescObject.GetComponent<TMP_Text>();
            float minimumAreaTonnage = Config.Param("taf_naval_invasion_minimum_area_tonnage", 25000);

            if (__instance.choosenProvince != null)
            {
                DescText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ChangeNavalInvasionTarget"), __instance.choosenProvince.Name);
            }
            else if (invadable.Count > 0)
            {
                DescText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_InsufficientTonnage"), minimumAreaTonnage.ToString("N0"));
            }
            else
            {
                DescText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_SufficientTonnage"), minimumAreaTonnage.ToString("N0"));
            }

            // Set "No" to "Cancel" because it was bothering me
            if (__instance.choosenProvince == null)  __instance.No.gameObject.GetChildren()[0].GetComponent<TMP_Text>().text = LocalizeManager.Localize("$Ui_World_PopWindows_Cancel");

            // Melon<TweaksAndFixes>.Logger.Msg(ModUtils.DumpHierarchy(__instance.ProvinceTemplate));

            // Add a listener to the "Choose Province" button to know when to update the province list
            __instance.ChooseProvince.onClick.AddListener(new System.Action(() => {

                Player MainPlayer = ExtraGameData.MainPlayer();

                // Sanity check
                if (MainPlayer == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CampaignNavalInvasionPopupUi.Postfix_Init]. Default behavior will be used.");
                    return;
                }

                Il2CppSystem.Collections.Generic.Dictionary<string, Province> invadableNames = new Il2CppSystem.Collections.Generic.Dictionary<string, Province>();

                foreach (Province province in invadable)
                {
                    invadableNames.Add(province.Name, province);
                }

                Il2CppSystem.Collections.Generic.Dictionary<string, Province> invadingNames = new Il2CppSystem.Collections.Generic.Dictionary<string, Province>();

                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Province, CampaignConquestEvent> province in invading)
                {
                    invadingNames.Add(province.Key.Name, province.Key);
                }

                Il2CppSystem.Collections.Generic.Dictionary<string, Province> uninvadableNames = new Il2CppSystem.Collections.Generic.Dictionary<string, Province>();

                foreach (Province province in uninvadable)
                {
                    uninvadableNames.Add(province.Name, province);
                }

                // Add listener to all valid province options, disable all invalid targets
                for (int i = 0; i < __instance.provincesObjects.Count; i++)
                {
                    GameObject obj = __instance.provincesObjects[i];
                    TMP_Text tonnageTMPText = obj.GetChild("Tonnage").GetComponent<TMP_Text>();
                    TMP_Text provinceTMPText = obj.GetChild("Province").GetComponent<TMP_Text>();
                    string provinceText = provinceTMPText.text;
                    Button provinceButton = obj.GetComponent<Button>();

                    // Title text
                    if (TitleIndexes.ContainsKey(i))
                    {
                        SetButtonUninteractable(provinceButton, provinceTMPText, tonnageTMPText, Color.white);
                        provinceTMPText.text = TitleIndexes[i];
                        tonnageTMPText.text = "";
                        continue;
                    }

                    // Indent all non-title entries
                    provinceTMPText.text = "  " + provinceText;

                    if (invadingNames.ContainsKey(provinceText))
                    {
                        Color light_grey = new Color(0.8f, 1f, 0.8f);
                        tonnageTMPText.text = invading[invadingNames[provinceText]].CurrentTonnage + " t/" + invading[invadingNames[provinceText]].RequiredTonnage + " t";
                        SetButtonUninteractable(provinceButton, provinceTMPText, tonnageTMPText, light_grey, (invading[invadingNames[provinceText]].CurrentTonnage < invading[invadingNames[provinceText]].RequiredTonnage ? Color.red : light_grey));
                    }
                    else if (__instance.choosenProvince != null && provinceText == __instance.choosenProvince.Name)
                    {
                        Color green = new Color(0.3f, 0.9f, 0.3f);
                        SetButtonUninteractable(provinceButton, provinceTMPText, tonnageTMPText, green);
                    }
                    else if (uninvadableNames.ContainsKey(provinceText))
                    {
                        Color grey = new Color(0.5f, 0.5f, 0.5f);
                        SetButtonUninteractable(provinceButton, provinceTMPText, tonnageTMPText, grey);
                    }
                    else
                    {
                        Color light_grey = new Color(0.85f, 0.85f, 0.85f);
                        provinceButton.OnLeave(new System.Action(() =>
                        {
                            provinceTMPText.color = light_grey;
                            tonnageTMPText.color = light_grey;
                        }));
                        provinceTMPText.color = light_grey;
                        tonnageTMPText.color = light_grey;

                        obj.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
                        {
                            DescText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ConfirmInvasion"), minimumAreaTonnage.ToString("N0"), __instance.choosenProvince.Name);

                            __instance.Yes.gameObject.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
                            {
                                LastConfirmedChoice = __instance.choosenProvince;
                                Patch_CampaignPoliticsWindow.ForceNavalInvasionButtonsActive();
                            }));

                            __instance.No.gameObject.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
                            {
                                __instance.choosenProvince = null;

                                if (Patch_CampaignPoliticsWindow.PlayerHasPerformedActionThisTurn())
                                {
                                    __instance.choosenProvince = LastConfirmedChoice;
                                }

                                Patch_CampaignPoliticsWindow.ForceNavalInvasionButtonsActive();

                            }));
                        }));
                    }
                }

                // Melon<TweaksAndFixes>.Logger.Msg(ModUtils.DumpHierarchy(__instance.ChooseProvinceWindow));
            }));
        }
    }
}
