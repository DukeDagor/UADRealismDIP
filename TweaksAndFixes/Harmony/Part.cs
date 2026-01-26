using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Diagnostics;
using System.Reflection;

#pragma warning disable CS8603

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Part))]
    internal class Patch_Part
    {
        // ########## PART MIRRORING LOGIC ########## //

        // Matched mirrors
        public static Il2CppSystem.Collections.Generic.Dictionary<Part, Part> mirroredParts = new Il2CppSystem.Collections.Generic.Dictionary<Part, Part>();

        // From-To mirrored rotation. The placed part's rotation is applied to the mirrored part
        public static Il2CppSystem.Collections.Generic.Dictionary<Part, Part> applyMirrorFromTo = new Il2CppSystem.Collections.Generic.Dictionary<Part, Part>();

        // Uncentered parts with no mirror
        public static Il2CppSystem.Collections.Generic.List<Part> unmatchedParts = new Il2CppSystem.Collections.Generic.List<Part>();

        // Ignore one remove-part call. When the mirrored part's default rotation causes a collision problem, it gets deleted imediately.
        // We need to ignore one collision check so we can set the correct rotation value.
        public static Part TrySkipDestroy = null;

        [HarmonyPatch(nameof(Part.AutoRotatePart))]
        [HarmonyPrefix]
        internal static bool Prefix_AutoRotatePart(Part __instance, bool leftRight, bool forwardBack)
        {
            // Kill auto-rotation dead unless the AI is using it
            if (!Patch_Ui.UseNewConstructionLogic())
            {
                return true;
            }

            return false;
        }

        [HarmonyPatch(nameof(Part.AnimateRotate))]
        [HarmonyPrefix]
        internal static bool Prefix_AnimateRotatet(Part __instance, float angle)
        {
            if (!Patch_Ui.UseNewConstructionLogic())
            {
                return true;
            }

            if (__instance.Name().Contains("Dual Barbette for")) return false;

            // Ignore animated rotation values that don't match the new rotation incraments
            if (!ModUtils.NearlyEqual(Math.Abs(angle), Patch_Ui.RotationValue))
            {
                // Melon<TweaksAndFixes>.Logger.Warning("Does not equal rotation override: " + angle + " != " + Patch_Ui.RotationValue);
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Part.ShowAsTransparent))]
        [HarmonyPostfix]
        internal static void Postfix_ShowAsTransparent(Part __instance)
        {
            // Why in Gods name do they not store the currently active part *ANYWHERE*
            Patch_Ui.UpdateSelectedPart(__instance);
        }

        [HarmonyPatch(nameof(Part.Place))]
        [HarmonyPostfix]
        internal static void Postfix_Place(Part __instance, Vector3 pos, bool autoRotate = true)
        {
            if (!Patch_Ui.UseNewConstructionLogic())
            {
                return;
            }

            // They use the Part.Place function for moving the selected part. 
            if (__instance != Patch_Ui.SelectedPart)
            {
                // Melon<TweaksAndFixes>.Logger.Msg("New part: ");
                // Melon<TweaksAndFixes>.Logger.Msg("  " + __instance.Name());
                // Melon<TweaksAndFixes>.Logger.Msg("  " + __instance.transform.position.ToString());
                // Melon<TweaksAndFixes>.Logger.Msg("  " + __instance.transform.rotation.eulerAngles.ToString());

                // Ignore if it's centered
                if (pos.x == 0.0f)
                {
                    return;
                }

                // Melon<TweaksAndFixes>.Logger.Msg("Matching Parts:");

                // Find mirrored part
                Part placedPart = __instance;
                Part mirroredPart = null;

                foreach (Part part in ShipM.GetActiveShip().parts)
                {
                    if (part == null) continue;
                    if (part.transform == null) continue;
                    if (part == __instance) continue;
                    if (part == Patch_Ui.SelectedPart) continue;

                    Vector3 partPos = part.transform.position;
                    if (partPos.y != pos.y) continue;
                    if (partPos.z != pos.z) continue;

                    if (partPos == pos)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"Found duplicated part: {part.name}");

                        if (mirroredParts.ContainsKey(part))
                        {
                            if (mirroredParts.ContainsKey(mirroredParts[part]))
                            {
                                if (mirroredParts.ContainsKey(mirroredParts[part])) mirroredParts.Remove(mirroredParts[part]);
                                if (applyMirrorFromTo.ContainsKey(mirroredParts[part])) applyMirrorFromTo.Remove(mirroredParts[part]);
                            }

                            if (applyMirrorFromTo.ContainsKey(part)) applyMirrorFromTo.Remove(part);
                            if (mirroredParts.ContainsKey(part)) mirroredParts.Remove(part);
                        }

                        // ShipM.GetActiveShip().RemovePart(part);
                        return;
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg("Found part mirror");

                    mirroredPart = part;
                    break;
                }

                // If the mirrored part is found, add it to mirroring and register a skip
                if (mirroredPart != null)
                {
                    Vector3 partRot = mirroredPart.transform.eulerAngles;
                    placedPart.transform.eulerAngles = new Vector3(partRot.x, -partRot.y, partRot.z);

                    applyMirrorFromTo[mirroredPart] = placedPart;

                    TrySkipDestroy = placedPart;

                    // Melon<TweaksAndFixes>.Logger.Msg("Part mirrored successfully");
                }

                // Melon<TweaksAndFixes>.Logger.Msg("");
            }
        }

        public static Stopwatch stopWatchTotal = new Stopwatch();
        public static Stopwatch stopWatch = new Stopwatch();
        public static Dictionary<string, double> loadedModels = new();

        [HarmonyPatch(nameof(Part.LoadModel))]
        [HarmonyPrefix]
        internal static void Prefix_LoadModel(Part __instance)
        {
            // stopWatch.Restart();
            // stopWatchTotal.Start();

            // if (__instance.data.model != "(custom)")
            // {
            //     // Util.ResourcesLoad<GameObject>(__instance.data.model);
            //     if (!Util.resCache.ContainsKey(__instance.data.model)) Melon<TweaksAndFixes>.Logger.Msg($"Loaded: {__instance.data.model}");
            // }
        }

        [HarmonyPatch(nameof(Part.LoadModel))]
        [HarmonyPostfix]
        internal static void Postfix_LoadModel(Part __instance)
        {
            MountOverrideData.ApplyMountOverridesToPart(__instance);

            // Melon<TweaksAndFixes>.Logger.Msg($"Used: {__instance.model.name.Replace("(Clone)", "")}");
            // Melon<TweaksAndFixes>.Logger.Msg($"\n{ModUtils.DumpHierarchy(__instance.gameObject)}\n\n\n\n");

            // stopWatchTotal.Stop();
            // stopWatch.Stop();
            // if (!loadedModels.ContainsKey(__instance.model.name.Replace("(Clone)", ""))) loadedModels.Add(__instance.model.name.Replace("(Clone)", ""), stopWatch.Elapsed.TotalSeconds);
            // else loadedModels[__instance.model.name.Replace("(Clone)", "")] += stopWatch.Elapsed.TotalSeconds;
        }

        // [HarmonyPatch(nameof(Part.UnloadModel))]
        // [HarmonyPrefix]
        // internal static bool Prefix_UnloadModel(Part __instance)
        // {
        //     // Melon<TweaksAndFixes>.Logger.Msg($"Unloaded: {__instance.data.model}");
        // 
        //     return false;
        // }



        [HarmonyPatch(nameof(Part.GunBarrelLength))]
        [HarmonyPrefix]
        internal static bool Prefix_GunBarrelLength(PartData data, Ship ship, bool update, ref string __result)
        {
            if (Config.Param("taf_guns_direct_assign_caliber_length_modifier", 0) == 0)
            {
                return true;
            }

            Ship.TurretCaliber? cal = null;

            if (ship == null || !data.isGun)
            {
                __result = "/??";
                return false;
            }

            foreach (var caliber in ship.shipGunCaliber)
            {
                if (caliber == null) continue;

                var calPart = caliber.turretPartData;

                if (calPart.caliber != data.caliber) continue;

                if (Ship.IsCasemateGun(calPart) != Ship.IsCasemateGun(data)) continue;

                cal = caliber;
                break;
            }

            if (cal == null)
            {
                return true;
            }

            float caliberLength = -1;

            foreach (var part in ship.parts)
            {
                if (part == null) continue;

                if (part.data.caliber != data.caliber) continue;

                if (part.data.barrels != data.barrels) continue;

                if (Ship.IsCasemateGun(part.data) != Ship.IsCasemateGun(data)) continue;

                if (part.barrelLength == -1)
                {
                    part.UpdateCollidersSize(ship);
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"{part.key.modelKey.modelName} : {part.key.modelKey.caliberLengthModifier} * {cal.length} * {cal.diameter} = {ModUtils.toInt(part.key.modelKey.caliberLengthModifier * cal.length)}");
                part.caliberLength = ModUtils.toInt((float)Math.Round(part.key.modelKey.caliberLengthModifier * (cal.length / 100.0f + 1)));
                caliberLength = part.caliberLength;
            }

            __result = $"/{caliberLength}";

            return false;
        }



        // ########## Fixes by Crux10086 ########## //

        [HarmonyPatch(nameof(Part.UpdatePartScale))]
        [HarmonyPrefix]
        public static void Prefix_UpdatePartScale(Part __instance, Ship ship)
        {
            if (ship == null || ship.shipGunCaliber == null || !__instance.data.isGun || __instance.model == null)
            {
                return;
            }

            if (__instance.barrelModels != null && __instance.barrelModels.Count != 0)
            {
                bool flag = false;
                var barrelModels = __instance.barrelModels;
                foreach (var barrelModel in barrelModels)
                {
                    if (barrelModel == null)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    return;
                }
            }
            
            __instance.barrelModels = new Il2CppSystem.Collections.Generic.List<MeshRenderer>();
            Transform child = __instance.model.transform.GetChild(0);
            int childCount = child.GetChildCount();
            
            for (int i = 0; i < childCount; i++)
            {
                var subChildren = child.GetChild(i).GetChildren();
                foreach (var subChild in subChildren)
                {
                    if (subChild.transform.name.Contains("barrel"))
                    {
                        __instance.barrelModels.Add(subChild.gameObject.GetComponent<MeshRenderer>());
                    }
                }
            }
            
            if (__instance.barrelModels.Count == 0)
            {
                __instance.barrelModels.Add(child.GetChild(0).gameObject.GetComponent<MeshRenderer>());
            }
        }





        // ########## Fire Angle Overrides ########## //

        private static void OverrideFiringAngle(Part __instance, ref Part.FireSectorInfo fireSector)
        {
            bool hasBreak = false;

            float startAngle = __instance.mount.transform.eulerAngles.y + __instance.mount.angleLeft;

            if (startAngle < 0)
            {
                startAngle += 360;
                hasBreak = true;
            }

            float endAngle = __instance.mount.transform.eulerAngles.y + __instance.mount.angleRight;

            if (endAngle > 360)
            {
                endAngle -= 360;
                hasBreak = true;
            }

            fireSector.groupsAll.Clear();
            fireSector.groupsShoot.Clear();

            if (!hasBreak)
            {
                Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> badGroupA = new();
                Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> shootGroup = new();
                Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> badGroupB = new();

                bool foundShoot = false;

                foreach (var sector in fireSector.steps)
                {
                    if (sector.Key > startAngle && sector.Key < endAngle)
                    {
                        sector.Value.status = Part.SectorStep.Status.Shoot;

                        shootGroup.Add(sector.Value);
                        foundShoot = true;
                    }
                    else
                    {
                        sector.Value.status = Part.SectorStep.Status.Bad;

                        if (!foundShoot)
                        {
                            badGroupA.Add(sector.Value);
                        }
                        else
                        {
                            badGroupB.Add(sector.Value);
                        }
                    }
                }

                fireSector.shootableAngleTotal = endAngle - startAngle;

                if (badGroupA.Count > 0) fireSector.groupsAll.Add(badGroupA);
                fireSector.groupsAll.Add(shootGroup);
                if (badGroupB.Count > 0) fireSector.groupsAll.Add(badGroupB);

                fireSector.groupsShoot.Add(shootGroup);
            }
            else
            {
                Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> shootGroupA = new();
                Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> badGroup = new();
                Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> shootGroupB = new();

                bool foundBad = false;

                foreach (var sector in fireSector.steps)
                {
                    if ((sector.Key > startAngle && sector.Key <= 360) || (sector.Key < endAngle && sector.Key >= 0))
                    {
                        sector.Value.status = Part.SectorStep.Status.Shoot;

                        if (!foundBad)
                        {
                            shootGroupA.Add(sector.Value);
                        }
                        else
                        {
                            shootGroupB.Add(sector.Value);
                        }
                    }
                    else
                    {
                        sector.Value.status = Part.SectorStep.Status.Bad;

                        badGroup.Add(sector.Value);
                        foundBad = true;
                    }
                }

                fireSector.shootableAngleTotal = (endAngle) + (360 - startAngle);

                if (shootGroupA.Count > 0) fireSector.groupsAll.Add(shootGroupA);
                fireSector.groupsAll.Add(badGroup);
                if (shootGroupB.Count > 0) fireSector.groupsAll.Add(shootGroupB);

                if (shootGroupA.Count > 0) fireSector.groupsShoot.Add(shootGroupA);
                if (shootGroupB.Count > 0) fireSector.groupsShoot.Add(shootGroupB);
            }
        }

        private static void MergeFiringAngle(Part __instance, ref Part.FireSectorInfo fireSector)
        {
            bool hasBreak = false;

            float startAngle = __instance.mount.transform.eulerAngles.y + __instance.mount.angleLeft;

            if (startAngle < 0)
            {
                startAngle += 360;
                hasBreak = true;
            }

            float endAngle = __instance.mount.transform.eulerAngles.y + __instance.mount.angleRight;

            if (endAngle > 360)
            {
                endAngle -= 360;
                hasBreak = true;
            }

            fireSector.groupsAll.Clear();
            fireSector.groupsShoot.Clear();
            fireSector.shootableAngleTotal = 0;

            if (!hasBreak)
            {
                foreach (var sector in fireSector.steps)
                {
                    if (sector.Key <= startAngle || sector.Key >= endAngle)
                    {
                        sector.Value.status = Part.SectorStep.Status.Bad;
                    }
                }
            }
            else
            {
                foreach (var sector in fireSector.steps)
                {
                    if (!(sector.Key > startAngle && sector.Key <= 360) && !(sector.Key < endAngle && sector.Key >= 0))
                    {
                        sector.Value.status = Part.SectorStep.Status.Bad;
                    }
                }
            }

            Il2CppSystem.Collections.Generic.HashSet<Part.SectorStep> group = new();
            Part.SectorStep lastSector = new();
            float groupStartAngle = 0;
            float lastSectorAngle = 0;

            foreach (var sector in fireSector.steps)
            {
                if (group.Count != 0 && lastSector.status != sector.Value.status)
                {
                    fireSector.groupsAll.Add(group);

                    if (lastSector.status == Part.SectorStep.Status.Shoot)
                    {
                        fireSector.groupsShoot.Add(group);
                        fireSector.shootableAngleTotal += lastSectorAngle - groupStartAngle + fireSector.stepAngle;
                    }

                    groupStartAngle = sector.Key;
                    group = new();
                }

                group.Add(sector.Value);
                lastSectorAngle = sector.Key;
                lastSector = sector.Value;
            }

            fireSector.groupsAll.Add(group);

            if (lastSector.status == Part.SectorStep.Status.Shoot)
            {
                fireSector.groupsShoot.Add(group);
                fireSector.shootableAngleTotal += lastSectorAngle - groupStartAngle + fireSector.stepAngle;
            }
        }

        public class Il2CppList<T> : Il2CppSystem.Collections.Generic.List<T> {}

        private static Il2CppList<Part> omitted_parts = new();

        private static void CollectBigGunIgnoreSmallGun(Part bigGun)
        {
            if (bigGun.data.type != "gun") return;

            float ratio = Config.Param("taf_large_gun_ignore_small_gun_ratio", 0.25f);
            float skipableCaliber = bigGun.data.GetCaliberInch(bigGun.ship) * ratio + 0.05f;

            if (skipableCaliber < 2) return;

            // Melon<TweaksAndFixes>.Logger.Msg($"Gun ({__instance.data.GetCaliberInch(__instance.ship)} > 12) {__instance.name}");

            foreach (Part part in bigGun.ship.parts)
            {
                if (omitted_parts.Contains(part)) continue;

                if (part.data.type != "gun") continue;

                if (part.data.GetCaliberInch(bigGun.ship) > skipableCaliber) continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Omitting gun ({part.data.GetCaliberInch(__instance.ship)} < 4) {part.name}");

                omitted_parts.Add(part);
            }
        }

        private static void CollectBigGunIgnoreTorpedoTubes(Part bigGun)
        {
            if (bigGun.data.type != "gun") return;

            float ratio = Config.Param("taf_large_gun_ignore_torpedo_tubes", 4);

            if (ratio < 2) return;

            // Melon<TweaksAndFixes>.Logger.Msg($"Gun ({__instance.data.GetCaliberInch(__instance.ship)} > 12) {__instance.name}");

            foreach (Part part in bigGun.ship.parts)
            {
                if (omitted_parts.Contains(part)) continue;

                if (part.data.type != "torpedo") continue;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Omitting gun ({part.data.GetCaliberInch(__instance.ship)} < 4) {part.name}");

                // Melon<TweaksAndFixes>.Logger.Msg($"  Omitting gun ({part.data.GetCaliberInch(bigGun.ship)} < {ratio}) {part.name}");

                omitted_parts.Add(part);
            }
        }

        private static void CollectTorpedoTubesIgnoreBigGun(Part torpedoTube)
        {
            if (torpedoTube.data.type != "torpedo") return;

            float ratio = Config.Param("taf_torpedo_tubes_ignore_large_gun", 5);

            if (ratio < 2) return;

            // Melon<TweaksAndFixes>.Logger.Msg($"Gun ({__instance.data.GetCaliberInch(__instance.ship)} > 12) {__instance.name}");

            foreach (Part part in torpedoTube.ship.parts)
            {
                if (omitted_parts.Contains(part)) continue;

                if (part.data.type != "gun") continue;

                if (part.data.GetCaliberInch(torpedoTube.ship) + 0.01f < ratio) continue;

                omitted_parts.Add(part);
            }
        }

        [HarmonyPatch(nameof(Part.CalcFireSectorNonAlloc))]
        [HarmonyPrefix]
        internal static void Prefix_CalcFireSectorNonAlloc(Part __instance)
        {
            if (Config.Param("taf_large_gun_ignore_small_gun_enable", 1) != 1) return;

            CollectBigGunIgnoreSmallGun(__instance);
            CollectBigGunIgnoreTorpedoTubes(__instance);
            CollectTorpedoTubesIgnoreBigGun(__instance);

            foreach (Part part in omitted_parts)
            {
                part.transform.position += new Vector3(1000, 1000, 1000);
            }
        }

        [HarmonyPatch(nameof(Part.CalcFireSectorNonAlloc))]
        [HarmonyPostfix]
        internal static void Postfix_CalcFireSectorNonAlloc(Part __instance, ref Part.FireSectorInfo fireSector)
        {
            foreach (Part part in omitted_parts)
            {
                part.transform.position -= new Vector3(1000, 1000, 1000);

                if (part.mount != null)
                {
                    part.transform.position = part.mount.transform.position;
                }
            }

            omitted_parts.Clear();

            if (__instance.mount == null) return;

            if ((int)__instance.mount.angleRight == 0 && (int)__instance.mount.angleLeft == 0) return;
            
            if (__instance.mount.ignoreExpand && __instance.mount.ignoreParent)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Overriding fire angle... Start: {fireSector.shootableAngleTotal}");
                OverrideFiringAngle(__instance, ref fireSector);
            }
            else
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Merging fire angle... Start: {fireSector.shootableAngleTotal}");
                MergeFiringAngle(__instance, ref fireSector);
            }


            // Melon<TweaksAndFixes>.Logger.Msg($"  {__instance.Name()}.Mount.Total Angle: {fireSector.shootableAngleTotal}");
        }

        public static float GetMountMinParam(Part parent)
        {
            float min = -1;

            if (parent.data.paramx.ContainsKey("mount_min"))
            {
                if (parent.data.paramx["mount_min"].Count == 0)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {parent.data.name}.");
                }

                else if (!float.TryParse(parent.data.paramx["mount_min"][0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out min))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {parent.data.name}.");
                }
            }

            return min;
        }

        public static float GetMountMaxParam(Part parent)
        {
            float max = -1;

            if (parent.data.paramx.ContainsKey("mount_max"))
            {
                if (parent.data.paramx["mount_max"].Count == 0)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {parent.data.name}.");
                }

                else if (!float.TryParse(parent.data.paramx["mount_max"][0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out max))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {parent.data.name}.");
                }
            }

            return max;
        }

        public static float GetMountMultParam(Part parent)
        {
            float mult = -1;

            if (parent.data.paramx.ContainsKey("mount_mult"))
            {
                if (parent.data.paramx["mount_mult"].Count == 0)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {parent.data.name}.");
                }

                else if (!float.TryParse(parent.data.paramx["mount_mult"][0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out mult))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to parse {parent.data.name}.");
                }
            }

            return mult;
        }

        // ########## MODIFIED MOUNT LOGIC ########## //

        //private static string? _MountErrorLoc = null;
        internal static bool _IgnoreNextActiveBad = false;

        [HarmonyPatch(nameof(Part.SetVisualMode))]
        [HarmonyPrefix]
        internal static void Prefix_SetVisualMode(Part __instance, ref Part.VisualMode m)
        {
            if (m == Part.VisualMode.ActiveBad && _IgnoreNextActiveBad) //Patch_Ui._InUpdateConstructor && ((Patch_Ui_c._SetBackToBarbette && __instance.data == Patch_Ui_c._BarbetteData) || __instance.data.isBarbette))
            {
                _IgnoreNextActiveBad = false;
                //if (_MountErrorLoc == null)
                //    _MountErrorLoc = LocalizeManager.Localize("$Ui_Constr_MustPlaceOnMount");

                //if (G.ui.constructorCentralText2.text.Contains(_MountErrorLoc) || G.ui.constructorCentralText2.text == "mount1")
                if (Part.CanPlaceGeneric(__instance.data, __instance.ship == null ? G.ui.mainShip : __instance.ship, true, out _) && !__instance.CanPlace(out var deny) && (deny == "mount 1" || deny == "mount1"))
                {
                    m = Part.VisualMode.Highlight;
                    //if (/*(__instance.data.isWeapon || __instance.data.isBarbette) &&*/ G.ui.placingPart == __instance)
                    //{
                    if (!Util.FocusIsInInputField())
                    {
                        if (GameManager.CanHandleKeyboardInput())
                        {
                            var b = G.settings.Bindings;
                            float angle = UnityEngine.Input.GetKeyDown(b.RotatePartLeft.Code) ? -45f :
                                UnityEngine.Input.GetKeyDown(b.RotatePartRight.Code) ? 45f : 0f;
                            if (angle != 0f)
                            {
                                __instance.transform.Rotate(Vector3.up, angle);
                                __instance.AnimateRotate(angle);
                                G.ui.OnConShipChanged(false);
                            }
                        }
                    }
                    //}
                }
            }
        }

        //[HarmonyPatch(nameof(Part.TryFindMount))]
        //[HarmonyPostfix]
        internal static void Postfix_TryFindMount(Part __instance, bool autoRotate)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Called TryFindMount on {__instance.name} ({__instance.data.name}) {(__instance.mount != null ? "Mounted" : string.Empty)}");
            if (!__instance.CanPlace(out string denyReason))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Can't place. Deny reason {(denyReason == null ? "<null>" : denyReason)}");
            }
        }
        //[HarmonyPatch(nameof(Part.Mount))]
        //[HarmonyPostfix]
        internal static void Postfix_Mount(Part __instance, Mount mount)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Mounting part {__instance.name} to {(mount == null ? "<<nothing>>" : (mount.parentPart == null ? (mount.name + " (no parent)") : (mount.name + " on " + mount.parentPart.name)))}");
        }
    }

    // We can't target ref arguments in an attribute, so
    // we have to make this separate class to patch with a
    // TargetMethod call.
    [HarmonyPatch(typeof(Part))]
    internal class Patch_Part_CanPlaceGeneric
    {
        internal static MethodBase TargetMethod()
        {
            //return AccessTools.Method(typeof(Part), nameof(Part.CanPlace), new Type[] { typeof(string).MakeByRefType(), typeof(List<Part>).MakeByRefType(), typeof(List<Collider>).MakeByRefType() });

            // Do this manually
            var methods = AccessTools.GetDeclaredMethods(typeof(Part));
            foreach (var m in methods)
            {
                if (m.Name != nameof(Part.CanPlaceGeneric))
                    continue;
    
                return m;
            }

            return null;
        }
    
        // TODO: Actually check FCAP ratios to better match the desired FCAP.
        internal static bool Prefix(Part __instance, PartData data, Ship ship, bool partIsReal, ref string denyReason, ref bool __result)
        {
            if (!GameManager.IsAutodesignActive) return true;

            if (data.isGun)
            {
                float max_main_gun_cal = -1;
                float min_main_gun_cal = -1;
                float max_main_gun_barrels = -1;
                float min_main_gun_barrels = -1;
                float max_main_gun_count = -1;
                float max_sec_gun_count = -1;

                var st = ship.shipType;

                if (st.paramx.ContainsKey("shipgen_limit"))
                {
                    foreach (var stat in st.paramx["shipgen_limit"])
                    {
                        var split = stat.Split(':');

                        if (split.Length != 2)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `shipTypes.csv` `shipgen_limit` param: `{stat}` for ID `{st.name}`. Must be formatted `shipgen_limit(stat:number; stat:number; ...)`.");
                            continue;
                        }

                        string tag = split[0];

                        if (!float.TryParse(split[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out float val))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `shipTypes.csv` `shipgen_limit` param: `{stat}` for ID `{st.name}`. Must be valid number.");
                            continue;
                        }

                        switch (tag)
                        {
                            case "max_main_gun_cal": max_main_gun_cal = val; break;
                            case "min_main_gun_cal": min_main_gun_cal = val; break;
                            case "max_main_gun_barrels": max_main_gun_barrels = val; break;
                            case "min_main_gun_barrels": min_main_gun_barrels = val; break;
                            case "max_main_gun_count": max_main_gun_count = val; break;
                            case "max_sec_gun_count": max_sec_gun_count = val; break;
                            default:
                                Melon<TweaksAndFixes>.Logger.Error($"Invalid `shipTypes.csv` `shipgen_limit` param: `{stat}` for ID `{st.name}`. Unsuported stat. Can only be [max_main_gun_cal, min_main_gun_cal, max_main_gun_barrels, min_main_gun_barrels, max_main_gun_count, max_sec_gun_count]");
                                break;
                        }
                    }
                }

                var hd = ship.hull.data;

                if (hd.paramx.ContainsKey("shipgen_limit"))
                {
                    foreach (var stat in hd.paramx["shipgen_limit"])
                    {
                        var split = stat.Split(':');

                        if (split.Length != 2)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_limit` param: `{stat}` for ID `{hd.name}`. Must be formatted `shipgen_limit(stat:number; stat:number; ...)`.");
                            continue;
                        }

                        string tag = split[0];

                        if (!float.TryParse(split[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out float val))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_limit` param: `{stat}` for ID `{hd.name}`. Must be valid number.");
                            continue;
                        }

                        switch (tag)
                        {
                            case "max_main_gun_cal": max_main_gun_cal = val; break;
                            case "min_main_gun_cal": min_main_gun_cal = val; break;
                            case "max_main_gun_barrels": max_main_gun_barrels = val; break;
                            case "min_main_gun_barrels": min_main_gun_barrels = val; break;
                            case "max_main_gun_count": max_main_gun_count = val; break;
                            case "max_sec_gun_count": max_sec_gun_count = val; break;
                            default:
                                Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_limit` param: `{stat}` for ID `{hd.name}`. Unsuported stat. Can only be [max_main_gun_cal, min_main_gun_cal, max_main_gun_barrels, min_main_gun_barrels, max_main_gun_count, max_sec_gun_count]");
                                break;
                        }
                    }
                }

                if (ship.IsMainCal(data))
                {
                    if ((max_main_gun_cal != -1 && data.GetCaliberInch(ship) > max_main_gun_cal)
                        || (min_main_gun_cal != -1 && data.GetCaliberInch(ship) < min_main_gun_cal))
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"Gun cal size {data.GetCaliberInch(ship)} outside range {min_main_gun_cal} ~ {max_main_gun_cal}");
                        __result = false;
                        denyReason = "size";
                        return false;
                    }

                    if ((max_main_gun_barrels != -1 && data.barrels > max_main_gun_barrels)
                        || (min_main_gun_barrels != -1 && data.barrels < min_main_gun_barrels))
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"Gun barrel cnt {data.barrels} outside range {min_main_gun_barrels} ~ {max_main_gun_barrels}");
                        __result = false;
                        denyReason = "barrel count";
                        return false;
                    }

                    if ((max_main_gun_count != -1 && ship.mainGuns.Count > max_main_gun_count))
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"Main gun cnt {ship.mainGuns.Count} outside range N/A ~ {max_main_gun_count}");
                        __result = false;
                        denyReason = "count";
                        return false;
                    }
                }
                else if (!Ship.IsCasemateGun(data))
                {
                    if (max_sec_gun_count != -1)
                    {
                        int gunCounts = new();

                        foreach (var part in ship.parts)
                        {
                            if (ship.IsMainCal(part.data)) continue;

                            if (part.data != data) continue;

                            gunCounts++;
                        }

                        if (gunCounts > max_sec_gun_count)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"Sec gun cnt {gunCounts} outside range N/A ~ {max_sec_gun_count}");
                            __result = false;
                            denyReason = "count";
                            return false;
                        }
                    }
                }
            }

            if (data.isFunnel)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"CanPlaceGeneric: {data.nameUi}! ({__instance?.Name() ?? "NULL"})");

                if (!ship.statsValid)
                    ship.CStats();

                var eff = ship.stats.GetValueOrDefault(G.GameData.stats["smoke_exhaust"]);

                if (eff == null)
                    return true;

                if (eff.total < Config.Param("taf_shipgen_target_funnel_cap", 150f))
                    return true;

                // Melon<TweaksAndFixes>.Logger.Msg($"  FCap {eff.total} >= {Config.Param("taf_shipgen_target_funnel_cap", 150f)}");

                int count = 0;
                float thisFunnelCap = 0;
                float total = 0;

                foreach (var part in ship.parts)
                {
                    if (!part.data.isFunnel) continue;

                    count++;

                    total += part.data.statsx[G.GameData.stats["fcap"]];

                    if (part.data != data) continue;

                    thisFunnelCap = part.data.statsx[G.GameData.stats["fcap"]];

                    // Melon<TweaksAndFixes>.Logger.Msg($"    {part.data.nameUi} -> {part.data.statsx[G.GameData.stats["fcap"]]} fcap");
                }

                if (thisFunnelCap == 0)
                    return true;

                // Melon<TweaksAndFixes>.Logger.Msg($"  {count} funnels : {total} fCap stat");

                if (count <= 1)
                    return true;

                if (eff.total * (1f - (thisFunnelCap / total)) < Config.Param("taf_shipgen_target_funnel_cap", 150f))
                    return true;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Denying mounting of {data.nameUi}!");

                __result = false;
                denyReason = "amount";
                return false;
            }

            return true;
        }
    }

    // We can't target ref arguments in an attribute, so
    // we have to make this separate class to patch with a
    // TargetMethod call.
    // [HarmonyPatch(typeof(Part))]
    // internal class Patch_Part_CanPlace
    // {
    //     internal static MethodBase TargetMethod()
    //     {
    //         //return AccessTools.Method(typeof(Part), nameof(Part.CanPlace), new Type[] { typeof(string).MakeByRefType(), typeof(List<Part>).MakeByRefType(), typeof(List<Collider>).MakeByRefType() });
    // 
    //         // Do this manually
    //         var methods = AccessTools.GetDeclaredMethods(typeof(Part));
    //         foreach (var m in methods)
    //         {
    //             if (m.Name != nameof(Part.CanPlace))
    //                 continue;
    // 
    //             if (m.GetParameters().Length == 3)
    //                 return m;
    //         }
    // 
    //         return null;
    //     }
    // 
    //     internal static bool Prefix(Part __instance, ref bool __result) //, out List<Part> overlapParts, out List<Collider> overlapBorders)
    //     {
    //         if (__instance == null)
    //         {
    //             Melon<TweaksAndFixes>.Logger.Msg("Skipping check can place!");
    //             __result = false;
    //             return false;
    //         }
    //         return true;
    //         // We could try to be fancier, but let's just clobber.
    //         // Note we won't necessarily be in the midst of the barbette patch, so
    //         // we can't rely on checking that. But it's possible the reset failed,
    //         // so we take the setback case too.
    //         //if (Patch_Ui._InUpdateConstructor && ((Patch_Ui_c._SetBackToBarbette && __instance.data == Patch_Ui_c._BarbetteData) || __instance.data.isBarbette))
    //         //{
    //         //    if (denyReason == "mount1")
    //         //        __result = true;
    //         //}
    // 
    //     }
    // }
}
