using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2CppUiExt;
using UnityEngine.EventSystems;

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

        /// <summary>
        /// Re-enable the TAF_LoadGame button when the options menu (PopupMenu) is open.
        /// Call at end of Ui.Update so it runs after any game logic that disables our button.
        /// </summary>
        public static void EnsureLoadButtonEnabled()
        {
            GameObject activeWindow = GetActivePopupMenuWindow();
            if (activeWindow == null) return;
            GameObject ourBtn = activeWindow.GetChild("TAF_LoadGame");
            if (ourBtn == null)
            {
                for (int i = 0; i < activeWindow.transform.childCount; i++)
                {
                    GameObject c = activeWindow.transform.GetChild(i).gameObject;
                    if (c.name == "TAF_LoadGame" || c.name.StartsWith("TAF_LoadGame"))
                    { ourBtn = c; break; }
                }
            }
            if (ourBtn != null)
            {
                ourBtn.SetActive(true);
                if (ourBtn.TryGetComponent(out Button b))
                    b.interactable = true;
            }
        }

        /// <summary>
        /// Returns the Window of the currently active PopupMenu (template or clone under PopWindows).
        /// Clone name is typically "PopupMenu(Clone)".
        /// </summary>
        private static GameObject GetActivePopupMenuWindow()
        {
            GameObject template = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu");
            if (template != null && template.active)
            {
                GameObject w = template.GetChild("Window");
                if (w != null) return w;
            }
            GameObject popWindows = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows");
            if (popWindows != null)
            {
                for (int i = 0; i < popWindows.transform.childCount; i++)
                {
                    GameObject child = popWindows.transform.GetChild(i).gameObject;
                    if (child.active && (child.name == "PopupMenu" || child.name.StartsWith("PopupMenu(")))
                    {
                        GameObject w = child.GetChild("Window");
                        if (w != null) return w;
                        break;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Add a "Load Game" button to the options menu (PopupMenu) after SaveCampaign; click opens the load-game popup.
        /// Registers an update to keep the button enabled when the menu is shown (template or clone).
        /// </summary>
        public static void AddLoadButton()
        {
            GameObject popupMenu = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu");
            if (popupMenu == null) return;
            GameObject window = popupMenu.GetChild("Window");
            if (window == null) return;
            GameObject buttonTemplate = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu/Window/ButtonBase");
            if (buttonTemplate == null) return;

            GameObject after = window.GetChild("SaveCampaign");
            int siblingIndex = after != null ? after.transform.GetSiblingIndex() + 1 : -1;

            GameObject btn = GameObject.Instantiate(buttonTemplate);
            btn.transform.SetParent(window, false);
            btn.name = "TAF_LoadGame";
            btn.SetActive(true);
            btn.transform.localPosition = Vector3.zero;
            btn.transform.localScale = Vector3.one;

            GameObject textObj = btn.GetChild("Text (TMP)");
            if (textObj != null)
            {
                textObj.TryDestroyComponent<LocalizeText>();
                if (textObj.TryGetComponent(out TMP_Text tmp))
                    tmp.text = "Load Game";
            }
            if (btn.TryGetComponent(out Button buttonComp))
            {
                buttonComp.onClick.RemoveAllListeners();
                buttonComp.onClick.AddListener(new System.Action(() =>
                {
                    GameObject activeWindow = GetActivePopupMenuWindow();
                    Button loadButton = null;
                    if (activeWindow != null)
                    {
                        GameObject popupMenu = activeWindow.transform.parent != null ? activeWindow.transform.parent.gameObject : null;
                        if (popupMenu != null)
                            popupMenu.SetActive(false);
                        GameObject loadBtn = activeWindow.GetChild("LoadCampaign");
                        if (loadBtn != null && loadBtn.TryGetComponent(out Button lb))
                            loadButton = lb;
                    }
                    if (loadButton != null)
                        loadButton.onClick.Invoke();
                    else
                    {
                        GameObject saveWindow = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/SaveWindow");
                        if (saveWindow != null)
                            saveWindow.SetActive(true);
                    }
                }));
            }
            if (siblingIndex >= 0)
                btn.transform.SetSiblingIndex(siblingIndex);
        }

        ////////////////////// MODIFICATIONS //////////////////////



        public static void ApplyUiModifications()
        {
            CreateBailoutPopup();

            CreateHidePopupsButton();

            ApplyMainMenuModifications();

            ApplySettingsMenuModifications();

            ApplyCampaignWindowModifications();
            ApplyDockyardModifications();

            AddLoadButton();

            G.GameData.tooltips["file_converter"] = new TooltipData();

            G.GameData.tooltips["file_converter"].name = "file_converter";
            // G.GameData.tooltips["file_converter"].title = "";
            G.GameData.tooltips["file_converter"].text = "$toolltip_file_converter";

            // Global/Ui/UiMain/Loading/LayoutDesc/Desc/DescText

            // UiM.ModifyUi(ModUtils.GetChildAtPath("Global/Ui/UiMain/Loading/LayoutDesc/Desc/DescText"));

            var loadingScreenText = ModUtils.GetChildAtPath("Global/Ui/UiMain/Loading/LayoutDesc/Desc/DescText").GetComponent<TMP_Text>();
            loadingScreenText.alignment = TextAlignmentOptions.Center;


            ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows/PortPopupSmall").transform.SetSiblingIndex(16);

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

        public static void UpdateConstructorUi()
        {
            Text SaveBtnText = G.ui.conUpperButtons.GetChild("Layout").GetChild("Save").GetChild("Text").GetComponent<Text>();
            if (GameManager.IsCustomBattle)
                SaveBtnText.text = ModUtils.LocalizeF("$Ui_Constr_SaveDesign");

            var speedSlider = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Speed/Slider").GetComponent<Slider>();

            float inc = Config.Param("speed_step", 0.1f);

            // Melon<TweaksAndFixes>.Logger.Msg($"  Inc: `{inc}`");

            speedSlider.minValue = G.ui.mainShip.shipType.speedMin / inc;
            speedSlider.maxValue = G.ui.mainShip.shipType.speedMax / inc;

            // Melon<TweaksAndFixes>.Logger.Msg($"  Min / Max = {speedSlider.minValue} / {speedSlider.maxValue}");

            speedSlider.onValueChanged.RemoveAllListeners();
            speedSlider.onValueChanged.AddListener(new System.Action<float>((float f) => {
                
                if (!G.ui.allowEdit)
                    return;

                f *= inc;

                // Melon<TweaksAndFixes>.Logger.Msg($"Changed: {f}");

                f = Mathf.Clamp(f,
                    G.ui.mainShip.shipType.speedMin,
                    G.ui.mainShip.shipType.speedMax
                );

                // Melon<TweaksAndFixes>.Logger.Msg($"  Set speed: {f}");

                G.ui.mainShip.SetSpeedMax(f * 0.51444399f);

                G.ui.OnConShipChanged(true);
            }));
            
            speedSlider.OnMouseUp(new System.Action(() => {}));

            var speedText = ModUtils
                .GetChildAtPath(
                "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Speed/Text"
                ).GetComponent<Text>();

            var speedEdit = ModUtils
                .GetChildAtPath(
                "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/Speed/Edit"
                ).GetComponent<InputField>();
            
            float lastGoodValue = Mathf.Round((G.ui.mainShip.speedMax / 0.51444399f) / inc) * inc;
            // speedText.text = $"{lastGoodValue}";
            // speedEdit.text = $"{lastGoodValue}";

            var speedTextOnClick = speedText.gameObject.GetComponent<OnClickH>();
            speedTextOnClick.action = new System.Action<PointerEventData>((PointerEventData ev) => {

                if (!G.ui.allowEdit)
                    return;

                // Melon<TweaksAndFixes>.Logger.Msg($"Clicked!");
                speedText.gameObject.SetActive(false);
                speedEdit.gameObject.SetActive(true);
                speedEdit.Select();
                speedEdit.ActivateInputField();

                int numZeros = (int)(Math.Log10(1f / inc) + 1f);

                // Melon<TweaksAndFixes>.Logger.Msg($"{numZeros}");
                speedEdit.SetText(lastGoodValue.ToString($"0:0.{new String('0', numZeros)}"));
            });

            speedEdit.onEndEdit.RemoveAllListeners();
            speedEdit.onEndEdit.AddListener(new System.Action<string>((string value) =>
            {
                if (!G.ui.allowEdit)
                    return;

                // Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");
                speedEdit.gameObject.SetActive(false);
                speedEdit.DeactivateInputField();
                speedText.gameObject.SetActive(true);

                float parsedSpeed = 0;
                
                if (value.Length == 0 || !ModUtils.TryParse(value, out parsedSpeed))
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{parsedSpeed}`");
                }
                else
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{parsedSpeed}`");
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Stepped: `{Math.Round(parsedSpeed / inc) * inc}`");

                    parsedSpeed = Mathf.Clamp(
                        Mathf.Round(parsedSpeed / inc) * inc,
                        G.ui.mainShip.shipType.speedMin,
                        G.ui.mainShip.shipType.speedMax
                    );

                    G.ui.mainShip.SetSpeedMax(parsedSpeed * 0.51444399f);

                    lastGoodValue = parsedSpeed;

                    G.ui.OnConShipChanged(true);
                }
            }));
        }

        public static void AddConfirmPopupToButton(Button button, string? text = default, System.Action? before = null, System.Action? after = null, bool invokeOnYes = true, bool invokeOnNo = false)
        {
            if (button.onClick.PrepareInvoke().Count == 1)
            {
                if (text == default || text == null)
                {
                    // TODO: Localize (not that it gets used anywhere)
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
                            if (invokeOnYes)
                                baseCall.Invoke(
                                    new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Object>(
                                        System.Array.Empty<Il2CppSystem.Object>()
                                    )
                                );
                            if (after != null) after.Invoke();
                        }),
                        new System.Action(() =>
                        {
                            if (invokeOnNo)
                                baseCall.Invoke(
                                    new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Object>(
                                        System.Array.Empty<Il2CppSystem.Object>()
                                    )
                                );
                        })
                    );
                }));

                // Empty event is a marker that we visited this button already
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
                if (child.name == "CloneShip") continue;
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

        // ========== MAIN MENU ========== //

        public static void ApplyMainMenuModifications()
        {
            ApplySkirmishSetupModifications();

            G.ui.NewGameWindow.ChangeFleetCreation(1);
            G.ui.NewGameWindow.ChangeDesignUsage(1);
            G.ui.NewGameWindow.ChangeSharedDesigns(1);
        }

        public class SkirmishSetupMod
        {
            public class SkirmishPlayer
            {
                public Dictionary<ShipType, Dictionary<Guid, int>> shipAmounts = new();
                public Dictionary<Guid, Ship> shipInstances = new();
                public Dictionary<Guid, Ship.Store> shipDesigns = new();
                public Dictionary<ShipType, bool> shipTypeAvailible = new();
                public int year = 1890;
                public PlayerData player = null;

                public void ClearShips()
                {
                    shipInstances.Clear();
                    shipDesigns.Clear();

                    foreach (var type in G.GameData.shipTypes)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"{type.Key}");

                        if (type.Value.paramx.ContainsKey("no_build"))
                            continue;

                        if (!shipAmounts.ContainsKey(type.Value))
                            continue;

                        shipAmounts[type.Value].Clear();
                    }
                }
            }

            public bool initialized = false;
            public SkirmishPlayer player1 = new();
            public SkirmishPlayer player2 = new();
            public DayCycleAndWeather.TimesOfDay daytime = DayCycleAndWeather.TimesOfDay.Day;
            public DayCycleAndWeather.WeatherType weather = DayCycleAndWeather.WeatherType.Clear;
            public int distance = 10000;
            public bool useShared = true;
            public bool usePredefs = true;

            public void Randomize()
            {
                player1.ClearShips();
                player2.ClearShips();

                float fleetSizeMod = (float)System.Random.Shared.NextDouble() * 0.5f + 0.5f;

                for (int i = 0; i < 2; i++)
                {
                    var currPlayer = i == 0 ? player1 : player2;

                    foreach (var type in G.GameData.shipTypes)
                    {
                        if (type.Value.paramx.ContainsKey("no_build"))
                            continue;

                        Melon<TweaksAndFixes>.Logger.Msg($"{type.Key}");

                        int max = ModUtils.toInt(type.Value.buildRatio * fleetSizeMod * 1.25f);
                        int amount = ModUtils.toInt(System.Random.Shared.NextDouble() * max + 0.5f);

                        Melon<TweaksAndFixes>.Logger.Msg($"  {amount} / {max} / {type.Value.buildRatio}");

                        if (amount == 0 || amount > type.Value.buildRatio * fleetSizeMod)
                            continue;

                        int numClasses = 1 + ModUtils.toInt(System.Random.Shared.NextDouble() * 2.5f * Mathf.Log10(amount));
                        int avgPerClass = amount / numClasses;

                        Melon<TweaksAndFixes>.Logger.Msg($"  {numClasses} | {avgPerClass}");

                        var counts = currPlayer.shipAmounts.ValueOrNew(type.Value);

                        for (int k = 0; k < 10 && amount > 0; k++)
                        {
                            int num = 1 + ModUtils.toInt((0.25f + System.Random.Shared.NextDouble() * 0.75f) * (avgPerClass - 1) * 2f);

                            if (amount >= num)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"  {num} / {amount}");

                                amount -= num;
                            }
                            else
                            {
                                num = amount;

                                Melon<TweaksAndFixes>.Logger.Msg($"  {num} / {amount}");

                                amount = 0;
                            }

                            counts.Add(Guid.NewGuid(), num);
                        }
                    }
                }
            }

            public void Clear()
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Clearing");

                skirmishSetupMod.weather = DayCycleAndWeather.WeatherType.Clear;
                skirmishSetupMod.daytime = DayCycleAndWeather.TimesOfDay.Day;
                skirmishSetupMod.distance = 10000;
                skirmishSetupMod.player1.year = 1890;
                skirmishSetupMod.player1.ClearShips();
                skirmishSetupMod.player2.year = 1890;
                skirmishSetupMod.player2.ClearShips();

                ApplyToSK();
            }

            public void ApplyToSK()
            {
                var sk = G.ui.skirmishSetup;
                var skm = UiM.skirmishSetupMod;

                if (!skm.initialized)
                {
                    foreach (var type in G.GameData.shipTypes)
                    {
                        skm.player1.shipAmounts.ValueOrNew(type.Value);
                        skm.player2.shipAmounts.ValueOrNew(type.Value);
                    }

                    skm.initialized = true;
                }

                sk.daytime = skm.daytime;
                sk.weather = skm.weather;
                sk.distance = skm.distance;
                sk.player1.year = skm.player1.year;
                sk.player2.year = skm.player2.year;

                foreach (var type in G.GameData.shipTypes)
                {
                    if (!skm.player1.shipAmounts.ContainsKey(type.Value)
                        || !skm.player2.shipAmounts.ContainsKey(type.Value))
                        continue;

                    sk.player1.shipAmounts.AddOrSet(type.Value, skm.player1.shipAmounts[type.Value].Count);
                    sk.player2.shipAmounts.AddOrSet(type.Value, skm.player2.shipAmounts[type.Value].Count);
                }

                BattleManager.Instance.QueryCustomBattleAvailableHulls(
                    sk, out var p1IsHullAvailible, out var p2IsHullAvailible
                );

                foreach (var type in G.GameData.shipTypes)
                {
                    if (p1IsHullAvailible.ContainsKey(type.Value))
                        sk.player1.isHullAvailable.AddOrSet(type.Value, p1IsHullAvailible[type.Value]);
                    if (p2IsHullAvailible.ContainsKey(type.Value))
                        sk.player2.isHullAvailable.AddOrSet(type.Value, p2IsHullAvailible[type.Value]);
                }

                G.ui.Refresh();
            }

            public bool InitializePlayerMadeShips()
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Reiniting Player Ships:");

                // TODO: Store a full copy of each ship in DontDestroyOnLoad to copy from.
                //       The game deletes the pointers to our ship/store objects.
                //       This is required for us to do multi-year stuff.

                // TODO: Stopgap = just remove design refferences we can't find in the full shiplist.

                // player1.shipInstances.Clear();

                bool foundAll = true;

                foreach (var design in player1.shipDesigns.ToList())
                {
                    // if (player1.shipInstances.HasValue(design.Key))
                    //     continue;

                    // var p = G.ui.skirmishSetup.player1.country.Player();
                    // 
                    // var ship = Ship.Create(null, p, false, false, false);
                    // var guidRet = new Il2CppSystem.Nullable<Il2CppSystem.Guid>();
                    // if (!ship.FromStore(design.Value, guidRet, null, p, false))
                    // {
                    //     Melon<TweaksAndFixes>.Logger.Error($"Couldn't load {design.Value.vesselName} ({design.Value.hullName}, {design.Value.YearCreated})");
                    //     ship.Erase();
                    //     continue;
                    // }

                    Melon<TweaksAndFixes>.Logger.Msg($"  Ship: {player1.shipInstances.ValOrDef(design.Key, null)?.Name(false, false) ?? "NULL!"}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  Design: {design.Value.vesselName}");

                    bool success = false;

                    foreach (var s in ShipM.GetAllShips())
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"    {s.Name(false, false)} : {s.id}");

                        if (s.id == design.Value.id)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      MATCH!");
                            player1.shipInstances.AddOrSet(design.Key, s);
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        player1.shipInstances.Remove(design.Key);
                        player1.shipDesigns.Remove(design.Key);

                        // foundAll = false;
                    }
                }

                // player2.shipInstances.Clear();

                Melon<TweaksAndFixes>.Logger.Msg($"Reiniting Enemy Ships:");

                foreach (var design in player2.shipDesigns.ToList())
                {
                    // if (player2.shipInstances.HasValue(design.Key))
                    //     continue;

                    // var p = G.ui.skirmishSetup.player2.country.Player();
                    // 
                    // var ship = Ship.Create(null, p, false, false, false);
                    // var guidRet = new Il2CppSystem.Nullable<Il2CppSystem.Guid>();
                    // if (!ship.FromStore(design.Value, guidRet, null, p, false))
                    // {
                    //     Melon<TweaksAndFixes>.Logger.Error($"Couldn't load {design.Value.vesselName} ({design.Value.hullName}, {design.Value.YearCreated})");
                    //     ship.Erase();
                    //     continue;
                    // }

                    Melon<TweaksAndFixes>.Logger.Msg($"  Ship: {player2.shipInstances.ValOrDef(design.Key, null)?.Name(false, false) ?? "NULL!"}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  Design: {design.Value.vesselName}");

                    bool success = false;

                    foreach (var s in ShipM.GetAllShips())// G.ui.skirmishSetup.player2.country.Player().fleet.ToList())
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"    {s.Name(false, false)} : {s.id}");

                        if (s.id == design.Value.id)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      MATCH!");
                            player2.shipInstances.AddOrSet(design.Key, s);
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        player2.shipInstances.Remove(design.Key);
                        player2.shipDesigns.Remove(design.Key);

                        // foundAll = false;
                    }
                }

                // Melon<TweaksAndFixes>.Logger.Msg($" Found all: {foundAll}");

                return foundAll;
            }

            public void UpdateShip(Ship.Store updated)
            {
                foreach (var design in player1.shipDesigns.ToList())
                {
                    if (design.Value.id == updated.id)
                    {
                        player1.shipInstances.Remove(design.Key);
                        player1.shipDesigns.AddOrSet(design.Key, updated);
                        break;
                    }
                }

                foreach (var design in player2.shipDesigns.ToList())
                {
                    if (design.Value.id == updated.id)
                    {
                        player2.shipInstances.Remove(design.Key);
                        player2.shipDesigns.AddOrSet(design.Key, updated);
                        break;
                    }
                }
            }
        }

        public static SkirmishSetupMod skirmishSetupMod = new SkirmishSetupMod();
        
        public static GameObject playerUi;
        public static GameObject playerUi1;
        public static GameObject fleetList1;
        public static GameObject fleetList1ScrollView;
        public static GameObject fleetList1Container;

        public static GameObject playerUi2;
        public static GameObject fleetList2;
        public static GameObject fleetList2ScrollView;
        public static GameObject fleetList2Container;

        public static GameObject sTypeEntryTemplate;
        public static GameObject sClassEntryTemplate;

        public static void ApplySkirmishSetupModifications()
        {
            GameObject upperButtons = ModUtils.GetChildAtPath("Global/Ui/UiMain/Skirmish/Window/Buttons/ButtonsSub2/");

            GameObject random = ModUtils.GetChildAtPath("Random", upperButtons);
            GameObject tafRandom = GameObject.Instantiate(random);
            tafRandom.transform.SetParent(upperButtons, false);
            tafRandom.transform.SetSiblingIndex(random.transform.GetSiblingIndex());
            tafRandom.GetComponent<Button>().onClick.RemoveAllListeners();
            tafRandom.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
            {
                G.ui.SkirmishSetupRandomize();
                skirmishSetupMod.Randomize();
                G.ui.Refresh();
            }));
            random.active = false;

            GameObject clear = ModUtils.GetChildAtPath("Clear", upperButtons);
            GameObject tafClear = GameObject.Instantiate(clear);
            tafClear.transform.SetParent(upperButtons, false);
            tafClear.transform.SetSiblingIndex(clear.transform.GetSiblingIndex());
            tafClear.GetComponent<Button>().onClick.RemoveAllListeners();
            tafClear.GetComponent<Button>().onClick.AddListener(new System.Action(() =>
            {
                G.ui.SkirmishSetupClear();
                skirmishSetupMod.Clear();
                G.ui.Refresh();
            }));
            clear.active = false;

            // GameObject unlock = ModUtils.GetChildAtPath("Unlock", upperButtons);

            GameObject sharedDesignsBase = ModUtils.GetChildAtPath("Shared Designs", upperButtons);

            GameObject sharedDesigns = GameObject.Instantiate(sharedDesignsBase);
            sharedDesigns.name = "TAF Shared Designs";
            sharedDesigns.transform.SetParent(upperButtons, false);
            sharedDesigns.transform.SetSiblingIndex(5);
            sharedDesigns.GetComponent<LayoutElement>().preferredWidth = 150;
            AddTooltip(sharedDesigns, "$TAF_tooltip_skirmish_shared_design_toggle");
            var sharedDesignsText = sharedDesigns.GetChild("Text").GetComponent<Text>();
            sharedDesignsText.text = ModUtils.LocalizeF(
                    "$TAF_Ui_SkirmishSettup_SharedDesignToggle_" + (skirmishSetupMod.useShared ? "On" : "Off"));
            var sharedDesignsButton = sharedDesigns.GetComponent<Button>();
            sharedDesignsButton.onClick.RemoveAllListeners();
            sharedDesignsButton.onClick.AddListener(new System.Action(() =>
            {
                skirmishSetupMod.useShared = !skirmishSetupMod.useShared;
                sharedDesignsText.text = ModUtils.LocalizeF(
                        "$TAF_Ui_SkirmishSettup_SharedDesignToggle_" + (skirmishSetupMod.useShared ? "On" : "Off"));
            }));

            GameObject predefDesigns = GameObject.Instantiate(sharedDesignsBase);
            predefDesigns.name = "TAF Predef Designs";
            predefDesigns.transform.SetParent(upperButtons, false);
            predefDesigns.transform.SetSiblingIndex(6);
            predefDesigns.GetComponent<LayoutElement>().preferredWidth = 150;
            AddTooltip(predefDesigns, "$TAF_tooltip_skirmish_predef_design_toggle");
            var predefDesignsText = predefDesigns.GetChild("Text").GetComponent<Text>();
            predefDesignsText.text = ModUtils.LocalizeF(
                    "$TAF_Ui_SkirmishSettup_PredefDesignToggle_" + (skirmishSetupMod.usePredefs ? "On" : "Off"));
            var predefDesignsButton = predefDesigns.GetComponent<Button>();
            predefDesignsButton.onClick.RemoveAllListeners();
            predefDesignsButton.onClick.AddListener(new System.Action(() =>
            {
                skirmishSetupMod.usePredefs = !skirmishSetupMod.usePredefs;
                predefDesignsText.text = ModUtils.LocalizeF(
                        "$TAF_Ui_SkirmishSettup_PredefDesignToggle_" + (skirmishSetupMod.usePredefs ? "On" : "Off"));
            }));

            sharedDesignsBase.active = false;

            GameObject weatherSettings = ModUtils.GetChildAtPath("Weather Settings", upperButtons);
            weatherSettings.GetComponent<LayoutElement>().preferredWidth = 150;

            GameObject daytimeSettings = ModUtils.GetChildAtPath("Daytime Settings", upperButtons);
            daytimeSettings.GetComponent<LayoutElement>().preferredWidth = 150;

            playerUi = ModUtils.GetChildAtPath("Global/Ui/UiMain/Skirmish/Window/Players");
            playerUi1 = ModUtils.GetChildAtPath("Player1", playerUi);
            playerUi1.GetChild("Spacer").active = false;
            fleetList1 = ModUtils.GetChildAtPath("FleetList", playerUi1);
            fleetList1.active = false;

            GameObject conSv = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Right/Scroll View");

            fleetList1ScrollView = GameObject.Instantiate(conSv);
            fleetList1ScrollView.SetParent(playerUi1);
            fleetList1ScrollView.name = "TAF_FleetList";
            fleetList1ScrollView.transform.localPosition = Vector3.zero;
            fleetList1ScrollView.transform.SetScale(1, 1, 1);
            fleetList1ScrollView.GetComponent<LayoutElement>().preferredWidth = 350;

            GameObject fleetList1SvVp = fleetList1ScrollView.GetChild("Viewport");
            fleetList1SvVp.GetComponent<Image>().enabled = true;
            fleetList1SvVp.GetComponent<Mask>().enabled = true;

            fleetList1Container = ModUtils.GetChildAtPath("Viewport/Cont", fleetList1ScrollView);
            fleetList1Container.GetComponent<VerticalLayoutGroup>().spacing = 0;
            foreach (var child in fleetList1Container.GetChildren())
            {
                child.TryDestroy(true);
            }

            sTypeEntryTemplate = GameObject.Instantiate(ModUtils.GetChildAtPath("Line", fleetList1));
            sTypeEntryTemplate.name = "ShipType";
            sTypeEntryTemplate.transform.SetParent(fleetList1Container.transform);
            sTypeEntryTemplate.active = false;
            sTypeEntryTemplate.transform.localPosition = Vector3.zero;
            sTypeEntryTemplate.transform.SetScale(1, 1, 1);
            sTypeEntryTemplate.GetChild("MoreLess").GetChild("Less").TryDestroy(true);
            sTypeEntryTemplate.GetChild("MoreLess").GetChild("More").GetComponent<LayoutElement>().minWidth = 30;   

            sClassEntryTemplate = GameObject.Instantiate(ModUtils.GetChildAtPath("Line", fleetList1));
            sClassEntryTemplate.name = "Class";
            sClassEntryTemplate.transform.SetParent(fleetList1Container.transform);
            sClassEntryTemplate.active = false;
            sClassEntryTemplate.transform.localPosition = Vector3.zero;
            sClassEntryTemplate.transform.SetScale(1, 1, 1);
            sClassEntryTemplate.GetChild("MoreLess").GetChild("More").GetComponent<LayoutElement>().minWidth = 30;
            sClassEntryTemplate.GetChild("MoreLess").GetChild("Less").GetComponent<LayoutElement>().minWidth = 30;
            GameObject sClassEntryDelete = GameObject.Instantiate(sClassEntryTemplate.GetChild("MoreLess").GetChild("Less"));
            sClassEntryDelete.SetParent(sClassEntryTemplate);
            sClassEntryDelete.name = "Delete";
            sClassEntryDelete.transform.SetScale(0.8f, 0.8f, 0.8f);
            sClassEntryDelete.transform.SetAsFirstSibling();
            sClassEntryDelete.GetComponent<LayoutElement>().minWidth = 30;

            var rawData = File.ReadAllBytes(Config._BasePath + "/TAFData/cross.png");
            var cross = new Texture2D(256, 256, TextureFormat.DXT5, true);
            if (!ImageConversion.LoadImage(cross, rawData))
            {
                Melon<TweaksAndFixes>.Logger.Error("Failed to load cross.png from TAFData!");
            }
            var crossSprite = Sprite.Create(cross, new(0, 0, cross.width, cross.height), new(0.5f, 0.5f));
            sClassEntryDelete.GetChild("Image").GetComponent<Image>().sprite = crossSprite;

            // Melon<TweaksAndFixes>.Logger.Msg($"fleetList1 {ModUtils.DumpHierarchy(playerUi1.transform.parent.gameObject)}");

            // ChangeShipTypeInSkirmish
            
        }

        public static void GetSkirmishSetupPlayer2Ui()
        {
            playerUi2 = ModUtils.GetChildAtPath("Player2", playerUi);
            fleetList2 = ModUtils.GetChildAtPath("FleetList", playerUi2);
            fleetList2ScrollView = ModUtils.GetChildAtPath("TAF_FleetList", playerUi2);
            fleetList2Container = ModUtils.GetChildAtPath("Viewport/Cont", fleetList2ScrollView);
        }

        public static void UpdateSkirmishSetupModifications()
        {
            // Global/Ui/UiMain/Constructor/Right/Scroll View

            // Global/Ui/UiMain/Skirmish/Window/Players/Player1/FleetList/Line

            // Check for invalid state and initialization status

            if (playerUi1 == null)
                return;

            if (G.GameData.shipTypes == null)
                return;

            if (playerUi.GetChild("Player2", true) == null)
                return;

            if (!skirmishSetupMod.initialized)
                return;

            // Cache the stored values from the base UI container
            //   No point in redoing this, since it works fine.
            if (G.ui.skirmishSetup?.player1?.isHullAvailable != null)
            {
                var ShipTypeAvailibleM = skirmishSetupMod.player1.shipTypeAvailible;
                ShipTypeAvailibleM.Clear();
                
                foreach (var type in G.GameData.shipTypes)
                {
                    if (!G.ui.skirmishSetup.player1.isHullAvailable.ContainsKey(type.Value))
                        continue;

                    ShipTypeAvailibleM.Add(type.Value, G.ui.skirmishSetup.player1.isHullAvailable[type.Value]);

                    if (!G.ui.skirmishSetup.player1.isHullAvailable[type.Value])
                    {
                        G.ui.skirmishSetup.player1.shipAmounts[type.Value] = 0;
                    }
                }

                skirmishSetupMod.weather = G.ui.skirmishSetup.weather;
                skirmishSetupMod.daytime = G.ui.skirmishSetup.daytime;
                skirmishSetupMod.distance = G.ui.skirmishSetup.distance;

                if (skirmishSetupMod.player1.player != G.ui.skirmishSetup.player1.country
                    || skirmishSetupMod.player1.year != G.ui.skirmishSetup.player1.year)
                {
                    // TODO: Add an "Are you sure?" message when changing nations
                    // MessageBoxUI.Show(
                    //     "Clear Selected Designs",
                    //     "Changing the player will clear the selected ship list. Are you sure you want to continue?",
                    //     null, false, ModUtils.LocalizeF("$Ui_Popup_Generic_Yes"),
                    //     ModUtils.LocalizeF("$Ui_Popup_Generic_No"),
                    //     new System.Action(() => {
                    //     })
                    // );

                    // skirmishSetupMod.player1.shipAmounts.Clear();
                    skirmishSetupMod.player1.shipDesigns.Clear();
                    skirmishSetupMod.player1.shipInstances.Clear();
                }

                if (skirmishSetupMod.player2.player != G.ui.skirmishSetup.player2.country
                    || skirmishSetupMod.player1.year != G.ui.skirmishSetup.player1.year
                    || skirmishSetupMod.player2.year != G.ui.skirmishSetup.player2.year)
                {
                    // TODO: Add an "Are you sure?" message when changing nations
                    // MessageBoxUI.Show(
                    //     "Clear Selected Designs",
                    //     "Changing the player will clear the selected ship list. Are you sure you want to continue?",
                    //     null, false, ModUtils.LocalizeF("$Ui_Popup_Generic_Yes"),
                    //     ModUtils.LocalizeF("$Ui_Popup_Generic_No"),
                    //     new System.Action(() => {
                    //     })
                    // );

                    // skirmishSetupMod.player2.shipAmounts.Clear();
                    skirmishSetupMod.player2.shipDesigns.Clear();
                    skirmishSetupMod.player2.shipInstances.Clear();
                }

                skirmishSetupMod.player1.player = G.ui.skirmishSetup.player1.country;
                skirmishSetupMod.player1.year = G.ui.skirmishSetup.player1.year;
                skirmishSetupMod.player2.player = G.ui.skirmishSetup.player2.country;
                skirmishSetupMod.player2.year = G.ui.skirmishSetup.player2.year;
            }

            if (G.ui.skirmishSetup?.player2?.isHullAvailable != null)
            {
                var ShipTypeAvailibleM = skirmishSetupMod.player2.shipTypeAvailible;
                ShipTypeAvailibleM.Clear();

                foreach (var type in G.GameData.shipTypes)
                {
                    if (!G.ui.skirmishSetup.player2.isHullAvailable.ContainsKey(type.Value))
                        continue;

                    ShipTypeAvailibleM.Add(type.Value, G.ui.skirmishSetup.player2.isHullAvailable[type.Value]);

                    if (!G.ui.skirmishSetup.player2.isHullAvailable[type.Value])
                    {
                        G.ui.skirmishSetup.player2.shipAmounts[type.Value] = 0;
                    }
                }
            }

            // Init player2 UI separately, since it's created dynamically.
            if (playerUi2 == null)
                GetSkirmishSetupPlayer2Ui();

            // Melon<TweaksAndFixes>.Logger.Msg($"Destroying old entries:");

            // Destroy all active UI to be recreated
            foreach (var child in fleetList1Container.GetChildren())
            {
                if (!child.active) continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Destroy {child.name}");

                child.TryDestroy(true);
            }

            // Destroy all active UI to be recreated
            foreach (var child in fleetList2Container.GetChildren())
            {
                if (!child.active) continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Destroy {child.name}");

                child.TryDestroy(true);
            }

            foreach (var type in G.GameData.shipTypes)
            {
                if ((type.Value.paramx.ContainsKey("no_build") && type.Key != "tr")
                    || !skirmishSetupMod.player1.shipTypeAvailible.ValOrDef(type.Value, false))
                    continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Type {type.Value.nameUi}");

                var sType = GameObject.Instantiate(sTypeEntryTemplate);
                sType.active = true;
                sType.transform.SetParent(fleetList1Container, false);

                var counts = skirmishSetupMod.player1.shipAmounts.ValueOrNew(type.Value);
                var designs = skirmishSetupMod.player1.shipDesigns;
                var instances = skirmishSetupMod.player1.shipInstances;

                var typeText = sType.GetChild("Type").GetComponent<Text>();
                typeText.text = $"{type.Value.nameFull} Classes";
                float alpha = counts.Count > 0 ? 1 : 0.5f;
                typeText.color = new(1, 1, 1, alpha);

                var sTypeAmountText = sType.GetChild("Amount").GetComponent<Text>();
                sTypeAmountText.text = $"{counts.Count}";

                int classIndex = sType.transform.GetSiblingIndex() + 1;
                int classNumber = 1;

                foreach (var sClassEntry in counts)
                {
                    var sClass = GameObject.Instantiate(sClassEntryTemplate);
                    sClass.active = true;
                    sClass.transform.SetParent(fleetList1Container, false);
                    // subEntry.transform.SetScale(1,1,1);
                    sClass.transform.SetSiblingIndex(classIndex++);

                    var classText = sClass.GetChild("Type").GetComponent<Text>();
                    if (!skirmishSetupMod.player1.shipInstances.ContainsKey(sClassEntry.Key))
                    {
                        classText.text = $"Class #{classNumber++}";
                        classText.color = new(1, 1, 1, 1);
                    }
                    else
                    {
                        classText.text = $"{skirmishSetupMod.player1.shipInstances[sClassEntry.Key].Name(false, false)}";
                        classText.color = new(0.8f, 1, 0.8f, 1);
                    }

                    var sClassAmountText = sClass.GetChild("Amount").GetComponent<Text>();
                    sClassAmountText.text = $"{sClassEntry.Value}";

                    var addShipCountBtn = ModUtils.GetChildAtPath("MoreLess/More", sClass).GetComponent<Button>();
                    var subShipCountBtn = ModUtils.GetChildAtPath("MoreLess/Less", sClass).GetComponent<Button>();
                    var subShipCountBtnImg = ModUtils.GetChildAtPath("Bg", subShipCountBtn.gameObject).GetComponent<Image>();
                    var delShipCountBtn = ModUtils.GetChildAtPath("Delete", sClass).GetComponent<Button>();

                    if (sClassEntry.Value <= 1)
                    {
                        subShipCountBtn.interactable = false;
                        subShipCountBtnImg.color = new(1, 1, 1, 0.5f);
                        skirmishSetupMod.player1.shipAmounts[type.Value][sClassEntry.Key] = 1;
                    }
                    else
                    {
                        subShipCountBtn.interactable = true;
                        subShipCountBtnImg.color = new(1, 1, 1, 1);
                    }

                    addShipCountBtn.onClick.RemoveAllListeners();
                    addShipCountBtn.onClick.AddListener(new Action(() => {
                        skirmishSetupMod.player1.shipAmounts[type.Value][sClassEntry.Key]++;

                        subShipCountBtn.interactable = true;
                        subShipCountBtnImg.color = new(1, 1, 1, 1);

                        sClassAmountText.text = $"{skirmishSetupMod.player1.shipAmounts[type.Value][sClassEntry.Key]}";
                    }));

                    subShipCountBtn.onClick.RemoveAllListeners();
                    subShipCountBtn.onClick.AddListener(new Action(() => {
                        skirmishSetupMod.player1.shipAmounts[type.Value][sClassEntry.Key]--;

                        if (skirmishSetupMod.player1.shipAmounts[type.Value][sClassEntry.Key] == 1)
                        {
                            subShipCountBtn.interactable = false;
                            subShipCountBtnImg.color = new(1, 1, 1, 0.5f);
                        }
                        else
                        {
                            subShipCountBtn.interactable = true;
                            subShipCountBtnImg.color = new(1, 1, 1, 1);
                        }

                        sClassAmountText.text = $"{skirmishSetupMod.player1.shipAmounts[type.Value][sClassEntry.Key]}";
                    }));

                    delShipCountBtn.onClick.RemoveAllListeners();
                    delShipCountBtn.onClick.AddListener(new Action(() => {

                        if (designs.ContainsKey(sClassEntry.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"Removing: {designs[sClassEntry.Key].vesselName} ({designs[sClassEntry.Key].id})"
                            );

                            foreach (var vessel in CampaignController.Instance.CampaignData.GetShips)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"  Vessel: {vessel.vesselName} ({vessel.id})");

                                if (vessel.id == designs[sClassEntry.Key].id)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"    Found!");
                                    // vessel.Erase();
                                    vessel.isShipChoisedInCustomBattle = false;
                                    break;
                                }
                            }
                        }

                        designs.TryRemove(sClassEntry.Key);
                        instances.TryRemove(sClassEntry.Key);
                        if (SelectedShip == sClassEntry.Key)
                            SelectedShip = Guid.Empty;
                        counts.Remove(sClassEntry.Key);
                        G.ui.skirmishSetup.player1.shipAmounts[type.Value] = counts.Count;
                        G.ui.skirmishSetup.Dirty();
                        G.ui.Refresh();
                    }));
                }

                var addShipClassBtn = sType.GetChild("MoreLess").GetChild("More").GetComponent<Button>();
                addShipClassBtn.onClick.RemoveAllListeners();
                addShipClassBtn.onClick.AddListener(new Action(() => {
                    counts.Add(Guid.NewGuid(), 1);
                    G.ui.skirmishSetup.player1.shipAmounts[type.Value] = counts.Count;
                    G.ui.skirmishSetup.Dirty();
                    G.ui.Refresh();
                }));

                // Disable unavailible shiptypes
            }

            foreach (var type in G.GameData.shipTypes)
            {
                if ((type.Value.paramx.ContainsKey("no_build") && type.Key != "tr")
                    || !skirmishSetupMod.player2.shipTypeAvailible.ValOrDef(type.Value, false))
                    continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Type {type.Value.nameUi}");

                var sType = GameObject.Instantiate(sTypeEntryTemplate);
                sType.active = true;
                sType.transform.SetParent(fleetList2Container, false);

                var counts = skirmishSetupMod.player2.shipAmounts.ValueOrNew(type.Value);
                var designs = skirmishSetupMod.player2.shipDesigns;
                var instances = skirmishSetupMod.player2.shipInstances;

                var typeText = sType.GetChild("Type").GetComponent<Text>();
                typeText.text = $"{type.Value.nameFull} Classes";
                float alpha = counts.Count > 0 ? 1 : 0.5f;
                typeText.color = new(1, 1, 1, alpha);

                var sTypeAmountText = sType.GetChild("Amount").GetComponent<Text>();
                sTypeAmountText.text = $"{counts.Count}";

                int classIndex = sType.transform.GetSiblingIndex() + 1;
                int classNumber = 1;

                foreach (var sClassEntry in counts)
                {
                    var sClass = GameObject.Instantiate(sClassEntryTemplate);
                    sClass.active = true;
                    sClass.transform.SetParent(fleetList2Container, false);
                    // subEntry.transform.SetScale(1,1,1);
                    sClass.transform.SetSiblingIndex(classIndex++);

                    var classText = sClass.GetChild("Type").GetComponent<Text>();
                    if (!skirmishSetupMod.player2.shipInstances.ContainsKey(sClassEntry.Key))
                    {
                        classText.text = $"Class #{classNumber++}";
                        classText.color = new(1, 1, 1, 1);
                    }
                    else
                    {
                        classText.text = $"{skirmishSetupMod.player2.shipInstances[sClassEntry.Key].Name(false, false)}";
                        classText.color = new(0.8f, 1, 0.8f, 1);
                    }

                    var sClassAmountText = sClass.GetChild("Amount").GetComponent<Text>();
                    sClassAmountText.text = $"{sClassEntry.Value}";

                    var addShipCountBtn = ModUtils.GetChildAtPath("MoreLess/More", sClass).GetComponent<Button>();
                    var subShipCountBtn = ModUtils.GetChildAtPath("MoreLess/Less", sClass).GetComponent<Button>();
                    var subShipCountBtnImg = ModUtils.GetChildAtPath("Bg", subShipCountBtn.gameObject).GetComponent<Image>();
                    var delShipCountBtn = ModUtils.GetChildAtPath("Delete", sClass).GetComponent<Button>();

                    if (sClassEntry.Value <= 1)
                    {
                        subShipCountBtn.interactable = false;
                        subShipCountBtnImg.color = new(1, 1, 1, 0.5f);
                        skirmishSetupMod.player2.shipAmounts[type.Value][sClassEntry.Key] = 1;
                    }
                    else
                    {
                        subShipCountBtn.interactable = true;
                        subShipCountBtnImg.color = new(1, 1, 1, 1);
                    }

                    addShipCountBtn.onClick.RemoveAllListeners();
                    addShipCountBtn.onClick.AddListener(new Action(() => {
                        skirmishSetupMod.player2.shipAmounts[type.Value][sClassEntry.Key]++;

                        sClassAmountText.text = $"{skirmishSetupMod.player2.shipAmounts[type.Value][sClassEntry.Key]}";

                        subShipCountBtn.interactable = true;
                        subShipCountBtnImg.color = new(1, 1, 1, 1);
                    }));

                    subShipCountBtn.onClick.RemoveAllListeners();
                    subShipCountBtn.onClick.AddListener(new Action(() => {
                        skirmishSetupMod.player2.shipAmounts[type.Value][sClassEntry.Key]--;

                        if (skirmishSetupMod.player2.shipAmounts[type.Value][sClassEntry.Key] == 1)
                        {
                            subShipCountBtn.interactable = false;
                            subShipCountBtnImg.color = new(1, 1, 1, 0.5f);
                        }
                        else
                        {
                            subShipCountBtn.interactable = true;
                            subShipCountBtnImg.color = new(1, 1, 1, 1);
                        }

                        sClassAmountText.text = $"{skirmishSetupMod.player2.shipAmounts[type.Value][sClassEntry.Key]}";
                    }));

                    delShipCountBtn.onClick.RemoveAllListeners();
                    delShipCountBtn.onClick.AddListener(new Action(() => {

                        if (designs.ContainsKey(sClassEntry.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"Removing: {designs[sClassEntry.Key].vesselName} ({designs[sClassEntry.Key].id})"
                            );

                            foreach (var vessel in CampaignController.Instance.CampaignData.GetShips)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"  Vessel: {vessel.vesselName} ({vessel.id})");

                                if (vessel.id == designs[sClassEntry.Key].id)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"    Found!");
                                    // vessel.Erase();
                                    vessel.isShipChoisedInCustomBattle = false;
                                    break;
                                }
                            }
                        }

                        designs.TryRemove(sClassEntry.Key);
                        instances.TryRemove(sClassEntry.Key);
                        if (SelectedShip == sClassEntry.Key)
                            SelectedShip = Guid.Empty;
                        counts.Remove(sClassEntry.Key);
                        G.ui.skirmishSetup.player2.shipAmounts[type.Value] = counts.Count;
                        G.ui.skirmishSetup.Dirty();
                        G.ui.Refresh();
                    }));
                }

                var addShipClassBtn = sType.GetChild("MoreLess").GetChild("More").GetComponent<Button>();
                addShipClassBtn.onClick.RemoveAllListeners();
                addShipClassBtn.onClick.AddListener(new Action(() => {
                    counts.Add(Guid.NewGuid(), 1);
                    G.ui.skirmishSetup.player2.shipAmounts[type.Value] = counts.Count;
                    G.ui.skirmishSetup.Dirty();
                    G.ui.Refresh();
                }));
            }
        }

        public static void OnStartCustomBattle(bool doBuild)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"{doBuild} && {SelectedShip} != {Guid.Empty}");

            // Listen to change player & year => Clear ship lists
            // Listen to remove ship class => Remove from list & update selected

            if (doBuild && SelectedShip != Guid.Empty)
            {
                var player =
                    G.ui.IsCustomBattleShipsPlayers()
                    ? skirmishSetupMod.player1
                    : skirmishSetupMod.player2;

                var playerSk =
                    G.ui.IsCustomBattleShipsPlayers()
                    ? G.ui.skirmishSetup.player1
                    : G.ui.skirmishSetup.player2;

                skirmishSetupMod.InitializePlayerMadeShips();

                // if (player.shipInstances.ContainsKey(SelectedShip))
                // {
                //     Melon<TweaksAndFixes>.Logger.Msg($"  {SelectedShip} exists!");
                // 
                //     skirmishSetupMod.InitializePlayerMadeShips();
                // 
                //     // Calls UpdateShipTypeButtons(true), which grabs and stores the new selected ship
                //     GameManager.Instance.ToConstructor(
                //         false, player.shipInstances[SelectedShip],
                //         true, null, null, false, playerSk.country.Player()
                //     );
                // }
            }

            UpdateShipTypeButtons(doBuild);
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
                            popup.GetComponent<MessageBoxUI>().Hide();
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

            int bailoutEventNumber = Config.Param("taf_bailout_event_number", 81);

            EventData prompt = G.GameData.events[$"{bailoutEventNumber}"];
            EventData response = G.GameData.events[$"{bailoutEventNumber}_a"];

            bailoutEventWindowYesBtn.onClick.AddListener(new System.Action(() =>
            {
                if (bailoutPlayer == null)
                {
                    bailoutEvent.SetActive(false);
                    Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid player for bailout event!");
                    return;
                }

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
            bailoutEventWindowHeader.GetComponent<TMP_Text>().text = ModUtils.LocalizeF("$TAF_Ui_Event_GovernmentBailout"); // TODO: Localize

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
            promptStr += $"\n\n{ModUtils.ColorNumber(response.money, "", "%", false, true)} {ModUtils.LocalizeF("$Ui_World_NavalFunds")} ({ModUtils.ColorNumber(player.Budget() * response.money / 100, "$", "", false, true)})";
            promptStr += $"\n{ModUtils.ColorNumber(response.budget, "", "%")} {ModUtils.LocalizeF("$Ui_World_NavalBudgetPercent")}";
            promptStr += $"\n{ModUtils.ColorNumber(response.wealth, "", "%")} {ModUtils.LocalizeF("$Ui_World_GDP")}";
            promptStr += $"\n{ModUtils.ColorNumber(response.reputation)} {ModUtils.LocalizeF("$Ui_Event_NavalPrestige")}";
            promptStr += $"\n{ModUtils.ColorNumber(response.respect, "", "", true)} {ModUtils.LocalizeF("$Ui_Event_Unrest")}";

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

        public static bool showPopups = true;

        public static void CreateHidePopupsButton()
        {
            GameObject popupSaveWindow = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/SaveWindow");
            GameObject regularPopupsRoot = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows");
            var regularPopups = regularPopupsRoot.GetChildren();
            GameObject basePopupsRoot = ModUtils.GetChildAtPath("Ui/UiMain", G.container);

            showPopups = true;

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
                    // if (Input.GetKey(KeyCode.J))
                    return;
                }

                if (!GameManager.IsWorldMap)
                {
                    hidePopupsButton.SetActive(false);
                    return;
                }

                // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"  Checking premade popups...");

                foreach (GameObject child in regularPopups)
                {
                    if (child.name != "Event Window"
                        && child.name != "Battle Window"
                        && child.name != "WarReparationWindowUI")
                        continue;

                    if (child.active)
                    {
                        // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"    Found popup {child.name}!");
                        hasPopups = true;
                        break;
                    }
                }

                regularPopupsRoot.transform.localPosition = showPopups ? Vector3.zero : new(1000000,0,0);

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

                        child.transform.localPosition = showPopups ? Vector3.zero : new(1000000, 0, 0);
                    }

                    hidePopupsButton.transform.SetSiblingIndex(children.Count - 1);
                }

                // if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"  Has popups: {hasPopups} | !Show popups {!showPopups} | !Loading Screen {!GameManager.IsLoadingScreenActive} | World Map {GameManager.IsWorldMap} | !Save Menu {!popupSaveWindow.active}");

                hidePopupsButton.SetActive(
                    (hasPopups || !showPopups)
                    && (!GameManager.IsLoadingScreenActive && !popupSaveWindow.active)
                );
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

            // Global/Ui/UiMain/Constructor/Parts/ScrollRect/Viewport/Content/Part/Button/Image

            // ScrollRect -> -400 -176 | 400 40
            // Content -> padding.t = 72 * i

            GameObject partSelector =
                ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Parts/ScrollRect/Viewport/Content/Part/Button/Bg");
            partSelector.TryDestroyComponent<Shadow>();
            var partSelectorImage = partSelector.GetComponent<Image>();
            partSelectorImage.material = new Material(Shader.Find("UI/Default"));
            partSelectorImage.material.SetColor("_Color", new(0.5f, 0.5f, 0.5f, 0.5f));

            CreateTopBarRotationButton();
            CreateTopBarRotationText();
            CreateArmorQualityButton();

            G.ui.conUpperButtons.GetChild("Layout").GetChild("SpaceEater").active = false;
            GameObject Launch = GameObject.Instantiate(G.ui.conUpperButtons.GetChild("Layout").GetChild("Save"));
            Launch.GetChild("IconGood").TryDestroy(true);
            HorizontalLayoutGroup group = G.ui.conUpperButtons.GetChild("Layout").GetComponent<HorizontalLayoutGroup>();
            Launch.transform.SetParent(group.transform, false);//ui.conUpperButtons.GetChild("Layout"));
            Launch.transform.SetSiblingIndex(G.ui.conUpperButtons.GetChild("Layout").GetChildren().Count - 7);
            Launch.name = "TAF_Launch_To_Custom_Battle";
            AddTooltip(Launch, "$TAF_tooltip_launch_to_custom_battle");
            var LaunchText = Launch.GetChild("Text").GetComponent<Text>();
            LaunchText.text = ModUtils.LocalizeF("$TAF_Ui_Dockyard_Launch");
            var LaunchButton = Launch.GetComponent<Button>();
            LaunchButton.onClick.RemoveAllListeners();
            LaunchButton.onClick.AddListener(new System.Action(() =>
            {
                Melon<TweaksAndFixes>.Logger.Msg($"To battle!");
                GameManager.Instance.CustomBattleConstructorFinished();
            }));
            LayoutElement layout = Launch.GetComponent<LayoutElement>();
            layout.preferredWidth = 100;

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

                Launch.active = !GameManager.IsCampaign && !GameManager.IsSharedDesignConstructor;

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

        public static GameObject SSCDropdown;
        public static GameObject SSCDropdownDropdownContainer;
        public static GameObject SSCDropdownElementTemplate;
        public static GameObject SSCDropdownButton;
        public static Button SSCDropdownButtonComp;
        public static Text SSCDropdownButtonText;
        public static bool SkipNextUpdateShipTypeButtons = false;

        public static Guid SelectedShip;

        public static void ApplyShipTypeButtonsModifications()
        {
            // MAX -265 -50
            // MIN 780 0

            Ui ui = G.ui;
            HorizontalLayoutGroup group = ui.conUpperButtons.GetChild("Layout").GetComponent<HorizontalLayoutGroup>();

            SSCDropdown = new GameObject();
            SSCDropdown.name = "TAF_SelectShipClassDropdown";
            SSCDropdown.active = false;
            LayoutElement layout = SSCDropdown.AddComponent<LayoutElement>();
            SSCDropdown.transform.SetParent(group.transform, false);
            SSCDropdown.transform.SetAsLastSibling();
            layout.preferredWidth = 100;

            GameObject conSv = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Right/Scroll View");

            var SSCDropdownScrollView = GameObject.Instantiate(conSv);
            SSCDropdownScrollView.SetParent(SSCDropdown, false);
            SSCDropdownScrollView.name = "TAF_FleetList";
            SSCDropdownScrollView.transform.localPosition = Vector3.zero;
            SSCDropdownScrollView.active = false;
            var SSCDropdownSvRt = SSCDropdownScrollView.GetComponent<RectTransform>();
            SSCDropdownSvRt.offsetMax = new(110, -45);
            SSCDropdownSvRt.offsetMin = new(0, -575);

            GameObject SSCDropdownSvVp = SSCDropdownScrollView.GetChild("Viewport");
            SSCDropdownSvVp.GetComponent<Image>().enabled = true;
            SSCDropdownSvVp.GetComponent<Mask>().enabled = true;

            SSCDropdownDropdownContainer = ModUtils.GetChildAtPath("Viewport/Cont", SSCDropdownScrollView);
            SSCDropdownDropdownContainer.GetComponent<VerticalLayoutGroup>().spacing = 0;
            foreach (var child in SSCDropdownDropdownContainer.GetChildren())
            {
                child.TryDestroy(true);
            }


            SSCDropdownButton = GameObject.Instantiate(ModUtils.GetChildAtPath("Layout/Undo", ui.conUpperButtons));
            SSCDropdownButton.transform.SetParent(SSCDropdown, false);
            SSCDropdownButton.transform.localPosition = Vector3.zero;
            var SSCDropdownButtonRt = SSCDropdownButton.GetComponent<RectTransform>();
            SSCDropdownButtonRt.offsetMax = new(100, 0);
            SSCDropdownButtonRt.offsetMin = new(0, -40);
            SSCDropdownButton.name = "Dropdown_Button";
            AddTooltip(SSCDropdownButton, "$TAF_tooltip_skirmish_ship_type_dropdown");

            SSCDropdownButton.GetChild("Text").TryDestroyComponent<LocalizeText>();
            SSCDropdownButtonText = SSCDropdownButton.GetChild("Text").GetComponent<Text>();
            SSCDropdownButtonText.text = ModUtils.LocalizeF(
                "$TAF_Ui_Dockyard_TopBar_SkirmishShipType",
                "Random AI Ship", "?", "??", "?"
            );
            
            SSCDropdownButtonComp = SSCDropdownButton.GetComponent<Button>();
            SSCDropdownButtonComp.onClick.RemoveAllListeners();
            SSCDropdownButtonComp.onClick.AddListener(new System.Action(() =>
            {
                SSCDropdownScrollView.active = !SSCDropdownScrollView.active;
            }));


            SSCDropdownElementTemplate = GameObject.Instantiate(SSCDropdownButton);
            SSCDropdownElementTemplate.transform.SetParent(SSCDropdownDropdownContainer, false);
            SSCDropdownElementTemplate.active = false;
            SSCDropdownElementTemplate.name = "Template";
            SSCDropdownElementTemplate.GetComponent<LayoutElement>().preferredWidth = 100;
            SSCDropdownElementTemplate.TryDestroyComponent<Shadow>();
            var SSCDropdownElementTemplateBg = SSCDropdownElementTemplate.GetComponent<Image>();
            SSCDropdownElementTemplateBg.material = new Material(Shader.Find("UI/Default"));
            SSCDropdownElementTemplateBg.material.SetColor("_Color", new(0.5f, 0.5f, 0.5f, 0.5f));
            SSCDropdownElementTemplate.TryDestroyComponent<OnEnter>();
            SSCDropdownElementTemplate.TryDestroyComponent<OnLeave>();

            // UiM.ModifyUi(G.ui.conUpperRight).ReplaceOffsets(new Vector2(780, 0), new Vector2(-265, -50));
            G.ui.conUpperRight.active = false;
        }

        public static bool ToConstructorAfterSwitchPlayer()
        {
            var player =
                !G.ui.IsCustomBattleShipsPlayers()
                ? skirmishSetupMod.player1
                : skirmishSetupMod.player2;

            var playerSk =
                !G.ui.IsCustomBattleShipsPlayers()
                ? G.ui.skirmishSetup.player1
                : G.ui.skirmishSetup.player2;

            ShipType preferredType = null;
            Guid preferredId = Guid.Empty;

            foreach (var type in G.GameData.shipTypes)
            {
                if (type.Value.paramx.ContainsKey("no_build"))
                    continue;

                if (!player.shipTypeAvailible.ValOrDef(type.Value, false))
                    continue;

                var counts = player.shipAmounts.ValueOrNew(type.Value);

                foreach (var sClassEntry in counts)
                {
                    if (preferredType == null)
                    {
                        preferredType = type.Value;
                        preferredId = sClassEntry.Key;
                    }

                    if (!player.shipInstances.HasValue(sClassEntry.Key))
                        continue;

                    Melon<TweaksAndFixes>.Logger.Msg($"  Found: {player.shipInstances[sClassEntry.Key] == null} => {player.shipInstances[sClassEntry.Key].Name(false, false)}");

                    SelectedShip = sClassEntry.Key;
                    GameManager.Instance.ToConstructor(
                        false, player.shipInstances[sClassEntry.Key],
                        true, null, type.Value, false, G.ui.GetEnemyForPlayer()
                    );
                    G.ui.currentShipInSkirmish = G.ui.GetIntNumberFromShipType(type.Value);

                    return true;
                }
            }

            if (preferredType == null || preferredId == Guid.Empty)
            {
                return false;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  Searching for shared design:");

            foreach (var sDesign in CampaignController.Instance.CampaignData.GetShips)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  {sDesign.vesselName}");
                if (sDesign.IsSharedDesign
                    && sDesign.player == playerSk.country.Player()
                    && sDesign.shipType == preferredType)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    Match!");

                    SelectedShip = preferredId;
                    GameManager.Instance.ToConstructor(
                        false, sDesign, true, null, preferredType, false, playerSk.country.Player()
                    );
                    G.ui.currentShipInSkirmish = G.ui.GetIntNumberFromShipType(preferredType);
                    return true;
                }
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  Creating: {preferredType.name}");

            SelectedShip = preferredId;
            GameManager.Instance.ToConstructor(
                true, null, true, null, preferredType, false, G.ui.GetEnemyForPlayer()
            );

            G.ui.currentShipInSkirmish = G.ui.GetIntNumberFromShipType(preferredType);

            return true;
        }

        public static void UpdateShipTypeButtons(bool doBuild)
        {
            if (SkipNextUpdateShipTypeButtons)
            {
                SkipNextUpdateShipTypeButtons = false;
                return;
            }

            // When entering with saved ships, override the ship it opens the constructor with

            if (doBuild && !GameManager.IsCampaign && !GameManager.IsSharedDesignConstructor)
            {
                SSCDropdown.active = true;
            }
            else
            {
                SSCDropdown.active = false;
                return;
            }

            foreach (var child in SSCDropdownDropdownContainer.GetChildren())
            {
                if (child.activeSelf == false) continue;
                child.TryDestroy(true);
            }

            bool first = true;

            Melon<TweaksAndFixes>.Logger.Msg($"UpdateShipTypeButtons: {SelectedShip}");
            // Melon<TweaksAndFixes>.Logger.Msg($"  {ShipM.GetActiveShip()?.Name(false, false) ?? "NULL!"}");

            if (ShipM.GetActiveShip() == null)
                return;

            var SwitchPlayerTypeBtn = G.ui.conUpperButtons.GetChild("Layout").GetChild("SwitchPlayerType").GetComponent<Button>();
            var SwitchPlayerTypeTxt = G.ui.conUpperButtons.GetChild("Layout").GetChild("SwitchPlayerType").GetChild("Text").GetComponent<Text>();
            SwitchPlayerTypeBtn.onClick.RemoveAllListeners();
            SwitchPlayerTypeBtn.onClick.AddListener(new System.Action(() => {
                bool success = ToConstructorAfterSwitchPlayer();

                if (!success)
                    return;

                if (G.ui.IsCustomBattleShipsPlayers())
                    SwitchPlayerTypeTxt.text = ModUtils.LocalizeF("$Ui_Academy_MissionInfo_Detail_Enemy");
                else
                    SwitchPlayerTypeTxt.text = ModUtils.LocalizeF("$Ui_Academy_MissionInfo_Detail_You");
            }));

            var player =
                G.ui.IsCustomBattleShipsPlayers()
                ? skirmishSetupMod.player1
                : skirmishSetupMod.player2;

            var playerSk =
                G.ui.IsCustomBattleShipsPlayers()
                ? G.ui.skirmishSetup.player1
                : G.ui.skirmishSetup.player2;

            if (SelectedShip == Guid.Empty && player.shipInstances.Count != 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  {PlayerController.Instance.Ship.Name(false, false) ?? "NULL"}");

                if (!skirmishSetupMod.InitializePlayerMadeShips())
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    Failed to re-init ships!");
                    return;
                }

                foreach (var ship in player.shipInstances)
                {
                    if (ship.Value == PlayerController.Instance.Ship)
                    {
                        SelectedShip = ship.Key;
                        break;
                    }
                }

                Melon<TweaksAndFixes>.Logger.Msg($"  {SelectedShip}");
            }

            foreach (var type in G.GameData.shipTypes)
            {
                if (type.Value.paramx.ContainsKey("no_build"))
                    continue;

                if (!player.shipTypeAvailible.ValOrDef(type.Value, false))
                    continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  {type.Key}");

                var counts = player.shipAmounts.ValueOrNew(type.Value);

                int index = 1;

                foreach (var sClassEntry in counts)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  {sClassEntry.Key}");

                    if (player.shipInstances.HasValue(sClassEntry.Key))
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Instance: {player.shipInstances[sClassEntry.Key].Name(false, false)}");
                    }

                    if (player.shipDesigns.HasValue(sClassEntry.Key))
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Designs: {player.shipDesigns[sClassEntry.Key].vesselName}");
                    }

                    var SSCDropdownElement = GameObject.Instantiate(SSCDropdownElementTemplate);
                    SSCDropdownElement.transform.SetParent(SSCDropdownDropdownContainer, false);
                    SSCDropdownElement.active = true;

                    if (player.shipDesigns.HasValue(sClassEntry.Key)
                        && !player.shipInstances.HasValue(sClassEntry.Key))
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Null ship detected!");
                        skirmishSetupMod.InitializePlayerMadeShips();
                    }

                    if (first && player.shipInstances.Count == 0)
                    {
                        SelectedShip = sClassEntry.Key;
                        var currShip = PlayerController.Instance.Ship;
                        player.shipInstances.AddOrSet(sClassEntry.Key, currShip);
                        player.shipDesigns.AddOrSet(sClassEntry.Key, currShip.ToStore());
                        // G.ui.customBattleShipListUI[G.ui.GetIntNumberFromShipType(currShip.shipType)] = currShip;
                        Melon<TweaksAndFixes>.Logger.Msg($"  1 {sClassEntry.Key} -> {currShip.Name(false, false)}");
                        first = false;
                    }

                    else if (SelectedShip == sClassEntry.Key)
                    {
                        var currShip = PlayerController.Instance.Ship;
                        player.shipInstances.AddOrSet(SelectedShip, currShip);
                        player.shipDesigns.AddOrSet(SelectedShip, currShip.ToStore());
                        // G.ui.customBattleShipListUI[G.ui.GetIntNumberFromShipType(currShip.shipType)] = currShip;
                        Melon<TweaksAndFixes>.Logger.Msg($"  2 {SelectedShip} -> {currShip.Name(false, false)}");
                    }

                    string locText = ModUtils.LocalizeF(
                        "$TAF_Ui_Dockyard_TopBar_SkirmishShipType",
                        player.shipInstances.ContainsKey(sClassEntry.Key) ?
                            player.shipInstances[sClassEntry.Key].Name(false, false) : "Random AI Ship",
                        $"{sClassEntry.Value}", $"{type.Value.nameUi}", $"{index++}"
                    );

                    var SSCDropdownElementText = SSCDropdownElement.GetChild("Text").GetComponent<Text>();
                    SSCDropdownElementText.text = locText;

                    var SSCDropdownElementButton = SSCDropdownElement.GetComponent<Button>();
                    SSCDropdownElementButton.onClick.RemoveAllListeners();
                    SSCDropdownElementButton.onClick.AddListener(new Action(() => {
                        bool hasDesign = player.shipInstances.ContainsKey(sClassEntry.Key);

                        if (hasDesign && !player.shipInstances.HasValue(sClassEntry.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"  Error! ID {sClassEntry.Key} has null ship!" +
                                $" Design = {(player.shipDesigns.HasValue(sClassEntry.Key) ? player.shipDesigns[sClassEntry.Key].vesselName : "NULL")}");
                            return;
                        }

                        if (player.shipInstances.HasValue(sClassEntry.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"  Clicked Instance: {player.shipInstances[sClassEntry.Key].Name(false, false)}");
                        }

                        if (player.shipDesigns.HasValue(sClassEntry.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"  Clicked Designs: {player.shipDesigns[sClassEntry.Key].vesselName}");
                        }

                        // Melon<TweaksAndFixes>.Logger.Msg($"  #### {ShipM.GetActiveShip().Name(false, false)} {ShipM.GetActiveShip().parts.Count > 0}");

                        // Ask to delete design if it has no parts on it
                        // if (ShipM.GetActiveShip().parts.Count == 0)
                        // {
                        //     var shipBeforeSwitch = ShipM.GetActiveShip();
                        // 
                        //     Melon<TweaksAndFixes>.Logger.Msg($"  {shipBeforeSwitch.Name(false, false)} is empty!");
                        // 
                        //     MessageBoxUI.Show(
                        //         "Empty Design",
                        //         "The current design is empty, do you want to delete it?",
                        //         null, false, ModUtils.LocalizeF("$Ui_Popup_Generic_Yes"),
                        //         ModUtils.LocalizeF("$Ui_Popup_Generic_No"),
                        //         new System.Action(() => {
                        //             shipBeforeSwitch.Erase();
                        //             player.shipInstances.TryRemove(SelectedShip);
                        //             player.shipDesigns.TryRemove(SelectedShip);
                        //             UpdateShipTypeButtons(true);
                        //         })
                        //     );
                        // }

                        Melon<TweaksAndFixes>.Logger.Msg($"  {hasDesign} : {BattleManager.Instance.customBattleSharedDesigns.Count}");

                        // First try and get a random shared design
                        if (!hasDesign)
                        {
                            foreach (var sDesign in CampaignController.Instance.CampaignData.GetShips)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"  {sDesign.vesselName}");
                                Melon<TweaksAndFixes>.Logger.Msg($"    {sDesign.IsSharedDesign}");
                                Melon<TweaksAndFixes>.Logger.Msg($"    {sDesign.player == playerSk.country.Player()}");
                                Melon<TweaksAndFixes>.Logger.Msg($"    {sDesign.shipType == type.Value}");
                                if (sDesign.IsSharedDesign
                                    && sDesign.player == playerSk.country.Player()
                                    && sDesign.shipType == type.Value)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"    Match!");

                                    SelectedShip = sClassEntry.Key;
                                    GameManager.Instance.ToConstructor(
                                        false, sDesign, true, null, type.Value, false, playerSk.country.Player()
                                    );
                                    G.ui.currentShipInSkirmish = G.ui.GetIntNumberFromShipType(type.Value);
                                    SSCDropdownButtonText.text = locText;

                                    // G.ui.customBattleShipListUI[G.ui.GetIntNumberFromShipType(ShipM.GetActiveShip().shipType)] = ShipM.GetActiveShip();
                                    Melon<TweaksAndFixes>.Logger.Msg($"  3 {sClassEntry.Key} -> {ShipM.GetActiveShip().Name(false, false)}");
                                    return;
                                }
                            }
                        }

                        // If no shared designs exist, then create a new hull
                        SelectedShip = sClassEntry.Key;
                        // Calls UpdateShipTypeButtons(true), which grabs and stores the new selected ship
                        GameManager.Instance.ToConstructor(
                            !hasDesign,
                            hasDesign ? player.shipInstances[sClassEntry.Key] : null,
                            true, null, type.Value, false, playerSk.country.Player()
                        );
                        G.ui.currentShipInSkirmish = G.ui.GetIntNumberFromShipType(type.Value);
                        SSCDropdownButtonText.text = locText;
                        // G.ui.customBattleShipListUI[G.ui.GetIntNumberFromShipType(ShipM.GetActiveShip().shipType)] = ShipM.GetActiveShip();
                        if (!hasDesign) Melon<TweaksAndFixes>.Logger.Msg($"  3 {sClassEntry.Key} -> {ShipM.GetActiveShip().Name(false, false)}");
                    }));

                    if (SelectedShip == sClassEntry.Key)
                    {
                        SSCDropdownButtonText.text = locText;
                    }
                }
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

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseCountry/Country/Flag
            GameObject countryFlag = ModUtils.GetChildAtPath(
                "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseCountry/Country/Flag"
            );
            Image countryFlagImage = countryFlag.GetComponent<Image>();

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseCountry/Country/Flag
            GameObject countryName = ModUtils.GetChildAtPath(
                "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseCountry/Country/Name"
            );
            Text countryNameText = countryName.GetComponent<Text>();


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

                if (value.Length > 0 && !ModUtils.TryParse("" + value[^1], out _))
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

                if (value.Length == 0 || !ModUtils.TryParse(value, out parsedYear) || parsedYear == G.ui.sharedDesignYear || parsedYear < Config.StartingYear || parsedYear > Config.Param("taf_shared_designer_max_year", 1960))
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{parsedYear}`");
                    InputChooseYearEditField.text = G.ui.sharedDesignYear.ToString();
                    InputChooseYearStaticText.text = G.ui.sharedDesignYear.ToString();
                }
                else
                {
                    G.ui.ClearPlacingPart();
                    // G.ui.mainShip.LeaveConstructor();

                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{parsedYear}`");
                    G.ui.sharedDesignYear = parsedYear;
                    countryNameText.text = Player.GetNameUI(null, G.ui.sharedDesignPlayer, G.ui.sharedDesignYear);

                    GameManager.Instance.RefreshSharedDesign(parsedYear, G.ui.sharedDesignPlayer);
                    InputChooseYearStaticText.text = value;
                    countryFlagImage.sprite = Player.Flag(G.ui.sharedDesignPlayer, true, null, G.ui.sharedDesignYear);
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
            layout.preferredWidth = 100;
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
            layout.preferredWidth = 100;
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
            GameObject SettingsRoot = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window/Root");

            SettingsRoot.GetParent().transform.SetSiblingIndex(0);

            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/General/Viewport/Content/ArmorQualityValue -> ArmorQualityInPenetrationData

            GameObject GeneralOptionsContent = ModUtils.GetChildAtPath("RightSide/General/Viewport/Content", SettingsRoot);

            GameObject ArmorQualityInPenetrationData = GeneralOptionsContent.GetChild("ArmorQualityInPenetrationData");
            ArmorQualityInPenetrationData.TryDestroyComponent<HorizontalLayoutGroup>();

            GameObject ArmorQualityValue = GeneralOptionsContent.GetChild("ArmorQualityValue");
            ArmorQualityValue.transform.SetParent(ArmorQualityInPenetrationData);
            ArmorQualityValue.transform.localPosition = Vector3.zero;
            RectTransform ArmorQualityValueRT = ArmorQualityValue.GetComponent<RectTransform>();
            ArmorQualityValueRT.offsetMax = new(0, 0);
            ArmorQualityValueRT.offsetMin = new(0, -24);

            GameObject ArmorQualityInPenetrationDataSlider = ArmorQualityInPenetrationData.GetChild("ArmorQualityInPenetrationDataSlider");
            ArmorQualityInPenetrationDataSlider.transform.localPosition = new(90, 0, 0);
            Slider ArmorQualityInPenetrationDataSliderS = ArmorQualityInPenetrationDataSlider.GetComponent<Slider>();
            ArmorQualityInPenetrationDataSliderS.maxValue = Config.Param("taf_settings_max_armor_quality", 400);

            // Show Map Image toggle

            GameObject ShowMapImage = GameObject.Instantiate(GeneralOptionsContent.GetChild("Analytics"));
            ShowMapImage.name = "Show Map Image";
            ShowMapImage.SetParent(GeneralOptionsContent);
            ShowMapImage.transform.SetScale(1, 1, 1);
            SetLocalizedTextTag(ShowMapImage.GetChild("Label"), "$TAF_Ui_Settings_ShowMapImage");
            Toggle ShowMapImageT = ShowMapImage.GetChild("Campaign Toggle").GetComponent<Toggle>();
            ShowMapImageT.onValueChanged.RemoveAllListeners();
            ShowMapImageT.Set(TAF_Settings.settings.showMapImage);
            ShowMapImageT.onValueChanged.AddListener(new System.Action<bool>((bool val) => {
                SetSaveSettingsButtonActive();

                Melon<TweaksAndFixes>.Logger.Msg($"Show map image: {val}");
                TAF_Settings.settings.showMapImage = val;

                if (GameManager.IsWorld)
                {
                    GameObject mapImage = ModUtils.GetChildAtPath("2DMap/Map", WorldCampaign.instance.worldEx);
                    var mapRenderer = mapImage.GetComponent<MeshRenderer>();
                    mapRenderer.enabled = UiM.TAF_Settings.settings.showMapImage;
                }
            }));

            // Deck Prop Spacing

            GameObject deckPropCoverage = GameObject.Instantiate(ArmorQualityInPenetrationData);
            deckPropCoverage.name = "Deck Prop Coverage";
            deckPropCoverage.SetParent(GeneralOptionsContent);
            deckPropCoverage.transform.SetScale(1, 1, 1);
            SetLocalizedTextTag(deckPropCoverage.GetChild("Label"), "$TAF_Ui_Settings_DeckPropCoverage");

            GameObject deckPropCoverageV = deckPropCoverage.GetChild("ArmorQualityValue");
            TMP_Text deckPropCoverageVT = deckPropCoverageV.GetChild("Label").GetComponent<TMP_Text>();
            deckPropCoverageVT.text = $"{TAF_Settings.settings.deckPropCoverage}%";

            GameObject deckPropCoverageS = deckPropCoverage.GetChild("ArmorQualityInPenetrationDataSlider");
            deckPropCoverageS.name = "DeckPropCoverageSlider";
            Slider deckPropCoverageSC = deckPropCoverageS.GetComponent<Slider>();
            deckPropCoverageSC.minValue = 0;
            deckPropCoverageSC.maxValue = 4;
            deckPropCoverageSC.onValueChanged.RemoveAllListeners();
            deckPropCoverageSC.onValueChanged.AddListener(new System.Action<float>((float val) => {
                TAF_Settings.settings.deckPropCoverage = ModUtils.toInt(val * 25);
                deckPropCoverageVT.text = $"{val*25}%";
            }));
            deckPropCoverageSC.OnMouseUp(new System.Action(() => {
                SetSaveSettingsButtonActive();

                float val = TAF_Settings.settings.deckPropCoverage;

                Melon<TweaksAndFixes>.Logger.Msg($"Set deck prop percent: {val}");

                foreach (var ship in ShipM.GetActiveShips())
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Update clutter for ship {ship.Name(false, false)}");
                    Patch_Ship.UpdateDeckClutter(ship);
                }
            }));
            deckPropCoverageSC.Set(TAF_Settings.settings.deckPropCoverage / 25);

            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/General/Viewport/Content/Language/Campaign Dropdown
            // TMP_Dropdown

            GameObject languageDropdown = ModUtils.GetChildAtPath(
                "Global/Ui/UiMain/Popup/Options Window/Root/RightSide/General/Viewport/Content/Language"
            );
            GameObject refitDateFormat = GameObject.Instantiate(languageDropdown);
            refitDateFormat.name = "RefitDateFormatDropdown";
            refitDateFormat.transform.SetParent(GeneralOptionsContent);
            refitDateFormat.transform.SetScale(1, 1, 1);
            SetLocalizedTextTag(refitDateFormat.GetChild("Label"), "$TAF_Ui_Settings_RefitDateFormat");

            TMP_Dropdown dropdown = refitDateFormat.GetChild("Campaign Dropdown").GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            var options = new Il2CppSystem.Collections.Generic.List<string>();
            options.Add($"{ModUtils.NumToMonth(2)} 1920");
            options.Add($"2/1920");
            options.Add($"1920 {ModUtils.NumToMonth(2)}");
            options.Add($"1920/2");
            dropdown.AddOptions(options);
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(new System.Action<int>((int i) => {
                SetSaveSettingsButtonActive();
                Melon<TweaksAndFixes>.Logger.Msg($"Set Refit Date Format: {options[i]} (#{i})");
                TAF_Settings.settings.refitDateFormat = i;
            }));
            dropdown.SetValue(TAF_Settings.settings.refitDateFormat, false);

            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/Sound/Viewport/Content/General Volume

            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/Graphic Options/Viewport/Content

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
            public static TAF_Settings settings = new();
            public static int CurrentSettingsVersion = 1;
            public int version { get; set; }
            public float uiScale { get; set; }
            public float uiScaleDefault { get; set; }
            public bool showMapImage { get; set; }
            public int deckPropCoverage { get; set; }
            /*
             0: Month Year | Feb. 1930
             1: MM/YYYY    | 02/1930
             2: Year Month | 1930 Feb.
             3: YYYY/MM    | 1930/02
             */
            public int refitDateFormat { get; set; }

            public TAF_Settings()
            {
                version = CurrentSettingsVersion;
                uiScale = 2f;
                uiScaleDefault = -1;
                showMapImage = true;
                deckPropCoverage = 50;
                refitDateFormat = 0;
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
                        Melon<TweaksAndFixes>.Logger.Msg($"          version : {TAF_Settings.settings.version}");
                        Melon<TweaksAndFixes>.Logger.Msg($"          uiScale : {TAF_Settings.settings.uiScale}");
                        Melon<TweaksAndFixes>.Logger.Msg($"   uiScaleDefault : {TAF_Settings.settings.uiScaleDefault}");
                        Melon<TweaksAndFixes>.Logger.Msg($"     showMapImage : {TAF_Settings.settings.showMapImage}");
                        Melon<TweaksAndFixes>.Logger.Msg($" deckPropCoverage : {TAF_Settings.settings.deckPropCoverage}%");
                        Melon<TweaksAndFixes>.Logger.Msg($"  refitDateFormat : {TAF_Settings.settings.refitDateFormat}");
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

        // ========== GENERAL ========== //


        public static GameObject InfoText = new();
        public static Text InfoTextElement = new();
        public static float FadeTime = 5.0f;
        public static float TimeLeft = 0f;
        public static bool InfoTextInitalized = false;
        public static bool HasFadeEnded = true;

        public static void ShowTextTopLeft(string text, float fadeTime = 5.0f)
        {
            // TODO: Support queueing if this gets used more often
            if (!HasFadeEnded)
                return;

            if (!InfoTextInitalized)
            {
                InfoText = GameObject.Instantiate(G.ui.overlayUi.GetChild("Version"));
                InfoText.name = "TAF_InfoText";
                InfoText.SetParent(G.ui.overlayUi);
                InfoText.transform.position = new Vector3(500, 2050, 0);
                InfoText.transform.SetScale(1, 1, 1);
                InfoText.GetChild("VersionText").name = "Text";
                InfoTextElement = InfoText.GetChild("Text").GetComponent<Text>();
                InfoTextElement.text = text;
                InfoTextElement.fontSize = 20;

                InfoTextInitalized = true;
            }

            FadeTime = fadeTime;
            TimeLeft = FadeTime;
            InfoTextElement.color = new Color(1, 1, 1, 1);
            HasFadeEnded = false;

            MelonCoroutines.Start(Update());
        }

        internal static System.Collections.IEnumerator Update()
        {
            while (true)
            {
                if (TimeLeft > 0)
                {
                    TimeLeft -= Time.deltaTime;

                    if (TimeLeft <= FadeTime / 2.0f)
                    {
                        InfoTextElement.color = new Color(1, 1, 1, (TimeLeft / FadeTime) * 2);
                    }

                    yield return new WaitForEndOfFrame();
                }
                else if (!HasFadeEnded)
                {
                    HasFadeEnded = true;
                    InfoTextElement.color = new Color(1, 1, 1, 0);
                    TimeLeft = 0;

                    yield break;
                }
            }
        }



        // ========== Function Overrides ========== //

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

                int turnsSinceCheck = CampaignController.Instance.CurrentDate.MonthsPassedSince(rel.LastTreatyCheckDate);
                var checkThresh = rel.TreatyCheckMonthTreashold;
                if (checkThresh == 0)
                {
                    checkThresh = Config.Param("war_min_duration", 5);
                    rel.TreatyCheckMonthTreashold = checkThresh;
                }
                if (turnsSinceCheck < checkThresh)
                    continue;
                rel.LastTreatyCheckDate = CampaignController.Instance.CurrentDate;

                Melon<TweaksAndFixes>.Logger.Msg($"Checking war: {rel.a.Name(false)} ({vpA} vp) vs. {rel.b.Name(false)} ({vpB} vp)");

                // Early check: If the war has gone on for too long with no VP, just call for peace
                int turnsSinceStart = CampaignController.Instance.CurrentDate.MonthsPassedSince(rel.recentWarStartDate);

                if (vpA + vpB < lowVPThreshold && turnsSinceStart > monthsForLowVPWarEnd)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Triggering White Peace Event: Rel A {vpA} vp + Rel B {vpB} vp < {lowVPThreshold} (Low VP Threash) && {turnsSinceStart} (Mo. Since Start) > {monthsForLowVPWarEnd} (Mo. For White Peace)");

                    _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$TAF_Ui_War_WhitePeace"), vpA >= vpB);
                    continue;
                }

                // TODO: Add peace checks for no fleet, revolution, bad finances, blockade, major VP difference
                // if (!a.revolution && a.govermentChangedPenalty > 0)
                // {
                //     Melon<TweaksAndFixes>.Logger.Msg($"  Triggering revolotionary peace for {a.Name(false)}: {a.govermentChangedPenalty} > 0");
                // 
                //     _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$TAF_Ui_War_WhitePeace"), false);
                //     continue;
                // }
                // 
                // if (!b.revolution && b.govermentChangedPenalty > 0)
                // {
                //     Melon<TweaksAndFixes>.Logger.Msg($"  Triggering revolotionary peace for {b.Name(false)}: {b.govermentChangedPenalty} > 0");
                // 
                //     _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$TAF_Ui_War_WhitePeace"), true);
                //     continue;
                // }

                Player loserPlayer = null;

                if (Mathf.Abs(vpB - vpA) >= peace_min_vp_difference && Mathf.Max((vpB + 1f) / (vpA + 1f), (vpB + 1f) / (vpA + 1f)) >= peace_enemy_vp_ratio && vpA + vpB >= peace_vp_sum_prolonged_war)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Rel A {vpA} vp - Rel B {vpB} vp < {peace_min_vp_difference} (Min VP diff) && Rel A {vpA} vp / Rel B {vpB} vp >= {peace_enemy_vp_ratio} (VP Ratio) && Rel A {vpA} vp + Rel B {vpB} vp >= {peace_vp_sum_prolonged_war} (VP Prolonged War)");
                    loserPlayer = vpB > vpA ? a : b;
                    Melon<TweaksAndFixes>.Logger.Msg($"    Chose loser: {loserPlayer.Name(false)}");
                }

                // else if (Mathf.Max((vpB + 1f) / (vpA + 1f), (vpB + 1f) / (vpA + 1f)) > 5)
                // {
                //     Melon<TweaksAndFixes>.Logger.Msg($"  Rel A {vpA} vp / Rel B {vpB} vp >= {peace_enemy_vp_ratio} (VP Ratio)");
                //     loserPlayer = vpB > vpA ? a : b;
                //     Melon<TweaksAndFixes>.Logger.Msg($"    Chose loser: {loserPlayer.Name(false)}");
                // }

                else if (turnsSinceStart >= monthsForEconCollapse)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  {turnsSinceStart} Mo. since start > {monthsForEconCollapse} Mo. for econ collapse");

                    var wgeA = a.WealthGrowthEffective();
                    var wgeB = b.WealthGrowthEffective();

                    Melon<TweaksAndFixes>.Logger.Msg($"    Rel A GDP growth {wgeA}");
                    Melon<TweaksAndFixes>.Logger.Msg($"    Rel B GDP growth {wgeB}");

                    var alRel = CampaignControllerM.GetAllianceRelation(a, b);

                    float vpAA = 0, vpAB = 0;

                    if (alRel != null)
                    {
                        if (alRel.A.Players.Contains(a.data))
                        {
                            vpAA = alRel.vpA;
                            vpAB = alRel.vpB;
                        }
                        else
                        {
                            vpAA = alRel.vpB;
                            vpAB = alRel.vpA;
                        }

                        Melon<TweaksAndFixes>.Logger.Msg($"    Rel A Alliance VP {vpAA}");
                        Melon<TweaksAndFixes>.Logger.Msg($"    Rel B Alliance VP {vpAB}");
                    }

                    // Nation A's econ is failing
                    if (wgeA <= 0 && wgeB > 0)
                    {
                        if (vpAA < vpAB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel A Alliance vp {vpAA} < Rel B Alliance vp {vpAB}");
                            loserPlayer = a;
                        }
                        else if (vpA < vpB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel A vp {vpA} < Rel B vp {vpB}");
                            loserPlayer = a;
                        }
                    }

                    // Nation B's econ is failing
                    else if (wgeB <= 0 && wgeA > 0)
                    {
                        if (vpAA > vpAB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel A Alliance vp {vpAA} > Rel B Alliance vp {vpAB}");
                            loserPlayer = b;
                        }
                        else if (vpA > vpB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel A vp {vpA} > Rel B vp {vpB}");
                            loserPlayer = b;
                        }
                    }

                    // Nation A's and nation B's econ is failing
                    else// if (wgeA <= 0 && wgeB <= 0)
                    {
                        if (vpAA > vpAB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel A Alliance vp {vpAA} > Rel B Alliance vp {vpAB}");
                            loserPlayer = b;
                        }
                        else if (vpAA < vpAB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel B Alliance vp {vpAB} > Rel A Alliance vp {vpAA}");
                            loserPlayer = a;
                        }
                        else if (vpA > vpB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel A vp {vpA} > Rel B vp {vpB}");
                            loserPlayer = b;
                        }
                        else if (vpA < vpB)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel B vp {vpB} > Rel A vp {vpA}");
                            loserPlayer = a;
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"      Rel B vp {vpB} == Rel A vp {vpA}, choosing randomly");
                            loserPlayer = UnityEngine.Random.Range(0, 1) == 0 ? a : b;
                        }
                    }

                    if (loserPlayer != null)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"        Chose loser: {loserPlayer.Name(false)}");
                    }

                    /*if (wgeA <= 0)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"    Rel A GDP growth {wgeA} <= 0");

                        if (wgeB <= 0)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"    Rel B GDP growth {wgeA} <= 0");

                            if (vpAA > vpAB)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    Rel A Alliance vp {vpAA} > Rel B Alliance vp {vpAB}");
                                loserPlayer = b;
                            }
                            else if (vpAA < vpAB)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    Rel B Alliance vp {vpAB} > Rel A Alliance vp {vpAA}");
                                loserPlayer = a;
                            }
                            else if (vpA > vpB)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    Rel A vp {vpA} > Rel B vp {vpB}");
                                loserPlayer = b;
                            }
                            else if (vpA < vpB)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    Rel B vp {vpB} > Rel A vp {vpA}");
                                loserPlayer = a;
                            }
                            else
                            {
                                loserPlayer = UnityEngine.Random.Range(0, 1) == 0 ? a : b;
                            }

                            Melon<TweaksAndFixes>.Logger.Msg($"      Chose loser: {loserPlayer.Name(false)}");
                        }

                        else if ((alRel != null && (alRel.A.Players.Contains(a.data) ?
                                (alRel.vpB > alRel.vpA)
                                : (alRel.vpA > alRel.vpB))))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"    Rel A {(alRel.A.Players.Contains(a.data) ? alRel.vpB : alRel.vpA)} aliance vp > Rel B {(alRel.A.Players.Contains(a.data) ? alRel.vpA : alRel.vpB)} aliance vp"
                            );
                            loserPlayer = vpB > vpA ? a : b;
                            Melon<TweaksAndFixes>.Logger.Msg($"      Chose loser: {loserPlayer.Name(false)}");
                        }
                        else
                        {
                            loserPlayer = vpB > vpA ? a : b;
                        }
                    }
                    else if (wgeB <= 0)
                    {
                        if (vpA > vpB
                            || (alRel != null
                                && (alRel.A.Players.Contains(a.data) ?
                                    (alRel.vpA > alRel.vpB)
                                    : (alRel.vpB > alRel.vpA))))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"    Rel A {vpA} vp > Rel B {vpB} vp || Rel A {(alRel.A.Players.Contains(a.data) ? alRel.vpA : alRel.vpB)} aliance vp > Rel B {(alRel.A.Players.Contains(a.data) ? alRel.vpB : alRel.vpA)} aliance vp"
                            );
                            loserPlayer = a;
                            Melon<TweaksAndFixes>.Logger.Msg($"      Chose loser: {loserPlayer.Name(false)}");
                        }
                    }*/
                }

                if (loserPlayer != null)
                {
                    if (loserPlayer == a)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Result (Show to player: {hasHuman}): Loser {a.data.nameUi}, Winner {b.data.nameUi}");
                        _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$Ui_World_TheWarIsNotGoingWellThe") + "{0} {1}" + LocalizeManager.Localize("$Ui_World_asksYouShouldAskUnfPeace").Replace("{0}", "{2}"), false);
                    }
                    else
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Result (Show to player: {hasHuman}): Loser {b.data.nameUi}, Winner {a.data.nameUi}");
                        _this.AskForPeace(hasHuman, rel, PlayerController.Instance, LocalizeManager.Localize("$Ui_World_WeAreWinningSnThe") + "{0} {1}" + LocalizeManager.Localize("$Ui_World_desperAsksPeaceTreaty"), true);
                    }
                }
            }
        }

        public static void PrintTreatyRelationsMatrix()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Active treaties:");

            foreach (var rel in CampaignController.Instance.CampaignData.Relations)
            {
                if (rel.Value.PeaceTreatyChance != -1)
                {
                    Melon<TweaksAndFixes>.Logger.Msg(
                        $"  {rel.Key.Key.Name(false)} : {rel.Key.Value.Name(false)} -> {rel.Value.PeaceTreatyChance}% (Peace treaty allowed: {rel.Value.CanSignPeace()})"
                    );
                }
            }
        }

        public static void AskForPeace(Ui _this, bool isPlayerRelation, Relation relation, Player whoAsk, string msg, bool oppositePlayer)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Ask for peace: {relation.a.Name(false)} vs. {relation.b.Name(false)}");

            if (!isPlayerRelation)
            {
                CampaignController.Instance.PeaceTreaty(relation, UnityEngine.Random.value < 0.5f);
                PrintTreatyRelationsMatrix();
                return;
            }

            if (oppositePlayer)
            {
                whoAsk = relation.PlayerBesides(whoAsk);
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  Winner: {whoAsk.Name(false)} vs. Loser: {relation.PlayerBesides(whoAsk).Name(false)}");

            string playerNameA = whoAsk.Name();
            string playerGovtA = whoAsk.GovermentType();
            string playerNameB = relation.PlayerBesides(whoAsk).Name();

            string message = string.Empty;

            if (msg.Contains("{2}"))
            {
                message = String.Format(msg, playerNameA, playerGovtA, playerNameB);
            }
            else
            {
                message = String.Format(msg, playerNameA, playerGovtA);
            }

            string peaceTreaty = LocalizeManager.Localize("$Ui_World_Politics_PeaceTreaty");
            string agree = LocalizeManager.Localize("$Ui_World_Agree");
            string fightToTheEnd = LocalizeManager.Localize("$Ui_World_FightToTheEnd");

            Melon<TweaksAndFixes>.Logger.Msg($"  Message: {message}");

            MessageBoxUI.Show(
                peaceTreaty, message, null, true, agree, fightToTheEnd,
                new System.Action(() => {
                    CampaignController.Instance.PeaceTreaty(relation, true);
                    G.ui.RefreshCampaignUI();
                    PrintTreatyRelationsMatrix();
                }),
                new System.Action(() => {
                    CampaignController.Instance.PeaceTreaty(relation, false);
                    G.ui.RefreshCampaignUI();
                    PrintTreatyRelationsMatrix();
                })
            );
        }

        public static void UpdateCampaignCamera(Cam _this)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"  Campaign:");

            // ========== Configuration ========== //

            // Update width/height
            if (Screen.width != _this.width || Screen.height != _this.height)
            {
                _this.CampaignModePositionChanged = true;
                _this.width = Screen.width;
                _this.height = Screen.height;
                _this.screenSizeRation = Screen.width / Screen.height;
            }

            // Orthographic config
            if (!_this.cameraComp.orthographic)
            {
                _this.cameraComp.orthographic = true;
                _this.cameraComp.transform.eulerAngles = new(90, 180, 0);
            }

            // ========== Get Input ========== //

            Vector3 mp = Input.mousePosition;
            bool isMouseOverGame = !(0 > mp.x || 0 > mp.y || Screen.width < mp.x || Screen.height < mp.y);

            bool canZoomWithScrollWheel = 
                (BasePopupWindow.AllowScroll || !UiM.showPopups)
                && GameManager.IsWorldMap
                && GameManager.CanHandleMouseInput() && isMouseOverGame;

            bool canPan =
                (!BasePopupWindow.IsAnyPopupActive || !UiM.showPopups)
                && GameManager.IsWorldMap && isMouseOverGame;

            if (canZoomWithScrollWheel)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"    Scroll {Input.mouseScrollDelta.y}");

                _this.MinFov = 3;
                _this.MaxFov = 45;

                _this.fov -= Input.mouseScrollDelta.y * _this.Sensitivity * (_this.fov / _this.distanceBase);
                // Melon<TweaksAndFixes>.Logger.Msg($"    Fov {_this.fov}");
                _this.fov = Mathf.Clamp(_this.fov, _this.MinFov, _this.MaxFov);
                // Melon<TweaksAndFixes>.Logger.Msg($"    Fov Clamped {_this.fov}");
            }

            if (Input.GetMouseButtonDown(0) && GameManager.IsWorldMap)
            {
                _this.prevMousePos = Input.mousePosition;
            }

            _this.newCameraPos = _this.transform.position;

            // Mouse map pan
            if (Input.GetMouseButton(0) && canPan && (GameManager.CanHandleMouseInput() || Cam.IsDrag))
            {
                _this.newCameraPos =
                    _this.transform.position +
                    new Vector3(Input.mousePosition.x - _this.prevMousePos.x, 0, Input.mousePosition.y - _this.prevMousePos.y) * Time.deltaTime * _this.CampaignMapMoveSpeed;

                _this.prevMousePos = Input.mousePosition;
                _this.CampaignModePositionChanged = true;
                Cam.IsDrag = true;
            }
            else
            {
                Cam.IsDrag = false;
            }

            // Keyboard map pan
            if (canPan && GameManager.CanHandleKeyboardInput())
            {
                Vector3 keyboardInput = _this.GetKeyboardInputForMoveCam(false, true);

                if (keyboardInput != Vector3.zero)
                {
                    _this.newCameraPos =
                        _this.transform.position + new Vector3(keyboardInput.x, 0, keyboardInput.y) * Time.deltaTime * _this.CampaignMapMoveSpeed;
                    _this.CampaignModePositionChanged = true;
                }
            }

            // ========== Update Cam ========== //

            float newFov = Mathf.Lerp(_this.cameraComp.orthographicSize, _this.fov, _this.zoomDampen * Time.deltaTime);

            // Melon<TweaksAndFixes>.Logger.Msg($"    Target {newFov}");

            _this.cameraComp.orthographicSize = newFov;
            _this.prevFov = _this.fov;

            // Melon<TweaksAndFixes>.Logger.Msg($"    Ortho {_this.cameraComp.orthographicSize}");

            if (_this.CampaignMapPrevOrthoSize != newFov)
            {
                _this.CampaignModePositionChanged = true;
                _this.CampaignMapPrevOrthoSize = newFov;
            }

            _this.CampaignMapFovPercents = (newFov - _this.MinFov) / (_this.MaxFov - _this.MinFov);
            _this.CampaignMapMoveSpeed = 2.5f * _this.CampaignMapFovPercents + 0.25f;

            // Check minimap position
            if (_this.newPosFromMiniMap != Vector3.zero)
            {
                _this.newCameraPos = new(_this.newPosFromMiniMap.x, _this.transform.position.y, _this.newPosFromMiniMap.z);
                _this.newPosFromMiniMap = Vector3.zero;
                _this.CampaignModePositionChanged = true;
            }

            // Force to proper y position for map
            _this.newCameraPos = new(_this.newCameraPos.x, 10f, _this.newCameraPos.z);

            // Update position and clamp to map bounds
            _this.transform.position = _this.newCameraPos;
            _this.CheckCameraBorders();

            // Configure grid alpha
            if (_this.Grid != null)
            {
                _this.grid3Alpha = Mathf.Clamp01((_this.CampaignMapFovPercents - 0.5f) + (_this.CampaignMapFovPercents + 0.5f));
                _this.Grid.SetColor("_LineColor1", new Color(0, 0, 0, _this.grid1Alpha * _this.MaxAlpha));
                _this.grid2Alpha = Mathf.Clamp01((_this.CampaignMapFovPercents / 0.66f) - _this.grid3Alpha + 0.1f);
                _this.Grid.SetColor("_LineColor2", new Color(0, 0, 0, _this.grid2Alpha * _this.MaxAlpha));
                _this.grid1Alpha = 1.5f - Mathf.Clamp01(_this.CampaignMapFovPercents / 0.33f) - _this.grid3Alpha;
                _this.Grid.SetColor("_LineColor3", new Color(0, 0, 0, _this.grid3Alpha * _this.MaxAlpha));
            }
        }

        private static bool isRmbDrag = false;
        private static bool isMmbDrag = false;
        private static bool ignoreFirstRmbFrame = true;
        private static bool ignoreFirstMmbFrame = true;

        public static void UpdateOrbitCamera(Cam _this)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"  Not Campaign:");

            // ========== Configuration ========== //

            // Orthographic config
            if (_this.cameraComp.orthographic)
            {
                _this.cameraComp.orthographic = false;
                _this.cameraComp.fieldOfView = 50;
            }

            // ========== Get Input ========== //

            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0.0f, 0.05f);

            Vector3 mp = Input.mousePosition;
            bool isMouseOverGame = !(0 > mp.x || 0 > mp.y || Screen.width < mp.x || Screen.height < mp.y);

            bool allowCameraControl = G.ui.AllowCameraControl() && isMouseOverGame;

            Vector3 deltaPan = new Vector3();

            bool isBattle = Patch_SceneManager.sceneState == GameManager.GameState.Battle;

            // Melon<TweaksAndFixes>.Logger.Msg($"{isMouseOverGame} | {G.ui.AllowCameraControl()} | {GameManager.CanHandleMouseInput()}");

            if (allowCameraControl && GameManager.CanHandleMouseInput())
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"    Allow cam control & can handle mouse input:");

                // Calculate zoom
                float mouseDelta = _this.allowMouseScroll.Invoke() ? -Input.mouseScrollDelta.y : 0;
                float zoomDelta = _this.zoomSensitivity * mouseDelta;
                bool camUpPressed = Input.GetKey(G.settings.Bindings.CameraUp.Code);
                bool camDownPressed = Input.GetKey(G.settings.Bindings.CameraDown.Code);

                // Melon<TweaksAndFixes>.Logger.Msg($"    Calc Zoom: {_this.allowMouseScroll.Invoke()} | {-Input.mouseScrollDelta.y} | {_this.zoomSensitivity} | {zoomDelta}");

                float zoomTo = Mathf.Clamp(
                    ((camUpPressed ? 3 : 0) - (camDownPressed ? 3 : 0) + zoomDelta) * (_this.distance / _this.distanceBase) + _this.distanceDesired,
                    _this.distanceMin,
                    _this.distanceMax
                );

                _this.distanceDesired = zoomTo;
            }

            // Mouse camera rotation
            if (allowCameraControl && (GameManager.CanHandleMouseInput() || isRmbDrag) && Input.GetMouseButton(1))
            {
                // Always skip the first frame update for camera drag.
                // Should prevent the camera jerking from happening anymore.
                if (ignoreFirstRmbFrame)
                {
                    _this.prevMousePos = Input.mousePosition;
                    ignoreFirstRmbFrame = false;
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"{Input.mousePosition.ToString("F0"),-16} -> {_this.prevMousePos.ToString("F0"),-16} = {(Input.mousePosition - _this.prevMousePos).ToString("F0"),-16} : dt = {dt}");

                _this.rotationX -=
                    (_this.allowRotateX ?
                        (Input.mousePosition.y - _this.prevMousePos.y) * dt * _this.rotationSensitivityX : 0);

                _this.rotationY +=
                    (_this.allowRotateY ?
                        (Input.mousePosition.x - _this.prevMousePos.x) * dt * _this.rotationSensitivityY : 0);

                _this.rotationX = Mathf.Clamp(_this.rotationX, _this.limitMinRotationX, _this.limitMaxRotationX);

                isRmbDrag = true;

                // Melon<TweaksAndFixes>.Logger.Msg($"      {Input.mousePosition} -> {_this.prevMousePos}");
            }
            else
            {
                ignoreFirstRmbFrame = true;
                isRmbDrag = false;
            }

            if (allowCameraControl && (GameManager.CanHandleMouseInput() || isMmbDrag) && Input.GetMouseButton(2))
            {
                // Always skip the first frame update for camera drag.
                // Should prevent the camera jerking from happening anymore.
                if (ignoreFirstMmbFrame)
                {
                    _this.prevMousePos = Input.mousePosition;
                    ignoreFirstMmbFrame = false;
                }

                deltaPan += -Vector3.forward * (Input.mousePosition.y - _this.prevMousePos.y) / Screen.height * _this.panSensitivityX +
                            -Vector3.right * (Input.mousePosition.x - _this.prevMousePos.x) / Screen.width * _this.panSensitivityY;

                isMmbDrag = true;
            }
            else
            {
                ignoreFirstMmbFrame = true;
                isMmbDrag = false;
            }

            // Keyboard camera rotation
            if (allowCameraControl && !Util.FocusIsInInputField() && GameManager.CanHandleKeyboardInput())
            {
                if (Input.GetKey(isBattle ?
                    G.settings.Bindings.BattleCameraRotationLeft.Code :
                    G.settings.Bindings.ConstructorCameraRotationLeft.Code))
                    _this.rotationY = (dt * _this.rotationSensitivityKeyMod * _this.rotationSensitivityY) + _this.rotationY;

                if (Input.GetKey(isBattle ?
                    G.settings.Bindings.BattleCameraRotationRight.Code :
                    G.settings.Bindings.ConstructorCameraRotationRight.Code))
                    _this.rotationY = -(dt * _this.rotationSensitivityKeyMod * _this.rotationSensitivityY) + _this.rotationY;
            }

            if (allowCameraControl && !Util.FocusIsInInputField())
            {
                Vector3 keyboardInput = _this.GetKeyboardInputForMoveCam(false, false);

                // Melon<TweaksAndFixes>.Logger.Msg($"      X: {_this.rotationX} | Y: {_this.rotationY}");
                // Melon<TweaksAndFixes>.Logger.Msg($"      In: {keyboardInput}");

                deltaPan += keyboardInput;
            }

            // Zoom transition
            if (_this.transition != null)
            {
                if (_this.transition.isRunning)
                {
                    Vector3 ease = Util.EaseInOut(
                        _this.transitionFrom,
                        _this.transitionTo,
                        _this.transition.progress
                    );

                    _this.lookingAt = ease;

                    float zoomTo = Util.Clamp(
                        (ease - _this.transitionFrom).magnitude
                            * Mathf.Sin(_this.transition.progress * 3.14159265f) * 0.2f
                            + _this.transitionDistance, 0, 4000
                    );

                    _this.distance = zoomTo;
                    _this.distanceDesired = zoomTo;
                }
                else
                {
                    _this.lookingAt = _this.transitionTo;
                    _this.distance = _this.transitionDistance;
                    _this.transition = null;
                }
            }

            if (_this.plane != null)
            {
                // Check if the camera has been panned
                _this.inputScroll = deltaPan != Vector3.zero;

                // Parse Pan input
                float radX = (_this.limitMaxRotationX - _this.rotationX + _this.limitMinRotationX) / 180f * Mathf.PI;
                float radY = (_this.rotationY - 90f) / 180f * Mathf.PI;

                float panMult = _this.scrollSensitivity *
                    Mathf.Pow(1.2f, G.settings.panSensitivity) *
                    Mathf.Pow(_this.distance / _this.distanceBase, 0.8f);

                deltaPan *= panMult * dt;

                _this.lookingAt += new Vector3(
                    -deltaPan.x * Mathf.Sin(radY) + deltaPan.z * Mathf.Cos(radY),
                    0,
                    -deltaPan.x * Mathf.Cos(radY) - deltaPan.z * Mathf.Sin(radY)
                );

                // Melon<TweaksAndFixes>.Logger.Msg($"      Pan: {deltaPan} : {Mathf.Sin(radY)} | {Mathf.Cos(radY)}");

                _this.distance = Mathf.Lerp(_this.distance, _this.distanceDesired, _this.zoomDampen * dt);

                // Clamp lookingAt to bounds
                if (_this.transition == null)
                {
                    Vector3 planeCollider = _this.plane.size;

                    _this.lookingAt =
                        Mathf.Clamp(_this.lookingAt.x, -planeCollider.x * 0.5f, planeCollider.x * 0.5f) * Vector3.right +
                        Mathf.Clamp(_this.lookingAt.z, -planeCollider.z * 0.5f, planeCollider.z * 0.5f) * Vector3.forward;
                }

                // Update position & rotation
                Vector3 originalPosition = _this.transform.position;
                Vector3 offset = new Vector3(
                    _this.distance * _this.distanceMod * Mathf.Sin(-radX) * Mathf.Cos(-radY),
                    _this.distance * _this.distanceMod * Mathf.Cos(-radX),
                    _this.distance * _this.distanceMod * Mathf.Sin(-radX) * Mathf.Sin(-radY)
                );
                Vector3 focusOffset = new Vector3();
                Vector3 verticalOffset = new Vector3();

                // Battle camera has different camera positioning
                if (isBattle)
                {
                    // Several hours of my life were spent finding these ratios...
                    focusOffset = new Vector3(0, _this.distance / 6f, 0);
                    verticalOffset = new Vector3(0, _this.distance / 30f, 0);
                }

                _this.transform.position =
                    _this.plane.transform.position + _this.plane.transform.TransformDirection(_this.plane.center) +
                    _this.lookingAt + offset + verticalOffset;
                _this.transform.forward = -offset + focusOffset;

                // Update tracking variables
                _this.cameraMovement = _this.transform.position - originalPosition;
                _this.prevMousePos = Input.mousePosition;
                _this.lookingAtPosition = _this.transform.TransformPoint(_this.lookingAt);

                // Melon<TweaksAndFixes>.Logger.Msg($"{_this.prevMousePos.ToString("F0"),-16}");

                var dof = _this.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
                if (dof != null)
                {
                    dof.focalLength = _this.distance * _this.distanceMod;
                    // Melon<TweaksAndFixes>.Logger.Msg($"      F-Len: {dof.focalLength}");
                }
            }
        }

        public static void SettupMainMenuCam(Cam _this)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"  Not Campaign:");

            // ========== Configuration ========== //

            // Orthographic config
            if (_this.cameraComp.orthographic)
            {
                _this.cameraComp.orthographic = false;
                _this.cameraComp.fieldOfView = 50;
            }

            // Zoom transition
            if (_this.transition != null)
            {
                if (_this.transition.isRunning)
                {
                    Vector3 ease = Util.EaseInOut(
                        _this.transitionFrom,
                        _this.transitionTo,
                        _this.transition.progress
                    );

                    _this.lookingAt = ease;

                    float zoomTo = Util.Clamp(
                        (ease - _this.transitionFrom).magnitude
                            * Mathf.Sin(_this.transition.progress * 3.14159265f) * 0.2f
                            + _this.transitionDistance, 0, 4000
                    );

                    _this.distance = zoomTo;
                    _this.distanceDesired = zoomTo;
                }
                else
                {
                    _this.lookingAt = _this.transitionTo;
                    _this.distance = _this.transitionDistance;
                    _this.transition = null;
                }
            }

            // Parse Pan input
            float radX = (_this.limitMaxRotationX - _this.rotationX + _this.limitMinRotationX) / 180f * Mathf.PI;
            float radY = (_this.rotationY - 90f) / 180f * Mathf.PI;

            // Update position & rotation
            Vector3 offset = new Vector3(
                _this.distance * _this.distanceMod * Mathf.Sin(-radX) * Mathf.Cos(-radY),
                _this.distance * _this.distanceMod * Mathf.Cos(-radX),
                _this.distance * _this.distanceMod * Mathf.Sin(-radX) * Mathf.Sin(-radY)
            );
            
            _this.transform.position = _this.lookingAt + offset;
            _this.transform.forward = -offset;

            // Update tracking variables
            _this.cameraMovement = new();
            _this.prevMousePos = Input.mousePosition;
            _this.lookingAtPosition = _this.transform.TransformPoint(_this.lookingAt);

            // Melon<TweaksAndFixes>.Logger.Msg($"{_this.prevMousePos.ToString("F0"),-16}");

            var dof = _this.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
            if (dof != null)
            {
                dof.focalLength = _this.distance * _this.distanceMod;
                // Melon<TweaksAndFixes>.Logger.Msg($"      F-Len: {dof.focalLength}");
            }
        }

        public static void CamUpdate(Cam _this)
        {
            _this.rotationSensitivityKeyMod = 10;
            _this.panSensitivityX = 50;
            _this.panSensitivityY = 50;

            _this.distanceMin = 1;
            _this.limitMaxRotationX = 89.95f;

            _this.CampaignModePositionChanged = false;

            // Melon<TweaksAndFixes>.Logger.Msg($"Update: {GameManager.Instance.CurrentState}");
            
            switch (Patch_SceneManager.sceneState)
            {
                case GameManager.GameState.Battle:
                case GameManager.GameState.Constructor:
                    if (Patch_SceneManager.sceneState == GameManager.GameState.Constructor
                        && PlayerController.Instance.Ship != null)
                    {
                        _this.distanceMax = 300;
                        _this.distanceStart = PlayerController.Instance.Ship.hullSize.size.z;
                        _this.startingRotationX = 45;
                        _this.startingRotationY = 270;
                    }
                    else if (Patch_SceneManager.sceneState == GameManager.GameState.Battle)
                    {
                        _this.distanceMax = 5000;
                        _this.distanceStart = 500;
                        _this.startingRotationX = 30;
                        _this.startingRotationY = 0;
                    }

                    if (GameManager.Instance.CurrentState == GameManager.GameState.LoadingCustom)
                    {
                        _this.rotationX = _this.startingRotationX;
                        _this.rotationY = _this.startingRotationY;
                        _this.distance = _this.distanceStart;
                        _this.distanceDesired = _this.distanceStart;
                        if(PlayerController.Instance.Ship != null)
                        {
                            _this.lookingAt = PlayerController.Instance.Ship.transform.position;
                            _this.distance = PlayerController.Instance.Ship.hullSize.size.z;
                        }
                        // isFirstAnimation = true;
                    }

                    UpdateOrbitCamera(_this);
                    break;
                
                case GameManager.GameState.World:
                    if (GameManager.Instance.CurrentState != GameManager.GameState.LoadingCustom)
                        UpdateCampaignCamera(_this);
                    // else
                    //     _this.CampaignModePositionChanged = true;
                    break;

                default:
                    break;

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
