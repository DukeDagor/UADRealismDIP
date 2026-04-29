using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Linq;
using UnityEngine;
using static Il2Cpp.CampaignController;

#pragma warning disable CS8602
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(CampaignController))]
    internal class Patch_CampaignController
    {
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

        private static bool IsAiBuildDebugEnabled()
            => Config.Param("taf_debug_ai_shipbuilding", 0) != 0;

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

        // 
        [HarmonyPatch(nameof(CampaignController.BuildNewShips))]
        [HarmonyPostfix]
        internal static void Postfix_BuildNewShips(CampaignController __instance, Player player, float tempPlayerCash, AiBuildTrace __state)
        {
            LogAiBuildTrace(__instance, player, tempPlayerCash, __state, false);
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

        [HarmonyPatch(nameof(CampaignController.CheckPredefinedDesigns))]
        [HarmonyPrefix]
        internal static void Prefix_CheckPredefinedDesigns(CampaignController __instance, bool prewarm)
        {
            if (__instance._currentDesigns == null || (PredefinedDesignsData.NeedLoadRestrictive(prewarm) && !PredefinedDesignsData.Instance.LastLoadWasRestrictive))
            {
                if (!PredefinedDesignsData.Instance.LoadPredefSets(prewarm))
                {
                    Melon<TweaksAndFixes>.Logger.BigError("Tried to load predefined designs but failed! YOUR CAMPAIGN WILL NOT WORK.");
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

    [HarmonyPatch(typeof(CampaignController._GenerateRandomDesigns_d__202))]
    internal class Patch_GenerateRandomDesigns
    {
        [HarmonyPatch(nameof(CampaignController._GenerateRandomDesigns_d__202.MoveNext))]
        [HarmonyPrefix]
        internal static bool Prefix_MoveNext(CampaignController._GenerateRandomDesigns_d__202 __instance, ref bool __result)
        {
            if (__instance.__1__state != 0)
                return true;

            if (Patch_CampaignController.ShouldSkipPrestartRandomDesigns(__instance.prewarming))
            {
                Patch_CampaignController.LogSkippedPrestartRandomDesigns(__instance.player);
                __instance.__1__state = -2;
                __result = false;
                return false;
            }

            return true;
        }
    }
}
