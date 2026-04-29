using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Reflection;

namespace TweaksAndFixes.Harmony
{
    internal static class GGDesignerAutodesign
    {
        private static int _suppressClearShipDepth;

        internal static bool IsDesignerContext()
        {
            return Patch_GameManager.CurrentSubGameState == Patch_GameManager.SubGameState.InSharedDesigner
                || Patch_GameManager.CurrentSubGameState == Patch_GameManager.SubGameState.InConstructorNew
                || Patch_GameManager.CurrentSubGameState == Patch_GameManager.SubGameState.InConstructorExisting;
        }

        internal static bool ShouldSuppressClearShip(Ui ui)
        {
            return ui != null && _suppressClearShipDepth > 0 && IsDesignerContext();
        }

        internal static void BeginSuppressClearShip(string reason)
        {
            if (!IsDesignerContext())
                return;

            _suppressClearShipDepth++;
            Melon<TweaksAndFixes>.Logger.Msg($"Designer autodesign: preserving current ship on {reason}.");
        }

        internal static void EndSuppressClearShip()
        {
            if (_suppressClearShipDepth > 0)
                _suppressClearShipDepth--;
        }
    }

    [HarmonyPatch(typeof(Ui), nameof(Ui.ClearShip))]
    internal class Patch_GGDesignerAutodesign_ClearShip
    {
        [HarmonyPrefix]
        internal static bool Prefix_ClearShip(Ui __instance)
        {
            // Designer autodesign normally clears the ship after a failed run or a user stop.
            // When those callers set the guard, leave the current partial design visible instead.
            return !GGDesignerAutodesign.ShouldSuppressClearShip(__instance);
        }
    }

    [HarmonyPatch(typeof(Ui), nameof(Ui.StopShipGeneration))]
    internal class Patch_GGDesignerAutodesign_StopShipGeneration
    {
        [HarmonyPrefix]
        internal static void Prefix_StopShipGeneration()
        {
            // The stop button path stops the coroutine and then calls ClearShip before ending autodesign.
            // Suppress only that cleanup so user interruption keeps the current designer state intact.
            if (GameManager.IsAutodesignActive)
                GGDesignerAutodesign.BeginSuppressClearShip("user interruption");
        }

        [HarmonyPostfix]
        internal static void Postfix_StopShipGeneration()
        {
            GGDesignerAutodesign.EndSuppressClearShip();
        }

        [HarmonyFinalizer]
        internal static void Finalizer_StopShipGeneration()
        {
            GGDesignerAutodesign.EndSuppressClearShip();
        }
    }

    [HarmonyPatch]
    internal class Patch_GGDesignerAutodesign_RandomShipDone
    {
        private static MethodBase? _targetMethod;

        [HarmonyPrepare]
        internal static bool Prepare()
        {
            // Il2Cpp interop does not always expose compiler-generated callback names exactly
            // as Cpp2IL prints them. Match the RandomShip completion callback by signature.
            foreach (MethodBase method in AccessTools.GetDeclaredMethods(typeof(Ui.__c__DisplayClass591_0)))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(bool)
                    && parameters[1].ParameterType == typeof(int)
                    && parameters[2].ParameterType == typeof(float))
                {
                    _targetMethod = method;
                    return true;
                }
            }

            Melon<TweaksAndFixes>.Logger.Warning("Designer autodesign: RandomShip completion callback not found; failure preservation patch disabled.");
            return false;
        }

        internal static MethodBase TargetMethod()
        {
            return _targetMethod;
        }

        [HarmonyPrefix]
        internal static void Prefix_RandomShipDone(bool result)
        {
            // Ui.RandomShip's completion callback clears the ship when GenerateRandomShip reports failure.
            // Keep the generated/partial layout in the designer so the failure can be inspected or adjusted.
            if (!result)
                GGDesignerAutodesign.BeginSuppressClearShip("generation failure");
        }

        [HarmonyPostfix]
        internal static void Postfix_RandomShipDone()
        {
            GGDesignerAutodesign.EndSuppressClearShip();
        }

        [HarmonyFinalizer]
        internal static void Finalizer_RandomShipDone()
        {
            GGDesignerAutodesign.EndSuppressClearShip();
        }
    }
}
