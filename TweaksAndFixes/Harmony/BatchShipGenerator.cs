using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

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

        private static readonly Guid id = Guid.NewGuid();

        // Format: 00h 00m 00s
        private static readonly string timeFormat = "hh'h 'mm'm 'ss's'";

        // Input tracking
        private static List<string> nations = new();
        private static List<string> types = new();
        private static List<string> years = new();

        // Entry tracking
        private static readonly List<string> failedOrder = new();
        private static readonly Dictionary<string, ShipGenEntry> failed = new();
        private static readonly Dictionary<string, ShipGenEntry> succeeded = new();
        private static readonly HashSet<string> checkedStrings = new();
        private static ShipGenEntry retryEntry;

        // Count tracking
        private static int designsRequested = -1;
        private static int designsAttempted = 0;
        private static int designsCompleted = 0;
        private static int designsFailed = 0;
        private static int batchCount = 1;

        // Time tracking
        private static bool genStarted = false;
        private static DateTime totalTime = new(0);
        private static DateTime batchTime = new(0);

        // UI tracking
        private static string lastDisplayText = "";
        private static bool updateLog = true;

        // State machine
        private static bool stopOnBatchEnd = false;

        private static string EntryKey(ShipGenEntry entry)
        {
            return $"{entry.type}/{entry.player}/{entry.year}";
        }

        private static string EntryKey(string player, string type, int year)
        {
            return $"{type}/{player}/{year}";
        }

        private static string EntryKey(string player, string type, string year)
        {
            return $"{type}/{player}/{year}";
        }

        private static void AddEntry(Dictionary<string, ShipGenEntry> list, string player, string type, int year, int count, int tries, float time, bool addToFailedOrder = false)
        {
            string key = EntryKey(player, type, year);

            if (list.ContainsKey(key))
            {
                list[key].count += count;
                list[key].time += time;
                list[key].tries += tries;
            }

            else
            {
                if (addToFailedOrder)
                {
                    failedOrder.Add(key);
                }

                list.Add(key, new ShipGenEntry(player, type, year, tries, time));
            }
        }

        private static ShipGenEntry? FindEntry(Dictionary<string, ShipGenEntry> list, ShipGenEntry entry)
        {
            string key = EntryKey(entry);

            if (list.ContainsKey(key))
            {
                return list[key];
            }

            return null;
        }

        private static readonly List<string> batchLogList = new();

        [HarmonyPatch(nameof(BatchShipGenerator.Start))]
        [HarmonyPostfix]
        internal static void Postfix_Start(BatchShipGenerator __instance)
        {
            __instance.startButton.onClick.AddListener(new System.Action(() => {
                genStarted = true;

                if (totalTime.Ticks == 0) totalTime = DateTime.Now;
                if (batchTime.Ticks == 0) batchTime = DateTime.Now;

                if (designsRequested == -1)
                {
                    string nation = __instance.nationDropdown.options[__instance.nationDropdown.value].text.ToLower();

                    if (__instance.nationDropdown.value == 0)
                    {
                        foreach (var option in __instance.nationDropdown.options)
                        {
                            if (__instance.nationDropdown.options.IndexOf(option) == 0) continue;

                            nations.Add(option.text);
                        }
                    }
                    else
                    {
                        nations.Add(nation);
                    }

                    string type = __instance.shipTypeDropdown.options[__instance.shipTypeDropdown.value].text.ToLower();

                    if (__instance.shipTypeDropdown.value == 0)
                    {
                        foreach (var option in __instance.shipTypeDropdown.options)
                        {
                            if (__instance.shipTypeDropdown.options.IndexOf(option) == 0) continue;

                            types.Add(option.text);
                        }
                    }
                    else
                    {
                        types.Add(type);
                    }

                    int yearCount = 0;

                    foreach (var year in __instance.yearsSelected)
                    {
                        if (year.isOn)
                        {
                            yearCount++;
                            years.Add(year.GetChild("Label").GetComponent<TextMeshProUGUI>().text);
                        }
                    }

                    int count = int.Parse(__instance.shipsAmount.text);

                    designsRequested =
                        (__instance.nationDropdown.value == 0 ? __instance.nationDropdown.options.Count : 1) *
                        (__instance.shipTypeDropdown.value == 0 ? __instance.shipTypeDropdown.options.Count : 1) *
                        yearCount * count;
                }
            }));
        }

        [HarmonyPatch(nameof(BatchShipGenerator.Update))]
        [HarmonyPrefix]
        internal static void Prefix_Update(BatchShipGenerator __instance)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    GameManager.Quit();
                }
                else
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

                // ===== Check Errors ===== //

                foreach (var error in __instance.errors)
                {
                    updateLog = true;

                    var split = error.Split(", ");

                    string player = split[0].Substring(8);
                    string type = split[1].Substring(6);
                    string yearStr = split[3].Substring(6);

                    if (!int.TryParse(yearStr, out int year))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `year` for failed ship gen: {error}");
                        continue;
                    }

                    string triesStr = split[4].Substring(7);

                    if (!int.TryParse(triesStr, out int tries))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `tries` for failed ship gen: {error}");
                        continue;
                    }

                    string timeStr = split[5].Substring(6, split[5].Length - 8);

                    if (!int.TryParse(timeStr, out int time))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `time` for failed ship gen: {error}");
                        continue;
                    }

                    float timeSec = (float)time / 1000f;

                    Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Failed"),-10}: {player,-10} | {type} | {year} | {tries,-2} | {timeSec}s");

                    designsAttempted += tries;
                    designsFailed++;

                    AddEntry(failed, player, type, year, 1, tries, timeSec, true);
                }

                __instance.errors.Clear();

                // ===== Check Successes ===== //

                foreach (var info in __instance.info)
                {
                    updateLog = true;

                    var split = info.Split(", ");

                    string player = split[0].Substring(8);
                    string type = split[1].Substring(6);
                    string yearStr = split[3].Substring(6);

                    if (!int.TryParse(yearStr, out int year))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `year` for created ship gen: {info}");
                        continue;
                    }

                    string triesStr = split[4].Substring(7);

                    if (!int.TryParse(triesStr, out int tries))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `tries` for created ship gen: {info}");
                        continue;
                    }

                    string timeStr = split[5].Substring(6, split[5].Length - 8);

                    if (!int.TryParse(timeStr, out int time))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Failed to parse `time` for created ship gen: {info}");
                        continue;
                    }

                    float timeSec = (float)time / 1000f;

                    Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Succeeded"),-10}: {player,-10} | {type} | {year} | {tries,-2} | {timeSec}s");

                    designsAttempted += tries;
                    designsCompleted++;

                    AddEntry(succeeded, player, type, year, 1, tries, timeSec);
                }

                __instance.info.Clear();

                // ===== Check Batch End ===== //

                if (__instance.progress.text.StartsWith("DONE"))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"");
                    Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Batch", $"{batchCount}")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Batch_Time", (DateTime.Now - batchTime).ToString(timeFormat))}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", (DateTime.Now - totalTime).ToString(timeFormat))}");

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
                                string retryKey = EntryKey(retryEntry);

                                failed.Remove(retryKey);
                                failedOrder.Remove(retryKey);
                            }

                            // If it still has counts left, reset the failed entry try count and time to 0 for next batch
                            else 
                            {
                                retryEntry.tries = 0;
                                retryEntry.time = 0;
                            }
                        }
                    }

                    if (failed.Count > 0 && !stopOnBatchEnd)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"");

                        // Fetch next batch & relocate selected failiure to end of list
                        string failedKey = failedOrder.First();
                        retryEntry = failed[failedKey];
                        failedOrder.Remove(failedKey);
                        failedOrder.Add(failedKey);

                        // Configuire the BSG variables
                        List<int> yearList = new();
                        yearList.Add(retryEntry.year);
                        ConfigureBSG(__instance, retryEntry.player, retryEntry.type, yearList, retryEntry.count);

                        // Set selected failiure count to 0
                        retryEntry.count = 0;

                        // Restart the process
                        batchCount++;
                        batchTime = DateTime.Now;
                        __instance.startButton.onClick.Invoke();
                    }

                    else
                    {
                        __instance.progress.text = 
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Finished")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Tries", $"{designsAttempted}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Batches", $"{batchCount}")}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", (DateTime.Now - totalTime).ToString(timeFormat))}\n" +
                            $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_See_Log")}\n";

                        Melon<TweaksAndFixes>.Logger.Msg($"");
                        Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Finished")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Tries", $"{designsAttempted}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Batches", $"{batchCount}")}");
                        Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", (DateTime.Now - totalTime).ToString(timeFormat))}");
                        Melon<TweaksAndFixes>.Logger.Msg($"");

                        if (failed.Count > 0)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"  {ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Failed")}:");

                            Dictionary<string, Dictionary<string, Dictionary<int, ShipGenEntry>>> failedMap = new();

                            foreach (var failure in failed)
                            {
                                failedMap.ValueOrNew(failure.Value.player).ValueOrNew(failure.Value.type).Add(failure.Value.year, failure.Value);
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
                                succeededMap.ValueOrNew(success.Value.player).ValueOrNew(success.Value.type).Add(success.Value.year, success.Value);
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

            // ===== Update Log ===== //

            if (updateLog && genStarted)
            {
                updateLog = false;

                // Overal statistics
                string progressLog =
                    $"requested,{designsRequested}\n" +
                    $"completed,{designsCompleted}\n" +
                    $"failures,{designsFailed}\n" +
                    $"attempts,{designsAttempted}\n" +
                    $"batch_count,{batchCount}\n" +
                    $"total_time,{(DateTime.Now - totalTime).TotalSeconds:N0}\n" +
                    $"batch_time,{(DateTime.Now - batchTime).TotalSeconds:N0}\n" +
                    $"# Nation_Year,type:completed/failed/tries/time\n";

                // Append all year/nation/type info
                foreach (var n in nations)
                {
                    foreach (var y in years)
                    {
                        progressLog += $"{n}_{y},";
                        
                        foreach (var t in types)
                        {
                            string key = EntryKey(n, t, y);

                            ShipGenEntry failure = null;

                            ShipGenEntry success = null;

                            if (!succeeded.ContainsKey(key) && !failed.ContainsKey(key))
                            {
                                progressLog += $"{t}:0/0/0/0;";

                                continue;
                            }

                            if (succeeded.ContainsKey(key))
                            {
                                success = succeeded[key];

                                progressLog += $"{t}:{success.count}/0/{success.tries}/{success.time:N0};";

                                continue;
                            }

                            if (failed.ContainsKey(key))
                            {
                                failure = failed[key];

                                progressLog += $"{t}:0/{failure.count}/{failure.tries}/{failure.time:N0};";

                                continue;
                            }

                            success = succeeded[key];

                            failure = failed[key];

                            progressLog += $"{t}:{success.count}/{failure.count}/{success.tries + failure.tries}/{success.time + failure.time:N0};";

                        }

                        // Trim ending semicolon
                        progressLog = progressLog[..^1];

                        progressLog += $"\n";
                    }
                }

                // Make folder and file
                if (!Directory.Exists(Path.Join(Config._BasePath, "BatchShipGeneratorLogs")))
                {
                    Directory.CreateDirectory(Path.Join(Config._BasePath, "BatchShipGeneratorLogs"));
                }

                File.WriteAllText(Path.Join(Config._BasePath, "BatchShipGeneratorLogs", $"progressLog_{id.ToString("N")[..8]}.csv"), progressLog);
            }

            // ===== Update UI ===== //

            if (__instance.progress.text != lastDisplayText)
            {
                // Overal info
                string extraText =
                    $"\n\n{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Batch", $"{batchCount}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Requested", $"{designsRequested}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Completed", $"{designsCompleted}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Tries", $"{designsAttempted}")}\n" +
                    $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Total_Time", (DateTime.Now - totalTime).ToString(timeFormat))}\n\n";

                // User controls
                if (!stopOnBatchEnd)
                {
                    extraText += $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stop_At_Batch_End")}\n";
                }
                else
                {
                    extraText += $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stoping_At_Batch_End")}\n";
                }

                extraText += $"{ModUtils.LocalizeF("$TAF_Ui_BatchShipGenerator_Stop_Imediate")}\n";

                // TODO: Implement this at some point :P
                // extraText += $"Press [Tab] to toggle overlay visibility.\n";

                // Trim useless text
                var lastNewlineIndex = __instance.progress.text.LastIndexOf("\n");

                if (lastNewlineIndex == -1) lastNewlineIndex = __instance.progress.text.Length;

                __instance.progress.text = __instance.progress.text.Substring(0, lastNewlineIndex) + extraText;

                lastDisplayText = __instance.progress.text;
            }
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
                if (option.text.ToLower() == nation.ToLower())
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
                if (option.text.ToLower() == type.ToLower())
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
