using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(Shell))]
    internal class Patch_Shell
    {

        public static Dictionary<Shell, Vector3> shellTargetData = new();

        public static Shell updating;

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
                if (__instance.willHitTarget) Melon<TweaksAndFixes>.Logger.Msg($"Shell hit! {shellTargetData.Count}");

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
