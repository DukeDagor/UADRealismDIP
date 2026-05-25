using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Harmony;

#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8603
#pragma warning disable CS8618

namespace TweaksAndFixes
{
    public static class Database
    {
        private static string[,] _GunGradeTechs;
        private static int[,] _GunGradeYears;
        private static readonly Dictionary<string, int> _PartYears = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> _PartTechs = new Dictionary<string, string>();
        private static readonly Dictionary<string, List<string>> _TechParts = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, string> _ComponentTechs = new Dictionary<string, string>();
        private static readonly Dictionary<string, int> _ComponentYears = new Dictionary<string, int>();
        private static readonly Dictionary<string, HashSet<string>> _ParamToHulls = new Dictionary<string, HashSet<string>>();
        private static readonly Dictionary<string, HashSet<string>> _PartToHulls = new Dictionary<string, HashSet<string>>();
        private static readonly List<string> _AllHulls = new List<string>();
        private static readonly HashSet<string> _TechRequiredForShip = new HashSet<string>();
        private static readonly HashSet<string> _TechImprovementForShip = new HashSet<string>();
        private static readonly HashSet<string> _StartingTechs = new HashSet<string>();

        private static string[] _TorpGradeTechs;
        private static int[] _TorpGradeYears;
        private static List<Dictionary<string, string>> _TorpTubeTechs;
        private static List<Dictionary<string, int>> _TorpTubeYears;

        public static void FillDatabase()
        {
            _GunGradeTechs = new string[21, Config.MaxGunGrade + 1];
            _GunGradeYears = new int[21, Config.MaxGunGrade + 1];
            _TorpGradeTechs = new string[Config.MaxTorpGrade + 1];
            _TorpGradeYears = new int[Config.MaxTorpGrade + 1];
            _TorpTubeTechs = new();
            for (int i = 0; i < Config.MaxTorpBarrels + 1; i++)
            {
                _TorpTubeTechs.Add(new Dictionary<string, string>());
            }
            _TorpTubeYears = new();
            for (int j = 0; j < Config.MaxTorpBarrels + 1; j++)
            {
                _TorpTubeYears.Add(new Dictionary<string, int>());
            }
            _PartYears.Clear();
            _PartTechs.Clear();
            _TechParts.Clear();
            _ComponentTechs.Clear();
            _ComponentYears.Clear();
            _ParamToHulls.Clear();
            _PartToHulls.Clear();
            _AllHulls.Clear();

            FillTechData();
            FillPartDatabase();
        }

