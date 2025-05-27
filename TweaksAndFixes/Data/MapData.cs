//#define LOGHULLSTATS
//#define LOGHULLSCALES
//#define LOGPARTSTATS
//#define LOGGUNSTATS

using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSystem.Runtime.Remoting.Messaging;

#pragma warning disable CS0649
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8605
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8625

namespace TweaksAndFixes
{
    public class MapData
    {
        private static void LogChanged<T>(T a, T b, string name, ref string log)
        {
            if (typeof(T) == typeof(float))
            {
                object tmp = (object)a;
                tmp = (float)Math.Round((float)tmp, 6);
                a = (T)tmp;
                tmp = (object)b;
                tmp = (float)Math.Round((float)tmp, 6);
                b = (T)tmp;
            }
            if (a.Equals(b))
                return;


            log += $" {name}: {a}->{b}.";
        }

        public abstract class MapDataLoader<T> where T : MapElement2D
        {
            [Serializer.Field] public string name;
            public abstract void Apply(T old);
            public abstract void FillFrom(T old);
        }

        public class PortElementDTO : MapDataLoader<PortElement>
        {
            //@name,enabled,nameUi,province,#provinceName,latitude,longitude,#posLink,portСapacity,#baseportcapacity,#balancer,seaControlDistanceMultiplier,inBeingDistanceMultiplier,#changesea
            [Serializer.Field] string nameUi;
            [Serializer.Field] string province;
            [Serializer.Field] int portCapacity = -1;
            [Serializer.Field] float seaControlDistanceMultiplier = -1;
            [Serializer.Field] float inBeingDistanceMultiplier = -1;

            public override void Apply(PortElement old)
            {
                if (Config.OverrideMap == Config.OverrideMapOptions.LogDifferences)
                {
                    string logStr = string.Empty;
                    LogChanged(old.Name, nameUi, "nameUi", ref logStr);
                    LogChanged(old.ProvinceId, province, "province", ref logStr);
                    LogChanged(old.PortCapacity, portCapacity, "portCapacity", ref logStr);
                    LogChanged(old.SeaControlDistanceMultiplier, seaControlDistanceMultiplier, "seaControlDistanceMultiplier", ref logStr);
                    LogChanged(old.InBeingDistanceMultiplier, inBeingDistanceMultiplier, "inBeingDistanceMultiplier", ref logStr);

                    if (logStr != string.Empty)
                        Melon<TweaksAndFixes>.Logger.Msg($"Differences for port {old.Id}:{logStr}");
                }
                old.Name = nameUi;
                old.ProvinceId = province;
                old.PortCapacity = portCapacity;
                old.SeaControlDistanceMultiplier = seaControlDistanceMultiplier;
                old.InBeingDistanceMultiplier = inBeingDistanceMultiplier;
            }

            public override void FillFrom(PortElement old)
            {
                name = old.Id;
                nameUi = old.Name;
                province = old.ProvinceId;
                portCapacity = old.PortCapacity;
                seaControlDistanceMultiplier = old.SeaControlDistanceMultiplier;
                inBeingDistanceMultiplier = old.InBeingDistanceMultiplier;
            }
        }

        public class ProvinceDTO : MapDataLoader<Province>
        {
            //@name,enabled,nameUi,area,controller,controller_1890,controller_1900,controller_1910,controller_1920,controller_1930,controller_1940,claim,isHome,
            ///type,latitude,longitude,#posLink,development,port,population,oilDiscoveryYear,oilDiscoveryBaseChance,oilCapacity,oilReservesInTurns,revoltChance,provinceArmyPercentage,
            ///ProvinceDefenderBonus,neighbour_provinces,#Naval_Defense_Level,#Enemies,#Protectors,#Rebellion_Chance,#Naval_Invasion_Chance,#Min_Naval_Invasion_Displacement,#AverageFleetStrength,#comment,#comment2
            [Serializer.Field] string nameUi;
            [Serializer.Field] string area;
            [Serializer.Field] string controller;
            [Serializer.Field] string controller_1890;
            [Serializer.Field] string controller_1900;
            [Serializer.Field] string controller_1910;
            [Serializer.Field] string controller_1920;
            [Serializer.Field] string controller_1930;
            [Serializer.Field] string controller_1940;
            [Serializer.Field] string claim;
            [Serializer.Field] int isHome;
            [Serializer.Field] string type;
            [Serializer.Field] float development;
            [Serializer.Field] float port;
            [Serializer.Field] float population;
            [Serializer.Field] int oilDiscoveryYear;
            [Serializer.Field] float oilDiscoveryBaseChance;
            [Serializer.Field] float oilCapacity;
            [Serializer.Field] float oilReservesInTurns;
            [Serializer.Field] float revoltChance;
            [Serializer.Field] float provinceArmyPercentage;
            [Serializer.Field] float ProvinceDefenderBonus;
            [Serializer.Field] string neighbour_provinces;

