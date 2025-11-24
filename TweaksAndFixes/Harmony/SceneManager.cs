using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(SceneManager))]
    internal class Patch_SceneManager
    {
        [HarmonyPatch(nameof(SceneManager.LoadSceneAsync))]
        [HarmonyPrefix]
        [HarmonyPatch(new Type[] { typeof(string) })]
        internal static bool Prefix_LoadSceneAsync(string sceneName, ref AsyncOperation __result)
        {
            SceneManager.LoadScene(sceneName);

            __result = Resources.LoadAsync("techGroups");

            return false;
        }
    }
}
