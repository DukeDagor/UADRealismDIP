using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Il2Cpp.CampaignController;
using static MelonLoader.MelonLogger;

namespace TweaksAndFixes.Modules
{
    internal class EventSystem
    {

        private static EventX? lastEvent = null;
        private static EventData? lastAnswer = null;
        private static float AnswerEventWealth = 0;

        // Prefix
        internal static void OnNewTurn(CampaignController __instance)
        {
            foreach (var player in CampaignController.Instance.CampaignData.PlayersMajor)
            {
                // naval_funds(min;max)
                QueueEvent(player, false, "naval_funds");

                // unrest(min;max)
                QueueEvent(player, false, "unrest");
            }
        }

        // Prefix
        public static void AnswerEvent(EventX ev, ref EventData answer)
        {
            lastEvent = ev;
            lastAnswer = answer;
            var cc = CampaignController.Instance;

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

            // trigger(name:chance;name:chance;...)
            if (answer.paramx.ContainsKey("trigger"))
            {
                Dictionary<string, int> triggerMap = new();
                int totalProbability = 0;
                string[] split;

                foreach (var trigger in answer.paramx["trigger"])
                {
                    // Has parcentage
                    if (trigger.Contains(':'))
                    {
                        split = trigger.Split(':');
                        int probability = 0;

                        if (!ModUtils.TryParse(split[1], out probability) || probability < 1 || probability > 100)
                        {
                            Melon<TweaksAndFixes>.Logger.Error(
                                $"Event param 'trigger' chance must be a valid number between 1 and 100!" +
                                $" For event '{answer.name}' chance '{split[1]}' is not a valid number!"
                            );

                            continue;
                        }

                        totalProbability += probability;
                        triggerMap.Add(split[0], probability);
                    }

                    // Instant trigger (100%)
                    else
                    {
                        if (answer.paramx["trigger"].Count > 1)
                        {
                            Melon<TweaksAndFixes>.Logger.Error(
                                $"Event param 'trigger' can only have multiple triggers when they all have a probability." +
                                $" E.X. trigger(name:chance;name:chance;...)"
                            );
                        }

                        QueueEvent(ev.player, true, trigger);

                        break;
                    }
                }

                if (totalProbability > 100)
                {
                    Melon<TweaksAndFixes>.Logger.Error(
                        $"Event param 'trigger' total chance must be between 1 and 100!" +
                        $" For event '{answer.name}' total = '{totalProbability}'"
                    );
                }
                else if (triggerMap.Count > 0)
                {
                    int random = ModUtils.toInt(Random.Shared.NextDouble() * 100.5);
                    string target;

                    do
                    {
                        var trigger = triggerMap.First();
                        target = trigger.Key;
                        triggerMap.Remove(target);
                        random -= trigger.Value;
                    }
                    while (random > 0 && triggerMap.Count > 0);

                    QueueEvent(ev.player, true, target);
                }
            }

            // war(start_war/end_war/negotiate_peace/end_player_wars/end_all_wars)
            if (answer.paramx.ContainsKey("war"))
            {
                switch (answer.paramx["war"][0])
                {
                    case "end_player_wars":
                        foreach (var member in new Il2CppSystem.Collections.Generic.List<Player>(ev.player.AtWarWith()))
                        {
                            var rel = RelationExt.Between(cc.CampaignData.Relations, ev.player, member);
                            CampaignController.Instance.AdjustAttitude(rel, 100, true, false, null, true);
                        }
                        break;

                    case "end_all_wars":
                        foreach (var player in cc.CampaignData.PlayersMajor)
                        {
                            foreach (var member in new Il2CppSystem.Collections.Generic.List<Player>(player.AtWarWith()))
                            {
                                var rel = RelationExt.Between(cc.CampaignData.Relations, player, member);
                                CampaignController.Instance.AdjustAttitude(rel, 100, true, false, null, true);
                            }
                        }
                        break;
                }
            }

            // alliance(admit_member/remove_member/leave/disolve/disolve_all)
            if (answer.paramx.ContainsKey("alliance"))
            {
                switch (answer.paramx["alliance"][0])
                {
                    case "leave":
                        foreach (var member in new Il2CppSystem.Collections.Generic.List<Player>(ev.player.InAllianceWith()))
                        {
                            var rel = RelationExt.Between(cc.CampaignData.Relations, ev.player, member);
                            CampaignController.Instance.AdjustAttitude(rel, -100, true, false, null, true);
                        }
                        break;

                    case "disolve_all":
                        foreach (var player in cc.CampaignData.PlayersMajor)
                        {
                            foreach (var member in new Il2CppSystem.Collections.Generic.List<Player>(player.InAllianceWith()))
                            {
                                var rel = RelationExt.Between(cc.CampaignData.Relations, player, member);
                                CampaignController.Instance.AdjustAttitude(rel, -100, true, false, null, true);
                            }
                        }
                        break;
                }
            }

            // end_campaign(type)
            if (answer.paramx.ContainsKey("end_campaign"))
            {
                switch (answer.paramx["end_campaign"][0])
                {
                    case "HighUnrest":
                        CampaignController.Instance.FinishCampaign(ev.player, FinishCampaignType.HighUnrest);
                        break;

                    case "PeaceSigned":
                        CampaignController.Instance.FinishCampaign(ev.player, FinishCampaignType.PeaceSigned);
                        break;

                    case "LoseEvent":
                        CampaignController.Instance.FinishCampaign(ev.player, FinishCampaignType.LoseEvent);
                        break;

                    case "Retirement":
                        CampaignController.Instance.FinishCampaign(ev.player, FinishCampaignType.Retirement);
                        break;

                    case "TotalDefeat":
                        CampaignController.Instance.FinishCampaign(ev.player, FinishCampaignType.TotalDefeat);
                        break;

                    case "TotalBankrupt":
                        CampaignController.Instance.FinishCampaign(ev.player, FinishCampaignType.TotalBankrupt);
                        break;

                    default:
                        Melon<TweaksAndFixes>.Logger.Error(
                            $"Event param 'end_campaign' can only have a param with the following values (case sensitive):\n" +
                            $"  HighUnrest, PeaceSigned, LoseEvent, Retirement, TotalDefeat, TotalBankrupt"
                        );
                        break;
                }
            }

            // change_government(revolution/type)
            if (answer.paramx.ContainsKey("change_government"))
            {
                // TODO: Turn into a function & add forced govt type change

                Player player = ev.player;

                string evTitle = string.Empty;
                string evBody = string.Empty;
                string electionsTextHeader = string.Empty;
                string electionsTextData = string.Empty;
                Player? loser = null;

                cc.ChangePartyPercentage(player);
                cc.Elections(player, out electionsTextHeader, out electionsTextData, true);

                if (player.isMain)
                {
                    evTitle = ModUtils.LocalizeF("$Ui_World_NationalRevolution");
                }
                else
                {
                    evTitle = ModUtils.LocalizeF("$Ui_World_National0Revolution", player.Name());
                }

                // If naval prestige is below 25 and there's a revolution, then the player get's the boot.
                if (player.reputation <= Config.Param("naval_prestige_vs_unrest_threshold", 25f))
                {
                    // Already having a revolution
                    if (player.revolution)
                    {
                        return;
                    }

                    loser = player;

                    // Player loses
                    if (player.isMain)
                    {
                        evBody = ModUtils.LocalizeF("Ui_World_UnrestBecameUncontrollablyHighCountry");
                    }

                    // Ai loses
                    else
                    {
                        evBody = ModUtils.LocalizeF("Ui_World_UnrestBecame0PeopleTrust1Country", player.Name(), player.GetAiName());
                        player.revolution = true;
                    }
                }

                // Naval prestige was sufficient
                else
                {
                    // Skip if we had a revolution recently
                    if (player.govermentChangedPenalty > 0.0f)
                        return;

                    // Main player just gets a slap on the wrist
                    if (player.isMain)
                    {
                        evBody = ModUtils.LocalizeF("Ui_World_UnrestBecameYouRemainYourPosAsAdmiral");
                    }

                    // Replace AI admiral
                    else
                    {
                        string currAiName = player.GetAiName();
                        player.ChangeAiName();
                        string newAiName = player.GetAiName();
                        evBody = ModUtils.LocalizeF(
                            "Ui_World_DueRecentNegative0Admiralty1Duty2Management", player.Name(), currAiName, newAiName
                        );
                    }

                    player.govermentChangedPenalty = Config.Param("goverment_changed_naval_funds_penalty", 0.5f);
                }

                MessageBoxUI.Show(
                    evTitle, evBody,
                    null, true, ModUtils.LocalizeF("Ui_Popup_Generic_Ok"), null,
                    new System.Action(() => {
                        if (loser == null) return;

                        if (loser.isAi)
                        {
                            G.ui.ReportElections(loser, electionsTextData);
                        }
                        else
                        {
                            MessageBoxUI.Show(
                                electionsTextHeader, electionsTextData,
                                null, true, ModUtils.LocalizeF("Ui_Popup_Generic_Ok"), null,
                            new System.Action(() => {
                                cc.FinishCampaign(player, FinishCampaignType.HighUnrest);
                            })
                            );
                        }
                    })
                );
            }

            if (answer.paramx.ContainsKey("randomize")) { }

            AnswerEventWealth = answer.wealth;
            answer.wealth = 0;
        }

