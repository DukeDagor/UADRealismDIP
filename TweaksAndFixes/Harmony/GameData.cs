using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using TweaksAndFixes.Data;

#pragma warning disable CS8600
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(GameData))]
    internal class Patch_GameData
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameData.LoadVersionAndData))]
        internal static void Postfix_LoadVersionAndData(GameData __instance)
        {
            if (!Directory.Exists(Config._BasePath))
            {
                Melon<TweaksAndFixes>.Logger.Error("Failed to find Mods directory: " + Config._BasePath + ". Creating.");
                Directory.CreateDirectory(Config._BasePath);
            }

            GameDataM.LoadData(__instance);
            Patch_Ui.UpdateVersionString(G.ui);
        }        

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameData.PostProcessAll))]
        internal static void Prefix_PostProcessAll(GameData __instance)
        {
            Patch_PlayerData.PatchPlayerMaterials();
            //Serializer.CSV.TestNative();

            // We have to load Config (from params) before postprocess runs
            // because some other systems require Config to be live. This
            Config.LoadConfig();
            // TODO: Add a multicast delegate to run post-load config

            Melon<TweaksAndFixes>.Logger.Msg("************************************************** Loading TAF data:");
            GradeExtensions.LoadData();
            GenArmorData.LoadData();
            AccuraciesExInfo.LoadData();
            MountOverrideData.LoadData();

            // TODO: Ew! Replace this trash!!!
            foreach (var ev in G.GameData.events)
            {
                ev.Value.param.Replace("low_money", "");
                if (ev.Value.paramx.ContainsKey("low_money")) ev.Value.paramx.Remove("low_money");
            }
        }

        private static readonly List<string> _FixKeys = new List<string>();
        private static void FixRandPart(RandPart rp)
        {
            foreach (var kvp in rp.paramx)
            {
                switch (kvp.Key)
                {
                    case "and":
                    case "or":
                    case "mount":
                    case "!mount":
                    case "delete_unmounted":
                    case "delete_refit":
                    case "scheme":
                    case "tr_rand_mod":
                    case "auto_refit":
                        return;
                }
                _FixKeys.Add(kvp.Key);
            }
            foreach (var key in _FixKeys)
            {
                var newVal = key.Replace(")", string.Empty).Replace("(", string.Empty);
                rp.paramx.Remove(key);
                // we use and unless it doesn't exist and or does.
                if (!rp.paramx.TryGetValue("and", out var lst) && !rp.paramx.TryGetValue("or", out lst))
                {
                    lst = new Il2CppSystem.Collections.Generic.List<string>();
                    rp.paramx["and"] = lst;
                }
                lst.Add(newVal);
                //Melon<TweaksAndFixes>.Logger.Msg($"Fixing Randpart {rp.name} with param {rp.param}, invalid key {key}. New value {newVal}");
            }
            _FixKeys.Clear();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameData.PostProcessAll))]
        internal static void Postfix_PostProcessAll(GameData __instance)
        {
            Debug.Log("Finished internal PostProcessAll");
            foreach (var rp in __instance.randParts.Values)
            {
                FixRandPart(rp);
            }
            foreach (var rp in __instance.randPartsRefit.Values)
            {
                FixRandPart(rp);
            }

            foreach (var data in __instance.parts.Values)
            {
                if (!data.isGun)
                    continue;

                data.GetCaliberInch();
            }
            //Serializer.CSV.TestNativePost();

            // Run after other things have a chance to update GameData
            MelonCoroutines.Start(FillDatabase());
        }

        internal static System.Collections.IEnumerator FillDatabase()
        {
            yield return new WaitForEndOfFrame();

            //var textAsset = Resources.Load<TextAsset>("packedShips");
            //var packedShipsStore = Util.DeserializeObjectByte<CampaignDesigns.Store>(textAsset.bytes);
            //var packedShips = packedShipsStore; //CampaignDesigns.FromStore(packedShipsStore);
            //string logstr = $"packedShips data";
            //foreach (var spy in packedShips.shipsPerYear)
            //{
            //    logstr += $"\n{spy.Key}: {spy.Value.shipsPerPlayer.Count} players";
            //    foreach (var spp in spy.Value.shipsPerPlayer)
            //    {
            //        logstr += $"\n\t{spp.Key}: {spp.Value.shipsPerType.Count} types:";
            //        bool c2 = false;
            //        foreach (var spt in spp.Value.shipsPerType)
            //        {
            //            if (c2)
            //                logstr += ",";
            //            c2 = true;
            //            logstr += " " + spt.Value.Count + " " + spt.Key;
            //        }
            //        logstr += ".";
            //        foreach (var spt in spp.Value.shipsPerType)
            //        {
            //            //logstr += $"\n\t\t{spt.Key}: {spt.Value.Count} ships.";
            //            bool comma = false;
            //            foreach (var s in spt.Value)
            //            {
            //                if (s.YearCreated == spy.Key)
            //                    continue;

            //                if (comma)
            //                    logstr += ",";
            //                comma = true;
            //                logstr += $" year mismatch: {s.vesselName} ({s.tonnage:N0}t, {s.YearCreated})";
            //            }
            //        }
            //    }
            //}
            //Melon<TweaksAndFixes>.Logger.Msg(logstr);

            //var json = Il2CppNewtonsoft.Json.JsonConvert.SerializeObject(packedShips.shipsPerYear[0].Value, Il2CppNewtonsoft.Json.Formatting.Indented, Util.SerializerSettings); //Il2CppNewtonsoft.Json.JsonConvert.SerializeObject(packedObj, Il2CppNewtonsoft.Json.Formatting.Indented, Util.SerializerSettings);
            //File.WriteAllText(Path.Combine(Config._BasePath, "packedShips.json"), json);

            Database.FillDatabase();

            Melon<TweaksAndFixes>.Logger.Msg("**************************************** Loaded database and config");
            if (Config.Param("taf_hot_reload", 0f) > 0f)
                GameDataReloader.Create();

            UiM.ApplyUiModifications();

            //string path = Path.Combine(Config._BasePath, Config._PredefinedDesignsFile);

            //var bytes = File.ReadAllBytes(path);
            //if (bytes == null)
            //    Melon<TweaksAndFixes>.Logger.Msg("null bytes");
            //var store = Util.DeserializeObjectByte<CampaignDesigns.Store>(bytes);
            //if (store == null)
            //    Melon<TweaksAndFixes>.Logger.Msg("Null store");
            //else
            //{
            //    Melon<TweaksAndFixes>.Logger.Msg("got store");
            //    bool isValid = true;
            //    int dCount = 0;
            //    if (store.shipsPerYear == null)
            //    {
            //        isValid = false;
            //        Melon<TweaksAndFixes>.Logger.Error("spy");
            //    }
            //    else
            //    {
            //        Melon<TweaksAndFixes>.Logger.Msg("got spy");
            //        foreach (var spy in store.shipsPerYear)
            //        {
            //            if (spy.Value == null)
            //            {
            //                isValid = false;
            //                Melon<TweaksAndFixes>.Logger.Msg("spy val null, but key " + spy.Key);
            //            }
            //            else if (spy.Value.shipsPerPlayer == null)
            //            {
            //                isValid = false;
            //                Melon<TweaksAndFixes>.Logger.Error(spy.Key + ": spp");
            //            }
            //            else
            //            {
            //                Melon<TweaksAndFixes>.Logger.Msg("got spp");
            //                foreach (var spp in spy.Value.shipsPerPlayer)
            //                {
            //                    if (spp.Value == null)
            //                    {
            //                        isValid = false;
            //                        Melon<TweaksAndFixes>.Logger.Error("spp value null, but key " + spp.Key);
            //                    }
            //                    else if (spp.Value.shipsPerType == null)
            //                    {
            //                        isValid = false;
            //                        Melon<TweaksAndFixes>.Logger.Error(spp.Key + ": spt");
            //                    }
            //                    else
            //                    {
            //                        Melon<TweaksAndFixes>.Logger.Msg("got spt");
            //                        foreach (var spt in spp.Value.shipsPerType)
            //                        {
            //                            if (spt.Value == null)
            //                            {
            //                                isValid = false;
            //                                Melon<TweaksAndFixes>.Logger.Error(spt.Key + ": spt");
            //                            }
            //                            else
            //                            {
            //                                dCount += spt.Value.Count;
            //                                Melon<TweaksAndFixes>.Logger.Msg("got list");
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    Melon<TweaksAndFixes>.Logger.Msg($"valid? {isValid} : {dCount}");
            //}

            //foreach (var f in typeof(Part).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))
            //{
            //    if (f.Name.StartsWith("NativeMethod"))
            //        Melon<TweaksAndFixes>.Logger.Msg("Field " + f.Name);
            //}

            //foreach (var hull in G.GameData.parts.Values)
            //    if (hull.isHull)
            //        hull.nameUi = Patch_Ship.GetHullModelKey(hull);

            //var playerToShips = new Dictionary<PlayerData, Dictionary<ShipType, List<Tuple<PartData, int>>>>();
            //foreach (var hull in G.GameData.parts.Values)
            //{
            //    if (!hull.isHull)
            //        continue;

            //    foreach (var pd in hull.countriesx)
            //    {
            //        var year = Database.GetYear(hull);
            //        if (year < 0)
            //            continue;
            //        playerToShips.ValueOrNew(pd).ValueOrNew(hull.shipType).Add(new Tuple<PartData, int>(hull, year));
            //    }
            //}

            //string logstr = "\nname,nameUi,type,year,model,scale,country,tonnageMin,tonnageMax,speedLimiter,param";
            //foreach (var kvp in playerToShips)
            //{
            //    foreach (var list in kvp.Value.Values)
            //    {
            //        list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            //        foreach (var tpl in list)
            //        {
            //            var hull = tpl.Item1;
            //            var year = tpl.Item2;
            //            string countries = kvp.Key.name;
            //            if (hull.countriesx.Count > 1)
            //            {
            //                foreach (var c in hull.countriesx)
            //                    if (c != kvp.Key)
            //                        countries += ", " + c.name;
            //                countries = "\"" + countries + "\"";
            //            }
            //            logstr += $"\n{hull.name},{hull.nameUi},{hull.shipType.name},{year},{hull.model},{hull.scale:F2},{countries},{hull.tonnageMin:F0},{hull.tonnageMax:F0},{hull.speedLimiter:F1},\"{hull.param}\"";
            //        }
            //    }
            //}
            //Debug.Log(logstr);
        }
    }
}