            public override void Apply(Province old)
            {
                if (Config.OverrideMap == Config.OverrideMapOptions.LogDifferences)
                {
                    string logStr = string.Empty;
                    LogChanged(old.Name, nameUi, "nameUI", ref logStr);
                    LogChanged(old.AreaId, area, "area", ref logStr);
                    LogChanged(old.Controller, controller, "controller", ref logStr);
                    LogChanged(old.Controller_1890, controller_1890, "controller_1890", ref logStr);
                    LogChanged(old.Controller_1900, controller_1900, "controller_1900", ref logStr);
                    LogChanged(old.Controller_1910, controller_1910, "controller_1910", ref logStr);
                    LogChanged(old.Controller_1920, controller_1920, "controller_1920", ref logStr);
                    LogChanged(old.Controller_1930, controller_1930, "controller_1930", ref logStr);
                    LogChanged(old.Controller_1940, controller_1940, "controller_1940", ref logStr);
                    LogChanged(old.Claim, claim, "Claim", ref logStr);
                    LogChanged(old.IsHome, isHome > 0, "isHome", ref logStr);
                    LogChanged(old.Type, type, "type", ref logStr);
                    LogChanged(old.InitialDevelopment, development, "development", ref logStr);
                    LogChanged(old.Port, port, "Port", ref logStr);
                    LogChanged(old.population, population, "population", ref logStr);
                    LogChanged(old.oilDiscoveryYear, oilDiscoveryYear, "oilDiscoveryYear", ref logStr);
                    LogChanged(old.oilDiscoveryBaseChance, oilDiscoveryBaseChance, "oilDiscoveryBaseChance", ref logStr);
                    LogChanged(old.oilCapacity, oilCapacity, "oilCapacity", ref logStr);
                    LogChanged(old.oilReservesInTurns, oilReservesInTurns, "oilReservesInTurns", ref logStr);
                    LogChanged(old.RevoltChance, revoltChance, "revoltChance", ref logStr);
                    LogChanged(old.ProvinceArmyPercentage, provinceArmyPercentage, "provinceArmyPercentage", ref logStr);
                    LogChanged(old.ProvinceDefenderBonus, ProvinceDefenderBonus, "ProvinceDefenderBonus", ref logStr);
                    LogChanged(old.NeighbourProvincesString, neighbour_provinces, "neighbour_provinces", ref logStr);

                    if (logStr != string.Empty)
                        Melon<TweaksAndFixes>.Logger.Msg($"Differences for province {old.Id}:{logStr}");
                }

                old.Name = nameUi;
                old.AreaId = area;
                old.Controller = controller;
                old.Controller_1890 = controller_1890;
                old.Controller_1900 = controller_1900;
                old.Controller_1910 = controller_1910;
                old.Controller_1920 = controller_1920;
                old.Controller_1930 = controller_1930;
                old.Controller_1940 = controller_1940;
                old.Claim = claim;
                old.IsHome = isHome > 0;
                old.Type = type;
                old.InitialDevelopment = development;
                old.Port = port;
                old.population = population;
                old.oilDiscoveryYear = oilDiscoveryYear;
                old.oilDiscoveryBaseChance = oilDiscoveryBaseChance;
                old.oilCapacity = oilCapacity;
                old.oilReservesInTurns = oilReservesInTurns;
                old.RevoltChance = revoltChance;
                old.ProvinceArmyPercentage = provinceArmyPercentage;
                old.ProvinceDefenderBonus = ProvinceDefenderBonus;
                old.NeighbourProvincesString = neighbour_provinces;
            }

            public override void FillFrom(Province old)
            {
                name = old.Id;
                nameUi = old.Name;
                area = old.AreaId;
                controller = old.Controller;
                controller_1890 = old.Controller_1890;
                controller_1900 = old.Controller_1900;
                controller_1910 = old.Controller_1910;
                controller_1920 = old.Controller_1920;
                controller_1930 = old.Controller_1930;
                controller_1940 = old.Controller_1940;
                claim = old.Claim;
                isHome = old.IsHome ? 1 : 0;
                type = old.Type;
                development = old.InitialDevelopment;
                port = old.Port;
                population = old.population;
                oilDiscoveryYear = old.oilDiscoveryYear;
                oilDiscoveryBaseChance = old.oilDiscoveryBaseChance;
                oilCapacity = old.oilCapacity;
                oilReservesInTurns = old.oilReservesInTurns;
                revoltChance = old.RevoltChance;
                provinceArmyPercentage = old.ProvinceArmyPercentage;
                ProvinceDefenderBonus = old.ProvinceDefenderBonus;
                neighbour_provinces = old.NeighbourProvincesString;
            }
        }

        public static void LoadMapData()
        {
            if (Config.OverrideMap == Config.OverrideMapOptions.DumpData)
                WriteData();
            else
                LoadData();
        }

        private static void WriteData()
        {
            bool success = true;
            success &= Write<PortElementDTO, PortElement>("portsDump", CampaignMap.Instance.Ports.Ports);
            success &= Write<ProvinceDTO, Province>("provincesDump", CampaignMap.Instance.Provinces.Provinces);
            if (success)
                Melon<TweaksAndFixes>.Logger.Msg($"Wrote original map data successfully`");
            else
                Melon<TweaksAndFixes>.Logger.Error($"Failed to write original map data");
        }

