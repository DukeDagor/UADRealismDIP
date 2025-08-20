using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader.CoreClrUtils;
using TweaksAndFixes.Data;
using Il2CppSystem.Linq;
using static Il2Cpp.Ship;
using System.Drawing;
using MelonLoader.TinyJSON;

#pragma warning disable CS8625

namespace TweaksAndFixes
{

    // ########## HIT CHANCE OVERRIDES ########## //

    [HarmonyPatch(typeof(Ship.HitChanceCalc))]
    internal class Patch_Ship_HitChanceCalc
    {
        internal static MethodBase TargetMethod()
        {
            //return AccessTools.Method(typeof(Part), nameof(Part.CanPlace), new Type[] { typeof(string).MakeByRefType(), typeof(List<Part>).MakeByRefType(), typeof(List<Collider>).MakeByRefType() });

            // Do this manually
            var methods = AccessTools.GetDeclaredMethods(typeof(Ship.HitChanceCalc));
            foreach (var m in methods)
            {
                if (m.Name != nameof(Ship.HitChanceCalc.Add))
                    continue;

                if (m.GetParameters().Length == 3)
                    return m;
            }

            return null;
        }

        public static Dictionary<string, HashSet<string>> IgnoreList = new Dictionary<string, HashSet<string>>();

        static Patch_Ship_HitChanceCalc()
        {
            IgnoreList["Gun"] = new HashSet<string> { "All" };
            IgnoreList["Crew Training"] = new HashSet<string> { "All" };
            IgnoreList["Time"] = new HashSet<string> { "All" };
            IgnoreList["Weather"] = new HashSet<string> { "All" };
            IgnoreList["Wind"] = new HashSet<string> { "All" };
            IgnoreList["Sea Waves"] = new HashSet<string> { "All" };
            IgnoreList["Sun Glare"] = new HashSet<string> { "All" };
        }

        internal static void Prefix(Ship.HitChanceCalc __instance, ref float mult, ref string reason, ref string value)
        {
            if (!AccuraciesExInfo.HasEntries())
            {
                return;
            }

            // Some multipliers are unamed, these have no real corrolation, so they are ignored.
            if (reason.Length == 0)
            {
                return;
            }

            if (IgnoreList.ContainsKey(reason) && (IgnoreList[reason].Contains(value) || IgnoreList[reason].Contains("All")))
            {
                return;
            }

            float modifiedMultiplier = mult;
            string modifiedName = reason;
            string modifiedSubname = value;

            bool changed = AccuraciesExInfo.UpdateAccuracyInfo(ref modifiedName, ref modifiedSubname, ref modifiedMultiplier);

            mult = modifiedMultiplier;
            reason = modifiedName;
            value = modifiedSubname;

            if (!changed)
            {
                if (!IgnoreList.ContainsKey(reason))
                {
                    IgnoreList[reason] = new HashSet<string>();
                }

                if (!IgnoreList[reason].Contains(value))
                {
                    IgnoreList[reason].Add(value);
                    Melon<TweaksAndFixes>.Logger.Error("Unknown accuracy modifier: " + reason + " : " + value + " : " + mult);
                }
            }
            else
            {
                // Melon<TweaksAndFixes>.Logger.Msg(reason + " : " + value + " : " + mult + " = " + __instance.multsCombined);
            }
        }
    }


    [HarmonyPatch(typeof(Ship))]
    internal class Patch_Ship
    {
        // ########## NEW CONSTRUCTOR LOGIC ########## //

