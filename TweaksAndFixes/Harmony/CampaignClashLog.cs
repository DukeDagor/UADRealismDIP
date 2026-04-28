using HarmonyLib;
using Il2Cpp;
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
        private static readonly Dictionary<string, string> _TooltipsByText = new();
        private static readonly FieldInfo _ReportsField = AccessTools.Field(typeof(CampaignLogReporterUI), "reports");
        private static CampaignLogReporterUI _Reporter;
        private static GameObject _Root;
        private static bool _LoggedCreateFailure;

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

            string line = BuildLine(battle);
            string tooltip = BuildTooltip(battle);
            _TooltipsByText[line] = tooltip;

            try
            {
                _Reporter.Report(battle.Attacker, true, line, Array.Empty<Il2CppSystem.Object>());
                AttachTooltips();
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"TAF clash log failed to report battle: {ex.Message}");
            }
        }

        private static void Update(Ui ui)
        {
            if (Config.Param("taf_campaign_clash_log_enabled", 1) != 1)
            {
                SetVisible(false);
                return;
            }

            bool visible = GameManager.IsCampaign && GameManager.IsWorldMap && ui?.LogReporter != null && ui.LogReporter.gameObject.activeInHierarchy;
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            if (Ensure(ui))
            {
                SetVisible(true);
                PositionAboveCampaignLog(ui);
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
                _Root.transform.SetParent(original.transform.parent, false);
                _Root.transform.SetSiblingIndex(Math.Max(0, original.transform.GetSiblingIndex()));
                _Reporter = _Root.GetComponent<CampaignLogReporterUI>();
                _Reporter.Clear();

                PositionAboveCampaignLog(ui);
                SetVisible(true);
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
            target.sizeDelta = new Vector2(source.sizeDelta.x, Mathf.Min(source.sizeDelta.y, 120f));
            target.localScale = source.localScale;

            float sourceHeight = source.rect.height > 1f ? source.rect.height : Math.Abs(source.sizeDelta.y);
            float targetHeight = target.rect.height > 1f ? target.rect.height : Math.Abs(target.sizeDelta.y);
            target.anchoredPosition = source.anchoredPosition + new Vector2(0f, (sourceHeight + targetHeight) * 0.5f + 8f);
        }

        private static void SetVisible(bool visible)
        {
            if (_Root != null && _Root.activeSelf != visible)
                _Root.SetActive(visible);
        }

        private static bool IsClashBattle(CampaignBattle battle)
        {
            if (battle == null || battle.Type == null)
                return false;

            return battle.Type.CurrentType == BattleTypeEx.EBattleType.TaskForce
                || battle.IsTaskForceBattle();
        }

        private static string BuildLine(CampaignBattle battle)
        {
            string attacker = PlayerName(battle.Attacker);
            string defender = PlayerName(battle.Defender);
            string location = SafeLocation(battle);
            string result = battle.CurrentState == CampaignBattleBase.State.Finished ? ResultText(battle) : "pending";
            return $"Clash: {attacker} vs {defender} - {location} - {result}";
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

            return $"{type}\n"
                + $"{date} - {location}\n"
                + $"{attacker}: {attackerShips} ships, {attackerTonnage:0} t, sunk {attackerSunk}\n"
                + $"{defender}: {defenderShips} ships, {defenderTonnage:0} t, sunk {defenderSunk}\n"
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

        private static string ResultText(CampaignBattle battle)
        {
            if (battle == null)
                return "unknown";

            if (battle.Victor != null)
                return $"{PlayerName(battle.Victor)} victory";

            if (battle.Avoider != null)
                return $"{PlayerName(battle.Avoider)} avoided battle";

            if (battle.CurrentState != CampaignBattleBase.State.Finished)
                return "pending";

            return $"VP {battle.VictoryPointsAttacker:0.#} / {battle.VictoryPointsDefender:0.#}";
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

        private static void AttachTooltips()
        {
            if (_Reporter == null || _ReportsField == null)
                return;

            try
            {
                var reports = _ReportsField.GetValue(_Reporter) as Il2CppSystem.Collections.Generic.List<CampaignLogReporterElement>;
                if (reports == null)
                    return;

                foreach (CampaignLogReporterElement element in reports)
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
    }
}
