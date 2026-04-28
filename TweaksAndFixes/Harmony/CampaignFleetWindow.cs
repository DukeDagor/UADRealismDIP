using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine.UI;
using MelonLoader;
using System.Reflection;
using UnityEngine.EventSystems;

namespace TweaksAndFixes.Harmony
{
    // FleetSortBy
    [HarmonyPatch(typeof(CampaignFleetWindow))]
    internal class Patch_CampaignFleetWindow
    {
        private static HashSet<GameObject> OnClickVisited = new();
        private static HashSet<GameObject> ForeignDesignClickVisited = new();
        private static Player? _DesignViewerPlayer = null;
        private static GameObject? _DesignViewerButton = null;
        private static GameObject? _DesignViewerToolbar = null;
        private static readonly Dictionary<Player, GameObject> _DesignViewerFlagButtons = new();
        private static readonly Dictionary<GameObject, Image> _DesignViewerFlagImages = new();
        private static string _DesignViewerToolbarSignature = string.Empty;
        private static readonly HashSet<GameObject> _DesignShipCountHeaderTooltips = new();
        private static Vector2? _DesignShipsOffsetMinOriginal = null;
        private static Vector2? _DesignShipsOffsetMaxOriginal = null;
        private static Vector2? _DesignHeaderOffsetMinOriginal = null;
        private static Vector2? _DesignHeaderOffsetMaxOriginal = null;
        private static bool _RefreshingDesignViewerList = false;
        private static bool _SuppressSortedPlayerDesignRefresh = false;
        private const float DesignViewerToolbarStripHeight = 24f;
        private const float DesignViewerToolbarTopGapMargin = 4f;
        private const float DesignViewerToolbarFallbackTopOffset = 42f;
        private const float DesignViewerContentTopGap = 32f;
        private static readonly MethodInfo? _RefreshAllShipsUi = AccessTools.Method(typeof(CampaignFleetWindow), "RefreshAllShipsUi");
        private static readonly MethodInfo? _SetDesignImageAndInfoForFirstShip = AccessTools.Method(typeof(CampaignFleetWindow), "SetDesignImageAndInfoForFirstShip");
        private static readonly MethodInfo? _SetShipInfoAndImage = AccessTools.Method(typeof(CampaignFleetWindow), "SetShipInfoAndImage");

        private static bool IsViewingForeignDesigns => _DesignViewerPlayer != null && _DesignViewerPlayer != ExtraGameData.MainPlayer();

        private struct DesignShipCounts
        {
            public int Active;
            public int Building;
            public int Other;

            public int Total => Active + Building + Other;
        }

        private static bool IsDesignTabActive(CampaignFleetWindow window)
        {
            if (window == null)
                return false;

            return window.DesignScroll != null && window.DesignScroll.activeInHierarchy;
        }

        private static bool HasDesignTab(CampaignFleetWindow window)
        {
            return window?.DesignScroll != null;
        }

        private static void HideDesignViewerButton()
        {
            if (_DesignViewerButton != null)
                _DesignViewerButton.SetActive(false);
            if (_DesignViewerToolbar != null)
                _DesignViewerToolbar.SetActive(false);
            RestoreDesignViewerContentLayout(G.ui?.FleetWindow);
        }

        private static List<Player> GetDesignViewerPlayers()
        {
            List<Player> players = new();
            Player mainPlayer = ExtraGameData.MainPlayer();
            if (mainPlayer != null)
                players.Add(mainPlayer);

            var campaign = CampaignController.Instance;
            if (campaign?.CampaignData?.PlayersMajor == null)
                return players;

            foreach (Player player in campaign.CampaignData.PlayersMajor)
            {
                if (player == null || player == mainPlayer || !player.isAi)
                    continue;

                players.Add(player);
            }

            players.Sort((a, b) =>
            {
                if (a == mainPlayer) return -1;
                if (b == mainPlayer) return 1;
                return string.Compare(a.Name(false), b.Name(false), StringComparison.Ordinal);
            });

            return players;
        }

        private static Player GetCurrentDesignViewerPlayer()
        {
            var mainPlayer = ExtraGameData.MainPlayer();
            var players = GetDesignViewerPlayers();

            if (_DesignViewerPlayer == null || !players.Contains(_DesignViewerPlayer))
                _DesignViewerPlayer = mainPlayer ?? players.FirstOrDefault();

            return _DesignViewerPlayer;
        }

