using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using UnityEngine.UI;
using Il2CppCoffee.UIExtensions;
using Il2CppTMPro;
using Il2CppUiExt;
using System.Xml.Linq;
using static TweaksAndFixes.Config;
using System.Net.Http.Headers;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(CampaignNavalInvasionPopupUi))]
    internal class Patch_CampaignNavalInvasionPopupUi
    {
        // Track the last "Naval Invasion" button owner we last hovered over (yes, I needed to make it that scuffed)
        public static Player NavalInvasionUiNation;

        // Change message based on the number of valid targets
        public static int ValidInvasionTargetCount = 0;

        public static Il2CppSystem.Collections.Generic.List<Province> NavalInvasionsThisTurn = new Il2CppSystem.Collections.Generic.List<Province>();

        public static Il2CppSystem.Collections.Generic.Dictionary<int, string> TitleIndexes = new Il2CppSystem.Collections.Generic.Dictionary<int, string>();

        // Find all current naval invasions
        public static Il2CppSystem.Collections.Generic.List<Province> GetProvinceListOfCurrentNavalInvasions(Player Attacker, Player Defender)
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

                    if (campaignConquestEvent.Attacker == Attacker.data && campaignConquestEvent.Defender == Defender.data)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg("  " + campaignConquestEvent.Id + " : " + campaignConquestEvent.Name + " -> " + campaignConquestEvent.EnemyProvince.Name + " (" + campaignConquestEvent.EnemyPort.Name + ")");

                        provinces.Add(campaignConquestEvent.EnemyProvince);
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

            foreach (Area area in CampaignMap.Instance.Areas.Areas)
            {
                float areaTonnage = CampaignController.Instance.AreaCurrentTonnage(area, Attacker);

                // Check for minimum tonnage in the sea region
                if (areaTonnage >= Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage)
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
                        if (NavalInvasionsThisTurn.Contains(province)) continue;

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

            foreach (Area area in CampaignMap.Instance.Areas.Areas)
            {
                float areaTonnage = CampaignController.Instance.AreaCurrentTonnage(area, Attacker);

                // Check for FAILIER to meet minimum tonnage in the sea region
                if (areaTonnage < Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg(area.Name + " : " + areaTonnage);

                    foreach (Province province in area.Provinces)
                    {
                        // Exclude provinces with no port
                        if (!province.HavePort) continue;

                        // Check if province is under attack
                        if (provincesUnderAttack.Contains(province)) continue;

                        // Check if the province is owned by the enemy
                        if (province.ControllerPlayer != Defender)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg("Skipped: " + area.Name + " -> " + province.Name);
                            continue;
                        }

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

        [HarmonyPatch(nameof(CampaignNavalInvasionPopupUi.Init))]
        [HarmonyPrefix]
        internal static void Prefix_Init(CampaignNavalInvasionPopupUi __instance, ref Il2CppSystem.Collections.Generic.List<Province> provinces, Il2CppSystem.Action onConfirm)
        {
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

            Province TitleProvince = CampaignMap.Instance.Provinces.Provinces[0];

            provinces.Clear();
            TitleIndexes.Clear();

            // Get new invasions
            if (NavalInvasionsThisTurn.Count > 0)
            {
                TitleIndexes.Add(0, "New Naval Invasions");
                provinces.Add(TitleProvince);
            }

            foreach (Province province in NavalInvasionsThisTurn)
            {
                if (province.ControllerPlayer != NavalInvasionUiNation) continue;
                provinces.Add(province);
            }

            if (provinces.Count == 1)
            {
                 provinces.Remove(TitleProvince);
                 TitleIndexes.Remove(0);
            }

            // Get current invasions
            Il2CppSystem.Collections.Generic.List<Province> invading = GetProvinceListOfCurrentNavalInvasions(MainPlayer, NavalInvasionUiNation);

            if (invading.Count > 0)
            {
                TitleIndexes.Add(provinces.Count, "Current Naval Invasions");
                provinces.Add(TitleProvince);
            }

            foreach (Province province in invading)
            {
                provinces.Add(province);
            }

            // Get valid provinces
            Il2CppSystem.Collections.Generic.List<Province> invadable = GetProvinceListForNavalInvasion(MainPlayer, NavalInvasionUiNation);

            if (invadable.Count > 0)
            {
                TitleIndexes.Add(provinces.Count, "Naval Invasion Options");
                provinces.Add(TitleProvince);
            }

            foreach (Province province in invadable)
            {
                provinces.Add(province);
            }

            // Track valid province count
            ValidInvasionTargetCount = provinces.Count;

            // Get invalid provinces and add them to the list
            Il2CppSystem.Collections.Generic.List<Province> uninvadable = GetListOfBlockedNavalInvasionProvinces(MainPlayer, NavalInvasionUiNation);

            if (uninvadable.Count > 0)
            {
                TitleIndexes.Add(provinces.Count, "Insufficient Tonnage");
                provinces.Add(TitleProvince);
            }

            foreach (Province province in uninvadable)
            {
                provinces.Add(province);
            }

            // Melon<TweaksAndFixes>.Logger.Msg(ModUtils.DumpHierarchy(__instance.gameObject));

        }

        [HarmonyPatch(nameof(CampaignNavalInvasionPopupUi.Init))]
        [HarmonyPostfix]
        internal static void Postfix_Init(CampaignNavalInvasionPopupUi __instance, Il2CppSystem.Collections.Generic.List<Province> provinces, Il2CppSystem.Action onConfirm)
        {
            // Get the explanation text box and update the text based on the number of valid invasion targets
            GameObject child = __instance.gameObject.Get("Window").Get("Text");
            TMP_Text text = child.GetComponent<TMP_Text>();

            bool DisableOptions = Patch_CampaignPoliticsWindow.HasLaunchedNavalInvasionThisTurn;

            if (DisableOptions)
            {
                text.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_WaitUntilNextTurn"), NavalInvasionsThisTurn[0].Name);
            }
            else if (ValidInvasionTargetCount == 0)
            {
                text.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_InsufficientTonnage"), Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage.ToString("N0"));
                // text.text = "In order to launch a naval invasion, you require at least [" + Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage.ToString("N0") + "] tonnes of naval assets in the sea region of the province you want to invade.";
            }
            else
            {
                text.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_SufficientTonnage"), Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage.ToString("N0"));
                // text.text = "You have naval assets over [" + Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage.ToString("N0") + "] tonnes in a sea region containing enemy ports. Ensure you have enough tonnage to secure the province you invade!";
            }

            // Set "No" to "Cancel" because it was bothering me
            __instance.No.gameObject.GetChildren()[0].GetComponent<TMP_Text>().text = "Cancel";

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

                Il2CppSystem.Collections.Generic.List<string> newInvasionNames = new Il2CppSystem.Collections.Generic.List<string>();

                foreach (Province province in NavalInvasionsThisTurn)
                {
                    newInvasionNames.Add(province.Name);
                }

                Il2CppSystem.Collections.Generic.List<string> invadingNames = new Il2CppSystem.Collections.Generic.List<string>();

                foreach (Province province in GetProvinceListOfCurrentNavalInvasions(MainPlayer, NavalInvasionUiNation))
                {
                    invadingNames.Add(province.Name);
                }

                Il2CppSystem.Collections.Generic.List<string> uninvadableNames = new Il2CppSystem.Collections.Generic.List<string>();

                foreach (Province province in GetListOfBlockedNavalInvasionProvinces(MainPlayer, NavalInvasionUiNation))
                {
                    uninvadableNames.Add(province.Name);
                }

                // Add listener to all valid province options, disable all invalid targets
                for (int i = 0; i < __instance.provincesObjects.Count; i++)
                {
                    GameObject obj = __instance.provincesObjects[i];
                    TMP_Text tonnageTMPText = obj.GetChild("Tonnage").GetComponent<TMP_Text>();
                    TMP_Text provinceTMPText = obj.GetChild("Province").GetComponent<TMP_Text>();
                    string provinceText = provinceTMPText.text;
                    Button provinceButton = obj.GetComponent<Button>();

                    if (TitleIndexes.ContainsKey(i))
                    {
                        provinceButton.onClick.RemoveAllListeners();
                        Color white = new Color(1f, 1f, 1f);
                        provinceButton.OnEnter(new System.Action(() =>
                        {
                            provinceTMPText.color = white;
                            tonnageTMPText.color = white;
                        }));
                        provinceButton.OnLeave(new System.Action(() =>
                        {
                            provinceTMPText.color = white;
                            tonnageTMPText.color = white;
                        }));
                        provinceTMPText.text = TitleIndexes[i];
                        provinceTMPText.color = white;
                        tonnageTMPText.color = white;
                        tonnageTMPText.text = "";
                        continue;
                    }

                    provinceTMPText.text = "  " + provinceText;

                    if (newInvasionNames.Contains(provinceText))
                    {
                        provinceButton.onClick.RemoveAllListeners();
                        Color green = new Color(0.3f, 0.9f, 0.3f);
                        provinceButton.OnEnter(new System.Action(() =>
                        {
                            provinceTMPText.color = green;
                            tonnageTMPText.color = green;
                        }));
                        provinceButton.OnLeave(new System.Action(() =>
                        {
                            provinceTMPText.color = green;
                            tonnageTMPText.color = green;
                        }));
                        provinceTMPText.color = green;
                        tonnageTMPText.color = green;
                    }
                    else if (invadingNames.Contains(provinceText))
                    {
                        // existingNavalInvasion.Interactable(false);
                        provinceButton.onClick.RemoveAllListeners();
                        Color blue = new Color(1f, 1f, 1f);
                        provinceButton.OnEnter(new System.Action(() =>
                        {
                            provinceTMPText.color = blue;
                            tonnageTMPText.color = blue;
                        }));
                        provinceButton.OnLeave(new System.Action(() =>
                        {
                            provinceTMPText.color = blue;
                            tonnageTMPText.color = blue;
                        }));
                        provinceTMPText.color = blue;
                        tonnageTMPText.color = blue;
                    }
                    else if (uninvadableNames.Contains(provinceText))
                    {
                        provinceButton.onClick.RemoveAllListeners();
                        Color grey = new Color(0.5f, 0.5f, 0.5f);
                        provinceButton.OnEnter(new System.Action(() => 
                        {
                            provinceTMPText.color = grey;
                            tonnageTMPText.color = grey;
                        }));
                        provinceButton.OnLeave(new System.Action(() => 
                        {
                            provinceTMPText.color = grey;
                            tonnageTMPText.color = grey;
                        }));
                        provinceTMPText.color = grey;
                        tonnageTMPText.color = grey;
                    }
                    else if (DisableOptions)
                    {
                        provinceButton.onClick.RemoveAllListeners();
                        Color grey = new Color(0.5f, 0.5f, 0.5f);
                        provinceButton.OnEnter(new System.Action(() =>
                        {
                            provinceTMPText.color = grey;
                            tonnageTMPText.color = grey;
                        }));
                        provinceButton.OnLeave(new System.Action(() =>
                        {
                            provinceTMPText.color = grey;
                            tonnageTMPText.color = grey;
                        }));
                        provinceTMPText.color = grey;
                        tonnageTMPText.color = grey;
                        continue;
                    }
                    else 
                    {
                        obj.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
                        {
                            text.text = String.Format(LocalizeManager.Localize("$TAF_Ui_NavalInvasion_ConfirmInvasion"), Config.USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage.ToString("N0"), __instance.choosenProvince.Name);
                            
                            // Set "No" to "Cancel" because it was bothering me
                            __instance.No.gameObject.GetChildren()[0].GetComponent<TMP_Text>().text = LocalizeManager.Localize("$Ui_World_PopWindows_Cancel");

                            __instance.Yes.gameObject.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
                            {
                                NavalInvasionsThisTurn.Add(__instance.choosenProvince);
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
