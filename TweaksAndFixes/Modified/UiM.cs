using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using Il2CppTMPro;

#pragma warning disable CS8600
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public class UiM
    {
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

            public UiModification MultiplyAnchors(float min, float max)
            {
                modAnchor = true;
                this.anchorMin = anchorMinOriginal * min;
                this.anchorMax = anchorMaxOriginal * max;

                return this;
            }

            public UiModification MultiplyAnchors(Vector2 min, Vector2 max)
            {
                modAnchor = true;
                this.anchorMin = anchorMinOriginal * min;
                this.anchorMax = anchorMaxOriginal * max;

                return this;
            }

            public UiModification ResetAnchors()
            {
                modAnchor = false;
                this.anchorMin = anchorMinOriginal;
                this.anchorMax = anchorMaxOriginal;

                return this;
            }


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

                return this;
            }

            public UiModification ReplaceOffsetMax(Vector2 offsetMax)
            {
                modOffset = true;
                this.offsetMax = offsetMax;

                return this;
            }

            public UiModification MultiplyOffsets(float min, float max)
            {
                modOffset = true;
                this.offsetMin = offsetMinOriginal * min;
                this.offsetMax = offsetMaxOriginal * max;

                return this;
            }

            public UiModification MultiplyOffsets(Vector2 min, Vector2 max)
            {
                modOffset = true;
                this.offsetMin = offsetMinOriginal * min;
                this.offsetMax = offsetMaxOriginal * max;

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

            public UiModification MultiplyLayoutDimensions(float width, float height)
            {
                modLayoutDimensions = true;
                this.layoutWidth = layoutWidthOriginal * width;
                this.layoutHeight = layoutHeightOriginal * height;

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

            public UiModification MultiplyAnchoredPosition(Vector2 mult)
            {
                modAnchoredPosition = true;
                this.anchoredPosition = anchoredPositionOriginal * mult;

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

        public static void ApplyUiModifications()
        {
            UiM.ModifyUi(G.ui.FleetWindow.Root.GetChild("Root")).MultiplyOffsets(new Vector2(800f / 640f, 400f / 343.9f), new Vector2(800f / 640f, 400f / 343.9f));

            ApplyCampaginDesignTabModifications();
            ApplyCampaignFleetTabModifications();

            ApplySettingsMenuModifications();

            // Global/Ui/UiMain/Constructor/Left/Scroll View/

            GameObject ConstructorLeftPannel = ModUtils.GetChildAtPath("Global/Ui/UiMain/Constructor/Left/Scroll View");

            ModifyUi(ConstructorLeftPannel).SetChildOrder("Scrollbar Vertical", "Scrollbar Horizontal", "Viewport");

            // uiRangesCont -> Child -> RangeCanvas

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/NationAndYearSelection/ChooseYear

            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/ShipName

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
            InputField InputChooseYearEditField = InputChooseYearEdit.GetComponent<InputField>();
            InputChooseYearEditField.text = "1890";
            //InputChooseYearEditField.textComponent.fontSize += 5;

            GameObject InputChooseYearStatic = InputChooseYear.GetChild("EditName").GetChild("Static");
            InputChooseYearStatic.GetChild("Header").TryDestroy();
            InputChooseYearStatic.transform.SetScale(1.3f, 1.3f, 1.3f);
            Text InputChooseYearStaticText = InputChooseYearStatic.GetChild("Text").GetComponent<Text>();
            InputChooseYearStaticText.text = "1890";
            //InputChooseYearStaticText.fontSize += 5;

            GameObject spacer = GameObject.Instantiate(InputChooseYear.GetChild("EditName").GetChild("Static").GetChild("EditIcon"));
            spacer.transform.SetParent(InputChooseYearStatic.transform);
            spacer.transform.SetScale(1f, 1f, 1f);
            spacer.transform.SetSiblingIndex(0);
            spacer.TryDestroyComponent<Image>();
            spacer.TryDestroyComponent<CanvasGroup>();
            spacer.TryDestroyComponent<Outline>();

            Button btn = InputChooseYear.GetChild("EditName").AddComponent<Button>();
            btn.onClick.AddListener(new System.Action(() =>
            {
                InputChooseYearBG.SetActive(true);
                InputChooseYearEdit.SetActive(true);
                InputChooseYearEditField.ActivateInputField();
                InputChooseYearStatic.SetActive(false);
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

            GameObject regularPopupsRoot = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows");
            var regularPopups = regularPopupsRoot.GetChildren();
            GameObject basePopupsRoot = ModUtils.GetChildAtPath("Ui/UiMain", G.container);

            bool showPopups = true;

            GameObject nextTurnButton = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/Windows/Map Window/Next Turn Panel/Next Turn Button");
            GameObject hidePopupsButton = GameObject.Instantiate(nextTurnButton);
            hidePopupsButton.transform.SetParent(ModUtils.GetChildAtPath("Ui/UiMain", G.container));
            hidePopupsButton.name = "Hide Popups";
            hidePopupsButton.transform.SetScale(1,1,1);
            hidePopupsButton.transform.position = new Vector3(3490, 110, 0);
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

                if (!GameManager.IsWorld)
                {
                    hasPopups = false;
                    showPopups = true;
                    return;
                }

                foreach (GameObject child in regularPopups)
                {
                    if (child.name != "Event Window" && child.name != "Battle Window" && child.name != "WarReparationWindowUI" && child.name != "EventPopupUI") continue;

                    if (child.active)
                    {
                        hasPopups = true;
                        break;
                    }
                }
            
                regularPopupsRoot.SetActive(showPopups);
            
                if (basePopupsRoot.GetChild("MessageBox(Clone)", true) != null)
                {
                    bool isFirst = true;

                    var children = basePopupsRoot.GetChildren();

                    foreach (GameObject child in children)
                    {
                        if (child.name != "MessageBox(Clone)") continue;
                        
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

                hidePopupsButton.SetActive((hasPopups || !showPopups) && (!GameManager.IsLoadingScreenActive && GameManager.IsWorldMap));
            }));

            // Global/Ui/UiMain/WorldEx/Windows/Map Window/Next Turn Panel/Next Turn Button

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

        private static Slider beamSliderComp;

        public static void OnConstructorShipChanged()
        {
            Ship ship = ShipM.GetActiveShip();

            Melon<TweaksAndFixes>.Logger.Msg($"OnConstructorShipChanged");

            if (ship == null) return;

            Melon<TweaksAndFixes>.Logger.Msg($"{ship.hull.data.beamMin} ~ {ship.hull.data.beamMax}");

            beamSliderComp.minValue = ship.hull.data.beamMin * 10;
            beamSliderComp.maxValue = ship.hull.data.beamMax * 10;
            beamSliderComp.value = ship.beam * 10;
        }

        private static void ApplyCampaginDesignTabModifications()
        {
            // DESIGNS

            GameObject capacityBar = G.ui.FleetWindow.Root.GetChild("Root").GetChild("Shipbuilding Capacity Header");

            // UiM.ModifyUi(capacityBar).MultiplyOffsets(Vector2.one, new Vector2(0.9f, 1.0f));

            // MIN: -837.9719 -601.6474 -> -1200 -700
            // MAX: -7.8599 -44.3526 -> 0 -44.3526

            GameObject shipDesigns = G.ui.FleetWindow.Root.GetChild("Root").GetChild("Design Ships");

            UiM.ModifyUi(shipDesigns).MultiplyOffsets(new Vector2(1200f / 837.9719f, -700f / -601.6474f), Vector2.one);

            UiM.ModifyUi(shipDesigns.GetChild("Scrollbar Vertical")).MultiplyOffsets(new Vector2(1.0f, 0.0f), Vector2.one);//.ReplaceAnchoredPosition(new Vector2(-10, 0));

            // MIN: -837.9719 -44.3531 -> -1200 -44.3531
            // MAX: -8.1926 -14.1529 -> -20 -14.1529

            GameObject designHeader = G.ui.FleetWindow.DesignHeader.gameObject;

            UiM.ModifyUi(designHeader).MultiplyOffsets(new Vector2(1200f / 837.9719f, 1.0f), Vector2.one);//new Vector2(-20f / -8.1926f, 1.0f));

            // Design Ship Info / ShipIsoImage / ShipIsometrImage
            // MIN -0 -314.4801 -> 0 -332.5008
            // MAX 314.48 -0.0001 -> 350 17.4992

            GameObject shipIsoImage = G.ui.FleetWindow.DesignShipInfoRoot.GetChild("ShipIsoImage").GetChild("ShipIsometrImage");

            UiM.ModifyUi(shipIsoImage).MultiplyOffsets(new Vector2(1.0f, -332.5f / -314.4801f), new Vector2(350f / 314.48f, 1.0f));

            // Design Ship Info / Text / ShipTextInfo
            // MIN 0 -271.12 -> -20 -470
            // MAX 314.48 0 -> 350 0

            GameObject shipTextInfo = G.ui.FleetWindow.DesignShipInfoRoot.GetChild("Text").GetChild("ShipTextInfo");

            UiM.ModifyUi(shipTextInfo).MultiplyOffsets(new Vector2(1.0f, -470f / -271.12f), new Vector2(350f / 314.48f, 1.0f));

            GameObject designHeaderName = G.ui.FleetWindow.DesignHeader.GetChild("Name");

            UiM.ModifyUi(designHeaderName).MultiplyLayoutDimensions(500f / 200f, 1.0f);

            GameObject designTemplateName = G.ui.FleetWindow.DesignTemplate.Name.gameObject;

            UiM.ModifyUi(designTemplateName).MultiplyLayoutDimensions(500f / 200f, 1.0f);

            // Design Buttons
            // -1042.223 31.1152

            GameObject designButtons = G.ui.FleetWindow.DesignButtonsRoot;

            UiM.ModifyUi(designButtons).MultiplyOffsets(new Vector2(-1200f / -1042.223f, 1.0f), Vector2.one);

        }

        private static void ApplyCampaignFleetTabModifications()
        {
            // Fleet Buttons

            FleetWindow_ShipElementUI fleetTemplate = G.ui.FleetWindow.FleetTemplate;

            GameObject fleetButtons = G.ui.FleetWindow.FleetButtonsRoot;

            UiM.ModifyUi(fleetButtons).MultiplyOffsets(new Vector2(-1585f / -1271.83f, 1f), Vector2.one);

            fleetTemplate.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();

            GameObject setRole = GameObject.Instantiate(fleetButtons.GetChild("View"));
            setRole.transform.localPosition = new Vector3();
            setRole.transform.SetParent(fleetButtons.GetComponent<LayoutGroup>().transform);
            setRole.transform.SetScale(1.2114f, 1.2114f, 1.2114f);
            setRole.name = "Set Role";
            GameObject setRoleText = setRole.gameObject.GetChildren()[0];
            setRoleText.GetComponent<TMP_Text>().text = "Set Role";
            setRoleText.TryDestroyComponent<LocalizeText>();
            Button setRoleButton = setRole.GetComponent<Button>();
            setRoleButton.onClick.RemoveAllListeners();
            setRoleButton.onClick.AddListener(new System.Action(() =>
            {
                if (G.ui.FleetWindow.selectedElements.Count > 0)
                {
                    G.ui.FleetWindow.selectedElements[^1].RoleSelectionButton.onClick.Invoke();
                }
            }));

            GameObject setCrew = GameObject.Instantiate(fleetButtons.GetChild("View"));
            setCrew.transform.localPosition = new Vector3();
            setCrew.transform.SetParent(fleetButtons.GetComponent<LayoutGroup>().transform);
            setCrew.transform.SetScale(1.2114f, 1.2114f, 1.2114f);
            setCrew.name = "Set Crew";
            GameObject setCrewText = setCrew.gameObject.GetChildren()[0];
            setCrewText.GetComponent<TMP_Text>().text = "Set Crew";
            setCrewText.TryDestroyComponent<LocalizeText>();
            Button setCrewButton = setCrew.GetComponent<Button>();
            setCrewButton.onClick.RemoveAllListeners();
            setCrewButton.onClick.AddListener(new System.Action(() =>
            {
                if (G.ui.FleetWindow.selectedElements.Count == 1)
                {
                    G.ui.FleetWindow.selectedElements[0].CrewAction.onClick.Invoke();

                    GameObject popup = G.ui.gameObject.GetChild("MessageBox(Clone)", true);

                    if (popup != null)
                    {
                        Slider slider = popup.GetChild("Root").GetChild("Campaign Slider").GetComponent<Slider>();

                        // slider.Set(G.ui.FleetWindow.selectedElements[0].CurrentShip.GetTotalCrew());
                    }
                }
            }));

            GameObject viewOnMap = GameObject.Instantiate(fleetButtons.GetChild("View"));
            viewOnMap.transform.localPosition = new Vector3();
            viewOnMap.transform.SetParent(fleetButtons.GetComponent<LayoutGroup>().transform);
            viewOnMap.transform.SetScale(1.2114f, 1.2114f, 1.2114f);
            viewOnMap.name = "View On Map";
            GameObject viewOnMapText = viewOnMap.gameObject.GetChildren()[0];
            viewOnMapText.GetComponent<TMP_Text>().text = "View On Map";
            viewOnMapText.TryDestroyComponent<LocalizeText>();
            Button viewOnMapButton = viewOnMap.GetComponent<Button>();
            viewOnMapButton.onClick.RemoveAllListeners();
            viewOnMapButton.onClick.AddListener(new System.Action(() =>
            {
                if (G.ui.FleetWindow.selectedElements.Count == 1)
                {
                    G.ui.FleetWindow.selectedElements[0].AreaButton.onClick.Invoke();
                    G.ui.FleetWindow.selectedElements[0].AreaButton.onClick.Invoke();
                }
            }));

            UiM.ModifyUi(fleetButtons.GetChild("Mothballed")).SetActive(false, false);

            UiM.ModifyUi(fleetButtons.GetChild("View")).SetActive(false, false);

            UiM.ModifyUi(fleetButtons).SetChildOrder("AddCrewToggle", "Set Role", "View On Map", "ChangePort", "Set Crew", "Suspend", "Scrap", "Cancel Sale");




            // Fleet Ships

            UiM.ModifyUi(G.ui.FleetWindow.Root.GetChild("Root").GetChild("Fleet Ships")).MultiplyOffsets(new Vector2(-1585f / -1271.83f, -700f / -601.6475f), Vector2.one);




            // Fleet Template

            var templateGroup = fleetTemplate.GetComponent<LayoutGroup>();
            templateGroup.padding.left = 10;
            templateGroup.padding.right = 10;

            fleetTemplate.gameObject.GetChild("Crew").name = "CrewAction";

            GameObject roleText = GameObject.Instantiate(fleetTemplate.RoleSelectionButton.gameObject.GetChildren()[0]);
            roleText.SetParent(fleetTemplate.RoleSelectionButton.gameObject.GetParent());
            roleText.transform.SetScale(1, 1, 1);
            roleText.transform.localPosition = new Vector3();
            roleText.name = "RoleText";
            roleText.GetComponent<TMP_Text>().text = "ERROR";
            roleText.GetComponent<TMP_Text>().fontSize = 11;
            roleText.GetComponent<TMP_Text>().fontSizeMin = 11;
            roleText.GetComponent<TMP_Text>().fontSizeMax = 11;

            // fleetTemplate.Area.GetComponent<TMP_Text>().fontSizeMax = 11;

            UiM.ModifyUi(fleetTemplate.gameObject)
                .SetChildOrder(
                "Selected", "Type", "Name", "NameInputField", "Class", "Damage", "Ammo", "Fuel", "Role",
                "Status", "Area", "Port", "Port Selection", "Cost", "Crew", "Tonnage", "Date", "Speed", "Weapons", "Sold"
            ); // , "CrewAction"

            UiM.ModifyUi(fleetTemplate.Type.gameObject).MultiplyLayoutDimensions(60f / 40f, 1.0f);

            UiM.ModifyUi(fleetTemplate.Name.gameObject).MultiplyLayoutDimensions(275f / 200f, 1.0f);

            UiM.ModifyUi(fleetTemplate.NameInputField.gameObject).MultiplyLayoutDimensions(275f / 160f, 1.0f);

            fleetTemplate.NameInputField.gameObject.GetComponent<TMP_InputField>().onValidateInput = null;

            // GameObject conNameInput = ModUtils.GetChildAtPath("Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/ShipName/EditName/Edit");

            // fleetTemplate.NameInputField.gameObject.name = "Area (old)";

            // GameObject go = TMP_DefaultControls.CreateInputField(GetStandardResources());

            // GameObject nameInputField = GameObject.Instantiate(conNameInput);
            // nameInputField.SetParent(fleetTemplate.gameObject);
            // nameInputField.transform.SetScale(1, 1, 1);
            // nameInputField.name = "NameInputFieldTest";
            // LayoutElement nameInputElement = nameInputField.GetOrAddComponent<LayoutElement>();
            // nameInputElement.preferredWidth = 225f;
            // nameInputElement.preferredHeight = 225f;
            // InputField field = nameInputField.GetComponent<InputField>();
            // field.text = "TESTING 123";
            // field.onEndEdit.RemoveAllListeners();
            // field.onValueChange.RemoveAllListeners();
            // field.onValueChanged.RemoveAllListeners();
            // 
            // var action = new System.Action<string>((string str) =>
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg($"Input {str}");
            // });
            // 
            // field.onEndEdit.AddListener(action);

            UiM.ModifyUi(fleetTemplate.Class.gameObject).MultiplyLayoutDimensions(275f / 110f, 1.0f);

            UiM.ModifyUi(fleetTemplate.Damage.gameObject).MultiplyLayoutDimensions(60f / 90f, 1.0f);

            UiM.ModifyUi(fleetTemplate.Ammo.gameObject).MultiplyLayoutDimensions(60f / 75f, 1.0f);

            UiM.ModifyUi(fleetTemplate.Fuel.gameObject).MultiplyLayoutDimensions(60f / 75f, 1.0f);

            GameObject hidenElements = new GameObject();
            hidenElements.SetParent(fleetTemplate.gameObject);
            hidenElements.name = "HidenElements";
            hidenElements.SetActive(false);

            UiM.ModifyUi(fleetTemplate.Speed.gameObject).SetActive(false, false);
            // fleetTemplate.Speed.gameObject.SetParent(hidenElements);

            UiM.ModifyUi(fleetTemplate.Weapons.gameObject).SetActive(false, false);
            // fleetTemplate.Weapons.gameObject.SetParent(hidenElements);

            UiM.ModifyUi(fleetTemplate.RoleSelectionButton.gameObject).SetActive(false, false);
            // fleetTemplate.RoleSelectionButton.gameObject.SetParent(hidenElements);

            UiM.ModifyUi(fleetTemplate.CrewAction.gameObject.GetParent()).SetActive(false, false);
            // fleetTemplate.CrewAction.gameObject.GetParent().SetParent(hidenElements);

            UiM.ModifyUi(fleetTemplate.Sold.gameObject).SetActive(false, false);
            // fleetTemplate.Sold.gameObject.SetParent(hidenElements);

            // UiM.ModifyUi(fleetTemplate.Area.gameObject).SetActive(false, false);

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
                .MultiplyOffsets(new Vector2(-1585f / -1271.83f, 1f), Vector2.one)
                .SetChildOrder("Type", "Name", "Class", "Damage", "Ammo", "Fuel", "Role", "Status", "Area", "Port", "Cost", "Crew", "Tonnage", "Date", "Speed", "Weapons", "CrewAction", "Sold");

            UiM.ModifyUi(fleetHeader.GetChild("Damage")).MultiplyLayoutDimensions(60f / 85f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Ammo")).MultiplyLayoutDimensions(60f / 75f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Fuel")).MultiplyLayoutDimensions(60f / 70f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Name")).MultiplyLayoutDimensions(275f / 160f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Status")).MultiplyLayoutDimensions(100f / 110f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Port")).MultiplyLayoutDimensions(110f / 90f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Class")).MultiplyLayoutDimensions(275f / 100f, 1.0f);

            UiM.ModifyUi(fleetHeader.GetChild("Speed")).SetActive(false, false);

            UiM.ModifyUi(fleetHeader.GetChild("Weapons")).SetActive(false, false);

            UiM.ModifyUi(fleetHeader.GetChild("CrewAction")).SetActive(false, false);

            UiM.ModifyUi(fleetHeader.GetChild("Sold")).SetActive(false, false);
        }

        private static void ApplySettingsMenuModifications()
        {
            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/Sound/Viewport/Content/General Volume

            // Global/Ui/UiMain/Popup/Options Window/Root/RightSide/Graphic Options/Viewport/Content

            GameObject SettingsRoot = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Options Window/Root");

            GameObject GraphicsOptionsContent = ModUtils.GetChildAtPath("RightSide/Graphic Options/Viewport/Content", SettingsRoot);

            ModifyUi(GraphicsOptionsContent).SetChildOrder("Quality", "Resolution", "UI Scale", "Fullscreen Mode", "VSync", "Post Effects", "Shadow Details", "Anti Aliasing", "FXAA", "Anisotropic", "FPS", "Textures");

            GameObject uiScaleSlider = GameObject.Instantiate(ModUtils.GetChildAtPath("RightSide/Sound/Viewport/Content/General Volume", SettingsRoot));
            uiScaleSlider.transform.localPosition = new Vector3();
            uiScaleSlider.transform.SetParent(GraphicsOptionsContent.GetComponent<LayoutGroup>().transform);
            uiScaleSlider.transform.SetScale(1, 1, 1);
            uiScaleSlider.name = "UI Scale";
            GameObject uiScaleSliderText = uiScaleSlider.GetChild("Label");
            uiScaleSliderText.GetComponent<TMP_Text>().text = "UI Scale (BETA)";
            uiScaleSliderText.TryDestroyComponent<LocalizeText>();
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
                TAF_Settings.settings = Serializer.JSON.LoadJsonFile<TAF_Settings>(SavePath.path);

                if (TAF_Settings.settings.version != TAF_Settings.CurrentSettingsVersion)
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
            float peace_enemy_vp_ratio = MonoBehaviourExt.Param("peace_enemy_vp_ratio", 2f);
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
    }
}
