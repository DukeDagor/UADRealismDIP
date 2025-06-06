﻿using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using TweaksAndFixes;

#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8625

namespace UADRealism
{
    [HarmonyPatch(typeof(Ship))]
    internal class Patch_Ship
    {
        internal static bool _IsChangeHull = false;
        internal static bool _IsLoadingStore = false;
        internal static bool _IsRefreshPatched = false;

        internal struct RefreshHullData
        {
            public float _draught;
            public float _tonnage;
            public float _yPosScaling;
            public float _oldYScale;
            public bool _valuesSet;
        }

        internal static System.Collections.IEnumerator RefreshHullRoutine(Ship ship)
        {
            yield return new WaitForEndOfFrame();
            if (ship.modelState == Ship.ModelState.Constructor || ship.modelState == Ship.ModelState.Battle)
                ship.RefreshHull(false); // will be made true if sections need updating
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.ChangeHull))]
        internal static void Prefix_Ship_ChangeHull(Ship __instance, PartData data, Ship.Store store, ref bool byHuman, out bool __state)
        {
            _IsChangeHull = true;
            if (store != null)
            {
                _IsLoadingStore = true;
                __instance.ModData().SetIgnoreNextPartYChange(true);
            }

            __instance.ModData().OnChangeHullPre(data);

            __state = byHuman;
            // We can't reach into the middle of this code.
            // So instead we'll manually handle the "byHuman" case that
            // invokes AdjustHullStats when we get to our postfix.
            // (Before net6 Harmony was crashing on nullables, but it works
            // now so we can prefix ok, but we'll keep this workaround.)

            byHuman = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.ChangeHull))]
        internal static void Postfix_Ship_ChangeHull(Ship __instance, Ship.Store store, bool __state)
        {
            // Stock code bug: shiptypes give speed min/max in knots, but code
            // uses m/s. So we have to reset speedmax here. This uses the same
            // random as the game.
            __instance.speedMax = Mathf.Clamp(ModUtils.Range(0.9f, 1.1f) * __instance.hull.data.speedLimiter, __instance.shipType.speedMin, __instance.shipType.speedMax) * ShipStats.KnotsToMS;
            if (!_IsLoadingStore)
            {
                ShipM.SetShipBeamDraughtFineness(__instance);
                // and we should rerurn this since hull refreshed
                __instance.hull.AddOrUpdatePlacedObjects();
            }


            if (__state) // i.e. if byHuman
            {
                float minT = __instance.TonnageMin();
                float maxT = __instance.TonnageMax();
                if (__instance.player != null)
                {
                    float tLim = __instance.player.TonnageLimit(__instance.shipType);
                    maxT = Math.Min(maxT, tLim);
                }
                __instance.SetTonnage(Mathf.Lerp(minT, maxT, Util.Range(0f, 1f)));

                float oldSpeed = __instance.speedMax;

                float year = __instance.GetYear(__instance);
                float targetWeight = Util.Remap(year, 1890f, 1940f, 0.6f, 0.7f, true);
                ShipM.AdjustHullStats(__instance, -1, targetWeight, new System.Func<bool>(() =>
                {
                    float newTargetWeight = Util.Remap(year, 1890f, 1940f, 0.65f, 0.75f, true);
                    return newTargetWeight >= __instance.Weight() / __instance.Tonnage();

                }), null);

                // If our speed was adjusted, we should change hull geometry to optimize.
                if (oldSpeed != __instance.speedMax)
                {
                    ShipM.SetShipBeamDraughtFineness(__instance);
                    // and we should rerurn this.
                    __instance.hull.AddOrUpdatePlacedObjects();
                }
            }

            _IsChangeHull = false;
            _IsLoadingStore = false;

            // This refresh may or may not be needed, but it won't really hurt.
            // It's to catch the case where ChangeHull runs _during_ a scene transition,
            // i.e. before the scene is set to Constructor.
            MelonCoroutines.Start(RefreshHullRoutine(__instance));
        }

        private static readonly List<Part> _GunMatsToClear = new List<Part>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.RefreshHull))]
        internal static void Prefix_Ship_RefreshHull(Ship __instance, ref bool updateSections, out RefreshHullData __state)
        {

            __state = new RefreshHullData();

            if (__instance == null || __instance.hull == null)
            {
                Melon<UADRealismMod>.Logger.BigError("Ship or hull null!");
                return;
            }

            var data = __instance.hull.data;
            if (data == null)
            {
                Melon<UADRealismMod>.Logger.BigError("hull PartData null!");
                return;
            }

            // We are probably going to change hull geometry.
            // That means we need to remove from cache any parts
            // that rely on hull geometry for their weight (i.e.
            // barbette heights). The hull will get updated regardless.
            if (__instance.matsCache != null)
            {
                foreach (var kvp in __instance.matsCache)
                {
                    if (kvp.Key.data.isGun)
                        _GunMatsToClear.Add(kvp.Key);
                }
                foreach (var p in _GunMatsToClear)
                    __instance.matsCache.Remove(p);
                _GunMatsToClear.Clear();
                __instance.weightsValid = false;
            }

            var hData = ShipStats.GetData(data);
            if (hData == null)
            {
                Melon<UADRealismMod>.Logger.BigError("Couldn't find hulldata for " + ShipStats.GetHullModelKey(data));
                return;
            }

            _IsRefreshPatched = true;

            __state._tonnage = __instance.tonnage;
            __state._draught = __instance.draught;

            int secsToUse = __instance.ModData().SectionsFromFineness();

            if (__instance.hull.middles == null)
            {
                Melon<UADRealismMod>.Logger.BigError("hull middles list null!");
            }
            else
            {
                updateSections = secsToUse != __instance.hull.middles.Count;
            }

            if (hData._statsSet.Length <= secsToUse || secsToUse < 0)
            {
                Melon<UADRealismMod>.Logger.BigError($"SecsToUse ({secsToUse}) out of bounds for StatsSet length {hData._statsSet.Length}");
                return;
            }

            float scaleFactor = ShipStats.GetHullScaleFactor(__instance, hData._statsSet[secsToUse].Vd) / __instance.hull.model.transform.localScale.z;
            float oldScaleYLocal = __instance.hull.bow.GetParent().GetParent().transform.localScale.y;
#if LOGSHIP
            Debug.Log($"{hData._key}: tonnage desired: {__instance.tonnage:F0} in ({data.tonnageMin:F0},{data.tonnageMax:F0}). Scale {scaleFactor:F3} (total {(scaleFactor * __instance.hull.model.transform.localScale.z):F3}). Vd for 1/1={hData._statsSet[secsToUse].Vd:F0}\nS={secsToUse} ({data.sectionsMin}-{data.sectionsMax}).");
#endif
            __instance.hull.bow.GetParent().GetParent().transform.localScale = Vector3.one * scaleFactor;

            // We have to fake our tonnage so base RefreshHull sets the right number of sections
            float slider = Mathf.InverseLerp(data.sectionsMin - 0.499f, data.sectionsMax + 0.499f, secsToUse);
            __instance.tonnage = Mathf.Lerp(data.tonnageMin, data.tonnageMax, slider);
            float bmMult = 1f + __instance.beam * 0.01f;
            // We don't scale the hull height based on draught, just beam
            // (to preserve beam/draught ratio). We'll scale Y based on freeboard.
            float freeboardMult = (1f + __instance.ModData().Freeboard * 0.01f);
            __state._yPosScaling = bmMult * freeboardMult;
            __state._oldYScale = oldScaleYLocal * __instance.hull.bow.transform.localScale.y;
            __instance.draught = (__state._yPosScaling - 1f) * 100f;
            __state._valuesSet = true;
        }

        private static readonly Il2CppSystem.Collections.Generic.List<GameObject> _hullSecitons = new Il2CppSystem.Collections.Generic.List<GameObject>();
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.RefreshHull))]
        internal static void Postfix_Ship_RefreshHull(Ship __instance, RefreshHullData __state)
        {
            if (!_IsRefreshPatched || !__state._valuesSet)
                return;

            __instance.draught = __state._draught;
            __instance.tonnage = __state._tonnage;

            var hData = ShipStats.GetData(__instance.hull.data);

            // Fix Y positions so the waterline is preserved (B/T changing shouldn't change waterline)
            _hullSecitons.AddRange(__instance.hull.hullSections);
            foreach (var sec in _hullSecitons)
            {
                sec.transform.localPosition = new Vector3(sec.transform.localPosition.x, hData._yPos * __state._yPosScaling, sec.transform.localPosition.z);
            }
            _hullSecitons.Clear();

            // If this is user-initiated, we should keep parts
            // at deck-height
            if (!_IsLoadingStore && !__instance.ModData().IgnoreNextPartYChange)
            {
                float scaleRatio = __instance.hull.bow.GetParent().GetParent().transform.localScale.y * __instance.hull.bow.transform.localScale.y / __state._oldYScale;
                foreach (Part p in __instance.parts)
                {
                    if (p == __instance.hull)
                        continue;
                    // The above offset code makes sure the hull remains centered around the waterline, and the waterline is the origin
                    // so this should just work
                    p.transform.localPosition = new Vector3(p.transform.localPosition.x, p.transform.localPosition.y * scaleRatio, p.transform.localPosition.z);
                }
            }
            __instance.ModData().SetIgnoreNextPartYChange(false);
        }

