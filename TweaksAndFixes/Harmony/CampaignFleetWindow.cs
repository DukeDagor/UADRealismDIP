using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine.UI;
using MelonLoader;

namespace TweaksAndFixes.Harmony
{
    // FleetSortBy
    [HarmonyPatch(typeof(CampaignFleetWindow))]
    internal class Patch_CampaignFleetWindow
    {
        private static HashSet<GameObject> OnClickVisited = new();

        // [HarmonyPatch(nameof(CampaignFleetWindow.Refresh))]
        // [HarmonyPrefix]
        // internal static bool Prefix_Refresh(CampaignFleetWindow __instance, bool isDesign)
        // {
        //     if (Input.GetKey(KeyCode.M))
        //     {
        //         if (isDesign)
        //         {
        //             __instance.Root.GetChild("Root").GetChild("Border").UiVisible(true);
        // 
        //             __instance.Root.GetChild("Root").GetChild("Shipbuilding Capacity Header").UiVisible(true);
        //             __instance.Root.GetChild("Root").GetChild("Design Header").SetActive(true);
        //             __instance.Root.GetChild("Root").GetChild("Design Ships").SetActive(true);
        //             __instance.Root.GetChild("Root").GetChild("Design Ship Info").SetActive(true);
        //             __instance.Root.GetChild("Root").GetChild("Design Buttons").SetActive(true);
        // 
        //             __instance.Root.GetChild("Root").GetChild("Fleet Header").SetActive(false);
        //             __instance.Root.GetChild("Root").GetChild("Fleet Ships").SetActive(false);
        //             __instance.Root.GetChild("Root").GetChild("Fleet Buttons").SetActive(false);
        //         }
        //         else
        //         {
        //             __instance.Root.GetChild("Root").GetChild("Border").SetActive(true);
        // 
        //             __instance.Root.GetChild("Root").GetChild("Shipbuilding Capacity Header").SetActive(false);
        //             __instance.Root.GetChild("Root").GetChild("Design Header").SetActive(false);
        //             __instance.Root.GetChild("Root").GetChild("Design Ships").SetActive(false);
        //             __instance.Root.GetChild("Root").GetChild("Design Ship Info").SetActive(false);
        //             __instance.Root.GetChild("Root").GetChild("Design Buttons").SetActive(false);
        // 
        //             __instance.Root.GetChild("Root").GetChild("Fleet Header").SetActive(true);
        //             __instance.Root.GetChild("Root").GetChild("Fleet Ships").SetActive(true);
        //             __instance.Root.GetChild("Root").GetChild("Fleet Buttons").SetActive(true);
        //         }
        // 
        //         return false;
        //     }
        // 
        //     return true;
        // }