        private static bool VerifyProvinces(Il2CppSystem.Collections.Generic.List<Province> provinces)
        {
            bool success = true;
            HashSet<string> seenProvinces = new HashSet<string>();
            HashSet<string> seenNeighbors = new HashSet<string>();

            foreach (var province in provinces)
            {
                // Check for duplicates
                if (seenProvinces.Contains(province.Id) || seenProvinces.Contains(province.Name))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Province {province.Id} {province.Name} is duplicated");
                    success = false;
                }
                
                // Track checked provinces
                seenProvinces.Add(province.Id);
                seenProvinces.Add(province.Name);

                // Fetch neighbors, split by comma
                string[] neighbors = province.NeighbourProvincesString.Split(',');

                // Skip if the province has no neighbors
                if (neighbors.Length == 1 && neighbors[0].Trim().Length == 0)
                {
                    continue;
                }

                // Reset neighbor tracking and check for duplicates
                seenNeighbors.Clear();
                for (int i = neighbors.Length; i-- > 0;)
                {
                    string neighbor = neighbors[i].Trim();
                    neighbors[i] = neighbor;
                    if (seenNeighbors.Contains(neighbor))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Province \"{province.Id}\" has duplicate neighbor \"{neighbor}\"");
                        success = false;
                    }
                    seenNeighbors.Add(neighbor);
                }

                // Check if province has itself as a neighbor
                if (neighbors.Contains(province.Id))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Province \"{province.Id}\" has self as neighbor");
                    success = false;
                }

                // Loop over all neighbors and check if they exist and are two-way paired
                foreach (string neighbor in neighbors)
                {
                    bool logErr = true;

                    // Skip if the province has no neighbors
                    if (neighbor.Length == 0)
                    {
                        continue;
                    }

                    // Check list of provinces for a match
                    foreach (var otherProvince in provinces)
                    {
                        if (otherProvince.Id != neighbor)
                        {
                            continue;
                        }

                        logErr = false;
                        string[] neighborsO = otherProvince.NeighbourProvincesString.Split(',');

                        for (int i = neighborsO.Length; i-- > 0;)
                        {
                            neighborsO[i] = neighborsO[i].Trim();
                        }
                        
                        if (!neighborsO.Contains(province.Id))
                        {
                            Melon<TweaksAndFixes>.Logger.Warning($"Province \"{province.Id}\" has neighbor \"{neighbor}\" but that does not have \"{province.Id}\" as neighbor");
                            success = false;
                        }
                    }

                    // If no match is found, print a warning
                    if (logErr)
                    {
                        Melon<TweaksAndFixes>.Logger.Warning($"Province \"{province.Id}\" has neighbor \"{neighbor}\" but that province could not be found");
                        success = false;
                    }
                }
            }

            return success;
        }

        private static void LoadData()
        {
            bool success = true;
            success &= Load<PortElementDTO, PortElement>("ports", CampaignMap.Instance.Ports.Ports);
            success &= Load<ProvinceDTO, Province>("provinces", CampaignMap.Instance.Provinces.Provinces);
            success &= VerifyProvinces(CampaignMap.Instance.Provinces.Provinces);
            if (success)
                Melon<TweaksAndFixes>.Logger.Msg($"Loaded map data successfully");
            else
                Melon<TweaksAndFixes>.Logger.Error($"Port and/or Province override data may be malformed!");
        }

        private static bool Load<T, U>(string assetName, Il2CppSystem.Collections.Generic.List<U> oldList) where U : MapElement2D where T : MapDataLoader<U>, new()
        {
            var input = Serializer.CSV.GetTextFromFileOrAsset(assetName);
            if (input == null)
                return false;

            if (input.Length < 2)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Fewer than 2 lines of text in asset `{assetName}`");
                return false;
            }
            Dictionary<string, T> dict = new Dictionary<string, T>();

            bool success = Serializer.CSV.Read<Dictionary<string, T>, string, T>(input, dict, "name", true, true);
            foreach (var old in oldList)
                if (dict.TryGetValue(old.Id, out var item))
                    item.Apply(old);

            return success;
        }

        private static bool Write<T, U>(string fileBase, Il2CppSystem.Collections.Generic.List<U> oldList) where U : MapElement2D where T : MapDataLoader<U>, new()
        {
            if (!Directory.Exists(Config._BasePath))
            {
                Melon<TweaksAndFixes>.Logger.Error("Failed to find Mods directory: " + Config._BasePath);
                return false;
            }
            string filePath = Path.Combine(Config._BasePath, fileBase + ".csv");

            List<T> list = new List<T>();
            foreach (var old in oldList)
            {
                var item = new T();
                item.FillFrom(old);
                list.Add(item);
            }

            return Serializer.CSV.Write<List<T>, T>(list, filePath, "name", true);
        }
    }
}