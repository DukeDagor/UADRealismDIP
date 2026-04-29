using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace TweaksAndFixes
{
    internal static class GGShipgenSpeed
    {
        private static int _generateRandomShipState = -1;
        private static Il2CppSystem.Random? _rnd;

        private static bool IsVanillaBaselineShipgen()
        {
            return Patch_Ship.UseVanillaShipgenBaseline() && _generateRandomShipState >= 0;
        }

        private static float GetMinSpeed(Ship ship)
        {
            float minSpeedKnots = Mathf.Clamp(GetMinSpeedMult(ship) * ship.hull.data.speedLimiter, ship.shipType.speedMin, ship.shipType.speedMax);
            minSpeedKnots = Mathf.Max(GetAbsoluteMinSpeedKnots(ship), minSpeedKnots - GetMinSpeedRelaxationKnots(ship));
            return minSpeedKnots * ShipM.KnotsToMS;
        }

        private static float GetAbsoluteMinSpeedKnots(Ship ship)
        {
            return Mathf.Max(1f, ship.shipType.speedMin - GetMinSpeedRelaxationKnots(ship));
        }

        private static float GetMinSpeedRelaxationKnots(Ship ship)
        {
            string typeName = ship.shipType.name;
            return typeName == "tb" || typeName == "dd" || typeName == "cl" ? 2f : 1f;
        }

        private static float GetMinSpeedMult(Ship ship)
        {
            float year = ship.GetYear(ship);
            float mult;
            float randFactor;
            if (ship.shipType.name == "dd" || ship.shipType.name == "tb")
            {
                mult = 0.925f;
                randFactor = -0.027f;
            }
            else
            {
                switch (ship.hull.data.Generation)
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

            if (_rnd != null)
                mult *= Util.Range(1f + randFactor, 1f, _rnd);

            return mult;
        }

        private static float GetMaxSpeed(Ship ship)
        {
            return Mathf.Min(ship.shipType.speedMax, ship.hull.data.speedLimiter * GetMaxSpeedMult(ship)) * ShipM.KnotsToMS;
        }

        private static float GetMaxSpeedMult(Ship ship)
        {
            float year = ship.GetYear(ship);
            float mult = ship.shipType.name == "cl" || ship.shipType.name == "tb" || ship.shipType.name == "dd" ? 1f : 1.05f;
            mult *= Util.Remap(year, 1890f, 1940f, 1.05f, 1.0f, true);
            if (_rnd != null)
                mult *= Util.Range(0.95f, 1.05f, _rnd);

            return mult;
        }

        private static void ClampAndNormalizeSpeed(Ship ship, ref float speedMax)
        {
            if (ship == null || ship.hull?.data == null || ship.shipType == null || speedMax <= 0f)
                return;

            float minSpeed = GetMinSpeed(ship);
            float maxSpeed = Mathf.Max(minSpeed, GetMaxSpeed(ship));
            speedMax = Mathf.Clamp(speedMax, minSpeed, maxSpeed);

            float speedKnots = speedMax / ShipM.KnotsToMS;
            float absoluteMinSpeedKnots = GetAbsoluteMinSpeedKnots(ship);
            if (speedKnots < Mathf.Max(1f, absoluteMinSpeedKnots - 0.25f))
                return;

            float wholeKnots = Mathf.Floor(speedKnots);
            wholeKnots = Mathf.Clamp(wholeKnots, absoluteMinSpeedKnots, ship.shipType.speedMax);
            speedMax = wholeKnots * ShipM.KnotsToMS;
        }

        [HarmonyPatch(typeof(Ship))]
        internal class Patch_SetSpeedMax
        {
            [HarmonyPatch(nameof(Ship.SetSpeedMax))]
            [HarmonyPrefix]
            internal static void Prefix(Ship __instance, ref float speedMax)
            {
                // Patch intent: keep vanilla's speed selection/reduction flow, but constrain any
                // generated ship speed to TAF's hull-aware min/max range, relax the enforced
                // minimum by ship class, and normalize to whole knots. This copies the useful
                // speed policy without porting TAF AdjustHullStats.
                if (!IsVanillaBaselineShipgen())
                    return;

                if (Patch_BattleManager_d115._ShipGenInfo.customSpeed > 0f)
                    return;

                ClampAndNormalizeSpeed(__instance, ref speedMax);
            }
        }

        [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__573))]
        internal class Patch_GenerateRandomShipSpeedLifecycle
        {
            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPrefix]
            internal static void Prefix(Ship._GenerateRandomShip_d__573 __instance)
            {
                if (!Patch_Ship.UseVanillaShipgenBaseline())
                    return;

                // Patch intent: expose the active vanilla shipgen state and RNG to the speed
                // clamp while MoveNext is running. The copied TAF speed multipliers use the same
                // coroutine RNG for their small variation when it is available.
                _generateRandomShipState = __instance.__1__state;
                _rnd = __instance.__8__1?.rnd;
            }

            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPostfix]
            internal static void Postfix()
            {
                _generateRandomShipState = -1;
                _rnd = null;
            }
        }
    }
}
