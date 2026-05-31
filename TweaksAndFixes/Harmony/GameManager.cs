using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;

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
            UiM.UpdateShipTypeButtons(true);
        }

        public static bool ignoreNextToConstructor = false;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.ToConstructor))]
        internal static bool Prefix_ToConstructor(ref bool newShip, Ship viewShip, ref bool allowEdit, IEnumerable<Ship> allowEditMany, ShipType shipTypeNew, bool needCleanup, Player newPlayer)
        {
            if (ignoreNextToConstructor)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Skipping ToConstructor");
                ignoreNextToConstructor = false;
                return false;
            }

            if (!GameManager.Instance.isCampaign)
            {
                allowEdit = true;
            }


            if (newShip && allowEdit && viewShip == null)
            {
            }
            else if (!newShip && allowEdit && viewShip != null)
            {
            }
            else if (!newShip && !allowEdit && viewShip != null)
            {
            }
            else
            {
                Melon<TweaksAndFixes>.Logger.Error($"Unknown constructor state! Using fallback!");
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
                newShip = true;
                allowEdit = true;
            }

            Patch_Ui.OnConstructorShipChanged();

            return true;
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
            UiM.UpdateShipTypeButtons(true);
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

        [HarmonyPatch(nameof(GameManager.ToCustomBattle))]
        [HarmonyPrefix]
        internal static bool Postfix_ToCustomBattle(bool doBuild, bool isRestart)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Prefix_ToCustomBattle");

            GameManager.Instance.ChangeState(GameManager.GameState.LoadingCustom, true);

            Melon<TweaksAndFixes>.Logger.Msg($"State Changed");

            MelonCoroutines.Start(Patch_BattleManager.UpdateLoadingCustomBattleM(doBuild, isRestart));

            Melon<TweaksAndFixes>.Logger.Msg($"Corutine Called");

            return false;
        }

        [HarmonyPatch(nameof(GameManager.CustomBattleConstructorFinished))]
        [HarmonyPrefix]
        internal static bool Postfix_CustomBattleConstructorFinished()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Prefix_CustomBattleConstructorFinished");

            BattleManager.Instance.CustomBattleSavePlayerDesigns();

            GameManager.Instance.ChangeState(GameManager.GameState.LoadingCustom, true);

            Melon<TweaksAndFixes>.Logger.Msg($"State Changed");

            MelonCoroutines.Start(Patch_BattleManager.UpdateLoadingCustomBattleM(true, false));

            Melon<TweaksAndFixes>.Logger.Msg($"Corutine Called");

            return false;
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(nameof(GameManager.UpdateLoadingConstructor))]
        // internal static void Prefix_UpdateLoadingConstructor(bool needCleanup, Il2CppSystem.Action onDone)
        // {
        //     Melon<TweaksAndFixes>.Logger.Msg($"UpdateLoadingConstructor: bool needCleanup {needCleanup}, Il2CppSystem.Action onDone {(onDone != null ? onDone.method_ptr : "NULL")}");
        // }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.ChangeState))]
        internal static void Prefix_ChangeState(GameManager.GameState newState, bool raiseEnterStateEvents)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"ChangeState: GameState newState {newState}, bool raiseEnterStateEvents {raiseEnterStateEvents}");
            if (newState == GameManager.GameState.CustomBattleSetup
                 || newState == GameManager.GameState.Battle)
            {
                UiM.SelectedShip = Guid.Empty;
            }

            if (newState == GameManager.GameState.MainMenu)
            {
                // TODO: Clear extraData
            }
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

        public static void SaveCampaignStore(CampaignController.Store store, int index, bool autosave = false)
        {
            Storage.WriteByte(
                $"Saves/save_{index}.bin",
                Util.SerializeObjectByte(store)
            );
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.SaveInternal))]
        internal static bool Prefix_SaveInternal(GameManager __instance, bool force, bool ignoreStateCheck)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Save Game: forced = {force}, ignoreStateCheck = {ignoreStateCheck}");

            if (!GameManager.IsActualGame && !ignoreStateCheck)
                return false;

            if (!GameManager.IsCampaign && !force)
                return false;

            __instance.savePending = false;

            if (CampaignController.Instance.IsFinished)
                return false;

            var store = CampaignController.Instance.GetStore();

            SaveCampaignStore(store, GameManager.Instance.currentCampaignSlotIndex);

            UiM.ShowTextTopLeft(ModUtils.LocalizeF("$TAF_Ui_FadeText_GameSaved"));

            return false;
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


        public static System.Collections.IEnumerator BackToCampaignCoroutine(
            Il2CppSystem.Action<Il2CppSystem.Action> onLoaded, GameManager.UIState overrideState)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Start loading");
            // OnDone -> DisplayClass100
            // BackToCampaignCoroutine_b__97

            // G.ui._loadingText_k__BackingField = ModUtils.LocalizeF("$Ui_World_LoadingGame");

            Melon<TweaksAndFixes>.Logger.Msg($"  Loading World");

            G.ui._loadingText_k__BackingField = ModUtils.LocalizeF("$Ui_World_LoadingWorld");

            yield return new WaitForEndOfFrame();
            // ========== //

            G.ui._dontChangeLoadingScreen_k__BackingField = true;

            Melon<TweaksAndFixes>.Logger.Msg($"  Updaing game state");

            GameManager.Instance.ChangeState(GameManager.GameState.World);

            yield return new WaitForEndOfFrame();
            // ========== //

            Melon<TweaksAndFixes>.Logger.Msg($"  Showing world");

            WorldCampaign.instance.Show(true);

            yield return new WaitForEndOfFrame();
            // ========== //

            Melon<TweaksAndFixes>.Logger.Msg($"  Changing UI state");

            if (overrideState != GameManager.UIState.None)
                GameManager.Instance.ChangeStateUI(overrideState);
            else
                GameManager.Instance.ChangeStateUI(GameManager.Instance.PrevStateUI);

            yield return new WaitForEndOfFrame();
            // ========== //

            Patch_BattleManager.BeforeLoadScene();

            yield return new WaitForEndOfFrame();
            // ========== //

            Melon<TweaksAndFixes>.Logger.Msg($"  Loading scene");

            // Why in gods name did they use the Async version for this stuff only?
            //   The scenes being loaded are tiny, only taking a couple microseconds to load...
            UnityEngine.SceneManagement.SceneManager.LoadScene(G.level.worldScene);

            Melon<TweaksAndFixes>.Logger.Msg($"  Done!");

            yield return new WaitForEndOfFrame();
            // ========== //

            Patch_BattleManager.AfterLoadScene();

            yield return new WaitForEndOfFrame();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.BackToCampaign))]
        internal static bool Prefix_BackToCampaign(Il2CppSystem.Action<Il2CppSystem.Action> onLoaded, GameManager.UIState overrideState)
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.Battle)
                return true;

            GameManager.Instance.ChangeState(GameManager.GameState.LoadingCustom);

            Melon<TweaksAndFixes>.Logger.Msg($"Calling BackToCampaign coroutine...");
            MelonCoroutines.Start(BackToCampaignCoroutine(onLoaded, overrideState));

            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager._LoadCampaign_d__98))]
    internal class Patch_GameManager_LoadCampaigndCoroutine
    {
        // public static Stopwatch watch = new();
        // public static int lastState = 0;

        // This method calls CampaignController.PrepareProvinces *before* CampaignMap.PreInit
        // So we patch here and skip the preinit patch.
        [HarmonyPatch(nameof(GameManager._LoadCampaign_d__98.MoveNext))]
        [HarmonyPrefix]
        internal static void Prefix_MoveNext(GameManager._LoadCampaign_d__98 __instance)
        {
            // TODO: Patch state 17 (G.ui.PrepareShipAllTex(ship))
            // watch.Start();
            // 
            // if (__instance.__1__state != lastState)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg($"{__instance.__1__state} -> {lastState} : {watch.ElapsedMilliseconds}");
            //     watch.Restart();
            //     lastState = __instance.__1__state;
            // }

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
        }

        // [HarmonyPatch(nameof(GameManager._LoadCampaign_d__98.MoveNext))]
        // [HarmonyPostfix]
        // internal static void Postfix_MoveNext(GameManager._LoadCampaign_d__98 __instance)
        // {
        //     watch.Stop();
        // }
    }
}