        /*
            Postfix:
            answer.wealth = AnswerEventWealth;
            AnswerEventWealth = 0;
        */

        public static bool QueueEvent(Player player, EventData data, bool immediate = false)
        {
            CampaignController cc = CampaignController.Instance;

            EventX e = new();
            e.data = data;
            e.player = player;
            e.seed = Util.FromTo(0, 1000000);
            e.active = true;
            e.date = cc.CurrentDate;

            if (!e.Init())
                return false;

            if (immediate)
            {
                cc.CampaignData.Events.Insert(0, e);
                cc.CampaignData.EventsByPlayer[player.data].Insert(0, e);
            }
            else
            {
                cc.CampaignData.Events.Add(e);
                cc.CampaignData.EventsByPlayer[player.data].Add(e);
            }

            // If it's going to be seen by the player, wait for the beginning of-turn parsing
            if (e.showEventToMainPlayer || !player.isAi)
            {
                if (immediate)
                {
                    // Either show right away, or put on the top of the queue
                    // Make sure to disable UI.RefreshEvents if the queue isn't empty
                }

                return true;
            }

            bool isSpecial = e.data.paramx.ContainsKey("war_special") || e.data.paramx.ContainsKey("alliance_special");

            // Parse non-player special events separately.
            if (isSpecial)
            {
                cc.DisplaySpecialEvents(player, data, e);
                var rel = RelationExt.Between(cc.CampaignData.Relations, PlayerController.Instance, player);
                rel.LastSpecialEventDate = cc.CurrentDate;
            }

            // If it's a major AI player and it doesn't affect the player, then answer it randomly.
            else if (player.isMain || !e.codeToPlayer.ContainsValue(PlayerController.Instance))
            {
                cc.AnswerEvent(e, e.data.answers.Random());
            }

            return true;
        }

