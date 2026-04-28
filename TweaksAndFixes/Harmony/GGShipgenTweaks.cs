using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace TweaksAndFixes
{
    internal static class GGShipgenTweaks
    {
        private static bool IsSmallTorpedoCraft(Ship ship)
        {
            string? shipType = ship?.shipType?.name;
            return shipType == "dd" || shipType == "tb";
        }

        private static bool IsHeavyShip(Ship ship)
        {
            string? shipType = ship?.shipType?.name;
            return shipType == "ca" || shipType == "bc" || shipType == "bb";
        }

        private static bool IsTorpedoRandPart(RandPart randPart)
        {
            return randPart?.type == "torpedo";
        }

        private static bool ShouldUseSmallCraftVanillaBaselineTweaks(Ship ship)
        {
            return Patch_Ship.UseVanillaShipgenBaseline()
                && IsSmallTorpedoCraft(ship)
                && ship?.hull?.data != null;
        }

        private static bool ShouldBanHeavyShipTorpedoRandPart(Ship ship, RandPart randPart)
        {
            return Patch_Ship.UseVanillaShipgenBaseline()
                && IsHeavyShip(ship)
                && IsTorpedoRandPart(randPart);
        }

        private static float MaxLegalShipgenTonnage(Ship ship)
        {
            float minTonnage = ship.TonnageMin();
            float upper = ship.TonnageMax();
            if (upper <= 0f)
                return 0f;

            if (ship.player != null && ship.shipType != null)
            {
                float techLimit = ship.player.TonnageLimit(ship.shipType);
                if (techLimit > 0f)
                    upper = Mathf.Min(upper, techLimit);

                if (GameManager.Instance != null && GameManager.Instance.isCampaign && ship.player.shipyard > 0f)
                    upper = Mathf.Min(upper, ship.player.shipyard);
            }

            if (upper < minTonnage)
                return 0f;

            float rounded = Ship.RoundTonnageToStep(upper);
            return Mathf.Clamp(rounded, minTonnage, upper);
        }

        private static void SetShipgenDisplayTonnage(Ship ship, float displayTonnage)
        {
            float beamDraughtBonus = ship.BeamDraughtBonus();
            if (beamDraughtBonus <= 0f)
                beamDraughtBonus = 1f;

            float rawTonnage = displayTonnage / beamDraughtBonus;
            ship.SetTonnage(rawTonnage);

            if (ship.Tonnage() + 0.5f >= displayTonnage)
                return;

            ship.tonnage = rawTonnage;
            ship.UpdateHullStats();
        }

        [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__573))]
        internal class Patch_GenerateRandomShipMinSmallCraftBeamDraught
        {
            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPrefix]
            internal static bool Prefix(Ship._GenerateRandomShip_d__573 __instance, ref bool __result)
            {
                // Patch intent: for the vanilla-baseline generator, DD/TB hulls should always
                // start from their most compact legal shape. This replaces only coroutine state
                // 2, where vanilla normally randomizes beam and draught.
                if (__instance.__1__state != 2)
                    return true;

                Ship ship = __instance.__4__this;
                if (!ShouldUseSmallCraftVanillaBaselineTweaks(ship))
                    return true;

                ship.SetBeam(ship.hull.data.beamMin, true);
                ship.SetDraught(ship.hull.data.draughtMin, true);
                ship.RefreshHull(true);

                __instance.__1__state = 3;
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__573))]
        internal class Patch_GenerateRandomShipMaxSmallCraftTonnage
        {
            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPrefix]
            internal static bool Prefix(Ship._GenerateRandomShip_d__573 __instance, ref bool __result)
            {
                // Patch intent: for the same DD/TB vanilla-baseline path, use the maximum legal
                // displacement instead of vanilla's random tonnage roll. The cap respects hull max,
                // player tech tonnage, and campaign shipyard capacity when those are available.
                if (__instance.__1__state != 3)
                    return true;

                Ship ship = __instance.__4__this;
                if (!ShouldUseSmallCraftVanillaBaselineTweaks(ship))
                    return true;

                float maxTonnage = MaxLegalShipgenTonnage(ship);
                if (maxTonnage <= 0f)
                    return true;

                SetShipgenDisplayTonnage(ship, maxTonnage);

                __instance.__1__state = 5;
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ship.__c__DisplayClass590_0))]
        internal class Patch_GetPartsBanHeavyShipTorpedoes
        {
            [HarmonyPatch(nameof(Ship.__c__DisplayClass590_0._GetParts_b__0))]
            [HarmonyPrefix]
            internal static bool Prefix(Ship.__c__DisplayClass590_0 __instance, ref bool __result)
            {
                // Patch intent: under the vanilla-baseline generator, CA/BC/BB hulls should not
                // receive torpedo launcher randparts. This blocks torpedo candidates at the
                // vanilla Ship.GetParts(randPart) filter boundary before any part is created.
                if (!ShouldBanHeavyShipTorpedoRandPart(__instance.__4__this, __instance.randPart))
                    return true;

                __result = false;
                return false;
            }
        }
    }
}
