using MelonLoader;
using UnityEngine;
using Il2Cpp;

#pragma warning disable CS8600
#pragma warning disable CS8603
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    public class CampaignControllerM
    {
        public static bool RequestForcedGameSave = false;
        public static int RequestForcedGameSaveFrameDelay = 0;
        private static GameObject BattleConclusionPopup = null;

        public static void Update()
        {
            if (!GameManager.Instance.isCampaign)
            {
                RequestForcedGameSave = false;
                return;
            }

            if (BattleConclusionPopup == null)
            {
                BattleConclusionPopup = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows/BattleWindow");
            }

            // Global/Ui/UiMain/WorldEx/PopWindows/BattleWindow

            if (RequestForcedGameSave && 
                (GameManager.Instance.CurrentState == GameManager.GameState.Constructor || GameManager.Instance.CurrentState == GameManager.GameState.World) && 
                !BattleConclusionPopup.active)
            {
                if (RequestForcedGameSaveFrameDelay-- > 0) return;

                Melon<TweaksAndFixes>.Logger.Msg($"Making auto save...");
                // GameManager.Instance.SaveCampaignProgress();
                GameManager.Instance.SaveInternal(true);
                Melon<TweaksAndFixes>.Logger.Msg($"Save complete!");

                RequestForcedGameSave = false;
            }
        }


        private struct ShipScrapInfo
        {
            public float score;
            public float weight;
            public Ship ship;

            public ShipScrapInfo(Ship s, float sc, float wgt)
            {
                ship = s;
                score = sc;
                weight = wgt;
            }
        }

        public static void HandleScrapping(CampaignController _this, Player player, bool prewarming)
        {
            if (!player.isAi)
                return;

            if (prewarming)
                return;

            if (Config.ScrappingSpread)
            {
                if (_this.CurrentDate.turn % G.GameData.playersMajor.Count != player.data.Id % G.GameData.playersMajor.Count)
                    return;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"Starting scrapping for player {player.data.name}");
            List<ShipScrapInfo> scrapCandidates = new List<ShipScrapInfo>();

            float buildTimeCoeff = Config.Param("taf_scrap_buildTimeCoeff", 1f);
            float mbAdd = Config.Param("taf_scrap_mothballScoreAddYears", 5f);
            int shipsScrapped = 0;
            float tonnageScrapped = 0;

            float totalTonnage = 0f;

            foreach (var s in player.GetFleetAll())
            {
                if (s.isScrapped || s.isSunk || s.isBuilding || s.isRefit)
                    continue;

                float score = _this.CurrentDate.YearsPassedSince(s.dateFinished) - buildTimeCoeff * (s.design == null ? s : s.design).BuildingTime(false);
                if (s.isMothballed || s.isLowCrew)
                    score += mbAdd;

                float weight = s.Weight();
                scrapCandidates.Add(new ShipScrapInfo(s, score, weight));
                totalTonnage += weight;
            }

            if (scrapCandidates.Count == 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"-------->Error: no potential ships to scrap!");
                return;
            }
            
            float maxTonnageParam = MonoBehaviourExt.Param("min_fleet_tonnage_for_scrap", 1f);
            float capTerm = Config.Param("taf_scrap_capacityCoeff", 0f) * Mathf.Pow(player.ShipbuildingCapacityLimit(), Config.Param("taf_scrap_capacityExponent", 1f));
            float hystAdd = Config.Param("taf_scrap_hysteresis", 50000f);
            float targetTonnage = maxTonnageParam + capTerm;

            if (totalTonnage < targetTonnage + hystAdd)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"--->Tonnage {totalTonnage:N0} less than target {targetTonnage:N0} with hysteresis {(targetTonnage+hystAdd):N0}, so aborting");
                return;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"--->Tonnage {totalTonnage:N0} surpassed target {targetTonnage:N0} with hysteresis {(targetTonnage + hystAdd):N0}, so continuing");

            // foreach (var c in scrapCandidates)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg($"------> Candidate {c.ship.Name(false, false).PadLeft(32)} : {c.score}");
            // }

            float toScrap = totalTonnage - targetTonnage;
            scrapCandidates.Sort((a, b) => b.score.CompareTo(a.score));
            int stopIdx;
            int iC = scrapCandidates.Count;
            for (stopIdx = 0; stopIdx < iC && toScrap > 0f; ++stopIdx)
            {
                toScrap -= scrapCandidates[stopIdx].weight;
            }
            if (stopIdx < iC)
                scrapCandidates.RemoveRange(stopIdx, iC - stopIdx);
            iC = stopIdx;

            // We know, by definition, we have to scrap the heaviest.
            // But we might not have to scrap everything else.
            toScrap = -toScrap;
            if (toScrap > 0f)
            {
                int idxHeaviest = iC - 1;
                float heaviest = scrapCandidates[idxHeaviest].weight;
                for (int i = iC; i-- > 0;)
                {
                    var ssi = scrapCandidates[i];
                    if (ssi.weight > heaviest)
                    {
                        idxHeaviest = i;
                        heaviest = ssi.weight;
                    }
                }
                ++shipsScrapped;
                tonnageScrapped += scrapCandidates[idxHeaviest].weight;
                _this.ScrapShip(scrapCandidates[idxHeaviest].ship, true);
                scrapCandidates.RemoveAt(idxHeaviest);
                --iC;

                // Greedily remove ships, worst score first.
                for (int i = iC; i-- > 0;)
                {
                    var ssi = scrapCandidates[i];
                    if (ssi.weight <= toScrap)
                    {
                        toScrap -= ssi.weight;
                        scrapCandidates.RemoveAt(i);
                    }
                }
            }

            // Finally, scrap all (remaining) ships to scrap
            for (int i = scrapCandidates.Count; i-- > 0;)
            {
                _this.ScrapShip(scrapCandidates[i].ship, true);
                ++shipsScrapped;
                tonnageScrapped += scrapCandidates[i].weight;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"---->Finished scrapping, {shipsScrapped} ships, {tonnageScrapped:N0} tons");
        }

        public static List<CampaignController.TaskForce> GetTaskForceInsideRadius(CampaignController _this, Vector3 coord, float radiusKm, CampaignController.TaskForce except, Func<CampaignController.TaskForce, bool> predicate)
        {
            List<CampaignController.TaskForce> ret = null;
            foreach (var tf in _this.CampaignData.TaskForces)
            {
                if (predicate != null && !predicate(tf))
                    continue;
                if (tf == except)
                    continue;

                float zone = tf.GetZoneRadius(false);
                float distSqr = (zone + radiusKm) * 0.005f;
                distSqr *= distSqr;
                if ((coord - tf.WorldPos).sqrMagnitude > distSqr)
                    continue;

                if (ret == null)
                    ret = new List<CampaignController.TaskForce>();
                ret.Add(tf);
            }

            return ret;
        }

        public static Ship GetSharedDesign(CampaignController _this, Player player, ShipType shipType, int year, bool checkTech = true, bool isEarlySavedShip = false)
        {
            //Melon<TweaksAndFixes>.Logger.Msg($"Getting shared design for {shipType.name} of {player.data.name}");

            if (!G.GameData.sharedDesignsPerNation.TryGetValue(player.data.name, out var designs))
                return null;

            List<Ship.Store> newerShips = new List<Ship.Store>();
            List<Ship.Store> olderShips = new List<Ship.Store>();
            int oldestNew = int.MaxValue;
            int newestOld = int.MinValue;
            List<float> techCoverage = new List<float>();
            int maxYearUp = Mathf.RoundToInt(Config.Param("taf_shareddesign_maxYearsIntoFuture", 10f));
            int maxYearDpwnForSplit = Mathf.RoundToInt(Config.Param("taf_shareddesign_yearsInPastForSplit", 5f));
            foreach (var tuple in designs)
            {
                var store = tuple.Item1;
                if (store.shipType != shipType.name)
                    continue;

                if (store.YearCreated > year + maxYearUp)
                    continue;

                if (store.YearCreated < year - maxYearDpwnForSplit)
                {
                    olderShips.Add(store);
                    if (store.YearCreated > newestOld)
                        newestOld = store.YearCreated;
                }
                else
                {
                    newerShips.Add(store);
                    techCoverage.Add(0f);
                    if (store.YearCreated < oldestNew)
                        oldestNew = store.YearCreated;
                }
            }
            //Melon<TweaksAndFixes>.Logger.Msg($"Found {newerShips.Count} / {olderShips.Count} ships");
            if (newerShips.Count == 0 && olderShips.Count == 0)
                return null;

            if (newerShips.Count == 0)
                oldestNew = newestOld - 5;

            // If some ships are borderline, grab them too.
            int yearBorder = Mathf.RoundToInt(Config.Param("taf_shareddesign_yearClosenessAroundSplit", 3f));
            if (newestOld + yearBorder > oldestNew)
            {
                for (int i = olderShips.Count - 1; i >= 0; --i)
                {
                    if (olderShips[i].YearCreated + yearBorder > oldestNew)
                    {
                        newerShips.Add(olderShips[i]);
                        techCoverage.Add(0f);
                        olderShips.RemoveAt(i);
                    }
                }
            }
            //Melon<TweaksAndFixes>.Logger.Msg($"Counts now {newerShips.Count} / {olderShips.Count} ships");

            if (checkTech)
                CachePlayerTechs(player);

            for (int i = 0; i < newerShips.Count; ++i)
            {
                var ship = Ship.Create(null, null, false, false, false);
                var guid = new Il2CppSystem.Nullable<Il2CppSystem.Guid>();
                if (!ship.FromStore(newerShips[i], guid, null, null, false))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Couldn't load {newerShips[i].vesselName} ({newerShips[i].hullName}, {newerShips[i].YearCreated})");
                    ship.Erase();
                    continue;
                }

                ship.SetActive(false);
                if (!isEarlySavedShip)
                {
                    if (PlayerController.Instance == null)
                    {
                        ship.Erase();
                        continue;
                    }
                    if (!PlayerController.Instance.CanBuildShipsFromDesign(ship, out _))
                    {
                        ship.Erase();
                        continue;
                    }
                }
                float techVal;
                if (checkTech)
                {
                    //TODO: Swap to store-based tech match
                    techVal = TechMatchRatio(ship);
                }
                else
                {
                    // until checking further whether tech will be reliable, don't trust tech commonality in this case
                    float yearDelta = Mathf.Abs(year - newerShips[i].YearCreated);
                    yearDelta += 1f;
                    yearDelta *= yearDelta;
                    techVal = 1f / (1f + yearDelta * 0.018f);
                }
                //Melon<TweaksAndFixes>.Logger.Msg($"Tech Val for {newerShips[i].vesselName} ({newerShips[i].hullName}, {newerShips[i].YearCreated}) is {techVal:F3}");
                if (techVal < 0f)
                {
                    ship.Erase();
                    continue;
                }

                techCoverage[i] = techVal;

                ship.Erase();
            }
            CleanupSDCaches();

            List<int> indices = new List<int>();
            List<int> indicesMed = new List<int>();
            List<int> indicesLow = new List<int>();
            float bestVal = Config.Param("taf_shareddesign_bestTechValue", 0.9f);
            float okVal = Config.Param("taf_shareddesign_okTechValue", 0.75f);
            float minVal = Config.Param("taf_shareddesign_minTechValue", 0.5f);
            for (int i = 0; i < techCoverage.Count; ++i)
                if (techCoverage[i] > bestVal)
                    indices.Add(i);
                else if (techCoverage[i] > okVal)
                    indicesMed.Add(i);
                else if (techCoverage[i] > minVal)
                    indicesLow.Add(i);

            if (indices.Count == 0)
                indices = indicesMed;

            if (indices.Count == 0)
                indices = indicesLow;

            if (indices.Count == 0)
                return null;

            //Melon<TweaksAndFixes>.Logger.Msg($"Choosing from {indices.Count} choices");
            int idx = indices[ModUtils.Range(0, indices.Count - 1)];
            var shipRet = Ship.Create(null, null, false, false, false);
            var guidRet = new Il2CppSystem.Nullable<Il2CppSystem.Guid>();
            if (!shipRet.FromStore(newerShips[idx], guidRet, null, null, false))
            {
                Melon<TweaksAndFixes>.Logger.Error($"Couldn't load {newerShips[idx].vesselName} ({newerShips[idx].hullName}, {newerShips[idx].YearCreated})");
                shipRet.Erase();
                return null;
            }

            shipRet.SetActive(false);

            if (shipRet.id == Il2CppSystem.Guid.Empty)
            {
                shipRet.id = Il2CppSystem.Guid.NewGuid();
            }

            return shipRet;
        }

        private enum TechRelevance
        {
            None,
            OlderComponent,
            Improvement,
            Required,

            COUNT
        }

        private static TechRelevance GetTechRelevance(Ship ship, TechnologyData tech, bool isOnShip, HashSet<CompType> availableTypes, int torpedoTubes)
        {
            if (tech.componentx != null)
            {
                if (!ship.IsComponentTypeAvailable(tech.componentx.typex))
                    return TechRelevance.None;

                // Assume if the player hasn't selected any component for this type, they don't want
                // any component, so newer options aren't relevant. If it wasn't contained in the
                // components that were available to the ship, then the tech counts as an improvement
                if (!ship.components.TryGetValue(tech.componentx.typex, out var comp))
                    return availableTypes.Contains(tech.componentx.typex) ? TechRelevance.None : TechRelevance.Improvement;
                else if (comp == tech.componentx)
                    return TechRelevance.Required;

                
                return comp.tech.year < tech.componentx.tech.year ? TechRelevance.Improvement : TechRelevance.OlderComponent;
            }

            TechRelevance bestRel = TechRelevance.None;

            foreach (var tc in ship.shipGunCaliber)
            {
                int calIn = Mathf.RoundToInt(tc.turretPartData.caliber * (1f / 25.4f));
                int grade = ship.TechGunGrade(tc.turretPartData);
                for (int g = 1; g < Config.MaxGunGrade + 1; ++g)
                {
                    var gTech = Database.GetGunTech(calIn, g);
                    if (gTech == tech.name)
                    {
                        if (g <= grade)
                            return TechRelevance.Required;
                        else if (!isOnShip)
                            bestRel = Max(bestRel, TechRelevance.Improvement);
                        
                        break;
                    }

                }
            }

            var partsUnlocked = Database.GetPartNamesForTech(tech.name);
            if (partsUnlocked != null)
            {
                    foreach (var part in ship.parts)
                        if (partsUnlocked.Contains(part.data.name))
                            return TechRelevance.Required;
                // FIXME do we care about better towers? Or funnels?
                // For now, let's say no.
                // (So we skip it isOnShip extra handling.)
            }

            foreach (var kvp in tech.effects)
            {
                switch (kvp.Key)
                {
                    case "start": break;
                    // handled above.
                    case "unlockpart": break;
                    case "unlock": break;
                    case "gun": break;
                    case "tonnage": break; // handled in main method

                    case "torp_mark":
                        if (torpedoTubes != -1)
                            if (isOnShip)
                                return TechRelevance.Required;

                            bestRel = Max(bestRel, TechRelevance.Improvement);
                        break;
                    case "torpedo_tubes":
                        if (torpedoTubes == -1)
                            break;

                        int.TryParse(kvp.Value[0][0], out var tubes);
                        if (torpedoTubes >= tubes)
                            return TechRelevance.Required;
                        // FIXME In the case where the tech allows more tubes than used
                        // it's not _necessarily_ an improvement, so do nothing.
                        break;

                    // FIXME ignore DC or Mines maybe?

                    default:
                        if (isOnShip)
                            return TechRelevance.Required;
                        bestRel = Max(bestRel, TechRelevance.Improvement);
                        break;
                }
            }

            return bestRel;
        }

        private static TechRelevance Max(TechRelevance a, TechRelevance b) => a > b ? a : b;


        private static readonly HashSet<TechnologyData> _TechDatasPlayer = new HashSet<TechnologyData>();
        private static readonly HashSet<string> _TechsPlayer = new HashSet<string>();
        private static readonly HashSet<string> _TechsShip = new HashSet<string>();
        private static readonly HashSet<string> _ExtraReqTechs = new HashSet<string>();
        private static readonly HashSet<CompType> _CompTypes = new HashSet<CompType>();
        private static readonly HashSet<string> _UsedComps = new HashSet<string>();
        private static readonly int[] _TechRelevanceCountsPlayer = new int[(int)TechRelevance.COUNT];
        private static readonly int[] _TechRelevanceCountsShip = new int[(int)TechRelevance.COUNT];

        public static void CleanupSDCaches()
        {
            _TechDatasPlayer.Clear();
            _TechsPlayer.Clear();
            _TechsShip.Clear();
            _ExtraReqTechs.Clear();
            _CompTypes.Clear();
            _UsedComps.Clear();
        }

        public static int CachePlayerTechs(Player player, bool noRandomYearOffset = false)
        {
            _TechDatasPlayer.Clear();
            _TechsPlayer.Clear();
            int maxYear = -1;
            foreach (var tech in player.technologies)
            {
                if (tech.progress == 100f || tech.IsEndTechResearched && tech.Index > 0)
                {
                    _TechDatasPlayer.Add(tech.data);
                    _TechsPlayer.Add(tech.data.name);
                    int year = GameManager.GetTechYear(tech.data, true, noRandomYearOffset);
                    if (year > maxYear)
                        maxYear = year;
                }
            }
            return maxYear;
        }

        public static float TechMatchRatio(Ship.Store store)
        {
            _TechsShip.Clear();
            _UsedComps.Clear();
            _CompTypes.Clear();
            _ExtraReqTechs.Clear();
            foreach (var t in store.techs)
            {
                if (Database.IsTechRequiredForShipDesign(t)
                    && !_TechsPlayer.Contains(t))
                    return -1f;

                _TechsShip.Add(t);
            }

            int maxTubeCount = -1;
            foreach (var p in store.parts)
            {
                if (!G.GameData.parts.TryGetValue(p.name, out var part))
                    return -1f;
                var tech = Database.GetPartTech(p.name);
                if (!string.IsNullOrEmpty(tech))
                {
                    if (!_TechsPlayer.Contains(tech))
                        return -1f;
                    _ExtraReqTechs.Add(tech);
                }

                if (part.type != "torpedo")
                    continue;
                var bar = part.name.Contains("x0") ? 0 : part.barrels;
                if (maxTubeCount < bar)
                    maxTubeCount = bar;
            }

            foreach (var kvp in store.components)
            {
                var tech = Database.GetCompTech(kvp.Value);
                if (!string.IsNullOrEmpty(tech))
                {
                    if (!_TechsPlayer.Contains(tech))
                        return -1f;
                    _ExtraReqTechs.Add(tech);
                }
            }

            foreach (var tcs in store.turretCalibers)
            {
                int grade = TAFShipData.GunGradeData.GradeFromTCSDataName(tcs.turretPartDataName, out string baseName);
                int cal = Mathf.RoundToInt(G.GameData.parts[baseName].caliber * (1f / 25.4f));
                string tech;
                if (grade < 1)
                {
                    grade = Database.MaxGunGradeFromTechs(cal, _TechsShip, out tech);
                }
                else
                {
                    tech = Database.GetGunTech(cal, grade);
                }
                if (!_TechsPlayer.Contains(tech))
                    return -1f;
                _ExtraReqTechs.Add(tech);
                for (int i = grade - 1; i > 0; --i)
                    _ExtraReqTechs.Add(Database.GetGunTech(cal, i));
            }

            if (maxTubeCount >= 0)
            {
                string tubeTech = Database.GetTorpTubeTech(maxTubeCount);
                if (!_TechsPlayer.Contains(tubeTech))
                    return -1f;
                _ExtraReqTechs.Add(tubeTech);
                for (int i = maxTubeCount - 1; i-- > 0;)
                    _ExtraReqTechs.Add(Database.GetTorpTubeTech(i));

                int grade = TAFShipData.TorpGradeFromStore(store, false);
                string tech;
                if (grade < 0)
                    grade = Database.MaxTorpGradeFromTechs(_TechsShip, out tech);
                else
                    tech = Database.GetTorpGradeTech(grade);
                if (!_TechsPlayer.Contains(tech))
                    return -1f;
                _ExtraReqTechs.Add(tech);
                for (int i = grade - 1; i > 0; --i)
                    _ExtraReqTechs.Add(Database.GetTorpGradeTech(i));
            }

            int reqTechsShared = 0;
            int impTechsPlayer = 0;
            foreach (var pt in _TechsPlayer)
            {
                if (_TechsShip.Contains(pt))
                {
                    if (Database.IsTechRequiredForShipDesign(pt) || _ExtraReqTechs.Contains(pt))
                        ++reqTechsShared;
                }
                // We will be slightly wrong here: we will count techs that
                // unlock components that are not available to this shiptype.
                // But they will be uniformly unavailable, so the ordering of
                // tech match scores should remain unchanged.
                else if (Database.IsTechImprovementForShipDesign(pt))
                    ++impTechsPlayer;
            }

            return reqTechsShared / (float)(reqTechsShared + impTechsPlayer);
        }

        public static float TechMatchRatio(Ship design)
        {
            _TechsShip.Clear();
            for (int i = (int)TechRelevance.COUNT; i-- > 0;)
            {
                _TechRelevanceCountsPlayer[i] = 0;
                _TechRelevanceCountsShip[i] = 0;
            }

            int maxTubeCount = -1;
            foreach (var p in design.parts)
            {
                if (!p.data.isTorpedo)
                    continue;
                var bar = p.data.name.Contains("x0") ? 0 : p.data.barrels;
                if (maxTubeCount < bar)
                    maxTubeCount = bar;
            }
            //Melon<TweaksAndFixes>.Logger.Msg($"Tech ratio: {design.vesselName} ({design.hull.name}: torps {torpedoTubes}");
            // Kinda slow, but eh.
            foreach (var c in G.GameData.components.Values)
                if (design.IsComponentAvailable(c))
                    _CompTypes.Add(c.typex);

            foreach (var tech in _TechDatasPlayer)
            {
                // Don't bother counting it if the ship already uses it
                if (design.techs.Contains(tech))
                    continue;
                var rel = GetTechRelevance(design, tech, false, _CompTypes, maxTubeCount);
                ++_TechRelevanceCountsPlayer[(int)rel];
            }
            //Melon<TweaksAndFixes>.Logger.Msg($"Tech ratio: {design.vesselName} ({design.hull.name}: Found {_TechsPlayer.Count} player techs, {design.techs.Count} ship techs");

            foreach (var tech in design.techs)
            {
                var rel = GetTechRelevance(design, tech, true, _CompTypes, maxTubeCount);
                //Melon<TweaksAndFixes>.Logger.Msg($"Tech ratio: {design.vesselName} ({design.hull.name}: tech {tech.name} has relevance {rel}");
                ++_TechRelevanceCountsShip[(int)rel];
                if (rel == TechRelevance.Required && !_TechDatasPlayer.Contains(tech))
                    return -1f;
            }

            // We know we don't lack any required techs. Let's see how closely we match.
            // FIXME ignore older components, and ignore mismatch in requireds (which should never happen).
            // Also completely ignore any ship improvement techs that aren't relevant to the ship.
            return _TechRelevanceCountsShip[(int)TechRelevance.Required] / (float)(_TechRelevanceCountsShip[(int)TechRelevance.Required] + _TechRelevanceCountsPlayer[(int)TechRelevance.Improvement]);
        }

        public static AllianceRelation GetAllianceRelation(Player a, Player b)
        {
            var CD = CampaignController.Instance.CampaignData;
            var allianceA = CD.GetPlayerAlliance(a);
            var allianceB = CD.GetPlayerAlliance(b);
            if (allianceA == null || allianceB == null)
                return null;

            foreach (var kvp in CD.AllianceRelations)
            {
                if ((kvp.Key.Key == allianceA && kvp.Key.Value == allianceB)
                    || (kvp.Key.Key == allianceB && kvp.Key.Value == allianceA))
                {
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}
