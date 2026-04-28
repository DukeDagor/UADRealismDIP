using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using System.Diagnostics;
using System.Reflection;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(GameManager))]
    internal class Patch_GameManager
    {
        public static bool _IsRefreshSharedDesign = false;
        
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.RefreshSharedDesign))]
        internal static void Prefix_RefreshSharedDesign()
        {
            _IsRefreshSharedDesign = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameManager.RefreshSharedDesign))]
        internal static void Postfix_RefreshSharedDesign()
        {
            _IsRefreshSharedDesign = false;
        }

        public enum SubGameState
        {
            InSharedDesigner,
            InConstructorNew, // New ship
            InConstructorExisting, // Refitting or modifying custom battle ship
            InConstructorViewMode, // Can't edit, only view
            LoadingPredefinedDesigns,
            Other,
        }

        public static SubGameState CurrentSubGameState = SubGameState.Other;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.ToSharedDesignsConstructor))]
        internal static void Prefix_ToSharedDesignsConstructor(int year, PlayerData nation, bool forceCreateNew)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"ToSharedDesignsConstructor: year {year}, nation {nation.nameUi}, forceCreateNew {forceCreateNew}");
            CurrentSubGameState = SubGameState.InSharedDesigner;
            Patch_Ui.OnConstructorShipChanged();

            if (UiM.InputChooseYearEditField != null)
            {
                UiM.InputChooseYearEditField.text = year.ToString();
                UiM.InputChooseYearStaticText.text = year.ToString();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameManager.ToSharedDesignsConstructor))]
        internal static void Postfix_ToSharedDesignsConstructor(int year, PlayerData nation, bool forceCreateNew)
        {
            Patch_Ship.LastCreatedShip = ShipM.GetActiveShip();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.ToConstructor))]
        internal static void Prefix_ToConstructor(bool newShip, Ship viewShip, ref bool allowEdit, IEnumerable<Ship> allowEditMany, ShipType shipTypeNew, bool needCleanup, Player newPlayer)
        {
            if (!GameManager.Instance.isCampaign)
            {
                allowEdit = true;
            }

            // Melon<TweaksAndFixes>.Logger.Msg(
            //     $"ToConstructor: " +
            //     $"bool newShip {newShip}, " +
            //     $"Ship viewShip {(viewShip != null ? viewShip.Name(false, false) : "NULL")}, " +
            //     $"bool allowEdit {allowEdit}, " +
            //     // $"IEnumerable<Ship> allowEditMany {(allowEditMany != null ? new List<Ship>(allowEditMany).Count : "NULL")}, " +
            //     $"IEnumerable<Ship> allowEditMany {allowEditMany}, " +
            //     $"ShipType shipTypeNew, {(shipTypeNew != null ? shipTypeNew.nameUi : "NULL")} " +
            //     $"bool needCleanup, {needCleanup} " +
            //     $"Player newPlayer {(newPlayer != null ? newPlayer.Name(false) : "NULL")} "
            // );

            Patch_Ui.OnConstructorShipChanged();
        }


        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameManager.ToConstructor))]
        internal static void Postfix_ToConstructor(bool newShip, Ship viewShip, bool allowEdit, IEnumerable<Ship> allowEditMany, ShipType shipTypeNew, bool needCleanup, Player newPlayer)
        {
            allowEdit = G.ui.allowEdit;

            if (newShip && allowEdit && viewShip == null)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  Regular constructor with new desgin");
                CurrentSubGameState = SubGameState.InConstructorNew;
            }
            else if (!newShip && allowEdit && viewShip != null)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  Refit mode or existing design: {viewShip.Name(false, false)}");
                CurrentSubGameState = SubGameState.InConstructorExisting;
            }
            else if (!newShip && !allowEdit && viewShip != null)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  View mode for: {viewShip.Name(false, false)}");
                CurrentSubGameState = SubGameState.InConstructorViewMode;
            }
            else
            {
                Melon<TweaksAndFixes>.Logger.Error($"Unknown constructor state!");
                Melon<TweaksAndFixes>.Logger.Error(
                    $"ToConstructor: " +
                    $"bool newShip {newShip}, " +
                    $"Ship viewShip {(viewShip != null ? viewShip.Name(false, false) : "NULL")}, " +
                    $"bool allowEdit {allowEdit}, " +
                    // $"IEnumerable<Ship> allowEditMany {(allowEditMany != null ? new List<Ship>(allowEditMany).Count : "NULL")}, " +
                    $"IEnumerable<Ship> allowEditMany {allowEditMany}, " +
                    $"ShipType shipTypeNew, {(shipTypeNew != null ? shipTypeNew.nameUi : "NULL")} " +
                    $"bool needCleanup, {needCleanup} " +
                    $"Player newPlayer {(newPlayer != null ? newPlayer.Name(false) : "NULL")} "
                );
            }


            Patch_Ship.LastCreatedShip = ShipM.GetActiveShip();
            // Melon<TweaksAndFixes>.Logger.Msg($"  Active Ship: {(Patch_Ship.LastCreatedShip == null ? "NULL" : Patch_Ship.LastCreatedShip.Name(false, false))}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.EndAutodesign))]
        internal static void Prefix_EndAutodesign()
        {
            Patch_ShipGenRandom.OnShipgenEnd();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.StartAutodesign))]
        internal static void Prefix_StartAutodesign()
        {
            Patch_ShipGenRandom.OnShipgenStart();
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(nameof(GameManager.UpdateLoadingConstructor))]
        // internal static void Prefix_UpdateLoadingConstructor(bool needCleanup, Il2CppSystem.Action onDone)
        // {
        //     Melon<TweaksAndFixes>.Logger.Msg($"UpdateLoadingConstructor: bool needCleanup {needCleanup}, Il2CppSystem.Action onDone {(onDone != null ? onDone.method_ptr : "NULL")}");
        // }

        // [HarmonyPrefix]
        // [HarmonyPatch(nameof(GameManager.ChangeState))]
        // internal static void Prefix_ChangeState(GameState newState, bool raiseEnterStateEvents)
        // {
        //     Melon<TweaksAndFixes>.Logger.Msg($"ChangeState: GameState newState {newState}, bool raiseEnterStateEvents {raiseEnterStateEvents}");
        // }

        [HarmonyPatch(nameof(GameManager.ChangeStateUI))]
        [HarmonyPrefix]
        internal static void Prefix_ChangeStateUI(GameManager.UIState newState, bool raiseEvents, out Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            __state = Patch_CampaignController.BeginCampaignLoadMethodTiming(nameof(GameManager.ChangeStateUI), $"newState={newState}, raiseEvents={raiseEvents}");
            Patch_CampaignController.EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(GameManager.ChangeStateUI))]
        [HarmonyPostfix]
        internal static void Postfix_ChangeStateUI(Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            Patch_CampaignController.EndCampaignLoadMethodTiming(__state);
        }

        [HarmonyPatch(nameof(GameManager.OnChangeStateUI))]
        [HarmonyPrefix]
        internal static void Prefix_OnChangeStateUI(out Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            __state = Patch_CampaignController.BeginCampaignLoadMethodTiming(nameof(GameManager.OnChangeStateUI));
            Patch_CampaignController.EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPatch(nameof(GameManager.OnChangeStateUI))]
        [HarmonyPostfix]
        internal static void Postfix_OnChangeStateUI(Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            Patch_CampaignController.EndCampaignLoadMethodTiming(__state);
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(nameof(GameManager.CanHandleMouseInput))]
        // internal static bool Prefix_CanHandleMouseInput(ref bool __result)
        // {
        //     if (!UiM.showPopups)
        //     {
        //         __result = true;
        // 
        //         return false;
        //     }
        // 
        //     return true;
        // }

        public static GameObject GameSavedInfoText = new();
        public static Text GameSavedInfoTextElement = new();
        public static float FadeTime = 5.0f;
        public static float TimeLeft = 0f;
        public static bool GameSavedInfoTextInitalized = false;
        public static bool HasFadeEnded = true;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.SaveInternal))]
        internal static void Prefix_SaveInternal(bool force, bool ignoreStateCheck)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Save Game: forced = {force}, ignoreStateCheck = {ignoreStateCheck}");

            if (!GameSavedInfoTextInitalized)
            {
                GameSavedInfoText = GameObject.Instantiate(G.ui.overlayUi.GetChild("Version"));
                GameSavedInfoText.name = "TAF_GameSavedInfoText";
                GameSavedInfoText.SetParent(G.ui.overlayUi);
                GameSavedInfoText.transform.position = new Vector3(500, 2050, 0);
                GameSavedInfoText.transform.SetScale(1, 1, 1);
                GameSavedInfoText.GetChild("VersionText").name = "TAF_GameSavedInfoTextElement";
                GameSavedInfoTextElement = GameSavedInfoText.GetChild("TAF_GameSavedInfoTextElement").GetComponent<Text>();
                GameSavedInfoTextElement.text = "Game Saved!";
                GameSavedInfoTextElement.fontSize = 20;

                GameSavedInfoTextInitalized = true;
            }

            TimeLeft = FadeTime;
            GameSavedInfoTextElement.color = new Color(1, 1, 1, 1);
            HasFadeEnded = false;
        }

        public static void Update()
        {
            if (TimeLeft > 0)
            {
                TimeLeft -= Time.deltaTime;

                if (TimeLeft <= FadeTime / 2.0f)
                {
                    GameSavedInfoTextElement.color = new Color(1, 1, 1, (TimeLeft / FadeTime) * 2);
                }
            }
            else if (!HasFadeEnded)
            {
                HasFadeEnded = true;
                GameSavedInfoTextElement.color = new Color(1, 1, 1, 0);
                TimeLeft = 0;
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.GetTechYear))]
        internal static bool Prefix_GetTechYear(TechnologyData t, ref int __result)
        {
            if (_IsRefreshSharedDesign && G.ui.sharedDesignYear == Config.StartingYear && !t.effects.ContainsKey("start"))
            {
                __result = 9999;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GameManager._LoadCampaign_d__98))]
    internal class Patch_GameManager_LoadCampaigndCoroutine
    {
        internal struct LoadCampaignTimingFrame
        {
            public bool Enabled;
            public int State;
            public long StartedAt;
        }

        private struct LoadCampaignTimingBucket
        {
            public int Calls;
            public double TotalMs;
            public double MaxMs;
        }

        private static readonly Dictionary<int, LoadCampaignTimingBucket> _LoadCampaignTimingBuckets = new();
        private static bool _LoadCampaignTimingActive = false;
        private static int _LoadCampaignTimingSession = 0;
        private static int _LoadCampaignTimingLastState = int.MinValue;
        private static long _LoadCampaignTimingStartedAt = 0;

        internal static bool IsTimingActive => _LoadCampaignTimingActive;
        internal static bool IsTimingEnabled => IsLoadCampaignTimingEnabled();
        internal static int TimingSession => _LoadCampaignTimingSession;
        internal static int TimingCurrentState => _LoadCampaignTimingLastState;
        internal static string TimingCurrentStateLabel => GetLoadCampaignStateLabel(_LoadCampaignTimingLastState);

        private static bool IsLoadCampaignTimingEnabled()
        {
            return Config.Param("taf_debug_campaign_load_timing", 0) != 0;
        }

        private static LoadCampaignTimingFrame BeginLoadCampaignTimingStep(GameManager._LoadCampaign_d__98 instance)
        {
            if (!IsLoadCampaignTimingEnabled())
            {
                return default;
            }

            int state = instance.__1__state;
            long now = Stopwatch.GetTimestamp();

            if (!_LoadCampaignTimingActive || state == 0)
            {
                _LoadCampaignTimingBuckets.Clear();
                _LoadCampaignTimingActive = true;
                _LoadCampaignTimingSession++;
                _LoadCampaignTimingLastState = int.MinValue;
                _LoadCampaignTimingStartedAt = now;
                Melon<TweaksAndFixes>.Logger.Msg(
                    $"Campaign load timing begin: session={_LoadCampaignTimingSession}, " +
                    $"state={state} ({GetLoadCampaignStateLabel(state)})");
            }

            if (_LoadCampaignTimingLastState != state)
            {
                _LoadCampaignTimingLastState = state;
                Melon<TweaksAndFixes>.Logger.Msg(
                    $"Campaign load timing enter: session={_LoadCampaignTimingSession}, " +
                    $"state={state} ({GetLoadCampaignStateLabel(state)}), loadingScreen={SafeIsLoadingScreenActive()}");
            }

            return new LoadCampaignTimingFrame
            {
                Enabled = true,
                State = state,
                StartedAt = now
            };
        }

        private static void EndLoadCampaignTimingStep(GameManager._LoadCampaign_d__98 instance, bool moveNextResult, LoadCampaignTimingFrame frame)
        {
            if (!frame.Enabled)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            double elapsedMs = ElapsedLoadCampaignMs(frame.StartedAt, now);

            if (!_LoadCampaignTimingBuckets.TryGetValue(frame.State, out var bucket))
            {
                bucket = default;
            }

            bucket.Calls++;
            bucket.TotalMs += elapsedMs;
            bucket.MaxMs = Math.Max(bucket.MaxMs, elapsedMs);
            _LoadCampaignTimingBuckets[frame.State] = bucket;

            float thresholdMs = Config.Param("taf_debug_campaign_load_timing_threshold_ms", 250f);
            if (elapsedMs >= thresholdMs)
            {
                int nextState = instance.__1__state;
                Melon<TweaksAndFixes>.Logger.Msg(
                    $"Campaign load timing slow step: session={_LoadCampaignTimingSession}, " +
                    $"state={frame.State} ({GetLoadCampaignStateLabel(frame.State)}), " +
                    $"next={nextState} ({GetLoadCampaignStateLabel(nextState)}), " +
                    $"elapsedMs={elapsedMs:0.0}, loadingScreen={SafeIsLoadingScreenActive()}, result={moveNextResult}");
            }

            if (!moveNextResult)
            {
                LogLoadCampaignTimingSummary(now);
                _LoadCampaignTimingActive = false;
            }
        }

        private static double ElapsedLoadCampaignMs(long startedAt, long endedAt)
        {
            return (endedAt - startedAt) * 1000.0 / Stopwatch.Frequency;
        }

        private static bool SafeIsLoadingScreenActive()
        {
            try
            {
                return GameManager.IsLoadingScreenActive;
            }
            catch
            {
                return false;
            }
        }

        private static void LogLoadCampaignTimingSummary(long endedAt)
        {
            double totalMs = ElapsedLoadCampaignMs(_LoadCampaignTimingStartedAt, endedAt);
            Melon<TweaksAndFixes>.Logger.Msg(
                $"Campaign load timing summary: session={_LoadCampaignTimingSession}, totalMs={totalMs:0.0}");

            for (int state = -1; state <= 20; state++)
            {
                LogLoadCampaignTimingBucket(state);
            }

            foreach (var entry in _LoadCampaignTimingBuckets)
            {
                if (entry.Key < -1 || entry.Key > 20)
                {
                    LogLoadCampaignTimingBucket(entry.Key);
                }
            }
        }

        private static void LogLoadCampaignTimingBucket(int state)
        {
            if (!_LoadCampaignTimingBuckets.TryGetValue(state, out var bucket))
            {
                return;
            }

            Melon<TweaksAndFixes>.Logger.Msg(
                $"Campaign load timing state: session={_LoadCampaignTimingSession}, " +
                $"state={state} ({GetLoadCampaignStateLabel(state)}), " +
                $"calls={bucket.Calls}, totalMs={bucket.TotalMs:0.0}, maxMs={bucket.MaxMs:0.0}");
        }

        private static string GetLoadCampaignStateLabel(int state)
        {
            return state switch
            {
                -1 => "not-started-or-done",
                0 => "start",
                1 => "load-save-entry",
                2 => "decode-save",
                3 => "campaign-controller-and-world",
                4 => "players",
                5 => "player-country-loop",
                6 => "map-data",
                7 => "world-map-data",
                8 => "ships-start",
                9 => "ships-design-id-fixups",
                10 => "other-vessels",
                11 => "battles",
                12 => "submarine-battles-log-date",
                13 => "blockades-task-forces",
                14 => "map-init-enter-world",
                15 => "post-world-mapui-log-tech-province-battles",
                16 => "player-ship-textures",
                17 => "player-ship-texture-continuation",
                18 => "navmesh-passable-areas-first",
                19 => "navmesh-passable-areas-second-and-predefs",
                _ => "unknown"
            };
        }

        // This method calls CampaignController.PrepareProvinces *before* CampaignMap.PreInit
        // So we patch here and skip the preinit patch.
        [HarmonyPatch(nameof(GameManager._LoadCampaign_d__98.MoveNext))]
        [HarmonyPrefix]
        internal static void Prefix_MoveNext(GameManager._LoadCampaign_d__98 __instance, out LoadCampaignTimingFrame __state)
        {
            __state = default;

            // TODO: Patch state 17 (G.ui.PrepareShipAllTex(ship))

            // Skip generating previews. They don't generate right anyway...
            if (__instance.__1__state == 17)
            {
                //foreach (var ship in CampaignController.Instance.CampaignData.GetShips)
                //{
                //    if (ship.player != PlayerController.Instance) continue;
                //
                //    if (!ship.isDesign && !ship.isRefitDesign) continue;
                //
                //    Melon<TweaksAndFixes>.Logger.Msg($"Loading parts for design {ship.Name(false, false)}");
                //
                //    // ship.hull.LoadModel(ship, false);
                //
                //    foreach (var part in ship.parts)
                //    {
                //        if (part.data.model == "(custom)") continue;
                //        Melon<TweaksAndFixes>.Logger.Msg($"  Loading {part.data.model}");
                //        Util.ResourcesLoad<GameObject>(part.data.model);
                //    }
                //
                //    Melon<TweaksAndFixes>.Logger.Msg($"  Generating preview...");
                //
                //    G.ui.GetShipPreviewTex(ship);
                //}

                __instance.__1__state++;
            }

            if (__instance.__1__state == 9)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Checking for null design IDs...");

                List<Ship.Store> designs = new();
                List<Ship.Store> nullIds = new();

                int total = 0;

                foreach (var ship in __instance.__8__1.store.Ships)
                {
                    if (!ship.isSharedDesign
                        || ship.status == VesselEntity.Status.Erased
                        || ship.status == VesselEntity.Status.Sunk
                        || ship.status == VesselEntity.Status.Scrapped
                        || ship.designId != Il2CppSystem.Guid.Empty)
                        continue;

                    // Null ID shared design
                    if (ship.id == Il2CppSystem.Guid.Empty)
                    {
                        ship.id = Il2CppSystem.Guid.NewGuid();
                        Melon<TweaksAndFixes>.Logger.Msg($"  Design '{ship.vesselName}' now has ID {ship.id}");
                        designs.Add(ship);
                    }
                    // Normal shared design
                    else if (ship.status == VesselEntity.Status.None)
                    {
                        designs.Add(ship);
                    }
                    // Null ID ship
                    else
                    {
                        total++;
                        nullIds.Add(ship);
                    }
                }

                if (total != 0) Melon<TweaksAndFixes>.Logger.Msg($"Found {total} null ID ships, matching to designs:");

                foreach (var ship in nullIds)
                {
                    bool found = false;

                    // Melon<TweaksAndFixes>.Logger.Msg($"  Checking Ship {ship.vesselName}");

                    foreach (var design in designs)
                    {
                        if (ship.parts.Count != design.parts.Count) continue;

                        bool failed = false;
                        for (int i = 0; i < ship.parts.Count; i++)
                        {
                            if (ship.parts[i].Id != design.parts[i].Id) failed = true;
                        }
                        if (failed) continue;

                        ship.designId = design.id;
                        found = true;
                        Melon<TweaksAndFixes>.Logger.Msg($"  Ship {ship.vesselName} is of design {design.vesselName}");
                        break;
                    }

                    if (!found)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Ship {ship.vesselName} has no matching design. Deleting from save.");
                        __instance.__8__1.store.Ships.Remove(ship);
                    }
                }
            }

            if (__instance.__1__state == 6 && (Config.OverrideMap != Config.OverrideMapOptions.Disabled))
            {
                MapData.LoadMapData();
                Patch_CampaignMap._SkipNextMapPatch = true;
            }

            __state = BeginLoadCampaignTimingStep(__instance);
        }

        [HarmonyPatch(nameof(GameManager._LoadCampaign_d__98.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(GameManager._LoadCampaign_d__98 __instance, bool __result, LoadCampaignTimingFrame __state)
        {
            EndLoadCampaignTimingStep(__instance, __result, __state);
        }
    }

    [HarmonyPatch]
    internal class Patch_GameManager_LoadCampaignFinalCallback
    {
        private static MethodBase TargetMethod()
        {
            foreach (var nestedType in typeof(GameManager).GetNestedTypes(AccessTools.all))
            {
                if (!nestedType.Name.Contains("DisplayClass98_0"))
                    continue;

                foreach (var method in nestedType.GetMethods(AccessTools.all))
                {
                    if (method.Name.Contains("LoadCampaign") && method.Name.Contains("24"))
                        return method;
                }
            }

            throw new MissingMethodException("Could not find GameManager LoadCampaign final callback <LoadCampaign>b__24.");
        }

        [HarmonyPrefix]
        internal static void Prefix_FinalCallback(out Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            __state = Patch_CampaignController.BeginCampaignLoadMethodTiming("GameManager.LoadCampaign.finalCallback");
            Patch_CampaignController.EndCampaignLoadMethodPrefix(ref __state);
        }

        [HarmonyPostfix]
        internal static void Postfix_FinalCallback(Patch_CampaignController.CampaignLoadMethodTimingFrame __state)
        {
            Patch_CampaignController.EndCampaignLoadMethodTiming(__state);
        }
    }
}
