using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using static Il2Cpp.Ui.SkirmishSetup;

namespace TweaksAndFixes.Harmony
{

    [HarmonyPatch(typeof(BatchShipGenerator))]
    internal class Patch_BatchShipGenerator
    {
        public class ShipGenEntry
        {
            public string player;
            public string type;
            public int year;
            public int count;
            public int tries;
            public float time;

            public ShipGenEntry(string player, string type, int year, int tries, float time, int count = 1)
            {
                this.player = player;
                this.type = type;
                this.year = year;
                this.count = count;
                this.tries = tries;
                this.time = time;
            }
        }

        private static readonly List<ShipGenEntry> failed = new();
        private static readonly List<ShipGenEntry> succeeded = new();
        private static readonly HashSet<string> checkedStrings = new();
        private static ShipGenEntry retryEntry;

        private static int designsRequested = -1;
        private static int designsCompleted = 0;
        private static int designsAttempted = 0;
        private static int batchCount = 1;

        private static readonly Stopwatch totalTime = new();
        private static readonly Stopwatch batchTime = new();

        private static string lastDisplayText = "";

        // Format: 00h 00m 00s
        private static readonly string timeFormat = "hh'h 'mm'm 'ss's'";

        private static bool stopOnBatchEnd = false;
        private static bool stopImmediate = false;
        private static bool isStopping = false;

        private static void AddEntry(List<ShipGenEntry> list, string player, string type, int year, int count, int tries, float time)
        {
            bool found = false;

            foreach (var entry in list)
            {
                if (entry.player != player) continue;
                if (entry.type != type) continue;
                if (entry.year != year) continue;

                found = true;

                entry.count += count;
                entry.time += time;
                entry.tries += tries;
            }

            if (!found)
            {
                list.Add(new ShipGenEntry(player, type, year, tries, time));
            }
        }

        private static ShipGenEntry? FindEntry(List<ShipGenEntry> list, ShipGenEntry key)
        {
            foreach (var entry in list)
            {
                if (entry.player != key.player) continue;
                if (entry.type != key.type) continue;
                if (entry.year != key.year) continue;

                return entry;
            }

            return null;
        }

        [HarmonyPatch(nameof(BatchShipGenerator.Start))]
        [HarmonyPostfix]
        internal static void Postfix_Start(BatchShipGenerator __instance)
        {
            __instance.startButton.onClick.AddListener(new System.Action(() => {
                if (!totalTime.IsRunning) totalTime.Start();
                if (!batchTime.IsRunning) batchTime.Start();

                if (designsRequested == -1)
                {
                    string nation = __instance.nationDropdown.options[__instance.nationDropdown.value].text;
                    string type = __instance.shipTypeDropdown.options[__instance.shipTypeDropdown.value].text;
                    int yearCount = 0;

                    foreach (var year in __instance.yearsSelected)
                    {
                        if (year.isOn) yearCount++;
                    }

                    if (yearCount == 0) yearCount = __instance.yearsSelected.Count;

                    int count = int.Parse(__instance.shipsAmount.text);

                    designsRequested =
                        (nation == "all" ? __instance.nationDropdown.options.Count : 1) *
                        (type == "all" ? __instance.shipTypeDropdown.options.Count : 1) *
                        yearCount * count;
                }
            }));
        }

        [HarmonyPatch(nameof(BatchShipGenerator.Update))]
        [HarmonyPrefix]
        internal static bool Prefix_Update(BatchShipGenerator __instance)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // if (Input.GetKey(KeyCode.LeftShift))
                // {
                //     stopImmediate = true;
                //     isStopping = true;
                // }
                // else
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"");
                    Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stoping_At_Batch_End")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"");

                    __instance.progress.text = __instance.progress.text.Replace(
                        $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stop_At_Batch_End")}\n",
                        $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stoping_At_Batch_End")}\n"
                    );
                    lastDisplayText = __instance.progress.text;

