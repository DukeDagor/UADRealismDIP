using MelonLoader;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;
using System.Globalization;

#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8625

namespace TweaksAndFixes
{
    public static class ShipM
    {
        public static List<Ship> ActiveShips = new(512); // TODO: If there's more active ships than this, then we have a problem...

        private static void UpdateActiveShipsList()
        {
            // if (ActiveShips.Count == 1 && ActiveShips[0]?.gameObject.GetParent()?.name == "ShipsActive") return;

            ActiveShips.Clear();

            if (Ship.shipsActiveCont == null) return;

            var children = Ship.shipsActiveCont.GetChildren();

            foreach (var child in children)
            {
                ActiveShips.Add(child.GetComponent<Ship>());
            }
        }

        public static Ship GetActiveShip()
        {
            UpdateActiveShipsList();

            if (ActiveShips.Count != 1)
            {
                return null;
            }

            return ActiveShips[0];
        }

        public static List<Ship> GetActiveShips()
        {
            UpdateActiveShipsList();

            return ActiveShips;
        }

        public static Dictionary<TKey, TValue> GetParamDict<TKey, TValue>(Ship _this, string pName) where TKey : notnull
        {
            _this.shipType.paramx.TryGetValue(pName, out var paramST);
            _this.hull.data.paramx.TryGetValue(pName, out var paramHull);
            if (paramST == null && paramHull == null)
                return null;

            Dictionary<TKey, TValue> dict;
            if (paramST == null)
                return Serializer.Human.ParamToParsedDictionary<TKey, TValue>(paramHull);
            else
                dict = Serializer.Human.ParamToParsedDictionary<TKey, TValue>(paramST);

            if (paramHull != null)
            {
                var hDict = Serializer.Human.ParamToParsedDictionary<TKey, TValue>(paramHull);
                if (hDict != null)
                    foreach (var kvp in hDict)
                        dict[kvp.Key] = kvp.Value;
            }

            return dict;
        }

        public static List<KeyValuePair<TKey, TValue>> GetParamList<TKey, TValue>(Ship _this, string pName) where TKey : notnull
        {
            _this.shipType.paramx.TryGetValue(pName, out var paramST);
            _this.hull.data.paramx.TryGetValue(pName, out var paramHull);
            if (paramST == null && paramHull == null)
                return null;

            // This will hopefully be cheaper than just parsing to dict and then converting.
            List<KeyValuePair<TKey, TValue>> list;
            if (paramST == null)
                return Serializer.Human.ParamToParsedKVPs<TKey, TValue>(paramHull);
            else
                list = Serializer.Human.ParamToParsedKVPs<TKey, TValue>(paramST);

            if (paramHull != null)
            {
                var hList = Serializer.Human.ParamToParsedKVPs<TKey, TValue>(paramHull);
                if (hList != null)
                {
                    foreach (var kvp in hList)
                    {
                        for (int i = list.Count; i-- > 0;)
                        {
                            var li = list[i];
                            if (li.Key.Equals(kvp.Key))
                            {
                                list[i] = kvp;
                                break;
                            }
                        }
                    }
                }
            }

            return list;
        }

        public static bool GetParamValue<T>(Ship _this, string pName, out T value)
        {
            value = default(T);
            Type type = typeof(T);
            Serializer.CSV.DataType dt = Serializer.CSV.FieldData.ValueDataType(type);
            if (dt == Serializer.CSV.DataType.INVALID)
                return false;

            _this.shipType.paramx.TryGetValue(pName, out var paramST);
            _this.hull.data.paramx.TryGetValue(pName, out var paramHull);

            if (paramHull == null || paramHull.Count != 1)
            {
                if (paramST == null || paramST.Count != 1)
                    return false;

                value = (T)Serializer.CSV.FieldData.ReadValue(paramST[0], dt, type);
                return true;
            }

            value = (T)Serializer.CSV.FieldData.ReadValue(paramHull[0], dt, type);
            return true;
        }

        public static bool GetParamValue<TKey, TValue>(Ship _this, string pName, TKey key, out TValue value) where TKey : notnull
        {
            value = default(TValue);
            Type type = typeof(TValue);
            Serializer.CSV.DataType dt = Serializer.CSV.FieldData.ValueDataType(type);
            if (dt == Serializer.CSV.DataType.INVALID)
                return false;

            _this.shipType.paramx.TryGetValue(pName, out var paramST);
            _this.hull.data.paramx.TryGetValue(pName, out var paramHull);
            if (paramST == null && paramHull == null)
                return false;

            if (paramHull != null)
            {
                if (paramHull.Count == 1)
                {
                    value = (TValue)Serializer.CSV.FieldData.ReadValue(paramHull[0], dt, type);
                    return true;
                }
                var dictH = Serializer.Human.ParamToParsedDictionary<TKey, TValue>(paramHull);
                if (dictH.TryGetValue(key, out value))
                    return true;
            }

            if (paramST.Count == 1)
            {
                value = (TValue)Serializer.CSV.FieldData.ReadValue(paramST[0], dt, type);
                return true;
            }
            var dictS = Serializer.Human.ParamToParsedDictionary<TKey, TValue>(paramST);
            if (dictS.TryGetValue(key, out value))
                return true;

            return false;
        }

        public static bool GetParamValueByYear<TValue>(Ship _this, string pName, out TValue value, bool hullYear = true)
        {
            int year;
            if (hullYear && Database.GetYear(_this.hull.data.name) is int hy && hy > 0)
                year = hy;
            else
                year = Mathf.RoundToInt(_this.GetYear(_this));

            value = default(TValue);
            Type type = typeof(TValue);
            Serializer.CSV.DataType dt = Serializer.CSV.FieldData.ValueDataType(type);
            if (dt == Serializer.CSV.DataType.INVALID)
                return false;

            _this.shipType.paramx.TryGetValue(pName, out var paramST);
            _this.hull.data.paramx.TryGetValue(pName, out var paramHull);
            if (paramST == null && paramHull == null)
                return false;

            if (paramHull != null && paramHull.Count == 1)
            {
                value = (TValue)Serializer.CSV.FieldData.ReadValue(paramHull[0], dt, type);
                return true;
            }

            List<KeyValuePair<int, TValue>> list;
            if (paramHull != null && paramST != null)
            {
                // We know Hull has pairs
                list = Serializer.Human.ParamToParsedKVPs<int, TValue>(paramHull);
                if (paramST.Count != 1)
                {
                    var list2 = Serializer.Human.ParamToParsedKVPs<int, TValue>(paramST);
                    if (list == null)
                        list = list2;
                    else if (list2 != null)
                    {
                        foreach (var kvp in list2)
                        {
                            bool insert = true;
                            for (int i = list.Count; i-- > 0;)
                            {
                                if (list[i].Key == kvp.Key)
                                {
                                    insert = false;
                                    break;
                                }
                            }
                            if (insert)
                                list.Add(kvp);
                        }
                    }
                }
                
            }
            else if (paramHull != null)
            {
                // We know Hull has pairs and ST is null
                list = Serializer.Human.ParamToParsedKVPs<int, TValue>(paramHull);
            }
            else
            {
                // we know Hull is null.
                // If single value in ST, done. Else we have to sort.
                if (paramST.Count == 1)
                {
                    value = (TValue)Serializer.CSV.FieldData.ReadValue(paramST[0], dt, type);
                    return true;
                }

                // use ST only
                list = Serializer.Human.ParamToParsedKVPs<int, TValue>(paramST);
            }

            if (list == null)
                return false;

            int lC = list.Count;

            if (lC == 1)
            {
                value = list[0].Value;
                return true;
            }

            list.Sort((a, b) => (a.Key.CompareTo(b.Key)));
            int idx = lC - 1;
            while (idx > 0 && year < list[idx].Key)
                --idx;
            value = list[idx].Value;
            return true;
        }

        public static Ship.TurretArmor FindMatchingTurretArmor(Ship ship, PartData data)
            => FindMatchingTurretArmor(ship, data.caliber, Ship.IsCasemateGun(data));

        public static Ship.TurretArmor FindMatchingTurretArmor(Ship ship, float caliber, bool isCasemate)
        {
            foreach (var ta in ship.shipTurretArmor)
                if (ta.turretPartData.caliber == caliber && ta.isCasemateGun == isCasemate)
                    return ta;

            return null;
        }

        public static void CloneFrom(this Ship.TurretArmor dest, Ship.TurretArmor src)
        {
            dest.barbetteArmor = src.barbetteArmor;
            dest.isCasemateGun = src.isCasemateGun;
            dest.sideTurretArmor = src.sideTurretArmor;
            dest.topTurretArmor = src.topTurretArmor;
        }

        public static Ship.TurretCaliber FindMatchingTurretCaliber(Ship ship, PartData data)
            => FindMatchingTurretCaliber(ship, data.caliber, Ship.IsCasemateGun(data));

        public static Ship.TurretCaliber FindMatchingTurretCaliber(Ship ship, float caliber, bool isCasemate)
        {
            foreach (var tc in ship.shipGunCaliber)
                if (tc.turretPartData.caliber == caliber && isCasemate == tc.isCasemateGun)
                    return tc;

            return null;
        }

