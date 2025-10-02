using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using UnityEngine.UI;
using Il2CppCoffee.UIExtensions;
using Il2CppTMPro;
using System.Collections;
using static Il2Cpp.CampaignController;
using Il2CppLitJson;

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

        [HarmonyPatch(nameof(CampaignController.CheckMinorNationThreat))]
        [HarmonyPrefix]
        internal static bool Prefix_CheckMinorNationThreat()
        {
            // Check params
            if (Config.Param("taf_disable_minor_and_medium_nation_land_invasions", 1) == 1)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Skipped attempt to generate minor nation revolt.");
                return false;
            }
            return true;
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

        [HarmonyPatch(nameof(CampaignController.OnNewTurn))]
        [HarmonyPrefix]
        internal static void Prefix_OnNewTurn(CampaignController __instance)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"OnNewTurn"); // <<< Trigger on start of new turn
            Patch_Player.ResetChangePlayerGDP();
        }

        // [HarmonyPatch(nameof(CampaignController.NextTurn))]
        // [HarmonyPrefix]
        // internal static void Prefix_NextTurn(CampaignController __instance)
        // {
        //     Melon<TweaksAndFixes>.Logger.Msg($"NextTurn"); // <<< Trigger on hit new turn button
        // }

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

            Melon<TweaksAndFixes>.Logger.Msg($"  Checking on {__instance.CurrentDate.turn}");

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
                queue.Text = String.Format(LocalizeManager.Localize("$TAF_Ui_Retirement_Body"), __instance.CurrentDate.AsDate().Year - 1890, retirementPromptFrequency);
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

        [HarmonyPatch(nameof(CampaignController.Init))]
        [HarmonyPrefix]
        internal static void Prefix_Init(ref int campaignDesignsUsage)
        {
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

                    // First, find overlapping allies. They break
                    // both alliances.
                    for (int i = __state.alliesA.Count; i-- > 0;)
                    {
                        Player p = __state.alliesA[i];
                        for (int j = __state.alliesB.Count; j-- > 0;)
                        {
                            if (__state.alliesB[j] == p)
                            {
                                __state.alliesA.RemoveAt(i);
                                __state.alliesB.RemoveAt(j);
                                var rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.a);
                                if (rel.isAlliance) // had better be true
                                    __instance.AdjustAttitude(rel, -rel.attitude, true, false, info, raiseEvents, true, fromCommonEnemy);
                                rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.b);
                                if (rel.isAlliance)
                                    __instance.AdjustAttitude(rel, -rel.attitude, true, false, info, raiseEvents, true, fromCommonEnemy);
                                break;
                            }
                        }
                    }

                    // All other allies declare war
                    foreach (var p in __state.alliesA)
                    {
                        var rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.b);
                        if (!rel.isWar)
                            __instance.AdjustAttitude(rel, -200f, true, false, info, raiseEvents, true, fromCommonEnemy);
                    }
                    foreach (var p in __state.alliesB)
                    {
                        var rel = RelationExt.Between(__instance.CampaignData.Relations, p, relation.a);
                        if (!rel.isWar)
                            __instance.AdjustAttitude(rel, -200f, true, false, info, raiseEvents, true, fromCommonEnemy);
                    }
                    // Allies declare war on each other
                    for (int i = __state.alliesA.Count; i-- > 0;)
                    {
                        Player a = __state.alliesA[i];
                        for (int j = __state.alliesB.Count; j-- > 0;)
                        {
                            Player b = __state.alliesB[i];
                            var rel = RelationExt.Between(__instance.CampaignData.Relations, a, b);
                            if (!rel.isWar)
                                __instance.AdjustAttitude(rel, -200f, true, false, info, raiseEvents, true, fromCommonEnemy);
                        }
                    }
                }
            }

            _PassThroughAdjustAttitude = false;
        }

        // [HarmonyPatch(nameof(CampaignController.BuildNewShips))]
        // [HarmonyPrefix]
        // internal static bool Prefix_BuildNewShips(CampaignController __instance, Player player, float tempPlayerCash)
        // {
        //     var shiplist = new Il2CppSystem.Collections.Generic.List<Ship>(player.fleet);
        //     
        //     Melon<TweaksAndFixes>.Logger.Msg($"Pre-Build {player.Name(false)}: {shiplist.Count}");
        // 
        //     foreach (var ship in shiplist)
        //     {
        //         Melon<TweaksAndFixes>.Logger.Msg($"  {ship.Name(false, false)}");
        //     }
        // 
        //     return true;
        // }
        // 
        // [HarmonyPatch(nameof(CampaignController.BuildNewShips))]
        // [HarmonyPostfix]
        // internal static void Postfix_BuildNewShips(CampaignController __instance, Player player, float tempPlayerCash)
        // {
        //     CampaignControllerM.HandleScrapping(__instance, player, _AiManageFleet != null && _AiManageFleet.prewarming//);
        //
        //     var shiplist = new Il2CppSystem.Collections.Generic.List<Ship>(player.fleet);
        //     
        //     Melon<TweaksAndFixes>.Logger.Msg($"Post-Build: {player.Name(false)}: {shiplist.Count}");
        //     
        //     foreach (var ship in shiplist)
        //     {
        //         Melon<TweaksAndFixes>.Logger.Msg($"  {ship.Name(false, false)}");
        //     }
        // }


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
}