        private static void CycleDesignViewerPlayer(CampaignFleetWindow window, int direction)
        {
            var players = GetDesignViewerPlayers();
            if (players.Count == 0)
                return;

            var current = GetCurrentDesignViewerPlayer();
            int idx = players.IndexOf(current);
            if (idx < 0)
                idx = 0;

            idx = (idx + direction) % players.Count;
            if (idx < 0)
                idx += players.Count;

            SetDesignViewerPlayer(window, players[idx]);
        }

        private static void EnsureDesignViewerButton(CampaignFleetWindow window)
        {
            if (!HasDesignTab(window))
            {
                HideDesignViewerButton();
                return;
            }

            if (_DesignViewerButton != null)
                _DesignViewerButton.SetActive(false);

            if (_DesignViewerToolbar == null)
            {
                _DesignViewerToolbar = new GameObject("TAF_DesignViewerToolbar");
                _DesignViewerToolbar.AddComponent<RectTransform>();
                HorizontalLayoutGroup layout = _DesignViewerToolbar.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 3f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.MiddleCenter;

                LayoutElement toolbarLayout = _DesignViewerToolbar.AddComponent<LayoutElement>();
                toolbarLayout.minHeight = 24f;
                toolbarLayout.preferredHeight = 24f;
            }

            CanvasGroup canvasGroup = _DesignViewerToolbar.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = _DesignViewerToolbar.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            RestoreDesignViewerContentLayout(window);
            MoveDesignViewerToolbarToTop(window);
            ApplyDesignViewerContentGap(window);

            _DesignViewerToolbar.SetActive(true);
            RebuildDesignViewerToolbarIfNeeded();
            UpdateDesignViewerFlagSizes(window);
            UpdateDesignViewerToolbar(window);
        }

        private static void MoveDesignViewerToolbarToTop(CampaignFleetWindow window)
        {
            if (_DesignViewerToolbar == null)
                return;

            GameObject parent = window?.Root?.GetChild("Root") ?? window?.Root;
            if (parent == null)
                return;

            if (_DesignViewerToolbar.transform.parent != parent.transform)
                _DesignViewerToolbar.transform.SetParent(parent.transform, false);

            RectTransform rect = _DesignViewerToolbar.GetComponent<RectTransform>();
            if (rect == null)
                rect = _DesignViewerToolbar.AddComponent<RectTransform>();

            RectTransform parentRect = parent.GetComponent<RectTransform>();
            RectTransform designRect = GetDesignShipsRect(window);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            float width = 900f;
            if (parentRect != null && parentRect.rect.width > 100f)
                width = Mathf.Min(width, parentRect.rect.width - 80f);
            if (designRect != null && designRect.rect.width > 100f)
                width = Mathf.Min(width, designRect.rect.width - 8f);
            width = Mathf.Max(320f, width);
            rect.sizeDelta = new Vector2(width, DesignViewerToolbarStripHeight);

            float yFromTop = -DesignViewerToolbarFallbackTopOffset;
            if (parentRect != null && designRect != null)
            {
                Vector3[] parentCorners = new Vector3[4];
                Vector3[] designCorners = new Vector3[4];
                parentRect.GetWorldCorners(parentCorners);
                designRect.GetWorldCorners(designCorners);

                float parentTop = parentRect.InverseTransformPoint(parentCorners[1]).y;
                float designTop = parentRect.InverseTransformPoint(designCorners[1]).y;
                float gap = parentTop - designTop;
                if (gap > DesignViewerToolbarStripHeight + DesignViewerToolbarTopGapMargin * 2f)
                {
                    float centerFromTop = gap * 0.5f;
                    float minCenterFromTop = DesignViewerToolbarTopGapMargin + DesignViewerToolbarStripHeight * 0.5f;
                    float maxCenterFromTop = gap - minCenterFromTop;
                    yFromTop = -Mathf.Clamp(centerFromTop, minCenterFromTop, maxCenterFromTop);
                }
            }

            rect.anchoredPosition = new Vector2(0f, yFromTop);
            _DesignViewerToolbar.transform.SetAsLastSibling();
        }

        private static RectTransform GetDesignShipsRect(CampaignFleetWindow window)
        {
            GameObject designShips = window?.Root?.GetChild("Root")?.GetChild("Design Ships");
            return designShips != null ? designShips.GetComponent<RectTransform>() : null;
        }