        public static Ship LastCreatedShip;
        public static float LastClonedShipWeight = 0;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.Create))]
        internal static void Postfix_Create(Ship __result, Ship design, Player player, bool isTempForBattle = false, bool isPrewarming = false, bool isSharedDesign = false)
        {
            // LastCreatedShip = __result;
            // 
            // if (LastCreatedShip == null) return;
        
            // Melon<TweaksAndFixes>.Logger.Msg($"{__result.id} : {__result.tonnage} + {(design != null ? (design.id + " : " + design.tonnage) : "NO DESIGN")}");

            LastClonedShipWeight = 0;
            if (design != null)
            {
                LastClonedShipWeight = design.tonnage;
            }

            // foreach (Mount mount in LastCreatedShip.mounts)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg(LastCreatedShip.Name(false, false) + ": " + mount.caliberMin + " - " + mount.caliberMax);
            // }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.GetRefitYearNameEnd))]
        internal static void Prefix_GetRefitYearNameEnd(Ship __instance, ref string __result)
        {
            __result = $" ({ ModUtils.NumToMonth(__instance.dateCreatedRefit.AsDate().Month)}. { __instance.dateCreatedRefit.AsDate().Year})";
            // Melon<TweaksAndFixes>.Logger.Msg($"GetRefitYearNameEnd: {__instance.Name(false, false, false, false, true)}{__result}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.RemovePart))]
        internal static bool Prefix_RemovePart(Ship __instance, Part part)
        {
            if (__instance == null) return false;

            if (part != Patch_Ui.SelectedPart && part.mount != null && part == Patch_Part.TrySkipDestroy || !__instance.parts.Contains(part))
            {
                Patch_Part.TrySkipDestroy = null;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.RemovePart))]
        internal static void Postfix_RemovePart(Ship __instance, Part part)
        {
            // Melon<TweaksAndFixes>.Logger.Msg(part.Name() + ": Removed");

            if (!Patch_Ui.UseNewConstructionLogic()) return;

            if (part == Patch_Ui.PickupPart && Input.GetMouseButtonUp(0))
            {
                Patch_Ui.PickedUpPart = true;
                // Melon<TweaksAndFixes>.Logger.Msg(part.Name() + ": Might be a pickup");
            }

            if (!_IsInChangeHullWithHuman && Patch_Part.unmatchedParts.Contains(part)) Patch_Part.unmatchedParts.Remove(part);

            if (!_IsInChangeHullWithHuman && G.settings.autoMirror && Patch_Part.mirroredParts.ContainsKey(part))
            {
                Part A = part;
                Part B = Patch_Part.mirroredParts[part];
                Patch_Part.mirroredParts.Remove(A);
                Patch_Part.mirroredParts.Remove(B);

                if (Patch_Part.applyMirrorFromTo.ContainsKey(A))
                {
                    Patch_Part.applyMirrorFromTo.Remove(A);
                }
                else
                {
                    Patch_Part.applyMirrorFromTo.Remove(B);
                }

                if (part == A)
                {
                    __instance.RemovePart(B);
                }
                else
                {
                    __instance.RemovePart(A);
                }
            }

            foreach (var mount in part.mountsInside)
            {
                if (mount.employedPart != null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  {mount.employedPart.Name()}");
                    __instance.RemovePart(mount.employedPart);
                }
            }
        }

        // RemoveChildForPart

        // [HarmonyPrefix]
        // [HarmonyPatch(nameof(Ship.RemoveChildForPart))]
        // internal static bool Prefix_RemoveChildForPart(Ship __instance, Part part)
        // {
        //     Melon<TweaksAndFixes>.Logger.Msg($"Removing children for part {part.Name()}");
        // 
        //     Stack<Part> stack = new Stack<Part>();
        // 
        //     foreach (var mount in part.mountsInside)
        //     {
        //         if (mount.employedPart != null)
        //         {
        //             Melon<TweaksAndFixes>.Logger.Msg($"  {mount.employedPart.Name()}");
        //             stack.Push(mount.employedPart);
        //         }
        //     }
        // 
        //     return true;
        // }





        // ########## MODIFIED SHIP GENERATION ########## //

        internal static int _GenerateShipState = -1;
        internal static bool _IsLoading = false;
        internal static Ship _ShipForLoading = null;
        internal static Ship.Store _StoreForLoading = null;
        internal static Ship._GenerateRandomShip_d__573 _GenerateRandomShipRoutine = null;
        internal static Ship._AddRandomPartsNew_d__591 _AddRandomPartsRoutine = null;
        internal static RandPart _LastRandPart = null;
        internal static bool _LastRPIsGun = false;
        internal static ShipM.BatteryType _LastBattery = ShipM.BatteryType.main;
        internal static ShipM.GenGunInfo _GenGunInfo = new ShipM.GenGunInfo();

        internal static bool UpdateRPGunCacheOrSkip(RandPart rp)
        {
            if (rp != _LastRandPart)
            {
                _LastRandPart = rp;
                _LastRPIsGun = rp.type == "gun";
                if (_LastRPIsGun)
                    _LastBattery = rp.condition.Contains("main_cal") ? ShipM.BatteryType.main : (rp.condition.Contains("sec_cal") ? ShipM.BatteryType.sec : ShipM.BatteryType.ter);
            }
            return !_LastRPIsGun;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.ToStore))]
        internal static void Postfix_ToStore(Ship __instance, ref Ship.Store __result)
        {
            __instance.TAFData().ToStore(__result, false);
        }

        // We can't patch FromStore because it has a nullable argument.
        // It has multiple early-outs. We're skipping:
        // * shipType can't be found in GameData
        // * tech not in GameData.
        // * part hull not in GameData
        // * can't find design
        // But we will patch the regular case
        internal static void Postfix_FromStore(Ship __instance)
        {
            if (__instance != null && _StoreForLoading != null)
                __instance.TAFData().ToStore(_StoreForLoading, true);

            _IsLoading = false;
            _ShipForLoading = null;
            _StoreForLoading = null;
        }

        // Successful FromStore
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.Init))]
        internal static void Postfix_Init(Ship __instance)
        {
            if (_IsLoading)
                Postfix_FromStore(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.TechGunGrade))]
        internal static void Postfix_TechGunGrade(Ship __instance, PartData gun, bool requireValid, ref int __result)
        {
            // Let's hope the gun grade cache is only used in this method!
            // If it's used elsewhere, we won't catch that case. The reason
            // is that we can't patch the cache if we want to use it at all,
            // because we need to preserve the _real_ grade but we also
            // don't want to cache-bust every time.

            __result = __instance.TAFData().GunGrade(gun, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.TechTorpedoGrade))]
        internal static void Postfix_TechTorpedoGrade(Ship __instance, PartData torpedo, bool requireValid, ref int __result)
        {
            // Let's hope the torp grade cache is only used in this method!
            // If it's used elsewhere, we won't catch that case. The reason
            // is that we can't patch the cache if we want to use it at all,
            // because we need to preserve the _real_ grade but we also
            // don't want to cache-bust every time.

            __result = __instance.TAFData().TorpedoGrade(__result);
        }

        [HarmonyPatch(nameof(Ship.AddedAdditionalTonnageUsage))]
        [HarmonyPrefix]
        internal static bool Prefix_AddedAdditionalTonnageUsage(Ship __instance)
        {
            ShipM.AddedAdditionalTonnageUsage(__instance);
            return false;
        }

        [HarmonyPatch(nameof(Ship.ReduceWeightByReducingCharacteristics))]
        [HarmonyPrefix]
        internal static bool Prefix_ReduceWeightByReducingCharacteristics(Ship __instance, Il2CppSystem.Random rnd, float tryN, float triesTotal, float randArmorRatio = 0, float speedLimit = 0)
        {
            ShipM.ReduceWeightByReducingCharacteristics(__instance, rnd, tryN, triesTotal, randArmorRatio, speedLimit);
            return false;
        }

        [HarmonyPatch(nameof(Ship.GenerateArmor))]
        [HarmonyPrefix]
        internal static bool Prefix_GenerateArmor(float armorMaximal, Ship shipHint, ref Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> __result)
        {
            __result = ShipM.GenerateArmorNew(armorMaximal, shipHint);
            return false;
        }

        internal static bool _IsInChangeHullWithHuman = false;
        // Work around difficulty in patching AdjustHullStats
        [HarmonyPatch(nameof(Ship.ChangeHull))]
        [HarmonyPrefix]
        internal static void Prefix_ChangeHull(Ship __instance, ref bool byHuman)
        {
            if (byHuman)
            {
                byHuman = false;
                _IsInChangeHullWithHuman = true;
            }
        }
        [HarmonyPatch(nameof(Ship.ChangeHull))]
        [HarmonyPostfix]
        internal static void Postfix_ChangeHull(Ship __instance)
        {
            // Patch_Ui.NeedsConstructionListsClear = true;
            // Melon<TweaksAndFixes>.Logger.Msg($"Changed: {LastCreatedShip?.Name(false, false)} to {__instance.Name(false, false)}");
            // Melon<TweaksAndFixes>.Logger.Msg($"Changed: {LastCreatedShip?.id} to {__instance.id}");
            // LastCreatedShip = __instance;
            _IsInChangeHullWithHuman = false;

            // LastClonedShipWeight

            Patch_Ui.UpdateActiveShip = true;

            if (Patch_GameManager._IsRefreshSharedDesign)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Change Hull in Refresh Shared Design");

                if (!G.GameData.sharedDesignsPerNation.ContainsKey(__instance.player.data.name))
                {
                    // Melon<TweaksAndFixes>.Logger.Warning($"Failed to find nation {__instance.player.data.name} for Shared Design {__instance.Name(false, false)}.");
                }
                else
                {
                    foreach (var ship in G.GameData.sharedDesignsPerNation[__instance.player.data.name])
                    {
                        if (ship.Item1.id != __instance.id) continue;

                        // Melon<TweaksAndFixes>.Logger.Msg($"  Stored: {ship.Item1.vesselName}: {ship.Item1.tonnage}");
                        __instance.tonnage = ship.Item1.tonnage;
                    }
                }
            }
            else if (LastClonedShipWeight != 0)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Change Hull outside Refresh Shared Design: {LastClonedShipWeight} : {__instance.tonnage}");
                __instance.tonnage = LastClonedShipWeight;
                LastClonedShipWeight = 0;
            }

            // if (G.ui.isConstructorRefitMode)
            // {
            //     Player player = ExtraGameData.MainPlayer();
            // 
            //     if (player == null)
            //     {
            //         Melon<TweaksAndFixes>.Logger.Error("Failed to get main player in Refit Mode. Build mode will be broken.");
            //         return;
            //     }
            // 
            //     if (player.designs.Count() < 2)
            //     {
            //         Melon<TweaksAndFixes>.Logger.Error("Design count less than 2. Failed to find refit ship reference. Build mode will be broken.");
            //         return;
            //     }
            // 
            //     LastCreatedShip = new Il2CppSystem.Collections.Generic.List<Ship>(player.designs)[^2];
            // }
        }
        [HarmonyPatch(nameof(Ship.SetDraught))]
        [HarmonyPostfix]
        internal static void Postfix_SetDraught(Ship __instance)
        {
            // Do what ChangeHull would do in the byHuman block
            if (_IsInChangeHullWithHuman)
            {
                float tonnageLimit = Mathf.Min(__instance.tonnage, __instance.TonnageMax());
                float tonnageToSet = Mathf.Lerp(__instance.TonnageMin(), tonnageLimit, UnityEngine.Random.Range(0f, 1f));
                __instance.SetTonnage(tonnageToSet);
                var designYear = __instance.GetYear(__instance);
                var origTargetWeightRatio = 1f - Util.Remap(designYear, 1890f, 1940f, 0.63f, 0.52f, true);
                var stopFunc = new System.Func<bool>(() =>
                {
                    return (__instance.Weight() / __instance.Tonnage()) <= (1f - Util.Remap(designYear, 1890f, 1940f, 0.63f, 0.52f, true));
                });
                ShipM.AdjustHullStats(__instance, -1, origTargetWeightRatio, stopFunc, true, true, true, true, true, null, -1f, -1f);
            }
        }

        [HarmonyPatch(nameof(Ship.ChangeRefitShipTech))]
        [HarmonyPostfix]
        internal static void Postfix_ChangeRefitShipTech(Ship __instance, Ship newDesign)
        {
            __instance.TAFData().OnRefit(newDesign);
        }

        private static List<PartData> _TempDatas = new List<PartData>();

        // Hook this just so we can run this after a random gun is added. Bleh.
        // We need to do this because, if we place a part of a _new_ caliber,
        // we need to check if we are now at the limit for caliber counts for
        // that battery, and if so remove all other-caliber datas from being
        // chosen.
        [HarmonyPatch(nameof(Ship.AddShipTurretArmor), new Type[] { typeof(Part) })]
        [HarmonyPostfix]
        internal static void Postfix_AddShipTurretArmor(Part part)
        {
            if (_AddRandomPartsRoutine == null || !_GenGunInfo.isLimited || UpdateRPGunCacheOrSkip(_AddRandomPartsRoutine.__8__1.randPart))
                return;

            // Register reports true iff we're at the count limit
            if (_GenGunInfo.RegisterCaliber(_LastBattery, part.data))
            {
                // Ideally we'd do RemoveAll, but we can't use a managed predicate
                // on the native list. We could reimplement RemoveAll, but I don't trust
                // calling RuntimeHelpers across the boundary. This should still be faster
                // than the O(n^2) of doing RemoveAts, because we don't have to copy
                // back to compress the array each time.
                for (int i = _AddRandomPartsRoutine._chooseFromParts_5__11.Count; i-- > 0;)
                    if (_GenGunInfo.CaliberOK(_LastBattery, _AddRandomPartsRoutine._chooseFromParts_5__11[i]))
                        _TempDatas.Add(_AddRandomPartsRoutine._chooseFromParts_5__11[i]);

                _AddRandomPartsRoutine._chooseFromParts_5__11.Clear();
                for (int i = _TempDatas.Count; i-- > 0;)
                    _AddRandomPartsRoutine._chooseFromParts_5__11.Add(_TempDatas[i]);

                _TempDatas.Clear();
            }
        }
    }

    // We can't target ref arguments in an attribute, so
    // we have to make this separate class to patch with a
    // TargetMethod call.
    [HarmonyPatch(typeof(Ship))]
    internal class Patch_Ship_IsComponentAvailable
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Ship), nameof(Ship.IsComponentAvailable), new Type[] { typeof(ComponentData), typeof(string).MakeByRefType() });
        }

        internal static bool Prefix(Ship __instance, ComponentData component, ref string reason, ref bool __result, out float __state)
        {
            __state = component.weight;

            var weight = ComponentDataM.GetWeight(component, __instance.shipType);
            
            //if (weight == component.weight)
            //    return true;
            //Melon<TweaksAndFixes>.Logger.Msg($"For component {component.name} and shipType {__instance.shipType.name}, overriding weight to {weight:F0}");

            if (weight <= 0f)
            {
                __result = false;
                reason = "Ship Type";
                return false;
            }
            component.weight = weight;
            return true;
        }

        internal static void Postfix(ComponentData component, float __state)
        {
            component.weight = __state;
        }
    }

    [HarmonyPatch(typeof(Ship.__c))]
    internal class Patch_Ship_c
    {
        // This method is called by the component selection process to set up
        // the weighted-random dictionary. So we need to patch it too. But
        // it doesn't know the ship in question. So we have to patch the calling
        // method to pass that on.
        // ALSO, it's code that's shared with IsComponentAvailable. But we
        // patch that by changing weight before and after the method. So there's
        // no need to do so here. So we abort if we're not in GenerateRandomShip.
        [HarmonyPatch(nameof(Ship.__c._GetComponentsToInstall_b__574_3))]
        [HarmonyPrefix]
        internal static bool Prefix_GetComponentsToInstall_b__565_3(ComponentData c, ref float __result)
        {
            if (Patch_Ship._GenerateRandomShipRoutine == null)
                return true;

            __result = ComponentDataM.GetWeight(c, Patch_Ship._GenerateRandomShipRoutine.__4__this.shipType);
            //if(__result != c.weight)
            //    Melon<TweaksAndFixes>.Logger.Msg($"Gen: For component {c.name} and shipType {Patch_Ship._GenerateRandomShipRoutine.__4__this.shipType.name}, overriding weight to {__result:F0}");
            return false;
        }
    }

    // This runs when selecting all possible parts for a RP
    // but once an RP is having parts placed, we also need to
    // knock options out whenever a caliber is picked. See
    // AddTurretArmor above.
    [HarmonyPatch(typeof(Ship.__c__DisplayClass590_0))]
    internal class Patch_Ship_c_GetParts
    {
        [HarmonyPatch(nameof(Ship.__c__DisplayClass590_0._GetParts_b__0))]
        [HarmonyPrefix]
        internal static bool Prefix_b0(Ship.__c__DisplayClass590_0 __instance, PartData a, ref bool __result)
        {
            // Super annoying we can't prefix GetParts itself to do the RP caching
            if (!Patch_Ship._GenGunInfo.isLimited || Patch_Ship.UpdateRPGunCacheOrSkip(__instance.randPart))
                return true;

            int partCal = (int)((a.caliber + 1f) * (1f / 25.4f));
            if (!Patch_Ship._GenGunInfo.CaliberOK(Patch_Ship._LastBattery, partCal))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__573))]
    internal class Patch_ShipGenRandom
    {
        //static string lastName = string.Empty;
        //static int shipCount = 0;

        internal struct GRSData
        {
            public int state;
            public float beamMin;
            public float beamMax;
            public float draughtMin;
            public float draughtMax;
        }

        const string bdlParamName = "ai_beamdraughtlimits";

        [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
        [HarmonyPrefix]
        internal static bool Prefix_MoveNext(Ship._GenerateRandomShip_d__573 __instance, out GRSData __state, ref bool __result)
        {
            Patch_Ship._GenerateRandomShipRoutine = __instance;
            //if (lastName != __instance.__4__this.vesselName)
            //{
            //    lastName = __instance.__4__this.vesselName;
            //    Melon<TweaksAndFixes>.Logger.Msg($"ShipGen {__instance.__4__this.vesselName} ({__instance.__4__this.hull.data.name}) for {__instance.__4__this.player.data.name}, #{shipCount++}"); // state {__instance.__1__state}");
            //}
            //Melon<TweaksAndFixes>.Logger.Msg($"{__instance.__4__this.vesselName}: state {__instance.__1__state}");

            // So we know what state we started in.
            __state = new GRSData();
            __state.state = __instance.__1__state;
            Patch_Ship._GenerateShipState = __state.state;
            var ship = __instance.__4__this;
            var hd = ship.hull.data;
            __state.beamMin = hd.beamMin;
            __state.beamMax = hd.beamMax;
            __state.draughtMin = hd.draughtMin;
            __state.draughtMax = hd.draughtMax;

            if (hd.paramx.TryGetValue(bdlParamName, out var bdlParam))
            {
                int iC = bdlParam.Count;
                if (iC == 4)
                {
                    for (int i = 0; i < iC; ++i)
                    {
                        if (!float.TryParse(bdlParam[i], out var val))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"For ship {hd.name}, {bdlParamName} failed to parse {bdlParam[i]}");
                            continue;
                        }
                        if (val != 0)
                        {
                            switch (i)
                            {
                                case 0: hd.beamMin = val; break;
                                case 1: hd.beamMax = val; break;
                                case 2: hd.draughtMin = val; break;
                                default: hd.draughtMax = val; break;
                            }
                        }
                    }
                }
                else
                    Melon<TweaksAndFixes>.Logger.Error($"For ship {hd.name}, {bdlParamName} doesn't have 4 elements. The elements should be beamMin, beamMax, draughtMin, draughtMax. Values of 0 mean keep existing non-AI values.");
            }
            switch (__state.state)
            {
                case 0:
                    __instance.__4__this.TAFData().ResetAllGrades();
                    break;

                case 6:
                    float weightTargetRand = Util.Range(0.875f, 1.075f, __instance.__8__1.rnd);
                    var designYear = ship.GetYear(ship);
                    float yearRemapToFreeTng = Util.Remap(designYear, 1890f, 1940f, 0.6f, 0.4f, true);
                    float weightTargetRatio = 1f - Mathf.Clamp(weightTargetRand * yearRemapToFreeTng, 0.45f, 0.65f);
                    var stopFunc = new System.Func<bool>(() =>
                    {
                        float targetRand = Util.Range(0.875f, 1.075f, __instance.__8__1.rnd);
                        return (ship.Weight() / ship.Tonnage()) <= (1.0f - Mathf.Clamp(targetRand * yearRemapToFreeTng, 0.45f, 0.65f));
                    });

                    // We can't access the nullable floats on this object
                    // so we cache off their values at the callsite (the
                    // only one that sets them).

                    ShipM.AdjustHullStats(
                      ship,
                      -1,
                      weightTargetRatio,
                      stopFunc,
                      Patch_BattleManager_d115._ShipGenInfo.customSpeed <= 0f,
                      Patch_BattleManager_d115._ShipGenInfo.customArmor <= 0f,
                      true,
                      true,
                      true,
                      __instance.__8__1.rnd,
                      Patch_BattleManager_d115._ShipGenInfo.limitArmor,
                      __instance._savedSpeedMinValue_5__3);

                    // We can't do the frame-wait thing easily, let's just advance straight-away
                    __instance.__1__state = 7;
                    break;

                case 10:
                    // We can't access the nullable floats on this object
                    // so we cache off their values at the callsite (the
                    // only one that sets them).

                    ShipM.AdjustHullStats(
                      ship,
                      1,
                      1f,
                      null,
                      Patch_BattleManager_d115._ShipGenInfo.customSpeed <= 0f,
                      Patch_BattleManager_d115._ShipGenInfo.customArmor <= 0f,
                      true,
                      true,
                      true,
                      __instance.__8__1.rnd,
                      Patch_BattleManager_d115._ShipGenInfo.limitArmor,
                      __instance._savedSpeedMinValue_5__3);

                    ship.UpdateHullStats();

                    foreach (var p in ship.parts)
                        p.UpdateCollidersSize(ship);

                    foreach (var p in ship.parts)
                        Part.GunBarrelLength(p.data, ship, true);

                    // We can't do the frame-wait thing easily, let's just advance straight-away
                    __instance.__1__state = 11;
                    break;
            }
            return true;
        }

        [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(Ship._GenerateRandomShip_d__573 __instance, GRSData __state)
        {
            var ship = __instance.__4__this;
            var hd = ship.hull.data;
            hd.beamMin = __state.beamMin;
            hd.beamMax = __state.beamMax;
            hd.draughtMin = __state.draughtMin;
            hd.draughtMax = __state.draughtMax;
            // For now, we're going to reset all grades regardless.
            //if (__state == 1 && (!__instance._isRefitMode_5__2 || !__instance.isSimpleRefit))
            //    __instance.__4__this.TAFData().ResetAllGrades();
            switch (__state.state)
            {
                case 0:
                    if (Config.ShipGenTweaks)
                    {
                        Patch_Ship._GenGunInfo.FillFor(__instance.__4__this);

                        if (!G.ui.isConstructorRefitMode)
                        {
                            //__instance._savedSpeedMinValue_5__3 = Mathf.Max(__instance.__4__this.shipType.speedMin,
                            //    Mathf.Min(__instance.__4__this.hull.data.speedLimiter - 2f, __instance.__4__this.hull.data.speedLimiter * G.GameData.parms.GetValueOrDefault("taf_genship_minspeed_mult")))
                            //    * ShipM.KnotsToMS;

                            // For now, let each method handle it.
                            __instance._savedSpeedMinValue_5__3 = -1f;
                        }
                    }
                    break;

                case 8: // Add parts
                    break;
            }

            Patch_Ship._GenerateRandomShipRoutine = null;
            Patch_Ship._GenerateShipState = -1;
            //Melon<TweaksAndFixes>.Logger.Msg($"GenerateRandomShip Iteration for state {__state} ended, new state {__instance.__1__state}");
        }
    }


    [HarmonyPatch(typeof(Ship._AddRandomPartsNew_d__591))]
    internal class Patch_Ship_AddRandParts
    {
        [HarmonyPatch(nameof(Ship._AddRandomPartsNew_d__591.MoveNext))]
        [HarmonyPrefix]
        internal static void Prefix_MoveNext(Ship._AddRandomPartsNew_d__591 __instance, out int __state)
        {
            Patch_Ship._AddRandomPartsRoutine = __instance;
            __state = __instance.__1__state;
            //Melon<TweaksAndFixes>.Logger.Msg($"Iteraing AddRandomPartsNew, state {__state}");
            //switch (__state)
            //{
            //    case 2: // pick a part and place it
            //            // The below is a colossal hack to get the game
            //            // to stop adding funnels past a certain point.
            //            // This patch doesn't really work, because components are selected
            //            // AFTER parts. Durr.
            //            if (!Config.ShipGenTweaks)
            //        return;

            //    var _this = __instance.__4__this;
            //    if (!_this.statsValid)
            //        _this.CStats();
            //    var eff = _this.stats.GetValueOrDefault(G.GameData.stats["smoke_exhaust"]);
            //    if (eff == null)
            //        return;
            //    if (eff.total < Config.Param("taf_generate_funnel_maxefficiency", 150f))
            //        return;

            //    foreach (var p in G.GameData.parts.Values)
            //    {
            //        if (p.type == "funnel")
            //            _this.badData.Add(p);
            //    }
            //    break;
            //}
        }

        [HarmonyPatch(nameof(Ship._AddRandomPartsNew_d__591.MoveNext))]
        [HarmonyPostfix]
        internal static void Postfix_MoveNext(Ship._AddRandomPartsNew_d__591 __instance, int __state)
        {
            Patch_Ship._AddRandomPartsRoutine = null;
            //Melon<TweaksAndFixes>.Logger.Msg($"AddRandomPartsNew Iteration for state {__state} ended, new state {__instance.__1__state}");
        }
    }

    [HarmonyPatch(typeof(VesselEntity))]
    internal class Patch_VesselEntityFromStore
    {
        // Harmony can't patch methods that take nullable arguments.
        // So instead of patching Ship.FromStore() we have to patch
        // this, which it calls near the start.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(VesselEntity.FromBaseStore))]
        internal static void Prefix_FromBaseStore(VesselEntity __instance, VesselEntity.VesselEntityStore store, bool isSharedDesign)
        {
            Ship ship = __instance.GetComponent<Ship>();
            if (ship == null)
                return;

            var sStore = store.TryCast<Ship.Store>();
            if (sStore == null)
                return;

            if (sStore.mission != null && LoadSave.Get(sStore.mission, G.GameData.missions) == null)
                return;

            Patch_Ship._IsLoading = true;
            Patch_Ship._ShipForLoading = ship;
            Patch_Ship._StoreForLoading = sStore;
            ship.TAFData().FromStore(sStore);
        }
    }
}
