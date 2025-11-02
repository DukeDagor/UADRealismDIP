using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Diagnostics;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(LocalizeManager))]
    internal class Patch_LocalizeManager
    {
        private static readonly HashSet<string> _SeenKeys = new HashSet<string>();

        private static bool _Initialized = false;

        private static int LoadLocFromFile(LocalizeManager.LanguagesData __result, FilePath file, bool clobber)
        {
            if (!file.Exists)
                return -1;

            var lines = File.ReadAllLines(file.path);

            for (int j = 0; j < lines.Length; ++j)
            {
                var line = lines[j];
                var split = line.Split(';');
                if (split.Length < 2)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error loading language file {file.name}, line {j + 1} `{line}` lacks key or value");
                    continue;
                }

                string key = split[0];
                if (_SeenKeys.Contains(key))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error loading language file {file.name}, line {j + 1} `{line}` is a duplicate key");
                    continue;
                }
                _SeenKeys.Add(key);
                if (!clobber && __result.Data.ContainsKey(key))
                    continue;

                string[] newArr = new string[split.Length - 1];
                for (int i = 1; i < split.Length; ++i)
                    newArr[i - 1] = LocalizeManager.__c.__9__24_0.Invoke(split[i]);

                __result.Data[key] = newArr;
            }
            int count = _SeenKeys.Count;
            _SeenKeys.Clear();
            return count;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(LocalizeManager.LoadLanguage))]
        internal static void Postfix_LoadLanguage(LocalizeManager __instance, string currentLanguage, ref LocalizeManager.LanguagesData __result)
        {
            if (!_Initialized)
            {
                _Initialized = true;
            }
            else
            {
                return;
            }

                int overrideCount = LoadLocFromFile(__result, new FilePath(FilePath.DirType.ModsDir, currentLanguage + ".lng"), true);
            if(overrideCount >= 0)
                Melon<TweaksAndFixes>.Logger.Msg($"Overriding language {currentLanguage} with {overrideCount} lines");

            if (LoadLocFromFile(__result, Config._LocFile, false) < 0)
                Melon<TweaksAndFixes>.Logger.Error($"Unable to find base TAF loc file {Config._LocFile} in {Config._DataDir}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizeManager))]
        [HarmonyPatch("Localize", new Type[]
        {
            typeof(string),
            typeof(Il2CppReferenceArray<Il2CppSystem.Object>)
        })]
        internal static void FixLocalize(ref string tag, Il2CppReferenceArray<Il2CppSystem.Object> p)
        {
            if (GameManager.Instance != null && GameManager.IsConstructor && 
                (p == null || ((Il2CppArrayBase<Il2CppSystem.Object>)(object)p).Length <= 0) && 
                !(tag != "$Ui_World_PopWindows_Port") && LocalizeManager.Instance.Language.Data.ContainsKey("$Ui_Constr_Port")
            )
            {
                tag = "$Ui_Constr_Port";
            }
        }
    }
}