        private static void ApplyDesignViewerContentGap(CampaignFleetWindow window)
        {
            RectTransform designRect = GetDesignShipsRect(window);
            if (designRect != null)
            {
                _DesignShipsOffsetMinOriginal ??= designRect.offsetMin;
                _DesignShipsOffsetMaxOriginal ??= designRect.offsetMax;
                Vector2 originalMax = _DesignShipsOffsetMaxOriginal.Value;
                designRect.offsetMax = new Vector2(originalMax.x, originalMax.y - DesignViewerContentTopGap);
            }

            RectTransform headerRect = window?.DesignHeader != null ? window.DesignHeader.GetComponent<RectTransform>() : null;
            if (headerRect != null)
            {
                _DesignHeaderOffsetMinOriginal ??= headerRect.offsetMin;
                _DesignHeaderOffsetMaxOriginal ??= headerRect.offsetMax;
                Vector2 originalMin = _DesignHeaderOffsetMinOriginal.Value;
                Vector2 originalMax = _DesignHeaderOffsetMaxOriginal.Value;
                headerRect.offsetMin = new Vector2(originalMin.x, originalMin.y - DesignViewerContentTopGap);
                headerRect.offsetMax = new Vector2(originalMax.x, originalMax.y - DesignViewerContentTopGap);
            }
        }

        private static void RestoreDesignViewerContentLayout(CampaignFleetWindow window)
        {
            RectTransform designRect = GetDesignShipsRect(window);
            if (designRect != null)
            {
                _DesignShipsOffsetMinOriginal ??= designRect.offsetMin;
                _DesignShipsOffsetMaxOriginal ??= designRect.offsetMax;
                designRect.offsetMin = _DesignShipsOffsetMinOriginal.Value;
                designRect.offsetMax = _DesignShipsOffsetMaxOriginal.Value;
            }

            RectTransform headerRect = window?.DesignHeader != null ? window.DesignHeader.GetComponent<RectTransform>() : null;
            if (headerRect != null)
            {
                _DesignHeaderOffsetMinOriginal ??= headerRect.offsetMin;
                _DesignHeaderOffsetMaxOriginal ??= headerRect.offsetMax;
                headerRect.offsetMin = _DesignHeaderOffsetMinOriginal.Value;
                headerRect.offsetMax = _DesignHeaderOffsetMaxOriginal.Value;
            }
        }

        private static void RebuildDesignViewerToolbarIfNeeded()
        {
            if (_DesignViewerToolbar == null)
                return;

            List<Player> players = GetDesignViewerPlayers();
            string signature = string.Join("|", players.Select(p => p?.data?.name ?? p?.Name(false) ?? "?"));
            bool hasMissingButton = _DesignViewerFlagButtons.Values.Any(button => button == null);
            if (_DesignViewerToolbarSignature == signature && _DesignViewerFlagButtons.Count == players.Count && !hasMissingButton)
                return;

            foreach (GameObject child in _DesignViewerToolbar.GetChildren())
            {
                if (child != null)
                    UnityEngine.Object.Destroy(child);
            }
            _DesignViewerFlagButtons.Clear();
            _DesignViewerFlagImages.Clear();

            foreach (Player player in players)
            {
                if (player == null)
                    continue;

                GameObject flagButton = new("TAF_DesignViewerFlag");
                flagButton.AddComponent<RectTransform>();
                flagButton.transform.SetParent(_DesignViewerToolbar.transform, false);
                flagButton.name = $"TAF_DesignViewerFlag_{player.data?.name ?? player.Name(false)}";
                flagButton.SetActive(true);

                LayoutElement layout = flagButton.GetComponent<LayoutElement>();
                if (layout == null)
                    layout = flagButton.AddComponent<LayoutElement>();
                layout.minWidth = 32f;
                layout.preferredWidth = 32f;
                layout.minHeight = 20f;
                layout.preferredHeight = 20f;

                RectTransform buttonRect = flagButton.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(32f, 20f);

                Image background = flagButton.AddComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0.18f);

                Button button = flagButton.AddComponent<Button>();
                button.targetGraphic = background;
                Player capturedPlayer = player;
                button.onClick.AddListener(new System.Action(() => SetDesignViewerPlayer(capturedPlayer)));

                GameObject flagImageObject = new("TAF_FlagImage");
                flagImageObject.AddComponent<RectTransform>();
                flagImageObject.transform.SetParent(flagButton.transform, false);
                RectTransform rect = flagImageObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image flagImage = flagImageObject.AddComponent<Image>();
                flagImage.sprite = player.Flag(true) ?? player.Flag(false);
                flagImage.preserveAspect = true;
                flagImage.raycastTarget = false;
                flagImage.color = Color.white;

                OnEnter onEnter = flagButton.AddComponent<OnEnter>();
                Player tooltipPlayer = player;
                onEnter.action = new System.Action(() =>
                {
                    if (!flagButton.active || (button != null && !button.interactable))
                    {
                        G.ui.HideTooltip();
                        return;
                    }

                    G.ui.ShowTooltip(BuildDesignViewerTooltip(tooltipPlayer), flagButton);
                });

                OnLeave onLeave = flagButton.AddComponent<OnLeave>();
                onLeave.action = new System.Action(() => G.ui.HideTooltip());

                _DesignViewerFlagButtons[player] = flagButton;
                _DesignViewerFlagImages[flagButton] = flagImage;
            }

