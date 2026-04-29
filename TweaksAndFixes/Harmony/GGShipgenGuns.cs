using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweaksAndFixes
{
    internal static class GGShipgenGuns
    {
        private static int _generateRandomShipState = -1;
        private static Ship? _summaryShip;
        private static float _summaryStartedAt;
        private static bool _summaryActive;

        private static bool IsVanillaBaselineShipgen()
        {
            return Patch_Ship.UseVanillaShipgenBaseline()
                && (_generateRandomShipState >= 0 || Patch_Ship.IsVanillaShipgenBaselineActive());
        }

        private static float FloorGunDiameterModifier(PartData data, float diameterModifier)
        {
            if (data == null || !data.isGun)
                return diameterModifier;

            float caliberMm = data.caliber + diameterModifier;
            float caliberInches = caliberMm * (1f / 25.4f);
            float flooredInches = Mathf.Max(1f, Mathf.Floor(caliberInches + 0.001f));
            return flooredInches * 25.4f - data.caliber;
        }

        private static void FloorGeneratedGunCalibers(Ship ship)
        {
            if (ship == null || ship.shipGunCaliber == null)
                return;

            foreach (Ship.TurretCaliber caliber in ship.shipGunCaliber)
            {
                if (caliber == null || caliber.turretPartData == null)
                    continue;

                float flooredDiameter = FloorGunDiameterModifier(caliber.turretPartData, caliber.diameter);
                if (Mathf.Abs(flooredDiameter - caliber.diameter) > 0.001f)
                    ship.SetCaliberDiameter(caliber, flooredDiameter);
            }
        }

        private static bool InstallFirstAvailableComponent(Ship ship, params string[] keys)
        {
            if (ship?.components == null)
                return false;

            foreach (string key in keys)
            {
                if (!G.GameData.components.TryGetValue(key, out ComponentData component))
                    continue;

                if (!ship.components.ContainsKey(component.typex))
                    continue;

                if (!ship.IsComponentAvailable(component))
                    continue;

                if (ship.components[component.typex] != component)
                    ship.InstallComponent(component);

                return true;
            }

            return false;
        }

        private static void OptimizeGeneratedGunComponents(Ship ship)
        {
            // Patch intent: generated vanilla-baseline designs should prefer the
            // penetration-heavy shell setup we would choose manually: maximum AP
            // ratios for main and secondary batteries, super-heavy shell weight
            // when available, and the best available AP/HE cap by penetration.
            InstallFirstAvailableComponent(ship, "shell_ratio_main_2");
            InstallFirstAvailableComponent(ship, "shell_ratio_sec_2");
            InstallFirstAvailableComponent(ship, "shell_S.heavy", "shell_heavy", "shell_normal");
            InstallFirstAvailableComponent(ship, "ap_5", "ap_2", "ap_1", "ap_0", "ap_4", "ap_3");
            InstallFirstAvailableComponent(ship, "he_3", "he_2", "he_0", "he_1", "he_4", "he_5");
        }

        private static string ShipLabel(Ship ship)
        {
            if (ship?.hull?.data == null)
                return "hull=?";

            string type = ship.shipType?.name?.ToUpperInvariant() ?? "?";
            string hullName = ship.hull.data.name ?? "?";
            string model = ship.hull.data.model ?? "?";
            return $"{type} {hullName}/{model}";
        }

        private static string PlayerLabel(Ship ship)
        {
            if (ship?.player?.data == null)
                return "?";

            return ship.player.data.name ?? "?";
        }

        private static int ShipYear(Ship ship)
        {
            try
            {
                return Mathf.RoundToInt(ship.GetYear(ship));
            }
            catch
            {
                try
                {
                    return ship.dateCreated.AsDate().Year;
                }
                catch
                {
                    return 0;
                }
            }
        }

        private static string GunSummary(Ship ship)
        {
            if (ship?.parts == null)
                return "guns=none";

            Dictionary<string, int> main = new();
            Dictionary<string, int> secondary = new();
            int totalGuns = 0;

            foreach (Part part in ship.parts)
            {
                if (part?.data == null || !part.data.isGun)
                    continue;

                totalGuns++;
                float caliber = part.data.GetCaliberInch(ship);
                string label = $"{part.data.barrels}x{caliber:0.#}in";
                Dictionary<string, int> target = ship.IsMainCal(part) ? main : secondary;
                target[label] = target.TryGetValue(label, out int count) ? count + 1 : 1;
            }

            if (totalGuns == 0)
                return "guns=none";

            string FormatBattery(Dictionary<string, int> battery)
            {
                if (battery.Count == 0)
                    return "none";

                return string.Join(", ", battery
                    .OrderByDescending(kvp => kvp.Key)
                    .Select(kvp => $"{kvp.Value} turrets {kvp.Key}"));
            }

            Patch_Ship.CountMainGuns(ship, out int mainTurrets, out int mainBarrels);
            return $"guns={totalGuns}, main={mainTurrets}/{mainBarrels} ({FormatBattery(main)}), secondary={FormatBattery(secondary)}";
        }

        private static string TorpedoSummary(Ship ship)
        {
            if (ship?.parts == null)
                return "torps=none";

            Dictionary<string, int> launchers = new();
            int totalLaunchers = 0;
            int totalTubes = 0;

            foreach (Part part in ship.parts)
            {
                if (part?.data == null || !part.data.isTorpedo)
                    continue;

                totalLaunchers++;
                int tubes = part.data.name.Contains("x0") ? 0 : part.data.barrels;
                totalTubes += tubes;

                int diameter = 0;
                try
                {
                    diameter = 14 + ship.TechTorpedoGrade(part.data);
                }
                catch
                {
                }

                string label = diameter > 0 ? $"{tubes}x{diameter}in" : $"{tubes}x?";
                launchers[label] = launchers.TryGetValue(label, out int count) ? count + 1 : 1;
            }

            if (totalLaunchers == 0)
                return "torps=none";

            string layout = string.Join(", ", launchers
                .OrderByDescending(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Value} launchers {kvp.Key}"));

            return $"torps={totalLaunchers}/{totalTubes} tubes ({layout})";
        }

        private static string SpeedSummary(Ship ship)
        {
            if (ship == null)
                return "speed=?";

            // Patch intent: log the generated design speed, not the effective
            // battle speed. Disassembly shows SetSpeedMax writes VesselEntity.speedMax
            // directly, while Ship.SpeedMax() multiplies that stored value by runtime
            // ship modifiers and can report zero during generation.
            float speed = ship.speedMax;

            if (speed <= 0f)
            {
                try
                {
                    speed = ship.SpeedMax(true, false, true);
                }
                catch
                {
                }
            }

            return speed > 0f ? $"speed={speed / ShipM.KnotsToMS:0.#}kn" : "speed=?";
        }

        private static string ArmorSummary(Ship ship)
        {
            if (ship?.armor == null || ship.armor.Count == 0)
                return "armor=none";

            static string Inch(Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> armor, Ship.A area)
                => $"{armor.ArmorValue(area) / 25.4f:0.#}";

            string belt = $"{Inch(ship.armor, Ship.A.Belt)}/{Inch(ship.armor, Ship.A.BeltBow)}/{Inch(ship.armor, Ship.A.BeltStern)}";
            string deck = $"{Inch(ship.armor, Ship.A.Deck)}/{Inch(ship.armor, Ship.A.DeckBow)}/{Inch(ship.armor, Ship.A.DeckStern)}";
            string turret = $"{Inch(ship.armor, Ship.A.TurretSide)}/{Inch(ship.armor, Ship.A.TurretTop)}/{Inch(ship.armor, Ship.A.Barbette)}";
            string citadel = $"{Inch(ship.armor, Ship.A.InnerBelt_1st)}/{Inch(ship.armor, Ship.A.InnerDeck_1st)}";
            string misc = $"{Inch(ship.armor, Ship.A.ConningTower)}/{Inch(ship.armor, Ship.A.Superstructure)}";

            return $"armor=in belt={belt}, deck={deck}, turret={turret}, citadel={citadel}, ct/super={misc}";
        }

        private static string RejectionSummary(Ship ship)
        {
            List<string> reasons = new();
            if (ship == null || ship.hull == null || ship.hull.data == null)
                return "ship data unavailable";

            try
            {
                Patch_Ship.CountMainGuns(ship, out int mainTurrets, out int mainBarrels);
                if (ship.hull.data.minMainTurrets > 0 && mainTurrets < ship.hull.data.minMainTurrets)
                    reasons.Add($"main turrets {mainTurrets}/{ship.hull.data.minMainTurrets}");

                if (ship.hull.data.minMainBarrels > 0 && mainBarrels < ship.hull.data.minMainBarrels)
                    reasons.Add($"main barrels {mainBarrels}/{ship.hull.data.minMainBarrels}");

                if (ship.Weight() > ship.Tonnage())
                    reasons.Add($"overweight {ship.Weight():0}t/{ship.Tonnage():0}t");
            }
            catch
            {
            }

            try
            {
                if (!ship.IsValidCostReqParts(
                    out string isValidCostReqPartsReason,
                    out Il2CppSystem.Collections.Generic.List<ShipType.ReqInfo> notPassed,
                    out Il2CppSystem.Collections.Generic.Dictionary<Part, string> badParts))
                {
                    if (notPassed != null && notPassed.Count > 0)
                    {
                        List<string> reqs = new();
                        foreach (ShipType.ReqInfo req in notPassed)
                            reqs.Add($"{req.stat.name}={ship.stats[req.stat].total:0.##} ({req.min:0.##}-{req.max:0.##})");

                        reasons.Add($"unmet reqs: {string.Join("; ", reqs)}");
                    }
                    else if (badParts != null && badParts.Count > 0)
                    {
                        reasons.Add($"invalid parts={badParts.Count}");
                    }
                    else if (!string.IsNullOrWhiteSpace(isValidCostReqPartsReason))
                    {
                        reasons.Add($"req parts: {isValidCostReqPartsReason}");
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (!ship.IsValidCostWeightBarbette(
                    out string isValidCostWeightBarbetteReason,
                    out Il2CppSystem.Collections.Generic.List<Part> errorBarbettePart))
                {
                    if (errorBarbettePart != null && errorBarbettePart.Count > 0)
                        reasons.Add($"empty barbettes={errorBarbettePart.Count}");
                    else if (!string.IsNullOrWhiteSpace(isValidCostWeightBarbetteReason))
                        reasons.Add($"barbette: {isValidCostWeightBarbetteReason}");
                }
            }
            catch
            {
            }

            try
            {
                if (!ship.player.IsTonnageAllowedByTech(ship.Tonnage(), ship.shipType))
                    reasons.Add("tonnage outside tech range");
            }
            catch
            {
            }

            try
            {
                if (!ship.IsValidWeightOffset())
                {
                    float instX = ship.stats_[G.GameData.stats["instability_x"]].total;
                    float instZ = ship.stats_[G.GameData.stats["instability_z"]].total;
                    reasons.Add($"balance x={instX:0.##}, z={instZ:0.##}");
                }
            }
            catch
            {
            }

            try
            {
                if (reasons.Count == 0 && !ship.IsValid(false))
                    reasons.Add("general validity failed");
            }
            catch
            {
            }

            return reasons.Count > 0 ? string.Join("; ", reasons.Take(4)) : "vanilla validation failed";
        }

        private static void LogAttemptRejected(Ship._GenerateRandomShip_d__573 routine, int rejectedAttempt)
        {
            Ship ship = routine.__4__this ?? _summaryShip;
            if (ship == null || !_summaryActive)
                return;

            float elapsed = _summaryStartedAt > 0f ? Time.realtimeSinceStartup - _summaryStartedAt : 0f;
            Melon<TweaksAndFixes>.Logger.Msg(
                $"GG shipgen attempt rejected: {ShipLabel(ship)}, attempt={rejectedAttempt}/{routine._triesTotal_5__4}, elapsed={elapsed:0.0}s, reason={RejectionSummary(ship)}");
        }

        private static void LogShipgenStart(Ship._GenerateRandomShip_d__573 routine)
        {
            Ship ship = routine.__4__this;
            if (ship == null)
                return;

            _summaryShip = ship;
            _summaryStartedAt = Time.realtimeSinceStartup;
            _summaryActive = true;

            // Patch intent: keep one concise, always-on breadcrumb for vanilla
            // baseline shipgen so logs show what was attempted without enabling
            // the much noisier TAF diagnostic trace.
            Melon<TweaksAndFixes>.Logger.Msg(
                $"GG shipgen start: {ShipLabel(ship)}, country={PlayerLabel(ship)}, year={ShipYear(ship)}, tonnage={ship.Tonnage():0}t/{ship.TonnageMax():0}t");
        }

        private static void LogShipgenEnd(Ship._GenerateRandomShip_d__573 routine)
        {
            Ship ship = routine.__4__this ?? _summaryShip;
            if (ship == null || !_summaryActive)
                return;

            float elapsed = _summaryStartedAt > 0f ? Time.realtimeSinceStartup - _summaryStartedAt : 0f;
            bool success = routine._tryN_5__5 != routine._triesTotal_5__4;
            string result = success ? "success" : "failure";
            string rejected = success ? string.Empty : $", reason={RejectionSummary(ship)}";

            Melon<TweaksAndFixes>.Logger.Msg(
                $"GG shipgen end: {ShipLabel(ship)}, result={result}, attempts={routine._tryN_5__5}/{routine._triesTotal_5__4}, elapsed={elapsed:0.0}s, {SpeedSummary(ship)}, weight={ship.Weight():0}t/{ship.Tonnage():0}t, {ArmorSummary(ship)}, {GunSummary(ship)}, {TorpedoSummary(ship)}{rejected}");

            _summaryShip = null;
            _summaryStartedAt = 0f;
            _summaryActive = false;
        }

        [HarmonyPatch(typeof(Ship))]
        internal class Patch_SetCaliberModifiers
        {
            [HarmonyPatch(nameof(Ship.SetCaliberDiameter))]
            [HarmonyPrefix]
            internal static void Prefix_SetCaliberDiameter(Ship.TurretCaliber __0, ref float __1)
            {
                // Patch intent: keep vanilla's gun choice but floor generated diameter
                // modifiers to the nearest lower whole-inch caliber. This avoids rejecting
                // otherwise useful gun candidates and leaves manual designer edits alone.
                if (IsVanillaBaselineShipgen() && __0?.turretPartData != null)
                    __1 = FloorGunDiameterModifier(__0.turretPartData, __1);
            }
        }

        [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__573))]
        internal class Patch_GenerateRandomShipGunLifecycle
        {
            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPrefix]
            internal static void Prefix(Ship._GenerateRandomShip_d__573 __instance, out int __state)
            {
                __state = __instance._tryN_5__5;

                if (!Patch_Ship.UseVanillaShipgenBaseline())
                    return;

                // Patch intent: expose vanilla shipgen activity to the gun modifier guards
                // without re-enabling the disabled TAF GenerateRandomShip override path.
                _generateRandomShipState = __instance.__1__state;

                if (__instance.__1__state == 0 && !_summaryActive)
                    LogShipgenStart(__instance);
            }

            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPostfix]
            internal static void Postfix(Ship._GenerateRandomShip_d__573 __instance, int __state, ref bool __result)
            {
                if (Patch_Ship.UseVanillaShipgenBaseline())
                {
                    OptimizeGeneratedGunComponents(__instance.__4__this);
                    FloorGeneratedGunCalibers(__instance.__4__this);

                    if (__state != __instance._tryN_5__5)
                        LogAttemptRejected(__instance, __state);

                    if (!__result || __instance.__1__state < 0)
                        LogShipgenEnd(__instance);
                }

                _generateRandomShipState = -1;
            }
        }
    }
}
