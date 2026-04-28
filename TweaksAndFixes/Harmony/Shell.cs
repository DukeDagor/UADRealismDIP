using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(Shell))]
    internal class Patch_Shell
    {
        private static readonly MethodInfo UseHeSetter = AccessTools.PropertySetter(typeof(Shell), nameof(Shell.useHe));
        private static bool _AutoShellPatchErrorLogged;

        public static Dictionary<Shell, Vector3> shellTargetData = new();

        public static Shell updating;

        [HarmonyPatch(nameof(Shell.Create))]
        [HarmonyPostfix]
        internal static void Postfix_Create(Shell __result, Part from, Ship target, Ship.ShellType shellType)
        {
            if (__result == null || Config.Param("taf_auto_shell_he_pen_logic_enabled", 1) <= 0)
                return;

            try
            {
                if (UseHeSetter == null)
                {
                    LogAutoShellPatchError("missing reflected Shell.useHe setter");
                    return;
                }

                if (shellType != Ship.ShellType.Auto)
                    return;

                Ship fromShip = from?.ship;
                PartData gunData = from?.data;
                if (fromShip == null || gunData == null || target == null)
                    return;

                float range = __result.travelRange;
                if (range <= 0f)
                    range = fromShip.weaponRangesHECache.GetValueOrDefault(gunData);
                if (range <= 0f)
                    range = fromShip.weaponRangesCache.GetValueOrDefault(gunData);
                if (range <= 0f)
                    return;

                Il2CppSystem.Nullable<Ship.ShellType> heShellType = new Il2CppSystem.Nullable<Ship.ShellType>(Ship.ShellType.He);
                Il2CppSystem.Nullable<Vector3> targetVelocity = new Il2CppSystem.Nullable<Vector3>();

                float heBeltPen = Ship.GetPenetration(gunData, fromShip, range, true, heShellType, targetVelocity);
                float heDeckPen = Ship.GetPenetration(gunData, fromShip, range, false, heShellType, targetVelocity);
                float beltArmor = target.GetEffectiveArmor(Ship.A.Belt, null, gunData, fromShip, false);
                float deckArmor = target.GetEffectiveArmor(Ship.A.Deck, null, gunData, fromShip, false);

                float ratio = Mathf.Max(0.01f, Config.Param("taf_auto_shell_he_pen_ratio", 1f));
                float armorFloor = Mathf.Max(0f, Config.Param("taf_auto_shell_he_armor_floor_mm", 1f));
                bool useHe = heBeltPen >= Mathf.Max(beltArmor, armorFloor) * ratio
                             && heDeckPen >= Mathf.Max(deckArmor, armorFloor) * ratio;

                bool vanillaUseHe = __result.useHe;
                UseHeSetter.Invoke(__result, new object[] { useHe });

                if (Config.Param("taf_auto_shell_he_pen_trace", 0) > 0 && vanillaUseHe != useHe)
                {
                    Melon<TweaksAndFixes>.Logger.Msg(
                        $"[AUTO-SHELL] {(useHe ? "HE" : "AP")} over vanilla {(vanillaUseHe ? "HE" : "AP")}: " +
                        $"gun={gunData.name}, range={range / 1000f:0.0}km, " +
                        $"HE pen belt/deck={heBeltPen:0.#}/{heDeckPen:0.#}mm, " +
                        $"armor belt/deck={beltArmor:0.#}/{deckArmor:0.#}mm, ratio={ratio:0.##}");
                }
            }
            catch (Exception ex)
            {
                LogAutoShellPatchError(ex.ToString());
            }
        }

        private static void LogAutoShellPatchError(string details)
        {
            if (_AutoShellPatchErrorLogged)
                return;

            _AutoShellPatchErrorLogged = true;
            Melon<TweaksAndFixes>.Logger.Warning($"[AUTO-SHELL] disabled after error: {details}");
        }

        [HarmonyPatch(nameof(Shell.Update))]
        [HarmonyPrefix]
        internal static void Prefix_Update(Shell __instance)
        {
            if (!shellTargetData.ContainsKey(__instance))
            {
                shellTargetData[__instance] = __instance.transform.position;
            }

            updating = __instance;
        }
        
        [HarmonyPatch(nameof(Shell.Update))]
        [HarmonyPostfix]
        internal static void Postfix_Update(Shell __instance)
        {
            // if (!__instance.willHitTarget) return;

            if (__instance.timer.isDone)
            {
                // if (__instance.willHitTarget) Melon<TweaksAndFixes>.Logger.Msg($"Shell hit! {shellTargetData.Count}");

                if (shellTargetData.ContainsKey(__instance)) shellTargetData.Remove(__instance);
            }

            updating = null;

            // if (!shellTargetData.ContainsKey(__instance))
            // {
            // 
            // }
        }
    }
}