        public static bool QueueEvent(Player player, bool immediate = false, params string[] tags)
        {
            List<EventData> options = new();

            bool atWar = player.IsAtWar();
            bool isSpecial = tags.Contains("war_special") || tags.Contains("alliance_special");

            foreach (var tag in tags)
            {
                foreach (var e in G.GameData.events)
                {
                    var paramx = e.Value.paramx;

                    // Skip answers
                    if (e.Value.type != "event") continue;

                    // If no special tags are included, skip special events
                    //   They require a target player to function
                    if (!isSpecial &&
                        (paramx.ContainsKey("war_special") || paramx.ContainsKey("alliance_special")))
                        continue;

                    // Check war/peace param
                    if (atWar && paramx.ContainsKey("peace")
                        || !atWar && paramx.ContainsKey("war"))
                        continue;

                    // Check player control
                    if (player.isAi && paramx.ContainsKey("player")
                        || !player.isAi && paramx.ContainsKey("ai"))
                        continue;

                    // Filter by tag / name
                    if (!paramx.ContainsKey(tag) && e.Value.name != tag) continue;

                    // Filter by last answer
                    //   If last answer is null, then ignore the param
                    if (paramx.ContainsKey("last_answer") && lastAnswer != null
                        && !paramx["last_answer"].Contains(lastAnswer.name))
                        continue;

                    // Filter by unrest
                    if (tag == "unrest")
                    {
                        if (paramx["unrest"].Count != 2
                            || !ModUtils.TryParse(paramx["unrest"][0], out float min)
                            || !ModUtils.TryParse(paramx["unrest"][1], out float max))
                        {
                            Melon<TweaksAndFixes>.Logger.Error(
                                $"Invalid event tag 'unrest'! Requires 3 valid numbers: 'unrest(min;max)'"
                            );
                            continue;
                        }

                        // If not in range, or if it fails the random chance skip it
                        if (player.unrest < min
                            || player.unrest > max
                            || (Random.Shared.NextDouble() * 100f) > e.Value.chance)
                            continue;
                    }

                    // Filter by unrest
                    if (tag == "naval_funds")
                    {
                        float min = float.MinValue, max = float.MaxValue;

                        if (paramx["naval_funds"].Count != 2
                            || (paramx["naval_funds"][0].Length != 0 && !ModUtils.TryParse(paramx["naval_funds"][0], out min))
                            || (paramx["naval_funds"][1].Length != 0 && !ModUtils.TryParse(paramx["naval_funds"][1], out max)))
                        {
                            Melon<TweaksAndFixes>.Logger.Error(
                                $"Invalid event tag 'naval_funds'! Requires 2 valid numbers or empty spaces: 'naval_funds(min;max)'"
                            );
                            continue;
                        }

                        if (MathF.Abs(min) > 720 || MathF.Abs(max) > 720)
                        {
                            Melon<TweaksAndFixes>.Logger.Error(
                                $"Invalid event tag 'naval_funds'! Min and Max are measured in months, not dollars." +
                                $" They are limited to 720 months (60 years) positive or negitive."
                            );
                            continue;
                        }

                        // If not in range, or if it fails the random chance skip it
                        if (player.cash < min * player.Budget()
                            || player.unrest > max * player.Budget()
                            || (Random.Shared.NextDouble() * 100f) > e.Value.chance)
                            continue;
                    }

                    options.Add(e.Value);
                }
            }

            if (options.Count == 0)
                return false;

            QueueEvent(player, options.Random(), immediate);

            return true;
        }

