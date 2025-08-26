﻿using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using static TweaksAndFixes.ModUtils;
using Il2CppUiExt;
using System.Text;
using System.Xml.Linq;

#pragma warning disable CS8604
#pragma warning disable CS8625
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Ui))]
    internal class Patch_Ui
    {
        private static bool hasPrintedVersion = false;

        [HarmonyPatch(nameof(Ui.Start))]
        [HarmonyPostfix]
        internal static void Postfix_Start(Ui __instance)
        {
            UpdateVersionString(__instance);

            UiM.ApplyUiModifications();
        }


        // ########## UPDATE CUSTOM VERSION STRING ########## //

        [HarmonyPatch(nameof(Ui.RefreshVersion))]
        [HarmonyPostfix]
        internal static void Postfix_RefreshVersion(Ui __instance)
        {
            UpdateVersionString(__instance);
        }

        internal static void UpdateVersionString(Ui ui)
        {
            if (G.GameData == null || G.GameData.paramsRaw == null || G.GameData.parms == null)
                return;

            int mode = (int)(Config.Param("taf_versiontext", 0f) + 0.01f);
            if (mode == 0)
                return;

            var vt = ui.overlayUi.Get("Version", false, false).Get<Text>("VersionText", false, false);
            string? text = Config.ParamS("taf_versiontext", string.Empty);
            switch (mode)
            {
                case 1: text = vt.text + " " + text; break;
                case 2: text = GameData.GameVersion + " " + text; break;
                // default: entirely replace
                case 4: text = string.Empty; break;
            }
            vt.text = text;
            if (!hasPrintedVersion && text != null && text.Length > 0)
            {
                hasPrintedVersion = true;

                Melon<TweaksAndFixes>.Logger.Msg($"************************************************** Overriding Version");
                Melon<TweaksAndFixes>.Logger.Msg($"#{new String('-', text.Length + 2)}#");
                Melon<TweaksAndFixes>.Logger.Msg($"| {text} |");
                Melon<TweaksAndFixes>.Logger.Msg($"#{new String('-', text.Length + 2)}#");
            }
        }






        // ########## INITALIZE SPRITE DATABASE IF NEED-BE ########## //

        [HarmonyPatch(nameof(Ui.ChooseComponentType))]
        [HarmonyPrefix]
        internal static void Prefix_ChooseComponentType()
        {
            // SpriteDatabase.Instance.OverrideResources();
        }






        // ########## CHECK FOR WHITE PEACE ########## //

        [HarmonyPatch(nameof(Ui.CheckForPeace))]
        [HarmonyPrefix]
        internal static bool Prefix_CheckForPeace(Ui __instance)
        {
            if (!Config.PeaceCheckOverride)
                return true;

            UiM.CheckForPeace(__instance);
            return false;
        }






        // ########## CUSTOM DOCKYARD LOGIC ########## //

        // TODO: Mirror pickup and putdown for child parts
        // TODO: Fix special rotation ignoring 0* and 180* rotation

        // States
        internal static bool _InUpdateConstructor = false;
        internal static bool _InConstructor = false;
        public static bool NeedsConstructionListsClear = false;
        public static bool NeedsForcedUpdate = false;
        public static bool TryPairPartsAndMountsOnce = false;
        public static bool UpdateActiveShip = false;

        // Selected part & related data
        public static Part SelectedPart = null;
        // public static Il2CppSystem.Collections.Generic.Dictionary<Part, float> SelectedPartMountRotationData = new Il2CppSystem.Collections.Generic.Dictionary<Part, float>();
        public static Mount PartMount = null;
        public static PartCategoryData PartCategory;

        // Rotation tracking
        public static float PartRotation = 0.0f;
        public static float MountedPartRotation = 0.0f;
        public static float RotationValue = 45.0f;
        public static float DefaultRotation = 0.0f;

        public static float LastPartZ = 0.0f;
        public static float LastAutoRotateZ = 0.0f;
        public static bool IsInAutoRotateMargin = false;

        // Rotation restrictions
        public static bool FixedRotation = false;
        public static bool FixedRotationValue = false;
        public static bool UseDefaultMountRotation = true;
        public static bool UseSpecialDefaultMountRotation = true; // When default rotation != 0 or 180
        public static bool IgnoreSoftAutoRotate = false;
        public static bool Mounted = false; // Used for casemates and underwater torpedoes

        // Selected part type
        public static bool SideGun = false;
        public static bool Casemate = false;
        public static bool MainTower = false;
        public static bool SecTower = false;
        public static bool Funnel = false;
        public static bool Barbette = false;
        public static bool UnderwaterTorpedo = false;

        // New UI Popups
        public static bool DeleteShipNextTurn = false;
        public static Button.ButtonClickedEvent DeleteShipEvent = null;
        public static Button.ButtonClickedEvent AskConfirmDeleteShipEvent = null;

        // Preserve Pickup and Clone rotation
        private static Vector3 PickupPartPosition;
        private static float PickupPartRotation;
        private static float PickupPartMountRotation;
        private static Vector3 ClonePartPosition;
        private static float ClonePartRotation;
        private static float ClonePartMountRotation;
        public static Part PickupPart = null;
        public static Part ClonePart = null;
        public static bool PickedUpPart = false;
        public static bool ClonedPart = false;

        // ////////// General Use Functions ////////// //

        public static bool UseNewConstructionLogic()
        {
            if (Config.Param("taf_dockyard_new_logic", 1) != 1)
            {
                return false;
            }

            return _InUpdateConstructor;
        }

        public static void SetDestroyNextTurn()
        {
            DeleteShipNextTurn = true;
        }

        public static void AddConfirmPopupToButton(Button button, string text = default, System.Action before = null, System.Action after = null)
        {
            if (button.onClick.PrepareInvoke().Count == 1)
            {
                if (text == default)
                {
                    text = "Are you sure?";
                }
                else
                {
                    text = LocalizeManager.Localize(text);
                }

                var baseCall = button.onClick.PrepareInvoke()[0];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(new System.Action(() =>
                {
                    G.ui.ShowConfirmation(text,
                        new System.Action(() =>
                        {
                            if (before != null) before.Invoke();
                            baseCall.Invoke(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Object>(System.Array.Empty<Il2CppSystem.Object>()));
                            if (after != null) after.Invoke();
                        }),
                        new System.Action(() => { })
                    );
                }));
                button.onClick.AddListener(new System.Action(() => { }));
            }
        }

        public static void DisableKeyButton(Button button, System.Action before = null, System.Action after = null)
        {
            if (button.onClick.PrepareInvoke().Count == 1)
            {
                var baseCall = button.onClick.PrepareInvoke()[0];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(new System.Action(() =>
                {
                    if (!Input.anyKey && !Input.anyKeyDown)
                    {
                        before?.Invoke();
                        baseCall.Invoke(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Object>(System.Array.Empty<Il2CppSystem.Object>()));
                        after?.Invoke();
                    }
                }));
                button.onClick.AddListener(new System.Action(() => { }));
            }

            ModUtils.DestroyChild(button.GetChild("HkBox(Clone)", true));
        }

        public static void UpdateRotationIncrament()
        {
            if (!FixedRotationValue)
            {
                RotationValue += 15.0f;
                if (RotationValue - 0.1 >= 45.0f)
                {
                    RotationValue = 15.0f;
                }
                // Melon<TweaksAndFixes>.Logger.Msg("Rotation inc: " + RotationValue);
            }
        }

        public static void AutoOrient()
        {
            if (SelectedPart != null && !FixedRotation)
            {
                if (Mounted) MountedPartRotation = 0;
                else PartRotation = SelectedPart.transform.position.z > 0 ? 0 : 180;
                // Melon<TweaksAndFixes>.Logger.Msg("Auto rotate: " + SelectedPart.transform.eulerAngles.y);
            }
        }

        public static void UpdateTopBarRotationButton(Ui ui)
        {
            GameObject RotationButton = ui.conUpperButtons.GetChild("Layout").GetChild("TAF_Rotation_Button", true);

            if (RotationButton == null)
            {
                GameObject template = ui.conUpperButtons.GetChild("Layout").GetChild("Undo");
                RotationButton = GameObject.Instantiate(template);
                HorizontalLayoutGroup group = ui.conUpperButtons.GetChild("Layout").GetComponent<HorizontalLayoutGroup>();
                RotationButton.transform.SetParent(group.transform, false);//ui.conUpperButtons.GetChild("Layout"));
                RotationButton.transform.SetSiblingIndex(ui.conUpperButtons.GetChild("Layout").GetChildren().Count - 4);
                RotationButton.name = "TAF_Rotation_Button";
                // UiExt.SetTooltip(RotationButton.GetComponent<Button>(), "TAF_Rotation_Button_TT", new System.Func<string>(() => { return "Test"; }));
                Text text = RotationButton.GetChild("Text").GetComponent<Text>();
                Button button = RotationButton.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(new System.Action(() =>
                {
                    UpdateRotationIncrament();
                }));
                LayoutElement layout = RotationButton.GetComponent<LayoutElement>();
                layout.preferredWidth = 145;
            }

            // if (FixedRotationValue) RotationButton.GetComponent<Button>().SetActive(false);
            // else RotationButton.GetComponent<Button>().SetActive(true);
            RotationButton.GetChild("Text").GetComponent<Text>().text = String.Format(LocalizeManager.Localize("$TAF_Ui_Dockyard_TopBar_RotationIncrementControl"), RotationValue) + "\u00B0";
            if (FixedRotationValue) RotationButton.GetComponent<Button>().Interactable(false);
            else RotationButton.GetComponent<Button>().Interactable(true);
        }

        public static void UpdateTopBarRotationText(Ui ui)
        {
            GameObject RotationText = ui.conUpperButtons.GetChild("Layout").GetChild("TAF_Rotation_Text", true);

            if (RotationText == null)
            {
                GameObject template = ui.conUpperButtons.GetChild("Layout").GetChild("Undo");
                RotationText = GameObject.Instantiate(template);
                HorizontalLayoutGroup group = ui.conUpperButtons.GetChild("Layout").GetComponent<HorizontalLayoutGroup>();
                RotationText.transform.SetParent(group.transform, false);//ui.conUpperButtons.GetChild("Layout"));
                RotationText.transform.SetSiblingIndex(ui.conUpperButtons.GetChild("Layout").GetChildren().Count - 4);
                RotationText.name = "TAF_Rotation_Text";
                // UiExt.SetTooltip(RotationButton.GetComponent<Button>(), "TAF_Rotation_Button_TT", new System.Func<string>(() => { return "Test"; }));
                Button button = RotationText.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(new System.Action(() =>
                {
                    AutoOrient();
                }));
                LayoutElement layout = RotationText.GetComponent<LayoutElement>();
                layout.preferredWidth = 145;

                // Melon<TweaksAndFixes>.Logger.Msg("\n" + ModUtils.DumpHierarchy(ui.constructorUi));
            }

            string RotationValue;

            if (Mounted)
            {
                if (Il2CppSystem.Math.Sign(MountedPartRotation) == 1 || (int)(Il2CppSystem.Math.Abs(MountedPartRotation)) == 0)
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(MountedPartRotation)}\u00B0 + {(int)(DefaultRotation + 0.5f)}\u00B0";
                }
                else
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(MountedPartRotation + 360)}\u00B0 + {(int)DefaultRotation + 0.5f}\u00B0";
                }
            }
            else
            {
                if (Il2CppSystem.Math.Sign(PartRotation) == 1 || (int)(Il2CppSystem.Math.Abs(PartRotation)) == 0)
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(PartRotation)}\u00B0";
                }
                else
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(PartRotation + 360)}\u00B0";
                }
            }

            RotationText.GetChild("Text").GetComponent<Text>().text = String.Format(LocalizeManager.Localize("$TAF_Ui_Dockyard_TopBar_RotationValueControl"), RotationValue);
            
            
            if (SelectedPart == null || FixedRotationValue) RotationText.GetComponent<Button>().Interactable(false);
            else RotationText.GetComponent<Button>().Interactable(true);
        }

        public static void UpdateArmorQualityButton(Ui ui)
        {
            GameObject UpdateArmorQualityButton = ui.constructorUi.GetChild("Left").GetChild("Scroll View").GetChild("Viewport").GetChild("Cont").GetChild("FoldArmor").GetChild("Armor").GetChild("TAF_Armour_Quality_Button", true);

            float ArmourQuality = 0;

            if (Patch_Ship.LastCreatedShip != null)
            {
                foreach (TechnologyData tech in Patch_Ship.LastCreatedShip.techsActual)
                {
                    // if (tech.type != "armor_quality") continue;

                    if (tech.effects.ContainsKey("armor_str"))
                    {
                        string newStrength = tech.effects["armor_str"][0][0];

                        float parsed = 0;

                        if (!float.TryParse(newStrength, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {tech.name}: armor_str({tech.effects["armor_str"][0][0]}).");
                            continue;
                        }

                        ArmourQuality += parsed;
                    }
                }
            }

            if (UpdateArmorQualityButton == null)
            {
                GameObject template = ui.conUpperButtons.GetChild("Layout").GetChild("Undo");
                UpdateArmorQualityButton = GameObject.Instantiate(template);
                GameObject parent = ui.constructorUi.GetChild("Left").GetChild("Scroll View").GetChild("Viewport").GetChild("Cont").GetChild("FoldArmor").GetChild("Armor");
                // HorizontalLayoutGroup group = new HorizontalLayoutGroup();//ui.GetChild("Left").GetChild("Scroll View").GetChild("Viewport").GetChild("Cont").GetChild("FoldArmor").GetComponent<HorizontalLayoutGroup>();
                // group.SetParent(parent);
                UpdateArmorQualityButton.transform.SetParent(parent.transform, false);//ui.conUpperButtons.GetChild("Layout"));
                UpdateArmorQualityButton.transform.SetSiblingIndex(2);//ui.conUpperButtons.GetChild("Layout").GetChildren().Count - 2);
                UpdateArmorQualityButton.name = "TAF_Armour_Quality_Button";
                // UiExt.SetTooltip(RotationButton.GetComponent<Button>(), "TAF_Rotation_Button_TT", new System.Func<string>(() => { return "Test"; }));
                Button button = UpdateArmorQualityButton.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                LayoutElement layout = UpdateArmorQualityButton.GetComponent<LayoutElement>();
                layout.preferredWidth = 100;
                layout.preferredHeight = 15;
                
                UpdateArmorQualityButton.GetChild("Text").TryDestroyComponent<LocalizeText>();
                UpdateArmorQualityButton.GetChild("Text").GetComponent<Text>().text = LocalizeManager.Localize("$TAF_Ui_Dockyard_ArmorTab_UpdateArmorQualitySetting");

                // Melon<TweaksAndFixes>.Logger.Msg("\n" + ModUtils.DumpHierarchy(ui.constructorUi));
            }


            if (Patch_Ship.LastCreatedShip == null || (int)(G.settings.armorQualityInPen + 0.05f) == ArmourQuality)
            {
                UpdateArmorQualityButton.GetComponent<Button>().SetActive(false);
            }
            else
            {
                Button button = UpdateArmorQualityButton.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(new System.Action(() =>
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Updating armor preview value to {ArmourQuality}.");
                    // G.settings.armorQualityInPen = ArmourQuality;
                    GameObject OptionsMenu = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window");
                    // G.ui.SettingsMenu.Show();
                    // G.ui.SettingsMenu.SelectTab(G.ui.SettingsMenu.Settings[0]);
                    GameObject ArmorQualitySlider = ModUtils.GetChildAtPath("Root/RightSide/General/Viewport/Content/ArmorQualityInPenetrationData/ArmorQualityInPenetrationDataSlider", OptionsMenu);
                    Slider ArmorQualitySliderContent = ArmorQualitySlider.GetComponent<Slider>();
                    ArmorQualitySliderContent.value = ArmourQuality;
                    // ArmorQualitySliderContent.onValueChanged.Invoke(ArmourQuality);
                    G.settings.Save();
                }));
                button.SetActive(true);
            }

        }

        private static float conUpperRightBasePostion = -1;

        public static void UpdateShipTypeButtons(Ui ui)
        {
            // __instance.conUpperRight

            if (conUpperRightBasePostion == -1) conUpperRightBasePostion = ui.conUpperRight.transform.position.y;

            ui.conUpperRight.transform.position = new Vector3(ui.conUpperRight.transform.position.x, conUpperRightBasePostion * 0.875f, 0.0f);
            ui.conUpperButtons.GetChild("Layout").GetChild("CloneShip").SetActive(true);
            ui.conUpperButtons.GetChild("Layout").GetChild("CloneShip").UiVisible(true);

            ui.conUpperButtons.GetChild("Layout").GetChild("DeleteShip").SetActive(true);
            ui.conUpperButtons.GetChild("Layout").GetChild("DeleteShip").UiVisible(true);
            ui.conUpperButtons.GetChild("Layout").GetChild("DeleteShip").GetComponent<Button>().interactable = true;
        }

        private static void AddConfirmationPopups(Ui ui)
        {
            var TopBarChildren = ui.conUpperButtons.GetChild("Layout").GetChildren();

            // ui.conUpperButtons.GetChild("Layout").GetComponent<LayoutGroup>();

            foreach (GameObject child in TopBarChildren)
            {
                if (child == null) continue;
                if (child.name.Contains("SpaceEater")) child.transform.SetParent(null, false);
                if (child.name.Contains("Space")) continue;
                if (child.name == "Undo") continue;
                if (child.name.StartsWith("TAF")) continue;
                //Melon<TweaksAndFixes>.Logger.Msg("  " + child.name);
            
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    string key = "$TAF_Ui_Dockyard_Confirm_Action_" + child.name;

                    System.Action before = null;
                    System.Action after = null;

                    if (child.name == "Save")
                    {
                        after = new System.Action(() =>
                        {
                            if (Patch_Ship.LastCreatedShip.IsValid())
                            {
                                CampaignControllerM.RequestForcedGameSave = true;
                            }
                        });
                    }

                    AddConfirmPopupToButton(button, key, before, after);
                }
            }

            // ui.conUpperButtons.GetChild("Layout").GetChild("Save").GetComponent<Button>().onClick.AddListener(new System.Action(() =>
            // {
            //     CampaignControllerM.RequestForcedGameSave = true;
            // }));

            var ShipOptions = ui.conShipTabs.GetChild("Cont").GetChildren();
            foreach (GameObject child in ShipOptions)
            {
                if (child.name == "Ship(Clone)")
                {
                    Button button = child.GetChild("Button").GetComponent<Button>();
                    if (button != null)
                    {
                        DisableKeyButton(button);
                        ModUtils.DestroyChild(button.GetChild("CloneShip", true), false);
                        ModUtils.DestroyChild(button.GetChild("DeleteShip", true), false);
                    }
                }
            }

            var PartCatagories = ui.constructorUi.GetChild("PartCategories").GetChildren();
            foreach (GameObject child in PartCatagories)
            {
                if (child.name == "PartCategory(Clone)")
                {
                    Button button = child.GetChild("Button").GetComponent<Button>();
                    if (button != null)
                    {
                        DisableKeyButton(button);
                    }
                }
            }

            var Parts = ui.constructorUi.GetChild("Parts").GetChild("ScrollRect").GetChild("Viewport").GetChild("Content").GetChildren();
            foreach (GameObject child in Parts)
            {
                if (child.name == "Part(Clone)")
                {
                    Button button = child.GetChild("Button").GetComponent<Button>();
                    if (button != null)
                    {
                        DisableKeyButton(button);
                    }
                }
                else if (child.name == "Back")
                {
                    Button button = child.GetComponent<Button>();
                    if (button != null)
                    {
                        DisableKeyButton(button, null, new System.Action(() => {
                            if (Patch_Ship.LastCreatedShip.shipType.name != "bb" && Patch_Ship.LastCreatedShip.shipType.name != "bc" && Patch_Ship.LastCreatedShip.shipType.name != "ca")
                                child.SetActive(false);
                            if (Patch_Ship.LastCreatedShip.shipType.name == "ca" && Patch_Ship.LastCreatedShip.hull.data.maxAllowedCaliber != -1 && Patch_Ship.LastCreatedShip.hull.data.maxAllowedCaliber < 9)
                                child.SetActive(false);
                            if (PartCategory.name != "gun_main")
                                child.SetActive(false);
                        }));
                    }
                }
            }

            AddConfirmPopupToButton(ui.FleetWindow.Delete, "$TAF_Ui_FleetWindow_Confirm_Action_Delete");
            AddConfirmPopupToButton(ui.FleetWindow.Scrap, "$TAF_Ui_FleetWindow_Confirm_Action_Scrap");
        }

        public static void UpdateSelectedPart(Part part)
        {
            // Track the part selected from the toolbox
            if (SelectedPart == null || SelectedPart != part)
            {
                // SelectedPartMountRotationData.Clear();
                SelectedPart = part;
                // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + SelectedPart.Name() + " : " + SelectedPart.data.type + " : " + SelectedPart.data.name);
                // Melon<TweaksAndFixes>.Logger.Msg($"Selected part: {ModUtils.DumpHierarchy(part.gameObject)}");

                // Why be consistant when you can have no sensable typeing system?
                Casemate = SelectedPart.data.name.StartsWith("casemate");
                SideGun = SelectedPart.data.name.EndsWith("side");
                UnderwaterTorpedo = SelectedPart.data.name.EndsWith("x0");
                MainTower = SelectedPart.data.isTowerMain;
                SecTower = !SelectedPart.data.isTowerMain && SelectedPart.data.isTowerAny;
                Funnel = SelectedPart.data.isFunnel;
                Barbette = SelectedPart.data.isBarbette;

                // Funnels have a fixed rotation
                if (Funnel)
                {
                    PartRotation = 0;
                    RotationValue = 0;
                    FixedRotation = true;
                    FixedRotationValue = true;
                    IgnoreSoftAutoRotate = true;
                    UseDefaultMountRotation = false;
                    UseSpecialDefaultMountRotation = false;
                }

                // Main Towers and Secondary Towers can be roatated 180*
                else if (MainTower)
                {
                    PartRotation = 0;
                    RotationValue = 180;
                    FixedRotation = false;
                    FixedRotationValue = true;
                    IgnoreSoftAutoRotate = true;
                    UseDefaultMountRotation = false;
                    UseSpecialDefaultMountRotation = false;
                }
                else if (SecTower)
                {
                    PartRotation = 180;
                    RotationValue = 180;
                    FixedRotation = false;
                    FixedRotationValue = true;
                    IgnoreSoftAutoRotate = true;
                    UseDefaultMountRotation = false;
                    UseSpecialDefaultMountRotation = false;
                }

                // Casemates have default rotations that don't really need to be changed
                // The current rotation is reset to 0 so they use the default mount rotation instead
                else if (Casemate)
                {
                    PartRotation = 0;
                    RotationValue = 45;
                    FixedRotation = false;
                    FixedRotationValue = false;
                    IgnoreSoftAutoRotate = true;
                    UseDefaultMountRotation = true;
                    UseSpecialDefaultMountRotation = false;
                }

                // Underwater torpedoes are fixed
                else if (UnderwaterTorpedo)
                {
                    PartRotation = 0;
                    RotationValue = 0;
                    FixedRotation = true;
                    FixedRotationValue = true;
                    IgnoreSoftAutoRotate = true;
                    UseDefaultMountRotation = true;
                    UseSpecialDefaultMountRotation = false;
                }

                // Same as normal parts, just ignore special rotations
                else if (Barbette)
                {
                    RotationValue = 45;
                    FixedRotation = false;
                    FixedRotationValue = false;
                    IgnoreSoftAutoRotate = false;
                    UseDefaultMountRotation = false;
                    UseSpecialDefaultMountRotation = false;
                }

                // Everything else has free rotation
                else
                {
                    RotationValue = 45;
                    FixedRotation = false;
                    FixedRotationValue = false;
                    IgnoreSoftAutoRotate = false;
                    UseDefaultMountRotation = false;
                    UseSpecialDefaultMountRotation = true;
                }

                IsInAutoRotateMargin = false;
            }
        }

        [HarmonyPatch(nameof(Ui.Update))]
        [HarmonyPostfix]
        internal static void Postfix_Update(Ui __instance)
        {
            GameObject bugReporter = __instance.commonUi.GetChild("Options").GetChild("BugReport");

            bugReporter.SetActive(false);
            bugReporter.UiVisible(false);

            if (Config.Param("taf_add_confirmation_popups", 1) == 1)
            {
                AddConfirmationPopups(__instance);
            }

            UiM.UpdateModifications();
            Patch_GameManager.Update();
            CampaignControllerM.Update();

            // New UI elements
            if (Config.Param("taf_dockyard_new_logic", 1) == 1)
            {
                UpdateTopBarRotationButton(__instance);
                UpdateTopBarRotationText(__instance);
                UpdateArmorQualityButton(__instance);
                UpdateShipTypeButtons(__instance);
            }
            
            if (MountOverrideData.canary == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Reloading overrides after indirect cache clear...");
                // MountOverrideData.OverrideMountData();
                SpriteDatabase.Instance.OverrideResources();
                // FlagDatabase.Recreate();
                MountOverrideData.canary = new();
                Melon<TweaksAndFixes>.Logger.Msg($"Done!");
            }

            if (Patch_GameManager.CurrentSubGameState == Patch_GameManager.SubGameState.InConstructorViewMode && GameManager.Instance.isCampaign)
            {
                NeedsForcedUpdate = true;
                // G.ui.OnConShipChanged();
                G.ui.placedPartsWarn.Clear();
                G.ui.placedPartsBad.Clear();
                G.ui.constructorCentralText1.text = "";
                G.ui.constructorCentralText1Little.text = "";
                G.ui.constructorCentralText2.text = "";
            
                // foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                // {
                //     part.visualMode = Part.VisualMode.Normal;
                // }
            }

            // Debug stuff
            if (Input.GetKey(KeyCode.J))
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    Melon<TweaksAndFixes>.Logger.Msg("CACHED ASSETS: ");
                    foreach (var res in Util.resCache)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  {res.key}");
                    }
                }

                else if (Input.GetKeyDown(KeyCode.I))
                {
                    Melon<TweaksAndFixes>.Logger.Msg("PLAYER INFO: ");
                    foreach (var player in CampaignController.Instance.CampaignData.PlayersMajor)
                    {
                        if (player.isDisabled)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"{player.Name(false)}: Disabled!");
                            continue;
                        }

                        Melon<TweaksAndFixes>.Logger.Msg($"  {player.Name(false)}:\n{ModUtils.DumpPlayerData(player)}");
                    }
                }

                else if (Input.GetKeyDown(KeyCode.L))
                {
                    // string finalCount = "";

                    StringBuilder finalCount = new StringBuilder(2 ^ 24);

                    finalCount.Append("# Index,Enabled,Part ID,Mount Rotation,Position,Mount Position Type,Valid Part Types,Min Caliber,Max Caliber,Min Barrel #,Max Barrel #,Collision Checks,Left Fire Angle Override,Right Fire Angle Override,Orient Fire Arc?,Rotate Same?,# Comment 1,# Comment 2\n");
                    finalCount.Append("#,,,(0-360),\"(x: +Starboard/-Port, y: +Up/-Down, z: Fore/Aft)\",\"(any, center, side)\",See Comment,\"in, 0 = all\",\"in, 0 = all\",0 = all,0 = all,See Comment,See Comment,See Comment,fore/aft or starboard/port. No clue what it does.,1=true/0=false. No clue what it does.,Editing/Creating,\n");
                    finalCount.Append("@index,enabled,parent,rotation,position,mount_pos_type,accepts,caliber_min,caliber_max,barrels_min,barrels_max,collision,angle_left,angle_right,orientation,rotate_same,Editing,Do not change @index.\n");
                    finalCount.Append("default,1,,0,,any,barbette,0,0,0,0,check_all,0,0,,,Creating,\"For creating new mounts, set the index to -1.\"");

                    HashSet<string> parsedModels = new HashSet<string>();

                    Queue<GameObject> models = new Queue<GameObject>();

                    // TODO: (Duke) Change this to use TAFData/baseGamePartModelData.csv instead

                    string str = File.ReadAllText(Config._BasePath + "\\partmodels.txt");

                    var split = str.Split("\n");

                    foreach (string name in split)
                    {
                        var modelName = name.TrimEnd();

                        // Melon<TweaksAndFixes>.Logger.Msg($"Loading: {modelName}");
                        models.Enqueue(Util.ResourcesLoad<GameObject>(modelName, false));
                    }

                    Stack<Tuple<GameObject, int>> stack = new Stack<Tuple<GameObject, int>>();

                    while (models.Count > 0)
                    {
                        bool hasMounts = false;
                        // string partCount = $"\n# {data.Value.model}:";

                        GameObject model = models.Dequeue();

                        foreach (GameObject child in model.GetChildren())
                        {
                            stack.Push(new Tuple<GameObject, int>(child, 0));
                        }

                        bool isHull = model.name.Contains("_hull_") || model.name.StartsWith("hull_") || model.name.EndsWith("_hull");
                        bool isTower = model.name.Contains("_tower_") || model.name.StartsWith("tower_") || model.name.EndsWith("_tower");
                        bool isBarbette = model.name.Contains("_barbette_") || model.name.StartsWith("barbette_") || model.name.EndsWith("_barbette");

                        finalCount.Append($"\n# {model.name},,,,,,,,,,,,,,,,,");

                        List<string> path = new();

                        Melon<TweaksAndFixes>.Logger.Msg($"Parsing: {model.name}...");

                        Dictionary<string, int> depthToIndex = new Dictionary<string, int>();

                        while (stack.Count > 0)
                        {
                            var pair = stack.Pop();
                            GameObject obj = pair.Item1;
                            int depth = pair.Item2;

                            if (obj == null)
                            {
                                // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                                continue;
                            }

                            // Melon<TweaksAndFixes>.Logger.Msg($"  Sub-parsing {obj.name} : {obj.Pointer}...");

                            Il2CppSystem.Collections.Generic.List<GameObject> children;

                            try
                            {
                                children = obj.GetChildren();
                            }
                            catch
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"  Error while parsing {obj.name}...");
                                continue;
                            }

                            foreach (GameObject child in children)
                            {
                                stack.Push(new Tuple<GameObject, int>(child, depth + 1));
                            }

                            // Update path:
                            //   Remove path elements based on current depth
                            //   Add current obj to path
                            path.Clear();
                            GameObject head = obj.GetParent();
                            int stop = 10;

                            while (head != null)
                            {
                                path.Insert(0, head.name + "/");
                                head = head.GetParent();
                                if (stop-- == 0) break;
                            }

                            path[^1] = path[^1].Replace("/", "");

                            string concatPath = string.Concat(path);

                            if (!obj.name.StartsWith("Mount"))
                            {
                                if (isHull)
                                {
                                    GameObject parent = obj.GetParent();

                                    if (parent == null) continue;

                                    if (parent.name == "Sections" || parent.name == "Variation")
                                    {
                                        finalCount.Append($"\n# {concatPath},,,,,,,,,,,,,,,,,");
                                    }
                                }

                                continue;
                            }

                            if (!depthToIndex.ContainsKey(concatPath)) depthToIndex[concatPath] = 0;
                            depthToIndex[concatPath]++;

                            if (isBarbette)
                            {
                                if (!obj.name.StartsWith("Mount:barbette"))
                                {
                                    continue;
                                }
                            }
                            else if (isTower)
                            {
                                if (obj.name.StartsWith("Mount:tower_main") || obj.name.StartsWith("Mount:si_barbette"))
                                {
                                    continue;
                                }
                            }
                            else if (isHull)
                            {
                                if (obj.name.StartsWith("Mount:tower_main") || obj.name.StartsWith("Mount:tower_sec") || obj.name.StartsWith("Mount:funnel") || obj.name.StartsWith("Mount:si_barbette"))
                                {
                                    continue;
                                }
                            }

                            hasMounts = true;

                            // Melon<TweaksAndFixes>.Logger.Msg($"PATH: {string.Concat(path)} + #{count} : {obj.name}");

                            finalCount.Append(MountObjToCSV(depthToIndex[concatPath], concatPath, obj.transform.localEulerAngles.y, obj.transform.localPosition, obj));
                        }
                    }

                    Melon<TweaksAndFixes>.Logger.Msg($"Done!");

                    File.WriteAllText(Config._BasePath + "\\mounts.txt", finalCount.ToString());
                }

                else if (Input.GetKeyDown(KeyCode.P))
                {
                }

                else if (Input.GetKeyDown(KeyCode.M))
                {
                    if (SelectedPart != null)
                    {
                        GameObject parent = SelectedPart.mount.gameObject.GetParent().GetParent();

                        // Melon<TweaksAndFixes>.Logger.Msg(DumpHierarchy(parent));

                        Melon<TweaksAndFixes>.Logger.Msg($"\n{DumpSelectedPartData(SelectedPart)}");
                    }
                    else
                    {
                        Part part = __instance.FindPartUnderMouseCursor();

                        if (part != null)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"\n{DumpPartData(part)}");
                        }
                        else
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"{Patch_Ship.LastCreatedShip.Name(false, false)}\n{DumpHierarchy(Patch_Ship.LastCreatedShip.hull.gameObject.GetChildren()[0].GetChildren()[0].GetChild("Sections"))}");
                            Melon<TweaksAndFixes>.Logger.Msg($"\n{DumpHullData(Patch_Ship.LastCreatedShip)}");
                        }
                    }
                }

                //Melon<TweaksAndFixes>.Logger.Msg("\n\n\n" + ModUtils.DumpHierarchy(ui.conUpperRight));
                //Melon<TweaksAndFixes>.Logger.Msg("\n\n\n" + ModUtils.DumpHierarchy(ui.conShipTypeButtons));
                //Melon<TweaksAndFixes>.Logger.Msg("\n\n\n" + ModUtils.DumpHierarchy(ui.conComponentsChoice));
                //Melon<TweaksAndFixes>.Logger.Msg("\n\n\n" + ModUtils.DumpHierarchy(ui.conDetails));
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            {
                GameObject saveButton = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu/Window/SaveCampaign");

                Melon<TweaksAndFixes>.Logger.Msg($"{GameManager.Instance.CurrentState}");

                if (GameManager.Instance.CurrentState == GameManager.GameState.World)
                {
                    CampaignControllerM.RequestForcedGameSave = true;
                }
            }
        }

        [HarmonyPatch(nameof(Ui.ChoosePartCategory))]
        [HarmonyPostfix]
        internal static void Postfix_ChoosePartCategory(Ui __instance, PartCategoryData category)
        {
            PartCategory = category;
            // Melon<TweaksAndFixes>.Logger.Msg(Patch_Ship.LastCreatedShip.shipType.name);
        }

        [HarmonyPatch(nameof(Ui.RemoveChildForPart))]
        [HarmonyPrefix]
        internal static bool Prefix_RemoveChildForPart(Ui __instance, Ship ship, Part testPart)
        {
            // Melon<TweaksAndFixes>.Logger.Msg(ship.vesselName + " : " + testPart.name);

            List<Part> children = new List<Part>();

            foreach (var pair in ship.mountsUsed)
            {
                if (pair.Key == null || pair.Value == null || testPart == null || testPart.gameObject.GetChildren().Count == 0)
                {
                    continue;
                }

                if (pair.Key.parentPart == testPart || pair.Key.gameObject.GetParent() == testPart.gameObject.GetChildren()[0])
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Found Child: {pair.Value.name}");
                    children.Add(pair.Value);
                }
            }

            foreach (var child in children)
            {
                ship.RemovePart(child);
            }

            return false;
        }

        [HarmonyPatch(nameof(Ui.UpdateConstructor))]
        [HarmonyPrefix]
        internal static void Prefix_UpdateConstructor(Ui __instance)
        {
            _InConstructor = true;
            _InUpdateConstructor = true;

            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
            {
                Part part = __instance.FindPartUnderMouseCursor();

                // Why be consistant when you can have no sensable typeing system?
                bool isCasemate         = part != null && part.data.name.StartsWith("casemate");
                bool isSideGun          = part != null && part.data.name.EndsWith("side");
                bool isUnderwaterTorpedo= part != null && part.data.name.EndsWith("x0");
                bool isMainTower        = part != null && part.data.isTowerMain;
                bool isSecTower         = part != null && !part.data.isTowerMain && part.data.isTowerAny;
                bool isFunnel           = part != null && part.data.isFunnel;
                bool isBarbette         = part != null && part.data.isBarbette;
                bool normalPart         = !(isCasemate || isSideGun || isUnderwaterTorpedo || isMainTower || isSecTower || isFunnel || isBarbette);

                if (part != null && Input.GetMouseButtonUp(0))
                {
                    PickupPartPosition = part.transform.position;
                    PickupPartRotation = part.transform.rotation.eulerAngles.y;

                    if ((isCasemate || isUnderwaterTorpedo) && part.mount != null)
                    {
                        PickupPartRotation -= part.mount.transform.eulerAngles.y;
                    }
                    else if (normalPart && part.mount != null && part.mount.parentPart != null)
                    {
                        int MountDefaultRotation = (int)(Il2CppSystem.Math.Abs(part.mount.transform.eulerAngles.y) + 0.1);

                        if ((MountDefaultRotation != 0 && MountDefaultRotation != 180) || part.mount.parentPart.data.isTowerAny || part.mount.parentPart.data.isFunnel)
                        {
                            PickupPartRotation -= MountDefaultRotation;
                        }
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg($"Pickup or Place: {part.Name()} + {PickupPartPosition} | {PickupPartRotation}");

                    // MightHavePickedUpPart = true;
                    PickupPart = part;
                }

                if (part != null && Input.GetMouseButtonUp(2))
                {
                    ClonePartPosition = part.transform.position;
                    ClonePartRotation = part.transform.rotation.eulerAngles.y;

                    if ((isCasemate || isUnderwaterTorpedo) && part.mount != null)
                    {
                        ClonePartRotation -= part.mount.transform.eulerAngles.y;
                    }
                    else if (normalPart && part.mount != null && part.mount.parentPart != null)
                    {
                        int MountDefaultRotation = (int)(Il2CppSystem.Math.Abs(part.mount.transform.eulerAngles.y) + 0.1);

                        if ((MountDefaultRotation != 0 && MountDefaultRotation != 180) || part.mount.parentPart.data.isTowerAny || part.mount.parentPart.data.isFunnel)
                        {
                            ClonePartRotation -= MountDefaultRotation;
                        }
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg($"Clone: {part.Name()} + {ClonePartPosition} | {ClonePartRotation}");

                    // MightHavePickedUpPart = true;
                    ClonedPart = true;
                    ClonePart = part;
                }
            }

            Patch_Ui_c.Postfix_16(); // just in case we somehow died after running b15 and before b16
        }

        [HarmonyPatch(nameof(Ui.UpdateConstructor))]
        [HarmonyPostfix]
        internal static void Postfix_UpdateConstructor(Ui __instance)
        {
            if (NeedsConstructionListsClear)
            {
                //  Melon<TweaksAndFixes>.Logger.Msg("Clearing paired component lists...");
                Patch_Part.applyMirrorFromTo.Clear();
                Patch_Part.mirroredParts.Clear();
                Patch_Part.unmatchedParts.Clear();
                NeedsConstructionListsClear = false;
                TryPairPartsAndMountsOnce = true;
            }


            if (UpdateActiveShip)
            {
                Patch_Ship.LastCreatedShip = ShipM.GetActiveShip();

                if (Patch_Ship.LastCreatedShip != null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Active ship: {Patch_Ship.LastCreatedShip.Name(false, false)}");
                    UpdateActiveShip = false;
                }
            }

            if (Patch_Ship.LastCreatedShip == null)
            {
                NeedsForcedUpdate = true;
                _InUpdateConstructor = false;
                return;
            }

            if (UseNewConstructionLogic() && Config.Param("taf_dockyard_remove_mount_restrictions", 1) == 1)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"{Patch_Ship.LastCreatedShip.Name(false, false)}\n{DumpHierarchy(Patch_Ship.LastCreatedShip.hull.gameObject.GetChildren()[0].GetChildren()[0].GetChild("Sections"))}");

                Part hull = Patch_Ship.LastCreatedShip.hull;

                if (hull != null &&
                    hull.gameObject.GetChildren().Count > 0 &&
                    hull.gameObject.GetChildren()[0].GetChildren().Count > 0 &&
                    hull.gameObject.GetChildren()[0].GetChildren()[0].GetChildren().Count > 0)
                {
                    var sections = Patch_Ship.LastCreatedShip.hull.gameObject.GetChildren()[0].GetChildren()[0].GetChild("Sections").GetChildren();

                    foreach (var section in sections)
                    {
                        if (section.GetChild("TAF_ALLOW_ALL_MOUNTS", true) != null) continue;
                     
                        // Melon<TweaksAndFixes>.Logger.Msg($"  {section.name}");

                        GameObject mountObj = new();
                        mountObj.AddComponent<Mount>();
                        mountObj.transform.SetParent(section);
                        mountObj.transform.position = new Vector3(-10000, -10000, -10000);
                        mountObj.name = "TAF_ALLOW_ALL_MOUNTS";
                        Mount mount = mountObj.GetComponent<Mount>();

                        mount.towerMain = true;
                        mount.towerSec = true;
                        mount.funnel = true;
                        mount.siBarbette = true;
                        mount.barbette = true;
                        mount.casemate = false;
                        mount.subTorpedo = false;
                        mount.deckTorpedo = true;
                        mount.special = false;

                        mount.caliberMin = 0;
                        mount.caliberMax = 20;

                        mount.barrelsMin = 0;
                        mount.barrelsMax = 4;

                        Patch_Ship.LastCreatedShip.mounts.Add(mount);
                        Patch_Ship.LastCreatedShip.hull.mountsInside.Add(mount);
                        Patch_Ship.LastCreatedShip.allowedMountsInternal.Add(mount);
                    }

                    Patch_Ship.LastCreatedShip.RefreshMounts();
                }
            }

            // var a = CampaignController.Instance.CampaignData.VesselsByPlayer[ExtraGameData.MainPlayer().data];
            // a[^1].

            // ExtraGameData.MainPlayer().designs[^1]

            bool inViewMode = Patch_GameManager.CurrentSubGameState == Patch_GameManager.SubGameState.InConstructorViewMode;

            if (!inViewMode)
            {
                foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                {
                    if (part == null) continue;

                    // MountOverrideData.ApplyMountOverride(part);

                    if (part.gameObject.GetChildren().Count == 0) continue;

                    MountOverrideData.ApplyMountOverride(part, part.gameObject.GetChildren()[0], "", true);
                }

                Part hull = Patch_Ship.LastCreatedShip.hull;

                if (hull != null &&
                    hull.gameObject.GetChildren().Count > 0 &&
                    hull.gameObject.GetChildren()[0].GetChildren().Count > 0 &&
                    hull.gameObject.GetChildren()[0].GetChildren()[0].GetChildren().Count > 0)
                {
                    GameObject ship = Patch_Ship.LastCreatedShip.hull.gameObject.GetChildren()[0].GetChildren()[0].GetChildren()[0];

                    foreach (GameObject section in ship.GetChildren())
                    {
                        MountOverrideData.ApplyMountOverride(Patch_Ship.LastCreatedShip.hull, section, $"{Patch_Ship.LastCreatedShip.hull.GetChildren()[0].name.Replace("(Clone)", "")}/Visual/Sections/", true);
                    }
                }
            }

            if (!inViewMode && UseNewConstructionLogic() && Patch_Ship.LastCreatedShip.parts.Count > 0)
            {
                if (NeedsForcedUpdate)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Forced ship changed update");

                    G.ui.OnConShipChanged();
                    NeedsForcedUpdate = false;
                }

                if (PickedUpPart || ClonedPart)
                {
                    PartRotation = PickedUpPart ? PickupPartRotation : ClonePartRotation;
                    //SelectedPart.Place(PickupPartPosition);
                    PickedUpPart = false;
                    ClonedPart = false;
                }

                Patch_Part.TrySkipDestroy = null;
                Part toRemove = null;

                // foreach (Part part in G.ui.placedPartsWarn)
                // {
                //     if (part == null || !part.data.isGun) continue;
                //     Part.FireSectorInfo info = new Part.FireSectorInfo();
                //     part.CalcFireSectorNonAlloc(info);
                //     if (info.shootableAngleTotal < 90) continue;
                //     toRemove = part;
                // }
                // 
                // if (toRemove != null)
                // {
                //     G.ui.placedPartsWarn.Remove(toRemove);
                //     toRemove = null;
                // }
                // 
                // if (G.ui.placedPartsWarn.Count == 0)
                // {
                //     
                // }

                // Update mirrored pairs since mounts have a mind of their own
                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Part, Part> pair in Patch_Part.applyMirrorFromTo)
                {
                    if (!Patch_Ship.LastCreatedShip.parts.Contains(pair.Key) || !Patch_Ship.LastCreatedShip.parts.Contains(pair.Value))
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg("Error: Failed to find parts for mirroring:");
                        // Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Value.Name());
                        // Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Key.Name());
                        toRemove = pair.key;
                        // Patch_Part.applyMirrorFromTo.Remove(pair.Key);
                        continue;
                    }

                    Vector3 partRot = pair.Key.transform.eulerAngles;
                    pair.Value.transform.eulerAngles = new Vector3(partRot.x, -partRot.y, partRot.z);
                }

                if (toRemove != null)
                {
                    Patch_Part.applyMirrorFromTo.Remove(toRemove);
                }

                foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                {
                    if (part == SelectedPart) continue;

                    if (part.mount != null)
                    {
                        if (part.mount.transform.position.x < -99000)
                        {
                            part.mount = null;
                        }
                        else if (part.transform.position != part.mount.transform.position)
                        {
                            Vector3 partPos = part.transform.position;
                            Vector3 mountPos = part.mount.transform.position;

                            if (Math.Abs(partPos.x - mountPos.x) > 0.5f || Math.Abs(partPos.y - mountPos.y) > 0.5f || Math.Abs(partPos.z - mountPos.z) > 0.5f)
                            {
                                // Melon<TweaksAndFixes>.Logger.Msg($"Incorrect part snap, unparenting part {part.Name()} at {part.transform.position} from mount at {part.mount.transform.position}");
                                part.mount = null;
                                continue;
                            }

                            // Melon<TweaksAndFixes>.Logger.Msg($"Snapping {part.Name()} at {part.transform.position} to mount at {part.mount.transform.position}");
                            part.transform.position = part.mount.transform.position;
                        }
                    }
                    else
                    {
                        foreach (Mount mount in Patch_Ship.LastCreatedShip.mounts)
                        {
                            if (mount.employedPart != null) continue;

                            if (ModUtils.NearlyEqual(mount.transform.position, part.transform.position))
                            {
                                // Melon<TweaksAndFixes>.Logger.Msg($"Snapping {part.Name()} at {part.transform.position} to mount at {mount.transform.position}");
                                part.Mount(mount);
                                break;
                            }
                        }
                    }
                }

                // Loop over all current parts
                foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                {
                    if (part == null) continue;

                    if (part == SelectedPart) continue;

                    if ((int)part.transform.position.x == 0) continue;

                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + part.Name() + " : " + part.visualMode + " : " + part.transform + " : " + part.hasModel);

                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + part.gameObject.GetChildren()[0].GetChild("Visual").GetComponent<Renderer>().material.color.ToString());
                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + part.gameObject.GetComponent<Renderer>());

                    // Check if part is still mirrored
                    if (Patch_Part.mirroredParts.ContainsKey(part))
                    {
                        Part pair = Patch_Part.mirroredParts[part];
                        bool unpair = false;

                        if (!NearlyEqual(Il2CppSystem.Math.Abs(part.transform.position.x), Il2CppSystem.Math.Abs(pair.transform.position.x))) // Starbord/port
                        {
                            unpair = true;
                        }
                        else if (!NearlyEqual(part.transform.position.y, pair.transform.position.y)) // Up/down
                        {
                            unpair = true;
                        }
                        else if (!NearlyEqual(part.transform.position.z, pair.transform.position.z)) // Fore/aft
                        {
                            unpair = true;
                        }

                        if (unpair)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg("Unpairing: ");
                            // Melon<TweaksAndFixes>.Logger.Msg("  " + part.Name());
                            // Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Name());
                            Patch_Part.mirroredParts.Remove(part);
                            Patch_Part.mirroredParts.Remove(pair);
                            Patch_Part.unmatchedParts.Add(part);
                            Patch_Part.unmatchedParts.Add(pair);
                        }

                        continue;
                    }

                    // Add unmirrored parts to unmatched parts
                    if (!Patch_Part.unmatchedParts.Contains(part))
                    {
                        Patch_Part.unmatchedParts.Add(part);
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg("Check for new mirrors: ");

                    // Check for new mirrors
                    for (int i = Patch_Part.unmatchedParts.Count - 1; i >= 0; i--)
                    {
                        Part pair = Patch_Part.unmatchedParts[i];
                        bool found = true;

                        if (pair == part) continue;
                        if (!Patch_Ship.LastCreatedShip.parts.Contains(pair))
                        {
                            Patch_Part.unmatchedParts.Remove(pair);
                            continue;
                        }
                        if ((int)pair.transform.position.x == 0) continue;

                        if (!NearlyEqual(Il2CppSystem.Math.Abs(part.transform.position.x), Il2CppSystem.Math.Abs(pair.transform.position.x))) found = false;
                        else if (!NearlyEqual(part.transform.position.y, pair.transform.position.y)) found = false;
                        else if (!NearlyEqual(part.transform.position.z, pair.transform.position.z)) found = false;

                        if (found)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg("Pairing: ");
                            // Melon<TweaksAndFixes>.Logger.Msg("  " + part.Name());
                            // Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Name());
                            Patch_Part.mirroredParts.Add(pair, part);
                            Patch_Part.mirroredParts.Add(part, pair);
                            Patch_Part.unmatchedParts.Remove(part);
                            Patch_Part.unmatchedParts.Remove(pair);
                            break;
                        }
                    }
                }

                // Melon<TweaksAndFixes>.Logger.Msg("Check selected part:");

                if (SelectedPart != null)
                {
                    if (Input.GetKeyDown(KeyCode.G)) UpdateRotationIncrament();

                    if (!FixedRotation && Input.GetKeyDown(G.settings.Bindings.RotatePartLeft.Code))
                    {
                        if (Mounted) MountedPartRotation -= RotationValue;
                        else PartRotation -= RotationValue;
                        SelectedPart.AnimateRotate(-RotationValue);
                        // Melon<TweaksAndFixes>.Logger.Msg("Rotate: " + SelectedPart.transform.eulerAngles.y);
                    }
                    else if (!FixedRotation && Input.GetKeyDown(G.settings.Bindings.RotatePartRight.Code))
                    {
                        if (Mounted) MountedPartRotation += RotationValue;
                        else PartRotation += RotationValue;
                        SelectedPart.AnimateRotate(RotationValue);
                        // Melon<TweaksAndFixes>.Logger.Msg("Rotate: " + SelectedPart.transform.eulerAngles.y);
                    }
                    else if (!FixedRotation && Input.GetKeyDown(KeyCode.F))
                    {
                        AutoOrient();
                    }

                    PartRotation %= 360;
                    MountedPartRotation %= 360;

                    if (!IgnoreSoftAutoRotate && SelectedPart.transform.position.z < 9000 && SelectedPart.transform.position.z > -9000)
                    {
                        // Outside Margin
                        if (IsInAutoRotateMargin && Il2CppSystem.Math.Abs(LastAutoRotateZ - SelectedPart.transform.position.z) > 5)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg("Outside margin: " + Il2CppSystem.Math.Abs(LastAutoRotateZ - SelectedPart.transform.position.z));
                            IsInAutoRotateMargin = false;
                        }
                        // else if (IsInAutoRotateMargin)
                        // {
                        //     if (LastPartZ != SelectedPart.transform.position.z) Melon<TweaksAndFixes>.Logger.Msg("Inside margin: " + Il2CppSystem.Math.Abs(LastAutoRotateZ - SelectedPart.transform.position.z));
                        // }

                        // Soft Auto-Rotate
                        if (!IsInAutoRotateMargin && (Il2CppSystem.Math.Abs(SelectedPart.transform.position.z) <= 5 || Il2CppSystem.Math.Sign(SelectedPart.transform.position.z) != Il2CppSystem.Math.Sign(LastPartZ)) && ((SelectedPart.transform.position.z > 0 || Mounted) ? 0 : 180) != PartRotation)
                        {
                            IsInAutoRotateMargin = true;
                            LastAutoRotateZ = SelectedPart.transform.position.z;

                            if (!Input.GetKeyDown(KeyCode.LeftShift) && !Input.GetKeyDown(KeyCode.LeftControl))
                            {
                                PartRotation = (SelectedPart.transform.position.z > 0 || Mounted) ? 0 : 180;
                                // Melon<TweaksAndFixes>.Logger.Msg("Auto rotate: " + PartRotation);
                            }
                        }

                        // if (LastPartZ != SelectedPart.transform.position.z) Melon<TweaksAndFixes>.Logger.Msg("Update Pos: " + SelectedPart.transform.position.z);
                        LastPartZ = SelectedPart.transform.position.z;
                    }

                    if (IgnoreSoftAutoRotate)
                    {
                        LastPartZ = SelectedPart.transform.position.z;
                        IsInAutoRotateMargin = false;
                    }

                    if (UseDefaultMountRotation && SelectedPart.mount != null)
                    {
                        DefaultRotation = SelectedPart.mount.transform.rotation.eulerAngles.y;
                        Mounted = true;

                        if (SelectedPart.mount != PartMount)
                        {
                            PartMount = SelectedPart.mount;
                            MountedPartRotation = 0;
                        }
                    }
                    else if (UseSpecialDefaultMountRotation && SelectedPart.mount != null)
                    {
                        int MountDefaultRotation = (int)(Il2CppSystem.Math.Abs(SelectedPart.mount.transform.rotation.eulerAngles.y) + 0.1);

                        if (SelectedPart.mount.parentPart.data.isTowerAny || SelectedPart.mount.parentPart.data.isFunnel)
                        {
                            DefaultRotation = SelectedPart.mount.transform.rotation.eulerAngles.y;
                            Mounted = true;

                            if (SelectedPart.mount != PartMount)
                            {
                                PartMount = SelectedPart.mount;
                                MountedPartRotation = 0;
                            }
                        }
                        else
                        {
                            DefaultRotation = 0;
                            Mounted = false;
                        }
                    }
                    else
                    {
                        DefaultRotation = 0;
                        Mounted = false;
                    }

                    Vector3 CurrentRotation = SelectedPart.transform.eulerAngles;
                    CurrentRotation.y = (Mounted ? MountedPartRotation : PartRotation) + DefaultRotation;
                    SelectedPart.transform.eulerAngles = CurrentRotation;

                    if (Input.GetKey(KeyCode.LeftControl) && !SideGun && SelectedPart.mount == null)
                    {
                        if (G.ui.fireSectorObj != null) G.ui.fireSectorObj.transform.SetX(0);
                        SelectedPart.Place(new Vector3(0, SelectedPart.transform.position.y, SelectedPart.transform.position.z), false);

                        // int group = -1;

                        if (Patch_Ship.LastCreatedShip.mountsUsed != null)
                        {
                            // foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Mount, Part> pair in Patch_Ship.LastCreatedShip.mountsUsed)
                            // {
                            //     if (pair.key.parentPart != SelectedPart) continue;
                            // 
                            //     Melon<TweaksAndFixes>.Logger.Msg("Part: " + pair.value.Name() + " on mount: #" + pair.key.packNumber);
                            // 
                            //     group = pair.key.packNumber;
                            // 
                            //     break;
                            // 
                            //     // Melon<TweaksAndFixes>.Logger.Msg("Used part mount: " + pair.value.Name());
                            //     // pair.value.transform.position = pair.key.transform.position;
                            //     // pair.value.transform.rotation = pair.key.transform.rotation;
                            // }

                            foreach (Mount mount in Patch_Ship.LastCreatedShip.mounts)
                            {
                                if (mount.parentPart == SelectedPart && mount.employedPart != null)
                                {
                                    // if (!SelectedPartMountRotationData.ContainsKey(mount.employedPart))
                                    // {
                                    //     SelectedPartMountRotationData[mount.employedPart] = mount.employedPart.transform.rotation.eulerAngles.y;
                                    // }

                                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part mount: " + mount.transform.position.ToString());
                                    mount.employedPart.transform.SetX(0);

                                    // Vector3 MountCurrentRotation = mount.employedPart.transform.eulerAngles;
                                    // MountCurrentRotation.y = mount.transform.rotation.y + SelectedPartMountRotationData[mount.employedPart];
                                    // mount.employedPart.transform.eulerAngles = MountCurrentRotation;
                                }
                            }
                        }
                    }
                }
            }

            _InUpdateConstructor = false;
        }

        [HarmonyPatch(nameof(Ui.ExitConstructor))]
        [HarmonyPostfix]
        internal static void Postfix_ExitConstructor(Ui __instance, bool changeState = true, bool quickLoading = true)
        {
            _InConstructor = false;
        }



        // ########## SHIP PREVIEWS ########## //

        [HarmonyPatch(nameof(Ui.GetShipPreviewTexGeneric))]
        [HarmonyPrefix]
        internal static bool Prefix_GetShipPreviewTexGeneric(Ui __instance, Ship ship, Dictionary<Il2CppSystem.Guid, Texture2D> cache, GameObject camera, Camera cameraActual, bool placeDiagonal, ref Texture2D __result)
        {
            if (Config.Param("taf_ship_previews_disable", 0) == 1)
            {
                __result = new Texture2D(0, 0);
                return false;
            }

            return true;
        }



        // ########## PART PREVIEW CACHING ########## //

        public static Dictionary<string, Texture2D> PartPreviewCache = new Dictionary<string, Texture2D>();
        private static string LastPartPreviewGuid = "";

        public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            // if (texture2D.width == targetX && texture2D.height == targetY) { return texture2D; }
            RenderTexture rt = new RenderTexture(targetX, targetY, 16);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }

        private static string GetPartPreviewGuid(PartData part, Ship ship)
        {
            string guid = "";

            // guid += " : " + part.type;
            // guid += " : " + part.Id;
            // guid += " : " + ship.Name(false, false, false, false, true);
            // guid += " : " + ship.GetNameFull();

            // torpedo_x(tubenumber)_(size) -> (mark)

            // (name)_(country)_(class/s)

            if (part.type == "gun")
            {
                guid += part.name.Replace("_side", "");
                guid += " : " + ship.name.Split(" ")[0]; // Ship type
                guid += " : " + ship.name.Split(" ")[2]; // Ship country

                // guid += part.type == "gun" ? (" : " + ship.TechGunGrade(part)) : "";

                PartModelData key = null;
                string name = part.name.Replace("_side", "");
                string type = ship.name.Split(" ")[0];
                string country = ship.name.Split(" ")[2].Trim(']').TrimStart('[');
                // Melon<TweaksAndFixes>.Logger.Msg(type + " : " + country);
                ShipType typeData = G.GameData.shipTypes[type.ToLower()];
                PlayerData countryData = ship.player.data; // G.GameData.players[country];

                // string compareStr = part.name + "_" + country;

                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, PartModelData> partModelEntry in G.GameData.partModels)
                {
                    // if (partModelEntry.key == partModelEntry.value.name) continue; // Skip generics
                    if (partModelEntry.value.subName != name) continue;
                    if (partModelEntry.value.shipTypesx.Count > 0 && !partModelEntry.value.shipTypesx.Contains(typeData)) continue;
                    if (partModelEntry.value.countriesx.Count > 0 && !partModelEntry.value.countriesx.Contains(countryData)) continue;
                    if (partModelEntry.value.models[ship.TechGunGrade(part)].Length == 0) continue;

                    key = partModelEntry.value;

                    // Melon<TweaksAndFixes>.Logger.Msg("  Found: " + key.name + " : " + key.models[ship.TechGunGrade(part)]);
                }

                if (key == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error("Could not find PartModelData for [" + name + "]. Using backup ID.");
                }
                else
                {
                    guid = name + key.models[ship.TechGunGrade(part)];
                }
            }
            else if (part.type == "torpedo")
            {
                guid += part.name;
                // guid += " : " + ship.name.Split(" ")[0]; // Ship type
                // guid += " : " + ship.name.Split(" ")[2]; // Ship country

                int torpedoIndex = int.Parse(ship.components[G.GameData.compTypes["torpedo_size"]].name.Split("_")[^1]) + 15;

                guid = part.name + "_" + torpedoIndex;

                guid = G.GameData.partModels[guid].models[ship.TechTorpedoGrade(part)];

                // foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, CompType> component in G.GameData.compTypes)
                // {
                //     Melon<TweaksAndFixes>.Logger.Msg(component.key + " : " + component.value.nameUi);
                // }
            }
            else
            {
                guid += part.model;
            }

            return guid;
        }

        [HarmonyPatch(nameof(Ui.GetPartPreviewTex))]
        [HarmonyPrefix]
        internal static bool Prefix_GetPartPreviewTex(Ui __instance, PartData part, Ship ship, ref Texture2D __result)
        {
            if (Config.Param("taf_part_previews_enable_caching", 1) != 1)
            {
                return true;
            }

            LastPartPreviewGuid = GetPartPreviewGuid(part, ship);

            if (PartPreviewCache.ContainsKey(LastPartPreviewGuid))
            {
                try
                {
                    // Unfortunately the only real way to check if the preveiew was destroyed is to call a function.
                    PartPreviewCache[LastPartPreviewGuid].GetPixel(0, 0);
                }
                catch
                {
                    // Melon<TweaksAndFixes>.Logger.Msg("Part preview was deleted: " + LastPartPreviewGuid);

                    // If it errors out, then we assume it was deleted and regenerate the preview.
                    PartPreviewCache.Remove(LastPartPreviewGuid);
                    return true;
                }

                //Melon<TweaksAndFixes>.Logger.Msg("Use cashed part preview: " + LastPartPreviewGuid);
                // Melon<TweaksAndFixes>.Logger.Msg(PartPreviewCache[LastPartPreviewGuid].Pointer);
                // Melon<TweaksAndFixes>.Logger.Msg(PartPreviewCache[LastPartPreviewGuid].name);
                __result = PartPreviewCache[LastPartPreviewGuid]; // Resize(PartPreviewCache[LastPartPreviewGuid], 256, 256);

                return false;
            }

            return true;
        }
        
        [HarmonyPatch(nameof(Ui.GetPartPreviewTex))]
        [HarmonyPostfix]
        internal static void Postfix_GetPartPreviewTex(Ui __instance, PartData part, Ship ship, ref Texture2D __result)
        {
            if (Config.Param("taf_part_previews_enable_caching", 1) != 1)
            {
                return;
            }

            if (!PartPreviewCache.ContainsKey(LastPartPreviewGuid))
            {
                if (Config.Param("taf_part_previews_half_resolution", 1) == 1)
                {
                    __result = Resize(__result, __result.width / 2, __result.height / 2);
                }

                // Melon<TweaksAndFixes>.Logger.Msg("Cashed new part preview: " + LastPartPreviewGuid + " | " + downscale.height + " : " + downscale.width + " | Cashe Size: " + PartPreviewCache.Count);
                PartPreviewCache[LastPartPreviewGuid] = __result;
            }
        }





        // ########## FIND PART UNDER MOUSE FIX ########## //

        // [HarmonyPatch(nameof(Ui.FindPartUnderMouseCursor))]
        // [HarmonyPrefix]
        // internal static bool Prefix_FindPartUnderMouseCursor(Ui __instance, ref Part __result)
        // {
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit> hits;
        //     hits = Physics.RaycastAll(ray, 10000);
        //     // Melon<TweaksAndFixes>.Logger.Msg(Input.mousePosition.ToString() + " : " + G.cam.transform.TransformDirection(Vector3.forward).ToString());
        //     __result = null;
        //     foreach (RaycastHit hit in hits)
        //     {
        //         if (hit.collider == null)
        //         {
        //             continue;
        //         }
        // 
        //         Melon<TweaksAndFixes>.Logger.Msg(hit.collider.gameObject.GetParent().name + " : " + hit.collider.name);
        // 
        //         if (hit.collider == null || hit.collider.name != "DeckSize")
        //         {
        //             continue;
        //         }
        // 
        //         GameObject hitObj = hit.collider.gameObject.GetParent().GetParent();
        // 
        //         if (hitObj != null)
        //         {
        //             // Melon<TweaksAndFixes>.Logger.Msg(hitObj.name + ": " + hit.collider.name);
        // 
        //             foreach (Part part in Patch_Ship.LastCreatedShip.parts)
        //             {
        //                 // Melon<TweaksAndFixes>.Logger.Msg("  Check: " + part.gameObject.name);
        //                 if (part.gameObject == hitObj)
        //                 {
        //                     Melon<TweaksAndFixes>.Logger.Msg("  Hit: " + part.Name());
        //                     __result = part;
        //                 }
        //             }
        //             // Melon<TweaksAndFixes>.Logger.Msg("\n" + ModUtils.DumpHierarchy(Patch_Ship.LastCreatedShip.gameObject));
        //         }
        // 
        //         if (__result != null) break;
        //     }
        // 
        //     return false;
        // }






        // ########## UPGRADE MARK BUTTONS ########## //

        static List<Ship.TurretCaliber> _Turrets = new List<Ship.TurretCaliber>();
        static List<Ship.TurretCaliber> _Casemates = new List<Ship.TurretCaliber>();

        private static void ClearAllButtons(Ui ui)
        {
            if (ui == null || ui.gameObject == null)
                return;

            // Would be faster to drill down but this works.
            var objTCs = ui.gameObject.Get("TurretCalibers");
            if (objTCs != null)
                ClearButtons(objTCs);

            var objCase = ui.gameObject.Get("CasemateCalibers");
            if (objCase != null)
                ClearButtons(objCase);

            var objComps = FindArmamentsComponentList(ui);
            if (objComps != null)
                ClearButtons(objComps);
        }

        private static GameObject FindArmamentsComponentList(Ui ui)
        {
            string label = LocalizeManager.Localize("$comptypes_category_armament");
            var objComps = ui.gameObject.Get("Components");
            //Melon<TweaksAndFixes>.Logger.Msg($"Finding complist. Label {label}. Child count {objComps.transform.childCount}");

            for (int i = objComps.transform.childCount - 1; i-- > 0;)
            {
                var subTrf = objComps.transform.GetChild(i);
                //Melon<TweaksAndFixes>.Logger.Msg($"Object: {subTrf.gameObject.name}");
                if (!subTrf.gameObject.name.StartsWith("Header"))
                    continue;
                var text = subTrf.gameObject.GetComponentInChildren<Text>();
                if (text == null)
                    continue;

                //Melon<TweaksAndFixes>.Logger.Msg($"Found header text with {text.text}, compare to {label}");
                if (text.text != label)
                    continue;

                var nextObj = objComps.transform.GetChild(i + 1).gameObject;
                //Melon<TweaksAndFixes>.Logger.Msg($"Next object name is {nextObj.name}");
                if (nextObj.name.StartsWith("Components"))
                    return nextObj;
            }

            //if (GameManager.IsConstructor)
            //    Melon<TweaksAndFixes>.Logger.Error("Could not find Armaments components list!");
            return null;
        }

        private static void ClearButtons(GameObject parent)
        {
            for (int i = parent.transform.childCount; i-- > 0;)
            {
                var subTrf = parent.transform.GetChild(i);
                if (subTrf == null || subTrf.gameObject == null || subTrf.gameObject.name != "ResetGrade")
                    continue;

                GameObject.DestroyImmediate(subTrf.gameObject);
            }
        }

        private static void EnsureAllButtons(Ui ui)
        {
            if (!GameManager.IsConstructor)
                return;

            if (PlayerController.Instance == null)
                return;
            var ship = PlayerController.Instance.Ship;
            if (ship == null)
                return;

            var objTCs = ui.gameObject.Get("TurretCalibers");
            if (objTCs == null)
                return;

            var objCase = ui.gameObject.Get("CasemateCalibers");
            if (objCase == null)
                return;

            if (ship.shipGunCaliber == null)
                return;

            // In case we blew up last execution
            _Turrets.Clear();
            _Casemates.Clear();

            // Part out the TCs
            foreach (var tc in ship.shipGunCaliber)
            {
                if (tc.isCasemateGun)
                    _Casemates.Add(tc);
                else
                    _Turrets.Add(tc);
            }

            _Turrets.Sort((a, b) => b.turretPartData.GetCaliber().CompareTo(a.turretPartData.GetCaliber()));
            EnsureTCButtons(ship, objTCs, _Turrets);

            _Casemates.Sort((a, b) => b.turretPartData.GetCaliber().CompareTo(a.turretPartData.GetCaliber()));
            EnsureTCButtons(ship, objCase, _Casemates);

            _Turrets.Clear();
            _Casemates.Clear();


            EnsureTorpButton(ship, ui);
        }

        private static void EnsureTCButtons(Ship ship, GameObject parent, List<Ship.TurretCaliber> tcs)
        {
            int idx = tcs.Count - 1;
            for (int i = parent.transform.childCount; i-- > 0 && idx >= 0;)
            {
                var subTrf = parent.transform.GetChild(i);
                if (subTrf == null)
                    continue;

                var obj = subTrf.gameObject;
                if (obj == null || !obj.activeSelf)
                    continue;

                var tc = tcs[idx--];
                if (tc == null || tc.turretPartData == null)
                    continue;
                if (!ship.TAFData().IsGradeOverridden(tc.turretPartData))
                    continue;

                var button = AddTCButton(obj, i + 1);
                if (button == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Could not add button for tc for part {tc.turretPartData.name}!");
                    continue;
                }
                button.onClick.RemoveAllListeners();
                float caliber = tc.turretPartData.caliber;
                bool isCasemate = tc.isCasemateGun;
                
                button.onClick.AddListener(new System.Action(() =>
                {
                    ship.TAFData().ResetGunGrade(caliber, isCasemate);
                }));
            }
        }

        private static Button AddTCButton(GameObject obj, int idx)
        {
            var buttonOld = obj.transform.GetChild("Less");
            if (buttonOld == null)
                return null;
            var textOld = obj.transform.GetChild("TextCaliber");
            if (textOld == null)
                return null;

            var buttonNew = GameObject.Instantiate(buttonOld);
            buttonNew.transform.SetParent(obj.transform.parent.transform, true);
            buttonNew.transform.SetSiblingIndex(idx);
            var textNew = GameObject.Instantiate(textOld);
            var le = textNew.GetComponent<LayoutElement>();
            if (le != null)
                GameObject.Destroy(le);
            var image = buttonNew.GetChild("Image");
            if (image != null && image.gameObject != null)
                GameObject.Destroy(image.gameObject);
            textNew.transform.SetParent(buttonNew.transform, true);
            textNew.name = "Text";
            var text = textNew.GetComponent<Text>();
            text.text = LocalizeManager.Localize("$TAF_Ui_Constr_UpgradeMark");
            var trf = textNew.GetComponent<RectTransform>();
            trf.sizeDelta = new Vector2(150, 40);
            text.fontSize = 35;
            text.resizeTextMaxSize = 20;
            trf.anchoredPosition = new Vector2(114, -20);
            var button = buttonNew.GetComponent<Button>();
            button.interactable = true;

            button.gameObject.name = "ResetGrade";

            return button;
        }

        private static void EnsureTorpButton(Ship ship, Ui ui)
        {
            if (!ship.TAFData().IsTorpGradeOverridden())
                return;

            //Melon<TweaksAndFixes>.Logger.Msg("Adding torp upgrade button");
            var sName = ui.gameObject.Get("ShipNew");
            var buttonOld = sName == null ? null : sName.GetChild("Button", true);
            if (buttonOld == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find button to clone!");
                return;
            }

            var compList = FindArmamentsComponentList(ui);
            if (compList == null)
                return;

            var buttonNew = GameObject.Instantiate(buttonOld);
            buttonNew.transform.SetParent(compList.transform, true);
            buttonNew.name = "ResetGrade";
            var image = buttonNew.GetChild("Image");
            if (image != null && image.gameObject != null)
                GameObject.Destroy(image.gameObject);

            var le = buttonNew.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 75;
            le.preferredWidth = 53;
            var text = buttonNew.transform.GetChild("Text").GetComponent<Text>();
            text.text = text.text = LocalizeManager.Localize("$TAF_Ui_Constr_UpgradeTorpMark");
            text.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            text.resizeTextMinSize = text.resizeTextMaxSize = 10;
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            buttonNew.GetChild("Bg").transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var button = buttonNew.GetComponent<Button>();
            button.onClick.AddListener(new System.Action(() =>
            {
                ship.TAFData().ResetTorpGrade();
            }));

            buttonNew.SetActive(true);
        }

        [HarmonyPatch(nameof(Ui.ConstructorUI))]
        [HarmonyPostfix]
        internal static void Postfix_ConstructorUI(Ui __instance)
        {
            ClearAllButtons(__instance);
            EnsureAllButtons(__instance);
            AddConfirmationPopups(__instance);
        }

        [HarmonyPatch(nameof(Ui.RefreshConstructorInfo))]
        [HarmonyPrefix]
        internal static void Prefix_RefreshConstructorInfo(Ui __instance)
        {
            ClearAllButtons(__instance);
            // SpriteDatabase.Instance.OverrideResources();
        }

        [HarmonyPatch(nameof(Ui.RefreshConstructorInfo))]
        [HarmonyPostfix]
        internal static void Postfix_RefreshConstructorInfo(Ui __instance)
        {
            EnsureAllButtons(__instance);
        }






        // ########## FIX DESIGN USEAGE ########## //

        [HarmonyPatch(nameof(Ui.NewGameUI))]
        [HarmonyPostfix]
        internal static void Postfix_NewGameUI(Ui __instance)
        {
            if (!GameManager.IsNewGame)
                return;
            Patch_CampaignNewGame.FixDesignUsage(__instance.NewGameWindow);
        }
    }

    [HarmonyPatch(typeof(Ui.__c))]
    internal class Patch_Ui_c
    {
        // ########## MODIFIED BARBETTE LOGIC ########## //

        internal static bool _SetBackToBarbette = false;
        internal static PartData _BarbetteData = null;
        internal static bool _IsFirstCallofB15 = true;

        [HarmonyPatch(nameof(Ui.__c._UpdateConstructor_b__545_15))]
        [HarmonyPostfix]
        internal static void Postfix_15()
        {
            if (Patch_Ui._InUpdateConstructor && _IsFirstCallofB15 && Patch_Ship._GenerateShipState < 0 && G.ui.currentPart != null && G.ui.currentPart.isBarbette
                && G.ui.placingPart != null && !G.ui.placingPart.data.paramx.ContainsKey("center"))
            {
                _SetBackToBarbette = true;
                _BarbetteData = G.ui.currentPart;
                _BarbetteData.isBarbette = false;
                Patch_Part._IgnoreNextActiveBad = true;
            }
            _IsFirstCallofB15 = false;
        }

        [HarmonyPatch(nameof(Ui.__c._UpdateConstructor_b__545_16))]
        [HarmonyPostfix]
        internal static void Postfix_16()
        {
            _IsFirstCallofB15 = true;
            if (_SetBackToBarbette)
            {
                if (!Patch_Ui._InUpdateConstructor)
                    Melon<TweaksAndFixes>.Logger.Warning("Made it to end of UpdateConstructor with unrestored Barbette");

                _SetBackToBarbette = false;
                if (_BarbetteData != null)
                {
                    _BarbetteData.isBarbette = true;
                    _BarbetteData = null;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Cam))]
    internal class Patch_Cam
    {
        [HarmonyPatch(nameof(Cam.Update))]
        [HarmonyPostfix]
        internal static void Postfix_CameraController(Cam __instance)
        {
            // Only works for battles and shipbuilder, there's a custom script for the map
            if (Config.Param("taf_camera_disable_edge_scroll", 1) == 1)
            {
                __instance.borderScrollSize = 0;
                __instance.borderScrollSensitivity = 0;
            }

            // __instance.transform.position = Vector3.zero;

            // Only works for the map, no clue why the other two use a different system
            __instance.CampaignZoomSpeed = 30;
            __instance.BattleZoomSpeed = 1;
        }
    }
}
