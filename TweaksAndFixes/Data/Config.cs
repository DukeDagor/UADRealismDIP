using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MelonLoader;
using static TweaksAndFixes.Config;

#pragma warning disable CS8601
#pragma warning disable CS8604
#pragma warning disable CS8605
#pragma warning disable CS8618

namespace TweaksAndFixes
{
    public class FilePath
    {
        public enum DirType
        {
            ModsDir,
            DataDir,
            Other,
        }

        public readonly string name;
        public readonly string path;
        public readonly string directory;
        public readonly string subDir;
        public readonly DirType dirType;
        public readonly bool required;

        public FilePath(DirType dir, string file, bool isRequired = false)
        {
            required = isRequired;
            name = file;
            directory = dir == DirType.ModsDir ? Config._BasePath : Config._DataPath;
            dirType = dir;
            path = Path.Combine(directory, file);
            subDir = dirType switch
            {
                DirType.ModsDir => "Mods",
                DirType.DataDir => Config._DataDir,
                _ => "<other path>"
            };
        }

        public FilePath(string fullPath, bool isRequired = false)
        {
            required = isRequired;
            name = Path.GetFileName(fullPath);
            path = fullPath;
            directory = Path.GetDirectoryName(fullPath);
            if (directory == Config._BasePath)
                dirType = DirType.ModsDir;
            else if (directory == Config._DataPath)
                dirType = DirType.DataDir;
            else
                dirType = DirType.Other;
            subDir = dirType switch
            {
                DirType.ModsDir => "Mods",
                DirType.DataDir => Config._DataDir,
                _ => "<other path>"
            };
        }

        public bool Exists => Directory.Exists(directory) && File.Exists(path);
        public bool ExistsIfRequired => !required || Exists;

        public void PrintError()
        {
            Melon<TweaksAndFixes>.Logger.Error($"Could not open file {name} under {subDir}, full path {path}");
        }

        public bool VerifyOrLog()
        {
            if (Exists)
                return true;
            PrintError();
            return false;
        }
    }

    public class Config
    {
        public static int CURRENT_USER_CONFIG_VERSION = 1000;

        public class UserConfig
        {
            public class ConfigNavalInvasionTonnage
            {
                public uint Minimum_Tonnage { get; set; }

                public ConfigNavalInvasionTonnage()
                {
                    Minimum_Tonnage = 25_000;
                }
            }

            public class ConfigFleetTension
            {
                public bool Disable { get; set; }

                public ConfigFleetTension()
                {
                    Disable = true;
                }
            }

            public class ConfigCampaginEndDate
            {
                public uint Campaign_End_Date { get; set; }

                public uint Prompt_Player_About_Retirement_Every_X_Months { get; set; }

                public ConfigCampaginEndDate()
                {
                    Campaign_End_Date = 1965;

                    Prompt_Player_About_Retirement_Every_X_Months = 6;
                }
            }

            public class ConfigMinorAndMediumNationLandInvasions
            {
                public bool Disable_Minor_Nation_Invasions { get; set; }
                public bool Disable_Medium_Nation_Invasions { get; set; }

                public ConfigMinorAndMediumNationLandInvasions()
                {
                    Disable_Minor_Nation_Invasions = true;
                    Disable_Medium_Nation_Invasions = true;
                }
            }

            public int Version { get; set; }
            public ConfigNavalInvasionTonnage Naval_Invasion_Minimum_Area_Tonnage { get; set; }
            public ConfigFleetTension Fleet_Tension { get; set; }
            public ConfigCampaginEndDate Campagin_End_Date { get; set; }
            public ConfigMinorAndMediumNationLandInvasions Minor_And_Medium_Nation_Land_Invasions { get; set; }

            public UserConfig()
            {
                Version = -1;
                Naval_Invasion_Minimum_Area_Tonnage = new ConfigNavalInvasionTonnage();
                Fleet_Tension = new ConfigFleetTension();
                Campagin_End_Date = new ConfigCampaginEndDate();
                Minor_And_Medium_Nation_Land_Invasions = new ConfigMinorAndMediumNationLandInvasions();
            }
        }
        
        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
        public class ConfigParse : System.Attribute
        {
            public string _name;
            public string _param;
            public float _checkValue = 0;
            public bool _invertCheck = true;
            public float _exceptVal = 0;
            public bool _log = true;

            public ConfigParse(string n, string p, bool useTAF = true)
            {
                _name = n;
                if (useTAF)
                    _param = "taf_" + p;
                else
                    _param = p;
            }

            public ConfigParse(string n, string p, float c, bool invert = false, bool useTAF = true)
                : this(n, p, useTAF)
            { _checkValue = c; _invertCheck = invert; }

