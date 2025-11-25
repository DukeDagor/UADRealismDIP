using Il2Cpp;
using MelonLoader;
using static UnityEngine.GraphicsBuffer;

namespace TweaksAndFixes.Data
{
    internal class AccuraciesExInfo : Serializer.IPostProcess
    {
        private static readonly Dictionary<string, AccuraciesExInfo> _Data = new Dictionary<string, AccuraciesExInfo>();

        private static readonly Dictionary<string, string> _LocalizationsIgnore = new()
        {
            { "Gun",                        "$Ui_Battle_Gun"},
            { "Crew Training",              "$Ui_Battle_CrewTraining"},
            { "Guns Grade",                 "$Accuracies_Guns_Grade"},
            { "Time",                       "$Accuracies_Time"},
            { "Weather",                    "$Accuracies_Weather"},
            { "Wind",                       "$Accuracies_Wind"},
            { "Sea Waves",                  "$Accuracies_Sea_Waves"},
            { "Sun Glare",                  "$Accuracies_Sun_Glare"},
        };

        private static readonly Dictionary<string, HashSet<string>> _LocalizedIgnoreKeys = new();

        private static readonly Dictionary<string, string> _Localizations = new() {
            { "",                           ""},
            { "Base",                       "$Ui_Battle_Base"},
            { "range Xkm",                  "$Ui_Battle_range0km"},
            { "Out of Range",               "$Ui_Battle_OutOfRange"},
            { "Range finding",              "$Ui_Battle_RangeFinding"},
            { "Flagship Communications",    "$Ui_Battle_FlagshipCommunications"},
            { "Far from Flagship",          "$Ui_Battle_FarFromFlagship"},
            { "Flooding Instability",       "$Ui_Battle_FloodingInstability"},
            { "Damage Instability",         "$Ui_Battle_DamageInstability"},
            { "Gun Recoil",                 "$Ui_Battle_GunRecoil"},
            { "Engine Vibrations",          "$Ui_Battle_EngineVibrations"},
            { "Guns of Different Barrels",  "$Ui_Battle_GunsOfDifferentBarrels"},
            { "Not Centerlined Guns",       "$Ui_Battle_NotCenterlinedGuns"},
            { "Various Funnel Emissions",   "$Ui_Battle_VariousFunnelEmissions"},
            { "Thick Smoke from own Funnel","$Ui_Battle_ThickSmokeFromOwnFunnel"},
            { "Own Guns' Splashes",         "$Ui_Battle_OwnGunsSplashes"},
            { "Other Guns' Splashes",       "$Ui_Battle_OtherGunsSplashes"},
            { "Own Maneuver",               "$Ui_Battle_OwnManeuver"},
            { "Own Cruise Speed",           "$Ui_Battle_OwnCruiseSpeed"},
            { "Shooting through Smoke",     "$Ui_Battle_ShootingThroughSmoke"},
            { "Range Found",                "$Ui_Battle_RangeFound"},
            { "Conning Tower damaged",      "$Ui_Battle_ConningTowerDamaged"},
            { "Fire Control damaged",       "$Ui_Battle_FireControlDamaged"},
            { "Target Ship Size",           "$Ui_Battle_TargetShipSize"},
            { "Target's Maneuver",          "$Ui_Battle_TargetManeuver"},
            { "Target's Slow Speed",        "$Ui_Battle_TargetSlowSpeed"},
            { "Target's Fast Speed",        "$Ui_Battle_TargetFastSpeed"},
            { "Target behind Smoke",        "$Ui_Battle_TargetBehindSmoke"},
            { "Technologies",               "$Ui_Battle_Technologies"},
            { "Long-Range Techs & Tower",   "$Ui_Battle_LongRangeTechsTower"},
            { "Hull Stability & Tower",     "$Ui_Battle_HullStabilityTower"},
            { "1-barrels Turret Tech",      "$Ui_Battle_0barrelsTurretTech"},
            { "2-barrels Turret Tech",      "$Ui_Battle_0barrelsTurretTech"},
            { "3-barrels Turret Tech",      "$Ui_Battle_0barrelsTurretTech"},
            { "4-barrels Turret Tech",      "$Ui_Battle_0barrelsTurretTech"},
            { "Casemate Tech",              "$Ui_Battle_CasemateTech"}
        };

