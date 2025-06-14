using MelonLoader;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Il2Cpp;

[assembly: MelonGame("Game Labs", "Ultimate Admiral Dreadnoughts")]
[assembly: MelonInfo(typeof(TweaksAndFixes.TweaksAndFixes), "TweaksAndFixes", "3.18.3", "NathanKell & DukeDagor")]
[assembly: MelonColor(255, 220, 220, 0)]
[assembly: HarmonyDontPatchAll]

namespace TweaksAndFixes
{
    public class TweaksAndFixes : MelonMod
    {
        private bool _LocLoaded = false;
        private readonly Dictionary<string, string> _localLoc = new Dictionary<string, string>();
        private bool _showVersionError = false;

        public override void OnInitializeMelon()
        {
            try
            {
                HarmonyInstance.PatchAll(MelonAssembly.Assembly);
            }
            catch (Exception e)
            {
                Melon<TweaksAndFixes>.Logger.BigError($"Exception patching with harmony: {e.GetType()}:\n{e}");
                _showVersionError = true;
            }

            base.OnInitializeMelon();
        }

        public override void OnLateInitializeMelon()
        {
            try
            {
                bool hasUnityExplorer = false;
                foreach (var mod in MelonMod.RegisteredMelons)
                {
                    if (mod.Info.Name == "UnityExplorer")
                    {
                        hasUnityExplorer = true;
                        break;
                    }
                }
                if (!hasUnityExplorer)
                    Application.add_logMessageReceived(new Action<string, string, LogType>(Application_logMessageReceived));
            }
            catch (Exception ex)
            {
                Melon<TweaksAndFixes>.Logger.Warning("Exception setting up Unity log listener, make sure Unity libraries have been unstripped!\n" + ex);
            }
            if (_showVersionError)
            {
                LoadLoc();
                MessageBoxUI.Show(_localLoc["$TAF_LoadError_HarmonyFail_Title"], _localLoc["$TAF_LoadError_HarmonyFail_Text"], null, false, null, null, new System.Action(() => { GameManager.Quit(); }));
            }
            if (!Config.RequiredFilesExist())
            {
                LoadLoc();
                MessageBoxUI.Show(_localLoc["$TAF_LoadError_DataFail_Title"], _localLoc["$TAF_LoadError_DataFail_Text"], null, false, null, null, new System.Action(() => { GameManager.Quit(); }));
            }
        }

        public override void OnDeinitializeMelon()
        {
            base.OnDeinitializeMelon();
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            string logStr = $"[Unity]: {condition ?? string.Empty}";
            switch (type)
            {
                case LogType.Log: Melon<TweaksAndFixes>.Logger.Msg(logStr); break;
                case LogType.Warning: Melon<TweaksAndFixes>.Logger.Warning(logStr); break;
                case LogType.Error:
                case LogType.Exception:
                    Melon<TweaksAndFixes>.Logger.Error($"[Unity]: {condition}\n{stackTrace}");
                    //Melon<TweaksAndFixes>.Logger.Error(logStr);
                    break;
            }
        }

        private void LoadLoc()
        {
            if (_LocLoaded)
                return;

            _localLoc["$TAF_LoadError_HarmonyFail_Title"] = "Version Mismatch";
            _localLoc["$TAF_LoadError_HarmonyFail_Text"] = "The installed version of <b>Tweaks and Fixes</b> doesn't match the installed version of Ultimate Admiral: Dreadnoughts. Patching has failed and your game will not funciton. Please either downgrade UAD to the correct version for your version of TAF, or download the correct version of TAF if available.";

            _localLoc["$TAF_LoadError_DataFail_Title"] = "TAF Installation Error";
            _localLoc["$TAF_LoadError_DataFail_Text"] = "<b>Tweaks and Fixes</b> was not installed properly and your game will not function. Please make sure you unzip the correct release of TAF to your Ultimate Admiral: Dreadnoughts folder and overwrite files if prompted. It is not enough to just copy the DLL.";

            if(Config._LocFile.Exists)
            {
                var lines = File.ReadAllLines(Config._LocFile.path);
                foreach (var l in lines)
                {
                    int idx = l.IndexOf(';');
                    if (idx < 0 || idx >= l.Length - 1)
                        continue;
                    _localLoc[l.Substring(0, idx)] = l.Substring(idx + 1);
                }
            }
            // TODO:
            // To get ingame language before LocalizeManager:
            //LocalizeManager.CurrentLanguageIndex pulls from G.settings.languageIndex (if PlayerPrefs haskey LanguageSelectedManually ) or from steamworks steamAPI
        }
    }
}