            LayoutElement toolbarLayout = _DesignViewerToolbar.GetComponent<LayoutElement>();
            if (toolbarLayout != null)
                toolbarLayout.preferredWidth = -1f;

            RectTransform toolbarRect = _DesignViewerToolbar.GetComponent<RectTransform>();
            if (toolbarRect != null)
                toolbarRect.sizeDelta = new Vector2(toolbarRect.sizeDelta.x, 24f);

            _DesignViewerToolbarSignature = signature;
        }

        private static void UpdateDesignViewerFlagSizes(CampaignFleetWindow window)
        {
            int count = _DesignViewerFlagButtons.Count;
            if (count == 0)
                return;

            float availableWidth = 900f;
            RectTransform toolbarRect = _DesignViewerToolbar != null ? _DesignViewerToolbar.GetComponent<RectTransform>() : null;
            if (toolbarRect != null && toolbarRect.rect.width > 10f)
                availableWidth = toolbarRect.rect.width;
            else
            {
                RectTransform designRect = GetDesignShipsRect(window);
                if (designRect != null && designRect.rect.width > 10f)
                    availableWidth = designRect.rect.width - 8f;
            }

            float spacing = 3f;
            float flagWidth = Mathf.Clamp((availableWidth - spacing * Math.Max(0, count - 1)) / count, 28f, 44f);
            float flagHeight = Mathf.Clamp(flagWidth * 0.62f, 17f, 24f);

            foreach (GameObject flagButton in _DesignViewerFlagButtons.Values)
            {
                if (flagButton == null)
                    continue;

                LayoutElement layout = flagButton.GetComponent<LayoutElement>() ?? flagButton.AddComponent<LayoutElement>();
                layout.minWidth = flagWidth;
                layout.preferredWidth = flagWidth;
                layout.minHeight = flagHeight;
                layout.preferredHeight = flagHeight;

                RectTransform buttonRect = flagButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                    buttonRect.sizeDelta = new Vector2(flagWidth, flagHeight);
            }

            LayoutElement toolbarLayout = _DesignViewerToolbar?.GetComponent<LayoutElement>();
            if (toolbarLayout != null)
            {
                toolbarLayout.minHeight = flagHeight;
                toolbarLayout.preferredHeight = flagHeight;
            }
        }

        private static CampaignFleetWindow GetLiveFleetWindow(CampaignFleetWindow fallback = null)
        {
            return G.ui?.FleetWindow ?? fallback;
        }

        private static void SetDesignViewerPlayer(Player player)
        {
            SetDesignViewerPlayer(GetLiveFleetWindow(), player);
        }