        private static readonly Dictionary<string, Dictionary<string, string>> _LocalizedKeys = new();

        [Serializer.Field] public string name = string.Empty;
        [Serializer.Field] public string subname = string.Empty;
        [Serializer.Field] public int enabled = 0;
        [Serializer.Field] public string name_override = string.Empty;
        [Serializer.Field] public string subname_override = string.Empty;
        [Serializer.Field] public float replace = 0;
        [Serializer.Field] public float multiplier = 0;
        [Serializer.Field] public float bonus = 0;
        [Serializer.Field] public float min = 0;
        [Serializer.Field] public float max = 0;

        public static bool HasEntries()
        {
            return _Data.Count > 0;
        }

        // Check values
        public void PostProcess()
        {
            if (replace < -100 && replace != -101f)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"AccuraciesEx: `{name}` has invalid replace value `{replace}`. Must be greater than -100.");
                replace = -101;
            }
            
            if (max == -101f)
            {
                max = float.PositiveInfinity;
            }

            if (min < -100)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"AccuraciesEx: `{name}` has invalid minimum value `{min}`. Must be greater than -100.");
            }
            
            if (max < -100)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"AccuraciesEx: `{name}` has invalid maximum value `{max}`. Must be greater than -100.");
            }
            
            if (min >= max)
            {
                Melon<TweaksAndFixes>.Logger.Warning($"AccuraciesEx: `{name}` has invalid minimum `{min}` and maximum `{max}`. Min must be less than max.");
            }

            _Data[subname + name] = this;
        }

        // Update accuracy based on replacement, multiplier, bonus, min, and max with optional offset for values where 0 = -100%.
        public float UpdateAccuracy(float baseAccuracy, bool applyOffset = true)
        {
            baseAccuracy = applyOffset ? (baseAccuracy - 1f) * 100f : baseAccuracy * 100f;

            // Equation: replace or Clamp(x * multiplier + bonus, min, max)
            baseAccuracy = (replace != -101 ? replace : Math.Clamp(baseAccuracy * multiplier + bonus, min, max));

            return applyOffset ? (baseAccuracy / 100f) + 1f : baseAccuracy / 100f;
        }

        private static void LocalizeKeys()
        {
            _LocalizedKeys[LocalizeManager.CurrentLanguage] = new();
            var langKeys = _LocalizedKeys[LocalizeManager.CurrentLanguage];
            langKeys[""] = "";
            langKeys["range Xkm"] = ModUtils.LocalizeF(_Localizations["range Xkm"]);

            _LocalizedIgnoreKeys[LocalizeManager.CurrentLanguage] = new();
            var ignoreKeys = _LocalizedIgnoreKeys[LocalizeManager.CurrentLanguage];

            foreach (var ignore in _LocalizationsIgnore)
            {
                ignoreKeys.Add(ModUtils.LocalizeF(ignore.Value));
            }

            foreach (var data in _Data)
            {
                if (data.Key == "default") continue;
                string name = data.Value.name;
                string subname = data.Value.subname;

                if (subname.EndsWith("km")) subname = "range Xkm";

                string localizedKey = 
                    ModUtils.LocalizeF(_Localizations[subname]) + 
                    ModUtils.LocalizeF(_Localizations[name]);

                if (localizedKey.Contains("{0}"))
                {
                    if (!subname.EndsWith("km"))
                    {
                        string num = "" + name[0];

                        localizedKey = string.Format(localizedKey, num);
                        // Melon<TweaksAndFixes>.Logger.Msg($"  {localizedKey} : {num}");
                    }
                }

                langKeys[localizedKey] = data.Key;
                
                // Melon<TweaksAndFixes>.Logger.Msg($"  {data.Key} -> {localizedKey}");
            }
        }

        private static string ExtractNumber(string s)
        {
            int start = -1;
            int end = -1;

            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] - 48 >= 0 && s[i] - 48 < 10)
                {
                    if (end == -1) end = i + 1;
                }
                else if (s[i] != '.' && end != -1 && start == -1)
                {
                    start = i + 1;
                    break;
                }
            }

            if (start == -1 || end == -1)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"{start} -> {end}");

                return "";
            }

            return s.Substring(start, end - start);
        }

        public static bool UpdateAccuracyInfo(ref string name, ref string subname, ref float accuracy)
        {
            // Check if the language changed
            if (!_LocalizedKeys.ContainsKey(LocalizeManager.CurrentLanguage))
            {
                LocalizeKeys();
            }

            // Check if the name or subname is ignoerd
            if (_LocalizedIgnoreKeys[LocalizeManager.CurrentLanguage].Contains(name)
                || _LocalizedIgnoreKeys[LocalizeManager.CurrentLanguage].Contains(subname))
            {
                return true;
            }

            var localizedKeys = _LocalizedKeys[LocalizeManager.CurrentLanguage];

            name ??= "";
            subname ??= "";

            if (!localizedKeys.ContainsKey(name))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Can't find name `{name}`");

                return false;
            }

            string locName = localizedKeys[name];
            bool isBase = locName == "Base";

            if (!localizedKeys.ContainsKey(subname) && !isBase)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Can't find subname `{subname}`");

                return false;
            }

            string locSubname;

            if (isBase && subname.Length > 0)
            {
                string subNameNum = ExtractNumber(subname);

                if (subNameNum.Length != 0)
                    locSubname = string.Format("range {0}km", subNameNum);
                else
                    locSubname = string.Empty;

                // Melon<TweaksAndFixes>.Logger.Msg($"Update: `{subname}` : `{locSubname}` : `{subNameNum}`");
            }
            else
            {
                locSubname = localizedKeys[subname];
            }

            string locKey = locSubname + locName;

            if (!_Data.ContainsKey(locKey))
            {
                locKey = locName;

                if (!_Data.ContainsKey(locKey))
                {
                    return false;
                }
            }

            // if the bonus is disabled, return 1 (0%) to prevent it from displaying in-game.
            if (_Data[locKey].enabled == 0)
            {
                accuracy = 1;
            }
            // Base accuracy starts at 0% instead of -100%, so it should not be offset.
            else if (isBase)
            {
                accuracy = _Data[locKey].UpdateAccuracy(accuracy, false);
            }
            // For all other -100% based accuracy multipliers, use offset.
            else
            {
                accuracy = _Data[locKey].UpdateAccuracy(accuracy);
            }

            // If a name override exists
            if (_Data[locKey].name_override.Length != 0)
            {
                name = ModUtils.LocalizeF(_Data[locKey].name_override);
            }

            // If a sub-name override exists
            if (_Data[locKey].subname_override.Length != 0)
            {
                subname = ModUtils.LocalizeF(_Data[locKey].subname_override);
            }

            return true;
        }

        // Load CSV with comment lines and a default line.
        public static void LoadData()
        {
            FilePath fp = Config._AccuraciesExFile;
            if (!fp.Exists)
            {
                return;
            }

            List<AccuraciesExInfo> list = new List<AccuraciesExInfo>();
            string? text = Serializer.CSV.GetTextFromFile(fp.path);

            if (text == null)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Failed to load `AccuraciesEx.csv`.");
                return;
            }

            Serializer.CSV.Read<List<AccuraciesExInfo>, AccuraciesExInfo>(text, list, true, true);

            Melon<TweaksAndFixes>.Logger.Msg($"Loaded {list.Count} accuracy rules.");
        }
    }
}
