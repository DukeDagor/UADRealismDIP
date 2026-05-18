using HarmonyLib;
using Il2Cpp;
using MelonLoader;

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

        [HarmonyPatch(nameof(Settings.LoadCustomBattleData))]
        [HarmonyPrefix]
        internal static bool Prefix_LoadCustomBattleData()
        {
            if (File.Exists(Storage.prefix + "custom_battle_data.bin"))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Deleting deprecated file: '{Storage.prefix + "custom_battle_data.bin"}'");
                File.Delete(Storage.prefix + "custom_battle_data.bin");
            }
            return false;
        }

        [HarmonyPatch(nameof(Settings.SaveCustomBattleData))]
        [HarmonyPrefix]
        internal static bool Prefix_SaveCustomBattleData()
        {
            return false;
        }
    }
}
