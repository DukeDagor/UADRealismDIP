using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Reflection;
using TweaksAndFixes.Data;
using static Il2Cpp.Ship;
using UnityEngine.UI;
using TweaksAndFixes.Harmony;

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
            // Melon<TweaksAndFixes>.Logger.Msg($"GetRefitYearNameEnd: {(__instance.designShipForRefit == null ? "NULL" : __instance.designShipForRefit.Name(false, false))}");
            // Melon<TweaksAndFixes>.Logger.Msg($"                     `{__instance.Name(true, false, false, false, false)}` `{__instance.Name(false, false, false, false, true)}` `{__instance.Name(true, false, false, false, true)}`");
            // Melon<TweaksAndFixes>.Logger.Msg($"                     Original `{__result}`");

            string prefix = __result.Contains(__instance.Name(false, false, false, false, true)) ? $"{__instance.Name(false, false, false, false, true)}" : "";

            // string prefix = __result[^1] != '2' ? $"{__result.Substring(0, __result.LastIndexOf('(') - 1)}" : "";

            __result = $"{prefix} ({ModUtils.NumToMonth(CampaignController.Instance.CurrentDate.AsDate().Month)}. {CampaignController.Instance.CurrentDate.AsDate().Year})";
            // Melon<TweaksAndFixes>.Logger.Msg($"                     New      `{__result}`");
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

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.CStats))]
        internal static void Postfix_CStats(Ship __instance)
        {
            if (__instance.stats_.ContainsKey(G.GameData.stats["floatability"]))
            {
                var stat = __instance.stats_[G.GameData.stats["floatability"]];

                // Melon<TweaksAndFixes>.Logger.Msg($"floatability:");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.basic}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.misc}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.tech}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.modifiers}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.total}");

                if (stat.total > Config.Param("taf_ship_stat_floatability_cap", 140f))
                {
                    stat.basic = Config.Param("taf_ship_stat_floatability_cap", 140f) - stat.modifiers;
                }
            }

            if (__instance.stats_.ContainsKey(G.GameData.stats["endurance"]))
            {
                var stat = __instance.stats_[G.GameData.stats["endurance"]];

                // Melon<TweaksAndFixes>.Logger.Msg($"endurance:");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.basic}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.misc}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.tech}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.modifiers}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {stat.total}");

                if (stat.total > Config.Param("taf_ship_stat_endurance_cap", 175f))
                {
                    stat.basic = Config.Param("taf_ship_stat_endurance_cap", 175f) - stat.modifiers;
                }
            }
        }


        // PartMats

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.PartMats))]
        internal static void Prefix_PartMats(Ship __instance, ref Il2CppSystem.Collections.Generic.List<MatInfo> __result)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"Costs:");

            foreach (var mat in __result)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  {mat.name} : {mat.cost}");

                if (__instance.shipType.paramx.ContainsKey(mat.name))
                {
                    foreach (var mod in __instance.shipType.paramx[mat.name])
                    {
                        var split = mod.Split(':');

                        if (split.Length != 2)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid cost modifier param for `{mat.name}`: `{mod}`. Invalid format. Should be name(cost:#;weight:#) or name(cost:#) or name(weight:#).");
                        }

                        string type = split[0];
                        string numRaw = split[1];

                        if (!float.TryParse(numRaw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out float mult))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid cost modifier param for `{mat.name}`: `{numRaw}`. Invalid number.");
                        }

                        if (type == "cost")
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"    {mat.cost} -> {mat.cost * mult}");
                            mat.cost *= mult;
                        }
                        else if (type == "weight")
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"    {mat.weight} -> {mat.weight * mult}");
                            mat.weight *= mult;
                        }
                    }
                }
            }
        }


        // ########## Ship Scuttling ########## //

        // CheckForSurrender

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.CheckForSurrender))]
        internal static bool Prefix_CheckForSurrender(Ship __instance, bool force)
        {
            float sinkThreashold = Config.Param("crew_percents_surrender_threshold", 0.3f);

            if (sinkThreashold <= __instance.CrewPercents && !force)
            {
                return true;
            }

            if (__instance.shipType.param.Contains("surrenders"))
            {
                Patch_Ui.replaceReportImportant = ModUtils.LocalizeF("$TAF_Ui_ReportShipSurrendered", __instance.Name());
                return true;
            }

            Patch_Ui.replaceReportImportant = ModUtils.LocalizeF("$TAF_Ui_ReportShipScuttled", __instance.Name());
            __instance.Sink("flooding");

            MelonCoroutines.Start(ExplodeCharges(
                __instance,
                BattleManager.Instance.CurrentBattle.Timer.leftTime,
                (float)System.Random.Shared.NextDouble() * 60 + 30
            ));

            // Effect.WaterSplash(10, __instance.gameObject.transform.position, new Quaternion());

            // Melon<TweaksAndFixes>.Logger.Msg($"Create Torp:");

            // Melon<TweaksAndFixes>.Logger.Msg($"Done:");

            return false;
        }

        private static bool IsBattleEnd(Ship ship)
        {
            return ship == null;
        }

        internal static System.Collections.IEnumerator ExplodeCharges(Ship ship, float startTime, float durration)
        {
            bool reportedWillScuttle = false;

            while (!IsBattleEnd(ship) && BattleManager.Instance.CurrentBattle.Timer.leftTime > startTime - durration)
            {
                if (!reportedWillScuttle && BattleManager.Instance.CurrentBattle.Timer.leftTime < startTime - durration + 5)
                {
                    G.ui.ReportImportant(ModUtils.LocalizeF("$TAF_Ui_ReportShipScuttling", ship.Name()), ship);
                    reportedWillScuttle = true;
                }

                yield return new WaitForSeconds(1);
            }

            if (IsBattleEnd(ship)) goto EXIT;

            Melon<TweaksAndFixes>.Logger.Msg($"Beginning scuttle effect for ship {ship.Name(false, false)}...");

            var mockPart = new Part();
            mockPart.data = G.GameData.parts["torpedo_x0"];
            mockPart.ship = ship;
            mockPart._ship_k__BackingField = ship;

            var sectionsGo = ModUtils.FindDeepChild(ship.hull.gameObject, "Sections"); // ship.hull.gameObject.GetChildren()[0].GetChildren()[0].GetChild("Sections");

            if (sectionsGo == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Error! Could not find `Sections` game object in ship! Aborting charge effect!\n{ModUtils.DumpHierarchy(ship.hull.gameObject)}");
                goto EXIT;
            }

            float foreZ = 0;
            float rearZ = 0;

            foreach (var section in sectionsGo.GetChildren())
            {
                if (!section.active) continue;

                foreach (var mesh in section.transform.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!mesh.gameObject.name.ToLower().Contains("hull")) continue;

                    float lengthZ = mesh.bounds.size.z;

                    if (section.transform.localPosition.z + lengthZ > foreZ)
                    {
                        foreZ = section.transform.localPosition.z + lengthZ;
                    }

                    if (section.transform.localPosition.z - lengthZ < rearZ)
                    {
                        rearZ = section.transform.localPosition.z - lengthZ;
                    }
                }
            }

            float lastCharge = 0;

            for (float i = 0.25f; i < 1f; i += 0.25f)
            {
                lastCharge = BattleManager.Instance.CurrentBattle.Timer.leftTime;

                var leftCharge = Torpedo.Create(
                    mockPart, ship.gameObject.transform.position,
                    Vector3.right, 9999999, 0
                );
                leftCharge.gameObject.SetParent(sectionsGo);

                leftCharge.transform.localPosition = new Vector3(-ship.collision.radius, 0, Mathf.Lerp(foreZ, rearZ, i));// i * ship.collision.height / 4);
                leftCharge.torpedoEffectScale = ship.collision.height / 100f + (float)(System.Random.Shared.NextDouble() - 0.5);
                leftCharge.Explode();

                leftCharge.transform.localPosition = new Vector3(ship.collision.radius, 0, leftCharge.transform.localPosition.z);// i * ship.collision.height / 4);
                leftCharge.torpedoEffectScale = ship.collision.height / 100f + (float)(System.Random.Shared.NextDouble() - 0.5);
                leftCharge.Explode();

                leftCharge.RemoveSelf();

                while (!IsBattleEnd(ship) && BattleManager.Instance.CurrentBattle.Timer.leftTime > lastCharge - 2)
                {
                    yield return new WaitForSeconds(0.25f);
                }

                if (IsBattleEnd(ship)) goto EXIT;
            }

            List<Part> guns = new();

            foreach (var part in ship.mainGuns)
            {
                guns.Add(part);
            }

            foreach (var part in ship.parts)
            {
                if (!part.data.isGun || ship.mainGuns.Contains(part)) continue;

                guns.Add(part);
            }

            float lastFlash = 0;
            float flashDurration = 0;

            foreach (var gun in guns)
            {
                lastFlash = BattleManager.Instance.CurrentBattle.Timer.leftTime;
                flashDurration = (float)System.Random.Shared.NextDouble() * 2.5f + 2.5f;

                ship.StartFire(ship.GetSectionFromPositions(gun.transform.position), gun.transform.position);

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                ship.AddSound("flash_fire", Vector3.zero, null, false);
                Effect.FlashFire(gun, 0);

                while (!IsBattleEnd(ship) && BattleManager.Instance.CurrentBattle.Timer.leftTime > lastFlash - flashDurration)
                {
                    yield return new WaitForSeconds(0.25f);
                }

                if (IsBattleEnd(ship)) goto EXIT;
            }

        EXIT:
            yield break;
        }

        // ########## Fixes by Crux10086 ########## //

        // Fix for broken deck hits

        // public static float percentDeck = 0.1f;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.GetSectionFromPositions))]
        internal static void Fix(Ship __instance, ref Vector3 tempPos)
        {
            if (Patch_Shell.updating == null)
            {
                return;
            }

            Vector3 startPos = Patch_Shell.shellTargetData[Patch_Shell.updating];
            Vector3 endPos = Patch_Shell.updating.transform.position;

            float distance = ModUtils.distance(startPos, endPos);

            // Should be impossible, but you never know
            if (distance <= 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Error: Invalid shell distance!");
                return;
            }

            float range = Patch_Shell.updating.from.ship.weaponRangesCache.GetValueOrDefault(Patch_Shell.updating.from.data);

            // Should be impossible, but you never know
            if (range <= 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Error: Invalid shell range!");
                return;
            }

            // Melon<TweaksAndFixes>.Logger.Msg($"{startPos} -> {endPos} = {distance} / {range} [AP = {Patch_Shell.updating.from.ship.weaponRangesAPCache.GetValueOrDefault(Patch_Shell.updating.from.data)}, HE = {Patch_Shell.updating.from.ship.weaponRangesHECache.GetValueOrDefault(Patch_Shell.updating.from.data)}");

            float percentDeckModifier = distance / range;

            int mark = Patch_Shell.updating.from.ship.TechGunGrade(Patch_Shell.updating.from.data);

            float min = Config.Param("taf_shell_deck_hit_percent_min", 0f);
            float max = Config.Param("taf_shell_deck_hit_percent_max", 1.2f);

            float deckPercent = ((max - min) * (percentDeckModifier * ((float)mark / (float)Config.MaxGunGrade))) + min;

            // Melon<TweaksAndFixes>.Logger.Msg($"{distance/1000:N2}km / {range/1000:N2}km = {percentDeckModifier * 100:N2}% | {mark} / {Config.MaxGunGrade} | deck width -> {deckPercent * 100:N2}% | {(deckPercent * deckPercent) / 3 * 100} deck hit chance.");

            // predictedDeckHits += (deckPercent * deckPercent) / 3;

            Bounds hullSize = __instance.hullSize;
            float y = hullSize.min.y * deckPercent;
            tempPos.y -= y;
        }

        // public static int total = 0;
        // public static int totalDeckHits = 0;
        // public static int totalBeltHits = 0;
        // public static int totalOtherHits = 0;
        // public static float predictedDeckHits = 0;
        // 
        // [HarmonyPrefix]
        // [HarmonyPatch(nameof(Ship.Report))]
        // internal static void Prefix_Report(Ship __instance, Ui.RImportance importance, string text, string tooltip, Ship otherShip)
        // {
        //     if (text.ToLower().Contains("deck"))
        //     {
        //         ++totalDeckHits;
        //         total++;
        //     }
        //     else if (text.ToLower().Contains("belt"))
        //     {
        //         ++totalBeltHits;
        //         total++;
        //     }
        //     else if (text.ToLower().Contains("hit"))
        //     {
        //         ++totalOtherHits;
        //         total++;
        //     }
        // }





        public static HashSet<string> rangeNameSet = new HashSet<string>();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.Update))]
        internal static void Postfix_Update(Ship __instance)
        {
            if (__instance.floatUpsCont != null && __instance.floatUpsCont.transform.localPosition.y < 100)
            {
                __instance.floatUpsCont.transform.localPosition = new Vector3(0, 120, 0);
            }

            if (__instance.uiRangesCont == null) return;
            if (__instance.uiRangesCont.active == false) return;

            // if (Input.GetKeyDown(KeyCode.J))
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg($"{__instance.Name(false, false, false, false, true)}");
            //     // Melon<TweaksAndFixes>.Logger.Msg($"{__instance.Name(false, false, false, false, true)} - {range.name}:");
            //     // Melon<TweaksAndFixes>.Logger.Msg($"  {Input.mousePosition} : {cam.WorldToScreenPoint(rect.transform.position)} : {worldMin} : {worldMax}");
            //     // Melon<TweaksAndFixes>.Logger.Msg($"  {Input.mousePosition.x > worldMin.x} {Input.mousePosition.x < worldMax.x} {Input.mousePosition.y > worldMin.y} {Input.mousePosition.y < worldMax.y}");
            // }

            rangeNameSet.Clear();

            foreach (GameObject range in __instance.uiRangesCont.GetChildren())
            {
                if (range.active == false) continue;
                // ShipsActive/CA Alkmaar (Alkmaar) [netherlands]/ShipIngameUi(Clone)/GunRanges/GunRange:Torp/RangeCanvas/RangeLayout/RangeText

                GameObject rangeCanvas = range.GetChild("RangeCanvas");

                if (!rangeCanvas.GetComponent<Canvas>().enabled) continue;

                RectTransform rect = rangeCanvas.GetComponent<RectTransform>();
                GameObject txtObj = ModUtils.GetChildAtPath("RangeCanvas/RangeLayout/RangeText", range);
                Text txt = txtObj.GetComponent<Text>(); // Text is monospace, each char is 30 wide and 100 tall.

                if (rangeNameSet.Contains(txt.text))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Disabling duplicate range: {txt.text}");
                    range.SetActive(false);
                    continue;
                }

                rangeNameSet.Add(txt.text);

                txtObj.TryDestroyComponent<OnEnter>();
                txtObj.TryDestroyComponent<OnLeave>();

                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, G.cam.cameraComp, out Vector2 outpoint);
                outpoint.x += txt.text.Length * 30 / 2;

                if (outpoint.x > 0 && -outpoint.y > 0 && outpoint.x < txt.text.Length * 30 && -outpoint.y < 100)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"{range.name}: INSIDE");
                    if (rect.gameObject.active) rect.gameObject.SetActive(false);
                }
                else
                {
                    if (!rect.gameObject.active) rect.gameObject.SetActive(true);
                }

                // if (Input.GetKeyDown(KeyCode.J))
                // {
                //     Melon<TweaksAndFixes>.Logger.Msg($"  {range.name}: {outpoint}");
                // }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.FindEnemyForBowSternGuns))]
        internal static void Postfix_FindEnemyForBowSternGuns(Ship __instance, ref Ship __result, PartData gunGroup, Dictionary<PartData, Aim> tempEnemies, List<Part> gunsOnNeededSide, ref Aim aim, ref Ship enemy)
        {
            if (__instance.torpedoMode == ShootMode.Aggressive) return;

            if (__instance.torpedoesAll.Count == 0) return;

            PartData torpData = __instance.torpedoesAll[0].data;

            if (gunGroup.name != torpData.name) return;

            if (__result == null) return;

            if (!__instance.weaponRangesCache.ContainsKey(torpData)) return;

            float rangeToEnemy = __instance.transform.position.GetDistanceXZ(__result.transform.position);

            // Melon<TweaksAndFixes>.Logger.Msg($"  {__instance.Name(false, false)} : {rangeToEnemy} > {__instance.weaponRangesCache[torpData]}");

            if (rangeToEnemy > __instance.weaponRangesCache[torpData] * Config.Param("taf_torpedo_max_launch_range_percent", 0.9f))
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"{__instance.Name(false, false)} : {rangeToEnemy} > {__instance.weaponRangesCache[torpData] * Config.Param("taf_torpedo_max_launch_range_percent", 0.8f)} : {__result.Name(false, false)} : {enemy?.Name(false, false) ?? "NULL"}");

                aim.target = null;
                __result = null;
                enemy = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.FindEnemyForOtherGuns))]
        internal static void Postfix_FindEnemyForOtherGuns(Ship __instance, ref Ship __result, PartData gunGroup, Dictionary<PartData, Aim> tempEnemies, List<Part> gunsOnNeededSide, ref Aim aim, ref Ship enemy)
        {
            if (__instance.torpedoMode == ShootMode.Aggressive) return;

            if (__instance.torpedoesAll.Count == 0) return;

            PartData torpData = __instance.torpedoesAll[0].data;

            if (gunGroup.name != torpData.name) return;

            if (__result == null) return;

            if (!__instance.weaponRangesCache.ContainsKey(torpData)) return;

            float rangeToEnemy = __instance.transform.position.GetDistanceXZ(__result.transform.position);

            // Melon<TweaksAndFixes>.Logger.Msg($"  {__instance.Name(false, false)} : {rangeToEnemy} > {__instance.weaponRangesCache[torpData]}");

            if (rangeToEnemy > __instance.weaponRangesCache[torpData] * Config.Param("taf_torpedo_max_launch_range_percent", 0.9f))
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"{__instance.Name(false, false)} : {rangeToEnemy} > {__instance.weaponRangesCache[torpData] * Config.Param("taf_torpedo_max_launch_range_percent", 0.8f)} : {__result.Name(false, false)} : {enemy?.Name(false, false) ?? "NULL"}");

                aim.target = null;
                __result = null;
                enemy = null;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.HitChanceTorpedoEst))]
        internal static void Prefix_HitChanceTorpedoEst(Ship ally, Ship enemy, ref float rangeToEnemy, float torpedoRange, ref float __result)
        {
            rangeToEnemy = ally.transform.position.GetDistanceXZ(enemy.transform.position);
        }

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

        public static float Reratio(float v, float a1, float b1, float a2, float b2)
        {
            if (a2 == b2) return a2;
            if (a1 == b1) return (a2 + b2) / 2;

            return b2 + Math.Abs(a2 - b2) * ((v - b1) / Math.Abs(a1 - b1));
        }

        private static void ClampShipStats(Ship ship)
        {
            bool modified = false;

            var sd = ship.hull.data;
            var st = ship.shipType;

            float speed_min = st.speedMin;
            float speed_max = st.speedMax;
            float beam_min = sd.beamMin;
            float beam_max = sd.beamMax;
            float draught_min = sd.draughtMin;
            float draught_max = sd.draughtMax;

            if (st.paramx.ContainsKey("shipgen_clamp"))
            {
                modified = true;

                foreach (var stat in st.paramx["shipgen_clamp"])
                {
                    var split = stat.Split(':');

                    if (split.Length != 2)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Invalid `shipTypes.csv` `shipgen_clamp` param: `{stat}` for ID `{st.name}`. Must be formatted `shipgen_clamp(stat:number; stat:number; ...)`.");
                        continue;
                    }

                    string tag = split[0];

                    if (!float.TryParse(split[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Invalid `shipTypes.csv` `shipgen_clamp` param: `{stat}` for ID `{st.name}`. Must be valid number.");
                        continue;
                    }

                    switch (tag)
                    {
                        case "speed_min": speed_min = Math.Clamp(val, st.speedMin, st.speedMax); break;
                        case "speed_max": speed_max = Math.Clamp(val, st.speedMin, st.speedMax); break;
                        case "beam_min": beam_min = Math.Clamp(val, sd.beamMin, sd.beamMax); break;
                        case "beam_max": beam_max = Math.Clamp(val, sd.beamMin, sd.beamMax); break;
                        case "draught_min": draught_min = Math.Clamp(val, sd.draughtMin, sd.draughtMax); break;
                        case "draught_max": draught_max = Math.Clamp(val, sd.draughtMin, sd.draughtMax); break;
                        default:
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_clamp` param: `{stat}` for ID `{st.name}`. Unsuported stat. Can only be [speed_min, speed_max, beam_min, beam_max, draught_min, draught_max]");
                            break;
                    }
                }
            }

            if (sd.paramx.ContainsKey("shipgen_clamp"))
            {
                modified = true;

                foreach (var stat in sd.paramx["shipgen_clamp"])
                {
                    var split = stat.Split(':');

                    if (split.Length != 2)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_clamp` param: `{stat}` for ID `{sd.name}`. Must be formatted `shipgen_clamp(stat:number; stat:number; ...)`.");
                        continue;
                    }

                    string tag = split[0];

                    if (!float.TryParse(split[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_clamp` param: `{stat}` for ID `{sd.name}`. Must be valid number.");
                        continue;
                    }

                    switch (tag)
                    {
                        case "speed_min":   speed_min   = Math.Clamp(val, st.speedMin, st.speedMax); break;
                        case "speed_max":   speed_max   = Math.Clamp(val, st.speedMin, st.speedMax); break;
                        case "beam_min":    beam_min    = Math.Clamp(val, sd.beamMin, sd.beamMax); break;
                        case "beam_max":    beam_max    = Math.Clamp(val, sd.beamMin, sd.beamMax); break;
                        case "draught_min": draught_min = Math.Clamp(val, sd.draughtMin, sd.draughtMax); break;
                        case "draught_max": draught_max = Math.Clamp(val, sd.draughtMin, sd.draughtMax); break;
                        default:
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `parts.csv` `shipgen_clamp` param: `{stat}` for ID `{sd.name}`. Unsuported stat. Can only be [speed_min, speed_max, beam_min, beam_max, draught_min, draught_max]");
                            break;
                    }
                }
            }

            if (!modified) return;

            speed_min = speed_min > speed_max ? speed_max : speed_min;
            beam_min = beam_min > beam_max ? beam_max : beam_min;
            draught_min = draught_min > draught_max ? draught_max : draught_min;

            // Melon<TweaksAndFixes>.Logger.Msg($"Mod stats for ship {ship.Name(false, false)}:");
            // if (speed_min != float.MinValue)   Melon<TweaksAndFixes>.Logger.Msg($"  speed:     {ship.speedMax * 1.943844f,10} -> {Math.Clamp(ship.speedMax, speed_min * 0.5144444f, speed_max * 0.5144444f) * 1.943844f}");
            // if (beam_min != float.MinValue)    Melon<TweaksAndFixes>.Logger.Msg($"  beam:      {ship.beam,10} -> {Util.Remap(ship.beam, sd.beamMin, sd.beamMax, beam_min, beam_max)}");
            // if (draught_min != float.MinValue) Melon<TweaksAndFixes>.Logger.Msg($"  draught:   {ship.draught,10} -> {Util.Remap(ship.draught, sd.draughtMin, sd.draughtMax, draught_min, draught_max)}");

            ship.SetSpeedMax(Math.Clamp(ship.speedMax, speed_min * 0.5144444f, speed_max * 0.5144444f));
            ship.SetBeam(Reratio(ship.beam, sd.beamMin, sd.beamMax, beam_min, beam_max));
            ship.SetDraught(Reratio(ship.draught, sd.draughtMin, sd.draughtMax, draught_min, draught_max));
        }

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

            ClampShipStats(ship);

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
