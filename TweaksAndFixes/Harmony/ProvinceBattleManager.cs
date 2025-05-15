using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;

#pragma warning disable CS8603

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(ProvinceBattleManager))]
    internal class Patch_ProvinceBattleManager
    {
        private static readonly List<Province> _AttackerProvs = new List<Province>();

        //[HarmonyPatch(nameof(ProvinceBattleManager.TryStartBattle))]
        //[HarmonyPrefix]
        internal static bool Prefix_TryStartBattle(Player player)
        {
            if (player.provinces.Count == 0)
                return false;

            float pow = player.NationPower();
            float army = player.ArmyForce();
            float log = player.LogisticsFactorFinal();
            float nation_attack_power_ratio = Config.Param("nation_attack_power_ratio", 1.25f);
            float major_offensive_attack_ratio = Config.Param("major_offensive_attack_ratio", 1f);
            float major_offensive_attack_ratio_nationpower = Config.Param("major_offensive_attack_ratio_nationpower", 2f);
            var enemies = player.AtWarWith().Shuffled().ToList();
            foreach (var e in enemies)
            {
                var eProvs = e.provinces;
                if (eProvs.Count == 0)
                    continue;

                float ePow = e.NationPower();
                float eArmy = e.ArmyForce();
                float eLog = e.LogisticsFactorFinal();

                float bestDist = float.MaxValue;
                Province? provinceA = null;
                Province? provinceD = null;

                Player attacker, defender;
                float powA, armyA, logA, powD, armyD, logD;
                if (pow * army * log * nation_attack_power_ratio >= ePow * eArmy * eLog)
                {
                    attacker = player;
                    defender = e;
                    powA = pow;
                    armyA = army;
                    logA = log;
                    powD = ePow;
                    armyD = eArmy;
                    logD = eLog;
                }
                else
                {
                    attacker = e;
                    defender = player;
                    powA = ePow;
                    armyA = eArmy;
                    logA = eLog;
                    powD = pow;
                    armyD = army;
                    logD = log;
                }
                bool foundAttProv = false;
                bool foundDefProv = false;
                bool foundRange = false;
                bool foundPow = false;

                int pC = attacker.provinces.Count;
                _AttackerProvs.Capacity = pC;
                foreach (var p in attacker.provinces)
                    _AttackerProvs.Add(p);
                int tries = Math.Min(10, pC);
                for (int i = 0; i < tries; ++i)
                {
                    int idx = UnityEngine.Random.Range(0, _AttackerProvs.Count - 1);
                    var prov = _AttackerProvs[idx];
                    _AttackerProvs.RemoveAt(idx);

                    if (ProvinceBattleManager.InBattle(prov))
                        continue;

                    foundAttProv = true;

                    var hasSeaDict = CampaignMap.ProvincesDb.HasSea[prov];
                    var distDict = CampaignMap.ProvincesDb.Distance[prov];

                    float localBestDist = float.MinValue;
                    Province? localBest = null;
                    foreach (var p in defender.provinces)
                    {
                        if (hasSeaDict[p] || !prov.NeighbourProvinces.Contains(p))
                            continue;
                        foundDefProv = true;

                        float dist = distDict[p];
                        if (dist > localBestDist)
                        {
                            localBestDist = dist;
                            localBest = p;
                        }
                    }
                    if (localBest != null && localBestDist <= 2000f)
                    {
                        foundRange = true;
                        float parmyA = attacker.ArmyForceForProvince(prov);
                        float parmyD = defender.ArmyForceForProvince(localBest);
                        if (parmyA * logA >= parmyD * logD * major_offensive_attack_ratio
                            || powA * armyA >= powD * armyD * major_offensive_attack_ratio_nationpower)
                        {
                            foundPow = true;
                            if (bestDist > localBestDist)
                            {
                                bestDist = localBestDist;
                                provinceA = prov;
                                provinceD = localBest;
                            }
                        }
                    }
                }
                _AttackerProvs.Clear();
                Melon<TweaksAndFixes>.Logger.Msg($"{player.data.name} vs {e.data.name} ({(player == attacker ? "attacker" : "defender")}). Found att prov {foundAttProv}, found def prov {foundDefProv}, in range {foundRange}, have power {foundPow}");
                if (provinceA != null && provinceD != null)
                {
                    if (provinceD.ControllerPlayer.data.threatChance >= UnityEngine.Random.Range(0f, 1f))
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"Starting battle between {provinceA.Id} ({provinceA.ControllerPlayer.data.name}) and {provinceD.Id} ({provinceD.ControllerPlayer.data.name})");
                        ProvinceBattleManager.StartBattle(provinceA, provinceD);
                        break;
                    }
                    else
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"*** Tried to start battle between {provinceA.Id} ({provinceA.ControllerPlayer.data.name}) and {provinceD.Id} ({provinceD.ControllerPlayer.data.name}) but threatChance ({provinceD.ControllerPlayer.data:F2}) was under roll");
                    }
                }
            }
            return false;
        }



        [HarmonyPatch(nameof(ProvinceBattleManager.TryGenerateSmallerNationsBattle))]
        [HarmonyPrefix]
        internal static bool Prefix_TryGenerateSmallerNationsBattle()
        {
            if (Config.USER_CONFIG.Minor_And_Medium_Nation_Land_Invasions.Disable_Minor_Nation_Invasions)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Skipped attempt to generate minor nation battle.");
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(ProvinceBattleManager.GenerateBattlesForMediumNations))]
        [HarmonyPrefix]
        internal static bool Prefix_GenerateBattlesForMediumNationse()
        {
            if (Config.USER_CONFIG.Minor_And_Medium_Nation_Land_Invasions.Disable_Medium_Nation_Invasions)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Skipped attempt to generate medium nation battle.");
                return false;
            }
            return true;
        }
    }
}
