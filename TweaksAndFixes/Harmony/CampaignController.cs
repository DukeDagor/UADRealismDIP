using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Linq;
using UnityEngine;
using static Il2Cpp.CampaignController;
using System.Diagnostics;

#pragma warning disable CS8602
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignController))]
    internal class Patch_CampaignController
    {
        internal struct CampaignLoadMethodTimingFrame
        {
            public bool Enabled;
            public string Method;
            public string Details;
            public int Session;
            public int LoadState;
            public string LoadStateLabel;
            public long StartedAt;
            public long PrefixEndedAt;
        }

        internal static CampaignLoadMethodTimingFrame BeginCampaignLoadMethodTiming(string method, string details = "")
        {
            if (!Patch_GameManager_LoadCampaigndCoroutine.IsTimingEnabled ||
                !Patch_GameManager_LoadCampaigndCoroutine.IsTimingActive)
            {
                return default;
            }

            var frame = new CampaignLoadMethodTimingFrame
            {
                Enabled = true,
                Method = method,
                Details = details,
                Session = Patch_GameManager_LoadCampaigndCoroutine.TimingSession,
                LoadState = Patch_GameManager_LoadCampaigndCoroutine.TimingCurrentState,
                LoadStateLabel = Patch_GameManager_LoadCampaigndCoroutine.TimingCurrentStateLabel,
                StartedAt = Stopwatch.GetTimestamp()
            };

            Melon<TweaksAndFixes>.Logger.Msg(
                $"Campaign load timing method begin: session={frame.Session}, " +
                $"state={frame.LoadState} ({frame.LoadStateLabel}), method={method}" +
                $"{FormatCampaignLoadTimingDetails(details)}");

            return frame;
        }

        internal static void EndCampaignLoadMethodPrefix(ref CampaignLoadMethodTimingFrame frame)
        {
            if (frame.Enabled)
            {
                frame.PrefixEndedAt = Stopwatch.GetTimestamp();
            }
        }

        internal static void EndCampaignLoadMethodTiming(CampaignLoadMethodTimingFrame frame)
        {
            if (!frame.Enabled)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            double totalMs = ElapsedCampaignLoadTimingMs(frame.StartedAt, now);
            string prefixTiming = "";

            if (frame.PrefixEndedAt != 0)
            {
                double prefixMs = ElapsedCampaignLoadTimingMs(frame.StartedAt, frame.PrefixEndedAt);
                double originalMs = ElapsedCampaignLoadTimingMs(frame.PrefixEndedAt, now);
                prefixTiming = $", prefixMs={prefixMs:0.0}, originalMs={originalMs:0.0}";
            }

            Melon<TweaksAndFixes>.Logger.Msg(
                $"Campaign load timing method end: session={frame.Session}, " +
                $"state={frame.LoadState} ({frame.LoadStateLabel}), method={frame.Method}, " +
                $"elapsedMs={totalMs:0.0}{prefixTiming}" +
                $"{FormatCampaignLoadTimingDetails(frame.Details)}");
        }

        private static double ElapsedCampaignLoadTimingMs(long startedAt, long endedAt)
        {
            return (endedAt - startedAt) * 1000.0 / Stopwatch.Frequency;
        }

        private static string FormatCampaignLoadTimingDetails(string details)
        {
            return string.IsNullOrEmpty(details) ? "" : $", {details}";
        }

        private static string DescribePredefinedDesignTiming(CampaignController controller, bool prewarm)
        {
            string currentDesigns = controller?._currentDesigns == null ? "null" : "loaded";
            string usage = controller == null ? "?" : controller.designsUsage.ToString();
            return $"prewarm={prewarm}, designsUsage={usage}, currentDesigns={currentDesigns}";
        }

        [HarmonyPatch(nameof(CampaignController.CheckTension))]
        [HarmonyPrefix]
        internal static bool Prefix_CheckTension()
        {
            if (Config.Param("taf_disable_fleet_tension", 1) == 1)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Skipping tension check...");
                return false;
            }
            return true;
        }


        // GetResearchSpeed
        [HarmonyPatch(nameof(CampaignController.GetResearchSpeed))]
        [HarmonyPrefix]
        internal static void Prefix_GetResearchSpeed(Player player, Technology tech)
        {
            if (Config.Param("taf_ai_disable_tech_priorities", 1) == 1 && player.isAi)
            {
                player.techPriorities.Clear();
            }
        }

        [HarmonyPatch(nameof(CampaignController.GetStore))]
        [HarmonyPostfix]
        internal static void Postfix_GetStore(ref CampaignController.Store __result)
        {
            for (int i = __result.Ships.Count - 1; i >= 0; i--)
            {
                if (__result.Ships[i].status == VesselEntity.Status.Erased
                    || __result.Ships[i].status == VesselEntity.Status.Sunk
                    || __result.Ships[i].status == VesselEntity.Status.Scrapped)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Removing {__result.Ships[i].status} ship/design '{__result.Ships[i].vesselName}'");

                    bool hasDesign = false;

                    if (__result.Ships[i].designId == Il2CppSystem.Guid.Empty)
                    {
                        for (int j = __result.Ships.Count - 1; j >= 0; j--)
                        {
                            if (__result.Ships[j].designId == __result.Ships[i].id)
                            {
                                // Melon<TweaksAndFixes>.Logger.Msg($"    Erased design has {__result.Ships[j].status} ship '{__result.Ships[j].vesselName}'");
                                hasDesign = true;
                                break;
                            }
                        }
                    }

                    if (!hasDesign)
                        __result.Ships.RemoveAt(i);
                }
            }
        }



        // ########## Fixes by Crux10086 ########## //

        // Direct fix for moving ships freeze

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CampaignController.__c))]
        [HarmonyPatch("_CheckMinorNationThreat_b__138_1")]
        public static void CheckMinorNationThreat_b__138_1(Player p, ref bool __result)
        {
            if (__result && p.data.name == "neutral")
            {
                __result = false;
            }
        }






        [HarmonyPatch(nameof(CampaignController.FinishCampaign))]
        [HarmonyPrefix]
        internal static bool Prefix_FinishCampaign(CampaignController __instance, Player loser, FinishCampaignType finishType)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Attempting to end campagin: {finishType} for {loser.Name(false)}");

            // Ignore all other campaign ending types
            if (finishType != FinishCampaignType.Retirement)
            {
                Melon<TweaksAndFixes>.Logger.Msg("  Not retirement, skipping end!");
                return true;
            }

            float campaignEndDate = Config.Param("taf_campaign_end_retirement_date", 1950);

            // If the year is less than the deisred retirement year, block the function
            if (__instance.CurrentDate.AsDate().Year < campaignEndDate)
            {
                return false;
            }

            // If the year is equal or greter than the desired retirement date, let it run
            return true;
        }

        public static bool isLoadingNewTurn = false;

        [HarmonyPatch(nameof(CampaignController.NextTurn))]
        [HarmonyPrefix]
        internal static void Prefix_NextTurn(CampaignController __instance)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"NextTurn"); // <<< Trigger on hit new turn button
            isLoadingNewTurn = true;
        }

        [HarmonyPatch(nameof(CampaignController.OnNewTurn))]
        [HarmonyPrefix]
        internal static void Prefix_OnNewTurn(CampaignController __instance)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"OnNewTurn"); // <<< Trigger on start of new turn

            EnsureAiDesignServiceStarted(__instance);

            var vessels = __instance.CampaignData.Vessels;

            for (int i = vessels.Count - 1; i >= 0; i--)
            {
                if (vessels[i].status == VesselEntity.Status.Erased
                    || vessels[i].status == VesselEntity.Status.Sunk
                    || vessels[i].status == VesselEntity.Status.Scrapped)
                {
                    if (vessels[i].vesselType == VesselEntity.VesselType.Submarine)
                        continue;

                    // Melon<TweaksAndFixes>.Logger.Msg($"Removing {vessels[i].status} ship '{vessels[i].vesselName}'");

                    bool hasDesign = false;

                    for (int j = vessels.Count - 1; j >= 0; j--)
                    {
                        if (((Ship)vessels[j]).design == vessels[i])
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"    Erased design has {vessels[j].status} ship '{vessels[j].vesselName}'");
                            hasDesign = true;
                            break;
                        }
                    }

                    if (!hasDesign)
                        Ship.TryToEraseVessel(vessels[i]);
                }
            }

            Patch_Player.ResetChangePlayerGDP();
            isLoadingNewTurn = false;
        }

        [HarmonyPatch(nameof(CampaignController.DeleteDesign))]
        [HarmonyPrefix]
        internal static bool Prefix_DeleteDesign(CampaignController __instance, Ship ship)
        {
            if (!ship.player.isAi) return true;

            foreach (Ship s in ship.player.GetFleetAll())
            {
                if (s.design != ship) continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"AI attempted to delete design {ship.Name(false, false)} despite having ships of this class afloat.");

                ship.SetStatus(VesselEntity.Status.Erased);

                return false;
            }

            return true;
        }

        private static float AnswerEventWealth = 0;

        [HarmonyPatch(nameof(CampaignController.AnswerEvent))]
        [HarmonyPrefix]
        internal static void Prefix_AnswerEvent(CampaignController __instance, EventX ev, ref EventData answer)
        {
            if (answer.wealth != 0) Patch_Player.RequestChangePlayerGDP(ev.player, answer.wealth / 100);

            // Melon<TweaksAndFixes>.Logger.Msg($"Answer Event for {ev.player.Name(false)}:");
            // Melon<TweaksAndFixes>.Logger.Msg($"  {ev.date.AsDate().ToString("y")}\t: {ev.data.name} -> {answer.name}");
            // var conditions = ev.data.param.Split(",");
            // Melon<TweaksAndFixes>.Logger.Msg($"  Condition    : {(conditions.Length > 0 ? conditions[0] : "NO CONDITION")}");
            // Melon<TweaksAndFixes>.Logger.Msg($"  Naval Funds  : {answer.transferMoney} {answer.money}%");
            // Melon<TweaksAndFixes>.Logger.Msg($"  Naval Budget : {answer.budget}%");
            // Melon<TweaksAndFixes>.Logger.Msg($"  GDP          : {answer.wealth}%");
            // Melon<TweaksAndFixes>.Logger.Msg($"  Relations    : {answer.relation}");
            // Melon<TweaksAndFixes>.Logger.Msg($"  Prestige     : {answer.reputation}");
            // Melon<TweaksAndFixes>.Logger.Msg($"  Unrest       : {answer.respect}");

            // ev.player.cash += player.Budget() * answer.money / 100;
            // ev.player.budgetMod += answer.budget;
            // ev.player.reputation += answer.reputation;
            // ev.player.AddUnrest(-answer.respect);
            // ev.data.param.Contains("special/message_for_player");

            AnswerEventWealth = answer.wealth;
            answer.wealth = 0;
        }

        [HarmonyPatch(nameof(CampaignController.AnswerEvent))]
        [HarmonyPostfix]
        internal static void Postfix_AnswerEvent(CampaignController __instance, EventX ev, EventData answer)
        {
            answer.wealth = AnswerEventWealth;
            AnswerEventWealth = 0;
        }

        [HarmonyPatch(nameof(CampaignController.CheckForCampaignEnd))]
        [HarmonyPostfix]
        internal static void Postfix_CheckForCampaignEnd(CampaignController __instance)
        {
            Melon<TweaksAndFixes>.Logger.Msg("Checking for campaign end...");

            if (__instance.CurrentDate.turn < 2)
            {
                Melon<TweaksAndFixes>.Logger.Msg("  Ignoring because it's the first turn...");
                return;
            }

            // Melon<TweaksAndFixes>.Logger.Msg($"  Checking on {__instance.CurrentDate.turn}");

            int activeCount = 0;
            List<Player> activePlayers = new();
            
            Player MainPlayer = ExtraGameData.MainPlayer();

            // sanity check
            if (MainPlayer == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CheckForCampaignEnd]. Default behavior will be used.");
                return;
            }

            foreach (Player player in __instance.CampaignData.Players)
            {
                if (player.isDisabled) continue;
                if (!player.isMajor) continue;
            
                activeCount++;
                activePlayers.Add(player);
            
                // if (player.isMain) continue;
                //
                // if (player.cash < -player.NationYearIncome() * 0.05f)
                // {
                //     // TotalBankrupt
                //     Melon<TweaksAndFixes>.Logger.Msg($"  {player.Name(false)} falls due to Total Bankruptcy.");
                //     // __instance.FinishCampaign(player, FinishCampaignType.TotalBankrupt);
                // }
                // else if (player.unrest >= 100)
                // {
                //     // HighUnrest
                //     Melon<TweaksAndFixes>.Logger.Msg($"  {player.Name(false)} falls due to High Unrest.");
                //     // __instance.FinishCampaign(player, FinishCampaignType.HighUnrest);
                // }
                // else if (player.provinces.Count == 0)
                // {
                //     // TotalDefeat
                //     Melon<TweaksAndFixes>.Logger.Msg($"  {player.Name(false)} falls due to Total Defeat.");
                //     // __instance.FinishCampaign(player, FinishCampaignType.TotalDefeat);
                // }
            }

            if (activePlayers.Count == 1)
            {
                // PeaceSigned
                Melon<TweaksAndFixes>.Logger.Msg($"  {activePlayers[0].Name(false)} wins due to Total Victory.");
                __instance.FinishCampaign(activePlayers[0], FinishCampaignType.PeaceSigned); // This is properly parsed by the base game. Only here for postarity.
                return;
            }

            bool hasPeaceBeenSigned = true;

            foreach (var relation in __instance.CampaignData.Relations)
            {
                if (!relation.Value.isAlliance)
                {
                    hasPeaceBeenSigned = false;
                    break;
                }
            }

            if (hasPeaceBeenSigned)
            {
                // PeaceSigned
                __instance.FinishCampaign(MainPlayer, FinishCampaignType.PeaceSigned);
                return;
            }

            if (MainPlayer.unrest >= 99.495)
            {
                // HighUnrest
                Melon<TweaksAndFixes>.Logger.Msg($"  {MainPlayer.Name(false)} falls due to High Unrest.");
                __instance.FinishCampaign(MainPlayer, FinishCampaignType.HighUnrest);
                return;
            }
            else if (MainPlayer.reputation <= -100)
            {
                // Low reputation
                Melon<TweaksAndFixes>.Logger.Msg($"  {MainPlayer.Name(false)} falls due to Low Reputation.");
                __instance.FinishCampaign(MainPlayer, FinishCampaignType.LoseEvent);
                return;
            }
            else if (MainPlayer.cash < 0)
            {
                // TotalBankrupt

                Melon<TweaksAndFixes>.Logger.Msg($"  {MainPlayer.Name(false)} ran out of cash!");

                if (MainPlayer.Budget() * 2 < -MainPlayer.cash)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  {MainPlayer.Name(false)} falls due to Total Bankruptcy.");
                    __instance.FinishCampaign(MainPlayer, FinishCampaignType.TotalBankrupt);
                    return;
                }

                Melon<TweaksAndFixes>.Logger.Msg($"  {MainPlayer.Name(false)} is getting a bailout!");

                UiM.ShowBailoutPopupForPlayer(MainPlayer);

                return;
            }
            else if (MainPlayer.provinces.Count == 0)
            {
                // TotalDefeat
                Melon<TweaksAndFixes>.Logger.Msg($"  {MainPlayer.Name(false)} falls due to Total Defeat.");
                __instance.FinishCampaign(MainPlayer, FinishCampaignType.TotalDefeat); // This is properly parsed by the base game. Only here for postarity.
                return;
            }

            float campaignEndDate = Config.Param("taf_campaign_end_retirement_date", 1950);
            float retirementPromptFrequency = Config.Param("taf_campaign_end_retirement_promt_every_x_months", 12);

            // If the year is equal or greter than the desired retirement date force game end
            if (__instance.CurrentDate.AsDate().Year >= campaignEndDate)
            {
                // Check for month interval
                int monthsSinceFirstRequest = __instance.CurrentDate.AsDate().Month + (__instance.CurrentDate.AsDate().Year - 1890) * 12;

                if (retirementPromptFrequency != 0 && monthsSinceFirstRequest % retirementPromptFrequency != 0)
                {
                    Melon<TweaksAndFixes>.Logger.Msg("  Skipping retirement request.");
                    return;
                }

                MessageBoxUI.MessageBoxQueue queue = new MessageBoxUI.MessageBoxQueue();
                queue.Header = LocalizeManager.Localize("$TAF_Ui_Retirement_Header");
                queue.Text = String.Format(LocalizeManager.Localize("$TAF_Ui_Retirement_Body"), __instance.CurrentDate.AsDate().Year - __instance.StartYear, retirementPromptFrequency);
                queue.Ok = LocalizeManager.Localize("$Ui_Popup_Generic_Yes");
                queue.Cancel = LocalizeManager.Localize("$Ui_Popup_Generic_No");
                queue.canBeClosed = false;
                queue.OnConfirm = new System.Action(() =>
                {
                    __instance.FinishCampaign(MainPlayer, FinishCampaignType.Retirement);
                });
                MessageBoxUI.Messages.Enqueue(queue);
            }

            // Melon<TweaksAndFixes>.Logger.Msg(ModUtils.DumpHierarchy(G.ui.WorldMapWindow));
        }

        internal static CampaignController._AiManageFleet_d__201? _AiManageFleet = null;
        private static int _SkippedPrewarmBuildNewShipsCount = 0;
        private static int _SkippedPrestartRandomDesignsCount = 0;
        private static int _SkippedServiceOwnedRandomDesignsCount = 0;
        private static object? _AiDesignServiceRoutine = null;
        private static CampaignController? _AiDesignServiceRequestedController = null;
        private static bool _AiDesignServiceStartRequested = false;
        private static System.Reflection.MethodInfo? _GenerateRandomDesignsMethod = null;
        internal static bool _AiDesignServiceRunningGenerateRandomDesigns = false;
        private static int _AiDesignServiceCycle = 0;
        private static int _AiDesignServiceNextPendingId = 0;
        private static readonly Dictionary<IntPtr, AiDesignGenerationTrace> _GenerateRandomDesignTraces = new();
        private static readonly Dictionary<IntPtr, AiDesignServiceJob> _AiDesignServiceJobs = new();
        private static readonly Dictionary<int, AiDesignServiceJob> _AiDesignServicePendingJobs = new();

        internal struct AiBuildTrace
        {
            public bool Enabled;
            public string PlayerName;
            public int Year;
            public int Month;
            public bool Prewarming;
            public float Cash;
            public float TempCash;
            public float Capacity;
            public int Designs;
            public int Building;
            public int Active;
            public int Other;
            public float BuildingTonnage;
            public float SmallestDesignTonnage;
            public float LargestDesignTonnage;
            public string DesignClasses;
            public string BuildingClasses;
            public HashSet<Il2CppSystem.Guid> DesignIds;
            public HashSet<Il2CppSystem.Guid> BuildingIds;
        }

        private struct AiDesignGenerationTrace
        {
            public bool Enabled;
            public string PlayerName;
            public int Year;
            public int Month;
            public bool Prewarming;
            public bool ServiceOwned;
            public int DesignCount;
            public string DesignClasses;
            public HashSet<Il2CppSystem.Guid> DesignIds;
        }

        private class AiDesignServiceJob
        {
            public string PlayerName = "?";
            public int Cycle;
            public int Year;
            public int Month;
            public int DesignCount;
            public string DesignClasses = "-";
            public HashSet<Il2CppSystem.Guid> DesignIds = new();
            public bool Completed;
            public bool Started;
            public bool Prewarming;
            public int PendingId;
            public IntPtr PlayerPointer;
            public IntPtr RoutinePointer;
        }

        private class AiGenerateRandomDesignRoutine
        {
            public Il2CppSystem.Collections.IEnumerator Enumerator = null!;
            public string RawType = "?";
        }

        private static bool IsAiBuildDebugEnabled()
            => Config.Param("taf_debug_ai_shipbuilding", 0) != 0;

        internal static bool IsAiDesignServiceEnabled()
            => Config.Param("taf_campaign_ai_design_service_enabled", 0) != 0;

        private static bool IsAiDesignServiceDebugEnabled()
            => Config.Param("taf_debug_ai_design_service", 0) != 0;

        internal static bool ShouldSkipServiceOwnedRandomDesigns()
            => IsAiDesignServiceEnabled() && Config.Param("taf_campaign_ai_design_service_disable_endturn_generation", 0) != 0;

        private static AiBuildTrace CaptureAiBuildTrace(CampaignController controller, Player player, float tempPlayerCash)
        {
            AiBuildTrace trace = new()
            {
                Enabled = IsAiBuildDebugEnabled() && player != null && player.isAi,
                PlayerName = player == null ? "?" : player.Name(false),
                Year = controller?.CurrentDate.AsDate().Year ?? 0,
                Month = controller?.CurrentDate.AsDate().Month ?? 0,
                Prewarming = _AiManageFleet != null && _AiManageFleet.prewarming,
                TempCash = tempPlayerCash,
                DesignIds = new HashSet<Il2CppSystem.Guid>(),
                BuildingIds = new HashSet<Il2CppSystem.Guid>(),
                DesignClasses = string.Empty,
                BuildingClasses = string.Empty
            };

            if (!trace.Enabled)
                return trace;

            try { trace.Cash = player.cash; } catch { trace.Cash = 0f; }
            try { trace.Capacity = player.ShipbuildingCapacityLimit(); } catch { trace.Capacity = 0f; }

            Dictionary<string, int> designClasses = new();
            foreach (Ship design in new Il2CppSystem.Collections.Generic.List<Ship>(player.designs))
            {
                if (design == null || !design.isDesign)
                    continue;

                trace.Designs++;
                trace.DesignIds.Add(design.id);
                AddClassCount(designClasses, design);
                float tonnage = SafeTonnage(design);
                if (tonnage > 0f)
                {
                    trace.SmallestDesignTonnage = trace.SmallestDesignTonnage <= 0f ? tonnage : Math.Min(trace.SmallestDesignTonnage, tonnage);
                    trace.LargestDesignTonnage = Math.Max(trace.LargestDesignTonnage, tonnage);
                }
            }

            Dictionary<string, int> buildingClasses = new();
            foreach (Ship ship in player.GetFleetAll())
            {
                if (ship == null || ship.isDesign || ship.isScrapped || ship.isSunk)
                    continue;

                if (ship.isBuilding || ship.isCommissioning)
                {
                    trace.Building++;
                    trace.BuildingIds.Add(ship.id);
                    AddClassCount(buildingClasses, ship.design ?? ship);
                    trace.BuildingTonnage += SafeTonnage(ship.design ?? ship);
                }
                else if (ship.isAlive && !ship.isRepairing && !ship.isRefit)
                    trace.Active++;
                else
                    trace.Other++;
            }

            trace.DesignClasses = FormatClassCounts(designClasses);
            trace.BuildingClasses = FormatClassCounts(buildingClasses);
            return trace;
        }

        private static void AddClassCount(Dictionary<string, int> counts, Ship ship)
        {
            string cls = ship?.shipType?.name?.ToUpperInvariant() ?? "?";
            counts[cls] = counts.TryGetValue(cls, out int count) ? count + 1 : 1;
        }

        private static float SafeTonnage(Ship ship)
        {
            if (ship == null)
                return 0f;

            try { return ship.Tonnage(); }
            catch { return 0f; }
        }

        private static string FormatClassCounts(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "-";

            return string.Join(", ", counts.OrderBy(kvp => ShipTypeSortRank(kvp.Key)).ThenBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        private static int ShipTypeSortRank(string cls)
        {
            return cls.ToLowerInvariant() switch
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

        private static void LogAiBuildTrace(CampaignController controller, Player player, float tempPlayerCash, AiBuildTrace before, bool skipped)
        {
            if (!before.Enabled)
                return;

            AiBuildTrace after = CaptureAiBuildTrace(controller, player, tempPlayerCash);
            int newDesigns = Math.Max(0, after.Designs - before.Designs);
            int newBuilding = Math.Max(0, after.Building - before.Building);
            List<string> newDesignNames = new();
            List<string> newBuildNames = new();

            foreach (Ship design in new Il2CppSystem.Collections.Generic.List<Ship>(player.designs))
            {
                if (design == null || !design.isDesign || before.DesignIds.Contains(design.id))
                    continue;

                newDesignNames.Add(DescribeAiBuildShip(design));
            }

            foreach (Ship ship in player.GetFleetAll())
            {
                if (ship == null || ship.isDesign || before.BuildingIds.Contains(ship.id))
                    continue;
                if (!ship.isBuilding && !ship.isCommissioning)
                    continue;

                newBuildNames.Add(DescribeAiBuildShip(ship));
            }

            string outcome = skipped ? "skipped" : (newDesigns == 0 && newBuilding == 0 ? "no new orders" : "changed");
            Melon<TweaksAndFixes>.Logger.Msg($"AI shipbuilding {outcome}: {before.PlayerName}, date={before.Year:D4}-{before.Month:D2}, prewarm={before.Prewarming}, cash={before.Cash:N0}, tempCash={before.TempCash:N0}, capacity={before.Capacity:N0}");
            Melon<TweaksAndFixes>.Logger.Msg($"  Before: designs={before.Designs} [{before.DesignClasses}], building={before.Building} [{before.BuildingClasses}], active={before.Active}, other={before.Other}");
            Melon<TweaksAndFixes>.Logger.Msg($"  After : designs={after.Designs} [{after.DesignClasses}], building={after.Building} [{after.BuildingClasses}], active={after.Active}, other={after.Other}");

            if (newDesignNames.Count > 0)
                Melon<TweaksAndFixes>.Logger.Msg($"  New designs: {string.Join("; ", newDesignNames)}");
            if (newBuildNames.Count > 0)
                Melon<TweaksAndFixes>.Logger.Msg($"  New builds : {string.Join("; ", newBuildNames)}");
            if (!skipped && newDesignNames.Count == 0 && newBuildNames.Count == 0)
            {
                float freeCapacityBefore = before.Capacity > 0f ? before.Capacity - before.BuildingTonnage : 0f;
                string designTonnage = before.Designs > 0 ? $"{before.SmallestDesignTonnage:N0}-{before.LargestDesignTonnage:N0}t" : "-";
                string inferred = before.Designs == 0
                    ? "no player.designs entries available to build from"
                    : before.Building > 0
                        ? "already has ships under construction; vanilla AI may be satisfied or budget/capacity gated"
                        : "has designs and no current builds; deeper CreateRandom/shared-design/budget traces should explain the drop";
                Melon<TweaksAndFixes>.Logger.Msg($"  No-build context: buildingTonnage={before.BuildingTonnage:N0}t, freeCapacityApprox={freeCapacityBefore:N0}t, designTonnageRange={designTonnage}, inferred={inferred}");
                Melon<TweaksAndFixes>.Logger.Msg("  No visible change from BuildNewShips; next checks are budget/capacity gates, available designs by class, and shipgen/shared-design failure logs.");
            }
        }

        internal static void EnsureAiDesignServiceStarted(CampaignController controller)
        {
            if (!IsAiDesignServiceEnabled())
                return;
            if (controller == null)
                return;

            _AiDesignServiceRequestedController = controller;
            _AiDesignServiceStartRequested = true;
        }

        internal static void UpdateAiDesignService()
        {
            if (!IsAiDesignServiceEnabled())
            {
                _AiDesignServiceStartRequested = false;
                return;
            }

            if (_AiDesignServiceRoutine != null)
                return;

            CampaignController controller = CampaignController.Instance ?? _AiDesignServiceRequestedController;
            if (controller == null || controller.CampaignData == null || controller.CampaignData.Players == null)
                return;
            if (GameManager.Instance == null || !GameManager.Instance.isCampaign || GameManager.Instance.CurrentState != GameManager.GameState.World || GameManager.IsLoadingScreenActive)
                return;

            string source = _AiDesignServiceStartRequested ? "OnNewTurn request" : "Ui.Update campaign pump";
            _AiDesignServiceStartRequested = false;
            _AiDesignServiceRoutine = MelonCoroutines.Start(AiDesignServiceLoop(controller));
            Melon<TweaksAndFixes>.Logger.Msg($"AI design service scheduled by {source} in state {GameManager.Instance.CurrentState}.");
        }

        private static System.Collections.IEnumerator AiDesignServiceLoop(CampaignController initialController)
        {
            Melon<TweaksAndFixes>.Logger.Msg("AI design service loop entered.");

            float waitUntil = Time.realtimeSinceStartup + Math.Max(0.1f, Config.Param("taf_campaign_ai_design_service_start_delay_seconds", 1f));
            while (Time.realtimeSinceStartup < waitUntil)
                yield return new WaitForEndOfFrame();

            while (IsAiDesignServiceEnabled())
            {
                CampaignController controller = CampaignController.Instance ?? initialController;
                if (controller == null || controller.CampaignData == null || controller.CampaignData.Players == null)
                {
                    waitUntil = Time.realtimeSinceStartup + 1f;
                    while (Time.realtimeSinceStartup < waitUntil)
                        yield return new WaitForEndOfFrame();
                    continue;
                }

                _AiDesignServiceCycle++;
                List<Player> players = new();
                foreach (Player player in controller.CampaignData.Players)
                {
                    if (ShouldRunAiDesignServiceFor(player))
                        players.Add(player);
                }

                if (IsAiDesignServiceDebugEnabled())
                {
                    int year = controller.CurrentDate.AsDate().Year;
                    int month = controller.CurrentDate.AsDate().Month;
                    Melon<TweaksAndFixes>.Logger.Msg($"AI design service cycle {_AiDesignServiceCycle} begin: date={year:D4}-{month:D2}, aiMajorPlayers={players.Count}");
                }

                foreach (Player player in players)
                {
                    yield return RunAiDesignServiceForPlayer(controller, player, _AiDesignServiceCycle);
                    waitUntil = Time.realtimeSinceStartup + Math.Max(0.05f, Config.Param("taf_campaign_ai_design_service_player_delay_seconds", 0.25f));
                    while (Time.realtimeSinceStartup < waitUntil)
                        yield return new WaitForEndOfFrame();
                }

                waitUntil = Time.realtimeSinceStartup + Math.Max(0.1f, Config.Param("taf_campaign_ai_design_service_cycle_delay_seconds", 5f));
                while (Time.realtimeSinceStartup < waitUntil)
                    yield return new WaitForEndOfFrame();
            }

            _AiDesignServiceRoutine = null;
            Melon<TweaksAndFixes>.Logger.Msg("AI design service stopped because taf_campaign_ai_design_service_enabled is off.");
        }

        private static bool ShouldRunAiDesignServiceFor(Player player)
        {
            if (player == null)
                return false;

            try
            {
                return player.isAi && player.isMajor && !player.isDisabled;
            }
            catch
            {
                return false;
            }
        }

        private static System.Collections.IEnumerator RunAiDesignServiceForPlayer(CampaignController controller, Player player, int cycle)
        {
            string playerName = player?.Name(false) ?? "?";
            int year = controller?.CurrentDate.AsDate().Year ?? 0;
            int month = controller?.CurrentDate.AsDate().Month ?? 0;
            HashSet<Il2CppSystem.Guid> beforeIds = new();
            string beforeClasses = CaptureDesignClassSummary(player, beforeIds, out int beforeCount);

            if (IsAiDesignServiceDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"AI design service begin: {playerName}, cycle={cycle}, date={year:D4}-{month:D2}, designs={beforeCount} [{beforeClasses}]");

            AiGenerateRandomDesignRoutine? routine = null;
            try
            {
                routine = InvokeGenerateRandomDesigns(controller, player, false);
            }
            catch (System.Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Error($"AI design service failed to create GenerateRandomDesigns for {playerName}: {ex.Message}");
            }

            if (routine == null)
                yield break;

            AiDesignServiceJob serviceJob = new()
            {
                PendingId = ++_AiDesignServiceNextPendingId,
                PlayerName = playerName,
                Cycle = cycle,
                Year = year,
                Month = month,
                DesignCount = beforeCount,
                DesignClasses = beforeClasses,
                DesignIds = beforeIds,
                PlayerPointer = PlayerPointer(player),
                Prewarming = false
            };

            _AiDesignServicePendingJobs[serviceJob.PendingId] = serviceJob;

            try
            {
                AiDesignCoroutineHost.GetOrCreate().StartDesignCoroutine(routine.Enumerator);
            }
            catch (System.Exception ex)
            {
                _AiDesignServicePendingJobs.Remove(serviceJob.PendingId);
                Melon<TweaksAndFixes>.Logger.Error($"AI design service failed to StartCoroutine for {playerName}: {ex.Message}");
                yield break;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"AI design service Unity coroutine requested: {playerName}, cycle={cycle}, pending={serviceJob.PendingId}, raw={routine.RawType}");

            float timeoutAt = Time.realtimeSinceStartup + Math.Max(5f, Config.Param("taf_campaign_ai_design_service_job_timeout_seconds", 90f));
            while (!serviceJob.Completed)
            {
                if (Time.realtimeSinceStartup > timeoutAt)
                {
                    _AiDesignServicePendingJobs.Remove(serviceJob.PendingId);
                    if (serviceJob.RoutinePointer != IntPtr.Zero)
                        _AiDesignServiceJobs.Remove(serviceJob.RoutinePointer);

                    string bound = serviceJob.Started ? $"routine=0x{serviceJob.RoutinePointer.ToInt64():X}" : "routine=unbound";
                    Melon<TweaksAndFixes>.Logger.Error($"AI design service timed out waiting for Unity-hosted GenerateRandomDesigns: {playerName}, cycle={cycle}, pending={serviceJob.PendingId}, {bound}");
                    yield break;
                }

                yield return new WaitForEndOfFrame();
            }

            _AiDesignServicePendingJobs.Remove(serviceJob.PendingId);
            if (serviceJob.RoutinePointer != IntPtr.Zero)
                _AiDesignServiceJobs.Remove(serviceJob.RoutinePointer);
        }

        private static AiGenerateRandomDesignRoutine? InvokeGenerateRandomDesigns(CampaignController controller, Player player, bool prewarming)
        {
            _GenerateRandomDesignsMethod ??= AccessTools.Method(typeof(CampaignController), "GenerateRandomDesigns", new[] { typeof(Player), typeof(bool) });
            if (_GenerateRandomDesignsMethod == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("AI design service could not find CampaignController.GenerateRandomDesigns(Player,bool).");
                return null;
            }

            object? rawRoutine = _GenerateRandomDesignsMethod.Invoke(controller, new object[] { player, prewarming });
            Il2CppSystem.Collections.IEnumerator? enumerator = rawRoutine as Il2CppSystem.Collections.IEnumerator;

            if (enumerator == null)
            {
                Melon<TweaksAndFixes>.Logger.Error($"AI design service could not cast GenerateRandomDesigns result to IEnumerator. raw={rawRoutine?.GetType().FullName ?? "null"}");
                return null;
            }

            return new AiGenerateRandomDesignRoutine
            {
                Enumerator = enumerator,
                RawType = rawRoutine?.GetType().FullName ?? "null"
            };
        }

        private static string CaptureDesignClassSummary(Player player, HashSet<Il2CppSystem.Guid>? ids, out int count)
        {
            count = 0;
            Dictionary<string, int> classes = new();
            if (player == null || player.designs == null)
                return "-";

            foreach (Ship design in new Il2CppSystem.Collections.Generic.List<Ship>(player.designs))
            {
                if (design == null || !design.isDesign)
                    continue;

                count++;
                ids?.Add(design.id);
                AddClassCount(classes, design);
            }

            return FormatClassCounts(classes);
        }

        internal static void BeginGenerateRandomDesignTrace(CampaignController._GenerateRandomDesigns_d__202 routine)
        {
            if (!IsAiDesignServiceDebugEnabled())
                return;

            IntPtr pointer = RoutinePointer(routine);
            if (pointer == IntPtr.Zero || routine == null || routine.player == null || !routine.player.isAi)
                return;

            HashSet<Il2CppSystem.Guid> beforeIds = new();
            string beforeClasses = CaptureDesignClassSummary(routine.player, beforeIds, out int beforeCount);
            AiDesignGenerationTrace trace = new()
            {
                Enabled = true,
                PlayerName = routine.player.Name(false),
                Year = CampaignController.Instance?.CurrentDate.AsDate().Year ?? 0,
                Month = CampaignController.Instance?.CurrentDate.AsDate().Month ?? 0,
                Prewarming = routine.prewarming,
                ServiceOwned = IsAiDesignServiceRoutine(routine) || _AiDesignServiceRunningGenerateRandomDesigns,
                DesignCount = beforeCount,
                DesignClasses = beforeClasses,
                DesignIds = beforeIds
            };

            _GenerateRandomDesignTraces[pointer] = trace;
            Melon<TweaksAndFixes>.Logger.Msg($"AI GenerateRandomDesigns begin: {trace.PlayerName}, date={trace.Year:D4}-{trace.Month:D2}, prewarm={trace.Prewarming}, service={trace.ServiceOwned}, designs={trace.DesignCount} [{trace.DesignClasses}]");
        }

        internal static void EndGenerateRandomDesignTrace(CampaignController._GenerateRandomDesigns_d__202 routine, bool result)
        {
            if (result)
                return;

            IntPtr pointer = RoutinePointer(routine);
            if (pointer == IntPtr.Zero || !_GenerateRandomDesignTraces.TryGetValue(pointer, out AiDesignGenerationTrace trace))
                return;

            _GenerateRandomDesignTraces.Remove(pointer);
            if (!trace.Enabled || routine == null || routine.player == null)
                return;

            string afterClasses = CaptureDesignClassSummary(routine.player, null, out int afterCount);
            List<string> newDesigns = new();
            foreach (Ship design in new Il2CppSystem.Collections.Generic.List<Ship>(routine.player.designs))
            {
                if (design == null || !design.isDesign || trace.DesignIds.Contains(design.id))
                    continue;

                newDesigns.Add(DescribeAiBuildShip(design));
            }

            string addedText = newDesigns.Count == 0 ? "-" : string.Join("; ", newDesigns);
            string outcome = newDesigns.Count == 0 ? "no persisted designs" : "persisted";
            Melon<TweaksAndFixes>.Logger.Msg($"AI GenerateRandomDesigns {outcome}: {trace.PlayerName}, date={trace.Year:D4}-{trace.Month:D2}, prewarm={trace.Prewarming}, service={trace.ServiceOwned}, result={result}, designs={trace.DesignCount}->{afterCount} [{afterClasses}], added={addedText}");
        }

        internal static bool IsAiDesignServiceRoutine(CampaignController._GenerateRandomDesigns_d__202 routine)
        {
            IntPtr pointer = RoutinePointer(routine);
            return pointer != IntPtr.Zero && _AiDesignServiceJobs.ContainsKey(pointer);
        }

        internal static bool TryBindAiDesignServiceRoutine(CampaignController._GenerateRandomDesigns_d__202 routine)
        {
            IntPtr pointer = RoutinePointer(routine);
            if (pointer == IntPtr.Zero)
                return false;
            if (_AiDesignServiceJobs.ContainsKey(pointer))
                return true;
            if (routine == null || routine.player == null)
                return false;

            IntPtr playerPointer = PlayerPointer(routine.player);
            AiDesignServiceJob? match = null;
            foreach (AiDesignServiceJob job in _AiDesignServicePendingJobs.Values)
            {
                if (job.Started || job.Prewarming != routine.prewarming)
                    continue;
                if (job.PlayerPointer != IntPtr.Zero && playerPointer != IntPtr.Zero && job.PlayerPointer != playerPointer)
                    continue;

                match = job;
                break;
            }

            if (match == null)
                return false;

            match.Started = true;
            match.RoutinePointer = pointer;
            _AiDesignServicePendingJobs.Remove(match.PendingId);
            _AiDesignServiceJobs[pointer] = match;
            Melon<TweaksAndFixes>.Logger.Msg($"AI design service Unity coroutine bound: {match.PlayerName}, cycle={match.Cycle}, pending={match.PendingId}, routine=0x{pointer.ToInt64():X}");
            return true;
        }

        internal static void CompleteAiDesignServiceJob(CampaignController._GenerateRandomDesigns_d__202 routine)
        {
            IntPtr pointer = RoutinePointer(routine);
            if (pointer == IntPtr.Zero || !_AiDesignServiceJobs.TryGetValue(pointer, out AiDesignServiceJob job) || job.Completed)
                return;

            job.Completed = true;
            if (routine == null || routine.player == null)
                return;

            string afterClasses = CaptureDesignClassSummary(routine.player, null, out int afterCount);
            List<string> newDesigns = new();
            foreach (Ship design in new Il2CppSystem.Collections.Generic.List<Ship>(routine.player.designs))
            {
                if (design == null || !design.isDesign || job.DesignIds.Contains(design.id))
                    continue;

                newDesigns.Add(DescribeAiBuildShip(design));
            }

            string newDesignText = newDesigns.Count == 0 ? "-" : string.Join("; ", newDesigns);
            if (newDesigns.Count > 0)
                Melon<TweaksAndFixes>.Logger.Msg($"AI design service verified persisted design(s): {job.PlayerName}, cycle={job.Cycle}, designs={job.DesignCount}->{afterCount} [{afterClasses}], added={newDesignText}");

            if (IsAiDesignServiceDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"AI design service Unity coroutine completed: {job.PlayerName}, cycle={job.Cycle}, designs={job.DesignCount}->{afterCount} [{afterClasses}], newDesigns={newDesignText}");
        }

        private static IntPtr RoutinePointer(CampaignController._GenerateRandomDesigns_d__202 routine)
        {
            try
            {
                return routine?.Pointer ?? IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static IntPtr PlayerPointer(Player player)
        {
            try
            {
                return player?.Pointer ?? IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static string DescribeAiBuildShip(Ship ship)
        {
            string cls = ship?.shipType?.name?.ToUpperInvariant() ?? "?";
            string name = ship?.Name(false, false, false, false, true) ?? "?";
            int year = ship == null ? 0 : (ship.isRefitDesign ? ship.dateCreatedRefit : ship.dateCreated).AsDate().Year;
            float tons = 0f;
            try { tons = ship?.Tonnage() ?? 0f; } catch { tons = 0f; }
            return $"{cls} {name} ({year}, {tons:N0}t)";
        }

        [HarmonyPatch(nameof(CampaignController.Init))]
        [HarmonyPrefix]
        internal static void Prefix_Init(bool createOwnFleet, ref int campaignDesignsUsage)
        {
            Patch_CampaignNewGame.LogCampaignStartFleetCreation(createOwnFleet);

            if (Config.ForceNoPredefsInNewGames)
                campaignDesignsUsage = 0;
        }

        [HarmonyPatch(nameof(CampaignController.GetSharedDesign))]
        [HarmonyPrefix]
        internal static bool Prefix_GetSharedDesign(CampaignController __instance, Player player, ShipType shipType, int year, bool checkTech, bool isEarlySavedShip, ref Ship __result)
        {
            __result = CampaignControllerM.GetSharedDesign(__instance, player, shipType, year, checkTech, isEarlySavedShip);
            return false;
        }

        // We're going to cache off relations before the adjustment
        // and then check for changes.
        internal struct RelationInfo
        {
            public bool isWar;
            public bool isAlliance;
            public float attitude;
            public bool isValid;
            public List<Player>? alliesA;
            public List<Player>? alliesB;

            public RelationInfo(Relation old)
            {
                isValid = true;

                isWar = old.isWar;
                isAlliance = old.isAlliance;
                attitude = old.attitude;

                // Hopefully the perf hit of the GC alloc is balanced
                // by doing it native (we could avoid the alloc by finding
                // these players, but it'd be in managed code)
                alliesA = new List<Player>();
                foreach (var p in old.a.InAllianceWith().ToList())
                    alliesA.Add(p);
                alliesB = new List<Player>();
                foreach (var p in old.b.InAllianceWith().ToList())
                    alliesB.Add(p);
            }

            public RelationInfo()
            {
                isValid = false;
                isWar = isAlliance = false;
                attitude = 0;
                alliesA = alliesB = null;
            }
        }
        private static bool _PassThroughAdjustAttitude = false;
        [HarmonyPatch(nameof(CampaignController.AdjustAttitude))]
        [HarmonyPrefix]
        internal static void Prefix_AdjustAttitude(CampaignController __instance, Relation relation, float attitudeDelta, bool canFullyAdjust, bool init, string info, bool raiseEvents, bool force, bool fromCommonEnemy, out RelationInfo __state)
        {
            if (init || _PassThroughAdjustAttitude || !Config.AllianceTweaks)
            {
                __state = new RelationInfo();
                return;
            }

            __state = new RelationInfo(relation);
        }

        [HarmonyPatch(nameof(CampaignController.AdjustAttitude))]
        [HarmonyPostfix]
        internal static void Postfix_AdjustAttitude(CampaignController __instance, Relation relation, float attitudeDelta, bool canFullyAdjust, bool init, string info, bool raiseEvents, bool force, bool fromCommonEnemy, RelationInfo __state)
        {
            if (init || !__state.isValid)
                return;

            // Don't cascade. AdjustAttitude calls itself a bunch of times.
            // If we're applying relation-change events, don't rerun for each
            // sub-call of this.
            _PassThroughAdjustAttitude = true;
            if (relation.isWar != __state.isWar)
            {
                if (__state.isWar)
                {
                    // at peace now
                    // *** Commented out for now.
                    // Eventually want to have alliance leaders make peace
                    // (except for the human player). But until that code's
                    // written, no point in removing the player from alliances
                    // because the game already does that.

                    // check if the human is allied to either
                    // and is at war too. If so, break the alliance.
                    // (We don't force the player into peace.)
                    //for (int i = __state.alliesA.Count; i-- > 0;)
                    //{
                    //    Player p = __state.alliesA[i];
                    //    if (!p.isAi)
                    //    {
                    //        var relA = RelationExt.Between(__instance.CampaignData.Relations, p, relation.a);
                    //        var relB = RelationExt.Between(__instance.CampaignData.Relations, p, relation.b);
                    //        if (relA.isAlliance && relB.isWar) // had better be true
                    //        {
                    //            __instance.AdjustAttitude(relA, -relA.attitude, true, false, info, raiseEvents, true, fromCommonEnemy);
                    //            __state.alliesA.RemoveAt(i);
                    //        }
                    //        break;
                    //    }
                    //}
                    //for (int i = __state.alliesB.Count; i-- > 0;)
                    //{
                    //    Player p = __state.alliesB[i];
                    //    if (!p.isAi)
                    //    {
                    //        var relA = RelationExt.Between(__instance.CampaignData.Relations, p, relation.a);
                    //        var relB = RelationExt.Between(__instance.CampaignData.Relations, p, relation.b);
                    //        if (relB.isAlliance && relA.isWar) // had better be true
                    //        {
                    //            __instance.AdjustAttitude(relB, -relB.attitude, true, false, info, raiseEvents, true, fromCommonEnemy);
                    //            __state.alliesB.RemoveAt(i);
                    //        }
                    //        break;
                    //    }
                    //}

                    // TODO: Do we want to have strongest nations sign for all others?
                }
                else
                {
                    // at war now

                    Melon<TweaksAndFixes>.Logger.Msg($"State for {relation.a.Name(false)} x {relation.b.Name(false)} changed to war:");

                    Melon<TweaksAndFixes>.Logger.Msg($"  Find overlapping allies");
                    // First, find overlapping allies. They break
                    // both alliances.
                    for (int i = __state.alliesA.Count - 1; i > 0; i--)
                    {
                        Player p = __state.alliesA[i];
                        for (int j = __state.alliesB.Count - 1; j > 0; j--)
                        {
                            if (__state.alliesB[j] == p)
                            {
                                __state.alliesA.RemoveAt(i);
                                __state.alliesB.RemoveAt(j);
                                var rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.a);
                                if (rel.isAlliance) // had better be true
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"    Set relation {rel.a.Name(false)} x {rel.b.Name(false)} to {-rel.attitude}");
                                    __instance.AdjustAttitude(rel, -rel.attitude, true, false, info, raiseEvents, true, fromCommonEnemy);
                                }
                                rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.b);
                                if (rel.isAlliance)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"    Set relation {rel.a.Name(false)} x {rel.b.Name(false)} to {-rel.attitude}");
                                    __instance.AdjustAttitude(rel, -rel.attitude, true, false, info, raiseEvents, true, fromCommonEnemy);
                                }
                                break;
                            }
                        }
                    }

                    Melon<TweaksAndFixes>.Logger.Msg($"  All other allies declare war");
                    // All other allies declare war
                    foreach (var p in __state.alliesA)
                    {
                        var rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.b);
                        if (!rel.isWar)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"    Set relation {rel.a.Name(false)} x {rel.b.Name(false)} to War");
                            __instance.AdjustAttitude(rel, -200f, true, false, info, raiseEvents, true, fromCommonEnemy);
                        }
                    }
                    foreach (var p in __state.alliesB)
                    {
                        var rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.a);
                        if (!rel.isWar)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"    Set relation {rel.a.Name(false)} x {rel.b.Name(false)} to War");
                            __instance.AdjustAttitude(rel, -200f, true, false, info, raiseEvents, true, fromCommonEnemy);
                        }
                    }

                    Melon<TweaksAndFixes>.Logger.Msg($"  Allies declare war on each other");
                    // Allies declare war on each other
                    for (int i = __state.alliesA.Count - 1; i > 0; i--)
                    {
                        Player a = __state.alliesA[i];
                        for (int j = __state.alliesB.Count - 1; j > 0; j--)
                        {
                            Player b = __state.alliesB[j];
                            var rel = RelationExt.Between(__instance.CampaignData.Relations, a, b);
                            if (!rel.isWar)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    Set relation {rel.a.Name(false)} x {rel.b.Name(false)} to War");
                                __instance.AdjustAttitude(rel, -200f, true, false, info, raiseEvents, true, fromCommonEnemy);
                            }
                        }
                    }
                }
            }

            _PassThroughAdjustAttitude = false;
        }

        [HarmonyPatch(nameof(CampaignController.BuildNewShips))]
        [HarmonyPrefix]
        internal static bool Prefix_BuildNewShips(CampaignController __instance, Player player, float tempPlayerCash, out AiBuildTrace __state)
        {
            __state = CaptureAiBuildTrace(__instance, player, tempPlayerCash);

            if (_AiManageFleet == null || !_AiManageFleet.prewarming)
                return true;

            if (!Patch_Ship.ShouldUseBlankSlateCampaignStart())
                return true;

            _SkippedPrewarmBuildNewShipsCount++;
            if (_SkippedPrewarmBuildNewShipsCount <= 12 || _SkippedPrewarmBuildNewShipsCount % 25 == 0)
            {
                string playerName = player == null ? "?" : player.Name(false);
                Melon<TweaksAndFixes>.Logger.Msg($"Skipping prewarm BuildNewShips for {playerName} ({_SkippedPrewarmBuildNewShipsCount} skipped).");
            }

            LogAiBuildTrace(__instance, player, tempPlayerCash, __state, true);
            __state.Enabled = false;
            return false;
        }

        internal static bool ShouldSkipPrestartRandomDesigns(bool prewarming)
        {
            return prewarming && Patch_Ship.ShouldSkipCampaignPrestartCreateRandom();
        }

        internal static void LogSkippedPrestartRandomDesigns(Player player)
        {
            _SkippedPrestartRandomDesignsCount++;
            if (_SkippedPrestartRandomDesignsCount > 12 && _SkippedPrestartRandomDesignsCount % 25 != 0)
                return;

            string playerName = player == null ? "?" : player.Name(false);
            int year = CampaignController.Instance?.CurrentDate.AsDate().Year ?? 0;
            Melon<TweaksAndFixes>.Logger.Msg($"Skipping pre-start GenerateRandomDesigns for {playerName}, year={year} ({_SkippedPrestartRandomDesignsCount} skipped).");
        }

        internal static void LogSkippedServiceOwnedRandomDesigns(Player player)
        {
            _SkippedServiceOwnedRandomDesignsCount++;
            if (_SkippedServiceOwnedRandomDesignsCount > 12 && _SkippedServiceOwnedRandomDesignsCount % 25 != 0)
                return;

            string playerName = player == null ? "?" : player.Name(false);
            int year = CampaignController.Instance?.CurrentDate.AsDate().Year ?? 0;
            Melon<TweaksAndFixes>.Logger.Msg($"Skipping vanilla GenerateRandomDesigns for {playerName}, year={year}; AI design service owns generation ({_SkippedServiceOwnedRandomDesignsCount} skipped).");
        }
        // 
        [HarmonyPatch(nameof(CampaignController.BuildNewShips))]
        [HarmonyPostfix]
        internal static void Postfix_BuildNewShips(CampaignController __instance, Player player, float tempPlayerCash, AiBuildTrace __state)
        {
            LogAiBuildTrace(__instance, player, tempPlayerCash, __state, false);
        }

        [HarmonyPatch(nameof(CampaignController.UpdateAllShipsWeightCost))]
        [HarmonyPrefix]
        internal static void Prefix_UpdateAllShipsWeightCost(bool force, out CampaignLoadMethodTimingFrame __state)
        {
            __state = BeginCampaignLoadMethodTiming(nameof(CampaignController.UpdateAllShipsWeightCost), $"force={force}");
            EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(CampaignController.UpdateAllShipsWeightCost))]
        [HarmonyPostfix]
        internal static void Postfix_UpdateAllShipsWeightCost(CampaignLoadMethodTimingFrame __state)
        {
            EndCampaignLoadMethodTiming(__state);
        }


        [HarmonyPatch(nameof(CampaignController.ScrapOldAiShips))]
        [HarmonyPrefix]
        internal static bool Prefix_ScrapOldAiShips(CampaignController __instance, Player player)
        {
            if (Config.ScrappingChange && player.isMajor)
            {
                CampaignControllerM.HandleScrapping(__instance, player, _AiManageFleet != null && _AiManageFleet.prewarming);
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(CampaignController.UpdateNavmeshPassableAreas))]
        [HarmonyPrefix]
        internal static void Prefix_UpdateNavmeshPassableAreas(out CampaignLoadMethodTimingFrame __state)
        {
            __state = BeginCampaignLoadMethodTiming(nameof(CampaignController.UpdateNavmeshPassableAreas));
            EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(CampaignController.UpdateNavmeshPassableAreas))]
        [HarmonyPostfix]
        internal static void Postfix_UpdateNavmeshPassableAreas(CampaignLoadMethodTimingFrame __state)
        {
            EndCampaignLoadMethodTiming(__state);
        }

        [HarmonyPatch(nameof(CampaignController.CheckPredefinedDesigns))]
        [HarmonyPrefix]
        internal static void Prefix_CheckPredefinedDesigns(CampaignController __instance, bool prewarm, out CampaignLoadMethodTimingFrame __state)
        {
            __state = BeginCampaignLoadMethodTiming(
                nameof(CampaignController.CheckPredefinedDesigns),
                DescribePredefinedDesignTiming(__instance, prewarm));

            if (__instance._currentDesigns == null || (PredefinedDesignsData.NeedLoadRestrictive(prewarm) && !PredefinedDesignsData.Instance.LastLoadWasRestrictive))
            {
                if (!PredefinedDesignsData.Instance.LoadPredefSets(prewarm))
                {
                    Melon<TweaksAndFixes>.Logger.BigError("Tried to load predefined designs but failed! YOUR CAMPAIGN WILL NOT WORK.");
                    __state.Details = DescribePredefinedDesignTiming(__instance, prewarm);
                    EndCampaignLoadMethodPrefix(ref __state);
                    return;
                }
            }

            if (Config.DontClobberTechForPredefs)
            {
                // We need to force the game not to clobber techs.
                // We do this by claiming we've already clobbered up to this year.
                int startYear;
                int year;
                if (prewarm)
                    startYear = __instance.StartYear;
                else
                    startYear = __instance.CurrentDate.AsDate().Year;
                __instance._currentDesigns.GetNearestYear(startYear, out year);
                __instance.initedForYear = year;
            }

            __state.Details = DescribePredefinedDesignTiming(__instance, prewarm);
            EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(CampaignController.CheckPredefinedDesigns))]
        [HarmonyPostfix]
        internal static void Postfix_CheckPredefinedDesigns(CampaignLoadMethodTimingFrame __state)
        {
            EndCampaignLoadMethodTiming(__state);
        }

        [HarmonyPatch(nameof(CampaignController.OnLoadingScreenHide))]
        [HarmonyPrefix]
        internal static void Prefix_OnLoadingScreenHide(out CampaignLoadMethodTimingFrame __state)
        {
            __state = BeginCampaignLoadMethodTiming(nameof(CampaignController.OnLoadingScreenHide));
            EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(CampaignController.OnLoadingScreenHide))]
        [HarmonyPostfix]
        internal static void Postfix_OnLoadingScreenHideTiming(CampaignLoadMethodTimingFrame __state)
        {
            EndCampaignLoadMethodTiming(__state);
        }

        [HarmonyPatch(nameof(CampaignController.OnLoadingScreenHide))]
        [HarmonyPostfix]
        internal static void Postfix_OnLoadingScreenHide()
        {
            if (!GameManager.IsMainMenu)
                return;

            PredefinedDesignsData.AddUIforBSG();
        }
    }

    [HarmonyPatch(typeof(CampaignController._AiManageFleet_d__201))]
    internal class Patch_AiManageFleet
    {
        [HarmonyPatch(nameof(CampaignController._AiManageFleet_d__201.MoveNext))]
        [HarmonyPrefix]
        internal static void Prefix_MoveNext(CampaignController._AiManageFleet_d__201 __instance)
        {
            Patch_CampaignController._AiManageFleet = __instance;
        }

        [HarmonyPatch(nameof(CampaignController._AiManageFleet_d__201.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(CampaignController._AiManageFleet_d__201 __instance)
        {
            Patch_CampaignController._AiManageFleet = null;
        }
    }

    [RegisterTypeInIl2Cpp]
    public class AiDesignCoroutineHost : MonoBehaviour
    {
        public static AiDesignCoroutineHost? Instance;

        public AiDesignCoroutineHost(IntPtr ptr) : base(ptr) { }

        public static AiDesignCoroutineHost GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            GameObject hostObject = new("TAF AI Design Coroutine Host");
            UnityEngine.Object.DontDestroyOnLoad(hostObject);
            Instance = hostObject.AddComponent<AiDesignCoroutineHost>();
            return Instance;
        }

        private void Awake()
        {
            Instance = this;
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }

        public Coroutine StartDesignCoroutine(Il2CppSystem.Collections.IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }

    [HarmonyPatch(typeof(CampaignController._GenerateRandomDesigns_d__202))]
    internal class Patch_GenerateRandomDesigns
    {
        [HarmonyPatch(nameof(CampaignController._GenerateRandomDesigns_d__202.MoveNext))]
        [HarmonyPrefix]
        internal static bool Prefix_MoveNext(CampaignController._GenerateRandomDesigns_d__202 __instance, ref bool __result)
        {
            if (__instance.__1__state != 0)
                return true;

            if (Patch_CampaignController.TryBindAiDesignServiceRoutine(__instance) ||
                Patch_CampaignController.IsAiDesignServiceRoutine(__instance) ||
                Patch_CampaignController._AiDesignServiceRunningGenerateRandomDesigns)
            {
                Patch_CampaignController.BeginGenerateRandomDesignTrace(__instance);
                return true;
            }

            if (Patch_CampaignController.ShouldSkipPrestartRandomDesigns(__instance.prewarming))
            {
                Patch_CampaignController.LogSkippedPrestartRandomDesigns(__instance.player);
                __instance.__1__state = -2;
                __result = false;
                return false;
            }

            if (!Patch_CampaignController.ShouldSkipServiceOwnedRandomDesigns())
            {
                Patch_CampaignController.BeginGenerateRandomDesignTrace(__instance);
                return true;
            }

            Patch_CampaignController.LogSkippedServiceOwnedRandomDesigns(__instance.player);
            __instance.__1__state = -2;
            __result = false;
            return false;
        }

        [HarmonyPatch(nameof(CampaignController._GenerateRandomDesigns_d__202.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(CampaignController._GenerateRandomDesigns_d__202 __instance, bool __result)
        {
            Patch_CampaignController.EndGenerateRandomDesignTrace(__instance, __result);
            if (!__result)
                Patch_CampaignController.CompleteAiDesignServiceJob(__instance);
        }
    }
}
