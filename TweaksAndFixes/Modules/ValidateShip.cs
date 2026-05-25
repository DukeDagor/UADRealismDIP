using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweaksAndFixes.Modules
{
    internal class ValidateShip
    {


        public static bool forceStop = false;

        public static System.Collections.IEnumerator CheckDesigns()
        {
            var prefix = Storage.prefix + $"sorted/{Config.ParamS("nation_to_parse", "ERROR")}";
            Melon<TweaksAndFixes>.Logger.Msg($"{prefix}");
            if (!Directory.Exists(prefix))
                yield break;

            Patch_Part_CanPlaceGeneric.ForceCheck = true;
            int cnt = 0;

            var files = Directory.GetFiles(prefix, "*.bindesign");
            foreach (var f in files)
            {
                cnt++;

                if (cnt % 50 == 0)
                {
                    Part.CleanPartsStorage();
                    // Util.ClearCaches();
                    // Util.ClearResourcesCache();
                }

                var store = Util.DeserializeObjectByte<Ship.Store>(File.ReadAllBytes(f));
                Melon<TweaksAndFixes>.Logger.Msg($"{store.vesselName} | {store.playerName} | {store.YearCreated} | #{cnt}");

                bool missingData = false;
                foreach (var part in store.parts.ToArray())
                {
                    if (G.GameData.parts.ContainsKey(part.name))
                        continue;

                    missingData = true;
                    Melon<TweaksAndFixes>.Logger.Msg($"  Missing part data: {part.name}");
                }

                if (missingData)
                {
                    Melon<TweaksAndFixes>.Logger.Warning($"  Invalid part data!");

                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/invlaid_part_data/", baseName + ".bindesign");
                    File.Move(f, path);
                    continue;
                }

                if (PlayerController.Instance.data != G.GameData.players[store.playerName]
                    || CampaignController.Instance.CurrentDate.AsDate().Year != store.YearCreated)
                {
                    GameManager.Instance.RefreshSharedDesign(
                        store.YearCreated, G.GameData.players[store.playerName]
                    );

                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                }

                var p = PlayerController.Instance;

                var ship = Ship.Create(null, p, false, false, false);
                var guidRet = new Il2CppSystem.Nullable<Il2CppSystem.Guid>();
                if (!ship.FromStore(store, guidRet, null, p, false))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"  Couldn't load {store.vesselName} ({store.hullName}, {store.YearCreated})");
                    ship.Erase();
                    continue;
                }

                ship.tonnage = store.tonnage;
                ship.id = store.id;

                ship.SetShipName(Ship.GenerateRandomName(true, ship.shipType, ship.player.data, ""));

                Melon<TweaksAndFixes>.Logger.Msg($"  Renamed: {ship.Name(false, false)} ({ship.Weight()} / {ship.Tonnage()})");

                GameManager.Instance.ToConstructor(false, ship);

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                // yield return new WaitForSeconds(2);

                Patch_ShipGenRandom.OptimizeComponents(ship);

                // yield return new WaitForSeconds(2);

                bool isTonnageAllowedByTech = ship.player.IsTonnageAllowedByTech(ship.Tonnage(), ship.shipType);

                bool done = false;

                while (!done && !forceStop)
                {
                    // yield return new WaitForSeconds(1);

                    done = true;

                    foreach (var part in ship.parts.ToArray())
                    {
                        if (part.data.isFunnel || part.data.isTowerAny)
                            continue;

                        if (!part.CanPlace(out string denyReason))
                        {
                            if (denyReason != "available")
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    '{part.Name(ship)}' : Can't Place -> {denyReason}");
                                ship.RemovePart(part);

                                done = false;
                            }
                        }

                        if (Ship.IsMainCal(part.data, ship.shipType) && part.data.isGun && part.ship != null)
                        {
                            Part.FireSectorInfo info = new();
                            part.CalcFireSectorNonAlloc(info);
                            if (info.shootableAngleTotal / info.groupsShoot.Count < 30)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"    Gun {part.Name(ship)} : Small fire sector {info.shootableAngleTotal}*");
                                ship.RemovePart(part);

                                done = false;
                            }
                        }
                    }
                }

                done = false;

                HashSet<Part> triedMount = new();

                while (!done && !forceStop)
                {
                    // yield return new WaitForSeconds(1);

                    done = true;

                    foreach (var part in ship.parts.ToArray())
                    {
                        if (!part.data.isFunnel && !part.data.isTowerAny)
                            continue;

                        if (!part.CanPlace(out string denyReason))
                        {
                            if (denyReason == "amount")
                                continue;

                            if (part.data.isFunnel)
                            {
                                if (denyReason == "floor" && !triedMount.Contains(part))
                                {
                                    var closest = Patch_Mount.GetClosestMount(ship, part);

                                    if (closest != null)
                                    {
                                        part.Mount(closest);
                                        triedMount.Add(part);
                                        Melon<TweaksAndFixes>.Logger.Msg($"    '{part.Name(ship)}' : Remounted to mount #{ship.mounts.IndexOf(closest)}");
                                        done = false;
                                    }
                                    else
                                    {
                                        Melon<TweaksAndFixes>.Logger.Msg($"    '{part.Name(ship)}' : Can't Place -> {denyReason}");
                                        ship.RemovePart(part);

                                        done = false;
                                    }
                                }
                                else
                                {
                                    Melon<TweaksAndFixes>.Logger.Msg($"    '{part.Name(ship)}' : Can't Place -> {denyReason}");
                                    ship.RemovePart(part);

                                    done = false;
                                }
                            }

                            if (part.data.isTowerAny && denyReason == "floor" && !triedMount.Contains(part))
                            {
                                var closest = Patch_Mount.GetClosestMount(ship, part);

                                if (closest != null)
                                {
                                    part.Mount(closest);
                                    triedMount.Add(part);
                                    Melon<TweaksAndFixes>.Logger.Msg($"    '{part.Name(ship)}' : Remounted to mount #{ship.mounts.IndexOf(closest)}");
                                    done = false;
                                }
                            }
                        }
                    }
                }

                done = false;

                while (!done && !forceStop)
                {
                    // yield return new WaitForSeconds(1);

                    done = true;

                    foreach (var part in ship.parts.ToArray())
                    {
                        if (!part.data.isFunnel)
                            continue;

                        if (!part.CanPlace(out string denyReason))
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"    '{part.Name(ship)}' : Can't Place -> {denyReason}");
                            ship.RemovePart(part);

                            done = false;
                        }
                    }
                }

                done = false;
                bool hasBadStructure = false;
                int mainGunCount = 0;
                int mainGunBarrels = 0;
                int torpCount = 0;
                bool hasFunnel = false;

                while (!done && !forceStop)
                {
                    // yield return new WaitForSeconds(1);

                    hasBadStructure = false;
                    done = true;

                    mainGunCount = 0;
                    mainGunBarrels = 0;
                    torpCount = 0;
                    hasFunnel = false;

                    foreach (var part in ship.parts.ToArray())
                    {
                        if (!part.CanPlace(out string denyReason))
                        {
                            if (part.data.isTowerAny)
                            {
                                hasBadStructure = true;
                            }
                        }

                        if (part.data.isFunnel)
                        {
                            hasFunnel = true;
                        }

                        if (Ship.IsMainCal(part.data, ship.shipType) && part.data.isGun)
                        {
                            mainGunCount++;
                            mainGunBarrels += part.data.barrels;
                        }

                        if (part.data.isTorpedo)
                        {
                            torpCount++;
                        }
                    }
                }


                if (forceStop)
                {
                    forceStop = false;
                    yield break;
                }

                // Melon<TweaksAndFixes>.Logger.Warning($"  DONE!");
                // yield return new WaitForSeconds(5);

                if (hasBadStructure)
                {
                    Melon<TweaksAndFixes>.Logger.Warning($"  Invalid superstructure!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/bad_tower/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                if (!hasFunnel)
                {
                    Melon<TweaksAndFixes>.Logger.Warning($"  Insufficient funnels!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/insufficient_funnels/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                bool isValidCostWeightBarbette = ship.IsValidCostWeightBarbette(
                    out string isValidCostWeightBarbetteReason,
                    out Il2CppSystem.Collections.Generic.List<Part> errorBarbettePart);

                if (errorBarbettePart.Count > 0)
                    Melon<TweaksAndFixes>.Logger.Msg($"  Found empty barbettes:");

                foreach (var errorBarb in errorBarbettePart)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    Remove empty barbette: {errorBarb.Name(ship)}");
                    ship.RemovePart(errorBarb);
                }

                if (Input.GetKeyDown(KeyCode.Delete))
                {
                    break;
                }

                if (mainGunCount == 1 && ship.shipType.name != "tb")
                {
                    Melon<TweaksAndFixes>.Logger.Warning($"  Single gun!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/single_main_gun/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                if (mainGunCount < ship.hull.data.minMainTurrets && mainGunBarrels < ship.hull.data.minMainBarrels)
                {
                    Melon<TweaksAndFixes>.Logger.Warning($"  Insufficient main guns!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/insufficient_guns/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                if (ship.shipType.requirementsx.ContainsKey(G.GameData.stats["torpedo"])
                    && torpCount <= 0)
                {
                    Melon<TweaksAndFixes>.Logger.Warning($"  Insufficient torpedos!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/insufficient_torps/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                bool isValidWeightOffset = ship.IsValidWeightOffset();

                if (!isValidWeightOffset)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Invalid weight offset!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/invalid_weight_offset/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                float tonnage = ship.Tonnage();
                float weight = ship.Weight();
                float lastWeight = weight;
                float ratio = weight / tonnage - 1;
                ship.tempGoodWeight = tonnage;

                var rnd = new Il2CppSystem.Random();

                for (int i = 0; i < 20 && ratio > 0.02f; i++)
                {
                    ShipM.ReduceWeightByReducingCharacteristics(ship, rnd, i, 20);

                    tonnage = ship.Tonnage();
                    weight = ship.Weight();
                    ratio = weight / tonnage - 1;

                    if (lastWeight != weight)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Trim weight: {(lastWeight / tonnage - 1) * 100}% -> {(weight / tonnage - 1) * 100}% ({lastWeight - weight}t)");
                        // yield return new WaitForSeconds(2);
                    }

                    lastWeight = weight;
                }

                tonnage = ship.Tonnage();
                weight = ship.Weight();
                ratio = weight / tonnage - 1;

                if (ratio > 0.025f)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Invalid weight ratio = {ratio * 100}% ({weight - tonnage}t)!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/over_weight/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                if (ratio < -0.15f)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Invalid weight ratio = {ratio * 100}% ({weight - tonnage}t)!");

                    var save = ship.ToStore();
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "bad/under_weight/", baseName + ".bindesign");
                    File.Move(f, path);

                    ship.Erase();
                    G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
                    continue;
                }

                Melon<TweaksAndFixes>.Logger.Msg($"  Valid ship!");

                {
                    var save = ship.ToStore();
                    save.tonnage = store.tonnage;
                    save.id = store.id;
                    var json = Util.SerializeObjectByte(save);
                    var baseName = Path.GetFileNameWithoutExtension(f);
                    var path = Path.Combine(Storage.prefix + "good/", baseName + ".bindesign");
                    File.WriteAllBytes(path, json);
                    File.Delete(f);
                }

                ship.Erase();
                G.GameData._sharedDesignsPerNation_k__BackingField.Clear();
            }

            GameManager.Quit();

            yield break;
        }



        // MelonCoroutines.Start(CheckDesigns());

        // var prefix = Storage.prefix + "to_test/";
        // Melon<TweaksAndFixes>.Logger.Msg($"{prefix}");
        // 
        // Patch_Part_CanPlaceGeneric.ForceCheck = true;
        // 
        // var files = Directory.GetFiles(prefix, "*.bindesign");
        // foreach (var f in files)
        // {
        //     // Melon<TweaksAndFixes>.Logger.Msg($"  {f}");
        // 
        //     foreach (var player in G.GameData.players)
        //     {
        //         if (player.Value.type != "major")
        //             continue;
        // 
        //         if (f.Contains(player.Value.nameUi)
        //             || f.Contains(player.Value.nameUiCommunism)
        //             || f.Contains(player.Value.nameUiFascism)
        //             || f.Contains(player.Value.nameUiMonarchy)
        //             || f.Contains(player.Value.nameUiOtherDemocracy))
        //         {
        //             Melon<TweaksAndFixes>.Logger.Msg($"  {f.Replace("to_test", $"sorted/{player.Value.nameUi}")}");
        // 
        //             if (!Directory.Exists(Path.GetDirectoryName(f).Replace("to_test", $"sorted/{player.Value.nameUi}")))
        //                 Directory.CreateDirectory(Path.GetDirectoryName(f).Replace("to_test", $"sorted/{player.Value.nameUi}"));
        //             File.Move(f, f.Replace("to_test", $"sorted/{player.Value.nameUi}"));
        // 
        //             break;
        //         }
        //     }
        // }

    }
}