        public static void CloneFrom(this Ship.TurretCaliber dest, Ship.TurretCaliber src)
        {
            dest.diameter = src.diameter;
            dest.isCasemateGun = src.isCasemateGun;
            dest.length = src.length;
        }

        public static bool ExistsMount(Ship ship, PartData part, Il2CppSystem.Collections.Generic.List<string> demandMounts = null, Il2CppSystem.Collections.Generic.List<string> excludeMounts = null, bool allowUsed = true)
        {
            foreach (var m in ship.mounts)
                if ((allowUsed || m.employedPart == null) && m.Fits(part, demandMounts, excludeMounts))
                    return true;

            return false;
        }

        private static float ArmorFunc(Ship shipHint, float ratio, float max, bool isLight)
        {
            float minArmor = isLight ? 0.05f : 0.75f;
            float unrounded = UnityEngine.Random.Range(minArmor, 1.1f) * max * ratio * (1f / 11f);
            return G.settings.RoundToArmorStep(unrounded);
        }

        private static void SetArmor(Ship shipHint, Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> dict, Ship.A area, float value)
        {
            dict[area] = Mathf.Clamp(value, shipHint.MinArmorForZone(area), shipHint.MaxArmorForZone(area));
        }

        private static float GenerateCitadelArmorPreFunc(Ship.A citadelArmorsLayer, float year)
        {
            float from, to;
            switch (citadelArmorsLayer)
            {
                case Ship.A.InnerBelt_1st:
                    from = 3f;
                    to = 9f;
                    break;
                case Ship.A.InnerBelt_2nd:
                    from = 2f;
                    to = 7f;
                    break;
                case Ship.A.InnerBelt_3rd:
                case Ship.A.InnerDeck_1st:
                    from = 1.5f;
                    to = 5f;
                    break;
                case Ship.A.InnerDeck_2nd:
                    from = 1.25f;
                    to = 4f;
                    break;
                case Ship.A.InnerDeck_3rd:
                    from = 1.2f;
                    to = 3f;
                    break;
                default:
                    return 0f;
            }

            return Util.Remap(year, 1890f, 1940f, from, to, true);
        }

        public static Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> GenerateArmor(float armorMaximal, Ship shipHint)
        {
            float year;

            if (GameManager.IsMission && BattleManager.Instance.CurrentAcademyMission != null)
            {
                if (shipHint.player.isMain)
                    year = BattleManager.Instance.CurrentAcademyMission.year;
                else
                    year = BattleManager.Instance.CurrentAcademyMission.enemyYear;
            }
            else if (GameManager.IsCustomBattle)
            {
                if (shipHint.player.isMain)
                {
                    if (BattleManager.Instance.CurrentCustomBattle != null)
                        year = BattleManager.Instance.CurrentCustomBattle.player1.year;
                    else
                        year = CampaignController.Instance.StartYear;
                }
                else
                {
                    year = BattleManager.Instance.CurrentCustomBattle.player2.year;
                }
            }
            else
            {
                year = CampaignController.Instance.CurrentDate.AsDate().Year;
            }

            var dict = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();
            var citArmor = shipHint.GetCitadelArmor();
            bool isLight = shipHint.shipType.name == "dd" || shipHint.shipType.name == "tb";
            float beltMult = 1f;
            float turSideMult = 1f;
            float barbetteMult = 1f;

            switch (shipHint.shipType.name)
            {
                case "bb":
                    beltMult = 1.5f;
                    turSideMult = 1.6f; // seems just wrong - 0.065f;
                    barbetteMult = 1.5f; // ditto 0.135f;
                    break;
                case "bc":
                    beltMult = 1.385f;
                    turSideMult = 1.42f; // stock 0.08f;
                    barbetteMult = 1.385f; // stock 0.04f;
                    break;
                case "ca":
                    beltMult = 1.05f;
                    turSideMult = 2f;
                    barbetteMult = 1.8f;
                    break;
                case "cl":
                    beltMult = 0.38f;
                    turSideMult = 5f;
                    barbetteMult = 50f;
                    break;
                case "dd":
                    beltMult = 1.2f;
                    turSideMult = 40f;
                    barbetteMult = 75f;
                    break;
            }

            float beltYearMult = Util.Remap(year, 1900f, 1940f, 12f, beltMult * 14f, true);
            float beltVal = ArmorFunc(shipHint, beltYearMult, armorMaximal, isLight);
            SetArmor(shipHint, dict, Ship.A.Belt, beltVal);
            float foreaftBeltYearMult = Util.Remap(year, 1900f, 1940f, 3.5f, 7.5f, true);
            SetArmor(shipHint, dict, Ship.A.BeltBow, ArmorFunc(shipHint, foreaftBeltYearMult, armorMaximal, isLight));
            SetArmor(shipHint, dict, Ship.A.BeltStern, ArmorFunc(shipHint, foreaftBeltYearMult, armorMaximal, isLight));
            
            float deckYearMult = Util.Remap(year, 1900f, 1940f, 1.25f, 7.5f, true);
            SetArmor(shipHint, dict, Ship.A.Deck, ArmorFunc(shipHint, deckYearMult, armorMaximal, isLight));
            float foreaftDeck = Util.Remap(year, 1900f, 1940f, 1.5f, 5f, true);
            SetArmor(shipHint, dict, Ship.A.DeckBow, ArmorFunc(shipHint, foreaftDeck, armorMaximal, isLight));
            SetArmor(shipHint, dict, Ship.A.DeckStern, ArmorFunc(shipHint, foreaftDeck, armorMaximal, isLight));

            SetArmor(shipHint, dict, Ship.A.ConningTower, ArmorFunc(shipHint, Util.Remap(year, 1900f, 1940f, 14f, 16f, true), armorMaximal, isLight));

            float super = Util.Remap(year, 1900f, 1940f, 1f, UnityEngine.Random.Range(0.5f, 1.5f), true);
            turSideMult *= Util.Remap(year, 1900f, 1940f, 0.7f, 1.1f, true) * armorMaximal;
            if (GameManager.IsMission && BattleManager.Instance.CurrentAcademyMission.paramx.ContainsKey("crutch_ironclad")
                && (shipHint.hull.name.Contains("monitor") || shipHint.hull.name.Contains("virginia")))
            {
                super += 9f;
                turSideMult += 13f;
            }
            SetArmor(shipHint, dict, Ship.A.Superstructure, ArmorFunc(shipHint, super, armorMaximal, isLight));

            SetArmor(shipHint, dict, Ship.A.TurretSide, ArmorFunc(shipHint, turSideMult, armorMaximal, isLight));
            SetArmor(shipHint, dict, Ship.A.TurretTop, ArmorFunc(shipHint, Util.Remap(year, 1900f, 1940f, 3f, 12f, true), armorMaximal, isLight));
            SetArmor(shipHint, dict, Ship.A.Barbette, ArmorFunc(shipHint, barbetteMult * armorMaximal * Util.Remap(year, 1900f, 1940f, 3.75f, 5f, true), armorMaximal, isLight));

            var zonesUI = Ship.armorZonesUi.ToList();
            foreach (var z in zonesUI)
            {
                dict.TryGetValue(z, out var val);
                dict[z] = Mathf.Clamp(val, shipHint.MinArmorForZone(z), shipHint.MaxArmorForZone(z));
            }

            if (citArmor != null)
            {
                foreach (var z in citArmor)
                {
                    float level = ArmorFunc(shipHint, GenerateCitadelArmorPreFunc(z, year), armorMaximal, isLight);
                    if (shipHint.armor == null)
                        dict[z] = level;
                    else
                        SetArmor(shipHint, dict, z, level);
                }
            }

            return dict;
        }

        public static Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> GenerateArmorNew(float armorMaximal, Ship shipHint)
        {
            if (shipHint == null)
                return GenerateArmor(armorMaximal, shipHint);

            float year;
            if (GameManager.IsMission && BattleManager.Instance.CurrentAcademyMission != null)
            {
                if (shipHint.player.isMain)
                    year = BattleManager.Instance.CurrentAcademyMission.year;
                else
                    year = BattleManager.Instance.CurrentAcademyMission.enemyYear;
            }
            else if (GameManager.IsCustomBattle)
            {
                if (shipHint.player.isMain)
                {
                    if (BattleManager.Instance.CurrentCustomBattle != null)
                        year = BattleManager.Instance.CurrentCustomBattle.player1.year;
                    else
                        year = CampaignController.Instance.StartYear;
                }
                else
                {
                    year = BattleManager.Instance.CurrentCustomBattle.player2.year;
                }
            }
            else
            {
                year = CampaignController.Instance.CurrentDate.AsDate().Year;
            }

            var info = GenArmorData.GetInfoFor(shipHint, Patch_Ship._GenerateShipState >= 0 ? -1f : year);
            if (info == null)
                return GenerateArmor(armorMaximal, shipHint);

            if (shipHint.armor == null)
                shipHint.armor = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();

            // Divide out the scaling done by GenerateRandomShip, since we scale by year ourselves.
            if (Patch_Ship._GenerateShipState == 5)
                armorMaximal /= Util.Remap(shipHint.GetYear(shipHint), 1890f, 1940f, 1.0f, 0.85f, true);

            var dict = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();
            var citArmor = shipHint.GetCitadelArmor();

            float maxBelt = info.GetMaxArmorValue(shipHint, Ship.A.Belt, null);
            float portion = Mathf.Min(1f, armorMaximal / maxBelt); // estimate what lerp value to use
            for (Ship.A a = Ship.A.Belt; a < Ship.A.InnerBelt_1st; a += 1)
                dict[a] = info.GetArmorValue(shipHint, a, portion);

            var oldDict = shipHint.armor;
            shipHint.armor = dict;
            if (citArmor != null)
                foreach (var a in citArmor)
                    dict[a] = info.GetArmorValue(shipHint, a, portion);

            shipHint.armor = oldDict;
            return dict;
        }