        private static void FillTechData()
        {
            foreach (var kvpT in G.GameData.technologies)
            {
                bool isRequired = false;
                bool isImprovement = false;
                foreach (var kvpE in kvpT.Value.effects)
                {
                    switch (kvpE.Key)
                    {
                        case "start":
                            _StartingTechs.Add(kvpT.Key);
                            break;
                        case "unlock":
                            foreach (var effList in kvpE.Value)
                            {
                                foreach (var v in effList)
                                {
                                    if (G.GameData.parts.TryGetValue(v, out var pData))
                                    {
                                        //Melon<TweaksAndFixes>.Logger.Msg($"Part {pData.name} needs tech {kvpT.key} of year {kvpT.Value.year}");
                                        _PartTechs[pData.name] = kvpT.Key;
                                        _PartYears[pData.name] = kvpT.Value.year;
                                        if (!_TechParts.TryGetValue(kvpT.Key, out var pl))
                                        {
                                            pl = new List<string>();
                                            _TechParts[kvpT.Key] = pl;
                                        }
                                        pl.Add(pData.name);
                                    }
                                }
                            }
                            break;
                        case "unlockpart":
                            // TODO: should we care about these as improvement techs?
                            foreach (var effList in kvpE.Value)
                            {
                                foreach (var v in effList)
                                {
                                    foreach (var pData in G.GameData.parts.Values)
                                    {
                                        if (pData.NeedUnlock != v)
                                            continue;
                                        //Melon<TweaksAndFixes>.Logger.Msg($"Part {pData.name} needs tech {kvpT.key} of year {kvpT.Value.year}");
                                        _PartTechs[pData.name] = kvpT.Key;
                                        _PartYears[pData.name] = kvpT.Value.year;
                                        if (!_TechParts.TryGetValue(kvpT.Key, out var pl))
                                        {
                                            pl = new List<string>();
                                            _TechParts[kvpT.Key] = pl;
                                        }
                                        pl.Add(pData.name);
                                    }
                                }
                            }
                            break;
                        case "gun":
                            isImprovement = true;
                            foreach (var effList in kvpE.Value)
                            {
                                if (effList.Count < 2 || !float.TryParse(effList[0], System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, ModUtils._InvariantCulture, out var calF))
                                    continue;
                                if (!int.TryParse(effList[1], out var grade))
                                    continue;
                                int cal = Mathf.RoundToInt(calF);
                                //Melon<TweaksAndFixes>.Logger.Msg($"Gun of {cal}in, grade {grade} needs tech {kvpT.key} of year {kvpT.Value.year}");
                                _GunGradeTechs[cal, grade] = kvpT.Key;
                                _GunGradeYears[cal, grade] = kvpT.Value.year;
                            }
                            break;

                        case "torp_mark":
                            isImprovement = true;
                            foreach (var effList in kvpE.Value)
                            {
                                if (effList.Count < 1)
                                    continue;
                                if (!int.TryParse(effList[0], out var grade))
                                    continue;
                                _TorpGradeTechs[grade] = kvpT.Key;
                                _TorpGradeYears[grade] = kvpT.Value.year;
                            }
                            break;
                        case "torpedo_tubes":
                            isImprovement = true;
                            int tubes = -1;
                            List<string> list = new List<string>();
                            foreach (var effList in kvpE.Value[0])
                            {
                                if (effList.Length < 1)
                                {
                                    Melon<TweaksAndFixes>.Logger.Error("Invalid torpedo_tubes tech effect format! Ensure there are no duplicate semicolons!");
                                    continue;
                                }
                                if (G.GameData.shipTypes.ContainsKey(effList))
                                {
                                    list.Add(effList);
                                }
                                else if (!int.TryParse(effList, out tubes))
                                {
                                    Melon<TweaksAndFixes>.Logger.Error("Invalid torpedo_tubes tech effect shiptype or number: \"" + effList + "\"!");
                                    continue;
                                }
                            }

                            if (tubes == -1) 
                            {
                                Melon<TweaksAndFixes>.Logger.Error("Invalid torpedo_tubes tech effect format! You must specify the number of tubes!");
                                continue;
                            }

                            if (list.Count == 0)
                            {
                                foreach (var type in G.GameData.shipTypes.Keys)
                                {
                                    list.Add(type);
                                }
                            }

                            // string text = "";
                            foreach (string item in list)
                            {
                                // text = text + item + " ";
                                _TorpTubeTechs[tubes][item] = kvpT.Key;
                                _TorpTubeYears[tubes][item] = kvpT.Value.year;
                            }

                            break;

                        default:
                            // if not specifically handled, just check if it's a required tech for a design
                            switch (kvpE.Key)
                            {
                                case "tonnage":
                                    isImprovement = true;
                                    break;
                                case "var":
                                case "obsolete":
                                case "max_crew_training":
                                case "training_efficiency":
                                case "flagship_bonus":
                                case "naval_bombardment":
                                case "power_projection":
                                case "escort_power":
                                case "invade_power":
                                case "minesweeping_tech":
                                case "minefieldmax":
                                case "armed_transports":
                                case "ship_crew_group_limit":
                                    break;
                                default:
                                    if (!kvpE.Key.StartsWith("submarine") && !kvpE.Key.StartsWith("sub_"))
                                    {
                                        isImprovement = true;
                                        isRequired = true;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                
                if (kvpT.Value.componentx != null)
                {
                    isImprovement = true;
                    //Melon<TweaksAndFixes>.Logger.Msg($"Component {kvpT.Value.componentx.name} needs tech {kvpT.key} of year {kvpT.Value.year}");
                    _ComponentTechs[kvpT.Value.componentx.name] = kvpT.Key;
                    _ComponentYears[kvpT.Value.componentx.name] = kvpT.Value.year;
                }
                else if (isRequired)
                    _TechRequiredForShip.Add(kvpT.Key);

                if (isImprovement)
                    _TechImprovementForShip.Add(kvpT.Key);

            }
        }

        private static void FillPartDatabase()
        {
            // Pass 1: Fill hull structures
            foreach (var kvp in G.GameData.parts)
            {
                var hull = kvp.Value;
                if (!hull.isHull)
                    continue;

                _AllHulls.Add(hull.name);

                foreach (var key in hull.paramx.Keys)
                {
                    if (!_ParamToHulls.TryGetValue(key, out var set))
                    {
                        set = new HashSet<string>();
                        _ParamToHulls.Add(key, set);
                    }
                    set.Add(hull.name);
                }
            }

            // Pass 2: fill part->hull structures
            foreach (var kvp in G.GameData.parts)
            {
                var part = kvp.Value;
                if (part.isHull)
                    continue;

                var possibleHulls = new HashSet<string>();
                bool foundTags = false;
                // There will be zero or more hashsets in needTags.
                // A hull has to have its params intersect with every hashset.
                foreach (var needSet in part.needTags)
                {
                    foreach (var s in needSet)
                    {
                        if (!_ParamToHulls.TryGetValue(s, out var set))
                            continue;

                        if (foundTags)
                        {
                            possibleHulls.IntersectWith(set);
                        }
                        else
                        {
                            foreach (var h in set)
                                possibleHulls.Add(h);
                        }
                    }
                    if (needSet.Count > 0)
                        foundTags = true;
                }
                if (!foundTags)
                {
                    foreach (var h in _AllHulls)
                        possibleHulls.Add(h);
                }
                else if (possibleHulls.Count == 0)
                {
                    _PartToHulls.Add(part.name, possibleHulls);
                    continue;
                }
                
                foreach (var excludeSet in part.excludeTags)
                {
                    foreach (var s in excludeSet)
                    {
                        if (!_ParamToHulls.TryGetValue(s, out var set))
                            continue;
                        possibleHulls.ExceptWith(set);
                    }
                }

                if (possibleHulls.Count == 0)
                {
                    _PartToHulls.Add(part.name, possibleHulls);
                    continue;
                }

                if (part.isGun)
                {
                    var calInch = part.GetCaliberInch();
                    possibleHulls.RemoveWhere(h =>
                    {
                        var hull = G.GameData.parts[h];
                        var sType = hull.shipType;
                        if ((calInch < sType.mainFrom || sType.mainTo < calInch) && (calInch < sType.secFrom || sType.secTo < calInch))
                            return true;

                        if (hull.maxAllowedCaliber > 0 && calInch > hull.maxAllowedCaliber)
                            return true;

                        if (hull.paramx.ContainsKey("unique_guns"))
                        {
                            return !part.paramx.ContainsKey("unique");
                        }
                        else
                        {
                            // Ignore max caliber for secondary guns from tech
                            // Ignore barrel limits from tech
                            return false;
                        }
                    });
                }

                // No need to check torpedo, since all paths lead to true.
                //if (part.isTorpedo)
                //{
                //    if (part.paramx.ContainsKey("sub_torpedo"))
                //        return true;
                //    if (part.paramx.ContainsKey("deck_torpedo"))
                //        return true;

                //    // Ignore barrel limit by tech
                //}

                _PartToHulls.Add(part.name, possibleHulls);
            }

            // Pass 3: Catch any parts that have missing years
            foreach (var pData in G.GameData.parts.Values)
            {
                if (_PartTechs.ContainsKey(pData.name) || pData.isHull)
                    continue;

                // Find the earliest hull that can mount this part
                if (!_PartToHulls.TryGetValue(pData.name, out var set))
                    continue;

                int earliest = int.MaxValue;
                string tech = string.Empty;
                foreach (var h in set)
                {
                    if (!_PartYears.TryGetValue(h, out var year))
                        continue;
                    if (earliest > year && _PartTechs.TryGetValue(h, out tech))
                        earliest = year;
                        
                }
                if (earliest == int.MaxValue)
                    continue;
                _PartTechs[pData.name] = tech;
                _PartYears[pData.name] = earliest;
            }
        }

        public static int GetYear(string dataName)
        {
            if (!_PartYears.TryGetValue(dataName, out var year))
                return -1;

            return year;
        }

        public static int GetGunYear(int caliberInch, int grade)
        {
            if (caliberInch < 0 || caliberInch >= _GunGradeYears.GetLength(0) || grade < 0 || grade >= _GunGradeYears.GetLength(1))
            {
                Melon<TweaksAndFixes>.Logger.Error($"Tried to get Gun Grade Year for caliber {caliberInch}, grade {grade}");
                return -1;
            }
            return _GunGradeYears[caliberInch, grade];
        }

        public static string GetGunTech(int caliberInch, int grade)
        {
            if (caliberInch < 0 || caliberInch >= _GunGradeYears.GetLength(0) || grade < 0 || grade >= _GunGradeYears.GetLength(1))
            {
                Melon<TweaksAndFixes>.Logger.Error($"Tried to get Gun Grade Tech for caliber {caliberInch}, grade {grade}");
                return string.Empty;
            }
            return _GunGradeTechs[caliberInch, grade];
        }

        public static bool CanHullMountPart(string partDataName, string hullDataName)
        {
            if (!_PartToHulls.TryGetValue(partDataName, out var hulls))
                return false;

            return hulls.Contains(hullDataName);
        }

        public static List<PartData> GetHullsForPart(string partDataName)
        {
            var lst = new List<PartData>();
            if (!_PartToHulls.TryGetValue(partDataName, out var hulls))
                return lst;

            foreach (var h in hulls)
                lst.Add(G.GameData.parts[h]);

            return lst;
        }

        public static HashSet<string> GetHullNamesForPart(string partDataName)
        {
            _PartToHulls.TryGetValue(partDataName, out var hulls);
            return hulls;
        }

        public static List<string> GetPartNamesForTech(string techName)
        {
            _TechParts.TryGetValue(techName, out var lst);
            return lst;
        }

        public static string GetPartTech(string partDataName)
        {
            _PartTechs.TryGetValue(partDataName, out var tech);
            return tech == null ? string.Empty : tech;
        }

        public static string GetCompTech(string compName)
        {
            _ComponentTechs.TryGetValue(compName, out var tech);
            return tech == null ? string.Empty : tech;
        }

        public static string GetTorpGradeTech(int grade)
        {
            if(grade < 0 || grade >= _TorpGradeTechs.Length)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Tried to get Torp Grade Tech for grade {grade}");
                return string.Empty;
            }
            return _TorpGradeTechs[grade];
        }

        public static string GetTorpTubeTech(int tubes, string shipTypes)
        {
            if (tubes < 0 || tubes >= Config.MaxTorpBarrels)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Tried to get Torp Tube Tech for tube count {tubes}");
                return string.Empty;
            }
            return _TorpTubeTechs[tubes][shipTypes];
        }

        public static bool IsTechRequiredForShipDesign(string techName)
        {
            return _TechRequiredForShip.Contains(techName);
        }

        public static bool IsTechImprovementForShipDesign(string techName)
        {
            return _TechImprovementForShip.Contains(techName);
        }

        public static int MaxGunGradeFromTechs(int cal, HashSet<string> techs, out string? techFound)
        {
            techFound = null;
            for (int i = 1; i <= Config.MaxGunGrade; ++i)
                if (techs.Contains(_GunGradeTechs[cal, i]))
                    techFound = _GunGradeTechs[cal, i];
                else
                    return i - 1;

            return Config.MaxGunGrade;
        }

        public static int MaxTorpGradeFromTechs(HashSet<string> techs, out string? techFound)
        {
            techFound = null;
            for (int i = 1; i <= Config.MaxTorpGrade; ++i)
                if (techs.Contains(_TorpGradeTechs[i]))
                    techFound = _TorpGradeTechs[i];
                else
                    return i - 1;

            return Config.MaxTorpGrade;
        }
    }
}
