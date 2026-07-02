using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        internal static bool Prefix_ToConstructor(
            ref bool newShip, Ship viewShip, ref bool allowEdit,
            Il2CppSystem.Collections.Generic.IEnumerable<Ship> allowEditMany,
            ShipType shipTypeNew, bool needCleanup, Player newPlayer
        )
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
        internal static void Postfix_ToConstructor(
            bool newShip, Ship viewShip, bool allowEdit,
            Il2CppSystem.Collections.Generic.IEnumerable<Ship> allowEditMany,
            ShipType shipTypeNew, bool needCleanup, Player newPlayer)
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

            Patch_SceneManager.ConfigureScene(newState);
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

            UiM.ShowTextTopLeft(ModUtils.LocalizeF("$TAF_Ui_FadeText_GameSaved"));
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

        internal static bool firstLoad = true;

        public static System.Collections.IEnumerator ToMainMenuCoroutine()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Start loading");

            yield return new WaitForEndOfFrame();

            G.ui.loadingText = ModUtils.LocalizeF("$Ui_LoadingMaiMenu");

            yield return new WaitForEndOfFrame();

            if (GameManager.IsCampaign)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Unload Campaign");
                GameManager.Instance.UnloadCampaign();
            }

            yield return new WaitForEndOfFrame();

            string currScene = SceneManager.GetActiveScene().name;

            // Melon<TweaksAndFixes>.Logger.Msg($"Current scene: {currScene}");
            if (currScene == G.level.mainMenuScene)
                yield break;

            Util.ClearResourcesCache();
            DecalsManager.Release();
            // if (!firstLoad)
            //     SceneManager.LoadScene(G.level.emptyScene);
            Resources.UnloadUnusedAssets();

            // Melon<TweaksAndFixes>.Logger.Msg($"Loaded empty scene");

            yield return new WaitForEndOfFrame();

            GC.Collect();
            GameManager.Instance.LoadCampaignProgress();

            // if (!firstLoad)
            // {
            //     // SceneManager.LoadScene(G.level.mainMenuScene);
            //     goto FINISH;
            // }

            yield return new WaitForEndOfFrame();

            // Melon<TweaksAndFixes>.Logger.Msg($"Caching constructor objects");
            Patch_SceneManager.bypass = true;
            SceneManager.LoadScene(G.level.constructorScene);

            yield return new WaitForEndOfFrame();

            GameObject _LevelConstructor = null;
            GameObject _ReflectionProbesCamera = null;
            
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go == null) continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  {go.name}");
                go.active = false;
                if (go.name == "LevelConstructor")
                {
                    _LevelConstructor = GameObject.Instantiate(go, G.container.transform);
                    _LevelConstructor.name = go.name;
                }
                if (go.name == "Reflection Probes Camera")
                {
                    _ReflectionProbesCamera = GameObject.Instantiate(go, G.container.transform);
                    _ReflectionProbesCamera.name = go.name;
                }
            }

            if (_LevelConstructor == null
                // || _LevelConstructorEnvMinimal == null
                || _ReflectionProbesCamera == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Failed to instantiate constructor scene!");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_LevelConstructor == null}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {_LevelConstructorEnvMinimal == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_ReflectionProbesCamera == null}");
                yield break;
            }

            yield return new WaitForEndOfFrame();

            // Melon<TweaksAndFixes>.Logger.Msg($"Caching campaign objects");
            SceneManager.LoadScene(G.level.worldScene);

            yield return new WaitForEndOfFrame();

            GameObject _Mesh = null;
            GameObject _Mesh2 = null;
            GameObject _BordersMesh = null;
            GameObject _From = null;
            GameObject _To = null;

            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  {go.name}");
                go.active = false;
                if (go.name == "Mesh")
                {
                    _Mesh = GameObject.Instantiate(go, G.container.transform);
                    _Mesh.name = go.name;
                }
                if (go.name == "Mesh2")
                {
                    _Mesh2 = GameObject.Instantiate(go, G.container.transform);
                    _Mesh2.name = go.name;
                }
                if (go.name == "BordersMesh")
                {
                    _BordersMesh = GameObject.Instantiate(go, G.container.transform);
                    _BordersMesh.name = go.name;
                }
                if (go.name == "From")
                {
                    _From = GameObject.Instantiate(go, G.container.transform);
                    _From.name = go.name;
                }
                if (go.name == "To")
                {
                    _To = GameObject.Instantiate(go, G.container.transform);
                    _To.name = go.name;
                }
            }
            
            if (_Mesh == null
                || _Mesh2 == null
                || _BordersMesh == null
                || _From == null
                || _To == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Failed to instantiate campaign scene!");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_Mesh == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_Mesh2 == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_BordersMesh == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_From == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  {_To == null}");
                yield break;
            }

            yield return new WaitForEndOfFrame();

            // Melon<TweaksAndFixes>.Logger.Msg($"Loading base scene");
            SceneManager.LoadScene(G.level.battleScene);

            yield return new WaitForEndOfFrame();

            GameObject DayCycleAndWeatherO = null;
            GameObject LevelBattle = null;
            GameObject WaterSurfaceCam = null;

            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  {go.name}");
                if (go.name == "Day cycle & weather")
                {
                    DayCycleAndWeatherO = go;
                }
                if (go.name == "LevelBattle")
                {
                    LevelBattle = go;
                }
                if (go.name == "WaterSurfaceCam")
                {
                    WaterSurfaceCam = go;
                }
            }

            yield return new WaitForEndOfFrame();

            GameObject LevelConstructor = GameObject.Instantiate(_LevelConstructor);
            LevelConstructor.name = _LevelConstructor.name;
            GameObject ReflectionProbesCamera = GameObject.Instantiate(_ReflectionProbesCamera);
            ReflectionProbesCamera.name = _ReflectionProbesCamera.name;
            
            GameObject Mesh0 = GameObject.Instantiate(_Mesh);
            Mesh0.name = _Mesh.name;
            GameObject Mesh2 = GameObject.Instantiate(_Mesh2);
            Mesh2.name = _Mesh2.name;
            GameObject BordersMesh = GameObject.Instantiate(_BordersMesh);
            BordersMesh.name = _BordersMesh.name;
            GameObject From = GameObject.Instantiate(_From);
            From.name = _From.name;
            GameObject To = GameObject.Instantiate(_To);
            To.name = _To.name;

            yield return new WaitForEndOfFrame();

            _LevelConstructor.TryDestroy(true);
            _ReflectionProbesCamera.TryDestroy(true);

            yield return new WaitForEndOfFrame();

            if (LevelConstructor == null
                || ReflectionProbesCamera == null
                || DayCycleAndWeatherO == null
                || LevelBattle == null
                || WaterSurfaceCam == null
                || Mesh0 == null
                || Mesh2 == null
                || BordersMesh == null
                || From == null
                || To == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Failed to coalesce constructor and battle scene!");
                Melon<TweaksAndFixes>.Logger.Msg($"  LevelConstructor           : {LevelConstructor == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  ReflectionProbesCamera     : {ReflectionProbesCamera == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  DayCycleAndWeatherO        : {DayCycleAndWeatherO == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  LevelBattle                : {LevelBattle == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  WaterSurfaceCam            : {WaterSurfaceCam == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  Mesh                       : {Mesh0 == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  Mesh2                      : {Mesh2 == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  BordersMesh                : {BordersMesh == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  From                       : {From == null}");
                Melon<TweaksAndFixes>.Logger.Msg($"  To                         : {To == null}");
                yield break;
            }

            Patch_SceneManager.LevelConstructor = LevelConstructor;
            Patch_SceneManager.ReflectionProbesCamera = ReflectionProbesCamera;
            Patch_SceneManager.DayCycleAndWeatherO = DayCycleAndWeatherO;
            Patch_SceneManager.LevelBattle = LevelBattle;
            Patch_SceneManager.WaterSurfaceCam = WaterSurfaceCam;
            Patch_SceneManager.Mesh0 = Mesh0;
            Patch_SceneManager.Mesh2 = Mesh2;
            Patch_SceneManager.BordersMesh = BordersMesh;
            Patch_SceneManager.From = From;
            Patch_SceneManager.To = To;

            Patch_SceneManager.bypass = false;
            Patch_SceneManager.inited = true;

            Patch_SceneManager.Mesh0.transform.position += new Vector3(0, 0.1f, 0);
            Patch_SceneManager.Mesh2.transform.position += new Vector3(0, 0.1f, 0);
            Patch_SceneManager.BordersMesh.transform.position += new Vector3(0, 0.1f, 0);
            Patch_SceneManager.From.transform.position += new Vector3(0, 0.1f, 0);
            Patch_SceneManager.To.transform.position += new Vector3(0, 0.1f, 0);

            if (Config.Param("taf_use_old_constructor_lighting", 0) == 0)
            {
                Patch_SceneManager.LevelConstructor.GetChild("Sun").active = false;
                Patch_SceneManager.LevelBattle.GetChild("Scene Lighting").active = true;
            }
            else
            {
                Patch_SceneManager.LevelConstructor.GetChild("Sun").active = true;
                Patch_SceneManager.LevelBattle.GetChild("Scene Lighting").active = false;
            }

            var dockA = ModUtils.GetChildAtPath("Dock/DryDock_001", Patch_SceneManager.LevelConstructor);

            var waterAll = dockA.GetChild("water_all_001");

            var waterHC = GameObject.Instantiate(waterAll, dockA.transform);
            waterHC.name = "water_in_huge_dock_002";
            waterHC.transform.localPosition = new(500f, 0.5f, 245f);
            waterHC.transform.SetScale(0.06f, 0.1f, 0.25f);

            var waterBC = GameObject.Instantiate(waterAll, dockA.transform);
            waterBC.name = "water_in_big_dock_002";
            waterBC.transform.localPosition = new(250f, 0.5f, 135f);
            waterBC.transform.SetScale(0.04f, 0.1f, 0.15f);

            var waterMC = GameObject.Instantiate(waterAll, dockA.transform);
            waterMC.name = "water_in_middle_dock_002";
            waterMC.transform.localPosition = new(0, 0.5f, 85f);
            waterMC.transform.SetScale(0.05f, 0.1f, 0.12f);

            var waterSC = GameObject.Instantiate(waterAll, dockA.transform);
            waterSC.name = "water_in_smoll_dock_002";
            waterSC.transform.localPosition = new(-250f, 0.5f, 65f);
            waterSC.transform.SetScale(0.03f, 0.1f, 0.11f);

            waterAll.active = false;


            if (Config.Param("taf_disable_all_water", 0) == 1)
            {
                Patch_SceneManager.LevelBattle.GetChild("opwOcean4OPW_00").active = false;
                Patch_SceneManager.LevelConstructor.GetChild("MoveWithShip").active = false;
                waterHC.active = false;
                waterBC.active = false;
                waterMC.active = false;
                waterSC.active = false;
            }

            Patch_SceneManager.SetConstructorWeather();

            yield return new WaitForEndOfFrame();

            GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);

            Melon<TweaksAndFixes>.Logger.Msg($"Loaded main menu");

            yield break;

            // Unused
            // while (!MigrationManager.IsDone())

        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.ToMainMenu))]
        internal static bool Prefix_ToMainMenu()
        {
            if (!firstLoad)
                return true;

            firstLoad = false;

            if (GameManager.Instance.CurrentState == GameManager.GameState.Loading
                || GameManager.Instance.CurrentState == GameManager.GameState.LoadingCustom)
            {
                G.ui.dontChangeLoadingScreen = true;
            }

            G.ui.quickLoadingScreen = true;

            if (GameManager.Instance.CurrentState == GameManager.GameState.World)
            {
                GameManager.Save(true, false);
            }

            if (GameManager.IsCampaign && GameManager.Instance.CurrentState == GameManager.GameState.Constructor)
            {
                G.ui.ExitFromRefitMode();
            }

            if (GameManager.IsCampaign)
            {
                G.ui.OnCampaignLeave();
            }

            GameManager.Instance.ChangeState(GameManager.GameState.LoadingCustom);

            Melon<TweaksAndFixes>.Logger.Msg($"Calling ToMainMenuCoroutine coroutine...");
            MelonCoroutines.Start(ToMainMenuCoroutine());

            return false;
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
