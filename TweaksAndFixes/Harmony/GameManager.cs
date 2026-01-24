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

                    if (ship.id == Il2CppSystem.Guid.Empty)
                    {
                        ship.id = Il2CppSystem.Guid.NewGuid();
                        Melon<TweaksAndFixes>.Logger.Msg($"  Design '{ship.vesselName}' now has ID {ship.id}");
                        designs.Add(ship);
                    }
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
                        Melon<TweaksAndFixes>.Logger.Msg($"  Ship {ship.vesselName} has no matching design!");
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