        private static readonly Dictionary<Ship.A, float> _AdjustPriorityArmorReduce = GenerateAdjustArmorPriorities(false);
        private static readonly Dictionary<Ship.A, float> _AdjustPriorityArmorIncrease = GenerateAdjustArmorPriorities(true);
        private static readonly Dictionary<Ship.A, float> _AdjustPriorityArmorLocal = new Dictionary<Ship.A, float>();
        private static readonly Dictionary<AdjustHullStatsItem, float> _AdjustHullStatsOptions = new Dictionary<AdjustHullStatsItem, float>();
        private static string[] _OpRangeValues = null;
        private static string[] _SurvValues = null;

        internal enum AdjustHullStatsItem
        {
            Speed,
            Range,
            Quarters,
            Armor,
            Surv,
            Empty
        }

        private static Dictionary<Ship.A, float> GenerateAdjustArmorPriorities(bool increase)
        {
            var armorPriority = new Dictionary<Ship.A, float>();
            if (increase)
            {
                armorPriority.Add(Ship.A.Belt, 31);
                armorPriority.Add(Ship.A.BeltBow, 18);
                armorPriority.Add(Ship.A.BeltStern, 17);
                armorPriority.Add(Ship.A.Deck, 18);
                armorPriority.Add(Ship.A.DeckBow, 16);
                armorPriority.Add(Ship.A.DeckStern, 15);
                armorPriority.Add(Ship.A.ConningTower, 12);
                armorPriority.Add(Ship.A.Superstructure, 12);
                armorPriority.Add(Ship.A.TurretTop, 25);
                armorPriority.Add(Ship.A.TurretSide, 25);
                armorPriority.Add(Ship.A.Barbette, 25);
                armorPriority.Add(Ship.A.InnerBelt_1st, 25);
                armorPriority.Add(Ship.A.InnerBelt_2nd, 24);
                armorPriority.Add(Ship.A.InnerBelt_3rd, 23);
                armorPriority.Add(Ship.A.InnerDeck_1st, 25);
                armorPriority.Add(Ship.A.InnerDeck_2nd, 24);
                armorPriority.Add(Ship.A.InnerDeck_3rd, 23);
            }
            else
            {
                armorPriority.Add(Ship.A.Belt, 4);
                armorPriority.Add(Ship.A.BeltBow, 10);
                armorPriority.Add(Ship.A.BeltStern, 12);
                armorPriority.Add(Ship.A.Deck, 7);
                armorPriority.Add(Ship.A.DeckBow, 10);
                armorPriority.Add(Ship.A.DeckStern, 12);
                armorPriority.Add(Ship.A.ConningTower, 10);
                armorPriority.Add(Ship.A.Superstructure, 10);
                armorPriority.Add(Ship.A.TurretTop, 10);
                armorPriority.Add(Ship.A.TurretSide, 10);
                armorPriority.Add(Ship.A.Barbette, 10);
                armorPriority.Add(Ship.A.InnerBelt_1st, 4);
                armorPriority.Add(Ship.A.InnerBelt_2nd, 4);
                armorPriority.Add(Ship.A.InnerBelt_3rd, 4);
                armorPriority.Add(Ship.A.InnerDeck_1st, 5);
                armorPriority.Add(Ship.A.InnerDeck_2nd, 5);
                armorPriority.Add(Ship.A.InnerDeck_3rd, 5);
            }

            return armorPriority;
        }

        public const float KnotsToMS = 0.514444444f;

        private static float _SpeedStep = -1f;
        private static float _SpeedStepR = -1f;
        public static float RoundSpeedToStep(float speed)
        {
            if (_SpeedStep < 0)
            {
                _SpeedStep = MonoBehaviourExt.Param("speed_step", 0.1f) * KnotsToMS;
                _SpeedStepR = 1f / _SpeedStep;
            }
            return Mathf.RoundToInt(speed * _SpeedStepR) * _SpeedStep;
        }

        private static VesselEntity.OpRange MinOpRange(Ship ship, VesselEntity.OpRange defaultMin = VesselEntity.OpRange.VeryLow)
        {
            if (ship.shipType.paramx.TryGetValue("range_min", out var minRange) && minRange.Count > 0)
            {
                if (_OpRangeValues == null)
                {
                    var vals = Enum.GetValues(typeof(VesselEntity.OpRange));
                    _OpRangeValues = new string[vals.Length];
                    for (int i = vals.Length; i-- > 0;)
                        _OpRangeValues[i] = vals.GetValue(i).ToString().ToLower();
                }

                string minR = minRange[0].ToLower();
                for (int i = 0; i < _OpRangeValues.Length; ++i)
                {
                    if (_OpRangeValues[i] == minR)
                        return (VesselEntity.OpRange)i;
                }
            }

            return defaultMin;
        }

        private static Ship.Survivability MinSurv(Ship ship, Ship.Survivability defaultMin = Ship.Survivability.VeryLow)
        {
            if (ship.shipType.paramx.TryGetValue("surv_min", out var min) && min.Count > 0)
            {
                if (_SurvValues == null)
                {
                    var vals = Enum.GetValues(typeof(Ship.Survivability));
                    _SurvValues = new string[vals.Length];
                    for (int i = vals.Length; i-- > 0;)
                        _SurvValues[i] = vals.GetValue(i).ToString().ToLower();
                }

                string minS = min[0].ToLower();
                for (int i = 0; i < _SurvValues.Length; ++i)
                {
                    if (_SurvValues[i] == minS)
                        return (Ship.Survivability)i;
                }
            }

            return defaultMin;
        }