        // Prefix
        public void CheckForCampaignEnd()
        {
            var cc = CampaignController.Instance;

            var MainPlayer = PlayerController.Instance;

            // Retirement params
            int campaignEndDate = Config.Param("taf_campaign_end_retirement_date", 1950);
            int retirementPromptFrequency = Config.Param("taf_campaign_end_retirement_promt_every_x_months", 12);

            // Get for month interval
            int monthsSinceFirstRequest = cc.CurrentDate.AsDate().Month + (cc.CurrentDate.AsDate().Year - 1890) * 12;

            // If the year is equal or greater than the desired retirement date force game end
            if (cc.CurrentDate.AsDate().Year >= campaignEndDate)
            {
                if (retirementPromptFrequency == 0 || monthsSinceFirstRequest % retirementPromptFrequency == 0)
                {
                    string Header = LocalizeManager.Localize("$TAF_Ui_Retirement_Header");
                    string Text = String.Format(
                    LocalizeManager.Localize("$TAF_Ui_Retirement_Body"),
                        cc.CurrentDate.AsDate().Year - cc.StartYear,
                        retirementPromptFrequency
                    );

                    MessageBoxUI.Show(
                        Header,
                        Text,
                        null, false,
                        LocalizeManager.Localize("$Ui_Popup_Generic_Yes"), LocalizeManager.Localize("$Ui_Popup_Generic_No"),
                    new System.Action(() =>
                        {
                            cc.FinishCampaign(MainPlayer, FinishCampaignType.Retirement);
                        }), null, null, null, null, false
                    );
                }
                else
                {
                    int monthsTillReprompt = retirementPromptFrequency - (monthsSinceFirstRequest % retirementPromptFrequency);

                    Melon<TweaksAndFixes>.Logger.Msg($"  Will repromt retirement request in {monthsTillReprompt} months.");
                }
            }

            int activeCount = 0;
            int alliedToPlayerCount = 0;

            foreach (var player in cc.CampaignData.PlayersMajor)
            {
                if (player.isDisabled) continue;

                if (player.StateBudget() <= 0)
                {
                    // Economic collapse
                    if (player.isMain)
                    {
                        cc.FinishCampaign(player, FinishCampaignType.TotalBankrupt);
                        return;
                    }
                    else if (player.CanDissolve())
                    {
                        G.ui.ReportMessage(string.Empty,
                            ModUtils.LocalizeF("Ui_World_The0hasBeenDissolvedDueEconomicCollapse", player.Name())
                        );
                        cc.DisablePlayer(player);
                        continue;
                    }
                }

                if (player.provinces.Count == 0)
                {
                    // Total defeat
                    if (player.isMain)
                    {
                        cc.FinishCampaign(player, FinishCampaignType.TotalDefeat);
                        return;
                    }
                    else if (player.CanDissolve())
                    {
                        G.ui.ReportMessage(string.Empty,
                            ModUtils.LocalizeF("Ui_World_The0HasBeenDissolvedDueLosingAllPorts", player.Name())
                        );
                        cc.DisablePlayer(player);
                        continue;
                    }
                }

                else if (player.homeProvinces.Count == 0)
                {
                    player.shipbuildingCapacityPenalty = 0.5f;
                }

                if (player.isAi)
                {
                    activeCount++;

                    if (player.InAllianceWith().ToList().Contains(MainPlayer))
                        alliedToPlayerCount++;
                }
            }

            if (activeCount == 0)
            {
                // Total victory
                cc.FinishCampaign(MainPlayer, FinishCampaignType.PeaceSigned);
                return;
            }

            if (activeCount == alliedToPlayerCount)
            {
                // World peace
                if (!QueueEvent(MainPlayer, false, "world_peace"))
                {
                    cc.FinishCampaign(MainPlayer, FinishCampaignType.PeaceSigned);
                }
                return;
            }

            return;
        }

        // Prefix
        public static void OnUnrestWasChanged(Player player)
        {
            var cc = CampaignController.Instance;

            // If unrest is below 100 skip
            if (player.unrest < 100)
                return;

            // revolution
            if (!QueueEvent(player, true, "revolution"))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Failed to find event tagged with 'revolution'!");
            }

            return;
        }
    }
}