                    stopOnBatchEnd = true;
                }
            }

            if (__instance.gameObject.GetChild("CheckingSaves").active)
            {
                __instance.progress.gameObject.transform.localPosition = Vector3.zero;

                if (__instance.progress.text.StartsWith("DONE") || isStopping)
                {
                    foreach (var info in __instance.info)
                    {
                        if (!checkedStrings.Contains(info))
                        {
                            checkedStrings.Add(info);
                            designsCompleted++;
                        }
                    }

                    Melon<TweaksAndFixes>.Logger.Msg($"");
                    Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Batch", $"{batchCount}")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Batch_Time", batchTime.Elapsed.ToString(timeFormat))}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", totalTime.Elapsed.ToString(timeFormat))}");

                    foreach (var error in __instance.errors)
                    {
                        var split = error.Split(", ");

                        string player = split[0].Substring(8);
                        string type = split[1].Substring(6);
                        string yearStr = split[3].Substring(6);

                        if (!int.TryParse(yearStr, out int year))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"    Failed to parse `year` for failed ship gen: {error}");
                            continue;
                        }

                        string triesStr = split[4].Substring(7);

                        if (!int.TryParse(triesStr, out int tries))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"    Failed to parse `tries` for failed ship gen: {error}");
                            continue;
                        }

                        string timeStr = split[5].Substring(6, split[5].Length - 8);

                        if (!int.TryParse(timeStr, out int time))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"    Failed to parse `time` for failed ship gen: {error}");
                            continue;
                        }

                        float timeSec = (float)time / 1000f;

                        Melon<TweaksAndFixes>.Logger.Msg($"    Failure: {player,-15} | {type} | {year} | {tries,-2} | {timeSec}s");

                        designsAttempted += tries;

                        AddEntry(failed, player, type, year, 1, tries, timeSec);
                    }

                    __instance.errors.Clear();

                    foreach (var info in __instance.info)
                    {
                        var split = info.Split(", ");

                        string player = split[0].Substring(8);
                        string type = split[1].Substring(6);
                        string yearStr = split[3].Substring(6);

                        if (!int.TryParse(yearStr, out int year))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"    Failed to parse `year` for created ship gen: {info}");
                            continue;
                        }

                        string triesStr = split[4].Substring(7);

                        if (!int.TryParse(triesStr, out int tries))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"    Failed to parse `tries` for created ship gen: {info}");
                            continue;
                        }

                        string timeStr = split[5].Substring(6, split[5].Length - 8);

                        if (!int.TryParse(timeStr, out int time))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"    Failed to parse `time` for created ship gen: {info}");
                            continue;
                        }

                        float timeSec = (float)time / 1000f;

                        Melon<TweaksAndFixes>.Logger.Msg($"    Success: {player,-15} | {type} | {year} | {tries,-2} | {timeSec}s");

                        designsAttempted += tries;

                        AddEntry(succeeded, player, type, year, 1, tries, timeSec);
                    }

                    __instance.info.Clear();

                    if (retryEntry != null)
                    {
                        // Check if any of the failed entries succeeded
                        var found = FindEntry(succeeded, retryEntry);

                        if (found != null)
                        {
                            // If so, then add the try count and time to the success entry
                            found.tries += retryEntry.tries;
                            found.time += retryEntry.time;

                            // If no more counts remain, remove the fail entry.
                            if (retryEntry.count == 0)
                            {
                                failed.Remove(retryEntry);
                            }

                            // If it still has counts left, reset the failed entry try count and time to 0 for next batch
                            else 
                            {
                                retryEntry.tries = 0;
                                retryEntry.time = 0;
                            }
                        }
                    }

                    if (failed.Count > 0 && !stopOnBatchEnd && !isStopping)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"");

                        // Fetch next batch & relocate selected failiure to end of list
                        retryEntry = failed.First();
                        failed.Remove(retryEntry);
                        failed.Add(retryEntry);
                        
                        // Configuire the BSG variables
                        List<int> yearList = new();
                        yearList.Add(retryEntry.year);
                        ConfigureBSG(__instance, retryEntry.player, retryEntry.type, yearList, retryEntry.count);

                        // Set selected failiure count to 0
                        retryEntry.count = 0;

                        // Restart the process
                        batchCount++;
                        batchTime.Restart();
                        __instance.startButton.onClick.Invoke();
                    }
                    else
                    {
                        isStopping = false;

                        __instance.progress.text = 
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Finished")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Tries", $"{designsAttempted}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Batches", $"{batchCount}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", totalTime.Elapsed.ToString(timeFormat))}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_See_Log")}\n";

                        Melon<TweaksAndFixes>.Logger.Msg($"");
                        Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Finished")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Tries", $"{designsAttempted}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Batches", $"{batchCount}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", totalTime.Elapsed.ToString(timeFormat))}");
                        Melon<TweaksAndFixes>.Logger.Msg($"");

                        if (failed.Count > 0)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Failed")}:");

                            Dictionary<string, Dictionary<string, Dictionary<int, ShipGenEntry>>> failedMap = new();

                            foreach (var failure in failed)
                            {
                                failedMap.ValueOrNew(failure.player).ValueOrNew(failure.type).Add(failure.year, failure);
                            }

                            foreach (var player in failedMap)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    {player.Key}:");
                                foreach (var type in player.Value)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"      {type.Key}:");
                                    foreach (var year in type.Value)
                                    {
                                        Melon<TweaksAndFixes>.Logger.Msg($"        {year.Key}: " +
                                            ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Count_Tries_Time",
                                                $"{year.Value.count,-2}",
                                                $"{year.Value.tries,-3}",
                                                $"{TimeSpan.FromSeconds(year.Value.time).ToString(timeFormat)}")
                                        );
                                    }
                                }
                            }
                        }

                        Melon<TweaksAndFixes>.Logger.Msg($"");

                        if (succeeded.Count > 0)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Succeeded")}:");

                            Dictionary<string, Dictionary<string, Dictionary<int, ShipGenEntry>>> succeededMap = new();

                            foreach (var success in succeeded)
                            {
                                succeededMap.ValueOrNew(success.player).ValueOrNew(success.type).Add(success.year, success);
                            }

                            foreach (var player in succeededMap)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    {player.Key}:");
                                foreach (var type in player.Value)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"      {type.Key}:");
                                    foreach (var year in type.Value)
                                    {
                                        Melon<TweaksAndFixes>.Logger.Msg($"        {year.Key}: " +
                                            ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Count_Tries_Time",
                                                $"{year.Value.count,-2}",
                                                $"{year.Value.tries,-3}",
                                                $"{TimeSpan.FromSeconds(year.Value.time).ToString(timeFormat)}")
                                        );
                                    }
                                }
                            }
                        }

                        lastDisplayText = __instance.progress.text;
                    }
                }
            }

            if (__instance.progress.text != lastDisplayText)
            {
                foreach (var info in __instance.info)
                {
                    if (!checkedStrings.Contains(info))
                    {
                        checkedStrings.Add(info);
                        designsCompleted++;
                    }
                }

                string extraText =
                    $"\n\n{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Batch", $"{batchCount}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Tries", $"{designsAttempted}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", totalTime.Elapsed.ToString(timeFormat))}\n\n";

                if (!stopOnBatchEnd)
                {
                    extraText += $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stop_At_Batch_End")}\n";
                }
                else
                {
                    extraText += $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stoping_At_Batch_End")}\n";
                }

                // TODO: Implement sooner rather than later
                // extraText += $"Press [Shift + ESC] to stop the generator immediately.\n";

                // TODO: Implement this at some point :P
                // extraText += $"Press [Tab] to toggle overlay visibility.\n";

                var lastNewlineIndex = __instance.progress.text.LastIndexOf("\n");

                if (lastNewlineIndex == -1) lastNewlineIndex = __instance.progress.text.Length;

                __instance.progress.text = __instance.progress.text.Substring(0, lastNewlineIndex) + extraText;

                lastDisplayText = __instance.progress.text;
            }

            return !stopImmediate;
        }

        public static void ConfigureBSG(BatchShipGenerator bsg, string nation, string type, List<int> years, int count)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Configuring Batch Ship Generator: {nation} | {type} | {String.Join(", ", years.ToArray())} | {count}");

            foreach (var child in bsg.yearToggleParent.GetChildren())
            {
                string text = child.GetChild("Label").GetComponent<TextMeshProUGUI>().text;

                if (text == "Fullscreen Mode") continue;

                var toggle = child.GetComponent<Toggle>();

                toggle.Set(false);

                if (!int.TryParse(text, out int year))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `{text}`");
                    continue;
                }

                if (years.Count == 0 || !years.Contains(year)) continue;

                toggle.Set(true);
            }

            bool found = false;

            foreach (var option in bsg.nationDropdown.options)
            {
                if (option.text == nation)
                {
                    bsg.nationDropdown.SetValue(bsg.nationDropdown.options.IndexOf(option));
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Melon<TweaksAndFixes>.Logger.Error($"BatchShipGeneratorConfigData Error: `nation` value must be a valid value `{nation}`");
            }

            found = false;

            foreach (var option in bsg.shipTypeDropdown.options)
            {
                if (option.text == type)
                {
                    bsg.shipTypeDropdown.SetValue(bsg.shipTypeDropdown.options.IndexOf(option));
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Melon<TweaksAndFixes>.Logger.Error($"BatchShipGeneratorConfigData Error: `ship_type` value must be a valid value `{type}`");
            }

            bsg.shipsAmount.SetText($"{count}");
        }
    }
}