        [HarmonyPatch(nameof(CampaignFleetWindow.Refresh))]
        [HarmonyPostfix]
        internal static void Postfix_Refresh(CampaignFleetWindow __instance)
        {
            foreach (var element in __instance.designUiByShip)
            {
                Ship s = element.Value.CurrentShip;

                if (s.isRefitDesign)
                {
                    element.Value.Year.text = $"{s.dateCreatedRefit.AsDate().Year}";
                }
                else
                {
                    element.Value.Year.text = $"{s.dateCreated.AsDate().Year}";
                }
            }

            if (G.ui.FleetWindow.selectedElements.Count == 0)
            {
                G.ui.FleetWindow.FleetButtonsRoot.GetChild("Set Crew").GetComponent<Button>().interactable = false;
                G.ui.FleetWindow.FleetButtonsRoot.GetChild("Set Role").GetComponent<Button>().interactable = false;
                G.ui.FleetWindow.FleetButtonsRoot.GetChild("View On Map").GetComponent<Button>().interactable = false;
            }

            foreach (var element in __instance.fleetUiByShip)
            {
                if (!UiM.HasModification(element.Value.gameObject))
                {
                    UiM.ModifyUi(element.Value.gameObject).SetOnUpdate(new System.Action<GameObject>((GameObject ui) => {

                        FleetWindow_ShipElementUI entry = ui.GetComponent<FleetWindow_ShipElementUI>();

                        TMP_Text roleText = entry.RoleSelectionButton.gameObject.GetParent().GetChild("RoleText").GetComponent<TMP_Text>();
                        TMP_Text trueRoleText = entry.RoleSelectionButton.gameObject.GetChildren()[0].GetComponent<TMP_Text>();

                        if (entry.Sold.text.Length > 1 && !roleText.text.Contains(entry.Sold.text))
                        {
                            roleText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_World_FleetDesign_SoldTo"), entry.Sold.text);
                            roleText.fontSizeMax = 8;
                        }
                        else if (entry.Sold.text.Length == 0 && roleText.text != trueRoleText.text)
                        {
                            roleText.text = trueRoleText.text;
                            roleText.fontSizeMax = 12;
                        }

                        if (entry.Area.gameObject.active) entry.Area.gameObject.SetActive(false);
                        if (entry.Port.gameObject.active) entry.Port.gameObject.SetActive(false);

                        if (entry.GetChild("Area").GetComponent<TMP_Text>().text != entry.Area.text) entry.GetChild("Area").GetComponent<TMP_Text>().text = entry.Area.text;
                        if (entry.GetChild("Port").GetComponent<TMP_Text>().text != entry.Port.text) entry.GetChild("Port").GetComponent<TMP_Text>().text = entry.Port.text;

                        if (entry.PortSelectionButton.IsActive())
                        {
                            if (entry.GetChild("Port").active) entry.GetChild("Port").SetActive(false);
                        }
                        else
                        {
                            if (!entry.GetChild("Port").active) entry.GetChild("Port").SetActive(true);
                        }
                    }));
                }

                GameObject fleetButtons = G.ui.FleetWindow.FleetButtonsRoot;
                
                if (!OnClickVisited.Contains(element.Value.gameObject))
                {
                    OnClickVisited.Add(element.Value.gameObject);

                    // Melon<TweaksAndFixes>.Logger.Msg($"Not visited: {element.Value.Name.text}");

                    element.Value.Btn.onClick.AddListener(new System.Action(() => {
                        
                        // Melon<TweaksAndFixes>.Logger.Msg($"Clicked: {G.ui.FleetWindow.selectedElements.Count}");
                        
                        int selectedCount = G.ui.FleetWindow.selectedElements.Count;

                        if (selectedCount == 0) return;

                        GameObject setCrewObj = G.ui.FleetWindow.FleetButtonsRoot.GetChild("Set Crew");
                        GameObject setRoleObj = G.ui.FleetWindow.FleetButtonsRoot.GetChild("Set Role");
                        GameObject viewOnMapObj = G.ui.FleetWindow.FleetButtonsRoot.GetChild("View On Map");

                        bool isUnavailible = false;
                        bool isBeingBuilt = false;
                        bool isOurs = true;

                        foreach (var selection in G.ui.FleetWindow.selectedElements)
                        {
                            isUnavailible |= selection.CurrentShip.IsInSea;
                            isBeingBuilt |=
                                selection.CurrentShip.isBuilding ||
                                selection.CurrentShip.isRefit ||
                                selection.CurrentShip.isCommissioning;
                            isOurs &= selection.Sold.text.Length == 0;
                        }

                        if (selectedCount == 0 || isUnavailible || !isOurs)
                        {
                            setCrewObj.GetComponent<Button>().interactable = false;
                        }
                        else
                        {
                            setCrewObj.GetComponent<Button>().interactable = true;
                        }

                        if (selectedCount > 1 || !isOurs || isBeingBuilt)
                        {
                            viewOnMapObj.GetComponent<Button>().interactable = false;
                        }
                        else
                        {
                            viewOnMapObj.GetComponent<Button>().interactable = true;
                        }

                        if (selectedCount < 0 || !isOurs)
                        {
                            setRoleObj.GetComponent<Button>().interactable = false;
                        }
                        else
                        {
                            setRoleObj.GetComponent<Button>().interactable = true;
                        }

                        if (!G.ui.FleetWindow.selectedElements.Contains(element.Value))
                        {
                            G.ui.FleetWindow.selectedElements.Add(element.Value);
                        }

                        // foreach (GameObject child in fleetButtons.GetChildren())
                        // {
                        //     if (child.GetComponent<Button>() == null) continue;
                        // 
                        //     if (!child.GetComponent<Button>().interactable)
                        //     {
                        //         child.SetActive(false);
                        //         child.UiVisible(false);
                        //         // Melon<TweaksAndFixes>.Logger.Msg($"    {child.name} is disabled!");
                        //     }
                        //     else
                        //     {
                        //         child.SetActive(true);
                        //         child.UiVisible(true);
                        //         // Melon<TweaksAndFixes>.Logger.Msg($"    {child.name} is enabled!");
                        //     }
                        // }
                    }));
                }
                
                // foreach (GameObject child in fleetButtons.GetChildren())
                // {
                //     if (child.GetComponent<Button>() == null) continue;
                // 
                //     if (!child.GetComponent<Button>().interactable)
                //     {
                //         child.SetActive(false);
                //         child.UiVisible(false);
                //         // Melon<TweaksAndFixes>.Logger.Msg($"    {child.name} is disabled!");
                //     }
                //     else
                //     {
                //         child.SetActive(true);
                //         child.UiVisible(true);
                //         // Melon<TweaksAndFixes>.Logger.Msg($"    {child.name} is enabled!");
                //     }
                // }
            }
        }
    }
}
