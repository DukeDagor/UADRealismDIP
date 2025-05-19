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
            if (Config.USER_CONFIG.Fleet_Tension.Disable)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Skipping tension check...");
                return false;
            }
            return true;
        }


        [HarmonyPatch(nameof(CampaignController.FinishCampaign))]
        [HarmonyPrefix]
        internal static bool Prefix_FinishCampaign(CampaignController __instance, Player loser, FinishCampaignType finishType)
        {
            // Ignore all other campaign ending types
            if (finishType != FinishCampaignType.Retirement)
            {
                return true;
            }

            // If the year is less than the deisred retirement year, block the function
            if (__instance.CurrentDate.AsDate().Year < Config.USER_CONFIG.Campagin_End_Date.Campaign_End_Date)
            {
                return false;
            }

            // If the year is equal or greter than the desired retirement date, let it run
            return true;
        }

        [HarmonyPatch(nameof(CampaignController.CheckForCampaignEnd))]
        [HarmonyPostfix]
        internal static void Postfix_CheckForCampaignEnd(CampaignController __instance)
        {
            // If the year is equal or greter than the desired retirement date force game end
            if (__instance.CurrentDate.AsDate().Year >= Config.USER_CONFIG.Campagin_End_Date.Campaign_End_Date)
            {
                // Check for month interval
                int monthsSinceFirstRequest = __instance.CurrentDate.AsDate().Month + (__instance.CurrentDate.AsDate().Year - 1890) * 12;

                if (Config.USER_CONFIG.Campagin_End_Date.Prompt_Player_About_Retirement_Every_X_Months != 0 && monthsSinceFirstRequest % Config.USER_CONFIG.Campagin_End_Date.Prompt_Player_About_Retirement_Every_X_Months != 0)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg("Skipping retirement request.");
                    return;
                }

                Player MainPlayer = ExtraGameData.MainPlayer();

                // sanity check
                if (MainPlayer == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error("Could not find MainPlayer in [CheckForCampaignEnd]. Default behavior will be used.");
                    return;
                }

                MessageBoxUI.MessageBoxQueue queue = new MessageBoxUI.MessageBoxQueue();
                queue.Header = "Considering Retirement";
                queue.Text = "After " + (__instance.CurrentDate.AsDate().Year - 1890) + " long years of service to your country, perhaps its time to step down.\nWould you like to end the campaign here? If you decline, you will be asked again in " + Config.USER_CONFIG.Campagin_End_Date.Prompt_Player_About_Retirement_Every_X_Months + " months.";
                queue.Ok = "Yes";
                queue.Cancel = "No";
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