            public ConfigParse(string n, string p, float c, bool invert = false, float e = 0, bool useTAF = true)
                : this(n, p, c, invert, useTAF)
            { _exceptVal = e; }
        }

        public static int MaxGunGrade = 5;
        // TODO: support extending? Support finding?
        public static int MaxTorpGrade = 5;
        public static int MaxTorpBarrels = 5;

        public static int StartingYear = 1890;

        public static UserConfig USER_CONFIG;

        internal static readonly string _BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        internal const string _DataDir = "TAFData";
        internal static readonly string _DataPath = Path.Combine(_BasePath, _DataDir);

        internal static readonly FilePath _FlagFile = new FilePath(FilePath.DirType.ModsDir, "flags.csv");
        internal static readonly FilePath _SpriteFile = new FilePath(FilePath.DirType.ModsDir, "sprites.csv");
        internal static readonly FilePath _GenArmorDataFile = new FilePath(FilePath.DirType.ModsDir, "genarmordata.csv");
        internal static readonly FilePath _GenArmorDefaultsFile = new FilePath(FilePath.DirType.DataDir, "genArmorDefaults.csv", true);
        internal static readonly FilePath _PredefinedDesignsFile = new FilePath(FilePath.DirType.ModsDir, "predefinedDesigns.bin");
        internal static readonly FilePath _PredefinedDesignsDataFile = new FilePath(FilePath.DirType.ModsDir, "predefinedDesignsData.csv");
        internal static readonly FilePath _LocFile = new FilePath(FilePath.DirType.DataDir, "locText.lng");

        public static bool RequiredFilesExist()
        {
            var fields = typeof(Config).GetFields(HarmonyLib.AccessTools.all);
            bool success = true;
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(FilePath))
                    continue;
                FilePath? fp = f.GetValue(null) as FilePath;
                if (fp == null)
                    continue;
                if (!fp.ExistsIfRequired)
                {
                    success = false;
                    Melon<TweaksAndFixes>.Logger.Error($"Missing file: {fp.name} of dir {fp.dirType} (full path {fp.path})");
                }
            }

