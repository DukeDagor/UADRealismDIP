using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;

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
        internal static void Prefix_CombatUpdateTimeSpeedLimit()
        {
            InUpdateSpeedLimit = true;
        }
        
        [HarmonyPatch(nameof(BattleManager.CombatUpdateTimeSpeedLimit))]
        [HarmonyPostfix]
        internal static void Postfix_CombatUpdateTimeSpeedLimit()
        {
            InUpdateSpeedLimit = false;

            SetTimeSpeedLimit();
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
