using HarmonyLib;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Settings))]
    internal class Patch_Settings
    {
        [HarmonyPatch(nameof(Settings.ApplyOnInit))]
        [HarmonyPostfix]
        internal static void Postix_Load()
        {
            UiM.LoadSettings();
        }

        [HarmonyPatch(nameof(Settings.Save))]
        [HarmonyPostfix]
        internal static void Postix_Save()
        {
            UiM.SaveSettings();
        }
    }
}