        private static void SetDesignViewerPlayer(CampaignFleetWindow window, Player player)
        {
            window = GetLiveFleetWindow(window);
            if (window == null || player == null || !HasDesignTab(window))
                return;

            _DesignViewerPlayer = player;
            UpdateDesignViewerToolbar(window);
            ClearCurrentDesignList(window);
            try
            {
                window.Refresh(true);
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"Design viewer country switch failed; restoring player designs. {ex.GetType().Name}: {ex.Message}");
                _DesignViewerPlayer = ExtraGameData.MainPlayer();
                HideDesignViewerButton();
            }
        }

        private static void StripClonedButtonEvents(GameObject buttonObject)
        {
            if (buttonObject == null)
                return;

            foreach (EventTrigger trigger in buttonObject.GetComponentsInChildren<EventTrigger>(true))
            {
                UnityEngine.Object.Destroy(trigger);
            }
            foreach (OnEnter onEnter in buttonObject.GetComponentsInChildren<OnEnter>(true))
            {
                UnityEngine.Object.Destroy(onEnter);
            }
            foreach (OnLeave onLeave in buttonObject.GetComponentsInChildren<OnLeave>(true))
            {
                UnityEngine.Object.Destroy(onLeave);
            }
            foreach (OnClickH onClick in buttonObject.GetComponentsInChildren<OnClickH>(true))
            {
                UnityEngine.Object.Destroy(onClick);
            }
        }

        private static void UpdateDesignViewerButtonText()
        {
            if (_DesignViewerButton == null)
                return;

            Player player = GetCurrentDesignViewerPlayer();
            string name = player == null ? "Designs: ?" : $"Designs: {player.Name(false)}";

            TMP_Text text = _DesignViewerButton.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = name;
        }

        private static void UpdateDesignViewerToolbar(CampaignFleetWindow window)
        {
            if (_DesignViewerToolbar == null)
                return;

            Player current = GetCurrentDesignViewerPlayer();
            foreach (var kvp in _DesignViewerFlagButtons)
            {
                GameObject flagButton = kvp.Value;
                if (flagButton == null)
                    continue;

                bool selected = kvp.Key == current;
                Image background = flagButton.GetComponent<Image>();
                if (background != null)
                    background.color = selected ? new Color(1f, 0.86f, 0.28f, 1f) : new Color(0.72f, 0.72f, 0.72f, 0.78f);

                if (_DesignViewerFlagImages.TryGetValue(flagButton, out Image flagImage) && flagImage != null)
                    flagImage.color = selected ? Color.white : new Color(0.74f, 0.74f, 0.74f, 0.92f);

                Outline outline = flagButton.GetComponent<Outline>();
                if (outline == null)
                    outline = flagButton.AddComponent<Outline>();
                outline.enabled = selected;
                outline.effectColor = new Color(1f, 0.72f, 0.08f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        private static string BuildDesignViewerTooltip(Player player)
        {
            if (player == null)
                return "Designs";

            var designs = GetViewedDesigns(player);
            Dictionary<string, int> designsByClass = new();
            foreach (Ship design in designs)
            {
                string cls = ShipClassLabel(design);
                designsByClass[cls] = designsByClass.TryGetValue(cls, out int count) ? count + 1 : 1;
            }

            Dictionary<string, DesignShipCounts> shipsByClass = new();
            DesignShipCounts total = new();
            foreach (Ship ship in player.GetFleetAll())
            {
                if (ship == null || ship.isDesign)
                    continue;

                string cls = ShipClassLabel(ship);
                DesignShipCounts counts = shipsByClass.TryGetValue(cls, out DesignShipCounts existing) ? existing : new DesignShipCounts();
                AddShipStateToCounts(ship, ref counts);
                AddShipStateToCounts(ship, ref total);
                shipsByClass[cls] = counts;
            }

            List<string> lines = new()
            {
                player.Name(false),
                $"Designs: {designs.Count}",
                $"Ships: {total.Total} ({total.Active}/{total.Building}/{total.Other})"
            };

            if (designsByClass.Count > 0)
                lines.Add($"Designs by class: {FormatSimpleClassCounts(designsByClass)}");
            if (shipsByClass.Count > 0)
                lines.Add($"Ships by class: {FormatShipClassCounts(shipsByClass)}");

            return string.Join("\n", lines);
        }

        private static DesignShipCounts GetDesignShipCounts(Player player, Ship design)
        {
            DesignShipCounts counts = new();
            if (player == null || design == null)
                return counts;

            foreach (Ship ship in player.GetFleetAll())
            {
                if (ship == null || ship.design != design)
                    continue;

                AddShipStateToCounts(ship, ref counts);
            }

            return counts;
        }

        private static void SetDesignShipCountText(FleetWindow_ShipElementUI ui, Player player, Ship design)
        {
            if (ui?.ShipCount == null || player == null || design == null)
                return;

            DesignShipCounts counts = GetDesignShipCounts(player, design);
            ui.ShipCount.text = $"{counts.Active}/{counts.Building}/{counts.Other}";
        }

        private static void AddShipStateToCounts(Ship ship, ref DesignShipCounts counts)
        {
            if (ship.isBuilding || ship.isCommissioning)
                counts.Building++;
            else if (ship.isAlive && !ship.isRefit && !ship.isRepairing)
                counts.Active++;
            else
                counts.Other++;
        }

        private static string ShipClassLabel(Ship ship)
        {
            return ship?.shipType?.name?.ToUpperInvariant() ?? "?";
        }

        private static string FormatSimpleClassCounts(Dictionary<string, int> counts)
        {
            return string.Join(", ", counts.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key} {kvp.Value}"));
        }

        private static string FormatShipClassCounts(Dictionary<string, DesignShipCounts> counts)
        {
            return string.Join(", ", counts.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key} {kvp.Value.Active}/{kvp.Value.Building}/{kvp.Value.Other}"));
        }

        private static void EnsureDesignShipCountHeaderTooltip(CampaignFleetWindow window)
        {
            GameObject header = window?.DesignHeader;
            if (header == null)
                return;

            GameObject target = null;
            foreach (GameObject child in header.GetChildren())
            {
                if (child == null)
                    continue;

                string childName = child.name ?? string.Empty;
                TMP_Text childText = child.GetComponent<TMP_Text>() ?? child.GetComponentInChildren<TMP_Text>();
                string text = childText?.text ?? string.Empty;
                if (childName.Contains("ShipCount", StringComparison.OrdinalIgnoreCase) ||
                    childName.Contains("Count", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Ship Count", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Count", StringComparison.OrdinalIgnoreCase))
                {
                    target = child;
                    break;
                }
            }

            if (target == null || _DesignShipCountHeaderTooltips.Contains(target))
                return;

            _DesignShipCountHeaderTooltips.Add(target);
            AddRawTooltip(target, "Shown as active/building/other.\nActive: afloat and available.\nBuilding: under construction or commissioning.\nOther: refit, repair, mothball, or otherwise unavailable.");
        }

        private static void AddRawTooltip(GameObject ui, string content)
        {
            if (ui == null)
                return;

            OnEnter onEnter = ui.AddComponent<OnEnter>();
            onEnter.action = new System.Action(() => G.ui.ShowTooltip(content, ui));

            OnLeave onLeave = ui.AddComponent<OnLeave>();
            onLeave.action = new System.Action(() => G.ui.HideTooltip());
        }

        private static Il2CppSystem.Collections.Generic.List<Ship> GetViewedDesigns(Player player)
        {
            Il2CppSystem.Collections.Generic.List<Ship> designs = new();
            if (player == null)
                return designs;

            List<Ship> sortedDesigns = new();
            void AddDesignCandidate(Ship ship, bool requireShips)
            {
                if (ship == null || (!ship.isDesign && !ship.isRefitDesign) || sortedDesigns.Contains(ship))
                    return;

                if (requireShips && GetDesignShipCounts(player, ship).Total == 0)
                    return;

                sortedDesigns.Add(ship);
            }

            var sourceDesigns = new Il2CppSystem.Collections.Generic.List<Ship>(player.designs);
            foreach (Ship ship in sourceDesigns)
                AddDesignCandidate(ship, false);

            foreach (Ship ship in player.GetFleetAll())
            {
                if (ship == null || ship.isDesign)
                    continue;

                AddDesignCandidate(ship.design, true);
            }

            sortedDesigns.Sort(CompareDesignsByDefaultClassOrder);
            foreach (Ship ship in sortedDesigns)
                designs.Add(ship);

            return designs;
        }

        private static int CompareDesignsByDefaultClassOrder(Ship a, Ship b)
        {
            int order = ShipTypeSortRank(a).CompareTo(ShipTypeSortRank(b));
            if (order != 0)
                return order;

            int year = ShipDesignYear(a).CompareTo(ShipDesignYear(b));
            if (year != 0)
                return year;

            return string.Compare(a?.Name(false, false, false, false, true), b?.Name(false, false, false, false, true), StringComparison.Ordinal);
        }

        private static int ShipTypeSortRank(Ship ship)
        {
            return (ship?.shipType?.name ?? string.Empty).ToLowerInvariant() switch
            {
                "bb" => 0,
                "bc" => 1,
                "ca" => 2,
                "cl" => 3,
                "dd" => 4,
                "tb" => 5,
                "ss" => 6,
                "tr" => 7,
                _ => 100
            };
        }

        private static int ShipDesignYear(Ship ship)
        {
            if (ship == null)
                return 0;

            return (ship.isRefitDesign ? ship.dateCreatedRefit : ship.dateCreated).AsDate().Year;
        }

        private static void SetForeignDesignButtonsInteractable(CampaignFleetWindow window, bool interactable)
        {
            if (window?.DesignButtonsRoot == null)
                return;

            foreach (GameObject child in window.DesignButtonsRoot.GetChildren())
            {
                if (child == _DesignViewerButton || child == _DesignViewerToolbar || child.name.StartsWith("TAF_DesignViewer"))
                    continue;

                Button button = child.GetComponent<Button>();
                if (button != null)
                    button.interactable = interactable;
            }

            SetDesignActionButtonsInteractable(window, interactable);
        }

        private static void SetDesignActionButtonsInteractable(CampaignFleetWindow window, bool interactable)
        {
            if (window == null)
                return;

            if (window.DesignView != null) window.DesignView.interactable = interactable;
            if (window.Delete != null) window.Delete.interactable = interactable;
            if (window.NewDesign != null) window.NewDesign.interactable = interactable;
            if (window.BuildShip != null) window.BuildShip.interactable = interactable;
            if (window.DesignRefit != null) window.DesignRefit.interactable = interactable;
            if (window.CancelSale != null) window.CancelSale.interactable = interactable;
        }

        private static void SetChildButtonInteractable(GameObject root, string childName, bool interactable)
        {
            GameObject child = root?.GetChild(childName);
            Button button = child != null ? child.GetComponent<Button>() : null;
            if (button != null)
                button.interactable = interactable;
        }

        private static void UpdateDesignSelectionActions(CampaignFleetWindow window, Player player, Ship ship, bool allowActions)
        {
            if (window == null || ship == null)
                return;

            if (!allowActions)
            {
                SetForeignDesignButtonsInteractable(window, false);
                return;
            }

            SetDesignActionButtonsInteractable(window, true);

            DesignShipCounts counts = GetDesignShipCounts(player, ship);
            if (window.Delete != null)
                window.Delete.interactable = counts.Total == 0;

            if (window.BuildShip != null)
                window.BuildShip.interactable = ship.isDesign || ship.isRefitDesign;
        }

        private static void ClearCurrentDesignList(CampaignFleetWindow window)
        {
            if (window == null)
                return;

            foreach (var element in window.designUiByShip)
            {
                if (element.Value != null)
                    element.Value.gameObject.SetActive(false);
            }

            window.designUiByShip.Clear();
            window.selectedElements.Clear();
            ForeignDesignClickVisited.Clear();
        }

        private static void RefreshViewedDesigns(CampaignFleetWindow window, bool allowActions)
        {
            Player player = GetCurrentDesignViewerPlayer();
            if (player == null || !IsDesignTabActive(window) || _RefreshAllShipsUi == null || _RefreshingDesignViewerList || _SuppressSortedPlayerDesignRefresh)
                return;

            try
            {
                _RefreshingDesignViewerList = true;
                var designs = GetViewedDesigns(player);
                ClearCurrentDesignList(window);
                _RefreshAllShipsUi.Invoke(window, new object[] { true, designs });
                _SetDesignImageAndInfoForFirstShip?.Invoke(window, new object[] { designs, null, true });

                AttachDesignSelectionHandlers(window, designs, allowActions);
                if (allowActions)
                    SetForeignDesignButtonsInteractable(window, true);
                else
                    SetForeignDesignButtonsInteractable(window, false);

                UpdateDesignViewerToolbar(window);
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"Design viewer sorted refresh failed; restoring vanilla player designs. {ex.GetType().Name}: {ex.Message}");
                if (allowActions)
                {
                    _SuppressSortedPlayerDesignRefresh = true;
                    try
                    {
                        window.Refresh(true);
                    }
                    finally
                    {
                        _SuppressSortedPlayerDesignRefresh = false;
                    }
                }
                else
                {
                    _DesignViewerPlayer = ExtraGameData.MainPlayer();
                    HideDesignViewerButton();
                }
            }
            finally
            {
                _RefreshingDesignViewerList = false;
            }
        }

        private static void AttachDesignSelectionHandlers(CampaignFleetWindow window, Il2CppSystem.Collections.Generic.List<Ship> designs, bool allowActions)
        {
            if (window == null)
                return;

            foreach (var element in window.designUiByShip)
            {
                Ship ship = element.Key;
                FleetWindow_ShipElementUI ui = element.Value;
                if (ship == null || ui?.Btn == null)
                    continue;

                System.Action selectAction = new(() => SelectViewedDesign(window, designs, ship, ui, allowActions));
                ui.Btn.onClick.AddListener(selectAction);

                Button rowButton = ui.gameObject.GetComponent<Button>();
                if (rowButton != null && rowButton != ui.Btn)
                    rowButton.onClick.AddListener(selectAction);

                foreach (Transform t in ui.gameObject.GetComponentsInChildren<Transform>(true))
                {
                    GameObject clickTarget = t.gameObject;
                    if (clickTarget == null || ForeignDesignClickVisited.Contains(clickTarget))
                        continue;

                    ForeignDesignClickVisited.Add(clickTarget);
                    OnClickH click = clickTarget.AddComponent<OnClickH>();
                    click.action = new System.Action<PointerEventData>(_ => SelectViewedDesign(window, designs, ship, ui, allowActions));
                }
            }
        }

        private static void SelectViewedDesign(CampaignFleetWindow window, Il2CppSystem.Collections.Generic.List<Ship> designs, Ship ship, FleetWindow_ShipElementUI ui, bool allowActions)
        {
            if (window == null || ship == null || ui == null)
                return;

            window.selectedElements.Clear();
            window.selectedElements.Add(ui);

            foreach (var element in window.designUiByShip)
            {
                if (element.Value?.Highlighted != null)
                    element.Value.Highlighted.gameObject.SetActive(element.Value == ui);
            }

            _SetShipInfoAndImage?.Invoke(window, new object[] { ship });
            _SetDesignImageAndInfoForFirstShip?.Invoke(window, new object[] { designs, ship, true });
            UpdateDesignSelectionActions(window, GetCurrentDesignViewerPlayer(), ship, allowActions);
        }

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
        [HarmonyPrefix]
        internal static void Prefix_Refresh(CampaignFleetWindow __instance, bool isDesign)
        {
            if (!isDesign || !HasDesignTab(__instance))
            {
                if (!isDesign)
                    _DesignViewerPlayer = ExtraGameData.MainPlayer();
                HideDesignViewerButton();
                return;
            }

            EnsureDesignViewerButton(__instance);
        }

        [HarmonyPatch(nameof(CampaignFleetWindow.Refresh))]
        [HarmonyPostfix]
        internal static void Postfix_Refresh(CampaignFleetWindow __instance, bool isDesign)
        {
            try
            {
            if (isDesign && HasDesignTab(__instance))
            {
                EnsureDesignViewerButton(__instance);
                EnsureDesignShipCountHeaderTooltip(__instance);
                RefreshViewedDesigns(__instance, !IsViewingForeignDesigns);
            }
            else
            {
                if (!isDesign)
                    _DesignViewerPlayer = ExtraGameData.MainPlayer();
                HideDesignViewerButton();
            }

            foreach (var element in __instance.designUiByShip)
            {
                FleetWindow_ShipElementUI ui = element.Value;
                Ship s = ui?.CurrentShip ?? element.Key;
                if (ui == null || s == null)
                    continue;

                Player designPlayer = GetCurrentDesignViewerPlayer();
                SetDesignShipCountText(ui, designPlayer, s);

                if (s.isRefitDesign)
                {
                    if (ui.Year != null)
                        ui.Year.text = $"{s.dateCreatedRefit.AsDate().Year}";
                }
                else
                {
                    if (ui.Year != null)
                        ui.Year.text = $"{s.dateCreated.AsDate().Year}";
                }

                if (__instance.selectedElements.Count > 0 && __instance.selectedElements[0] == ui)
                {
                    DesignShipCounts counts = GetDesignShipCounts(designPlayer, s);
                    if (__instance.Delete != null)
                        __instance.Delete.interactable = !IsViewingForeignDesigns && counts.Total == 0;
                    if (__instance.BuildShip != null && !IsViewingForeignDesigns)
                        __instance.BuildShip.interactable = s.isDesign || s.isRefitDesign;
                }
            }

            if (isDesign && IsViewingForeignDesigns)
                SetForeignDesignButtonsInteractable(__instance, false);

            if (__instance.selectedElements.Count == 0 && __instance.FleetButtonsRoot != null)
            {
                SetChildButtonInteractable(__instance.FleetButtonsRoot, "Set Crew", false);
                SetChildButtonInteractable(__instance.FleetButtonsRoot, "Set Role", false);
                SetChildButtonInteractable(__instance.FleetButtonsRoot, "View On Map", false);
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
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"Fleet window refresh patch failed; leaving vanilla UI intact. {ex.GetType().Name}: {ex.Message}");
                try
                {
                    HideDesignViewerButton();
                }
                catch
                {
                }
            }
        }
    }
}