            return success;
        }

        public enum OverrideMapOptions
        {
            Disabled,
            Enabled,
            DumpData,
            LogDifferences
        }

        [ConfigParse("New Scrapping Behavior", "scrap_enable")]
        public static bool ScrappingChange = false;
        [ConfigParse("Spread Scrapping Checks", "scrap_spread", _log = false)]
        public static bool ScrappingSpread = true;
        [ConfigParse("Ports/Provinces Overriding", "override_map")]
        public static OverrideMapOptions OverrideMap = OverrideMapOptions.Disabled;
        [ConfigParse("Ship Autodesign Tweaks", "shipgen_tweaks")]
        public static bool ShipGenTweaks = true;
        [ConfigParse("Alliance Behavior Tweaks", "alliance_changes")]
        public static bool AllianceTweaks = false;
        [ConfigParse("Use Non-Home Population for Crew", "crew_pool_colony_pop_ratio")]
        public static bool UseColonyInCrewPool = false;
        [ConfigParse("Use Improved Armor Generation Defaults", "genarmor_use_defaults")]
        public static bool UseGenArmorDefaults = false;
        [ConfigParse("Don't Force AI Tech with Predefined Designs", "no_force_tech_with_predefs")]
        public static bool DontClobberTechForPredefs = false;
        [ConfigParse("Disallow Predefined Designs in New Campaigns", "force_no_predef_designs")]
        public static bool ForceNoPredefsInNewGames = false;
        [ConfigParse("Peace Checking Improvements", "peace_check")]
        public static bool PeaceCheckOverride = false;

        public static void LoadConfig()
        {
            Melon<TweaksAndFixes>.Logger.Msg("************************************************** Loading config:");
            var fields = typeof(Config).GetFields(HarmonyLib.AccessTools.all);
            foreach (var f in fields)
            {
                var attrib = (ConfigParse?)f.GetCustomAttribute(typeof(ConfigParse));
                if (attrib == null)
                    continue;

                bool shouldLog = attrib._log;
                // Do this to suppress warning message (rather than using .Param)
                if (Il2Cpp.G.GameData.parms.TryGetValue(attrib._param, out var param))
                {
                    if (f.FieldType.IsEnum)
                    {
                        if (Il2Cpp.G.GameData.paramsRaw.TryGetValue(attrib._param, out var paramObj) && !string.IsNullOrEmpty(paramObj.str))
                        {
                            if (!Enum.TryParse(f.FieldType, paramObj.str, out var eResult))
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"{attrib._name}: Could not parse {paramObj.str}, using default value {eResult}");
                            }
                            else
                            {
                                if (shouldLog)
                                    Melon<TweaksAndFixes>.Logger.Msg($"{attrib._name}: {eResult}");
                            }
                            f.SetValue(null, eResult);
                        }
                        else
                        {
                            var eArray = Enum.GetValues(f.FieldType);
                            int val = (int)param;
                            var eResult = val >= eArray.Length ? eArray.GetValue(0) : eArray.GetValue(val);
                            if (val >= eArray.Length)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"{attrib._name}: Value {val} out of range, using default value {eResult}");
                            }
                            else
                            {
                                if (shouldLog)
                                    Melon<TweaksAndFixes>.Logger.Msg($"{attrib._name}: {eResult}");
                            }
                            f.SetValue(null, eResult);
                        }
                        shouldLog = false;
                    }
                    else
                    {
                        bool isEnabled;
                        if (attrib._invertCheck)
                            isEnabled = param != attrib._checkValue && (attrib._checkValue == attrib._exceptVal || param != attrib._exceptVal);
                        else
                            isEnabled = param == attrib._checkValue;
                        f.SetValue(null, isEnabled);
                    }
                }
                if (shouldLog)
                    Melon<TweaksAndFixes>.Logger.Msg($"{attrib._name}: {(f.FieldType.IsEnum ? f.GetValue(null) : ((bool)(f.GetValue(null)) ? "Enabled" : "Disabled"))}");
            }

            Melon<TweaksAndFixes>.Logger.Msg("************************************************** Loading user config:");

            USER_CONFIG = new UserConfig();

            // An error might occur past this point, can't catch it for some reason tho
            USER_CONFIG = Serializer.JSON.LoadJsonFile<UserConfig>("TweaksAndFixes.cfg");

            if (USER_CONFIG == null || USER_CONFIG.Version == -1)
            {
                Melon<TweaksAndFixes>.Logger.Warning("Failed to load [TweaksAndFixes.cfg]. Using defaults.");
            
                USER_CONFIG = new UserConfig();
            }

            if (USER_CONFIG.Version < CURRENT_USER_CONFIG_VERSION && USER_CONFIG.Version != -1)
            {
                Melon<TweaksAndFixes>.Logger.Warning("TweaksAndFixes.config is out of date. Please check the GitHub for an up-to-date version. Using defaults.");

                USER_CONFIG = new UserConfig();
            }
            
            Melon<TweaksAndFixes>.Logger.Msg("TweaksAndFixes.cfg:");
            Melon<TweaksAndFixes>.Logger.Msg("Version:                                : " + USER_CONFIG.Version);
            Melon<TweaksAndFixes>.Logger.Msg("Naval_Invasion_Minimum_Area_Tonnage");
            Melon<TweaksAndFixes>.Logger.Msg(" |.Minimum_Tonnage                      : " + USER_CONFIG.Naval_Invasion_Minimum_Area_Tonnage.Minimum_Tonnage);
            Melon<TweaksAndFixes>.Logger.Msg("Fleet_Tension");
            Melon<TweaksAndFixes>.Logger.Msg(" |.Disable                              : " + USER_CONFIG.Fleet_Tension.Disable);
            Melon<TweaksAndFixes>.Logger.Msg("Campagin_End_Date");
            Melon<TweaksAndFixes>.Logger.Msg(" |.Campaign_End_Date                    : " + USER_CONFIG.Campagin_End_Date.Campaign_End_Date);
            Melon<TweaksAndFixes>.Logger.Msg(" |.Request_Retirement_Every_X_Months    : " + USER_CONFIG.Campagin_End_Date.Prompt_Player_About_Retirement_Every_X_Months);
            Melon<TweaksAndFixes>.Logger.Msg("Minor_And_Medium_Nation_Land_Invasions");
            Melon<TweaksAndFixes>.Logger.Msg(" |.Disable_Minor_Nation_Invasions       : " + USER_CONFIG.Minor_And_Medium_Nation_Land_Invasions.Disable_Minor_Nation_Invasions);
            Melon<TweaksAndFixes>.Logger.Msg(" |.Disable_Medium_Nation_Invasions      : " + USER_CONFIG.Minor_And_Medium_Nation_Land_Invasions.Disable_Medium_Nation_Invasions);
        }

        public static float Param(string name, float defValue = 0f)
        {
            if (!Il2Cpp.G.GameData.parms.TryGetValue(name, out var param))
                return defValue;
            return param;
        }

        public static int Param(string name, int defValue = 0)
        {
            if (!Il2Cpp.G.GameData.parms.TryGetValue(name, out var param))
                return defValue;
            return (int)(param + 0.0001f);
        }

        public static string? ParamS(string name, string? defValue = null)
        {
            if (!Il2Cpp.G.GameData.paramsRaw.TryGetValue(name, out var param))
                return defValue;
            return param.str;
        }
    }
}
