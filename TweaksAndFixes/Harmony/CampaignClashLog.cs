using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS8603
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Ui))]
    internal static class Patch_CampaignClashLog
    {
        private const string ObjectName = "TAF_ClashLogReporter";
        private const string HeaderText = "Combat Log";
        private const string PlaceholderText = "Fleet clashes: none recorded";
        private const int MaxEntries = 8;
        private static readonly Dictionary<string, string> _TooltipsByText = new();
        private static readonly HashSet<string> _ReportedBattleIds = new();
        private static readonly HashSet<string> _ReportedLogLines = new();
        private static readonly List<ClashEntry> _Entries = new();
        private static CampaignLogReporterUI _Reporter;
        private static GameObject _Root;
        private static bool _LoggedCreateFailure;
        private static bool _HasRealEntries;
        private static bool _HasPlaceholder;
        private static string _LastTraceState = string.Empty;
        private static string _LastTracePosition = string.Empty;
        private static Vector2? _FixedAnchoredPosition;
        private static Vector2? _FixedSize;

        [HarmonyPatch(nameof(Ui.Update))]
        [HarmonyPostfix]
        internal static void Postfix_Update(Ui __instance)
        {
            Update(__instance);
        }

        [HarmonyPatch(nameof(Ui.ReportBattle))]
        [HarmonyPostfix]
        internal static void Postfix_ReportBattle(Ui __instance, CampaignBattle battle, bool refreshUi = true)
        {
            if (Config.Param("taf_campaign_clash_log_enabled", 1) != 1)
                return;

            if (!IsClashBattle(battle))
                return;

            if (!Ensure(__instance))
                return;

            AddBattleToLog(battle);
        }

        private static void Update(Ui ui)
        {
            if (Config.Param("taf_campaign_clash_log_enabled", 1) != 1)
            {
                SetVisible(false);
                return;
            }

            bool visible = GameManager.IsCampaign && GameManager.IsWorldMap && ui?.LogReporter != null;
            TraceState(visible ? "visible-gate-open" : $"hidden campaign={GameManager.IsCampaign} world={GameManager.IsWorldMap} reporter={ui?.LogReporter != null}");
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            if (Ensure(ui))
            {
                SetVisible(true);
                PositionAboveCampaignLog(ui);
                ConfigureCloneScrolling();
                SetHeaderText();
                SeedExistingBattles();
                if (_Entries.Count == 0)
                    MirrorExistingClashReports(ui);
                EnsurePlaceholder();
                AttachTooltips();
            }
        }

        private static bool Ensure(Ui ui)
        {
            if (_Reporter != null && _Root != null)
                return true;

            if (ui?.LogReporter == null || ui.LogReporter.gameObject == null)
                return false;

            try
            {
                GameObject original = ui.LogReporter.gameObject;
                _Root = GameObject.Instantiate(original);
                _Root.name = ObjectName;
                Transform parent = original.transform.parent;
                _Root.transform.SetParent(parent, false);
                _Root.transform.SetAsLastSibling();
                _Reporter = _Root.GetComponent<CampaignLogReporterUI>();
                _Reporter.Clear();
                ClearVisualReports();
                SetHeaderText();
                _TooltipsByText.Clear();
                _ReportedBattleIds.Clear();
                _ReportedLogLines.Clear();
                _Entries.Clear();
                _HasRealEntries = false;
                _HasPlaceholder = false;
                _FixedAnchoredPosition = null;
                _FixedSize = null;

                PositionAboveCampaignLog(ui);
                ConfigureCloneScrolling();
                SetVisible(true);
                TraceState("created");
                return true;
            }
            catch (Exception ex)
            {
                if (!_LoggedCreateFailure)
                {
                    _LoggedCreateFailure = true;
                    Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to create UI clone: {ex}");
                }

                _Root = null;
                _Reporter = null;
                return false;
            }
        }

        private static void PositionAboveCampaignLog(Ui ui)
        {
            if (_Root == null || ui?.LogReporter == null)
                return;

            RectTransform source = ui.LogReporter.rectTransform != null
                ? ui.LogReporter.rectTransform
                : ui.LogReporter.GetComponent<RectTransform>();
            RectTransform target = _Root.GetComponent<RectTransform>();
            if (source == null || target == null)
                return;

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.localScale = source.localScale;

            float sourceHeight = source.rect.height > 1f ? source.rect.height : Math.Abs(source.sizeDelta.y);
            float sourceWidth = source.rect.width > 1f ? source.rect.width : Math.Abs(source.sizeDelta.x);
            float visibleSourceHeight = Mathf.Clamp(sourceHeight, 90f, 180f);
            float targetHeight = Mathf.Clamp(Config.Param("taf_campaign_clash_log_height", 78f), 56f, 180f);
            float targetWidth = sourceWidth * Mathf.Clamp(Config.Param("taf_campaign_clash_log_width_mult", 1.2f), 0.8f, 1.6f);
            float verticalGap = Mathf.Clamp(Config.Param("taf_campaign_clash_log_vertical_gap", 24f), 0f, 80f);
            if (sourceWidth > 1f)
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

            if (_FixedAnchoredPosition.HasValue && _FixedSize.HasValue)
            {
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _FixedSize.Value.x);
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _FixedSize.Value.y);
                target.anchoredPosition = _FixedAnchoredPosition.Value;
            }
            else
            {
                target.position = source.position;
                target.anchoredPosition += new Vector2(0f, (visibleSourceHeight + targetHeight) * 0.5f + verticalGap);
                if (sourceHeight < 500f)
                {
                    _FixedAnchoredPosition = target.anchoredPosition;
                    _FixedSize = new Vector2(targetWidth, targetHeight);
                }
            }

            TracePosition($"position sourceAnchored={source.anchoredPosition} targetAnchored={target.anchoredPosition} "
                + $"sourceRect=({source.rect.width:0.0},{source.rect.height:0.0}) targetRect=({target.rect.width:0.0},{target.rect.height:0.0}) "
                + $"parent={target.transform.parent?.name ?? "none"}");
        }

        private static void ConfigureCloneScrolling()
        {
            if (_Root == null || _Reporter == null)
                return;

            try
            {
                foreach (ScrollRect scrollRect in _Root.GetComponentsInChildren<ScrollRect>(true))
                {
                    scrollRect.enabled = true;
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.verticalScrollbar = null;
                }

                foreach (Scrollbar scrollbar in _Root.GetComponentsInChildren<Scrollbar>(true))
                    scrollbar.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to configure cloned scrolling: {ex.Message}");
            }
        }

        private static void SetVisible(bool visible)
        {
            if (_Root != null && _Root.activeSelf != visible)
                _Root.SetActive(visible);
        }

        private static void TraceState(string state)
        {
            if (Config.Param("taf_campaign_clash_log_trace", 0) <= 0 || state == _LastTraceState)
                return;

            _LastTraceState = state;
            Melon<TweaksAndFixes>.Logger.Msg($"[CLASH-LOG] {state}");
        }

        private static void TracePosition(string state)
        {
            if (Config.Param("taf_campaign_clash_log_trace", 0) <= 0 || state == _LastTracePosition)
                return;

            _LastTracePosition = state;
            Melon<TweaksAndFixes>.Logger.Msg($"[CLASH-LOG] {state}");
        }

        internal static bool IsClashBattle(CampaignBattle battle)
        {
            if (battle == null || battle.Type == null)
                return false;

            if (battle.Type.CurrentType == BattleTypeEx.EBattleType.TaskForce || battle.IsTaskForceBattle())
                return true;

            try
            {
                string type = battle.Type.TypeToString();
                if (!string.IsNullOrEmpty(type)
                    && (type.IndexOf("clash", StringComparison.OrdinalIgnoreCase) >= 0
                        || type.IndexOf("task force", StringComparison.OrdinalIgnoreCase) >= 0
                        || type.Equals("Meeting", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            catch
            {
            }

            TraceState($"battle-skip type={SafeBattleTypeForTrace(battle)} state={battle.CurrentState}");
            return false;
        }

        private static void SeedExistingBattles()
        {
            if (_Reporter == null)
                return;

            try
            {
                var battles = CampaignController.Instance?.CampaignData?.Battles;
                if (battles == null)
                    return;

                List<CampaignBattle> sorted = new();
                foreach (CampaignBattle battle in battles)
                {
                    if (IsClashBattle(battle))
                        sorted.Add(battle);
                }

                sorted.Sort((a, b) => BattleSortKey(b).CompareTo(BattleSortKey(a)));
                foreach (CampaignBattle battle in sorted)
                    AddBattleToLog(battle, false);
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to seed existing battles: {ex.Message}");
            }
        }

        internal static void AddBattleToLog(CampaignBattle battle, bool newestFirst = true)
        {
            if (_Reporter == null || battle == null)
                return;

            string battleId = battle.Id.ToString();
            if (!string.IsNullOrEmpty(battleId))
                _ReportedBattleIds.Add(battleId);

            ClearPlaceholder();

            string line = BuildLine(battle);
            string tooltip = BuildTooltip(battle);
            AddLogLine(battle.Attacker, line, tooltip, $"battle:{battleId}", newestFirst);
        }

        private static void MirrorExistingClashReports(Ui ui)
        {
            if (_Reporter == null || ui?.LogReporter == null || ui.LogReporter.gameObject == null)
                return;

            try
            {
                foreach (CampaignLogReporterElement element in ui.LogReporter.gameObject.GetComponentsInChildren<CampaignLogReporterElement>(true))
                {
                    if (element == null || element.Text == null)
                        continue;

                    string line = element.Text.text;
                    if (!IsClashLogText(line))
                        continue;

                    string tooltip = $"Fleet clash report\n{StripRichText(line)}";
                    AddLogLine(ExtraGameData.MainPlayer(), line, tooltip, $"line:{line}", false);
                }
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to mirror vanilla reports: {ex.Message}");
            }
        }

        private static void AddLogLine(Player player, string line, string tooltip, string id, bool newestFirst)
        {
            if (_Reporter == null || string.IsNullOrWhiteSpace(line))
                return;

            ClearPlaceholder();
            _ReportedLogLines.Add(id);

            foreach (ClashEntry existing in _Entries)
            {
                if (existing.Id == id && existing.Line == line && existing.Tooltip == tooltip)
                    return;
            }

            _Entries.RemoveAll(x => x.Id == id);
            ClashEntry entry = new(id, player, line, tooltip);
            if (newestFirst)
                _Entries.Insert(0, entry);
            else
                _Entries.Add(entry);

            while (_Entries.Count > MaxEntries)
                _Entries.RemoveAt(_Entries.Count - 1);

            RenderEntries();
        }

        private static void RenderEntries()
        {
            if (_Reporter == null)
                return;

            try
            {
                _Reporter.Clear();
                ClearVisualReports();
                _TooltipsByText.Clear();

                foreach (ClashEntry entry in _Entries)
                {
                    _TooltipsByText[entry.Line] = entry.Tooltip;
                    _Reporter.Report(entry.Player, true, entry.Line, Array.Empty<Il2CppSystem.Object>());
                }

                _HasRealEntries = _Entries.Count > 0;
                _HasPlaceholder = false;
                TraceState($"entries-rendered count={_Entries.Count}");
                AttachTooltips();
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to report battle: {ex.Message}");
            }
        }

        private static bool IsClashLogText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string plain = StripRichText(text);
            return plain.IndexOf(" clashes with ", StringComparison.OrdinalIgnoreCase) >= 0
                || plain.IndexOf(" clash ", StringComparison.OrdinalIgnoreCase) >= 0
                || plain.IndexOf("clash of fleets", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            int guard = 0;
            while (guard++ < 40)
            {
                int start = text.IndexOf('<');
                if (start < 0)
                    break;

                int end = text.IndexOf('>', start);
                if (end < 0)
                    break;

                text = text.Remove(start, end - start + 1);
            }

            return text;
        }

        private static void EnsurePlaceholder()
        {
            if (_Reporter == null || _HasRealEntries || _HasPlaceholder)
                return;

            try
            {
                _TooltipsByText[PlaceholderText] = "No fleet clash reports are currently available.";
                _Reporter.Report(ExtraGameData.MainPlayer(), true, PlaceholderText, Array.Empty<Il2CppSystem.Object>());
                _HasPlaceholder = true;
                TraceState("placeholder-added");
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to add placeholder: {ex.Message}");
            }
        }

        private static void ClearPlaceholder()
        {
            if (_Reporter == null || !_HasPlaceholder || _HasRealEntries)
                return;

            _Reporter.Clear();
            ClearVisualReports();
            _TooltipsByText.Remove(PlaceholderText);
            _HasPlaceholder = false;
        }

        private static void ClearVisualReports()
        {
            if (_Root == null || _Reporter == null)
                return;

            try
            {
                foreach (CampaignLogReporterElement element in _Root.GetComponentsInChildren<CampaignLogReporterElement>(true))
                {
                    if (element == null || element.gameObject == null || element == _Reporter.Template)
                        continue;

                    UnityEngine.Object.Destroy(element.gameObject);
                }
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to clear cloned report rows: {ex.Message}");
            }
        }

        private static void SetHeaderText()
        {
            if (_Root == null)
                return;

            try
            {
                foreach (TMP_Text text in _Root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text == null || string.IsNullOrWhiteSpace(text.text))
                        continue;

                    if (text.text.Trim().Equals("Log", StringComparison.OrdinalIgnoreCase))
                    {
                        text.text = HeaderText;
                        TraceState("header-set");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to set header text: {ex.Message}");
            }
        }

        private static string BuildLine(CampaignBattle battle)
        {
            string attacker = PlayerName(battle.Attacker);
            string defender = PlayerName(battle.Defender);

            if (battle.Victor != null)
            {
                Player loser = SamePlayer(battle.Victor, battle.Attacker) ? battle.Defender : battle.Attacker;
                return $"{PlayerName(battle.Victor)} beat {PlayerName(loser)}{ShortLossText(battle, battle.Victor, loser)}";
            }

            if (battle.Avoider != null)
            {
                Player other = SamePlayer(battle.Avoider, battle.Attacker) ? battle.Defender : battle.Attacker;
                return $"{PlayerName(battle.Avoider)} avoided {PlayerName(other)}{ShortLossText(battle, battle.Avoider, other)}";
            }

            if (Math.Abs(battle.VictoryPointsAttacker - battle.VictoryPointsDefender) > 0.01f)
            {
                Player leader = battle.VictoryPointsAttacker > battle.VictoryPointsDefender ? battle.Attacker : battle.Defender;
                Player other = SamePlayer(leader, battle.Attacker) ? battle.Defender : battle.Attacker;
                return $"{PlayerName(leader)} led {PlayerName(other)}{ShortLossText(battle, leader, other)}";
            }

            string outcome = battle.CurrentState == CampaignBattleBase.State.Finished ? "draw" : "vs";
            return $"{attacker} {outcome} {defender}{ShortLossText(battle, battle.Attacker, battle.Defender)}";
        }

        private static string BuildTooltip(CampaignBattle battle)
        {
            string attacker = PlayerName(battle.Attacker);
            string defender = PlayerName(battle.Defender);
            string type = BattleTypeName(battle);
            string location = SafeLocation(battle);
            string date = SafeDate(battle);

            int attackerShips = CountShips(battle.AttackerShips);
            int defenderShips = CountShips(battle.DefenderShips);
            int attackerSunk = CountShips(battle.AttackerShipsSink);
            int defenderSunk = CountShips(battle.DefenderShipsSink);
            float attackerTonnage = SumTonnage(battle.AttackerShips);
            float defenderTonnage = SumTonnage(battle.DefenderShips);
            float attackerDamage = SumBattleDamage(battle, battle.AttackerShips);
            float defenderDamage = SumBattleDamage(battle, battle.DefenderShips);
            float attackerCrewLoss = SumCrewLosses(battle, battle.AttackerShips);
            float defenderCrewLoss = SumCrewLosses(battle, battle.DefenderShips);

            return $"{type}\n"
                + $"{date} - {location}\n"
                + $"{attacker}: {attackerShips} ships, {attackerTonnage:0} t, sunk {attackerSunk}, damage {attackerDamage:0.#}%, crew lost {attackerCrewLoss:0}\n"
                + $"{defender}: {defenderShips} ships, {defenderTonnage:0} t, sunk {defenderSunk}, damage {defenderDamage:0.#}%, crew lost {defenderCrewLoss:0}\n"
                + $"Result: {ResultText(battle)}";
        }

        private static string BattleTypeName(CampaignBattle battle)
        {
            try
            {
                string title = battle.Type.TypeToString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
            catch
            {
            }

            return "Fleet clash";
        }

        private static string SafeLocation(CampaignBattle battle)
        {
            try
            {
                string location = battle.GetLocationName();
                if (!string.IsNullOrWhiteSpace(location))
                    return location;
            }
            catch
            {
            }

            return "unknown waters";
        }

        private static string SafeDate(CampaignBattle battle)
        {
            try
            {
                var date = battle.Date.AsDate();
                return $"{ModUtils.NumToMonth(date.Month)} {date.Year}";
            }
            catch
            {
                return "Unknown date";
            }
        }

        private static int BattleSortKey(CampaignBattle battle)
        {
            if (battle == null)
                return 0;

            try
            {
                var date = battle.Date.AsDate();
                return date.Year * 100 + date.Month;
            }
            catch
            {
                return 0;
            }
        }

        private static string ResultText(CampaignBattle battle)
        {
            if (battle == null)
                return "unknown";

            if (battle.Victor != null)
                return $"{PlayerName(battle.Victor)} won";

            if (battle.Avoider != null)
                return $"{PlayerName(battle.Avoider)} avoided battle";

            if (Math.Abs(battle.VictoryPointsAttacker - battle.VictoryPointsDefender) > 0.01f)
            {
                Player leader = battle.VictoryPointsAttacker > battle.VictoryPointsDefender ? battle.Attacker : battle.Defender;
                return $"{PlayerName(leader)} led on VP";
            }

            if (battle.CurrentState != CampaignBattleBase.State.Finished)
                return "pending";

            return $"VP {battle.VictoryPointsAttacker:0.#} / {battle.VictoryPointsDefender:0.#}";
        }

        private static string ShortOutcomeText(CampaignBattle battle)
        {
            if (battle == null)
                return "Clash";

            if (battle.Victor != null)
                return $"{PlayerName(battle.Victor)} won";

            if (battle.Avoider != null)
                return $"{PlayerName(battle.Avoider)} avoided";

            if (Math.Abs(battle.VictoryPointsAttacker - battle.VictoryPointsDefender) > 0.01f)
            {
                Player leader = battle.VictoryPointsAttacker > battle.VictoryPointsDefender ? battle.Attacker : battle.Defender;
                return $"{PlayerName(leader)} ahead";
            }

            return battle.CurrentState == CampaignBattleBase.State.Finished ? "Draw" : "Clash";
        }

        private static string ShortLossText(CampaignBattle battle, Player first, Player second)
        {
            int attackerSunk = CountShips(battle.AttackerShipsSink);
            int defenderSunk = CountShips(battle.DefenderShipsSink);
            float attackerDamage = SumBattleDamage(battle, battle.AttackerShips);
            float defenderDamage = SumBattleDamage(battle, battle.DefenderShips);

            if (attackerSunk == 0 && defenderSunk == 0 && attackerDamage < 0.1f && defenderDamage < 0.1f)
                return string.Empty;

            bool firstIsAttacker = SamePlayer(first, battle.Attacker);
            int firstSunk = firstIsAttacker ? attackerSunk : defenderSunk;
            int secondSunk = firstIsAttacker ? defenderSunk : attackerSunk;
            float firstDamage = firstIsAttacker ? attackerDamage : defenderDamage;
            float secondDamage = firstIsAttacker ? defenderDamage : attackerDamage;

            return $" - {firstSunk}/{firstDamage:0.#}% vs {secondSunk}/{secondDamage:0.#}%";
        }

        private static bool SamePlayer(Player a, Player b)
        {
            if (a == null || b == null)
                return false;

            return a == b || a.data == b.data;
        }

        private static string PlayerName(Player player)
        {
            if (player == null)
                return "Unknown";

            try
            {
                return player.Name(false);
            }
            catch
            {
                return player.data?.nameUi ?? player.data?.name ?? "Unknown";
            }
        }

        private static int CountShips(Il2CppSystem.Collections.Generic.List<Ship> ships)
        {
            if (ships == null)
                return 0;

            int count = 0;
            foreach (Ship ship in ships)
            {
                if (ship != null)
                    count++;
            }

            return count;
        }

        private static float SumTonnage(Il2CppSystem.Collections.Generic.List<Ship> ships)
        {
            if (ships == null)
                return 0f;

            float total = 0f;
            foreach (Ship ship in ships)
            {
                if (ship == null)
                    continue;

                try
                {
                    total += ship.Tonnage();
                }
                catch
                {
                }
            }

            return total;
        }

        private static float SumBattleDamage(CampaignBattle battle, Il2CppSystem.Collections.Generic.List<Ship> ships)
        {
            if (battle?.BattleDamage == null || ships == null)
                return 0f;

            float total = 0f;
            foreach (Ship ship in ships)
            {
                if (ship == null)
                    continue;

                try
                {
                    if (battle.BattleDamage.TryGetValue(ship, out float damage))
                        total += damage;
                }
                catch
                {
                }
            }

            return total;
        }

        private static float SumCrewLosses(CampaignBattle battle, Il2CppSystem.Collections.Generic.List<Ship> ships)
        {
            if (battle?.StartCrewLosses == null || ships == null)
                return 0f;

            float total = 0f;
            foreach (Ship ship in ships)
            {
                if (ship == null)
                    continue;

                try
                {
                    if (battle.StartCrewLosses.TryGetValue(ship, out float losses))
                        total += losses;
                }
                catch
                {
                }
            }

            return total;
        }

        private static string SafeBattleTypeForTrace(CampaignBattle battle)
        {
            try
            {
                return battle?.Type?.TypeToString() ?? "null";
            }
            catch
            {
                return "error";
            }
        }

        private static void AttachTooltips()
        {
            if (_Reporter == null || _Root == null)
                return;

            try
            {
                foreach (CampaignLogReporterElement element in _Root.GetComponentsInChildren<CampaignLogReporterElement>(true))
                {
                    if (element == null || element.gameObject == null || element.Text == null)
                        continue;

                    string text = element.Text.text;
                    if (string.IsNullOrEmpty(text) || !_TooltipsByText.TryGetValue(text, out string tooltip))
                        continue;

                    EnsureRawTooltip(element.gameObject, tooltip);
                }
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to attach tooltip: {ex.Message}");
            }
        }

        private static void EnsureRawTooltip(GameObject ui, string content)
        {
            if (ui == null || ui.GetComponent<OnEnter>() != null)
                return;

            OnEnter onEnter = ui.AddComponent<OnEnter>();
            onEnter.action = new System.Action(() => G.ui?.ShowTooltip(content, ui));

            OnLeave onLeave = ui.AddComponent<OnLeave>();
            onLeave.action = new System.Action(() => G.ui?.HideTooltip());
        }

        private sealed class ClashEntry
        {
            public ClashEntry(string id, Player player, string line, string tooltip)
            {
                Id = id;
                Player = player;
                Line = line;
                Tooltip = tooltip;
            }

            public string Id { get; }
            public Player Player { get; }
            public string Line { get; }
            public string Tooltip { get; }
        }
    }

    [HarmonyPatch(typeof(BattleManager))]
    internal static class Patch_CampaignClashLog_BattleManager
    {
        [HarmonyTargetMethod]
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BattleManager), nameof(BattleManager.CompleteBattle), new[]
            {
                typeof(CampaignBattle),
                typeof(bool),
                typeof(bool)
            });
        }

        [HarmonyPostfix]
        internal static void Postfix_CompleteBattle(CampaignBattle battle)
        {
            if (Config.Param("taf_campaign_clash_log_enabled", 1) != 1 || battle == null)
                return;

            try
            {
                Ui ui = G.ui;
                if (ui == null || !GameManager.IsCampaign || !Patch_CampaignClashLog.IsClashBattle(battle))
                    return;

                Patch_CampaignClashLog.AddBattleToLog(battle);
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to record completed battle: {ex.Message}");
            }
        }
    }
}