        public static void AdjustHullStats(Ship _this, int delta, float targetWeightRatio, Func<bool> stopCondition, bool allowEditSpeed = true, bool allowEditArmor = true, bool allowEditCrewQuarters = true, bool allowEditRange = true, bool allowEditSurv = true, Il2CppSystem.Random rnd = null, float limitArmor = -1f, float limitSpeed = -1f)
        {
            float year = _this.GetYear(_this);
            Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> newArmor = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();
            Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> oldArmor = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();
            float oldArmorPortion = 0.5f;
            bool isCL = _this.shipType.name == "cl";
            bool isLightCraft = _this.shipType.name == "tb" || _this.shipType.name == "dd";
            bool cldd = isCL || isLightCraft;

            var minOpRange = MinOpRange(_this, VesselEntity.OpRange.Low);
            var minSurv = MinSurv(_this, Ship.Survivability.High);

            var gaInfo = GenArmorData.GetInfoFor(_this);
            bool useInfo = gaInfo != null;

            var armorMin = _this.shipType.armorMin;
            if (armorMin <= 0)
                armorMin = _this.shipType.armor;
            float armorInches;
            // All ships are expected to have a hint
            if (_this.shipType.paramx.TryGetValue("armor_min_hint", out var minArmorParam) && minArmorParam.Count > 0 && float.TryParse(minArmorParam[0], NumberStyles.Float | NumberStyles.AllowThousands, ModUtils._InvariantCulture, out var armorParam))
            {
                armorInches = armorMin * armorParam;
            }
            else
            {
                armorInches = armorMin * Util.Range(1f, 1.2f, rnd);
            }
            Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> armorMinHint = null;
            if (!useInfo)
            {
                armorMinHint = GenerateArmorNew(armorInches * 25.4f, _this);
            }

            float hullSpeedMS = _this.hull.data.speedLimiter * KnotsToMS;

            float oldSpeed = _this.speedMax;
            Ship.CrewQuarters oldQuarters = _this.CurrentCrewQuarters;
            var oldRange = _this.opRange < minOpRange ? minOpRange : _this.opRange;
            var oldSurv = _this.survivability < minSurv ? minSurv : _this.survivability;
            if (_this.armor == null)
            {
                _this.armor = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();
                _this.SetArmor(armorMinHint == null ? GenerateArmorNew(armorInches * 25.4f, _this) : armorMinHint);
            }

            if (useInfo)
                oldArmorPortion = gaInfo.EstimateLerp(_this);

            foreach (var kvp in _this.armor)
                oldArmor[kvp.Key] = kvp.Value;

            float minSpeed;
            float maxSpeed;
            if (Config.ShipGenTweaks)
            {
                minSpeed = GetMinSpeed(_this, limitSpeed, rnd);
                maxSpeed = GetMaxSpeed(_this, rnd, out _);
            }
            else
            {
                if (limitSpeed > 0f)
                {
                    minSpeed = limitSpeed;
                }
                else
                {
                    float mult;
                    if (isLightCraft)
                        mult = 0.92f;
                    else if (isCL)
                        mult = 0.86f;
                    else
                        mult = 0.79f;
                    minSpeed = hullSpeedMS * Util.Remap(year, 1890f, 1940f, 1.0f, 0.9f, true) * mult;
                }
                float maxMult = cldd ? 1.05f : 1.1f;
                maxSpeed = hullSpeedMS * maxMult * Util.Remap(year, 1890f, 1940f, 1.05f, 1.0f, true);

                minSpeed = Mathf.Max(minSpeed, _this.shipType.speedMin * KnotsToMS);
                maxSpeed = Mathf.Min(maxSpeed, _this.shipType.speedMax * KnotsToMS);
            }

            float armorLimit = limitArmor > 0f ? limitArmor : _this.shipType.armorMax * 3f * 25.4f;
            bool canMakeTarget;
            if (delta < 0)
            {
                _this.SetSpeedMax(minSpeed);
                _this.CurrentCrewQuarters = Ship.CrewQuarters.Cramped;
                _this.SetOpRange(minOpRange);
                _this.SetSurvivability(minSurv);

                if (gaInfo == null)
                {
                    foreach (var key in _this.armor.Keys)
                        newArmor[key] = Mathf.Max(armorMinHint[key], _this.MinArmorForZone(key));

                    _this.SetArmor(newArmor);
                }
                else
                {
                    gaInfo.SetArmor(_this, 0f);
                }

                canMakeTarget = _this.Weight() / _this.Tonnage() <= targetWeightRatio;
                if (!canMakeTarget)
                {
                    foreach (var kvp in _this.armor)
                        newArmor[kvp.Key] = _this.MinArmorForZone(kvp.Key);

                    _this.SetArmor(newArmor);
                    canMakeTarget = _this.Weight() / _this.Tonnage() <= targetWeightRatio;
                }
            }
            else
            {
                _this.SetSpeedMax(maxSpeed);
                _this.CurrentCrewQuarters = Ship.CrewQuarters.Spacious;
                _this.SetOpRange(VesselEntity.OpRange.VeryHigh);
                _this.SetSurvivability(Ship.Survivability.VeryHigh);

                for (Ship.A area = Ship.A.Belt; area <= Ship.A.Barbette; area += 1)
                    newArmor[area] = Mathf.Min(armorLimit, _this.MaxArmorForZone(area, null));

                _this.SetArmor(newArmor); // have to set before doing citadel armor
                // because citadel armor depends on base belt/deck armor.
                var citA = _this.GetCitadelArmor();
                if (citA != null)
                {
                    // We also have to set each one directly,
                    // since the next area depends on the previous.
                    foreach (var key in citA)
                        _this.SetArmor(key, Mathf.Min(armorLimit, _this.MaxArmorForZone(key, null)));

                    _this.RefreshHullStats();
                }

                canMakeTarget = targetWeightRatio * 0.997f <= _this.Weight() / _this.Tonnage();
            }

            if (!canMakeTarget)
                return;

            oldSpeed = RoundSpeedToStep(oldSpeed);

            // Reset
            _this.SetSpeedMax(oldSpeed);
            _this.CurrentCrewQuarters = oldQuarters;
            _this.SetOpRange(oldRange);
            _this.SetSurvivability(oldSurv);
            if (gaInfo == null)
                _this.SetArmor(oldArmor);
            else
                gaInfo.SetArmor(_this, oldArmorPortion);

            Dictionary<Ship.A, float> armorPriority = delta > 0 ? _AdjustPriorityArmorIncrease : _AdjustPriorityArmorReduce;
            float speedStep = MonoBehaviourExt.Param("speed_step", 0.5f) * delta * KnotsToMS;

            // We might already meet the requirements.
            if (stopCondition != null && stopCondition())
                return;

            float curArmorPortion = oldArmorPortion;
            
            for (int j = 0; j < 699; ++j)
            {
                // Recreate, since we might have touched citadel armor
                newArmor.Clear();
                oldArmor.Clear();
                foreach (var kvp in _this.armor)
                {
                    newArmor[kvp.Key] = kvp.Value;
                    oldArmor[kvp.Key] = kvp.Value;
                }

                oldSpeed = _this.speedMax;
                oldQuarters = _this.CurrentCrewQuarters;
                oldRange = _this.opRange;
                oldSurv = _this.survivability;
                oldArmorPortion = curArmorPortion;

                float newSpeed = speedStep + _this.speedMax;
                newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
                newSpeed = RoundSpeedToStep(newSpeed);
                Ship.CrewQuarters newQuarters = (Ship.CrewQuarters)Mathf.Clamp((int)_this.CurrentCrewQuarters + delta, (int)Ship.CrewQuarters.Cramped, (int)Ship.CrewQuarters.Spacious);
                VesselEntity.OpRange newOpRange = (VesselEntity.OpRange)Mathf.Clamp((int)_this.opRange + delta, (int)minOpRange, (int)VesselEntity.OpRange.VeryHigh);
                Ship.Survivability newSurv = (Ship.Survivability)Mathf.Clamp((int)_this.survivability + delta, (int)minSurv, (int)Ship.Survivability.VeryHigh);

                // We don't have to do all this work if we're not allowed to change armor anyway
                bool armorFound = false;
                if (allowEditArmor)
                {
                    if (useInfo)
                    {
                        // This is kind of dumb--we set the armor twice.
                        // But it's easier to do this than to carry around
                        // a bunch of duplicate TurretArmors.
                        float inc = delta * gaInfo.lerpStep;
                        while (delta > 0 ? curArmorPortion < 1f : curArmorPortion > 0f)
                        {
                            curArmorPortion = Mathf.Clamp(curArmorPortion + inc, 0f, 1f);
                            if (gaInfo.SetArmor(_this, curArmorPortion))
                            {
                                armorFound = true;
                                break;
                            }
                        }
                        if (armorFound)
                            gaInfo.SetArmor(_this, oldArmorPortion);
                    }
                    else
                    {
                        // We need to find a valid armor zone. Note
                        // due to citadel armor weirdness, we have
                        // to do this fresh each time.
                        foreach (var kvp in armorPriority)
                        {
                            float maxZone = _this.MaxArmorForZone(kvp.Key, null);
                            if (maxZone > 0f)
                            {
                                armorMinHint.TryGetValue(kvp.Key, out var minHint);
                                float minArmor = Mathf.Max(minHint, _this.MinArmorForZone(kvp.Key));
                                float maxArmor = Mathf.Min(armorLimit, maxZone);
                                _this.armor.TryGetValue(kvp.Key, out float oldLevel);
                                if ((delta > 0) ? (oldLevel < maxArmor) : (oldLevel > minArmor))
                                    _AdjustPriorityArmorLocal.Add(kvp.Key, kvp.Value);
                            }
                        }
                        if (_AdjustPriorityArmorLocal.Count > 0)
                        {
                            var randomA = ModUtils.RandomByWeights(_AdjustPriorityArmorLocal);
                            armorMinHint.TryGetValue(randomA, out var minHint);
                            float minArmor = Mathf.Max(minHint, _this.MinArmorForZone(randomA));
                            float maxArmor = Mathf.Min(armorLimit, _this.MaxArmorForZone(randomA, null));
                            _this.armor.TryGetValue(randomA, out float oldLevel);
                            float newArmorLevel = delta * 2.54f + oldLevel;
                            if (delta < 0)
                                newArmorLevel = Mathf.Floor(newArmorLevel);
                            else
                                newArmorLevel = Mathf.Ceil(newArmorLevel);
                            newArmorLevel = Mathf.Clamp(newArmorLevel, minArmor, maxArmor);

                            // by definition this check should be true because
                            // we shouldn't have added the zone otherwise. But just in case.
                            if (newArmorLevel != oldLevel)
                            {
                                armorFound = true;
                                newArmor[randomA] = newArmorLevel;
                            }

                            _AdjustPriorityArmorLocal.Clear();
                        }
                    }
                }

                _AdjustHullStatsOptions.Clear();
                if (allowEditSpeed && newSpeed != oldSpeed)
                    _AdjustHullStatsOptions.Add(AdjustHullStatsItem.Speed, delta > 0 ? 350f : 690f);
                if (allowEditCrewQuarters && newQuarters != oldQuarters)
                    _AdjustHullStatsOptions.Add(AdjustHullStatsItem.Quarters, 150f);
                if (armorFound)
                    _AdjustHullStatsOptions.Add(AdjustHullStatsItem.Armor, delta > 0 ? 850f : 450f);
                if (allowEditRange && newOpRange != oldRange)
                    _AdjustHullStatsOptions.Add(AdjustHullStatsItem.Range, delta > 0 ? 75f : 400f);
                if (allowEditSurv && newSurv != oldSurv)
                    _AdjustHullStatsOptions.Add(AdjustHullStatsItem.Surv, delta > 0 ? 400f : 400f);

                // if we can't make any more changes, we're done regardless.
                if (_AdjustHullStatsOptions.Count == 0)
                    return;

                // We're going to replace entries we pull and use with "empty"
                // so that we don't screw with our weights. (If we just removed items,
                // like stock, then quarters and range would be overrepresented.)
                AdjustHullStatsItem thingToChange = AdjustHullStatsItem.Empty;
                while ((thingToChange = ModUtils.RandomByWeights(_AdjustHullStatsOptions, null, rnd)) != AdjustHullStatsItem.Empty)
                {
                    float weight = _AdjustHullStatsOptions[thingToChange];
                    _AdjustHullStatsOptions.Remove(thingToChange);
                    _AdjustHullStatsOptions.ChangeValueFor(AdjustHullStatsItem.Empty, weight);

                    switch (thingToChange)
                    {
                        case AdjustHullStatsItem.Speed:
                            _this.SetSpeedMax(newSpeed);
                            break;
                        case AdjustHullStatsItem.Quarters:
                            _this.CurrentCrewQuarters = newQuarters;
                            break;
                        case AdjustHullStatsItem.Range:
                            _this.SetOpRange(newOpRange);
                            break;
                        case AdjustHullStatsItem.Surv:
                            _this.SetSurvivability(newSurv);
                            break;
                        default:
                            if (useInfo)
                                gaInfo.SetArmor(_this, curArmorPortion);
                            else
                                _this.SetArmor(newArmor);
                            break;
                    }

                    // We have to separately check if we're OVER max weight
                    // because the stopCondition might have a ratio of 1.0,
                    // so we would never meet it unless we were overweight
                    // (or so precisely on-target as to be impossible
                    // with floats).
                    if (delta > 0 &&
                        (_this.Weight() > _this.Tonnage()
                        || (GameManager.IsMission && _this.player.isMain && BattleManager.Instance != null
                            && BattleManager.Instance.MissionMainShip == _this && _this.Cost() > _this.player.cash)))
                    {
                        switch (thingToChange)
                        {
                            case AdjustHullStatsItem.Speed:
                                _this.SetSpeedMax(oldSpeed);
                                break;
                            case AdjustHullStatsItem.Quarters:
                                _this.CurrentCrewQuarters = oldQuarters;
                                break;
                            case AdjustHullStatsItem.Range:
                                _this.SetOpRange(oldRange);
                                break;
                            case AdjustHullStatsItem.Surv:
                                _this.SetSurvivability(oldSurv);
                                break;
                            default:
                                if (useInfo)
                                    gaInfo.SetArmor(_this, oldArmorPortion);
                                else
                                    _this.SetArmor(oldArmor);
                                break;
                        }
                        return;
                    }

                    // Now check vs requirements
                    if (stopCondition != null && stopCondition())
                        return;
                }
            }
        }

