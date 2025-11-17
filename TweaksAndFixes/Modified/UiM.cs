using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2CppUiExt;

#pragma warning disable CS8600
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public class UiM
    {
        private class UIDataMinMax
        {
            private bool mod = false;
            private Vector2 min = Vector2.zero;
            private Vector2 minOriginal = Vector2.zero;
            private Vector2 max = Vector2.zero;
            private Vector2 maxOriginal = Vector2.zero;

            private UiModification _this;

            public UiModification ReplaceAnchors(Vector2 anchorMin, Vector2 anchorMax)
            {
                mod = true;
                this.min = anchorMin;
                this.max = anchorMax;

                return _this;
            }

            public UiModification ReplaceAnchorMin(Vector2 anchorMin)
            {
                mod = true;
                this.min = anchorMin;

                return _this;
            }

            public UiModification ReplaceAnchorMax(Vector2 anchorMax)
            {
                mod = true;
                this.max = anchorMax;

                return _this;
            }

            public UiModification ResetAnchors()
            {
                mod = false;
                this.min = minOriginal;
                this.max = maxOriginal;

                return _this;
            }
        }

        public class UiModification
        {
            public RectTransform rectTransform;
            public LayoutElement layoutElement;

            public UiModification(GameObject ui)
            {
                enabledOriginal = ui.active;

                if (ui.TryGetComponent(out rectTransform))
                {
                    anchorMinOriginal = rectTransform.anchorMin;
                    anchorMaxOriginal = rectTransform.anchorMax;

                    offsetMinOriginal = rectTransform.offsetMin;
                    offsetMaxOriginal = rectTransform.offsetMax;

                    anchoredPositionOriginal = rectTransform.anchoredPosition;
                }

                if (ui.TryGetComponent(out layoutElement))
                {
                    layoutWidthOriginal = layoutElement.preferredWidth;
                    layoutHeightOriginal = layoutElement.preferredHeight;
                }
            }

            public void Apply(GameObject ui, bool forced, bool debug = false)
            {
                if (hasOnUpdate)
                {
                    onUpdate.Invoke(ui);
                }

                if (!forced) return;

                if (modActive)
                {
                    ui.SetActive(enabled);
                    ui.UiVisible(visible);
                }

                if (modChildOrder)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Mod Order");
                    foreach (string childName in childOrder)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"  {childName}");

                        GameObject child = ui.GetChild(childName, true);

                        if (child == null)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Could not find `{childName}` in `{ui.name}`!");
                            continue;
                        }

                        child.transform.SetSiblingIndex(childOrder.IndexOf(childName));
                    }
                }

                if ((modAnchor || modOffset) && ui.TryGetComponent(out rectTransform))
                {
                    if (debug) Melon<TweaksAndFixes>.Logger.Msg($"  RectTransform:");

                    if (modAnchor)
                    {
                        if (debug) Melon<TweaksAndFixes>.Logger.Msg($"    Anchor: {offsetMin} x {offsetMax}");

                        if (!ModUtils.NearlyEqual(rectTransform.anchorMin, anchorMin)) rectTransform.anchorMin = anchorMin;
                        if (!ModUtils.NearlyEqual(rectTransform.anchorMax, anchorMax)) rectTransform.anchorMax = anchorMax;
                    }

                    if (modOffset)
                    {
                        if (debug) Melon<TweaksAndFixes>.Logger.Msg($"    Offset: {offsetMin} x {offsetMax}");

                        if (!ModUtils.NearlyEqual(rectTransform.offsetMin, offsetMin)) rectTransform.offsetMin = offsetMin;
                        if (!ModUtils.NearlyEqual(rectTransform.offsetMax, offsetMax)) rectTransform.offsetMax = offsetMax;
                    }

                    if (modAnchoredPosition)
                    {
                        if (debug) Melon<TweaksAndFixes>.Logger.Msg($"    AnchoredPosition: {offsetMin} x {offsetMax}");

                        if (!ModUtils.NearlyEqual(rectTransform.anchoredPosition, anchoredPosition)) rectTransform.anchoredPosition = anchoredPosition;
                    }
                }

                if (modLayoutDimensions && ui.TryGetComponent(out layoutElement))
                {
                    if (debug) Melon<TweaksAndFixes>.Logger.Msg($"  Layout:");
                    if (modLayoutDimensions)
                    {
                        if (debug) Melon<TweaksAndFixes>.Logger.Msg($"    Dimensions: {layoutWidth} x {layoutHeight}");

                        if (!ModUtils.NearlyEqual(layoutElement.preferredWidth, layoutWidth)) layoutElement.preferredWidth = layoutWidth;
                        // if (!ModUtils.NearlyEqual(layoutElement.minWidth, layoutWidth)) layoutElement.minWidth = layoutWidth;
                        // if (!ModUtils.NearlyEqual(layoutElement.flexibleWidth, layoutWidth)) layoutElement.flexibleWidth = layoutWidth;
                        if (!ModUtils.NearlyEqual(layoutElement.preferredHeight, layoutHeight)) layoutElement.preferredHeight = layoutHeight;
                        // if (!ModUtils.NearlyEqual(layoutElement.minWidth, layoutHeight)) layoutElement.minWidth = layoutHeight;
                        // if (!ModUtils.NearlyEqual(layoutElement.flexibleWidth, layoutHeight)) layoutElement.flexibleWidth = layoutHeight;
                    }
                }
            }

            private bool modActive = false;
            private bool visible = true;
            private bool visibleOriginal = true;
            private bool enabled = true;
            private bool enabledOriginal = true;

            public UiModification SetActive(bool visible, bool enabled)
            {
                modActive = true;
                this.visible = visible;
                this.enabled = enabled;

                return this;
            }

            public UiModification SetVisible(bool visible)
            {
                modActive = true;
                this.visible = visible;

                return this;
            }

            public UiModification SetEnabled(bool enabled)
            {
                modActive = true;
                this.enabled = enabled;

                return this;
            }

            public UiModification ResetActive()
            {
                modActive = false;
                this.visible = visibleOriginal;
                this.enabled = enabledOriginal;

                return this;
            }


            private bool modChildOrder = false;
            private List<string> childOrder = new List<string>();
            private List<string> childOrderOriginal = new List<string>();

            public UiModification SetChildOrder(params string[] children)
            {
                modChildOrder = true;

                this.childOrder.Clear();

                foreach (string child in children)
                {
                    this.childOrder.Add(child);
                }

                return this;
            }

            public UiModification ResetChildOrder()
            {
                modChildOrder = false;

                this.childOrder.Clear();

                foreach (string child in this.childOrderOriginal)
                {
                    this.childOrder.Add(child);
                }

                return this;
            }


            private bool modAnchor = false;
            private Vector2 anchorMin = Vector2.zero;
            private Vector2 anchorMinOriginal = Vector2.zero;
            private Vector2 anchorMax = Vector2.zero;
            private Vector2 anchorMaxOriginal = Vector2.zero;

            public UiModification ReplaceAnchors(Vector2 anchorMin, Vector2 anchorMax)
            {
                modAnchor = true;
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;

                return this;
            }

            public UiModification ReplaceAnchorMin(Vector2 anchorMin)
            {
                modAnchor = true;
                this.anchorMin = anchorMin;

                return this;
            }

            public UiModification ReplaceAnchorMax(Vector2 anchorMax)
            {
                modAnchor = true;
                this.anchorMax = anchorMax;

                return this;
            }

            public UiModification ResetAnchors()
            {
                modAnchor = false;
                this.anchorMin = anchorMinOriginal;
                this.anchorMax = anchorMaxOriginal;

                return this;
            }


            private UIDataMinMax modOffsetData = new UIDataMinMax();
            private bool modOffset = false;
            private Vector2 offsetMin = Vector2.zero;
            private Vector2 offsetMinOriginal = Vector2.zero;
            private Vector2 offsetMax = Vector2.zero;
            private Vector2 offsetMaxOriginal = Vector2.zero;

            public UiModification ReplaceOffsets(Vector2 offsetMin, Vector2 offsetMax)
            {
                modOffset = true;
                this.offsetMin = offsetMin;
                this.offsetMax = offsetMax;

                return this;
            }

            public UiModification ReplaceOffsetMin(Vector2 offsetMin)
            {
                modOffset = true;
                this.offsetMin = offsetMin;
                this.offsetMax = this.offsetMaxOriginal;

                return this;
            }

            public UiModification ReplaceOffsetMax(Vector2 offsetMax)
            {
                modOffset = true;
                this.offsetMax = offsetMax;
                this.offsetMin = this.offsetMinOriginal;

                return this;
            }

            public UiModification ResetOffsets()
            {
                modOffset = false;
                this.offsetMin = offsetMinOriginal;
                this.offsetMax = offsetMaxOriginal;

                return this;
            }


            private bool modLayoutDimensions = false;
            private float layoutWidth = 0f;
            private float layoutWidthOriginal = 0f;
            private float layoutHeight = 0f;
            private float layoutHeightOriginal = 0f;

            public UiModification ReplaceLayoutDimensions(float width, float height)
            {
                modLayoutDimensions = true;
                this.layoutWidth = width;
                this.layoutHeight = height;

                return this;
            }

            public UiModification ResetLayoutDimensions()
            {
                modLayoutDimensions = false;
                this.layoutWidth = layoutWidthOriginal;
                this.layoutHeight = layoutHeightOriginal;

                return this;
            }


            private bool modAnchoredPosition = false;
            private Vector2 anchoredPosition = Vector2.zero;
            private Vector2 anchoredPositionOriginal = Vector2.zero;

            public UiModification ReplaceAnchoredPosition(Vector2 anchoredPosition)
            {
                modAnchoredPosition = true;
                this.anchoredPosition = anchoredPositionOriginal * anchoredPosition;

                return this;
            }

            public UiModification ResetAnchoredPosition()
            {
                modAnchoredPosition = false;
                this.anchoredPosition = anchoredPositionOriginal;

                return this;
            }



            private bool hasOnUpdate = false;
            private Action<GameObject> onUpdate;

            public UiModification SetOnUpdate(Action<GameObject> onUpdate)
            {
                hasOnUpdate = true;

                this.onUpdate = onUpdate;

                return this;
            }

            public UiModification ClearOnUpdate()
            {
                hasOnUpdate = false;

                return this;
            }
        }

        public static Dictionary<GameObject, UiModification> uiModifications = new();
        private static bool NeedsUpdate = false;

        public static GameObject InstanciateUI(GameObject template, GameObject parent, string name, Vector3 localPos, Vector3 scale)
        {
            GameObject ui = GameObject.Instantiate(template);
            ui.transform.SetParent(parent);//fleetButtons.GetComponent<LayoutGroup>().transform);
            ui.transform.localPosition = localPos;
            ui.transform.SetScale(scale);
            ui.name = name;
            return ui;
        }

        public static void SetLocalizedTextTag(GameObject ui, string tag)
        {
            LocalizeText localize = ui.GetComponent<LocalizeText>();

            if (localize == null)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Failed to get LocalizeText component from {ui.name}");
                return;
            }

            if (localize.LocalizedElements.Length != 1)
            {
                Melon<TweaksAndFixes>.Logger.Error($"{ui.name}.LocalizedElements.Length == {localize.LocalizedElements.Length}. Should be 1.");
                return;
            }

            TextMeshProUGUI TMPElement = localize.LocalizedElements[0].TextMeshPro;

            if (TMPElement == null)
            {
                TMPElement = ui.GetComponent<TextMeshProUGUI>();
            }

            Text textElement = localize.LocalizedElements[0].Text;

            if (textElement == null)
            {
                textElement = ui.GetComponent<Text>();
            }

            if (TMPElement == null && textElement == null)
            {
                Melon<TweaksAndFixes>.Logger.Error(
                    $"Failed to get TextMeshProUGUI/Text component from {ui.GetParent().name}.{ui.name}.LocalizeText.LocalizedElements and failed to fetch component from object."
                );
                return;
            }

            localize.LocalizedElements[0] = new LocalizeText.LocalizedElement();
            localize.LocalizedElements[0].Tag = tag;
            localize.LocalizedElements[0].DefaultText = "";
            localize.LocalizedElements[0].TextMeshPro = TMPElement;
            localize.LocalizedElements[0].Text = textElement;
        }

        public static void CreateLocalizedTextTag(GameObject ui, TextMeshProUGUI textElement, string tag)
        {
            LocalizeText localize = ui.AddComponent<LocalizeText>();

            localize.LocalizedElements.AddItem(new LocalizeText.LocalizedElement());

            localize.LocalizedElements[0].Tag = tag;
            localize.LocalizedElements[0].DefaultText = "";
            localize.LocalizedElements[0].TextMeshPro = textElement;
        }

        public static void SetButtonOnClick(GameObject ui, System.Action action)
        {
            Button button = ui.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public static void AddTooltip(GameObject ui, string content)
        {
            OnEnter onEnter = ui.AddComponent<OnEnter>();
            onEnter.action = new System.Action(() => {
                if (!ui.active || !ui.GetComponent<Button>().interactable)
                {
                    G.ui.HideTooltip();
                    return;
                }

                G.ui.ShowTooltip(LocalizeManager.Localize(content), ui);
            });

            OnLeave onLeave = ui.AddComponent<OnLeave>();
            onLeave.action = new System.Action(() => {
                G.ui.HideTooltip();
            });
        }

        public static UiModification ModifyUi(GameObject ui)
        {
            NeedsUpdate = true;

            if (ui == null)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Tried to modify null object!");
                return new UiModification(new GameObject());
            }

            // Melon<TweaksAndFixes>.Logger.Msg($"Modifying UI: {ui.name}");

            if (uiModifications.ContainsKey(ui))
            {
                return uiModifications[ui];
            }

            uiModifications.Add(ui, new UiModification(ui));

            return uiModifications[ui];
        }

        public static UiModification ModifyUi(GameObject ui, string childPath)
        {
            GameObject child = ModUtils.GetChildAtPath(childPath, ui);

            if (!child) return null;

            return ModifyUi(child);
        }

        public static void UpdateModifications(bool debug = false)
        {
            List<GameObject> removals = new List<GameObject>();

            foreach (var mod in uiModifications)
            {
                try
                {
                    if (mod.Key == null)
                    {
                        removals.Add(mod.Key);
                        continue;
                    }

                    if (mod.Key.GetParent() == null)
                    {
                        removals.Add(mod.Key);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    removals.Add(mod.Key);
                    continue;
                }

                if (debug) Melon<TweaksAndFixes>.Logger.Msg($"Update: {mod.Key.name}");

                mod.Value.Apply(mod.Key, NeedsUpdate, debug);
            }

            foreach (var ui in removals)
            {
                uiModifications.Remove(ui);
            }

            NeedsUpdate = false;
        }

        public static bool HasModification(GameObject ui)
        {
            return uiModifications.ContainsKey(ui);
        }



        ////////////////////// MODIFICATIONS //////////////////////



        public static void ApplyUiModifications()
        {
            CreateBailoutPopup();

            CreateHidePopupsButton();

            ApplySettingsMenuModifications();

            ApplyCampaignWindowModifications();
            ApplyDockyardModifications();

            G.GameData.tooltips["file_converter"] = new TooltipData();

            G.GameData.tooltips["file_converter"].name = "file_converter";
            // G.GameData.tooltips["file_converter"].title = "";
            G.GameData.tooltips["file_converter"].text = "$toolltip_file_converter";

            // Global/Ui/UiMain/Loading/LayoutDesc/Desc/DescText

            // UiM.ModifyUi(ModUtils.GetChildAtPath("Global/Ui/UiMain/Loading/LayoutDesc/Desc/DescText"));

            var loadingScreenText = ModUtils.GetChildAtPath("Global/Ui/UiMain/Loading/LayoutDesc/Desc/DescText").GetComponent<TMP_Text>();
            loadingScreenText.alignment = TextAlignmentOptions.Center;

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/(Beam/Draught)/Slider

            // GameObject beamSetting = GameObject.Instantiate(ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Beam"));
            // beamSetting.transform.SetParent(ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings"));
            // beamSetting.transform.SetScale(1, 1, 1);
            // beamSetting.name = "TAF Beam";
            // beamSliderComp = beamSetting.GetChild("Slider").GetComponent<Slider>();
            // beamSliderComp.onValueChanged.RemoveAllListeners();
            // // ModifyUi(beamSetting).SetOnUpdate(new System.Action<GameObject>((GameObject obj) =>
            // // {
            // //     Ship ship = ShipM.GetActiveShip();
            // // 
            // //     if (ship == null) return;
            // // 
            // //     // ship.hull.data.paramx
            // // 
            // //     // beamSliderComp.maxValue;
            // // }));
            // beamSliderComp.onValueChanged.AddListener(new System.Action<float>((float value) =>
            // {
            //     Ship ship = ShipM.GetActiveShip();
            // 
            //     if (ship == null) return;
            //     
            //     Melon<TweaksAndFixes>.Logger.Msg($"{value}");
            // 
            //     ship.SetBeam(value / 10);
            // }));
            // GameObject beamOld = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Beam");
            // beamOld.SetActive(false);

            // GameObject draughtSlider = (ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Draught/Slider"));
            // draughtSlider.transform.SetParent(ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Draught"));
            // draughtSlider.transform.SetScale(1, 1, 1);
            // draughtSlider.TryDestroyComponent<Slider>();
            // draughtSlider.AddComponent<Slider>();
            // draughtSlider.TryDestroyComponent<EventTrigger>();
            // Slider draughtSliderComp = draughtSlider.GetComponent<Slider>();
            // draughtSliderComp.onValueChanged.RemoveAllListeners();
            // draughtSliderComp.onValueChanged.AddListener(new System.Action<float>((float value) =>
            // {
            //     Ship ship = ShipM.GetActiveShip();
            // 
            //     if (ship == null) return;
            // 
            //     ship.SetDraught(value - 5);
            // }));
            // GameObject draughtSliderOld = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Draught/Slider");
            // draughtSliderOld.SetActive(false);
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

        public static void AddConfirmationPopups()
        {
            Ui ui = G.ui;

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

            AddConfirmPopupToButton(ui.FleetWindow.Delete, "$TAF_Ui_FleetWindow_Confirm_Action_Delete");
            AddConfirmPopupToButton(ui.FleetWindow.Scrap, "$TAF_Ui_FleetWindow_Confirm_Action_Scrap");
        }

        // ========== CAMPAIGN ========== //

        public static void ApplyCampaignWindowModifications()
        {
            UiM.ModifyUi(G.ui.FleetWindow.Root.GetChild("Root")).ReplaceOffsets(new Vector2(-800f, -400f), new Vector2(800f, 400f));

            ApplyCampaginDesignTabModifications();
            ApplyCampaignFleetTabModifications();

            ApplyPoliticsWindowModifications();
        }

        public static void ApplyPoliticsWindowModifications()
        {
            GameObject politicsWindow = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/Windows/Politics Window/");

            GameObject politicsHeader = ModUtils.GetChildAtPath("Root/Header", politicsWindow);

            politicsHeader.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            politicsHeader.GetChild("FlagAndName").GetComponent<LayoutElement>().preferredWidth = 200;

            politicsHeader.GetChild("GeneralInfo").GetComponent<LayoutElement>().preferredWidth = 270;

            politicsHeader.GetChild("Financial").GetComponent<LayoutElement>().preferredWidth = 310;

            politicsHeader.GetChild("Naval").GetComponent<LayoutElement>().preferredWidth = 250;

            politicsHeader.GetChild("Minor Allies").GetComponent<LayoutElement>().preferredWidth = 200;

            politicsHeader.GetChild("Minor Allies").GetComponent<LayoutElement>().flexibleWidth = -1;

            politicsHeader.GetChild("Relations").GetComponent<LayoutElement>().minWidth = 500;

            politicsHeader.GetChild("Relations").GetComponent<LayoutElement>().flexibleWidth = 12.5f;

            politicsHeader.GetChild("Actions").GetComponent<LayoutElement>().preferredWidth = 250;

            GameObject RelationsTemplate = politicsHeader.GetChild("Relations").GetChild("Template");

            UiM.ModifyUi(RelationsTemplate).SetOnUpdate(new System.Action<GameObject>((GameObject ui) => {
                if (RelationsTemplate.transform.localPosition.x != 0) RelationsTemplate.transform.localPosition = Vector3.zero;
            }));
        }

        private static void ApplyCampaginDesignTabModifications()
        {
            UiM.ModifyUi(G.ui.FleetWindow.Root, "Root/Design Ships").ReplaceOffsetMin(new Vector2(-1200.0f, -700.0f));

            UiM.ModifyUi(G.ui.FleetWindow.Root, "Root/Design Ships/Scrollbar Vertical").ReplaceOffsetMin(new Vector2(-15.0f, 0.0f));

            UiM.ModifyUi(G.ui.FleetWindow.DesignHeader.gameObject).ReplaceOffsetMin(new Vector2(-1200.0f, -44.4f));

            UiM.ModifyUi(G.ui.FleetWindow.DesignShipInfoRoot, "ShipIsoImage/ShipIsometrImage").ReplaceOffsets(new Vector2(0.0f, -332.5f), new Vector2(350.0f, 0.0f));

            UiM.ModifyUi(G.ui.FleetWindow.DesignShipInfoRoot, "Text/ShipTextInfo").ReplaceOffsets(new Vector2(0.0f, -470.0f), new Vector2(350.0f, 0.0f));

            UiM.ModifyUi(G.ui.FleetWindow.DesignHeader, "Name").ReplaceLayoutDimensions(500f, -1.0f);

            UiM.ModifyUi(G.ui.FleetWindow.DesignTemplate.Name.gameObject).ReplaceLayoutDimensions(500f, -1.0f);

            UiM.ModifyUi(G.ui.FleetWindow.DesignButtonsRoot).ReplaceOffsetMin(new Vector2(-1200.0f, 31.1f));
        }

        private static void ApplyCampaignFleetTabModifications()
        {
            // Fleet Buttons

            FleetWindow_ShipElementUI fleetTemplate = G.ui.FleetWindow.FleetTemplate;

            GameObject fleetButtons = G.ui.FleetWindow.FleetButtonsRoot;

            UiM.ModifyUi(fleetButtons).ReplaceOffsetMin(new Vector2(-1463.6f, 31.1f));

            fleetTemplate.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();

            GameObject setRole = InstanciateUI(fleetButtons.GetChild("View"), fleetButtons, "Set Role", Vector3.zero, new Vector3(1.2114f, 1.2114f, 1.2114f));
            SetLocalizedTextTag(setRole.gameObject.GetChildren()[0], "$TAF_Ui_World_FleetDesign_SetRole");
            AddTooltip(setRole, $"$TAF_tooltip_set_role");
            SetButtonOnClick(setRole, new System.Action(() =>
            {
                if (G.ui.FleetWindow.selectedElements.Count > 0)
                {
                    G.ui.FleetWindow.selectedElements[^1].RoleSelectionButton.onClick.Invoke();
                }
            }));

            GameObject setCrew = InstanciateUI(fleetButtons.GetChild("View"), fleetButtons, "Set Crew", Vector3.zero, new Vector3(1.2114f, 1.2114f, 1.2114f));
            SetLocalizedTextTag(setCrew.gameObject.GetChildren()[0], "$Ui_World_FleetDesign_SetCrew");
            AddTooltip(setCrew, $"$TAF_tooltip_set_crew");
            SetButtonOnClick(setCrew, new System.Action(() =>
            {
                if (G.ui.FleetWindow.selectedElements.Count == 1)
                {
                    G.ui.FleetWindow.selectedElements[0].CrewAction.onClick.Invoke();

                    GameObject popup = G.ui.gameObject.GetChild("MessageBox(Clone)", true);

                    if (popup != null)
                    {
                        Slider slider = popup.GetChild("Root").GetChild("Campaign Slider").GetComponent<Slider>();
                    }
                }
                else if (G.ui.FleetWindow.selectedElements.Count > 1)
                {
                    // Create base-popup
                    G.ui.FleetWindow.selectedElements[0].CrewAction.onClick.Invoke();

                    GameObject popup = G.ui.gameObject.GetChild("MessageBox(Clone)", true);

                    if (popup != null)
                    {
                        // Settup ship names for body text (TODO: make sure this works in other languages)
                        string shipNames = G.ui.FleetWindow.selectedElements[0].CurrentShip.Name(false, true);

                        for (int i = 1; i < G.ui.FleetWindow.selectedElements.Count && i < 3; i++)
                        {
                            shipNames += $", {G.ui.FleetWindow.selectedElements[i].CurrentShip.Name(false, true)}";
                        }

                        if (G.ui.FleetWindow.selectedElements.Count > 3)
                        {
                            shipNames += $", ... (+{G.ui.FleetWindow.selectedElements.Count - 3})";
                        }

                        // Calculate crew stats
                        float totalCrewCap = 0;
                        float existingCrew = 0;
                        float crewPool = G.ui.FleetWindow.selectedElements[0].CurrentShip.player.crewPool;

                        foreach (var element in G.ui.FleetWindow.selectedElements)
                        {
                            totalCrewCap += element.CurrentShip.GetTotalCrew();
                            existingCrew += element.CurrentShip.GetShipCrew();
                        }

                        // Melon<TweaksAndFixes>.Logger.Msg($"Crew Counts");
                        // Melon<TweaksAndFixes>.Logger.Msg($"  totalCrewCap:  {totalCrewCap}");
                        // Melon<TweaksAndFixes>.Logger.Msg($"  existingCrew:  {existingCrew}");
                        // Melon<TweaksAndFixes>.Logger.Msg($"  crewPool:      {crewPool}");
                        // Melon<TweaksAndFixes>.Logger.Msg($"  Max Percent:   {(int)(Math.Min(1f, crewPool / (totalCrewCap - existingCrew)) * 100f)}");

                        // Get body text
                        TMP_Text text = ModUtils.GetChildAtPath("Root/Data", popup).GetComponent<TMP_Text>();

                        // Configure slider
                        Slider slider = ModUtils.GetChildAtPath("Root/Campaign Slider", popup).GetComponent<Slider>();
                        slider.minValue = 0;
                        slider.maxValue = (int)(Math.Min(1f, crewPool / (totalCrewCap - existingCrew)) * 100f);
                        slider.onValueChanged.RemoveAllListeners();
                        slider.onValueChanged.AddListener(new System.Action<float>((float value) =>
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"Slider value changed: {value}");

                            string extraInfo = "";

                            if (value < 70 && value > 0)
                            {
                                extraInfo += String.Format(LocalizeManager.Localize("$Ui_World_FleetDesign_ShipStatusWillBeSetToCrew"), "<color=#FFDA2F>", "</color>");
                            }
                            else if (value == 0)
                            {
                                extraInfo += String.Format(LocalizeManager.Localize("$Ui_World_FleetDesign_ShipStatusWillBeSetToMoth"), "<color=#B5B5B5>", "</color>");
                            }

                            text.text = String.Format(LocalizeManager.Localize("$Ui_World_FleetDesign_ShipCrewAmount2"), $"{value:N0}%", shipNames, extraInfo);
                        }));
                        slider.Set(slider.maxValue); // Invoke after configuring onValueChanged

                        // Configure OK button
                        Button ok = ModUtils.GetChildAtPath("Root/Buttons/Ok", popup).GetComponent<Button>();
                        ok.onClick.RemoveAllListeners();
                        ok.onClick.AddListener(new System.Action(() => {
                            foreach (var element in G.ui.FleetWindow.selectedElements)
                            {
                                // Get new crew values
                                int existing = (int)(element.CurrentShip.GetShipCrew() + 0.05);
                                int setTo = (int)(Math.Round(element.CurrentShip.GetTotalCrew() * (slider.value / 100f)) + 0.05);

                                // Melon<TweaksAndFixes>.Logger.Msg($"{element.CurrentShip.Name(false, false)}");
                                // Melon<TweaksAndFixes>.Logger.Msg($"  GetTotalCrew:  {element.CurrentShip.GetTotalCrew()}");
                                // Melon<TweaksAndFixes>.Logger.Msg($"  GetShipCrew:   {existing}");
                                // Melon<TweaksAndFixes>.Logger.Msg($"  Set Crew To:   {existing} -> {setTo}");

                                if (setTo == existing) continue;

                                // Invoke the popup for seting the crew for a single ship.
                                // This is *horrifiyingly ugly* but doing it manually causes way too many problems
                                element.CrewAction.onClick.Invoke();

                                GameObject subPopup = null;

                                foreach (var child in G.ui.gameObject.GetChildren())
                                {
                                    if (child.name != "MessageBox(Clone)") continue;

                                    if (child == popup) continue;

                                    subPopup = child;
                                    break;
                                }

                                if (subPopup == null)
                                {
                                    Melon<TweaksAndFixes>.Logger.Error($"Failed to configure crew for ship {element.CurrentShip.Name(false, false)}!");
                                    continue;
                                }

                                Slider subSlider = ModUtils.GetChildAtPath("Root/Campaign Slider", subPopup).GetComponent<Slider>();
                                subSlider.Set(setTo);

                                Button subOk = ModUtils.GetChildAtPath("Root/Buttons/Ok", subPopup).GetComponent<Button>();
                                subOk.onClick.Invoke();

                                // Delete popup afterwords otherwise it sticks around for a frame
                                subPopup.transform.SetParent(null, false);
                            }

                            // Cleanup multi-ship popup
                            popup.transform.SetParent(null, false);
                        }));
                    }
                }
            }));

            GameObject viewOnMap = InstanciateUI(fleetButtons.GetChild("View"), fleetButtons, "View On Map", Vector3.zero, new Vector3(1.2114f, 1.2114f, 1.2114f));
            SetLocalizedTextTag(viewOnMap.gameObject.GetChildren()[0], "$TAF_Ui_World_FleetDesign_ViewOnMap");
            AddTooltip(viewOnMap, $"$TAF_tooltip_view_on_map");
            SetButtonOnClick(viewOnMap, new System.Action(() =>
            {
                if (G.ui.FleetWindow.selectedElements.Count == 1)
                {
                    // Needs to be invoked twice to center the camera
                    G.ui.FleetWindow.selectedElements[0].AreaButton.onClick.Invoke();
                    G.ui.FleetWindow.selectedElements[0].AreaButton.onClick.Invoke();
                }
            }));

            UiM.ModifyUi(fleetButtons.GetChild("Mothballed")).SetActive(false, false);

            UiM.ModifyUi(fleetButtons.GetChild("View")).SetActive(false, false);

            UiM.ModifyUi(fleetButtons).SetChildOrder("AddCrewToggle", "Set Role", "View On Map", "ChangePort", "Set Crew", "Suspend", "Scrap", "Cancel Sale");




            // Fleet Ships

            UiM.ModifyUi(G.ui.FleetWindow.Root.GetChild("Root").GetChild("Fleet Ships")).ReplaceOffsetMin(new Vector2(-1585.0f, -700.0f));




            // Fleet Template

            LayoutGroup templateGroup = fleetTemplate.GetComponent<LayoutGroup>();
            templateGroup.padding.left = 10;
            templateGroup.padding.right = 10;

            fleetTemplate.gameObject.GetChild("Crew").name = "CrewAction";
            
            GameObject roleText = InstanciateUI(
                fleetTemplate.RoleSelectionButton.gameObject.GetChildren()[0],
                fleetTemplate.RoleSelectionButton.gameObject.GetParent(),
                "RoleText", Vector3.zero, Vector3.one
            );
            roleText.GetComponent<TMP_Text>().text = "ERROR";
            roleText.GetComponent<TMP_Text>().fontSize = 11;
            roleText.GetComponent<TMP_Text>().fontSizeMin = 11;
            roleText.GetComponent<TMP_Text>().fontSizeMax = 11;

            UiM.ModifyUi(fleetTemplate.gameObject).SetChildOrder(
                "Selected", "Type", "Name", "NameInputField", "Class", "Damage", "Ammo", "Fuel", "Role",
                "Status", "Area", "Port", "Port Selection", "Cost", "Crew", "Tonnage", "Date", "Speed", "Weapons", "Sold"
            );

            UiM.ModifyUi(fleetTemplate.Type.gameObject).ReplaceLayoutDimensions(60f, -1.0f);

            UiM.ModifyUi(fleetTemplate.Name.gameObject).ReplaceLayoutDimensions(275f, -1.0f);

            UiM.ModifyUi(fleetTemplate.NameInputField.gameObject).ReplaceLayoutDimensions(275f, -1.0f);

            fleetTemplate.NameInputField.gameObject.GetComponent<TMP_InputField>().onValidateInput = null;

            UiM.ModifyUi(fleetTemplate.Class.gameObject).ReplaceLayoutDimensions(275f, -1.0f);

            UiM.ModifyUi(fleetTemplate.Damage.gameObject).ReplaceLayoutDimensions(60f, -1.0f);

            UiM.ModifyUi(fleetTemplate.Ammo.gameObject).ReplaceLayoutDimensions(60f, -1.0f);

            UiM.ModifyUi(fleetTemplate.Fuel.gameObject).ReplaceLayoutDimensions(60f, -1.0f);

            GameObject hidenElements = new GameObject();
            hidenElements.SetParent(fleetTemplate.gameObject);
            hidenElements.name = "HidenElements";
            hidenElements.SetActive(false);

            UiM.ModifyUi(fleetTemplate.Speed.gameObject).SetActive(false, false);

            UiM.ModifyUi(fleetTemplate.Weapons.gameObject).SetActive(false, false);

            UiM.ModifyUi(fleetTemplate.RoleSelectionButton.gameObject).SetActive(false, false);

            UiM.ModifyUi(fleetTemplate.CrewAction.gameObject.GetParent()).SetActive(false, false);

            UiM.ModifyUi(fleetTemplate.Sold.gameObject).SetActive(false, false);

            fleetTemplate.Area.gameObject.name = "Area (old)";

            GameObject areaText = GameObject.Instantiate(fleetTemplate.Area.gameObject);
            areaText.SetParent(fleetTemplate.gameObject);
            areaText.transform.SetScale(1, 1, 1);
            // areaText.transform.localPosition = new Vector3();
            areaText.name = "Area";
            areaText.TryDestroyComponent<LocalizeFont>();
            areaText.TryDestroyComponent<Button>();
            areaText.GetComponent<TMP_Text>().text = "ERROR";
            areaText.GetComponent<TMP_Text>().fontSize = 11;
            areaText.GetComponent<TMP_Text>().fontSizeMin = 11;
            areaText.GetComponent<TMP_Text>().fontSizeMax = 11;

            fleetTemplate.Area.gameObject.SetParent(hidenElements);

            // UiM.ModifyUi(fleetTemplate.Port.gameObject).SetActive(false, false);

            fleetTemplate.Port.gameObject.name = "Port (old)";

            GameObject portText = GameObject.Instantiate(fleetTemplate.Port.gameObject);
            portText.SetParent(fleetTemplate.gameObject);
            portText.transform.SetScale(1, 1, 1);
            // portText.transform.localPosition = new Vector3();
            portText.name = "Port";
            portText.TryDestroyComponent<LocalizeFont>();
            portText.TryDestroyComponent<Button>();
            portText.GetComponent<TMP_Text>().text = "ERROR";
            portText.GetComponent<TMP_Text>().fontSize = 13;
            portText.GetComponent<TMP_Text>().fontSizeMin = 13;
            portText.GetComponent<TMP_Text>().fontSizeMax = 13;

            fleetTemplate.Port.gameObject.SetParent(hidenElements);


            // Fleet Header

            GameObject fleetHeader = G.ui.FleetWindow.FleetHeader;

            var headerGroup = fleetHeader.GetComponent<LayoutGroup>();
            headerGroup.padding.left = 0;
            headerGroup.padding.right = 0;

            UiM.ModifyUi(fleetHeader)
                .ReplaceOffsetMin(new Vector2(-1585.0f, -44.4f))
                .SetChildOrder("Type", "Name", "Class", "Damage", "Ammo", "Fuel", "Role", "Status", "Area", "Port", "Cost", "Crew", "Tonnage", "Date", "Speed", "Weapons", "CrewAction", "Sold");

            UiM.ModifyUi(fleetHeader, "Damage").ReplaceLayoutDimensions(60f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Ammo").ReplaceLayoutDimensions(60f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Fuel").ReplaceLayoutDimensions(60f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Name").ReplaceLayoutDimensions(275f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Status").ReplaceLayoutDimensions(100f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Port").ReplaceLayoutDimensions(110f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Class").ReplaceLayoutDimensions(275f, -1.0f);

            UiM.ModifyUi(fleetHeader, "Speed").SetActive(false, false);

            UiM.ModifyUi(fleetHeader, "Weapons").SetActive(false, false);

            UiM.ModifyUi(fleetHeader, "CrewAction").SetActive(false, false);

            UiM.ModifyUi(fleetHeader, "Sold").SetActive(false, false);
        }

        private static GameObject bailoutEvent;
        private static TMP_Text bailoutEventWindowBodyText;
        private static TMP_Text bailoutEventWindowYesText;
        private static Player bailoutPlayer;

        public static void CreateBailoutPopup()
        {
            // Global/Ui/UiMain/Popup/Generic

            // Global/Ui/UiMain/Popup/Bailout Event/Window/TextScrollView/Viewport/Content/Text

            bailoutEvent = GameObject.Instantiate(ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Generic"));
            bailoutEvent.transform.SetParent(ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows"));
            bailoutEvent.name = "Bailout Event";
            bailoutEvent.transform.SetScale(1, 1, 1);
            bailoutEvent.transform.localPosition = Vector3.zero;
            RectTransform bailoutEventTransform = bailoutEvent.GetComponent<RectTransform>();
            bailoutEventTransform.offsetMin = Vector3.zero;
            bailoutEventTransform.offsetMax = Vector3.zero;
            bailoutEvent.GetChild("BgScreen").TryDestroy();

            GameObject bailoutEventWindow = bailoutEvent.GetChild("Window");
            bailoutEventWindow.GetChild("Image").TryDestroy();
            bailoutEventWindow.GetChild("TextScrollView").TryDestroy();
            bailoutEventWindow.GetChild("Bg").GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
            ModUtils.GetChildAtPath("Buttons/Ok", bailoutEventWindow).TryDestroy();
            ModUtils.GetChildAtPath("Buttons/No", bailoutEventWindow).TryDestroy();

            GameObject bailoutEventWindowYes = ModUtils.GetChildAtPath("Buttons/Yes", bailoutEventWindow);
            bailoutEventWindowYes.transform.SetScale(1.2f, 1.2f, 1.2f);
            GameObject bailoutEventWindowYesTextObj = bailoutEventWindowYes.GetChild("Text (TMP)");
            bailoutEventWindowYesTextObj.TryDestroyComponent<LocalizeText>();
            bailoutEventWindowYesText = bailoutEventWindowYesTextObj.GetComponent<TMP_Text>();
            Button bailoutEventWindowYesBtn = bailoutEventWindowYes.GetComponent<Button>();
            bailoutEventWindowYesBtn.onClick.RemoveAllListeners();
            bailoutEventWindowYesBtn.onClick.AddListener(new System.Action(() =>
            {
                if (bailoutPlayer == null)
                {
                    bailoutEvent.SetActive(false);
                    Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid player for bailout event!");
                    return;
                }

                int bailoutEventNumber = Config.Param("taf_bailout_event_number", 81);

                EventData prompt = G.GameData.events[$"{bailoutEventNumber}"];
                EventData response = G.GameData.events[$"{bailoutEventNumber}_a"];

                EventX ev = new EventX();
                ev.date = CampaignController.Instance.CurrentDate;
                ev.showEventToMainPlayer = true;
                ev.data = prompt;
                ev.player = bailoutPlayer;
                ev.Init();

                Melon<TweaksAndFixes>.Logger.Msg($"Bailout accepted!");

                CampaignController.Instance.AnswerEvent(ev, response);

                bailoutEvent.SetActive(false);
            }));

            GameObject bailoutEventWindowHeader = bailoutEventWindow.GetChild("Header");
            bailoutEventWindowHeader.GetComponent<TMP_Text>().text = "Government Bailout"; // TODO: Localize

            GameObject bailoutEventWindowBody = bailoutEventWindow.GetChild("TextOld");
            bailoutEventWindowBody.name = "Text";
            bailoutEventWindowBody.SetActive(true);
            bailoutEventWindowBodyText = bailoutEventWindowBody.GetComponent<TMP_Text>();
        }

        public static void ShowBailoutPopupForPlayer(Player player)
        {
            int bailoutEventNumber = Config.Param("taf_bailout_event_number", 81);

            EventData prompt = G.GameData.events[$"{bailoutEventNumber}"];
            EventData response = G.GameData.events[$"{bailoutEventNumber}_a"];

            bailoutPlayer = player;

            // TODO: Localize
            string promptStr = LocalizeManager.Localize(prompt.text);
            promptStr += $"\n\n{ModUtils.ColorNumber(response.money, "", "%", false, true)} Naval Funds ({ModUtils.ColorNumber(player.Budget() * response.money / 100, "$", "", false, true)})";
            promptStr += $"\n{ModUtils.ColorNumber(response.budget, "", "%")} Naval Budget";
            promptStr += $"\n{ModUtils.ColorNumber(response.wealth, "", "%")} GDP";
            promptStr += $"\n{ModUtils.ColorNumber(response.reputation)} Naval Prestige";
            promptStr += $"\n{ModUtils.ColorNumber(response.respect, "", "", true)} Unrest";

            // Melon<TweaksAndFixes>.Logger.Msg($"cash:                      {bailoutPlayer.cash}");
            // Melon<TweaksAndFixes>.Logger.Msg($"inflation:                 {bailoutPlayer.inflation}");
            // Melon<TweaksAndFixes>.Logger.Msg($"wealth:                    {bailoutPlayer.wealth}");
            // Melon<TweaksAndFixes>.Logger.Msg($"wealthGrowth:              {bailoutPlayer.wealthGrowth}");
            // Melon<TweaksAndFixes>.Logger.Msg($"wealthGrowthMul:           {bailoutPlayer.wealthGrowthMul}");
            // Melon<TweaksAndFixes>.Logger.Msg($"wealthGrowthEffectivePrev: {bailoutPlayer.wealthGrowthEffectivePrev}");
            // Melon<TweaksAndFixes>.Logger.Msg($"nationBaseIncomeGrowth:    {bailoutPlayer.nationBaseIncomeGrowth}");
            // Melon<TweaksAndFixes>.Logger.Msg($"budgetMod:                 {bailoutPlayer.budgetMod}");
            // Melon<TweaksAndFixes>.Logger.Msg($"unrest:                    {bailoutPlayer.unrest}");
            // Melon<TweaksAndFixes>.Logger.Msg($"reputation:                {bailoutPlayer.reputation}");
            // Melon<TweaksAndFixes>.Logger.Msg($"wealthGrowthEff:           {bailoutPlayer.WealthGrowthEffective()}");
            // Melon<TweaksAndFixes>.Logger.Msg($"wealthGrowthHit:           {(response.wealth / 100 + 1) * (bailoutPlayer.WealthGrowthEffective() + 1) - 1}");
            // 
            // bailoutPlayer.cash += player.Budget() * response.money / 100;
            // bailoutPlayer.wealthGrowth = -0.5f;//(response.wealth / 100 + 1) * (bailoutPlayer.WealthGrowthEffective() + 1) - 1;
            // bailoutPlayer.budgetMod += response.budget;
            // bailoutPlayer.reputation += response.reputation;
            // bailoutPlayer.AddUnrest(-response.respect);

            bailoutEventWindowBodyText.text = promptStr;
            bailoutEventWindowYesText.text = LocalizeManager.Localize(response.text);

            bailoutEvent.SetActive(true);
        }

        public static void CreateHidePopupsButton()
        {
            GameObject popupSaveWindow = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/SaveWindow");
            GameObject regularPopupsRoot = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows");
            var regularPopups = regularPopupsRoot.GetChildren();
            GameObject basePopupsRoot = ModUtils.GetChildAtPath("Ui/UiMain", G.container);

            bool showPopups = true;

            GameObject nextTurnButton = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/Windows/Map Window/Next Turn Panel/Next Turn Button");
            GameObject hidePopupsButton = GameObject.Instantiate(nextTurnButton);
            hidePopupsButton.transform.SetParent(ModUtils.GetChildAtPath("Ui/UiMain", G.container));
            hidePopupsButton.name = "Hide Popups";
            hidePopupsButton.transform.SetScale(1, 1, 1);
            hidePopupsButton.transform.position = new Vector3(3490, 110, 0); // -125 40
            RectTransform hidePopupsButtonRect = hidePopupsButton.GetComponent<RectTransform>();
            hidePopupsButtonRect.anchoredPosition = new Vector2(-125, 40);
            hidePopupsButtonRect.anchorMin = new Vector2(1, 0);
            hidePopupsButtonRect.anchorMax = new Vector2(1, 0);
            hidePopupsButton.SetActive(false);
            GameObject hidePopupsText = hidePopupsButton.GetChild("Text (TMP)");
            hidePopupsText.TryDestroyComponent<LocalizeText>();
            TMP_Text hidePopupsTextComp = hidePopupsText.GetComponent<TMP_Text>();
            hidePopupsTextComp.text = "Hide Popups";
            Button hidePopupsButtonComp = hidePopupsButton.GetComponent<Button>();
            hidePopupsButtonComp.onClick.RemoveAllListeners();
            hidePopupsButtonComp.onClick.AddListener(new System.Action(() => {
                showPopups = !showPopups;
                if (showPopups)
                {
                    hidePopupsTextComp.text = "Hide Popups";
                }
                else
                {
                    hidePopupsTextComp.text = "Show Popups";
                }
            }));

            ModifyUi(basePopupsRoot).SetOnUpdate(new System.Action<GameObject>((GameObject ui) => {
                bool hasPopups = false;

                // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"Update popup tracker:");

                if (!GameManager.IsWorld)
                {
                    hasPopups = false;
                    showPopups = true;
                    // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"  Not in world");
                    return;
                }

                // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"  Checking premade popups...");

                foreach (GameObject child in regularPopups)
                {
                    if (child.name != "Event Window" && child.name != "Battle Window" && child.name != "WarReparationWindowUI") continue;

                    if (child.active)
                    {
                        // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"    Found popup {child.name}!");
                        hasPopups = true;
                        break;
                    }
                }

                regularPopupsRoot.SetActive(showPopups);

                if (basePopupsRoot.GetChild("MessageBox(Clone)", true) != null)
                {
                    bool isFirst = true;

                    var children = basePopupsRoot.GetChildren();

                    // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"  Checking generic popups...");

                    foreach (GameObject child in children)
                    {
                        if (child.name != "MessageBox(Clone)") continue;

                        // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"    Parsing popup...");

                        if (!isFirst)
                        {
                            child.TryDestroyComponent<Image>();
                        }

                        isFirst = false;

                        hasPopups = true;

                        child.SetActive(showPopups);
                    }

                    hidePopupsButton.transform.SetSiblingIndex(children.Count - 1);
                }

                // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"  Has popups: {hasPopups} | !Show popups {!showPopups} | !Loading Screen {!GameManager.IsLoadingScreenActive} | World Map {GameManager.IsWorldMap} | !Save Menu {!popupSaveWindow.active}");

                hidePopupsButton.SetActive((hasPopups || !showPopups) && (!GameManager.IsLoadingScreenActive && GameManager.IsWorldMap && !popupSaveWindow.active));
            }));
        }

        // ========== DOCKYARD ========== //

        public static void ApplyDockyardModifications()
        {
            ApplyShipTypeButtonsModifications();

            ApplyInputYearModifications();

            // Global/Ui/UiMain/Constructor/Left/Scroll View/

            GameObject ConstructorLeftPannel = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View");

            ModifyUi(ConstructorLeftPannel).SetChildOrder("Scrollbar Vertical", "Scrollbar Horizontal", "Viewport");

            CreateTopBarRotationButton();
            CreateTopBarRotationText();
            CreateArmorQualityButton();

            GameObject bugReporter = G.ui.commonUi.GetChild("Options").GetChild("BugReport");

            GameObject CloneShip  = G.ui.conUpperButtons.GetChild("Layout").GetChild("CloneShip");
            GameObject DeleteShip = G.ui.conUpperButtons.GetChild("Layout").GetChild("DeleteShip");
            GameObject Undo = G.ui.conUpperButtons.GetChild("Layout").GetChild("Undo");

            ModifyUi(ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor")).SetOnUpdate(new System.Action<GameObject>((GameObject ui) => {
                if (Config.Param("taf_dockyard_remove_per_design_copy_delete_buttons", 1) == 1)
                {
                    RemoveConstructorDesignSmallCopyDelete();
                }

                if (Config.Param("taf_dockyard_remove_hotkey_buttons", 1) == 1)
                {
                    RemoveConstructorKeyButtons();
                }

                // New UI elements
                if (Config.Param("taf_dockyard_new_logic", 1) == 1)
                {
                    UpdateTopBarRotationButton();
                    UpdateTopBarRotationText();
                    UpdateArmorQualityButton();

                    if (!CloneShip.active) CloneShip.SetActive(true);

                    if (DeleteShip.active != !G.ui.isConstructorRefitMode) DeleteShip.SetActive(!G.ui.isConstructorRefitMode);

                    if (Undo.active) Undo.SetActive(false);
                }


                if (Config.Param("taf_add_confirmation_popups", 1) == 1)
                {
                    AddConfirmationPopups();
                }

                if (bugReporter.active) bugReporter.SetActive(false);
            }));
        }

        public static void ApplyShipTypeButtonsModifications()
        {
            // MAX -265 -50
            // MIN 780 0

            UiM.ModifyUi(G.ui.conUpperRight).ReplaceOffsets(new Vector2(780, 0), new Vector2(-265, -50));
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

        public static void RemoveConstructorKeyButtons()
        {
            Ui ui = G.ui;

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
                            if (Patch_Ui.PartCategory.name != "gun_main")
                                child.SetActive(false);
                        }));
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
        }

        public static void RemoveConstructorDesignSmallCopyDelete()
        {
            Ui ui = G.ui;

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
        }

        public static InputField InputChooseYearEditField;
        public static Text InputChooseYearStaticText;

        public static void ApplyInputYearModifications()
        {
            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseYear

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/ShipName

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/InputChooseYear/EditName/Edit/Placeholder

            GameObject ChooseNationYearYearSelector = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseYear");
            ModifyUi(ChooseNationYearYearSelector).SetEnabled(false);

            GameObject ChooseNationYear = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection");

            // ModifyUi(ChooseNationYear).SetChildOrder();

            GameObject InputChooseYear = GameObject.Instantiate(ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/ShipName"));
            InputChooseYear.SetParent(ChooseNationYear);
            InputChooseYear.transform.SetScale(1f, 1f, 1f);
            InputChooseYear.name = "InputChooseYear";
            InputChooseYear.GetComponent<LayoutElement>().preferredHeight = 40;

            GameObject InputChooseYearEditName = InputChooseYear.GetChild("EditName");
            LayoutGroup InputChooseYearEditNameLayout = InputChooseYearEditName.GetComponent<LayoutGroup>();
            InputChooseYearEditName.GetComponent<LayoutElement>().preferredHeight = 40;

            GameObject InputChooseYearBG = InputChooseYear.GetChild("EditName").GetChild("Bg");

            GameObject InputChooseYearEdit = InputChooseYear.GetChild("EditName").GetChild("Edit");
            InputChooseYearEdit.TryDestroyComponent<CheckShipName>();
            InputChooseYearEdit.transform.SetScale(1.3f, 1.3f, 1.3f);
            InputChooseYearEditField = InputChooseYearEdit.GetComponent<InputField>();
            InputChooseYearEditField.text = "1890";
            //InputChooseYearEditField.textComponent.fontSize += 5;

            GameObject InputChooseYearStatic = InputChooseYear.GetChild("EditName").GetChild("Static");
            InputChooseYearStatic.GetChild("Header").TryDestroy();
            InputChooseYearStatic.transform.SetScale(1.3f, 1.3f, 1.3f);
            InputChooseYearStaticText = InputChooseYearStatic.GetChild("Text").GetComponent<Text>();
            InputChooseYearStaticText.text = "1890";
            //InputChooseYearStaticText.fontSize += 5;

            GameObject spacer = GameObject.Instantiate(InputChooseYear.GetChild("EditName").GetChild("Static").GetChild("EditIcon"));
            spacer.transform.SetParent(InputChooseYearStatic.transform);
            spacer.transform.SetScale(1f, 1f, 1f);
            spacer.transform.SetSiblingIndex(0);
            spacer.TryDestroyComponent<Image>();
            spacer.TryDestroyComponent<CanvasGroup>();
            spacer.TryDestroyComponent<Outline>();

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/InputChooseYear/EditName/Edit/Placeholder

            GameObject placeholder = ModUtils.GetChildAtPath("InputChooseYear/EditName/Edit/Placeholder", ChooseNationYear);
            Text placeholderText = placeholder.GetComponent<Text>();

            Button btn = InputChooseYear.GetChild("EditName").AddComponent<Button>();
            btn.onClick.AddListener(new System.Action(() =>
            {
                InputChooseYearBG.SetActive(true);
                InputChooseYearEdit.SetActive(true);
                InputChooseYearEditField.ActivateInputField();
                InputChooseYearStatic.SetActive(false);
                placeholderText.text = LocalizeManager.Localize("$TAF_Ui_Dockyard_YearInput_PlaceHolderText");
            }));
            InputChooseYearEditField.onValidateInput = null;
            InputChooseYearEditField.onValueChange.AddListener(new System.Action<string>((string value) =>
            {
                int _ = 0;

                if (value.Length > 0 && !int.TryParse("" + value[^1], out _))
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Invalid: `{value[^1]}`");
                    InputChooseYearEditField.text = InputChooseYearEditField.text.Substring(0, InputChooseYearEditField.text.Length - 1);
                }
            }));
            InputChooseYearEditField.onEndEdit.AddListener(new System.Action<string>((string value) =>
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");
                InputChooseYearBG.SetActive(false);
                InputChooseYearEdit.SetActive(false);
                InputChooseYearEditField.DeactivateInputField();
                InputChooseYearStatic.SetActive(true);

                int parsedYear = 0;

                if (value.Length == 0 || !int.TryParse(value, out parsedYear) || parsedYear == G.ui.sharedDesignYear || parsedYear < 1890 || parsedYear > 1950)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{parsedYear}`");
                    InputChooseYearEditField.text = G.ui.sharedDesignYear.ToString();
                    InputChooseYearStaticText.text = G.ui.sharedDesignYear.ToString();
                }
                else
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{parsedYear}`");
                    G.ui.sharedDesignYear = parsedYear;
                    GameManager.Instance.RefreshSharedDesign(parsedYear, G.ui.sharedDesignPlayer);
                    InputChooseYearStaticText.text = value;
                }
            }));
        }

        public static GameObject RotationIncramentControl;
        public static Button RotationIncramentControlButton;
        public static Text RotationIncramentControlText;
        public static float RotationIncramentControlTextLastValue = 0;

        public static void CreateTopBarRotationButton()
        {
            Ui ui = G.ui;

            GameObject template = ui.conUpperButtons.GetChild("Layout").GetChild("Undo");
            RotationIncramentControl = GameObject.Instantiate(template);
            HorizontalLayoutGroup group = ui.conUpperButtons.GetChild("Layout").GetComponent<HorizontalLayoutGroup>();
            RotationIncramentControl.transform.SetParent(group.transform, false);//ui.conUpperButtons.GetChild("Layout"));
            RotationIncramentControl.transform.SetSiblingIndex(ui.conUpperButtons.GetChild("Layout").GetChildren().Count - 4);
            RotationIncramentControl.name = "TAF_Rotation_Increment_Control";
            AddTooltip(RotationIncramentControl, "$TAF_tooltip_rotation_increment_control");
            RotationIncramentControlText = RotationIncramentControl.GetChild("Text").GetComponent<Text>();
            RotationIncramentControl.GetChild("Text").TryDestroyComponent<LocalizeText>();
            RotationIncramentControlButton = RotationIncramentControl.GetComponent<Button>();
            RotationIncramentControlButton.onClick.RemoveAllListeners();
            RotationIncramentControlButton.onClick.AddListener(new System.Action(() =>
            {
                Patch_Ui.UpdateRotationIncrament();
            }));
            LayoutElement layout = RotationIncramentControl.GetComponent<LayoutElement>();
            layout.preferredWidth = 145;
        }

        public static void UpdateTopBarRotationButton()
        {
            if (RotationIncramentControlButton.interactable && Patch_Ui.FixedRotationValue) RotationIncramentControlButton.Interactable(false);
            else if(!RotationIncramentControlButton.interactable && !Patch_Ui.FixedRotationValue) RotationIncramentControlButton.Interactable(true);

            if (RotationIncramentControlTextLastValue == Patch_Ui.RotationValue) return;

            RotationIncramentControlTextLastValue = Patch_Ui.RotationValue;
            
            RotationIncramentControlText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_Dockyard_TopBar_RotationIncrementControl"), Patch_Ui.RotationValue) + "\u00B0";
        }

        public static GameObject RotationValueControl;
        public static Button RotationValueControlButton;
        public static Text RotationValueControlText;
        public static float RotationValueControlTextLastValue = 1e10f;
        public static bool RotationValueControlTextLastWasMounted = false;

        public static void CreateTopBarRotationText()
        {
            Ui ui = G.ui;

            GameObject template = ui.conUpperButtons.GetChild("Layout").GetChild("Undo");
            RotationValueControl = GameObject.Instantiate(template);
            HorizontalLayoutGroup group = ui.conUpperButtons.GetChild("Layout").GetComponent<HorizontalLayoutGroup>();
            RotationValueControl.transform.SetParent(group.transform, false);
            RotationValueControl.transform.SetSiblingIndex(ui.conUpperButtons.GetChild("Layout").GetChildren().Count - 4);
            RotationValueControl.name = "TAF_Rotation_Value_Control";
            AddTooltip(RotationValueControl, "$TAF_tooltip_rotation_value_control");
            RotationValueControlButton = RotationValueControl.GetComponent<Button>();
            RotationValueControlButton.onClick.RemoveAllListeners();
            RotationValueControlButton.onClick.AddListener(new System.Action(() =>
            {
                Patch_Ui.AutoOrient();
            }));
            LayoutElement layout = RotationValueControl.GetComponent<LayoutElement>();
            layout.preferredWidth = 145;
            RotationValueControlText = RotationValueControl.GetChild("Text").GetComponent<Text>();
            RotationValueControl.GetChild("Text").TryDestroyComponent<LocalizeText>();
        }

        public static void UpdateTopBarRotationText()
        {
            if (RotationValueControlButton.interactable && (Patch_Ui.SelectedPart == null || Patch_Ui.FixedRotationValue)) RotationValueControlButton.Interactable(false);
            else if(!RotationValueControlButton.interactable && !(Patch_Ui.SelectedPart == null || Patch_Ui.FixedRotationValue)) RotationValueControlButton.Interactable(true);

            string RotationValue;

            float hash = Patch_Ui.MountedPartRotation * 1000000 + Patch_Ui.DefaultRotation * 1000 + Patch_Ui.PartRotation;

            if (RotationValueControlTextLastValue == hash && RotationValueControlTextLastWasMounted == Patch_Ui.Mounted) return;

            RotationValueControlTextLastValue = hash;
            RotationValueControlTextLastWasMounted = Patch_Ui.Mounted;

            if (Patch_Ui.Mounted)
            {
                if (Il2CppSystem.Math.Sign(Patch_Ui.MountedPartRotation) == 1 || (int)(Il2CppSystem.Math.Abs(Patch_Ui.MountedPartRotation)) == 0)
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(Patch_Ui.MountedPartRotation)}\u00B0 + {(int)(Patch_Ui.DefaultRotation + 0.5f)}\u00B0";
                }
                else
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(Patch_Ui.MountedPartRotation + 360)}\u00B0 + {(int)Patch_Ui.DefaultRotation + 0.5f}\u00B0";
                }
            }
            else
            {
                if (Il2CppSystem.Math.Sign(Patch_Ui.PartRotation) == 1 || (int)(Il2CppSystem.Math.Abs(Patch_Ui.PartRotation)) == 0)
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(Patch_Ui.PartRotation)}\u00B0";
                }
                else
                {
                    RotationValue = $"{Il2CppSystem.Math.Abs(Patch_Ui.PartRotation + 360)}\u00B0";
                }
            }

            RotationValueControlText.text = String.Format(LocalizeManager.Localize("$TAF_Ui_Dockyard_TopBar_RotationValueControl"), RotationValue);
        }

        public static GameObject ArmorQualityButton;
        public static float ArmourQuality;

        public static void CreateArmorQualityButton()
        {
            Ui ui = G.ui;

            GameObject template = ui.conUpperButtons.GetChild("Layout").GetChild("Undo");
            ArmorQualityButton = GameObject.Instantiate(template);
            GameObject parent = ui.constructorUi.GetChild("Left").GetChild("Scroll View").GetChild("Viewport").GetChild("Cont").GetChild("FoldArmor").GetChild("Armor");
            ArmorQualityButton.transform.SetParent(parent.transform, false);
            ArmorQualityButton.transform.SetSiblingIndex(2);
            ArmorQualityButton.name = "TAF_Armour_Quality_Button";
            AddTooltip(ArmorQualityButton, "$TAF_tooltip_update_armor_quality_setting");
            Button button = ArmorQualityButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            LayoutElement layout = ArmorQualityButton.GetComponent<LayoutElement>();
            layout.preferredWidth = 100;
            layout.preferredHeight = 15;

            SetLocalizedTextTag(ArmorQualityButton.GetChild("Text"), "$TAF_Ui_Dockyard_ArmorTab_UpdateArmorQualitySetting");

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(new System.Action(() =>
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Updating armor preview value to {ArmourQuality}.");
                GameObject OptionsMenu = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window");
                GameObject ArmorQualitySlider = ModUtils.GetChildAtPath("Root/RightSide/General/Viewport/Content/ArmorQualityInPenetrationData/ArmorQualityInPenetrationDataSlider", OptionsMenu);
                Slider ArmorQualitySliderContent = ArmorQualitySlider.GetComponent<Slider>();
                ArmorQualitySliderContent.value = ArmourQuality;
                G.settings.Save();
            }));
        }

        public static void UpdateArmorQualityButton()
        {
            if (Patch_Ship.LastCreatedShip != null)
            {
                ArmourQuality = 0;

                foreach (TechnologyData tech in Patch_Ship.LastCreatedShip.techsActual)
                {
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

            if (Patch_Ship.LastCreatedShip == null || (int)(G.settings.armorQualityInPen + 0.05f) == ArmourQuality)
            {
                ArmorQualityButton.GetComponent<Button>().SetActive(false);
            }
            else
            {
                ArmorQualityButton.GetComponent<Button>().SetActive(true);
            }

        }

        // ========== SETTINGS ========== //

        private static void ApplySettingsMenuModifications()
        {
            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/Sound/Viewport/Content/General Volume

            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/Graphic Options/Viewport/Content

            GameObject SettingsRoot = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window/Root");

            SettingsRoot.GetParent().transform.SetSiblingIndex(0);

            GameObject GraphicsOptionsContent = ModUtils.GetChildAtPath("RightSide/Graphic Options/Viewport/Content", SettingsRoot);

            ModifyUi(GraphicsOptionsContent).SetChildOrder(
                "Quality", "Resolution", "UI Scale", "Fullscreen Mode", "VSync", "Post Effects", "Shadow Details", "Anti Aliasing", "FXAA", "Anisotropic", "FPS", "Textures"
            );

            // Global/Ui/UiMain/Popup/Options Window/Root/Reset
            AddConfirmPopupToButton(
                ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window/Root/Reset").GetComponent<Button>(),
                "$TAF_Ui_Settings_Confirm_Reset"
            );

            GameObject uiScaleSlider = GameObject.Instantiate(ModUtils.GetChildAtPath("RightSide/Sound/Viewport/Content/General Volume", SettingsRoot));
            uiScaleSlider.transform.localPosition = new Vector3();
            uiScaleSlider.transform.SetParent(GraphicsOptionsContent.GetComponent<LayoutGroup>().transform);
            uiScaleSlider.transform.SetScale(1, 1, 1);
            uiScaleSlider.name = "UI Scale";
            SetLocalizedTextTag(uiScaleSlider.GetChild("Label"), "$TAF_Ui_Popup_OptionsWindow_UI_Scale");
            Slider uiScaleSliderControl = uiScaleSlider.GetChild("Campaign Slider").GetComponent<Slider>();
            uiScaleSliderControl.onValueChanged.RemoveAllListeners();

            Canvas canvas = G.ui.gameObject.GetComponent<Canvas>();

            if (TAF_Settings.settings.uiScaleDefault < 0)
            {
                TAF_Settings.settings.uiScaleDefault = canvas.scaleFactor;
                TAF_Settings.settings.uiScale = canvas.scaleFactor;
                SaveSettings();
            }

            uiScaleSliderControl.value = TAF_Settings.settings.uiScale;
            uiScaleSliderControl.minValue = 1;
            uiScaleSliderControl.maxValue = 4;

            canvas.scaleFactor = uiScaleSliderControl.value;

            uiScaleSliderControl.onValueChanged.AddListener(new System.Action<float>((float value) =>
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Slider value changed: {value} / {TAF_Settings.settings.uiScaleDefault}");
                if (value > TAF_Settings.settings.uiScaleDefault - 0.1f && value < TAF_Settings.settings.uiScaleDefault + 0.1f)
                {
                    uiScaleSliderControl.value = TAF_Settings.settings.uiScaleDefault;
                }
            }));

            uiScaleSliderControl.OnMouseUp(new System.Action(() =>
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Setting UI scale to: {uiScaleSliderControl.value}");
                canvas.scaleFactor = uiScaleSliderControl.value;
                TAF_Settings.settings.uiScale = uiScaleSliderControl.value;
                SetSaveSettingsButtonActive();
            }));
        }

        public static void SetSaveSettingsButtonActive()
        {
            GameObject SaveButton = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window/Root/Apply");
            SaveButton.GetComponent<Button>().interactable = true;
        }

        public class TAF_Settings
        {
            public static TAF_Settings settings;
            public static int CurrentSettingsVersion = 1;
            public int version { get; set; }
            public float uiScale { get; set; }
            public float uiScaleDefault { get; set; }

            public TAF_Settings()
            {
                version = CurrentSettingsVersion;
                uiScale = 2f;
                uiScaleDefault = -1;
            }
        }

        public static void LoadSettings()
        {
            FilePath SavePath = new FilePath(FilePath.DirType.AppDataDir, "TAF_settings.json");

            if (SavePath.Exists)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Loading settings from {SavePath.path}...");

                try
                {
                    TAF_Settings.settings = Serializer.JSON.LoadJsonFile<TAF_Settings>(SavePath.path);

                    if (TAF_Settings.settings == null)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Settings file corrupted, resetting file.");
                        TAF_Settings.settings = new TAF_Settings();
                    }
                    else if (TAF_Settings.settings.version != TAF_Settings.CurrentSettingsVersion)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Settings file out of date, resetting file. File: {TAF_Settings.settings.version} != Latest: {TAF_Settings.CurrentSettingsVersion}");
                        TAF_Settings.settings = new TAF_Settings();
                    }
                    else
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"         version : {TAF_Settings.settings.version}");
                        Melon<TweaksAndFixes>.Logger.Msg($"         uiScale : {TAF_Settings.settings.uiScale}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  uiScaleDefault : {TAF_Settings.settings.uiScaleDefault}");
                    }
                }
                catch (Exception e)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Settings file corrupted, resetting file.");
                    TAF_Settings.settings = new TAF_Settings();
                }
            }
            else
            {
                TAF_Settings.settings = new TAF_Settings();
            }
        }

        public static void SaveSettings()
        {
            FilePath SavePath = new FilePath(FilePath.DirType.AppDataDir, "TAF_settings.json");
            Melon<TweaksAndFixes>.Logger.Msg($"Saving settings to {SavePath.path}...");
            Serializer.JSON.SaveJsonFile<TAF_Settings>(SavePath.path, TAF_Settings.settings);
        }

        public static void CheckForPeace(Ui _this)
        {
            int monthsForLowVPWarEnd = Config.Param("taf_war_max_months_for_low_vp_war", 12);
            int monthsForEconCollapse = Config.Param("taf_war_min_months_for_econ_collapse_peace", 24);
            float lowVPThreshold = Config.Param("taf_war_low_vp_threshold", 0f);

            float peace_min_vp_difference = MonoBehaviourExt.Param("peace_min_vp_difference", 10000f);
            float peace_enemy_vp_ratio = MonoBehaviourExt.Param("peace_enemy_vp_ratio ", 2f);
            float peace_vp_sum_prolonged_war = MonoBehaviourExt.Param("peace_vp_sum_prolonged_war", 150000f);

            var CD = CampaignController.Instance.CampaignData;

            foreach (var rel in CD.Relations.Values)
            {
                if (!rel.isWar)
                    continue;

                Player a, b;
                float vpA, vpB;
                bool hasHuman;
                if (rel.b == PlayerController.Instance)
                {
                    a = rel.b;
                    b = rel.a;
                    vpA = rel.victoryPointsB;
                    vpB = rel.victoryPointsA;
                    hasHuman = true;
                }
                else
                {
                    a = rel.a;
                    b = rel.b;
                    vpA = rel.victoryPointsA;
                    vpB = rel.victoryPointsB;
                    hasHuman = rel.a == PlayerController.Instance;
                }

                // Early check: If the war has gone on for too long with no VP, just call for peace
                int turnsSinceStart = CampaignController.Instance.CurrentDate.MonthsPassedSince(rel.recentWarStartDate);

                if (vpA + vpB < lowVPThreshold && turnsSinceStart > monthsForLowVPWarEnd)
                {
                    _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$TAF_Ui_War_WhitePeace"), vpA >= vpB);
                    continue;
                }

                int turnsSinceCheck = CampaignController.Instance.CurrentDate.MonthsPassedSince(rel.LastTreatyCheckDate);
                var checkThresh = rel.TreatyCheckMonthTreashold;
                if (checkThresh == 0)
                {
                    checkThresh = Config.Param("war_min_duration", 5);
                    rel.TreatyCheckMonthTreashold = checkThresh;
                }
                if (turnsSinceCheck < checkThresh)
                    continue;

                Player loserPlayer = null;
                if (Mathf.Abs(vpB - vpA) >= peace_min_vp_difference && Mathf.Max((vpB + 1f) / (vpA + 1f), (vpB + 1f) / (vpA + 1f)) >= peace_enemy_vp_ratio && vpA + vpB >= peace_vp_sum_prolonged_war)
                {
                    loserPlayer = vpB > vpA ? a : b;
                }
                else if (turnsSinceStart >= monthsForEconCollapse)
                {
                    var wgeA = a.WealthGrowthEffective();
                    var wgeB = b.WealthGrowthEffective();
                    if (wgeA <= 0)
                    {
                        if (wgeB <= 0)
                        {
                            var aRel = CampaignControllerM.GetAllianceRelation(a, b);

                            float vpAA, vpAB;
                            if (aRel.A.Players.Contains(a.data))
                            {
                                vpAA = aRel.vpA;
                                vpAB = aRel.vpB;
                            }
                            else
                            {
                                vpAA = aRel.vpB;
                                vpAB = aRel.vpA;
                            }
                            if (vpAA > vpAB)
                                loserPlayer = b;
                            else if (vpAA < vpAB)
                                loserPlayer = a;
                            else if (vpA > vpB)
                                loserPlayer = b;
                            else if (vpA < vpB)
                                loserPlayer = a;
                            else
                                loserPlayer = UnityEngine.Random.Range(0, 1) == 0 ? a : b;
                        }

                        if (vpB > vpA
                            || (CampaignControllerM.GetAllianceRelation(a, b) is AllianceRelation alRel
                                && (alRel.A.Players.Contains(a.data) ?
                                    (alRel.vpB > alRel.vpA)
                                    : (alRel.vpA > alRel.vpB))))
                        {
                            loserPlayer = a;
                        }
                    }
                    else if (wgeB <= 0)
                    {
                        if (vpA > vpB
                            || (CampaignControllerM.GetAllianceRelation(a, b) is AllianceRelation alRel
                                && (alRel.A.Players.Contains(a.data) ?
                                    (alRel.vpA > alRel.vpB)
                                    : (alRel.vpB > alRel.vpA))))
                        {
                            loserPlayer = a;
                        }
                    }
                }

                if (loserPlayer != null)
                {
                    if (loserPlayer == a)
                        _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$Ui_World_TheWarIsNotGoingWellThe") + "{0} {1}" + LocalizeManager.Localize("$Ui_World_asksYouShouldAskUnfPeace"), false);
                    else
                        _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$Ui_World_WeAreWinningSnThe") + "{0} {1}" + LocalizeManager.Localize("$Ui_World_desperAsksPeaceTreaty"), true);
                }
            }
        }


        // private static Slider beamSliderComp;
        // 
        // public static void OnConstructorShipChanged()
        // {
        //     Ship ship = ShipM.GetActiveShip();
        // 
        //     Melon<TweaksAndFixes>.Logger.Msg($"OnConstructorShipChanged");
        // 
        //     if (ship == null) return;
        // 
        //     Melon<TweaksAndFixes>.Logger.Msg($"{ship.hull.data.beamMin} ~ {ship.hull.data.beamMax}");
        // 
        //     beamSliderComp.minValue = ship.hull.data.beamMin * 10;
        //     beamSliderComp.maxValue = ship.hull.data.beamMax * 10;
        //     beamSliderComp.value = ship.beam * 10;
        // }

    }
}
