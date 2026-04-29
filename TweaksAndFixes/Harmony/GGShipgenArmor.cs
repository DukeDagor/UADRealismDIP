using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TweaksAndFixes
{
    internal static class GGShipgenArmor
    {
        private static int _generateRandomShipState = -1;

        private static bool IsVanillaBaselineShipgen()
        {
            return Patch_Ship.UseVanillaShipgenBaseline() && _generateRandomShipState >= 0;
        }

        private static Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>? TryGenerateDataDrivenArmor(float armorMaximal, Ship shipHint)
        {
            if (shipHint == null)
                return null;

            float year;
            if (GameManager.IsMission && BattleManager.Instance.CurrentAcademyMission != null)
            {
                year = shipHint.player.isMain
                    ? BattleManager.Instance.CurrentAcademyMission.year
                    : BattleManager.Instance.CurrentAcademyMission.enemyYear;
            }
            else if (GameManager.IsCustomBattle)
            {
                if (shipHint.player.isMain)
                {
                    year = BattleManager.Instance.CurrentCustomBattle != null
                        ? BattleManager.Instance.CurrentCustomBattle.player1.year
                        : CampaignController.Instance.StartYear;
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

            var info = GenArmorData.GetInfoFor(shipHint, _generateRandomShipState >= 0 ? -1f : year);
            if (info == null)
                return null;

            if (shipHint.armor == null)
                shipHint.armor = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();

            // GenerateRandomShip applies this year factor before calling Ship.GenerateArmor.
            // TAF armor tables already scale by year, so remove the vanilla factor here.
            if (_generateRandomShipState == 5)
                armorMaximal /= Util.Remap(shipHint.GetYear(shipHint), 1890f, 1940f, 1.0f, 0.85f, true);

            var dict = new Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float>();
            var citArmor = shipHint.GetCitadelArmor();

            float maxBelt = info.GetMaxArmorValue(shipHint, Ship.A.Belt, null);
            if (maxBelt <= 0f)
                return null;

            float portion = Mathf.Min(1f, armorMaximal / maxBelt);
            for (Ship.A a = Ship.A.Belt; a < Ship.A.InnerBelt_1st; a += 1)
                dict[a] = info.GetArmorValue(shipHint, a, portion);

            var oldDict = shipHint.armor;
            shipHint.armor = dict;
            if (citArmor != null)
            {
                foreach (var a in citArmor)
                    dict[a] = info.GetArmorValue(shipHint, a, portion);
            }

            shipHint.armor = oldDict;
            return dict;
        }

        private static bool SyncShipgenTurretArmor(Ship ship)
        {
            if (ship == null || ship.shipTurretArmor == null || ship.shipTurretArmor.Count == 0)
                return false;

            if (ship.armor == null)
                return false;

            var info = GenArmorData.GetInfoFor(ship);
            if (info == null)
                return false;

            float armorLerp = info.EstimateLerp(ship);
            return info.SyncTurretArmor(ship, armorLerp);
        }

        [HarmonyPatch(typeof(Ship))]
        internal class Patch_GenerateDataDrivenArmor
        {
            [HarmonyPatch(nameof(Ship.GenerateArmor))]
            [HarmonyPrefix]
            internal static bool Prefix(float armorMaximal, Ship shipHint, ref Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> __result)
            {
                // Patch intent: keep vanilla ship generation, but replace vanilla's armor
                // dictionary with TAF's data-driven armor table when the table is available.
                // If data is missing, fall through to vanilla instead of using broader TAF logic.
                if (!IsVanillaBaselineShipgen())
                    return true;

                var armor = TryGenerateDataDrivenArmor(armorMaximal, shipHint);
                if (armor == null)
                    return true;

                __result = armor;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ship._GenerateRandomShip_d__573))]
        internal class Patch_GenerateRandomShipArmorLifecycle
        {
            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPrefix]
            internal static void Prefix(Ship._GenerateRandomShip_d__573 __instance)
            {
                if (!Patch_Ship.UseVanillaShipgenBaseline())
                    return;

                _generateRandomShipState = __instance.__1__state;

                // Patch intent: vanilla creates per-gun turret armor entries after global armor
                // generation. Before vanilla validates guns and the final design, resync those
                // entries from the copied TAF armor table so guns do not keep stale/zero armor.
                if (_generateRandomShipState == 11)
                    SyncShipgenTurretArmor(__instance.__4__this);
            }

            [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
            [HarmonyPostfix]
            internal static void Postfix()
            {
                _generateRandomShipState = -1;
            }
        }
    }
}