#if disabled
        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Ship.CalcInstability))]
        //internal static void Prefix_CalcInstability(Ship __instance)
        //{
        //    if (__instance == null || __instance.armor == null || __instance.hullSize == null)
        //        return;

        //    isInCalcInstability = true;
        //    hasSetFirstInstability = false;
        //    isCalcInstabilityPatched = false;
        //    Melon<UADRealismMod>.Logger.Msg("Starting CalcInstability");
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(nameof(Ship.CalcInstability))]
        //internal static void Postfix_CalcInstability(Ship __instance)
        //{
        //    isInCalcInstability = false;
        //    if (!isCalcInstabilityPatched)
        //        return;

        //    isCalcInstabilityPatched = false;

        //    __instance.armor.Clear();
        //    foreach (var kvp in oldArmor)
        //        __instance.armor[kvp.Key] = kvp.Value;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Ship.instability), MethodType.Setter)]
        //internal static void Prefix_Set_Instability(Ship __instance)
        //{
        //    if (!isInCalcInstability)
        //        return;

        //    hasSetFirstInstability = true;
        //    Melon<UADRealismMod>.Logger.Msg("Set instability, now doing instab2");
        //}
#endif

        private static List<float> oldTurretArmors = new List<float>();

        // We're going to use this to fix HP in CStats
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.CalcInstability))]
        internal static void Prefix_CalcInstability(Ship __instance, bool force)
        {
            if (force)
                return;

            var fbSV = new Ship.StatValue();
            fbSV.basic = Util.Remap(__instance.ModData().Freeboard, ShipData._MinFreeboard, ShipData._MaxFreeboard, 0f, 100f);
            __instance.stats_[G.GameData.stats["freeboard"]] = fbSV;

            // We're inside Ship.CStats and we just computed smoke_exhaust and fcap.
            // But they use the stock HP calcs, bleh. So we recompute
            __instance.stats_.TryGetValue(G.GameData.stats["fcap"], out var fcapSV);
            if (fcapSV == null)
                return;

            __instance.stats_.TryGetValue(G.GameData.stats["smoke_exhaust"], out var smokeSV);
            if (smokeSV == null)
                return;

            float hpTofcap = MonoBehaviourExt.Param("engine_hp_to_fcap", 0.001f);
            float fcap = fcapSV.total;
            smokeSV.basic = 100f * Mathf.Clamp(fcap / (hpTofcap * ShipStats.GetHPRequired(__instance)), 0f, 3f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.CalcInstability))]
        internal static void Postfix_CalcInstability(Ship __instance)
        {
            if (ShipStats._IsRenderingHulls || __instance == null)
                return;

            if (__instance.hullSize.size.x == 0f
                || __instance.hullSize.size.y == 0f
                || __instance.hullSize.size.z == 0f
                || __instance.hullSize.min.y == 0f)
                return;

            var stats = ShipStats.GetScaledStats(__instance);
            float SHP = ShipStats.GetSHPRequired(__instance);
            float ihpMult = ShipStats.GetEngineIHPMult(__instance);
            string beamStr = stats.B.ToString("F2");
            if (stats.beamBulge != stats.B)
            {
                beamStr += $" ({stats.beamBulge:F2} at {stats.bulgeDepth:F2})";
            }
            string hp = ihpMult == 1f ? $"{SHP:N0} SHP" : $"{(SHP * ihpMult):N0} IHP";
#if LOGSHIP
            Melon<UADRealismMod>.Logger.Msg($"{__instance.vesselName}: {stats.Lwl:F2}x{beamStr}x{stats.T:F2} ({(stats.Lwl/stats.beamBulge):F1},{(stats.beamBulge/stats.T):F1}), {(stats.Vd * ShipStats.WaterDensity * 0.001d):F0}t. Cb={stats.Cb:F3}, Cm={stats.Cm:F3}, Cwp={stats.Cwp:F3}, Cp={stats.Cp:F3}, Cvp={stats.Cvp:F3}. {hp} (x{ihpMult:F2}) for {(__instance.speedMax/ShipStats.KnotsToMS):F1}kn");
#endif
            Patch_Ui._ShipForEnginePower = __instance;
            Melon<UADRealismMod>.Logger.Msg($"{__instance.vesselName}: {stats.Lwl:F2}x{beamStr}x{stats.T:F2} Cb={stats.Cb:F3}/Cwp={stats.Cwp:F3}/Cp={stats.Cp:F3}, {Ui.FormatEnginePower(SHP * ihpMult)}");

            //float topweight = 0f;
            //foreach (var p in __instance.parts)
            //{
            //    if (!p.isShown || p == __instance.hull)
            //        continue;
            //}


            //stability = arm.weightConning * 5f + (guns.borneWeight + guns.armWeight) * (2f * guns.superFactor - 1f) * 4f + weapons.miscWeightHull * 2f + weapons.miscWeightDeck * 3f + weapons.miscWeightAbove * 4f + arm.weightBeltUpper * 2f + arm.weightBeltMain + arm.weightBeltEnds + arm.weightDeck + (engine.hullWeight + guns.gunsWeight - guns.borneWeight) * 1.5f * free.freeboard / hull.draught;
            //if ((double)roomDeck < 1.0)
            //{
            //    stability = (float)((double)stability + (double)(engine.engineWeight + weapons.miscWeightVital + weapons.voidWeight) * Math.Pow(1f - roomDeck, 2.0));
            //}
            //if (stability > 0f)
            //{
            //    stability = (float)(Math.Sqrt((double)(hull.displacement * (hull.beamBulges / hull.draught) / stability) * 0.5) * Math.Pow(8.76755 / (double)hull.lengthBeam, 0.25));
            //}

            //seaboat = (float)(Math.Sqrt((double)free.freeboardCap / (2.4 * Math.Pow(hull.displacement, 0.2))) * (Math.Pow(stability * 5f * (hull.beamBulges / hull.length), 0.2) * Math.Sqrt(free.freeboardCap / hull.length * 20f) * (double)(hull.displacement / (hull.displacement + arm.weightBeltEnds * 3f + engine.hullWeight / 3f + (guns.borneWeight + guns.armWeight) * superFactorLong))) * 8.0);
            //if ((double)(hull.draught / hull.beamBulges) < 0.3)
            //{
            //    seaboat *= (float)Math.Sqrt((double)(hull.draught / hull.beamBulges) / 0.3);
            //}
            //if ((double)(engine.frictMax / (engine.frictMax + engine.waveMax)) < 0.55 && (double)engine.speedMax != 0.0)
            //{
            //    seaboat *= (float)Math.Pow(engine.frictMax / (engine.frictMax + engine.waveMax), 2.0);
            //}
            //else
            //{
            //    seaboat *= 0.3025f;
            //}
            //seaboat = Math.Min(seaboat, 2f);
            //steadinessAdj = Math.Min((float)steadiness * seaboat, 100f);

            //if (steadinessAdj < 50f)
            //{
            //    seakeeping = seaboat * steadinessAdj / 50f;
            //}
            //else
            //{
            //    seakeeping = seaboat;
            //}

            //recoil = (float)((double)(guns.broadside / hull.displacement * free.freeDistributed * guns.superFactor / hull.beamBulges) * Math.Pow(Math.Pow(hull.displacement, 0.3333333432674408) / (double)hull.beamBulges * 3.0, 2.0) * 7.0);
            //if ((double)stabilityAdj > 0.0)
            //{
            //    recoil /= stabilityAdj * ((50f - steadinessAdj) / 150f + 1f);
            //}

            //metacentre = (float)(Math.Pow(hull.beam, 1.5) * ((double)stabilityAdj - 0.5) / 0.5 / 200.0);
            //rollPeriod = (float)(0.42 * (double)hull.beamBulges / Math.Sqrt(metacentre));
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.ToStore))]
        internal static void Postfix_ToStore(Ship __instance, ref Ship.Store __result)
        {
            __instance.ModData().ToStore(__result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.GetDamagePlanTooltipText))]
        internal static void Prefix_GetDamagePlanTooltipText(Ship __instance)
        {
            Patch_Ui._ShipForEnginePower = __instance;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.EnginePower))]
        internal static bool Prefix_EnginePower(Ship __instance, ref float __result)
        {
            __result = ShipStats.GetHPRequired(__instance);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.GetMinRangeWhitStatEffectInKm))]
        internal static bool Prefix_GetMinRangeWhitStatEffectInKm(Ship __instance, ref int __result)
        {
            __result = ShipStats.GetRange(__instance, Ship.OpRange.VeryLow);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.GetMaxRangeWhitStatEffectInKm))]
        internal static bool Prefix_GetMaxRangeWhitStatEffectInKm(Ship __instance, ref int __result)
        {
            __result = ShipStats.GetRange(__instance, Ship.OpRange.VeryHigh);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.GetEngineMatsWeightCalculateValue))]
        internal static bool Prefix_GetEngineMatsWeightCalculateValue(Ship __instance, ref float __result)
        {
            __result = ShipM.GetEngineMatsWeightCalculateValue(__instance, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.PartMats))]
        internal static bool Prefix_PartMats(Ship __instance, PartData part, ref bool calcCosts, ref Il2CppSystem.Collections.Generic.List<Ship.MatInfo> __result, out bool __state)
        {
            // Always use cache if available
            var cachedList = ShipM.GetCachedPartMats(__instance, part);
            if (cachedList != null)
            {
                __result = cachedList;
                // Parts that are neither hull nor tower
                // are often duplicated. So we'll update all parts
                // sharing this data.
                __state = !part.isHull && !part.isTowerAny;
                return false;
            }

            __state = true;

            __result = ShipM.PartMats(__instance, part);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.PartMats))]
        internal static void Postfix_PartMats(Ship __instance, PartData part, bool calcCosts, ref Il2CppSystem.Collections.Generic.List<Ship.MatInfo> __result, bool __state)
        {
            if (__state)
                ShipM.CachePartMats(__instance, part, __result);
        }

        // This is used too many places to just patch one way.
        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Ship.SizeRatio))]
        //internal static bool Prefix_SizeRatio(Ship __instance, ref float __result)
        //{
        //    if (__instance.cachedSizeRatio == -1f || !GameManager.IsBattle)
        //        __instance.cachedSizeRatio = 0.006f * ShipStats.GetScaledStats(__instance).Lwl - 0.04f;

        //    __result = __instance.cachedSizeRatio;

        //    return false;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.MaxArmorForZone))]
        internal static bool Prefix_MaxArmorForZone(Ship __instance, Ship.A armorA, PartData gunPartData, ref float __result)
        {
            __result = ShipM.MaxArmorForZone(__instance, armorA, gunPartData);
            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Ship.AdjustHullStats))]
        //internal static bool Prefix_AdjustHullStats(Ship __instance, int delta, float targetWeight, Il2CppSystem.Func<bool> stopCondition, bool allowEditSpeed, bool allowEditArmor, bool allowEditCrewQuarters, Il2CppSystem.Random rnd,
        //    Il2CppSystem.Nullable<float> limitArmor, float limitSpeed)
        //{
        //    Melon<UADRealismMod>.Logger.Msg($"Starting AHS. Delta {delta}, Target {targetWeight:F2}, stop null? {(stopCondition == null)}, rnd null? {(rnd == null)}, limitA null? {(limitArmor == null)} hasval? {(limitArmor == null ? false : limitArmor.HasValue)}");

        //    // Stock has a bug where sometimes this is passed as a ratio and sometimes as an actual weight.
        //    // My version of the function takes the ratio.
        //    if (targetWeight > 1f)
        //        targetWeight /= __instance.Tonnage();

        //    ShipM.AdjustHullStats(__instance, delta, targetWeight, null, stopCondition, allowEditSpeed, allowEditArmor, allowEditCrewQuarters, true, null, rnd, limitArmor == null ? -1f : limitArmor.Value, limitSpeed);
        //    return true;
        //}

        // Not bothering to prefix MinArmorForZone because it can stay as default
        // (all it does is return minArmor for BeltMain, and 0 for everything else)

        internal static GenerateShip GenerateShipHandler = null;
        [HarmonyPatch(nameof(Ship.GenerateRandomShip))]
        [HarmonyPrefix]
        internal static void Prefix_GenerateRandomShip(Ship __instance, ref bool needWait)
        {
            // Just in case, set this true.
            // It's always true in the codebase RIGHT NOW but you never know.
            // And it needs to be true for the patching of the coroutine to work right.
            needWait = true;
            GenerateShipHandler = new GenerateShip(__instance);
        }

        [HarmonyPatch(nameof(Ship.GenerateRandomShip))]
        [HarmonyPostfix]
        internal static void Postfix_GenerateRandomShip(Ship __instance)
        {
            GenerateShipHandler = null;
        }
    }

    [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__566))]
    internal class Patch_ShipGenRandom
    {
        [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__566.MoveNext))]
        [HarmonyPrefix]
        internal static bool Prefix_MoveNext(Ship._GenerateRandomShip_d__566 __instance, out int __state, ref bool __result)
        {
            __state = __instance.__1__state;
            var gen = Patch_Ship.GenerateShipHandler;
            switch (__state)
            {
                // 0: Setup. We have to postfix this since
                    // we want to use some stuff it sets/
                    // we don't want it to clobber our values
                // 1: Remove parts. This is fine to run.
                case 2: // Beam/Draught
                case 3: // Tonnage
                case 4: // Clamp tonnage.
                    // We'll skip all these steps and select components instead.
                    __state = 4;
                    gen.SelectComponents(GenerateShip.ComponentSelectionPass.Initial);
                    // We then skip setting base hull stats (we do this in initial design)
                    // and AdjustHullStats.
                    __instance.__1__state = 7;
                    break;

                // 7: UpdateHullStats

                case 8: // Parts
                    // Before doing parts, set out how much tonnage is free?
                    gen.SelectParts();

                    __instance.__1__state = 9; // this waits a frame and advances to 10
                    break;

                case 9: // wait a frame
                    // We'll also use this to do work.

                    // Replace step 10.
                    __instance.__4__this.UpdateHullStats();
                    foreach (var part in __instance.__4__this.parts)
                        part.UpdateCollidersSize(__instance.__4__this);
                    foreach (var part in __instance.__4__this.parts)
                        Part.GunBarrelLength(part.data, __instance.__4__this, true);
                    // let 9 execute, don't change incoming state. This will set
                    // next state to 10.
                    break;

                case 10: // AHS and update parts. Done above.
                    // Skip this and just run 12.
                    __instance.__1__state = 11;
                    break;

                // 11: Verify maincal guns and barrels

                case 12: // Select components and then reduce weight.
                    // We'll only do components. Then skip past all the add/reduce steps to
                    // the end.
                    gen.SelectComponents(GenerateShip.ComponentSelectionPass.PostParts);
                    // FIXME run NeedComponentActive??
                    gen.AddArmorToLimit(__instance.__4__this.Tonnage());
                    __instance.__1__state = 17;
                    break;
            }
            return true;
        }

        [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__566.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(Ship._GenerateRandomShip_d__566 __instance, int __state)
        {
            var gen = Patch_Ship.GenerateShipHandler;
            switch (__instance.__1__state)
            {
                case 0: // Initial run. Set minspeed etc.
                    if (__instance.__1__state == 1)
                        gen.DesignShipInitial(__instance);
                    break;
            }
        }
    }

    //[HarmonyPatch(typeof(Ship._AddRandomPartsNew_d__583))]
    //internal class Patch_ShipAddParts
    //{
    //    [HarmonyPatch(nameof(Ship._AddRandomPartsNew_d__583.MoveNext))]
    //    [HarmonyPrefix]
    //    internal static bool Prefix_MoveNext(Ship._AddRandomPartsNew_d__583 __instance, out int __state)
    //    {
    //        __state = __instance.__1__state;
    //        switch (__state)
    //        {
    //            // 1: Find randpart
    //            // 2: Add part(s)
    //        }
    //        return true;
    //    }

    //    [HarmonyPatch(nameof(Ship._AddRandomPartsNew_d__583.MoveNext))]
    //    [HarmonyPostfix]
    //    internal static bool Postfix_MoveNext(Ship._AddRandomPartsNew_d__583 __instance, int __state)
    //    {
    //        switch (__instance.__1__state)
    //        {
                
    //        }
    //        return true;
    //    }
    //}
}