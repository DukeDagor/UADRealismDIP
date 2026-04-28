using HarmonyLib;
using Il2Cpp;
using System.Reflection;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(PlayerController))]
    internal class Patch_PlayerController
    {
        [HarmonyPatch(nameof(PlayerController.CloneShipRaw))]
        [HarmonyPostfix]
        internal static void Postfix_CloneShipRaw(Ship from, ref Ship __result)
        {
            __result.TAFData().OnClonePost(from.TAFData());
        }
    }

    [HarmonyPatch]
    internal class Patch_PlayerController_CanBuildShipsFromDesign
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            Type reasonType = typeof(string).MakeByRefType();

            MethodInfo method = AccessTools.Method(
                typeof(PlayerController),
                nameof(PlayerController.CanBuildShipsFromDesign),
                new[] { typeof(Ship), reasonType });
            if (method != null)
                yield return method;

            method = AccessTools.Method(
                typeof(PlayerController),
                nameof(PlayerController.CanBuildShipsFromDesign),
                new[] { typeof(Ship), typeof(int), reasonType });
            if (method != null)
                yield return method;
        }

        [HarmonyPrefix]
        internal static bool Prefix(Ship design, ref bool __result)
        {
            if (design == null || !design.isRefitDesign)
                return true;

            __result = true;
            return false;
        }
    }
}
