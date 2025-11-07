using Il2Cpp;
using MelonLoader;

namespace TweaksAndFixes.Data
{
    internal class AccuraciesExInfo : Serializer.IPostProcess
    {
        private static readonly Dictionary<string, AccuraciesExInfo> _Data = new Dictionary<string, AccuraciesExInfo>();

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
        [Serializer.Field] public string comment = string.Empty;

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

        public static bool UpdateAccuracyInfo(ref string name, ref string subname, ref float accuracy)
        {
            string key = subname + name;

            // Check for the combined sub+name key.
            if (!_Data.ContainsKey(key))
            {
                key = name;

                // For catch-all entries (Ex: Base, 1.1km), check for the name only. 
                if (!_Data.ContainsKey(key))
                {
                    return false;
                }
            }

            // if the bonus is disabled, return 1 (0%) to prevent it from displaying in-game.
            if (_Data[key].enabled == 0)
            {
                accuracy = 1;
            }
            // Base accuracy starts at 0% instead of -100%, so it should not be offset.
            else if (name == "Base")
            {
                accuracy = _Data[key].UpdateAccuracy(accuracy, false);
            }
            // For all other -100% based accuracy multipliers, use offset.
            else
            {
                accuracy = _Data[key].UpdateAccuracy(accuracy);
            }

            // If a name override exists
            if (_Data[key].name_override.Length != 0)
            {
                name = LocalizeManager.Localize(_Data[key].name_override);
            }

            // If a sub-name override exists
            if (_Data[key].subname_override.Length != 0)
            {
                subname = LocalizeManager.Localize(_Data[key].subname_override);
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
