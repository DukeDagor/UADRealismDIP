using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(TimeControl))]
    internal class Patch_TimeControl
    {
        public static int LastAutomaticTimeScaleSlowdown = 0;
        public static int LastUserTimeScale = 1;
        public static bool IgnoreNextAutomaticTimeScaleSlowdown = false;

        [HarmonyPatch(nameof(TimeControl.TimeScale))]
        [HarmonyPrefix]
        internal static void Prefix_TimeScale(ref float scale)
        {
            if (Config.Param("taf_disable_battle_simulation_speed_restrictions", 1) != 1)
            {
                return;
            }

            // Melon<TweaksAndFixes>.Logger.Msg("SET TIMESCALE: " + scale);

            int truncScale = (int)(scale + 0.1f);

            if (Patch_BattleManager.InUpdateSpeedLimit)
            {
                if (LastUserTimeScale > truncScale && truncScale != LastAutomaticTimeScaleSlowdown)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg("New override value: " + truncScale);
                    LastAutomaticTimeScaleSlowdown = truncScale;

                    if (IgnoreNextAutomaticTimeScaleSlowdown)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg("Ignore override.");
                        IgnoreNextAutomaticTimeScaleSlowdown = false;
                        scale = LastUserTimeScale;
                    }
                }
                else
                {
                    // Melon<TweaksAndFixes>.Logger.Msg("Setting scale to: " + LastUserTimeScale);
                    scale = LastUserTimeScale;
                }
            }
            else if (LastUserTimeScale != truncScale)
            {
                LastUserTimeScale = truncScale;
                IgnoreNextAutomaticTimeScaleSlowdown = true;
            }
        }
    }


    [HarmonyPatch(typeof(BattleManager))]
    internal class Patch_BattleManager
    {
        public static int LastTimeScale = 0;
        public static bool InUpdateSpeedLimit = false;

        public static void SetTimeSpeedLimit()
        {
            if (Config.Param("taf_disable_battle_simulation_speed_restrictions", 1) != 1)
            {
                return;
            }

            float speed = 30.0f;
            
            // TODO: Limit speed to 15x when speed is below 5x?

            BattleManager.Instance.CombatTimeSpeedLimit = new Il2CppSystem.Nullable<float>(speed);
        }

        [HarmonyPatch(nameof(BattleManager.CombatUpdateTimeSpeedLimit))]
        [HarmonyPrefix]
        internal static bool Prefix_CombatUpdateTimeSpeedLimit()
        {
            return false;

            // InUpdateSpeedLimit = true;
        }

        // [HarmonyPatch(nameof(BattleManager.CombatUpdateTimeSpeedLimit))]
        // [HarmonyPostfix]
        // internal static void Postfix_CombatUpdateTimeSpeedLimit()
        // {
        //     InUpdateSpeedLimit = false;
        // 
        //     SetTimeSpeedLimit();
        // }

        public static void BeforeLoadScene()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"BeforeLoadScene");

            var bm = BattleManager.Instance;
            Melon<TweaksAndFixes>.Logger.Msg($"  CalculateDamage");
            bm.CalculateDamage(bm.CurrentBattle);
            Melon<TweaksAndFixes>.Logger.Msg($"  BattleCompleteCalculateRelation");
            bm.BattleCompleteCalculateRelation(bm.CurrentBattle);
            Melon<TweaksAndFixes>.Logger.Msg($"  CalculateCrewTraining");
            bm.CalculateCrewTraining(bm.CurrentBattle);
            Melon<TweaksAndFixes>.Logger.Msg($"  RefreshFleetWindow");
            G.ui.RefreshFleetWindow();
            Melon<TweaksAndFixes>.Logger.Msg($"  ReportBattle");
            G.ui.ReportBattle(bm.CurrentBattle);
            Melon<TweaksAndFixes>.Logger.Msg($"  Refresh");
            G.ui.Refresh();
            Melon<TweaksAndFixes>.Logger.Msg($"  CheckBattle");
            Ui.CheckBattle(bm.CurrentBattle);

            Melon<TweaksAndFixes>.Logger.Msg($"Making auto save...");
            // GameManager.Instance.SaveCampaignProgress();
            GameManager.Instance.SaveInternal(true);
            Melon<TweaksAndFixes>.Logger.Msg($"Save complete!");
        }

        public static void AfterLoadScene()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"AfterLoadScene");

            var pos = BattleManager.Instance.CurrentBattle.BattleWorldPos;

            var bm = BattleManager.Instance;

            Melon<TweaksAndFixes>.Logger.Msg($"  Cleaning up...");
            G.ui._dontChangeLoadingScreen_k__BackingField = false;
            G.ui._quickLoadingScreen_k__BackingField = true;
            Melon<TweaksAndFixes>.Logger.Msg($"  Moving camera...");
            Cam.Instance.LookAtPointEx(pos);
            G.ui.CompleteLoadingScreen();
            Melon<TweaksAndFixes>.Logger.Msg($"  Transition complete!");
        }


        [HarmonyPatch(nameof(BattleManager.Update))]
        [HarmonyPostfix]
        internal static void Postfix_Update()
        {
            var scene = SceneManager.GetActiveScene();
            var sceneObjs = scene.GetRootGameObjects();
            GameObject tempObj = null;
            GameObject shellsObj = null;

            foreach (var obj in sceneObjs)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  {obj.name}");

                if (obj.name == "Temp")
                {
                    tempObj = obj;
                }
                if (obj.name == "Shells")
                {
                    shellsObj = obj;
                }
            }


            //Melon<TweaksAndFixes>.Logger.Msg($"Found Temp obj!");

            if (tempObj != null)
                foreach (var child in tempObj.GetChildren().ToArray())
                {
                    if (child.name != "TrailSmoke" && child.name != "TrailFire")
                        continue;

                    // Melon<TweaksAndFixes>.Logger.Msg($"  {child.name} ({child.transform.GetSiblingIndex()})");
                    child.TryDestroy(true);
                }

            // Melon<TweaksAndFixes>.Logger.Msg($"Found Temp obj!");

            if (shellsObj != null)
                foreach (var child in shellsObj.GetChildren().ToArray())
                {
                    bool foundOne = false;

                    // Melon<TweaksAndFixes>.Logger.Msg($"  {child.name} ({child.transform.GetSiblingIndex()})");

                    foreach (var subchild in child.GetChildren().ToArray())
                    {
                        if (!subchild.name.Contains("Loop"))
                            continue;

                        if (!foundOne)
                        {
                            foundOne = true;
                            continue;
                        }

                        // Melon<TweaksAndFixes>.Logger.Msg($"Deleting:");
                        // Melon<TweaksAndFixes>.Logger.Msg($"  {G.sound.currentlyPlaying.Count}");
                        G.sound.Stop(subchild);
                        subchild.TryDestroy(true);
                        // Melon<TweaksAndFixes>.Logger.Msg($"  {G.sound.currentlyPlaying.Count}");
                    }
                }
        }


        // LeaveBattle

        [HarmonyPatch(nameof(BattleManager.LeaveBattle))]
        [HarmonyPostfix]
        internal static void Postfix_LeaveBattle()
        {
            if (GameManager.Instance.isCampaign)
            {
                CampaignControllerM.RequestForcedGameSave = true;
            }
        }

        [HarmonyPatch(nameof(BattleManager.StartCustomBattle))]
        [HarmonyPrefix]
        internal static void Prefix_StartCustomBattle(Ui.SkirmishSetup skirmishSetup, bool doBuild)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Prefix_StartCustomBattle");
            UiM.SkipNextUpdateShipTypeButtons = true;
        }

        [HarmonyPatch(nameof(BattleManager.StartCustomBattle))]
        [HarmonyPostfix]
        internal static void Postfix_StartCustomBattle(BattleManager __instance, Ui.SkirmishSetup skirmishSetup, bool doBuild)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Postfix_StartCustomBattle");
            UiM.OnStartCustomBattle(doBuild);
        }

        public static System.Collections.IEnumerator UpdateLoadingCustomBattleM(bool doBuild, bool isRestart)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Start loading");

            G.ui._loadingText_k__BackingField = ModUtils.LocalizeF("$Ui_Battle_StartingCustomBattle");

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            var sk = UiM.skirmishSetupMod;

            // if (GameManager.IsCustomBattle)
            // {
            //     G.ui.InitialCustomBattleShipList();
            // }

            UiM.skirmishSetupMod.InitializePlayerMadeShips();

            bool canContinue = true;
            bool anyError = false;
            
            Player player = PlayerController.Instance;
            Player enemy = CampaignController.Instance.CampaignData.Players[0] == player
                ? CampaignController.Instance.CampaignData.Players[1]
                : CampaignController.Instance.CampaignData.Players[0];

            List<Il2CppSystem.Guid> sharedIgnoreList = new();
            List<Il2CppSystem.Guid> predefIgnoreList = new();

            for (int i = 0; i < 2; i++)
            {
                Player currPlayer =
                    i == 0 ? player : enemy;
                UiM.SkirmishSetupMod.SkirmishPlayer currSkPlayer =
                    i == 0 ? sk.player1 : sk.player2;
            
                Melon<TweaksAndFixes>.Logger.Msg($"  Player {currPlayer.data.name} ({i})");
            
                foreach (var sType in currSkPlayer.shipAmounts.ToList())
                {
                    if (!currSkPlayer.shipTypeAvailible.ValOrDef(sType.Key, false))
                        continue;

                    int sDesignNextNum = 1;
            
                    Melon<TweaksAndFixes>.Logger.Msg($"    Ship Type {sType.Key.nameUi}");
            
                    foreach (var sDesign in sType.Value.ToList())
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"      Design #{sDesignNextNum}");
            
                        if (isRestart || currSkPlayer.shipInstances.HasValue(sDesign.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"        Manual/reset design {currSkPlayer.shipInstances[sDesign.Key].Name(false, false)} : {currSkPlayer.shipInstances[sDesign.Key].id}"
                            );

                            sharedIgnoreList.Add(currSkPlayer.shipInstances[sDesign.Key].id);
                        }

                        else if (sk.useShared)
                        {
                            // Pull shared design
                            var sd = CampaignControllerM.GetSharedDesign(
                                CampaignController.Instance, currPlayer, sType.Key,
                                G.ui.skirmishSetup.player1.year, false, true, sharedIgnoreList
                            );

                            if (sd == null)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg(
                                    $"        Shared design {sType.Key.nameFull} #{sDesignNextNum}: Failed!"
                                );
                            }
                            else
                            {
                                Melon<TweaksAndFixes>.Logger.Msg(
                                    $"        Shared design {sd.Name(false, false)} : {sd.id}"
                                );
                                currSkPlayer.shipInstances.AddOrSet(sDesign.Key, sd);
                                currSkPlayer.shipDesigns.AddOrSet(sDesign.Key, sd.ToStore());
                                sharedIgnoreList.Add(sd.id);
                            }
                        }

                        if (sk.usePredefs && sType.Key.canBuild && !currSkPlayer.shipInstances.HasValue(sDesign.Key))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"        Loading predefs"
                            );

                            var cc = CampaignController.Instance;
                            bool failed = false;

                            if (cc._currentDesigns == null)
                            {
                                if (!PredefinedDesignsData.Instance.LoadPredefSets(true))
                                {
                                    Melon<TweaksAndFixes>.Logger.BigError("Tried to load predefined designs but failed!");
                                    sk.usePredefs = false;
                                    failed = true;
                                }
                            }

                            int sDesignNum = sDesignNextNum++;

                            G.ui._loadingText_k__BackingField =
                                ModUtils.LocalizeF("$Ui_Battle_Building0", $"{currPlayer.Name(false)} - {sType.Key.nameFull} #{sDesignNum}");

                            for (int k = 0; !failed && k < 20; k++)
                            {
                                var pd = PredefinedDesignsData.Instance.GetRandomShip(currPlayer, sType.Key, currSkPlayer.year);
                                var ps = Ship.Create(null, null, false, false, false);

                                if (!ps.FromStore(pd, new Il2CppSystem.Nullable<Il2CppSystem.Guid>(), null, null, false))
                                {
                                    Melon<TweaksAndFixes>.Logger.Error(
                                        $"Failed to load predef {pd.vesselName} for {currPlayer.Name(false)} ({currSkPlayer.year})." +
                                        $" Trying again..."
                                    );
                                    ps.Erase();
                                    continue;
                                }

                                if (predefIgnoreList.Contains(pd.id))
                                {
                                    ps.Erase();
                                    continue;
                                }

                                predefIgnoreList.Add(pd.id);

                                ps.CrewTrainingAmount = (float)(System.Random.Shared.NextDouble() * 100.0);
                                currSkPlayer.shipInstances.AddOrSet(sDesign.Key, ps);
                                currSkPlayer.shipDesigns.AddOrSet(sDesign.Key, pd);

                                Melon<TweaksAndFixes>.Logger.Msg(
                                    $"        Predef design {ps.Name(false, false)} : {ps.id}"
                                );

                                break;
                            }

                            yield return new WaitForEndOfFrame();
                        }

                        if (!currSkPlayer.shipInstances.HasValue(sDesign.Key))
                        {
                            // Generate

                            int sDesignNum = sDesignNextNum++;

                            G.ui._loadingText_k__BackingField =
                                ModUtils.LocalizeF("$Ui_Battle_Building0", $"{currPlayer.Name(false)} - {sType.Key.nameFull} #{sDesignNum}");

                            bool done = false;

                            var onDone = new System.Action<Ship>((Ship s) =>
                            {
                                done = true;

                                if (s == null || s.status == VesselEntity.Status.Erased)
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg(
                                        $"        Finished {sType.Key.nameFull} #{sDesignNum}: Failed!"
                                    );
                                    return;
                                }

                                Melon<TweaksAndFixes>.Logger.Msg(
                                    $"        Finished {sType.Key.nameFull} #{sDesignNum}: {s.Name(false, false)}:" +
                                    $" {s.parts.Count} parts, {s.mainGuns?.Count} guns"
                                );

                                currSkPlayer.shipInstances.AddOrSet(sDesign.Key, s);
                                currSkPlayer.shipDesigns.AddOrSet(sDesign.Key, s.ToStore());
                            });

                            G.ui.StartCoroutine_Auto(ShipM.Ship_CreateRandom(
                                sType.Key, currPlayer, sType.Key.name == "tr", true, onDone, false, false
                            ));

                            while (!done)
                            {
                                yield return new WaitForEndOfFrame();
                            }

                            Melon<TweaksAndFixes>.Logger.Msg(
                                $"        Generate design " +
                                $"{currSkPlayer.shipInstances[sDesign.Key].Name(false, false)}"
                            );

                            // Ask player if they want to continue after failing to gen ship
                            //   If no, set canContinue = false
                        }

                        // Create full count
                        var design = currSkPlayer.shipInstances[sDesign.Key];
                        
                        design.UpdateOwner();
                        
                        var ships = PlayerController.Instance.BuildShipsFromDesign(
                            design, sDesign.Value, true, null
                        );

                        foreach (var ship in ships)
                        {
                            ship._isTempForBattle_k__BackingField = true;
                            Melon<TweaksAndFixes>.Logger.Msg($"          Make ship from design: {ship.Name(false, false)} : {ship.id}");
                        }
                    }
                }

                currSkPlayer.shipInstances.Clear();
            }
            
            Melon<TweaksAndFixes>.Logger.Msg($"  Finished generating");
            
            G.ui._loadingText_k__BackingField = ModUtils.LocalizeF("$Ui_Battle_StartingBattle");
            
            var battle = new CampaignBattle();
            battle.CurrentState = 0;
            battle.Date = CampaignController.Instance.CurrentDate;
            battle.Type = G.GameData.battleTypesEx["custom_battle"];
            
            battle.Attacker = player;
            battle.AttackerShips = new(player.fleet);
            battle.Defender = enemy;
            battle.DefenderShips = new(enemy.fleet);
            
            Melon<TweaksAndFixes>.Logger.Msg($"  Configured battle");
            
            G.sound.PlayMusic("music_battle_start", true, false);
            
            Melon<TweaksAndFixes>.Logger.Msg($"  Playing music:");
            
            G.ui._dontChangeLoadingScreen_k__BackingField = true;
            GameManager.Instance.ToBattle(
                battle,
                new Il2CppSystem.Nullable<float>(36000),
                new Il2CppSystem.Nullable<float>(G.ui.skirmishSetup.distance),
                new Il2CppSystem.Nullable<float>(1000)
            );
            BattleManager.Instance.StartDistance = G.ui.skirmishSetup.distance;
            BattleManager.Instance.StartSpread = 1000;
            battle.Timer.Run(36000);

            Melon<TweaksAndFixes>.Logger.Msg($"  Battle start");

            // Start battle corutine
            yield break;
        }
    }


    [HarmonyPatch(typeof(BattleManager._UpdateLoadingMissionBuild_d__113))]
    internal class Patch_BattleManager_d115
    {

        // For some reason we can't access native nullables
        // so we have to cache off these custom and limit values
        // for armor and speed so they'll be accessible to our
        // patched AdjustHullStats method (see Ship GenerateRandomShip
        // coroutine patch).
        internal class BattleShipGenerationInfo
        {
            public bool isActive = false;
            public float limitArmor = -1f;
            public float limitSpeed = -1f;
            public float customSpeed = -1f;
            public float customArmor = -1f;
        }

        internal static readonly BattleShipGenerationInfo _ShipGenInfo = new BattleShipGenerationInfo();

        [HarmonyPatch(nameof(BattleManager._UpdateLoadingMissionBuild_d__113.MoveNext))]
        [HarmonyPrefix]
        internal static void Prefix_MoveNext(BattleManager._UpdateLoadingMissionBuild_d__113 __instance, out int __state)
        {
            __state = __instance.__1__state;
            if (__state == 3 || __state == 5)
            {
                _ShipGenInfo.isActive = true;
                var cm = __instance.__4__this.CurrentAcademyMission;
                if (__instance._isEnemy_5__5)
                {
                    _ShipGenInfo.limitArmor = cm.easyArmor;
                    if (_ShipGenInfo.limitArmor < 0f)
                        _ShipGenInfo.limitArmor = cm.normalArmor;

                    _ShipGenInfo.limitSpeed = cm.easySpeed;
                    if (_ShipGenInfo.limitSpeed < 0f)
                        _ShipGenInfo.limitSpeed = cm.normalSpeed;
                    if (_ShipGenInfo.limitSpeed > 0f)
                        _ShipGenInfo.limitSpeed *= ShipM.KnotsToMS;

                    if (cm.paramx.TryGetValue("armor", out var cArm))
                        _ShipGenInfo.customArmor = float.Parse(cArm[0], ModUtils._InvariantCulture);
                    else
                        _ShipGenInfo.customArmor = -1f;

                    if (cm.paramx.TryGetValue("speed", out var cSpd))
                        _ShipGenInfo.customSpeed = float.Parse(cSpd[0], ModUtils._InvariantCulture) * ShipM.KnotsToMS;
                    else
                        _ShipGenInfo.customSpeed = -1f;
                }
            }
        }

        [HarmonyPatch(nameof(BattleManager._UpdateLoadingMissionBuild_d__113.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(BattleManager._UpdateLoadingMissionBuild_d__113 __instance, int __state)
        {
            _ShipGenInfo.isActive = false;
            _ShipGenInfo.limitArmor = -1f;
            _ShipGenInfo.limitSpeed = -1f;
            _ShipGenInfo.customArmor = -1f;
            _ShipGenInfo.customSpeed = -1f;
        }
    }
}