        public static void AddedAdditionalTonnageUsage(Ship _this)
        {
            G.GameData.shipTypes.TryGetValue("amc", out var amcST);
            if (_this.shipType == amcST)
                return;

            float tonnage = _this.Tonnage();
            float weight = _this.Weight();

            float weightToStopArmor = tonnage * 0.9999f;
            // The game also checks if the ship is without armor.
            // But that's a bug, because there's other things to increase.
            if (weight >= weightToStopArmor)
                return;

            _this.tempGoodWeight = weightToStopArmor;
            float weightToStopRange = (1f - _this.weightPercentForOpRange) * tonnage;
            float weightToStopSurv = (1f - _this.weightPercentForSurvivability) * tonnage;

            bool underweight = weight < _this.tempGoodWeight;
            if (!_this.IsShipWhitoutArmor())
            {
                var gaInfo = GenArmorData.GetInfoFor(_this);
                if (gaInfo != null)
                {
                    float oldArmorLerp = gaInfo.EstimateLerp(_this);
                    float curArmorLerp = oldArmorLerp;

                    _this.limiter = 699;
                    // Kinda dumb but because of turret armor we set, and then unset if needed.
                    while (underweight && _this.limiter-- > 0)
                    {
                        oldArmorLerp = curArmorLerp;
                        while (curArmorLerp < 1f)
                        {
                            curArmorLerp = Mathf.Clamp(curArmorLerp + gaInfo.lerpStep, 0f, 1f);

                            if (gaInfo.SetArmor(_this, curArmorLerp))
                                break;
                        }

                        weight = _this.Weight();
                        if (weight > _this.tempGoodWeight)
                        {
                            gaInfo.SetArmor(_this, oldArmorLerp);
                            weight = _this.Weight();
                            // These steps are bigger. It's worth trying other things.
                            underweight = weight < _this.tempGoodWeight;
                            break;
                        }
                    }
                }
                else
                {
                    Il2CppSystem.Collections.Generic.List<int> armorQueue;
                    switch (_this.shipType.name)
                    {
                        case "bb":
                            armorQueue = _this.armorAddedQueueBB;
                            break;
                        case "bc":
                            armorQueue = _this.armorAddedQueueBC;
                            break;
                        case "ca":
                            armorQueue = _this.armorAddedQueueCA;
                            break;
                        case "cl":
                            armorQueue = _this.armorAddedQueueCL;
                            break;
                        case "dd":
                            armorQueue = _this.armorAddedQueueDD;
                            break;
                        default:
                            armorQueue = new Il2CppSystem.Collections.Generic.List<int>();
                            armorQueue.Add((int)Ship.A.TurretSide);
                            armorQueue.Add((int)Ship.A.Barbette);
                            armorQueue.Add((int)Ship.A.Belt);
                            armorQueue.Add((int)Ship.A.Deck);
                            armorQueue.Add((int)Ship.A.BeltBow);
                            armorQueue.Add((int)Ship.A.DeckBow);
                            armorQueue.Add((int)Ship.A.TurretTop);
                            armorQueue.Add((int)Ship.A.BeltStern);
                            armorQueue.Add((int)Ship.A.DeckStern);
                            armorQueue.Add((int)Ship.A.ConningTower);
                            armorQueue.Add((int)Ship.A.InnerBelt_1st);
                            armorQueue.Add((int)Ship.A.InnerDeck_1st);
                            armorQueue.Add((int)Ship.A.InnerBelt_2nd);
                            armorQueue.Add((int)Ship.A.InnerDeck_2nd);
                            armorQueue.Add((int)Ship.A.InnerBelt_3rd);
                            armorQueue.Add((int)Ship.A.InnerDeck_3rd);
                            armorQueue.Add((int)Ship.A.Superstructure);
                            break;
                    }

                    _this.limiter = 699;
                    bool changedArmor = true;
                    while (underweight && _this.limiter-- > 0 && changedArmor)
                    {
                        changedArmor = false;
                        foreach (var i in armorQueue)
                        {
                            Ship.A a = (Ship.A)i;
                            float max = _this.MaxArmorForZone(a);
                            if (!_this.armor.TryGetValue(a, out var curAmt) || curAmt >= max)
                                continue;

                            // This function is bugged. It's supposed to return
                            // a value based on how near max tonnage the ship is,
                            // but instead just returns 2 for armor and 1 for
                            // everything else
                            float stepMult = 2f; // _this.GetAdditionalWeightStepMult(true);
                            float armorStep = G.settings.armorStep;
                            float newAmt = G.settings.RoundToArmorStep(armorStep * stepMult + curAmt);
                            newAmt = Mathf.Clamp(newAmt, _this.MinArmorForZone(a), max);
                            if (newAmt == curAmt)
                                continue;

                            changedArmor = true;
                            _this.SetArmor(a, newAmt, true);
                            weight = _this.Weight();
                            if (weight > _this.tempGoodWeight)
                            {
                                _this.SetArmor(a, curAmt, true);
                                weight = _this.Weight();
                                underweight = false;
                                break;
                            }
                        }
                    }
                }
            }

            _this.limiter = 699;
            float step = MonoBehaviourExt.Param("speed_step", 0.1f);
            float maxMS = GetMaxSpeed(_this, null, out bool enforceMax);
            float minMS = _this.shipType.speedMin * KnotsToMS;
            while (underweight && _this.limiter-- > 0)
            {
                float oldSpeed = _this.speedMax;
                float newSpeed = RoundSpeedToStep(Mathf.Clamp(oldSpeed + step, minMS, maxMS));
                if (oldSpeed == newSpeed)
                    break;
                _this.SetSpeedMax(newSpeed);
                weight = _this.Weight();
                if (weight > _this.tempGoodWeight)
                {
                    _this.SetSpeedMax(oldSpeed);
                    weight = _this.Weight();
                    underweight = false;
                    break;
                }
            }

            if (!_this.isOpRangeDecreased && weight < weightToStopRange)
            {
                while (_this.opRange < VesselEntity.OpRange.VeryHigh)
                {
                    var oldRange = _this.opRange;
                    var newRange = (VesselEntity.OpRange)((int)oldRange + 1);
                    _this.opRange = newRange;
                    _this.GetOpRangeInKmCalculate();
                    weight = _this.Weight();
                    if (weight > weightToStopRange)
                    {
                        _this.opRange = oldRange;
                        _this.GetOpRangeInKmCalculate();
                        weight = _this.Weight();
                        break;
                    }
                }
            }

            if (!_this.isSurvivabilityDecreased && weight < weightToStopSurv)
            {
                while (_this.survivability < Ship.Survivability.VeryHigh)
                {
                    var oldSurv = _this.survivability;
                    var newSurv = (Ship.Survivability)((int)oldSurv + 1);
                    _this.SetSurvivability(newSurv);
                    weight = _this.Weight();
                    if (weight > weightToStopSurv)
                    {
                        _this.SetSurvivability(oldSurv);
                        weight = _this.Weight();
                        break;
                    }
                }
            }

            // Uncapped speed increase
            _this.limiter = 699;
            maxMS = _this.shipType.speedMax * KnotsToMS;
            while (!enforceMax && underweight && _this.limiter-- > 0)
            {
                float oldSpeed = _this.speedMax;
                float newSpeed = RoundSpeedToStep(Mathf.Clamp(oldSpeed + step, minMS, maxMS));
                if (oldSpeed == newSpeed)
                    break;
                _this.SetSpeedMax(newSpeed);
                weight = _this.Weight();
                if (weight > _this.tempGoodWeight)
                {
                    _this.SetSpeedMax(oldSpeed);
                    weight = _this.Weight();
                    underweight = false;
                    break;
                }
            }
        }

        public static void ReduceWeightByReducingCharacteristics(Ship _this, Il2CppSystem.Random rnd, float tryN, float triesTotal, float randArmorRatio = 0, float speedLimit = 0)
        {
            G.GameData.shipTypes.TryGetValue("tr", out var trST);
            if (_this.shipType == trST)
                return;

            float tonnage = _this.Tonnage();
            float weight = _this.Weight();
            float startWeight = weight;
            if (weight <= tonnage)
                return;

            float tryRatio = tryN * 100f / triesTotal;

            Dictionary<string, float> weightReductionLog = new();

            bool overweight = weight > _this.tempGoodWeight;
            if (overweight && _this.shipType.name == "dd"
                && G.GameData.components.ContainsKey("multi_bottom_0"))
            {
                _this.InstallComponent(G.GameData.components["multi_bottom_0"]);

                if (weight > _this.Weight())
                    weightReductionLog.Add("Set to Single Bottom", weight - _this.Weight());

                weight = _this.Weight();

                if (weight < _this.tempGoodWeight) goto Done;
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight)
            {
                if (tryRatio > 50)
                    _this.CurrentCrewQuarters = Ship.CrewQuarters.Standard;
                else
                    _this.CurrentCrewQuarters = Ship.CrewQuarters.Cramped;

                if (weight > _this.Weight())
                    weightReductionLog.Add("Crew Quarters", weight - _this.Weight());

                weight = _this.Weight(false, true);

                if (weight < _this.tempGoodWeight) goto Done;
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight && (_this.shipType.name == "dd" || _this.shipType.name == "tb" || tryRatio > 25)
                && G.GameData.components.ContainsKey("shell_normal"))
            {
                _this.InstallComponent(G.GameData.components["shell_normal"]);

                if (weight > _this.Weight())
                    weightReductionLog.Add("Shell Size", weight - _this.Weight());

                weight = _this.Weight();

                if (weight < _this.tempGoodWeight) goto Done;
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight && (_this.shipType.name == "dd" || _this.shipType.name == "tb")
                && G.GameData.components.ContainsKey("ammo_shell_less"))
            {
                _this.InstallComponent(G.GameData.components["ammo_shell_less"]);

                if (weight > _this.Weight())
                    weightReductionLog.Add("Ammo Storage", weight - _this.Weight());

                weight = _this.Weight();

                if (weight < _this.tempGoodWeight) goto Done;
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight && (_this.shipType.name == "dd" || _this.shipType.name == "tb" || tryRatio > 25))
            {
                foreach (var cal in _this.shipGunCaliber)
                {
                    _this.SetCaliberDiameter(cal, 0);

                    if (weight > _this.Weight())
                        weightReductionLog.Add(
                            $"{cal.turretPartData.GetCaliberInch(_this)}\" {(Ship.IsCasemateGun(cal.turretPartData) ? "Casemate" : "Turret")} Diam",
                            weight - _this.Weight()
                        );

                    weight = _this.Weight();

                    if (weight < _this.tempGoodWeight) goto Done;
                }
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight && (_this.shipType.name == "dd" || _this.shipType.name == "tb" || tryRatio > 50))
            {
                foreach (var cal in _this.shipGunCaliber)
                {
                    _this.SetCaliberLength(cal, Mathf.Clamp(cal.length, -1000, 0));

                    if (weight > _this.Weight())
                        weightReductionLog.Add(
                            $"{cal.turretPartData.GetCaliberInch(_this)}\" {(Ship.IsCasemateGun(cal.turretPartData) ? "Casemate" : "Turret")} Len",
                            weight - _this.Weight()
                        );

                    weight = _this.Weight();

                    if (weight < _this.tempGoodWeight) goto Done;
                }
            }

            overweight = weight > _this.tempGoodWeight;
            if (!_this.IsShipWhitoutArmor())
            {
                float tempWeight = weight;
                var gaInfo = GenArmorData.GetInfoFor(_this);
                if (gaInfo != null)
                {
                    float oldArmorLerp = gaInfo.EstimateLerp(_this);
                    float curArmorLerp = oldArmorLerp;

                    _this.limiter = 699;
                    while (overweight && _this.limiter-- > 0)
                    {
                        oldArmorLerp = curArmorLerp;
                        while (curArmorLerp > 0f)
                        {
                            curArmorLerp = Mathf.Clamp(curArmorLerp - gaInfo.lerpStep, 0f, 1f);
                            if (gaInfo.SetArmor(_this, curArmorLerp))
                                break;
                        }

                        weight = _this.Weight();
                        if (weight < _this.tempGoodWeight)
                        {
                            // we'll take the reduction - _this.SetArmor(a, curAmt, true);
                            //weight = _this.Weight();
                            overweight = false;
                            break;
                        }
                    }
                }
                else
                {
                    Il2CppSystem.Collections.Generic.List<int> armorQueue;
                    switch (_this.shipType.name)
                    {
                        case "bb":
                            armorQueue = _this.armorReduceQueueBB;
                            break;
                        case "bc":
                            armorQueue = _this.armorReduceQueueBC;
                            break;
                        case "ca":
                            armorQueue = _this.armorReduceQueueCA;
                            break;
                        case "cl":
                            armorQueue = _this.armorReduceQueueCL;
                            break;
                        case "dd":
                            armorQueue = _this.armorReduceQueueDD;
                            break;
                        default:
                            armorQueue = new Il2CppSystem.Collections.Generic.List<int>();
                            armorQueue.Add((int)Ship.A.BeltBow);
                            armorQueue.Add((int)Ship.A.BeltStern);
                            armorQueue.Add((int)Ship.A.DeckBow);
                            armorQueue.Add((int)Ship.A.ConningTower);
                            armorQueue.Add((int)Ship.A.DeckStern);
                            armorQueue.Add((int)Ship.A.Deck);
                            armorQueue.Add((int)Ship.A.Belt); // Typo: the game uses BeltBow here
                            armorQueue.Add((int)Ship.A.InnerBelt_1st);
                            armorQueue.Add((int)Ship.A.InnerBelt_2nd);
                            armorQueue.Add((int)Ship.A.InnerBelt_3rd);
                            armorQueue.Add((int)Ship.A.InnerDeck_1st);
                            armorQueue.Add((int)Ship.A.InnerDeck_2nd);
                            armorQueue.Add((int)Ship.A.InnerDeck_3rd);
                            armorQueue.Add((int)Ship.A.TurretSide);
                            armorQueue.Add((int)Ship.A.TurretTop);
                            armorQueue.Add((int)Ship.A.Barbette);
                            armorQueue.Add((int)Ship.A.Superstructure);
                            break;
                    }

                    _this.limiter = 699;
                    bool changedArmor = true;
                    while (overweight && _this.limiter-- > 0 && changedArmor)
                    {
                        changedArmor = false;
                        foreach (var i in armorQueue)
                        {
                            Ship.A a = (Ship.A)i;
                            float min = _this.MinArmorForZone(a);
                            if (!_this.armor.TryGetValue(a, out var curAmt) || curAmt == min)
                                continue;

                            // This function is bugged. It's supposed to return
                            // a value based on how near max tonnage the ship is,
                            // but instead just returns 28 for armor and 7 for
                            // everything else. But we're going to use 2, like the
                            // add case.
                            float stepMult = 2f;
                            float armorStep = G.settings.armorStep;
                            float newAmt = G.settings.RoundToArmorStep(curAmt - armorStep * stepMult);
                            newAmt = Mathf.Clamp(newAmt, min, _this.MaxArmorForZone(a));
                            if (newAmt == curAmt)
                                continue;

                            changedArmor = true;
                            _this.SetArmor(a, newAmt, true);
                            weight = _this.Weight();
                            if (weight < _this.tempGoodWeight)
                            {
                                // we'll take the reduction - _this.SetArmor(a, curAmt, true);
                                //weight = _this.Weight();
                                overweight = false;
                                break;
                            }
                        }
                    }
                }

                if (tempWeight > _this.Weight())
                    weightReductionLog.Add("Armor", tempWeight - _this.Weight());
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight)
            {
                float tempWeight = weight;
                float minMS;
                float speedLimiter = _this.hull.data.speedLimiter;
                float year = _this.GetYear(_this);

                if (Config.ShipGenTweaks)
                {
                    minMS = GetMinSpeed(_this, speedLimit, rnd);
                }
                else
                {
                    float lowerClamp = speedLimit;
                    float remap, range;

                    if (_this.shipType.name == "dd")
                    {
                        range = Util.Range(0.925f, 1f, rnd);
                        remap = Util.Remap(year, 1890f, 1940f, 0.7f, range, true);
                        if (speedLimit <= 0f)
                            lowerClamp = speedLimiter * 1.15f;
                    }
                    else
                    {
                        switch (_this.hull.data.Generation)
                        {
                            case 1:
                                range = Util.Range(0.8f, 0.89f, rnd);
                                remap = Util.Remap(year, 1890f, 1940f, 0.85f, range, true);
                                if (speedLimit <= 0f)
                                    lowerClamp = speedLimiter * 0.9f;
                                break;
                            case 2:
                                range = Util.Range(0.85f, 0.95f, rnd);
                                remap = Util.Remap(year, 1890f, 1940f, 0.75f, range, true);
                                if (speedLimit <= 0f)
                                    lowerClamp = speedLimiter * 0.96f;
                                break;
                            case 3:
                                range = Util.Range(0.81f, 0.92f, rnd);
                                remap = Util.Remap(year, 1890f, 1940f, 0.75f, range, true);
                                if (speedLimit <= 0f)
                                    lowerClamp = speedLimiter;
                                break;
                            default:
                            case 4:
                                range = Util.Range(0.8f, 0.92f, rnd);
                                remap = Util.Remap(year, 1890f, 1940f, 0.7f, range, true);
                                if (speedLimit <= 0f)
                                    lowerClamp = speedLimiter;
                                break;
                        }
                    }

                    // The code is bugged here, it uses lowerClamp as the lower end of the clamp, so the range/remap
                    // are never used!
                    minMS = Mathf.Clamp(remap * speedLimiter, Mathf.Max(_this.shipType.speedMin, lowerClamp), _this.shipType.speedMax) * KnotsToMS;
                }

                if (_this.speedMax > minMS)
                {
                    float step = MonoBehaviourExt.Param("speed_step", 0.1f);

                    _this.limiter = 699;
                    while (overweight && _this.limiter-- > 0)
                    {
                        float oldSpeed = _this.speedMax;
                        float newSpeed = RoundSpeedToStep(oldSpeed - step);
                        if (newSpeed < minMS)
                            break;

                        _this.SetSpeedMax(newSpeed);
                        weight = _this.Weight();
                        if (weight < _this.tempGoodWeight)
                        {
                            overweight = false;
                            break;
                        }
                    }
                }

                if (tempWeight > _this.Weight())
                    weightReductionLog.Add("Speed", tempWeight - _this.Weight());
            }

            overweight = weight > _this.tempGoodWeight;
            var minOpRange = MinOpRange(_this, VesselEntity.OpRange.Low);
            if (overweight && _this.opRange > minOpRange)
            {
                float tempWeight = weight;

                if (tryRatio > 75)
                    _this.limiter = 4;
                else if (tryRatio > 50)
                    _this.limiter = 3;
                else if (tryRatio > 25)
                    _this.limiter = 2;
                else
                    _this.limiter = 1;

                while (_this.opRange > minOpRange && _this.limiter-- > 0)
                {
                    _this.isOpRangeDecreased = true;
                    _this.opRange = (VesselEntity.OpRange)((int)_this.opRange - 1);
                    _this.GetOpRangeInKmCalculate();
                    weight = _this.Weight();
                    if (weight < _this.tempGoodWeight)
                    {
                        overweight = false;
                        break;
                    }
                }

                if (tempWeight > _this.Weight())
                    weightReductionLog.Add("Op. Range", tempWeight - _this.Weight());
            }

            overweight = weight > _this.tempGoodWeight;
            if (overweight)
            {
                float tempWeight = weight;

                Ship.Survivability minSurv = Config.ShipGenTweaks ? MinSurv(_this, Ship.Survivability.High) : (_this.shipType.name == "dd" || _this.shipType.name == "tb" ? Ship.Survivability.Low : Ship.Survivability.High);
                while (_this.survivability > minSurv)
                {
                    _this.isSurvivabilityDecreased = true;
                    _this.SetSurvivability((Ship.Survivability)((int)_this.survivability - 1));
                    weight = _this.Weight();
                    if (weight < _this.tempGoodWeight)
                        break;
                }

                if (tempWeight > _this.Weight())
                    weightReductionLog.Add("Bulkheads", tempWeight - _this.Weight());
            }

        Done:

            if (Config.Param("taf_debug_shipgen_info", 0) == 0) return;

            if (startWeight == weight || weightReductionLog.Count == 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Failed to reduce weight: {(int)weight}t / {(int)tonnage}t");
                return;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  Reduce weight: {(int)startWeight}t -> {(int)weight}t / {(int)tonnage}t");

            float totalReduction = 0;

            foreach (var reduction in weightReductionLog)
            {
                totalReduction += reduction.Value;
                Melon<TweaksAndFixes>.Logger.Msg($"    {reduction.Key,-20} : -{reduction.Value:0.00}t");
            }

            Melon<TweaksAndFixes>.Logger.Msg($"    {"Total",-20} : -{totalReduction:0.00}t");

            return;
        }

        public static float GetMinSpeed(Ship _this, float speedLimit, Il2CppSystem.Random rnd)
            => speedLimit > 0f ? speedLimit
                : Mathf.Clamp(GetParamSpeedMultMin(_this, rnd) * _this.hull.data.speedLimiter, _this.shipType.speedMin, _this.shipType.speedMax) * KnotsToMS;

        public static float GetParamSpeedMultMin(Ship _this, Il2CppSystem.Random rnd)
        {
            string key = $"g{_this.hull.data.Generation}";
            if(!GetParamValue(_this, "speedMultByGen_min", key, out float mult))
                return GetMinSpeedMult(_this, rnd);

            if (rnd != null && GetParamValue(_this, "speedMultByGen_rand", key, out float rMult))
                mult *= Util.Range(1f - rMult, 1f + rMult, rnd);

            return mult;
        }

        public static float GetMinSpeedMult(Ship _this, Il2CppSystem.Random rnd)
        {
            float year = _this.GetYear(_this);
            float mult, randFactor;
            if (_this.shipType.name == "dd" || _this.shipType.name == "tb")
            {
                mult = 0.925f;
                randFactor = -0.027f;
            }
            else
            {
                switch (_this.hull.data.Generation)
                {
                    case 1:
                        randFactor = -0.1f;
                        mult = Util.Remap(year, 1890f, 1940f, 0.89f, 0.85f, true);
                        break;
                    case 2:
                        randFactor = -0.105f;
                        mult = Util.Remap(year, 1890f, 1940f, 0.836f, 0.855f, true);
                        break;
                    case 3:
                        randFactor = -0.12f;
                        mult = Util.Remap(year, 1890f, 1940f, 0.77f, 0.84f, true);
                        break;
                    default:
                    case 4:
                        randFactor = -0.13f;
                        mult = Util.Remap(year, 1890f, 1940f, 0.76f, 0.85f, true);
                        break;
                }
            }
            if (rnd != null)
                mult *= Util.Range(1f + randFactor, 1f, rnd);

            return mult;
        }

        public static float GetMaxSpeed(Ship _this, Il2CppSystem.Random rnd, out bool enforce)
            => Mathf.Min(_this.shipType.speedMax, _this.hull.data.speedLimiter * GetParamSpeedMultMax(_this, rnd, out enforce)) * KnotsToMS;

        public static float GetParamSpeedMultMax(Ship _this, Il2CppSystem.Random rnd, out bool enforce)
        {
            enforce = false;
            string key = $"g{_this.hull.data.Generation}";
            if (!GetParamValue(_this, "speedMultByGen_max", key, out float mult))
                return GetMaxSpeedMult(_this, rnd);

            if (mult < 0)
            {
                mult *= -1;
                enforce = true;
            }

            if (rnd != null && GetParamValue(_this, "speedMultByGen_rand", key, out float rMult))
                mult *= Util.Range(1f - rMult, 1f + rMult, rnd);
            
            return mult;
        }

        public static float GetMaxSpeedMult(Ship _this, Il2CppSystem.Random rnd)
        {
            float year = _this.GetYear(_this);
            float mult = _this.shipType.name == "cl" || _this.shipType.name == "tb" || _this.shipType.name == "dd" ? 1f : 1.05f;
            mult *= Util.Remap(year, 1890f, 1940f, 1.05f, 1.0f, true);
            if (rnd != null)
                mult *= Util.Range(0.95f, 1.05f, rnd);

            return mult;
        }

        public static float GetCitadelMultFromBase(Ship.A a)
        {
            float mult = 1f;
            switch (a)
            {
                case Ship.A.InnerBelt_3rd: mult *= 0.8f; goto case Ship.A.InnerBelt_2nd;
                case Ship.A.InnerBelt_2nd: mult *= 0.8f; goto case Ship.A.InnerBelt_1st;
                case Ship.A.InnerBelt_1st: return mult * 0.5f;

                case Ship.A.InnerDeck_3rd: mult *= 0.8f; goto case Ship.A.InnerDeck_2nd;
                case Ship.A.InnerDeck_2nd: mult *= 0.8f; goto case Ship.A.InnerDeck_1st;
                case Ship.A.InnerDeck_1st: return mult * 0.6f;

                default: return 0f;
            }
        }

        public static float GetCitadelArmorMax(Ship.A a, float belt, float deck)
        {
            if (a < Ship.A.InnerBelt_1st)
                return 0f;

            float val = a > Ship.A.InnerBelt_3rd ? deck : belt;
            return val * GetCitadelMultFromBase(a);
        }

        private static readonly char[] _OpSplitChars = new char[] { '[', ']' };
        public enum RPOp
        {
            Tag,
            Zero,
        }

        public struct RPOperation
        {
            public RPOp op;
            public bool invert;
            public string arg;
        }

        private static List<RPOperation> ParseOperations(Il2CppSystem.Collections.Generic.List<string> args)
        {
            var list = new List<RPOperation>();
            foreach (var arg in args)
            {
                var split = arg.Split(_OpSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                    continue;
                RPOp op = RPOp.Tag;
                bool invert = false;
                switch (split[0])
                {
                    case "tag": op = RPOp.Tag; break;
                    case "!tag": op = RPOp.Tag; invert = true;  break;
                    case "zero": op = RPOp.Zero; break;
                    case "!zero": op = RPOp.Zero; invert = true;  break;
                    default: continue;
                }
                list.Add(new RPOperation() { op = op, invert = invert, arg = split[1] });
            }

            return list;
        }

        private static bool CheckOperationProcess(PartData hull, Ship ship, List<RPOperation> ops, bool isOr, List<string> varsNonzero)
        {
            if (ops.Count == 0)
                return true;

            foreach (var op in ops)
            {
                bool value;
                switch (op.op)
                {
                    case RPOp.Tag:
                        value = hull.paramx.ContainsKey(op.arg);
                        break;
                    case RPOp.Zero:
                        if (ship != null)
                            value = ship.TechVar(op.arg) == 0f;
                        else if (varsNonzero != null)
                            value = !varsNonzero.Contains(op.arg);
                        else
                            value = true;
                        break;
                    default:
                        continue;
                }
                if (op.invert)
                    value = !value;

                if (isOr)
                {
                    if (value)
                        return true;
                }
                else
                {
                    if (!value)
                        return false;
                }
            }

            if (isOr)
                return false;
            else
                return true;
        }

        public static bool CheckOperation(PartData hull, Ship ship, RandPart rp, List<string> vars)
        {
            if (ship != null)
                hull = ship.hull.data;

            foreach (var kvp in rp.paramx)
            {
                switch (kvp.Key)
                {
                    case "or":
                    case "and":
                        //var orList = ParseOperations(kvp.Value);
                        //if (orList.Count > 0)
                        //    orOps.Add(orList);
                        if (!CheckOperationProcess(hull, ship, ParseOperations(kvp.Value), kvp.Key == "or", vars))
                            return false;
                        break;
                }
            }

            return true;
        }

        private static void FillVars(RandPart rp, List<string> vars)
        {
            foreach (var kvp in rp.paramx)
            {
                switch (kvp.Key)
                {
                    case "or":
                    case "and":
                        var list = ParseOperations(kvp.Value);
                        foreach (var op in list)
                            if (op.op == RPOp.Zero && !vars.Contains(op.arg))
                                vars.Add(op.arg);
                        break;
                }
            }
        }

        public static List<KeyValuePair<List<string>, List<RandPart>>> FindApplicableRPs(PartData hull)
        {
            var rps = hull.shipType.randParts;
            var result = new List<KeyValuePair<List<string>, List<RandPart>>>();
            var techVars = new List<string>();
            foreach (var rp in rps)
                FillVars(rp, techVars);

            List<string> tempVars = new List<string>();
            int numCombs = 1 << techVars.Count;
            for (int i = 0; i < numCombs; ++i)
            {
                for (int j = 0; j < techVars.Count; ++j)
                {
                    if ((j & i) != 0)
                        tempVars.Add(techVars[j]);
                }
                List<RandPart> curParts = new List<RandPart>();
                foreach (var rp in rps)
                {
                    if (CheckOperation(hull, null, rp, tempVars))
                        curParts.Add(rp);
                }
                bool newSet = true;
                foreach (var kvp in result)
                {
                    if (ModUtils.OrderedListsEqual(kvp.Value, curParts))
                    {
                        newSet = false;
                        break;
                    }
                }
                if (newSet)
                {
                    List<string> vars = new List<string>();
                    vars.AddRange(tempVars);
                    result.Add(new KeyValuePair<List<string>, List<RandPart>>(vars, curParts));
                }
                tempVars.Clear();
            }

            return result;
        }

        public static void LogRPsForHull(PartData hull)
        {
            var rpSet = FindApplicableRPs(hull);
            if (rpSet.Count == 0)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Error: something went wrong finding RandParts for hull {hull.name}");
                return;
            }
            bool logTech = rpSet.Count > 1 || rpSet[0].Key.Count > 0;

            string logstr = $"Finding RandParts for hull {hull.name}. Found {rpSet.Count} different set(s) of RandParts.";
            foreach (var set in rpSet)
            {
                if (logTech)
                    logstr += $"\nTech Vars set to nonzero: {set.Key.Count}. {string.Join(", ", set.Key)}";
                foreach (var rp in set.Value)
                    logstr += $"\n          {rp.name}";
            }

            Melon<TweaksAndFixes>.Logger.Msg(logstr);
        }

        public enum BatteryType
        {
            main = 0,
            sec = 1,
            ter = 2,

            COUNT,
        }

        public class GenGunInfo
        {
            public int[] maxCounts = new int[(int)BatteryType.COUNT];
            public List<int>[] caliberCounts = new List<int>[(int)BatteryType.COUNT];
            public int[] maxCalibers = new int[(int)BatteryType.COUNT];
            public int[] minCalibers = new int[(int)BatteryType.COUNT];
            public bool isLimited = false;

            public GenGunInfo()
            {
                for (int i = maxCounts.Length; i-- > 0;)
                    caliberCounts[i] = new List<int>();
            }

            public void FillFor(Ship ship)
            {
                isLimited = false;

                const int noLimitCount = 999;
                for (int i = 0; i < maxCounts.Length; ++i)
                {
                    caliberCounts[i].Clear();
                    BatteryType b = (BatteryType)i;
                    if (!GetParamValueByYear(ship, $"calCount_{b}", out int count))
                        count = Config.Param($"taf_shipgen_limit_calibercounts_{b}", noLimitCount);
                    maxCounts[i] = count;
                    if (count != noLimitCount)
                        isLimited = true;

                    if (ship.hull.data.paramx.TryGetValue($"ai_max_caliber_{b}", out var lst) && lst.Count > 0 && int.TryParse(lst[0], out int mc))
                    {
                        isLimited = true;
                        maxCalibers[i] = mc;
                    }
                    else
                    {
                        maxCalibers[i] = noLimitCount;
                    }
                    // Make sure we can't have a main limit lower than sec (or main/sec lower than ter)
                    if (i > 0 && maxCalibers[i - 1] < maxCalibers[i])
                        maxCalibers[i] = maxCalibers[i - 1];

                    if (ship.hull.data.paramx.TryGetValue($"ai_min_caliber_{b}", out lst) && lst.Count > 0 && int.TryParse(lst[0], out mc))
                    {
                        isLimited = true;
                        minCalibers[i] = mc;
                    }
                    else
                    {
                        minCalibers[i] = 0;
                    }

                    if (minCalibers[i] > maxCalibers[i])
                        minCalibers[i] = maxCalibers[i];
                }
            }

            public bool CaliberOK(BatteryType b, PartData data)
                => CaliberOK(b, (int)((data.caliber + 1f) * (1f / 25.4f)));

            public bool CaliberOK(BatteryType b, int cal)
            {
                int idx = (int)b;
                if (cal > maxCalibers[idx] || cal < minCalibers[idx])
                    return false;

                if (caliberCounts[idx].Count < maxCounts[idx])
                    return true;

                return caliberCounts[idx].Contains(cal);
            }

            public bool RegisterCaliber(BatteryType b, PartData data)
                => RegisterCaliber(b, (int)((data.caliber + 1f) * (1f / 25.4f)));

            public bool RegisterCaliber(BatteryType b, int cal)
            {
                int idx = (int)b;
                // Add unique. Lets us just always register.
                if (!caliberCounts[idx].Contains(cal))
                {
                    caliberCounts[idx].Add(cal);
                    return caliberCounts[idx].Count >= maxCounts[idx];
                }
                return false;
            }
        }
    }
}