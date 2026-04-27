using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Reflection;
using System.Linq;
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

            TraceShipgenRemovedPart(__instance, part);
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

        internal static bool IsWithinShipgenInstabilityTolerance(Ship ship)
        {
            if (ship == null || G.GameData == null || G.GameData.stats == null)
                return false;

            if (!G.GameData.stats.ContainsKey("instability_x") || !G.GameData.stats.ContainsKey("instability_z"))
                return false;

            if (!ship.statsValid)
                ship.CStats();

            var instabilityX = G.GameData.stats["instability_x"];
            var instabilityZ = G.GameData.stats["instability_z"];
            if (!ship.stats_.ContainsKey(instabilityX) || !ship.stats_.ContainsKey(instabilityZ))
                return false;

            float transverseOffset = ship.stats_[instabilityX].total;
            float longitudinalOffset = ship.stats_[instabilityZ].total;
            float maxTransverseOffset = Config.Param("taf_shipgen_instability_x_max", 1f);
            float maxLongitudinalOffset = Config.Param("taf_shipgen_instability_z_max", 100f);

            return transverseOffset <= maxTransverseOffset && longitudinalOffset <= maxLongitudinalOffset;
        }

        internal static bool ShouldUseMaxShipgenDisplacement(Ship ship)
        {
            if (!Config.ShipGenTweaks || ship == null || ship.hull == null || ship.hull.data == null)
                return false;

            return true;
        }

        internal static bool ShouldUseShipgenGeometryDefaults(Ship ship)
        {
            if (!Config.ShipGenTweaks || ship == null || ship.hull == null || ship.hull.data == null)
                return false;

            return true;
        }

        internal static Ship CurrentShipgenShip()
        {
            if (_GenerateRandomShipRoutine != null)
                return _GenerateRandomShipRoutine.__4__this;

            if (_AddRandomPartsRoutine != null)
                return _AddRandomPartsRoutine.__4__this;

            return null;
        }

        internal static int ParsePositiveInt(string text, int fallback = 0)
        {
            return int.TryParse(text?.Trim(), out int value) && value > 0 ? value : fallback;
        }

        internal static string NormalizeShipgenProfileKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text.Trim().ToLowerInvariant().Replace("_", " ").Replace("-", " ");
        }

        internal static ShipgenHullProfile ShipgenProfileForShip(Ship ship = null)
        {
            if (!Config.ShipGenTweaks)
                return null;

            ship ??= CurrentShipgenShip();
            if (ship == null || ship.hull == null || ship.hull.data == null)
                return null;

            string profilesText = Config.ParamS("taf_shipgen_hull_profiles", "maine_hull_a:max_displacement=1,main_gun_max=9,tower_tier_max=1") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(profilesText))
                return null;

            string hullName = ship.hull.data.name ?? string.Empty;
            string hullModel = ship.hull.data.model ?? string.Empty;

            foreach (string rawProfile in profilesText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string profileText = rawProfile.Trim();
                if (profileText.Length == 0)
                    continue;

                string[] parts = profileText.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                    continue;

                string hullToken = parts[0].Trim();
                if (!string.Equals(hullToken, hullName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(hullToken, hullModel, StringComparison.OrdinalIgnoreCase))
                    continue;

                var profile = new ShipgenHullProfile { hull = hullToken };
                foreach (string rawRule in parts[1].Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string rule = rawRule.Trim();
                    if (rule.Length == 0)
                        continue;

                    string[] kv = rule.Split(new[] { '=' }, 2);
                    if (kv.Length != 2)
                        continue;

                    string key = NormalizeShipgenProfileKey(kv[0]);
                    string value = kv[1].Trim();

                    if (key == "max displacement")
                        profile.maxDisplacement = value != "0";
                    else if (key == "min beam draught" || key == "min beam draft" || key == "min dimensions" || key == "compact geometry")
                        profile.minBeamDraught = value != "0";
                    else if (key == "generator" || key == "special generator" || key == "shipgen")
                        profile.generator = value.Trim().ToLowerInvariant();
                    else if (key == "main gun max" || key == "main gun cap")
                        profile.mainGunMax = ParsePositiveInt(value);
                    else if (key == "tower tier max" || key == "tower tier cap")
                        profile.towerTierMax = ParsePositiveInt(value);
                    else if (key.StartsWith("tower "))
                    {
                        string family = key.Substring("tower ".Length).Trim();
                        int tier = ParsePositiveInt(value);
                        if (family.Length > 0 && tier > 0)
                            profile.towerFamilyTierMax[family] = tier;
                    }
                }

                return profile;
            }

            return null;
        }

        internal static int ApplyProfileMainGunCap(int adaptiveCap)
        {
            var profile = ShipgenProfileForShip();
            int profileCap = profile?.mainGunMax ?? 0;
            if (profileCap <= 0)
                return adaptiveCap;

            return adaptiveCap > 0 ? Math.Min(adaptiveCap, profileCap) : profileCap;
        }

        internal static int ApplyProfileTowerTierCap(PartData partData, int adaptiveCap)
        {
            var profile = ShipgenProfileForShip();
            if (profile == null)
                return adaptiveCap;

            int profileCap = 0;
            string family = ShipgenTowerFamily(partData);
            if (!string.IsNullOrEmpty(family))
                profile.towerFamilyTierMax.TryGetValue(family, out profileCap);

            if (profileCap <= 0)
                profileCap = profile.towerTierMax;

            if (profileCap <= 0)
                return adaptiveCap;

            return adaptiveCap > 0 ? Math.Min(adaptiveCap, profileCap) : profileCap;
        }

        internal static bool IsSpecialShipgenProfile(Ship ship, string generator)
        {
            var profile = ShipgenProfileForShip(ship);
            if (profile == null || string.IsNullOrWhiteSpace(profile.generator))
                return false;

            return string.Equals(profile.generator, generator, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ShouldUseSpecialTbShipgen(Ship ship)
        {
            if (!Config.ShipGenTweaks || ship == null || ship.hull == null || ship.hull.data == null || ship.shipType == null)
                return false;

            if (Config.Param("taf_shipgen_special_tb_generator_enabled", 0) == 0)
                return false;

            if (!IsSpecialShipgenProfile(ship, "gg_tb_minimal"))
                return false;

            return string.Equals(ship.shipType.name, "tb", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryGenerateSpecialTbParts(Ship ship)
        {
            if (!ShouldUseSpecialTbShipgen(ship))
                return false;

            if (IsShipgenDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"  Special hull shipgen: gg_tb_minimal for {ship.hull.data.name}/{ship.hull.data.model}");

            ApplySpecialTbHullDefaults(ship);
            ClearSpecialShipgenParts(ship);
            ship.RefreshMounts();

            bool generated = TryBacktrackSpecialTbParts(ship, out Part tower, out Part funnel, out bool placedMainGuns, out Part torpedo);

            ship.CheckCaliberOnShip(ship);
            ship.Init();
            ship.UpdateHullStats();
            ship.CStats();
            ship.CalcInstability(true);

            if (IsShipgenDebugEnabled())
            {
                CountMainGuns(ship, out int mainTurrets, out int mainBarrels);
                Melon<TweaksAndFixes>.Logger.Msg($"    special parts: tower={(tower != null ? tower?.data?.name : "none")}, funnel={(funnel != null ? funnel?.data?.name : "none")}, main_guns={(placedMainGuns ? $"{mainTurrets}/{mainBarrels}" : "none")}, torpedo={(torpedo != null ? torpedo?.data?.name : "none")}");
            }

            return generated;
        }

        private static bool TryBacktrackSpecialTbParts(Ship ship, out Part tower, out Part funnel, out bool placedMainGuns, out Part torpedo)
        {
            tower = null;
            funnel = null;
            torpedo = null;
            placedMainGuns = false;

            int explored = 0;
            int searchLimit = Config.Param("taf_shipgen_special_tb_search_limit", 160);
            var towerPlacements = SpecialShipgenPlacements(ship, "tower_main");
            ShuffleSpecialShipgenList(towerPlacements);

            foreach (var towerPlacement in towerPlacements)
            {
                if (explored++ >= searchLimit)
                    break;

                ClearSpecialShipgenParts(ship);
                RemoveSpecialTbGeneratedFunnelMounts(ship);
                ship.RefreshMounts();

                if (!TryCreateSpecialShipgenPart(ship, towerPlacement.PartData, towerPlacement.Mount, out tower))
                    continue;

                var generatedFunnelMounts = CreateSpecialTbFunnelMountsAfterTower(ship, tower);
                var funnelPlacements = SpecialShipgenPlacements(ship, "funnel", generatedFunnelMounts);

                foreach (var funnelPlacement in funnelPlacements)
                {
                    if (explored++ >= searchLimit)
                        break;

                    if (!TryCreateSpecialShipgenPart(ship, funnelPlacement.PartData, funnelPlacement.Mount, out funnel))
                        continue;

                    placedMainGuns = TryPlaceSpecialTbMainGuns(ship);
                    if (placedMainGuns && SpecialTbHasRequiredMainGuns(ship))
                    {
                        TryPlaceSpecialShipgenPart(ship, "torpedo", out torpedo);
                        if (IsShipgenDebugEnabled())
                            Melon<TweaksAndFixes>.Logger.Msg($"    special search: success after {explored} branches");

                        RemoveSpecialTbGeneratedFunnelMounts(ship, funnel?.mount);
                        return true;
                    }

                    RemoveSpecialShipgenPart(ship, funnel);
                    funnel = null;
                    ClearSpecialTbMainGuns(ship);
                    ship.RefreshMounts();
                }

                RemoveSpecialShipgenPart(ship, tower);
                tower = null;
                ClearSpecialTbMainGuns(ship);
                RemoveSpecialTbGeneratedFunnelMounts(ship);
                ship.RefreshMounts();
            }

            ClearSpecialShipgenParts(ship);
            RemoveSpecialTbGeneratedFunnelMounts(ship);
            ship.RefreshMounts();

            if (IsShipgenDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"    special search: failed after {explored} branches, tower_candidates={towerPlacements.Count}");

            return false;
        }

        private static bool SpecialTbHasRequiredMainGuns(Ship ship)
        {
            CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);
            int minTurrets = EffectiveSpecialTbMainGunTurretTarget(ship, null);
            int minBarrels = EffectiveSpecialTbMainGunBarrelTarget(ship, null);
            bool hasMainTurrets = minTurrets <= 0 || numMainTurrets >= minTurrets;
            bool hasMainBarrels = minBarrels <= 0 || numMainBarrels >= minBarrels;
            return hasMainTurrets && hasMainBarrels;
        }

        private static void ClearSpecialTbMainGuns(Ship ship)
        {
            for (int i = ship.parts.Count; i-- > 0;)
            {
                var part = ship.parts[i];
                if (part == null || part.data == null || !part.data.isGun)
                    continue;

                ship.RemovePart(part, true, true);
            }
        }

        private static void RemoveSpecialShipgenPart(Ship ship, Part part)
        {
            if (ship == null || part == null || !ship.parts.Contains(part))
                return;

            ship.RemovePart(part, true, true);
        }

        private static List<SpecialShipgenPlacement> SpecialShipgenPlacements(Ship ship, string partType, List<Mount> mountOverride = null)
        {
            var placements = new List<SpecialShipgenPlacement>();
            var seenMounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mounts = mountOverride ?? new List<Mount>();
            if (mountOverride == null)
            {
                foreach (var shipMount in ship.mounts)
                    mounts.Add(shipMount);
            }

            foreach (var partData in SpecialShipgenPartCandidates(ship, partType))
            {
                seenMounts.Clear();
                foreach (var mount in mounts)
                {
                    if (!IsRealSpecialShipgenMount(ship, mount, partData))
                        continue;

                    if (!mount.Fits(partData, null, null))
                        continue;

                    if (!seenMounts.Add(SpecialShipgenMountKey(mount)))
                        continue;

                    placements.Add(new SpecialShipgenPlacement(partData, mount));
                }
            }

            return placements;
        }

        private static List<Mount> CreateSpecialTbFunnelMountsAfterTower(Ship ship, Part tower)
        {
            var mounts = new List<Mount>();
            if (ship == null || tower == null || ship.hull == null || ship.hull.model == null)
                return mounts;

            GameObject parent = SpecialTbFunnelMountParent(ship);
            if (parent == null)
                return mounts;

            Transform hullTransform = ship.hull.model.gameObject.transform;
            Vector3 towerLocal = hullTransform.InverseTransformPoint(tower.mount != null ? tower.mount.transform.position : tower.transform.position);

            // Try both fore/aft directions because small hull sections do not share a useful convention.
            float[] zOffsets = new[] { 1.5f, 2.25f, 3f, 3.75f, 4.5f, -1.5f, -2.25f, -3f, -3.75f, -4.5f, 5.25f, -5.25f };
            float[] xOffsets = new[] { 0f, -0.35f, 0.35f };

            int index = 0;
            foreach (float zOffset in zOffsets)
            {
                foreach (float xOffset in xOffsets)
                {
                    Vector3 local = towerLocal + new Vector3(xOffset, 0f, zOffset);
                    Mount mount = CreateSpecialTbFunnelMount(ship, parent, hullTransform.TransformPoint(local), index++);
                    if (mount != null)
                        mounts.Add(mount);
                }
            }

            if (IsShipgenDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"    special parts: generated {mounts.Count} bridge-relative funnel mounts");

            ship.RefreshMounts();
            return mounts;
        }

        private static GameObject SpecialTbFunnelMountParent(Ship ship)
        {
            GameObject sections = ship?.hull?.model?.gameObject?.GetChild("Visual")?.GetChild("Sections");
            if (sections == null)
                return ship?.hull?.model?.gameObject;

            return sections.GetChild("Middle1") ?? sections.GetChild("Bow") ?? sections.GetChild("Stern") ?? sections;
        }

        private static Mount CreateSpecialTbFunnelMount(Ship ship, GameObject parent, Vector3 worldPosition, int index)
        {
            if (ship == null || parent == null)
                return null;

            GameObject mountObj = new GameObject();
            mountObj.AddComponent<Mount>();
            mountObj.transform.SetParent(parent.transform);
            mountObj.transform.position = worldPosition;
            mountObj.transform.rotation = ship.hull.model.gameObject.transform.rotation;
            mountObj.name = $"Mount:GG_TB_FUNNEL_{index}";

            Mount mount = mountObj.GetComponent<Mount>();
            mount.funnel = true;
            mount.towerMain = false;
            mount.towerSec = false;
            mount.siBarbette = false;
            mount.barbette = false;
            mount.casemate = false;
            mount.subTorpedo = false;
            mount.deckTorpedo = false;
            mount.special = false;
            mount.center = false;
            mount.side = false;
            mount.caliberMin = 0f;
            mount.caliberMax = 0f;
            mount.barrelsMin = 0;
            mount.barrelsMax = 0;
            mount.ignoreParent = true;

            ship.mounts.Add(mount);
            ship.hull.mountsInside.Add(mount);
            ship.allowedMountsInternal.Add(mount);
            return mount;
        }

        private static void RemoveSpecialTbGeneratedFunnelMounts(Ship ship, Mount keep = null)
        {
            if (ship == null)
                return;

            for (int i = ship.mounts.Count; i-- > 0;)
            {
                Mount mount = ship.mounts[i];
                if (mount == null || mount == keep || !IsSpecialTbGeneratedFunnelMount(mount))
                    continue;

                ship.mounts.Remove(mount);
                if (ship.hull != null && ship.hull.mountsInside.Contains(mount))
                    ship.hull.mountsInside.Remove(mount);
                if (ship.allowedMountsInternal != null && ship.allowedMountsInternal.Contains(mount))
                    ship.allowedMountsInternal.Remove(mount);

                mount.gameObject.TryDestroy();
            }
        }

        private static bool IsSpecialTbGeneratedFunnelMount(Mount mount)
        {
            return (mount?.name ?? string.Empty).StartsWith("Mount:GG_TB_FUNNEL_", StringComparison.OrdinalIgnoreCase);
        }

        private static List<PartData> SpecialShipgenPartCandidates(Ship ship, string partType)
        {
            var candidates = new List<PartData>();
            foreach (var partData in G.GameData.parts.Values)
            {
                if (partData == null || partData.type != partType)
                    continue;

                if (!IsSpecialTbPartCandidate(partData, partType))
                    continue;

                if (!Part.CanPlaceGeneric(partData, ship, false, out _))
                    continue;

                candidates.Add(partData);
            }

            candidates.Sort((a, b) =>
            {
                int weightCompare = a.weight.CompareTo(b.weight);
                return weightCompare != 0 ? weightCompare : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });

            return candidates;
        }

        private static void ShuffleSpecialShipgenList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = System.Random.Shared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void ApplySpecialTbHullDefaults(Ship ship)
        {
            if (ship == null || ship.hull == null || ship.hull.data == null)
                return;

            float oldBeam = ship.Beam();
            float oldDraught = ship.Draught();
            var oldQuarters = ship.CurrentCrewQuarters;

            ship.SetBeam(ship.hull.data.beamMin);
            ship.SetDraught(ship.hull.data.draughtMin);
            ship.CurrentCrewQuarters = Ship.CrewQuarters.Cramped;
            ship.UpdateHullStats();

            if (IsShipgenDebugEnabled() && (!Mathf.Approximately(oldBeam, ship.Beam()) || !Mathf.Approximately(oldDraught, ship.Draught()) || oldQuarters != ship.CurrentCrewQuarters))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"    special hull defaults: beam {oldBeam:0.##}->{ship.Beam():0.##}, draught {oldDraught:0.##}->{ship.Draught():0.##}, quarters {oldQuarters}->{ship.CurrentCrewQuarters}");
            }
        }

        private static void ClearSpecialShipgenParts(Ship ship)
        {
            for (int i = ship.parts.Count; i-- > 0;)
            {
                var part = ship.parts[i];
                if (part == null || part == ship.hull)
                    continue;

                ship.RemovePart(part, true, true);
            }
        }

        private static bool TryPlaceSpecialShipgenPart(Ship ship, string partType, out Part placedPart)
        {
            placedPart = null;

            var candidates = new List<PartData>();
            foreach (var partData in G.GameData.parts.Values)
            {
                if (partData == null || partData.type != partType)
                    continue;

                if (!IsSpecialTbPartCandidate(partData, partType))
                    continue;

                if (!Part.CanPlaceGeneric(partData, ship, false, out _))
                    continue;

                candidates.Add(partData);
            }

            candidates.Sort((a, b) => a.weight.CompareTo(b.weight));

            foreach (var partData in candidates)
            {
                foreach (var mount in ship.mounts)
                {
                    if (!IsRealSpecialShipgenMount(ship, mount, partData))
                        continue;

                    if (!mount.Fits(partData, null, null))
                        continue;

                    if (TryCreateSpecialShipgenPart(ship, partData, mount, out placedPart))
                        return true;
                }
            }

            if (IsShipgenDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"    special parts: failed to place {partType}, candidates={candidates.Count}");

            return false;
        }

        private static bool IsSpecialTbPartCandidate(PartData partData, string partType)
        {
            if (partData == null)
                return false;

            if (partType == "tower_main")
                return ShipgenTowerTier(partData) > 0 && ShipgenTowerTier(partData) <= 2;

            if (partType == "funnel")
                return (partData.nameUi ?? partData.name ?? string.Empty).StartsWith("Standard Funnel", StringComparison.OrdinalIgnoreCase);

            return true;
        }

        private static bool TryPlaceSpecialTbMainGuns(Ship ship)
        {
            CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);
            int minTurrets = Math.Max(0, ship.hull.data.minMainTurrets);
            int minBarrels = Math.Max(0, ship.hull.data.minMainBarrels);
            if ((minTurrets <= 0 || numMainTurrets >= minTurrets) && (minBarrels <= 0 || numMainBarrels >= minBarrels))
                return true;

            var candidates = new List<PartData>();
            foreach (var partData in G.GameData.parts.Values)
            {
                if (!IsSpecialTbMainGunCandidate(ship, partData))
                    continue;

                if (!Part.CanPlaceGeneric(partData, ship, false, out _))
                    continue;

                candidates.Add(partData);
            }

            bool loggedTargetCap = false;
            foreach (var partData in candidates
                .OrderBy(data => data.caliber)
                .ThenByDescending(data => data.barrels)
                .ThenBy(data => data.weight))
            {
                int targetTurrets = EffectiveSpecialTbMainGunTurretTarget(ship, partData);
                int targetBarrels = EffectiveSpecialTbMainGunBarrelTarget(ship, partData);
                if (!loggedTargetCap && ((minTurrets > 0 && targetTurrets < minTurrets) || (minBarrels > 0 && targetBarrels < minBarrels)) && IsShipgenDebugEnabled())
                {
                    CountSpecialTbMainGunMountCapacity(ship, partData, out int usableTurrets, out int usableBarrels);
                    Melon<TweaksAndFixes>.Logger.Msg($"    special parts: main gun target capped by usable mounts: turrets {minTurrets}->{targetTurrets}, barrels {minBarrels}->{targetBarrels}, usable={usableTurrets}/{usableBarrels}, gun={partData.name}");
                    loggedTargetCap = true;
                }

                if (!TryPlaceSpecialTbCenterMainGuns(ship, partData, targetTurrets, targetBarrels))
                    TryPlaceSpecialTbSideMainGuns(ship, partData, targetTurrets, targetBarrels);

                ship.CheckCaliberOnShip(ship);
                CountMainGuns(ship, out numMainTurrets, out numMainBarrels);
                if ((targetTurrets <= 0 || numMainTurrets >= targetTurrets) && (targetBarrels <= 0 || numMainBarrels >= targetBarrels))
                    return true;
            }

            if (IsShipgenDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"    special parts: failed to place main guns, candidates={candidates.Count}");

            return false;
        }

        private static bool IsSpecialTbMainGunCandidate(Ship ship, PartData partData)
        {
            if (ship == null || partData == null || !partData.isGun || Ship.IsCasemateGun(partData))
                return false;

            float caliberInches = partData.caliber * (1f / 25.4f);
            if (Math.Abs(caliberInches - Mathf.Round(caliberInches)) > 0.01f)
                return false;

            int caliber = Mathf.RoundToInt(caliberInches);
            int maxCaliber = ApplyProfileMainGunCap(4);
            if (caliber < 2 || caliber > maxCaliber)
                return false;

            return partData.barrels > 0 && partData.barrels <= 2;
        }

        private static int EffectiveSpecialTbMainGunTurretTarget(Ship ship, PartData partData)
        {
            int rawTarget = Math.Max(0, ship?.hull?.data?.minMainTurrets ?? 0);
            if (rawTarget <= 0)
                return 0;

            CountMainGuns(ship, out int currentTurrets, out _);
            int usableTurrets = 0;
            if (partData != null)
            {
                CountSpecialTbMainGunMountCapacity(ship, partData, out usableTurrets, out _);
            }
            else
            {
                foreach (var candidate in G.GameData.parts.Values)
                {
                    if (!IsSpecialTbMainGunCandidate(ship, candidate))
                        continue;

                    if (!Part.CanPlaceGeneric(candidate, ship, false, out _))
                        continue;

                    CountSpecialTbMainGunMountCapacity(ship, candidate, out int candidateTurrets, out _);
                    usableTurrets = Math.Max(usableTurrets, candidateTurrets);
                }
            }

            int totalPossibleTurrets = currentTurrets + usableTurrets;
            if (totalPossibleTurrets <= 0)
                return rawTarget;

            return Math.Max(1, Math.Min(rawTarget, totalPossibleTurrets));
        }

        private static int EffectiveSpecialTbMainGunBarrelTarget(Ship ship, PartData partData)
        {
            int rawTarget = Math.Max(0, ship?.hull?.data?.minMainBarrels ?? 0);
            if (rawTarget <= 0)
                return 0;

            CountMainGuns(ship, out _, out int currentBarrels);
            int usableBarrels = 0;
            if (partData != null)
            {
                CountSpecialTbMainGunMountCapacity(ship, partData, out _, out usableBarrels);
            }
            else
            {
                foreach (var candidate in G.GameData.parts.Values)
                {
                    if (!IsSpecialTbMainGunCandidate(ship, candidate))
                        continue;

                    if (!Part.CanPlaceGeneric(candidate, ship, false, out _))
                        continue;

                    CountSpecialTbMainGunMountCapacity(ship, candidate, out _, out int candidateBarrels);
                    usableBarrels = Math.Max(usableBarrels, candidateBarrels);
                }
            }

            int totalPossibleBarrels = currentBarrels + usableBarrels;
            if (totalPossibleBarrels <= 0)
                return rawTarget;

            return Math.Max(1, Math.Min(rawTarget, totalPossibleBarrels));
        }

        private static void CountSpecialTbMainGunMountCapacity(Ship ship, PartData partData, out int usableTurrets, out int usableBarrels)
        {
            usableTurrets = 0;
            usableBarrels = 0;
            if (ship == null || partData == null)
                return;

            if ((partData.name ?? string.Empty).Contains("_side"))
            {
                var mounts = new List<KeyValuePair<Mount, Vector3>>();
                var mountKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var mount in ship.mounts)
                {
                    if (!IsRealSpecialShipgenMount(ship, mount, partData) || !mount.side || !mount.Fits(partData, null, null))
                        continue;

                    if (!mountKeys.Add(SpecialShipgenMountKey(mount)))
                        continue;

                    mounts.Add(new KeyValuePair<Mount, Vector3>(mount, ship.hull.transform.InverseTransformPoint(mount.transform.position)));
                }

                foreach (var port in mounts)
                {
                    if (port.Value.x >= -0.01f)
                        continue;

                    foreach (var starboard in mounts)
                    {
                        if (starboard.Value.x <= 0.01f)
                            continue;

                        float score = Math.Abs(starboard.Value.z - port.Value.z) + Math.Abs(Math.Abs(starboard.Value.x) - Math.Abs(port.Value.x));
                        if (score < 1.25f)
                        {
                            usableTurrets += 2;
                            usableBarrels += partData.barrels * 2;
                            break;
                        }
                    }
                }

                return;
            }

            var centerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mount in ship.mounts)
            {
                if (!IsRealSpecialShipgenMount(ship, mount, partData) || !mount.center || !mount.Fits(partData, null, null))
                    continue;

                if (centerKeys.Add(SpecialShipgenMountKey(mount)))
                {
                    usableTurrets++;
                    usableBarrels += partData.barrels;
                }
            }
        }

        private static bool TryPlaceSpecialTbCenterMainGuns(Ship ship, PartData partData, int minTurrets, int minBarrels)
        {
            if ((partData.name ?? string.Empty).Contains("_side"))
                return false;

            var mounts = new List<Mount>();
            var mountKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mount in ship.mounts)
            {
                if (!IsRealSpecialShipgenMount(ship, mount, partData) || !mount.center || !mount.Fits(partData, null, null))
                    continue;

                if (!mountKeys.Add(SpecialShipgenMountKey(mount)))
                    continue;

                mounts.Add(mount);
            }

            mounts.Sort((a, b) =>
            {
                Vector3 localA = ship.hull.transform.InverseTransformPoint(a.transform.position);
                Vector3 localB = ship.hull.transform.InverseTransformPoint(b.transform.position);
                int centerCompare = Math.Abs(localA.x).CompareTo(Math.Abs(localB.x));
                return centerCompare != 0 ? centerCompare : Math.Abs(localB.z).CompareTo(Math.Abs(localA.z));
            });

            bool placedAny = false;
            var trialParts = new List<Part>();
            foreach (var mount in mounts)
            {
                if (!TryCreateSpecialShipgenPart(ship, partData, mount, out Part part))
                    continue;

                placedAny = true;
                trialParts.Add(part);
                ship.CheckCaliberOnShip(ship);
                CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);
                if ((minTurrets <= 0 || numMainTurrets >= minTurrets) && (minBarrels <= 0 || numMainBarrels >= minBarrels))
                    return true;
            }

            foreach (var part in trialParts)
            {
                if (part != null && ship.parts.Contains(part))
                    ship.RemovePart(part, true, true);
            }

            return placedAny;
        }

        private static bool TryPlaceSpecialTbSideMainGuns(Ship ship, PartData partData, int minTurrets, int minBarrels)
        {
            if (!(partData.name ?? string.Empty).Contains("_side"))
                return false;

            var mounts = new List<KeyValuePair<Mount, Vector3>>();
            var mountKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mount in ship.mounts)
            {
                if (!IsRealSpecialShipgenMount(ship, mount, partData) || !mount.side || !mount.Fits(partData, null, null))
                    continue;

                if (!mountKeys.Add(SpecialShipgenMountKey(mount)))
                    continue;

                mounts.Add(new KeyValuePair<Mount, Vector3>(mount, ship.hull.transform.InverseTransformPoint(mount.transform.position)));
            }

            mounts.Sort((a, b) =>
            {
                int zCompare = Math.Abs(a.Value.z).CompareTo(Math.Abs(b.Value.z));
                return zCompare != 0 ? zCompare : Math.Abs(a.Value.x).CompareTo(Math.Abs(b.Value.x));
            });

            foreach (var port in mounts)
            {
                if (port.Value.x >= -0.01f || port.Key.employedPart != null)
                    continue;

                Mount starboard = null;
                float bestScore = float.MaxValue;
                foreach (var pair in mounts)
                {
                    if (pair.Value.x <= 0.01f || pair.Key.employedPart != null)
                        continue;

                    float score = Math.Abs(pair.Value.z - port.Value.z) + Math.Abs(Math.Abs(pair.Value.x) - Math.Abs(port.Value.x));
                    if (score < bestScore)
                    {
                        bestScore = score;
                        starboard = pair.Key;
                    }
                }

                if (starboard == null)
                    continue;

                if (!TryCreateSpecialShipgenPart(ship, partData, port.Key, out Part portGun))
                    continue;

                if (!TryCreateSpecialShipgenPart(ship, partData, starboard, out Part starboardGun))
                {
                    ship.RemovePart(portGun, true, true);
                    continue;
                }

                ship.CheckCaliberOnShip(ship);
                CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);
                if ((minTurrets <= 0 || numMainTurrets >= minTurrets) && (minBarrels <= 0 || numMainBarrels >= minBarrels))
                    return true;

                if (portGun != null && ship.parts.Contains(portGun))
                    ship.RemovePart(portGun, true, true);
                if (starboardGun != null && ship.parts.Contains(starboardGun))
                    ship.RemovePart(starboardGun, true, true);
            }

            return false;
        }

        private static string SpecialShipgenMountKey(Mount mount)
        {
            if (mount == null)
                return string.Empty;

            return mount.name ?? mount.GetHashCode().ToString();
        }

        private static bool IsRealSpecialShipgenMount(Ship ship, Mount mount, PartData partData)
        {
            if (ship == null || mount == null || partData == null || mount.employedPart != null)
                return false;

            string mountName = mount.name ?? string.Empty;
            if (mountName.IndexOf("TAF_ALLOW", StringComparison.OrdinalIgnoreCase) >= 0 ||
                mountName.IndexOf("ALLOW_DEFAULT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                mountName.IndexOf("ALLOW_ALL", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            Vector3 local = ship.hull != null ? ship.hull.transform.InverseTransformPoint(mount.transform.position) : mount.transform.position;
            if (Math.Abs(local.x) > 100f || Math.Abs(local.y) > 100f || Math.Abs(local.z) > 100f)
                return false;

            return true;
        }

        private static bool TryCreateSpecialShipgenPart(Ship ship, PartData partData, Mount mount, out Part placedPart)
        {
            placedPart = null;

            if (!IsRealSpecialShipgenMount(ship, mount, partData))
                return false;

            Part part = null;
            try
            {
                var store = new Part.Store();
                store.name = partData.name;
                store.Id = new Il2CppSystem.Guid(System.Guid.NewGuid().ToString());
                store.position = mount.transform.position;
                store.rotation = mount.transform.rotation;

                part = Part.CreateFromStore(store, ship, ship.partsCont);
                if (part == null)
                    return false;

                part.SetActiveX(true);
                ship.AddPart(part);
                part.LoadModel(ship, true);
                part.Mount(mount);
                part.Place(mount.transform.position, true);

                if (!part.CanPlace(out string denyReason))
                {
                    if (IsShipgenDebugEnabled())
                        Melon<TweaksAndFixes>.Logger.Msg($"    special parts: rejected {partData.name} on mount {mount.name}: {denyReason}");

                    ship.RemovePart(part, true, true);
                    return false;
                }

                placedPart = part;
                ship.RefreshMounts();
                return true;
            }
            catch (Exception ex)
            {
                if (part != null && ship.parts.Contains(part))
                    ship.RemovePart(part, true, true);

                if (IsShipgenDebugEnabled())
                    Melon<TweaksAndFixes>.Logger.Msg($"    special parts: rejected {partData.name} on mount {mount.name}: {ex.GetType().Name}");

                return false;
            }
        }

        internal static void ForceMaxShipgenDisplacement(Ship ship)
        {
            bool forceMaxDisplacement = ShouldUseMaxShipgenDisplacement(ship);
            bool forceGeometryDefaults = ShouldUseShipgenGeometryDefaults(ship);
            if (!forceMaxDisplacement && !forceGeometryDefaults)
                return;

            float oldTonnage = ship.Tonnage();
            float oldBeam = ship.Beam();
            float oldDraught = ship.Draught();

            if (forceGeometryDefaults)
                ApplyShipgenGeometryDefaults(ship);

            if (forceMaxDisplacement)
            {
                float maxTonnage = MaxLegalShipgenTonnage(ship);
                if (maxTonnage > 0f && !Mathf.Approximately(ship.Tonnage(), maxTonnage))
                    SetShipgenTonnage(ship, maxTonnage);
            }

            if (Config.Param("taf_debug_shipgen_info", 0) != 0 &&
                (!Mathf.Approximately(oldTonnage, ship.Tonnage()) ||
                 !Mathf.Approximately(oldBeam, ship.Beam()) ||
                 !Mathf.Approximately(oldDraught, ship.Draught())))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Shipgen hull defaults: {ship.hull.data.name}/{ship.hull.data.model}, tonnage {oldTonnage:0}t -> {ship.Tonnage():0}t, beam {oldBeam:0.##} -> {ship.Beam():0.##}, draught {oldDraught:0.##} -> {ship.Draught():0.##}");
            }
        }

        internal static void ApplyShipgenGeometryDefaults(Ship ship)
        {
            if (ship == null || ship.hull == null || ship.hull.data == null)
                return;

            string shipType = ship.shipType?.name ?? string.Empty;
            bool isBattleship = string.Equals(shipType, "bb", StringComparison.OrdinalIgnoreCase);
            bool isSmallTorpedoCraft =
                string.Equals(shipType, "tb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipType, "dd", StringComparison.OrdinalIgnoreCase);

            float targetBeam = isBattleship ? ship.hull.data.beamMax : (isSmallTorpedoCraft ? ship.hull.data.beamMin : 0f);
            float targetDraught = isSmallTorpedoCraft ? ship.hull.data.draughtMin : 0f;

            targetBeam = Mathf.Clamp(targetBeam, ship.hull.data.beamMin, ship.hull.data.beamMax);
            targetDraught = Mathf.Clamp(targetDraught, ship.hull.data.draughtMin, ship.hull.data.draughtMax);

            if (!Mathf.Approximately(ship.Beam(), targetBeam))
                ship.SetBeam(targetBeam);

            if (!Mathf.Approximately(ship.Draught(), targetDraught))
                ship.SetDraught(targetDraught);

            ship.UpdateHullStats();
        }

        internal static float MaxLegalShipgenTonnage(Ship ship)
        {
            if (ship == null || ship.hull == null || ship.hull.data == null || ship.shipType == null)
                return 0f;

            float minTonnage = ship.TonnageMin();
            float hullMaxTonnage = ship.TonnageMax();
            if (hullMaxTonnage <= 0f)
                return 0f;

            float techLimit = ship.player != null ? ship.player.TonnageLimit(ship.shipType) : hullMaxTonnage;
            float shipyardLimit = 0f;
            float upper = hullMaxTonnage;
            if (techLimit > 0f)
                upper = Mathf.Min(upper, techLimit);

            if (GameManager.Instance != null && GameManager.Instance.isCampaign && ship.player != null && ship.player.shipyard > 0f)
            {
                shipyardLimit = ship.player.shipyard;
                upper = Mathf.Min(upper, ship.player.shipyard);
            }

            if (upper < minTonnage)
                return 0f;

            float rounded = Ship.RoundTonnageToStep(upper);
            float target = Mathf.Clamp(rounded, minTonnage, upper);

            if (Config.Param("taf_debug_shipgen_info", 0) != 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Shipgen tonnage cap: {ship.hull.data.name}/{ship.hull.data.model}, min={minTonnage:0}t, hullMax={hullMaxTonnage:0}t, tech={techLimit:0}t, shipyard={(shipyardLimit > 0f ? $"{shipyardLimit:0}t" : "n/a")}, target={target:0}t");
            }

            return target;
        }

        internal static void SetShipgenTonnage(Ship ship, float tonnage)
        {
            if (ship == null)
                return;

            float beamDraughtBonus = ship.BeamDraughtBonus();
            if (beamDraughtBonus <= 0f)
                beamDraughtBonus = 1f;

            float rawTonnage = tonnage / beamDraughtBonus;
            ship.SetTonnage(rawTonnage);

            if (ship.Tonnage() + 0.5f >= tonnage)
            {
                if (Config.Param("taf_debug_shipgen_info", 0) != 0 && !Mathf.Approximately(rawTonnage, tonnage))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Shipgen tonnage raw target: display={tonnage:0}t, beam/draught bonus={beamDraughtBonus:0.###}, raw={rawTonnage:0}t, final={ship.Tonnage():0}t");
                }
                return;
            }

            float setTonnage = ship.Tonnage();
            ship.tonnage = rawTonnage;
            ship.UpdateHullStats();

            if (Config.Param("taf_debug_shipgen_info", 0) != 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Shipgen tonnage setter clamp bypass: display={tonnage:0}t, beam/draught bonus={beamDraughtBonus:0.###}, raw={rawTonnage:0}t, SetTonnage={setTonnage:0}t, final={ship.Tonnage():0}t");
            }
        }

        internal static bool IsShipgenTonnageAllowed(Ship ship, float tonnage)
        {
            if (ship == null || ship.player == null || ship.shipType == null)
                return tonnage > 0f;

            if (GameManager.Instance != null && GameManager.Instance.isCampaign && ship.player.shipyard > 0f && tonnage > ship.player.shipyard)
                return false;

            return ship.player.IsTonnageAllowedByTech(tonnage, ship.shipType);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ship.IsValidWeightOffset))]
        internal static bool Prefix_IsValidWeightOffset(Ship __instance, ref bool __result, out float __state)
        {
            __state = Time.realtimeSinceStartup;
            if (!Config.ShipGenTweaks || _GenerateRandomShipRoutine == null)
                return true;

            if (!IsWithinShipgenInstabilityTolerance(__instance))
                return true;

            __result = true;

            if (Config.Param("taf_debug_shipgen_info", 0) != 0)
            {
                float instX = __instance.stats_[G.GameData.stats["instability_x"]].total;
                float instZ = __instance.stats_[G.GameData.stats["instability_z"]].total;
                Melon<TweaksAndFixes>.Logger.Msg($"  Accepting shipgen instability tolerance: x={instX:0.###}, z={instZ:0.###}");
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.IsValidWeightOffset))]
        internal static void Postfix_IsValidWeightOffset(float __state)
        {
            if (Config.ShipGenTweaks && _GenerateRandomShipRoutine != null)
                RecordShipgenPhase("call_valid_weight_offset", Time.realtimeSinceStartup - __state);
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

            var sectionsGo = ship.hull.model.gameObject.GetChild("Visual").GetChild("Sections");

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
            // Melon<TweaksAndFixes>.Logger.Msg($"{__instance.Name(false, false, false, false, true)} : Show deck props {UiM.TAF_Settings.settings.deckPropCoverage != float.MaxValue}");

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
        internal static readonly Dictionary<string, RandPartCandidateStats> _RandPartCandidateStats = new();
        internal static float _ShipgenStartedAt = 0f;
        internal static float _AttemptStartedAt = 0f;
        internal static float _ShipgenGunNormalizationUntil = 0f;
        internal static bool _FastRetriedTowerBeforeMainGun = false;
        internal static int _MainGunPlacementFailures = 0;
        internal static int _MainGunDownsizeReferenceCaliber = 0;
        internal static int _MainGunDownsizeMinimumCaliber = 0;
        internal static int _AttemptMainGunMaxAcceptedCaliber = 0;
        internal static int _AttemptMainGunMinAcceptedCaliber = 0;
        internal static float _TowerDownsizeReferenceWeight = 0f;
        internal static float _TowerDownsizeMinimumWeight = 0f;
        internal static float _AttemptTowerMaxAcceptedWeight = 0f;
        internal static float _AttemptTowerMinAcceptedWeight = 0f;
        internal static int _TowerDownsizeReferenceTier = 0;
        internal static int _TowerDownsizeMinimumTier = 0;
        internal static int _AttemptTowerMaxAcceptedTier = 0;
        internal static int _AttemptTowerMinSeenTier = 0;
        internal static int _AttemptTowerRejectedByDownsize = 0;
        internal static int _AttemptGunRejectedByLowerMark = 0;
        internal static int _ShipgenPlacementSeq = 0;
        internal static int _AttemptShipgenPlacementSeq = 0;
        internal static int _AttemptShipgenPlacementTraceCount = 0;
        internal static bool _AttemptShipgenPlacementTraceSuppressed = false;
        internal static int _AttemptShipgenRemoveSeq = 0;
        internal static int _AttemptShipgenRemoveTraceCount = 0;
        internal static bool _AttemptShipgenRemoveTraceSuppressed = false;
        internal static int _AttemptShipgenDeckProbeSeq = 0;
        internal static int _AttemptShipgenDeckProbeTraceCount = 0;
        internal static bool _AttemptShipgenDeckProbeTraceSuppressed = false;
        internal static int _AttemptShipgenCanPlaceSeq = 0;
        internal static int _AttemptShipgenCanPlaceTraceCount = 0;
        internal static bool _AttemptShipgenCanPlaceTraceSuppressed = false;
        internal static int _AttemptShipgenFlowSeq = 0;
        internal static int _AttemptShipgenFlowTraceCount = 0;
        internal static bool _AttemptShipgenFlowTraceSuppressed = false;
        internal static int _ShipgenFinalPartSeq = 0;
        internal static int _AttemptShipgenFinalPartSeq = 0;
        internal static int _ShipgenFinalPartTraceCount = 0;
        internal static bool _ShipgenFinalPartTraceSuppressed = false;
        internal static int _ShipgenAttemptTraceId = 0;
        internal static int _AttemptLifecyclePlacedMain = 0;
        internal static int _AttemptLifecyclePlacedGun = 0;
        internal static int _AttemptLifecyclePlacedTower = 0;
        internal static int _AttemptLifecyclePlacedFunnel = 0;
        internal static int _AttemptLifecycleRemovedMain = 0;
        internal static int _AttemptLifecycleRemovedGun = 0;
        internal static int _AttemptLifecycleRemovedTower = 0;
        internal static int _AttemptLifecycleRemovedFunnel = 0;
        internal static int _AttemptLifecycleFinalMain = 0;
        internal static int _AttemptLifecycleFinalGun = 0;
        internal static int _AttemptLifecycleFinalTower = 0;
        internal static int _AttemptLifecycleFinalFunnel = 0;
        internal static readonly Dictionary<string, int> _AttemptMainGunRejectReasons = new();
        internal static readonly Dictionary<string, int> _ShipgenTopGunMarkByBattery = new();
        internal static readonly Dictionary<string, int> _TowerDownsizeReferenceTierByFamily = new();
        internal static readonly Dictionary<string, int> _TowerDownsizeMinimumTierByFamily = new();
        internal static readonly Dictionary<string, int> _AttemptTowerMaxAcceptedTierByFamily = new();
        internal static readonly Dictionary<string, int> _AttemptTowerMinSeenTierByFamily = new();
        internal static readonly Dictionary<IntPtr, string> _ShipgenPartRandPartByPointer = new();
        internal static readonly Dictionary<string, RandPartAttemptStats> _AttemptRandPartStatsByBucket = new();
        internal static readonly HashSet<string> _AttemptRandPartsStarted = new();
        internal static readonly HashSet<string> _AttemptRandPartsSucceeded = new();
        internal static readonly HashSet<string> _AttemptRandPartsSkipped = new();
        internal static string _AttemptCurrentRandPartName = string.Empty;
        internal static string _AttemptCurrentRandPartBucket = string.Empty;
        internal static float _AttemptCurrentRandPartStartedAt = 0f;
        internal static readonly Dictionary<string, ShipgenPhaseStats> _AttemptShipgenPhaseStats = new();
        internal static readonly HashSet<string> _ShipgenRandPartsReorderedForShipTypes = new();
        internal static readonly HashSet<string> _HardBannedShipgenMainGunRandParts = new()
        {
            // Temporarily empty for shipgen flow tracing. Keep suspicious recipes
            // visible so we can see whether they fail locally or survive.
        };

        internal class ShipgenHullProfile
        {
            public string hull = string.Empty;
            public bool maxDisplacement;
            public bool minBeamDraught;
            public string generator = string.Empty;
            public int mainGunMax;
            public int towerTierMax;
            public readonly Dictionary<string, int> towerFamilyTierMax = new();
        }

        internal class SpecialShipgenPlacement
        {
            public PartData PartData;
            public Mount Mount;

            public SpecialShipgenPlacement(PartData partData, Mount mount)
            {
                PartData = partData;
                Mount = mount;
            }
        }

        internal class ShipgenPhaseStats
        {
            public string name = string.Empty;
            public int calls;
            public float elapsed;
        }

        internal class RandPartCandidateStats
        {
            public string name = string.Empty;
            public string type = string.Empty;
            public string condition = string.Empty;
            public int seen;
            public int accepted;
            public int rejectedByGame;
            public int rejectedByTAFCaliber;
            public int rejectedByTAFDownsize;
            public int rejectedByTAFTowerDownsize;
            public int rejectedByTAFNonWholeCaliber;
            public int rejectedByTAFLowerMark;
            public int rejectedByTAFSkippedRandPart;
            public int maxAcceptedCaliber;
            public int canPlaceChecks;
            public int canPlaceYes;
            public int canPlaceNo;
            public int placed;
            public int removed;
            public int final;

            public bool IsGun => type == "gun";
            public bool IsMainGun => type == "gun" && condition.Contains("main_cal");
            public string GunRole
            {
                get
                {
                    if (condition.Contains("main_cal"))
                        return "main";

                    if (condition.Contains("sec_cal"))
                        return "sec";

                    if (condition.Contains("ter_cal"))
                        return "ter";

                    return "other";
                }
            }

            public string MainGunLayout
            {
                get
                {
                    string layoutSource = $"{name}/{condition}";
                    if (layoutSource.Contains("main_center") || layoutSource.Contains("/mc/"))
                        return "center";

                    if (layoutSource.Contains("main_side") || layoutSource.Contains("single_side") || layoutSource.Contains("/mg2") || layoutSource.Contains("/mg3") || layoutSource.Contains("/ms/"))
                        return "side";

                    return "other";
                }
            }
        }

        internal class RandPartAttemptStats
        {
            public string bucket = string.Empty;
            public int recipesStarted;
            public int recipesSkipped;
            public int successfulRecipes;
            public int partsPlaced;
            public float elapsed;
        }

        internal static bool IsShipgenDebugEnabled()
            => Config.Param("taf_debug_shipgen_info", 0) != 0;

        internal static void MarkShipgenGunNormalizationActive(float seconds = 0.25f)
        {
            if (!Config.ShipGenTweaks)
                return;

            _ShipgenGunNormalizationUntil = Math.Max(_ShipgenGunNormalizationUntil, Time.realtimeSinceStartup + seconds);
        }

        internal static bool IsShipgenGunNormalizationActive()
        {
            if (!Config.ShipGenTweaks)
                return false;

            if (_GenerateRandomShipRoutine != null || _AddRandomPartsRoutine != null)
                return true;

            return _ShipgenGunNormalizationUntil > 0f && Time.realtimeSinceStartup <= _ShipgenGunNormalizationUntil;
        }

        internal static bool IsShipgenGunNormalizationDebugEnabled()
            => Config.Param("taf_debug_shipgen_gun_normalization", 0) != 0;

        internal static bool IsShipgenPlacementTraceEnabled()
            => IsShipgenDebugEnabled() && Config.Param("taf_debug_shipgen_placement_trace", 0) != 0;

        internal static bool IsShipgenFlowTraceRequested()
            => IsShipgenDebugEnabled() && Config.Param("taf_debug_shipgen_flow_trace", 0) != 0;

        internal static bool IsShipgenFlowTraceActive()
        {
            if (!IsShipgenFlowTraceRequested())
                return false;

            int targetAttempt = Config.Param("taf_debug_shipgen_flow_trace_attempt", 1);
            return targetAttempt <= 0 || _ShipgenAttemptTraceId == targetAttempt;
        }

        internal static void NormalizeShipgenSpeed(Ship ship, bool logChanges = false)
        {
            if (!Config.ShipGenTweaks || _GenerateRandomShipRoutine == null || Config.Param("taf_shipgen_whole_knot_speeds", 1) == 0)
                return;

            if (Patch_BattleManager_d115._ShipGenInfo.customSpeed > 0f)
                return;

            if (ship == null)
                return;

            if (ship.shipType == null)
                return;

            float oldSpeedKnots = ship.SpeedMax() / ShipM.KnotsToMS;
            float minSpeedKnots = ship.shipType.speedMin;
            float maxSpeedKnots = ship.shipType.speedMax;

            if (oldSpeedKnots < Math.Max(1f, minSpeedKnots - 0.25f))
                return;

            float newSpeedKnots = Mathf.Floor(oldSpeedKnots);
            newSpeedKnots = Mathf.Clamp(newSpeedKnots, minSpeedKnots, maxSpeedKnots);

            if (Math.Abs(oldSpeedKnots - newSpeedKnots) < 0.01f)
                return;

            ship.SetSpeedMax(newSpeedKnots * ShipM.KnotsToMS);

            if (logChanges && Config.Param("taf_debug_shipgen_info", 0) != 0)
                Melon<TweaksAndFixes>.Logger.Msg($"  Whole-knot speed: {oldSpeedKnots:0.0}kn -> {ship.SpeedMax() / ShipM.KnotsToMS:0}kn");
        }

        internal static void NormalizeShipgenSpeedArgument(Ship ship, ref float speedMax)
        {
            if (!Config.ShipGenTweaks || !IsShipgenGunNormalizationActive() || Config.Param("taf_shipgen_whole_knot_speeds", 1) == 0)
                return;

            if (Patch_BattleManager_d115._ShipGenInfo.customSpeed > 0f)
                return;

            if (ship == null || ship.shipType == null || speedMax <= 0f)
                return;

            float oldSpeedKnots = speedMax / ShipM.KnotsToMS;
            float minSpeedKnots = ship.shipType.speedMin;
            float maxSpeedKnots = ship.shipType.speedMax;

            if (oldSpeedKnots < Math.Max(1f, minSpeedKnots - 0.25f))
                return;

            float newSpeedKnots = Mathf.Clamp(Mathf.Floor(oldSpeedKnots), minSpeedKnots, maxSpeedKnots);
            if (Math.Abs(oldSpeedKnots - newSpeedKnots) < 0.01f)
                return;

            speedMax = newSpeedKnots * ShipM.KnotsToMS;

            if (Config.Param("taf_debug_shipgen_speed_info", 0) != 0)
                Melon<TweaksAndFixes>.Logger.Msg($"  Whole-knot speed arg: {oldSpeedKnots:0.0}kn -> {newSpeedKnots:0}kn");
        }

        internal static void ResetShipgenPlacementTrace(bool resetGlobal)
        {
            if (resetGlobal)
            {
                _ShipgenPlacementSeq = 0;
                _ShipgenFinalPartSeq = 0;
                _ShipgenFinalPartTraceCount = 0;
                _ShipgenFinalPartTraceSuppressed = false;
            }

            _AttemptShipgenPlacementSeq = 0;
            _AttemptShipgenPlacementTraceCount = 0;
            _AttemptShipgenPlacementTraceSuppressed = false;
            _AttemptShipgenRemoveSeq = 0;
            _AttemptShipgenRemoveTraceCount = 0;
            _AttemptShipgenRemoveTraceSuppressed = false;
            _AttemptShipgenDeckProbeSeq = 0;
            _AttemptShipgenDeckProbeTraceCount = 0;
            _AttemptShipgenDeckProbeTraceSuppressed = false;
            _AttemptShipgenCanPlaceSeq = 0;
            _AttemptShipgenCanPlaceTraceCount = 0;
            _AttemptShipgenCanPlaceTraceSuppressed = false;
            _AttemptShipgenFlowSeq = 0;
            _AttemptShipgenFlowTraceCount = 0;
            _AttemptShipgenFlowTraceSuppressed = false;
            _AttemptShipgenFinalPartSeq = 0;
            _ShipgenAttemptTraceId = Math.Max(1, _GenerateRandomShipRoutine?._tryN_5__5 ?? _ShipgenAttemptTraceId);
            _AttemptLifecyclePlacedMain = 0;
            _AttemptLifecyclePlacedGun = 0;
            _AttemptLifecyclePlacedTower = 0;
            _AttemptLifecyclePlacedFunnel = 0;
            _AttemptLifecycleRemovedMain = 0;
            _AttemptLifecycleRemovedGun = 0;
            _AttemptLifecycleRemovedTower = 0;
            _AttemptLifecycleRemovedFunnel = 0;
            _AttemptLifecycleFinalMain = 0;
            _AttemptLifecycleFinalGun = 0;
            _AttemptLifecycleFinalTower = 0;
            _AttemptLifecycleFinalFunnel = 0;
            _AttemptMainGunRejectReasons.Clear();
            _ShipgenPartRandPartByPointer.Clear();
        }

        internal static string ShipgenPartKind(PartData data)
        {
            if (data == null)
                return "part";

            if (data.isGun)
                return "gun";

            if (data.isTowerMain)
                return "tower_main";

            if (data.isTowerAny)
                return "tower_sec";

            if (data.isFunnel)
                return "funnel";

            if (data.isBarbette)
                return "barbette";

            return data.type ?? "part";
        }

        internal static string ShipgenPartLabel(Part part)
        {
            var data = part?.data;
            if (data == null)
                return "(unknown)";

            string label = string.IsNullOrWhiteSpace(data.nameUi) ? data.name : data.nameUi;
            if (data.isGun && part?.ship != null)
                label += $" {data.GetCaliberInch(part.ship):0.#}\"/{data.barrels}";

            return label;
        }

        internal static string ShipgenPartLocation(Part part, Vector3 fallback)
        {
            Vector3 local = fallback;
            var ship = part?.ship ?? _GenerateRandomShipRoutine?.__4__this ?? _AddRandomPartsRoutine?.__4__this;
            if (ship != null && ship.hull != null)
                local = ship.hull.transform.InverseTransformPoint(fallback);

            string side = Math.Abs(local.x) < 0.01f ? "center" : (local.x < 0f ? "port" : "starboard");
            return $"[{side}] z={local.z:0.00}, x={local.x:0.00}";
        }

        internal static string ShipgenPartMountAndRandPart(Part part, bool allowCurrentRandPart = true)
        {
            string randPartName = ShipgenRandPartNameForPart(part, allowCurrentRandPart);
            string randPart = string.IsNullOrWhiteSpace(randPartName) ? "rp=?" : $"rp={randPartName}";
            string mount = part?.mount == null ? "mount=none" : $"mount={part.mount.name}";
            return $"{mount}, {randPart}";
        }

        internal static string ShipgenPartDataLabel(PartData data)
        {
            if (data == null)
                return "(unknown)";

            string label = string.IsNullOrWhiteSpace(data.nameUi) ? data.name : data.nameUi;
            if (data.isGun)
                label += $" cal={data.caliber:0.#}mm/{data.barrels}";

            return $"{ShipgenPartKind(data)} {label} [{data.name}]";
        }

        internal static string ShipgenRandPartSummary(RandPart rp)
        {
            if (rp == null)
                return "rp=<null>";

            return $"rp={rp.name}, type={rp.type}, min={rp.min}, max={rp.max}, chance={rp.chance:0.###}, center={rp.center}, side={rp.side}, z={rp.rangeZFrom:0.###}..{rp.rangeZTo:0.###}, condition={rp.condition}, param={rp.param}";
        }

        internal static string ShipgenAddRandomPartsFlowState()
        {
            if (_AddRandomPartsRoutine == null)
                return "add_state=-1";

            string chooseCount = _AddRandomPartsRoutine._chooseFromParts_5__11 == null ? "?" : _AddRandomPartsRoutine._chooseFromParts_5__11.Count.ToString();
            return $"add_state={_AddRandomPartsRoutine.__1__state}, desired={_AddRandomPartsRoutine._desiredAmount_5__10}, choices={chooseCount}, n={_AddRandomPartsRoutine._n_5__15}, offset=({_AddRandomPartsRoutine._offsetX_5__13:0.00},{_AddRandomPartsRoutine._offsetZ_5__14:0.00})";
        }

        internal static void TraceShipgenFlow(string message)
        {
            if (!IsShipgenFlowTraceActive())
                return;

            int limit = Config.Param("taf_debug_shipgen_flow_trace_limit", 300);
            if (limit > 0 && _AttemptShipgenFlowTraceCount >= limit)
            {
                if (!_AttemptShipgenFlowTraceSuppressed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    {ShipgenAttemptPrefix()}flow trace suppressed after {limit} entries this attempt");
                    _AttemptShipgenFlowTraceSuppressed = true;
                }
                return;
            }

            _AttemptShipgenFlowSeq++;
            _AttemptShipgenFlowTraceCount++;
            Melon<TweaksAndFixes>.Logger.Msg($"    {ShipgenAttemptPrefix()}flow a{_AttemptShipgenFlowSeq:000}: {message}");
        }

        internal static void TraceShipgenRandPartBegin(RandPart rp)
        {
            if (!IsShipgenFlowTraceActive())
                return;

            TraceShipgenFlow($"randpart begin: {ShipgenRandPartSummary(rp)}, {ShipgenAddRandomPartsFlowState()}");
        }

        internal static void TraceShipgenCandidate(PartData data, RandPart rp, bool accepted, string reason)
        {
            if (!IsShipgenFlowTraceActive())
                return;

            if (!accepted && reason == "candidate_game_filter" && Config.Param("taf_debug_shipgen_flow_trace_rejected_game", 0) == 0)
                return;

            TraceShipgenFlow($"candidate {(accepted ? "accepted" : "rejected")} reason={reason}: {ShipgenPartDataLabel(data)}, {ShipgenRandPartSummary(rp)}, {ShipgenAddRandomPartsFlowState()}");
        }

        internal static void TraceShipgenMount(Part part, Mount mount, bool autoRotate)
        {
            RecordShipgenPartRandPart(part);
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            if (part == null || part.data == null)
                return;

            string mountLabel = mount == null ? "mount=none" : $"mount={mount.name}{(mount.parentPart?.data != null ? $" parent={ShipgenPartLabel(mount.parentPart)}" : string.Empty)}";
            TraceShipgenFlow($"mount: {ShipgenPartKind(part.data)} {ShipgenPartLabel(part)} {ShipgenPartLocation(part, part.transform.position)}, {mountLabel}, {ShipgenPartMountAndRandPart(part)}, autoRotate={autoRotate}, {ShipgenAddRandomPartsFlowState()}");
        }

        internal static void TraceShipgenUnmount(Part part, bool refreshMounts)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            if (part == null || part.data == null)
                return;

            TraceShipgenFlow($"unmount: {ShipgenPartKind(part.data)} {ShipgenPartLabel(part)} {ShipgenPartLocation(part, part.transform.position)}, {ShipgenPartMountAndRandPart(part)}, refreshMounts={refreshMounts}, {ShipgenAddRandomPartsFlowState()}");
        }

        internal static bool IsShipgenMainGunTracePart(Part part)
        {
            if (part == null || part.data == null || !part.data.isGun)
                return false;

            if (part.ship != null && part.ship.IsMainCal(part))
                return true;

            string randPart = _LastRandPart?.name ?? string.Empty;
            return randPart.Contains("main_cal") || randPart.Contains("main_center") || randPart.Contains("main_side");
        }

        internal static bool IsShipgenMainGunCandidate(PartData data)
        {
            if (data == null || !data.isGun)
                return false;

            if (_LastBattery == ShipM.BatteryType.main)
                return true;

            string randPart = _LastRandPart?.name ?? string.Empty;
            return randPart.Contains("main_cal") || randPart.Contains("main_center") || randPart.Contains("main_side");
        }

        internal static void RecordMainGunRejectReason(string reason, PartData data = null)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (data != null && !IsShipgenMainGunCandidate(data))
                return;

            if (string.IsNullOrWhiteSpace(reason))
                reason = "unknown";

            _AttemptMainGunRejectReasons[reason] = _AttemptMainGunRejectReasons.GetValueOrDefault(reason) + 1;
        }

        internal static string MainGunRejectReasonSummary()
        {
            if (_AttemptMainGunRejectReasons.Count == 0)
                return "none";

            return string.Join(", ", _AttemptMainGunRejectReasons
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key)
                .Take(8)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        internal static void CountShipgenLifecyclePart(Part part, bool placed, bool removed, bool final)
        {
            if (part == null || part.data == null)
                return;

            bool isMain = IsShipgenMainGunTracePart(part);
            bool isGun = part.data.isGun;
            bool isTower = part.data.isTowerMain || part.data.isTowerAny;
            bool isFunnel = part.data.isFunnel;

            if (placed)
            {
                if (isMain) _AttemptLifecyclePlacedMain++;
                if (isGun) _AttemptLifecyclePlacedGun++;
                if (isTower) _AttemptLifecyclePlacedTower++;
                if (isFunnel) _AttemptLifecyclePlacedFunnel++;
            }
            if (removed)
            {
                if (isMain) _AttemptLifecycleRemovedMain++;
                if (isGun) _AttemptLifecycleRemovedGun++;
                if (isTower) _AttemptLifecycleRemovedTower++;
                if (isFunnel) _AttemptLifecycleRemovedFunnel++;
            }
            if (final)
            {
                if (isMain) _AttemptLifecycleFinalMain++;
                if (isGun) _AttemptLifecycleFinalGun++;
                if (isTower) _AttemptLifecycleFinalTower++;
                if (isFunnel) _AttemptLifecycleFinalFunnel++;
            }
        }

        internal static string ShipgenAttemptPrefix()
            => _ShipgenAttemptTraceId > 0 ? $"attempt={_ShipgenAttemptTraceId} " : string.Empty;

        internal static int ShipgenAddRandomPartsState()
            => _AddRandomPartsRoutine?.__1__state ?? -1;

        internal static string ShipgenDeckLabel(Ship ship, BoxCollider deck)
        {
            if (deck == null)
                return "deck=none";

            string name = string.IsNullOrWhiteSpace(deck.name) ? "(unnamed)" : deck.name;
            Vector3 deckPos = deck.transform.position;
            Vector3 local = ship?.hull != null ? ship.hull.transform.InverseTransformPoint(deckPos) : deckPos;
            return $"deck={name} deck_z={local.z:0.00}, deck_x={local.x:0.00}";
        }

        internal static string ShipgenRandPartProbeDetails()
        {
            if (_LastRandPart == null)
                return string.Empty;

            string rangeX = _LastRandPart.rangeX.HasValue
                ? $"rangeX={_LastRandPart.rangeX.Value.x:0.###}..{_LastRandPart.rangeX.Value.y:0.###}"
                : "rangeX=none";
            string rangeZ = _LastRandPart.rangeZ.HasValue
                ? $"rangeZ={_LastRandPart.rangeZ.Value.x:0.###}..{_LastRandPart.rangeZ.Value.y:0.###}"
                : "rangeZ=none";

            return $", rp_min={_LastRandPart.min}, rp_max={_LastRandPart.max}, rp_z={_LastRandPart.rangeZFrom:0.###}..{_LastRandPart.rangeZTo:0.###}, {rangeX}, {rangeZ}";
        }

        internal static void TraceShipgenDeckProbe(Ship ship, Vector3 point, BoxCollider deck)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_AddRandomPartsRoutine == null || _LastRandPart == null)
                return;

            string randPart = _LastRandPart.name ?? string.Empty;
            if (!IsShipgenFlowTraceActive() && !randPart.Contains("main_cal") && !randPart.Contains("main_center") && !randPart.Contains("main_side"))
                return;

            int limit = Config.Param("taf_debug_shipgen_deck_probe_trace_limit", 80);
            if (limit > 0 && _AttemptShipgenDeckProbeTraceCount >= limit)
            {
                if (!_AttemptShipgenDeckProbeTraceSuppressed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    {ShipgenAttemptPrefix()}deck probe trace suppressed after {limit} entries this attempt");
                    _AttemptShipgenDeckProbeTraceSuppressed = true;
                }
                return;
            }

            _AttemptShipgenDeckProbeSeq++;
            _AttemptShipgenDeckProbeTraceCount++;

            Vector3 local = ship?.hull != null ? ship.hull.transform.InverseTransformPoint(point) : point;
            string side = Math.Abs(local.x) < 0.01f ? "center" : (local.x < 0f ? "port" : "starboard");
            Melon<TweaksAndFixes>.Logger.Msg(
                $"    {ShipgenAttemptPrefix()}deck probe a{_AttemptShipgenDeckProbeSeq:00}: probe=[{side}] z={local.z:0.00}, x={local.x:0.00}, {ShipgenDeckLabel(ship, deck)}, add_state={ShipgenAddRandomPartsState()}, rp={randPart}{ShipgenRandPartProbeDetails()}");
        }

        internal static void TraceShipgenCanPlace(Part part, bool result, string denyReason)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            if (!IsShipgenFlowTraceActive() && !IsShipgenMainGunTracePart(part))
                return;

            RecordGunRandPartLifecycle(part, "canplace", result);

            int limit = Config.Param("taf_debug_shipgen_canplace_trace_limit", 80);
            if (limit > 0 && _AttemptShipgenCanPlaceTraceCount >= limit)
            {
                if (!_AttemptShipgenCanPlaceTraceSuppressed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    {ShipgenAttemptPrefix()}canplace trace suppressed after {limit} entries this attempt");
                    _AttemptShipgenCanPlaceTraceSuppressed = true;
                }
                return;
            }

            _AttemptShipgenCanPlaceSeq++;
            _AttemptShipgenCanPlaceTraceCount++;

            Melon<TweaksAndFixes>.Logger.Msg(
                $"    {ShipgenAttemptPrefix()}canplace a{_AttemptShipgenCanPlaceSeq:00}: {ShipgenPartLabel(part)} {ShipgenPartLocation(part, part.transform.position)}, {ShipgenPartMountAndRandPart(part)}, add_state={ShipgenAddRandomPartsState()}, result={(result ? "yes" : "no")}{(!result && !string.IsNullOrWhiteSpace(denyReason) ? $":{denyReason}" : string.Empty)}");
        }

        internal static void TraceShipgenPlacement(Part part, Vector3 pos, bool autoRotate)
        {
            RecordShipgenPartRandPart(part);
            RecordGunRandPartLifecycle(part, "placed");
            RecordShipgenRandPartPlacementSuccess(part);
            CountShipgenLifecyclePart(part, true, false, false);

            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            if (part == null || part.data == null)
                return;

            int limit = Config.Param("taf_debug_shipgen_placement_trace_limit", 80);
            if (limit > 0 && _AttemptShipgenPlacementTraceCount >= limit)
            {
                if (!_AttemptShipgenPlacementTraceSuppressed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    placement trace suppressed after {limit} entries this attempt");
                    _AttemptShipgenPlacementTraceSuppressed = true;
                }
                return;
            }

            _ShipgenPlacementSeq++;
            _AttemptShipgenPlacementSeq++;
            _AttemptShipgenPlacementTraceCount++;

            Melon<TweaksAndFixes>.Logger.Msg(
                $"    {ShipgenAttemptPrefix()}place #{_ShipgenPlacementSeq:000}/a{_AttemptShipgenPlacementSeq:00}: {ShipgenPartKind(part.data)} {ShipgenPartLabel(part)} {ShipgenPartLocation(part, pos)}, {ShipgenPartMountAndRandPart(part)}, autoRotate={autoRotate}");
        }

        internal static string ShipgenPlacementValidity(Part part)
        {
            if (!IsShipgenMainGunTracePart(part))
                return string.Empty;

            List<string> checks = new();

            try
            {
                bool canPlace = part.CanPlace(out string denyReason);
                checks.Add($"canPlace={(canPlace ? "yes" : "no")}{(!canPlace && !string.IsNullOrWhiteSpace(denyReason) ? $":{denyReason}" : string.Empty)}");
                if (!canPlace)
                    RecordMainGunRejectReason($"remove_can_place:{(string.IsNullOrWhiteSpace(denyReason) ? "false" : denyReason)}");
            }
            catch (Exception ex)
            {
                checks.Add($"canPlace=error:{ex.GetType().Name}");
            }

            try
            {
                bool soft = part.CanPlaceSoft();
                checks.Add($"soft={(soft ? "yes" : "no")}");
                if (!soft)
                    RecordMainGunRejectReason("remove_can_place_soft:false");
            }
            catch (Exception ex)
            {
                checks.Add($"soft=error:{ex.GetType().Name}");
            }

            try
            {
                bool softLight = part.CanPlaceSoftLight();
                checks.Add($"softLight={(softLight ? "yes" : "no")}");
                if (!softLight)
                    RecordMainGunRejectReason("remove_can_place_soft_light:false");
            }
            catch (Exception ex)
            {
                checks.Add($"softLight=error:{ex.GetType().Name}");
            }

            return checks.Count > 0 ? $", validity={string.Join("/", checks)}" : string.Empty;
        }

        internal static void TraceShipgenRemovedPart(Ship ship, Part part)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            if (part == null || part.data == null)
                return;

            RecordGunRandPartLifecycle(part, "removed");
            CountShipgenLifecyclePart(part, false, true, false);

            int limit = Config.Param("taf_debug_shipgen_remove_trace_limit", 80);
            if (limit > 0 && _AttemptShipgenRemoveTraceCount >= limit)
            {
                if (!_AttemptShipgenRemoveTraceSuppressed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    {ShipgenAttemptPrefix()}remove trace suppressed after {limit} entries this attempt");
                    _AttemptShipgenRemoveTraceSuppressed = true;
                }
                return;
            }

            _AttemptShipgenRemoveSeq++;
            _AttemptShipgenRemoveTraceCount++;

            Melon<TweaksAndFixes>.Logger.Msg(
                $"    {ShipgenAttemptPrefix()}remove a{_AttemptShipgenRemoveSeq:00}: {ShipgenPartKind(part.data)} {ShipgenPartLabel(part)} {ShipgenPartLocation(part, part.transform.position)}, {ShipgenPartMountAndRandPart(part)}, remaining={ship?.parts?.Count ?? -1}{ShipgenPlacementValidity(part)}");
        }

        internal static void TraceShipgenFinalPart(Part part)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            if (part == null || part.data == null)
                return;

            RecordGunRandPartLifecycle(part, "final");

            int limit = Config.Param("taf_debug_shipgen_placement_trace_limit", 80);
            if (limit > 0 && _ShipgenFinalPartTraceCount >= limit)
            {
                if (!_ShipgenFinalPartTraceSuppressed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    final part trace suppressed after {limit} entries this shipgen");
                    _ShipgenFinalPartTraceSuppressed = true;
                }
                return;
            }

            _ShipgenFinalPartSeq++;
            _AttemptShipgenFinalPartSeq++;
            _ShipgenFinalPartTraceCount++;
            CountShipgenLifecyclePart(part, false, false, true);

            Melon<TweaksAndFixes>.Logger.Msg(
                $"    {ShipgenAttemptPrefix()}final #{_ShipgenFinalPartSeq:000}/a{_AttemptShipgenFinalPartSeq:00}: {ShipgenPartKind(part.data)} {ShipgenPartLabel(part)} {ShipgenPartLocation(part, part.transform.position)}, {ShipgenPartMountAndRandPart(part, false)}");
        }

        internal static void PrintShipgenLifecycleSummary(Ship ship)
        {
            if (!IsShipgenPlacementTraceEnabled())
                return;

            int survivingMainTurrets = 0;
            int survivingMainBarrels = 0;
            int survivingGuns = 0;
            int survivingTowers = 0;
            int survivingFunnels = 0;

            if (ship != null)
            {
                CountMainGuns(ship, out survivingMainTurrets, out survivingMainBarrels);
                foreach (var part in ship.parts)
                {
                    if (part?.data == null)
                        continue;

                    if (part.data.isGun)
                        survivingGuns++;
                    if (part.data.isTowerMain || part.data.isTowerAny)
                        survivingTowers++;
                    if (part.data.isFunnel)
                        survivingFunnels++;
                }
            }

            Melon<TweaksAndFixes>.Logger.Msg(
                $"  lifecycle attempt={_ShipgenAttemptTraceId}: placed main={_AttemptLifecyclePlacedMain}, guns={_AttemptLifecyclePlacedGun}, towers={_AttemptLifecyclePlacedTower}, funnels={_AttemptLifecyclePlacedFunnel}; removed main={_AttemptLifecycleRemovedMain}, guns={_AttemptLifecycleRemovedGun}, towers={_AttemptLifecycleRemovedTower}, funnels={_AttemptLifecycleRemovedFunnel}; final_hook main={_AttemptLifecycleFinalMain}, guns={_AttemptLifecycleFinalGun}, towers={_AttemptLifecycleFinalTower}, funnels={_AttemptLifecycleFinalFunnel}; surviving main={survivingMainTurrets}/{survivingMainBarrels}, guns={survivingGuns}, towers={survivingTowers}, funnels={survivingFunnels}");
            Melon<TweaksAndFixes>.Logger.Msg($"  main-gun reject reasons attempt={_ShipgenAttemptTraceId}: {MainGunRejectReasonSummary()}");
        }

        internal static void CountMainGuns(Ship ship, out int numMainTurrets, out int numMainBarrels)
        {
            numMainTurrets = 0;
            numMainBarrels = 0;

            foreach (var part in ship.parts)
            {
                if (!part.data.isGun) continue;
                if (!ship.IsMainCal(part)) continue;

                numMainTurrets++;
                numMainBarrels += part.data.barrels;
            }
        }

        internal static void PrintShipgenFinalGunParts(Ship ship)
        {
            if (ship == null || ship.parts == null)
                return;

            Dictionary<string, (PartData data, float caliber, int count, SortedSet<string> randParts)> mainGuns = new();
            Dictionary<string, (PartData data, float caliber, int count, SortedSet<string> randParts)> otherGuns = new();

            foreach (var part in ship.parts)
            {
                if (part == null || part.data == null || !part.data.isGun)
                    continue;

                PartData data = part.data;
                float caliber = data.GetCaliberInch(ship);
                string key = $"{data.name}|{data.barrels}|{caliber:0.###}";
                var target = ship.IsMainCal(part) ? mainGuns : otherGuns;
                string randPartName = ShipgenFinalGunRandPartName(part, ship.IsMainCal(part));
                if (target.TryGetValue(key, out var entry))
                {
                    if (!string.IsNullOrWhiteSpace(randPartName))
                        entry.randParts.Add(randPartName.Split('/')[0]);

                    target[key] = (entry.data, entry.caliber, entry.count + 1, entry.randParts);
                }
                else
                {
                    SortedSet<string> randParts = new();
                    if (!string.IsNullOrWhiteSpace(randPartName))
                        randParts.Add(randPartName.Split('/')[0]);

                    target[key] = (data, caliber, 1, randParts);
                }
            }

            string FormatGun((PartData data, float caliber, int count, SortedSet<string> randParts) entry)
            {
                string uiName = string.IsNullOrWhiteSpace(entry.data.nameUi) ? entry.data.name : entry.data.nameUi;
                string randParts = entry.randParts.Count == 0 ? "?" : string.Join(",", entry.randParts);
                return $"{entry.count}x {entry.caliber:0.#}\"/{entry.data.barrels} {uiName} [part={entry.data.name} rp={randParts}]";
            }

            List<string> mainSummary = mainGuns.Values
                .OrderByDescending(entry => entry.caliber)
                .ThenByDescending(entry => entry.data.barrels)
                .ThenBy(entry => entry.data.name)
                .Select(FormatGun)
                .ToList();
            if (mainSummary.Count > 0)
                Melon<TweaksAndFixes>.Logger.Msg($"  Main gun parts: {string.Join("; ", mainSummary)}");

            List<string> otherSummary = otherGuns.Values
                .OrderByDescending(entry => entry.caliber)
                .ThenByDescending(entry => entry.data.barrels)
                .ThenBy(entry => entry.data.name)
                .Take(12)
                .Select(FormatGun)
                .ToList();
            if (otherSummary.Count > 0)
            {
                int hidden = otherGuns.Count - otherSummary.Count;
                Melon<TweaksAndFixes>.Logger.Msg($"  Other gun parts: {string.Join("; ", otherSummary)}{(hidden > 0 ? $"; +{hidden} more groups" : string.Empty)}");
            }
        }

        internal static bool HasMainTower(Ship ship)
        {
            var towerMain = G.GameData.stats["tower_main"];
            var stat = ship.stats.GetValueOrDefault(towerMain);
            return stat != null && stat.total > 0;
        }

        internal static bool HasCheckedMainGunCandidates()
        {
            foreach (var stats in _RandPartCandidateStats.Values)
            {
                if (stats.IsMainGun && stats.seen > 0)
                    return true;
            }

            return false;
        }

        internal static void CountMainGunCandidateStats(out int seen, out int accepted)
        {
            seen = 0;
            accepted = 0;

            foreach (var stats in _RandPartCandidateStats.Values)
            {
                if (!stats.IsMainGun)
                    continue;

                seen += stats.seen;
                accepted += stats.accepted;
            }
        }

        internal static bool ShouldFastRetryTowerBeforeMainGun(Ship ship)
        {
            if (!Config.ShipGenTweaks || (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null))
                return false;

            if (Config.Param("taf_shipgen_fast_retry_tower_before_main_guns", 1) == 0)
                return false;

            if (_FastRetriedTowerBeforeMainGun)
                return false;

            if (ship == null || ship.hull == null || ship.hull.data == null)
                return false;

            if (ship.hull.data.minMainTurrets <= 0 && ship.hull.data.minMainBarrels <= 0)
                return false;

            CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);
            if (numMainTurrets != 0 || numMainBarrels != 0 || !HasMainTower(ship) || !HasCheckedMainGunCandidates())
                return false;

            float minSeconds = Config.Param("taf_shipgen_fast_retry_min_seconds", 3f);
            if (_AttemptStartedAt > 0f && Time.realtimeSinceStartup - _AttemptStartedAt < minSeconds)
                return false;

            CountMainGunCandidateStats(out int seen, out int accepted);
            int minCandidateChecks = Config.Param("taf_shipgen_fast_retry_main_gun_candidate_checks", 60);
            return seen >= minCandidateChecks && accepted > 0;
        }

        internal static void ResetShipgenAttemptGunLimiter(Ship ship)
        {
            if (!Config.ShipGenTweaks || ship == null)
                return;

            _LastRandPart = null;
            _LastRPIsGun = false;
            _LastBattery = ShipM.BatteryType.main;
            _AttemptMainGunMaxAcceptedCaliber = 0;
            _AttemptMainGunMinAcceptedCaliber = 0;
            _AttemptTowerMaxAcceptedWeight = 0f;
            _AttemptTowerMinAcceptedWeight = 0f;
            _AttemptTowerMaxAcceptedTier = 0;
            _AttemptTowerMinSeenTier = 0;
            _AttemptTowerRejectedByDownsize = 0;
            _AttemptGunRejectedByLowerMark = 0;
            ResetShipgenPlacementTrace(false);
            _AttemptTowerMaxAcceptedTierByFamily.Clear();
            _AttemptTowerMinSeenTierByFamily.Clear();
            _ShipgenTopGunMarkByBattery.Clear();
            ResetShipgenRandPartAttemptStats();
            _GenGunInfo.FillFor(ship);
        }

        internal static void ResetShipgenMainGunDownsize()
        {
            _MainGunPlacementFailures = 0;
            _MainGunDownsizeReferenceCaliber = 0;
            _MainGunDownsizeMinimumCaliber = 0;
            _AttemptMainGunMaxAcceptedCaliber = 0;
            _AttemptMainGunMinAcceptedCaliber = 0;
            _TowerDownsizeReferenceWeight = 0f;
            _TowerDownsizeMinimumWeight = 0f;
            _AttemptTowerMaxAcceptedWeight = 0f;
            _AttemptTowerMinAcceptedWeight = 0f;
            _TowerDownsizeReferenceTier = 0;
            _TowerDownsizeMinimumTier = 0;
            _AttemptTowerMaxAcceptedTier = 0;
            _AttemptTowerMinSeenTier = 0;
            _AttemptTowerRejectedByDownsize = 0;
            _AttemptGunRejectedByLowerMark = 0;
            ResetShipgenPlacementTrace(true);
            _TowerDownsizeReferenceTierByFamily.Clear();
            _TowerDownsizeMinimumTierByFamily.Clear();
            _AttemptTowerMaxAcceptedTierByFamily.Clear();
            _AttemptTowerMinSeenTierByFamily.Clear();
            _ShipgenTopGunMarkByBattery.Clear();
        }

        internal static int CurrentShipgenDownsizeSteps()
        {
            int afterFailures = Config.Param("taf_shipgen_main_gun_downsize_after_failures", 1);
            if (_MainGunPlacementFailures < afterFailures)
                return 0;

            return _MainGunPlacementFailures - afterFailures + 1;
        }

        internal static int CurrentShipgenMainGunDownsizeCap()
        {
            int step = Config.Param("taf_shipgen_main_gun_downsize_step_inches", 2);
            int downsizeSteps = CurrentShipgenDownsizeSteps();

            if (step <= 0 || _MainGunDownsizeReferenceCaliber <= 0 || downsizeSteps <= 0)
                return ApplyProfileMainGunCap(0);

            int penalty = downsizeSteps * step;
            int floor = _MainGunDownsizeMinimumCaliber > 0 ? _MainGunDownsizeMinimumCaliber : 1;
            return ApplyProfileMainGunCap(Math.Max(floor, _MainGunDownsizeReferenceCaliber - penalty));
        }

        internal static bool IsShipgenTowerPart(PartData partData)
            => partData != null && partData.isTowerAny;

        internal static int RomanNumeralValue(char c)
        {
            switch (c)
            {
                case 'I': return 1;
                case 'V': return 5;
                case 'X': return 10;
                default: return 0;
            }
        }

        internal static int ParseRomanNumeralTier(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            string normalized = text
                .Replace("\u0399", "I")
                .Replace("\u03b9", "I")
                .Replace("\u2160", "I")
                .Replace("\u2161", "II")
                .Replace("\u2162", "III")
                .Replace("\u2163", "IV")
                .Replace("\u2164", "V")
                .Replace("\u2165", "VI")
                .Replace("\u2166", "VII")
                .Replace("\u2167", "VIII");
            char[] separators = new[] { ' ', '\t', '(', ')', '[', ']', '{', '}', '-', '_', ',', '.', ';', ':' };
            string[] tokens = normalized.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            for (int i = tokens.Length - 1; i >= 0; i--)
            {
                string token = tokens[i].Trim().ToUpperInvariant();
                if (token.Length == 0 || token.Length > 6)
                    continue;

                int total = 0;
                int previous = 0;
                bool valid = true;
                for (int j = token.Length - 1; j >= 0; j--)
                {
                    int value = RomanNumeralValue(token[j]);
                    if (value == 0)
                    {
                        valid = false;
                        break;
                    }

                    total += value < previous ? -value : value;
                    previous = Math.Max(previous, value);
                }

                if (valid && total > 0 && total <= 20)
                    return total;
            }

            return 0;
        }

        internal static int ShipgenTowerTier(PartData partData)
        {
            if (!IsShipgenTowerPart(partData))
                return 0;

            int tier = ParseRomanNumeralTier(partData.nameUi);
            return tier > 0 ? tier : ParseRomanNumeralTier(partData.name);
        }

        internal static string ShipgenTowerFamily(PartData partData)
        {
            if (!IsShipgenTowerPart(partData))
                return string.Empty;

            string text = !string.IsNullOrWhiteSpace(partData.nameUi) ? partData.nameUi : partData.name;
            if (string.IsNullOrWhiteSpace(text))
                return partData.model ?? string.Empty;

            string normalized = text
                .Replace("\u0399", "I")
                .Replace("\u03b9", "I")
                .Replace("\u2160", "I")
                .Replace("\u2161", "II")
                .Replace("\u2162", "III")
                .Replace("\u2163", "IV")
                .Replace("\u2164", "V")
                .Replace("\u2165", "VI")
                .Replace("\u2166", "VII")
                .Replace("\u2167", "VIII");
            char[] separators = new[] { ' ', '\t', '(', ')', '[', ']', '{', '}', '-', '_', ',', '.', ';', ':' };
            string[] tokens = normalized.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            List<string> family = new();

            foreach (string raw in tokens)
            {
                string token = raw.Trim();
                if (token.Length == 0)
                    continue;

                string upper = token.ToUpperInvariant();
                if (ParseRomanNumeralTier(upper) > 0 || upper == "ENHANCED")
                    continue;

                family.Add(upper.ToLowerInvariant());
            }

            return family.Count > 0 ? string.Join(" ", family) : (partData.model ?? partData.name ?? string.Empty);
        }

        internal static float ShipgenTowerWeight(PartData partData)
        {
            if (!IsShipgenTowerPart(partData))
                return 0f;

            return Math.Max(0f, partData.weight);
        }

        internal static int CurrentShipgenTowerDownsizeTierCap()
        {
            int step = Config.Param("taf_shipgen_tower_downsize_step_tiers", 2);
            int downsizeSteps = CurrentShipgenDownsizeSteps();

            if (step <= 0 || _TowerDownsizeReferenceTier <= 0 || downsizeSteps <= 0)
                return 0;

            int penalty = downsizeSteps * step;
            int floor = _TowerDownsizeMinimumTier > 0 ? _TowerDownsizeMinimumTier : 1;
            return Math.Max(floor, _TowerDownsizeReferenceTier - penalty);
        }

        internal static int CurrentShipgenTowerDownsizeTierCap(PartData partData)
        {
            int step = Config.Param("taf_shipgen_tower_downsize_step_tiers", 2);
            int downsizeSteps = CurrentShipgenDownsizeSteps();
            string family = ShipgenTowerFamily(partData);

            if (step <= 0 || downsizeSteps <= 0 || string.IsNullOrEmpty(family) || !_TowerDownsizeReferenceTierByFamily.TryGetValue(family, out int reference) || reference <= 0)
                return ApplyProfileTowerTierCap(partData, 0);

            int floor = _TowerDownsizeMinimumTierByFamily.TryGetValue(family, out int minimum) && minimum > 0 ? minimum : 1;
            return ApplyProfileTowerTierCap(partData, Math.Max(floor, reference - (downsizeSteps * step)));
        }

        internal static string CurrentShipgenTowerDownsizeTierSummary()
        {
            if (_TowerDownsizeReferenceTierByFamily.Count == 0 || CurrentShipgenDownsizeSteps() <= 0)
                return string.Empty;

            List<string> caps = new();
            foreach (var kvp in _TowerDownsizeReferenceTierByFamily.OrderBy(kvp => kvp.Key))
            {
                if (kvp.Value <= 0)
                    continue;

                int step = Config.Param("taf_shipgen_tower_downsize_step_tiers", 2);
                if (step <= 0)
                    continue;

                int floor = _TowerDownsizeMinimumTierByFamily.TryGetValue(kvp.Key, out int minimum) && minimum > 0 ? minimum : 1;
                int cap = Math.Max(floor, kvp.Value - (CurrentShipgenDownsizeSteps() * step));
                caps.Add($"{kvp.Key}<={cap}");
            }

            return caps.Count > 0 ? string.Join("; ", caps.Take(4)) : string.Empty;
        }

        internal static float CurrentShipgenTowerDownsizeWeightCap()
        {
            float stepRatio = Config.Param("taf_shipgen_tower_downsize_step_ratio", 0.15f);
            int downsizeSteps = CurrentShipgenDownsizeSteps();

            if (stepRatio <= 0f || _TowerDownsizeReferenceWeight <= 0f || downsizeSteps <= 0)
                return 0f;

            float cap = _TowerDownsizeReferenceWeight * Math.Max(0.1f, 1f - (downsizeSteps * stepRatio));
            float floor = _TowerDownsizeMinimumWeight > 0f ? _TowerDownsizeMinimumWeight : 0f;
            return Math.Max(floor, cap);
        }

        internal static bool ShouldRejectMainGunByDownsize(ShipM.BatteryType battery, int partCal)
        {
            if (!Config.ShipGenTweaks || (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null) || battery != ShipM.BatteryType.main)
                return false;

            int cap = CurrentShipgenMainGunDownsizeCap();
            return cap > 0 && partCal > cap;
        }

        internal static bool ShouldRejectTowerByDownsize(PartData partData)
        {
            if (!Config.ShipGenTweaks || (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null) || !IsShipgenTowerPart(partData))
                return false;

            int tierCap = CurrentShipgenTowerDownsizeTierCap(partData);
            int tier = ShipgenTowerTier(partData);
            if (tierCap > 0 && tier > 0)
                return tier > tierCap;

            float cap = CurrentShipgenTowerDownsizeWeightCap();
            float weight = ShipgenTowerWeight(partData);
            return cap > 0f && weight > cap;
        }

        internal static bool ShouldRejectShipgenGunByNonWholeCaliber(PartData partData, ShipM.BatteryType battery)
        {
            if (!Config.ShipGenTweaks || _GenerateRandomShipRoutine == null || Config.Param("taf_shipgen_whole_inch_gun_calibers", 1) == 0)
                return false;

            if (partData == null || !partData.isGun)
                return false;

            float caliber = partData.caliber * (1f / 25.4f);
            return Math.Abs(caliber - Mathf.Round(caliber)) > 0.01f;
        }

        internal static string ShipgenGunMarkCacheKey(Ship ship, ShipM.BatteryType battery, PartData partData)
            => $"{ShipgenPartPointer(ship?.hull)}:{battery}:{Ship.IsCasemateGun(partData)}";

        internal static bool IsShipgenGunMarkCandidate(Ship ship, PartData partData, ShipM.BatteryType battery)
        {
            if (ship == null || partData == null || !partData.isGun)
                return false;

            if (battery == ShipM.BatteryType.main && !ship.IsMainCal(partData))
                return false;

            if (battery != ShipM.BatteryType.main && ship.IsMainCal(partData))
                return false;

            if (ShouldRejectShipgenGunByNonWholeCaliber(partData, battery))
                return false;

            if (_GenGunInfo.isLimited && !_GenGunInfo.CaliberOK(battery, partData))
                return false;

            return ship.IsPartAvailable(partData);
        }

        internal static int TopShipgenGunMarkForBattery(Ship ship, ShipM.BatteryType battery, PartData candidate)
        {
            if (ship == null || candidate == null || !candidate.isGun)
                return 0;

            string key = ShipgenGunMarkCacheKey(ship, battery, candidate);
            if (_ShipgenTopGunMarkByBattery.TryGetValue(key, out int cached))
                return cached;

            bool casemate = Ship.IsCasemateGun(candidate);
            int top = 0;
            foreach (var partData in G.GameData.parts.Values)
            {
                if (partData == null || !partData.isGun || Ship.IsCasemateGun(partData) != casemate)
                    continue;

                if (!IsShipgenGunMarkCandidate(ship, partData, battery))
                    continue;

                top = Math.Max(top, ship.TechGunGrade(partData, false));
            }

            _ShipgenTopGunMarkByBattery[key] = top;
            return top;
        }

        internal static bool ShouldRejectShipgenGunByLowerMark(Ship ship, PartData partData, ShipM.BatteryType battery)
        {
            if (!Config.ShipGenTweaks || (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null) || Config.Param("taf_shipgen_top_gun_mark_only", 1) == 0)
                return false;

            if (ship == null || partData == null || !partData.isGun)
                return false;

            int topMark = TopShipgenGunMarkForBattery(ship, battery, partData);
            if (topMark <= 0)
                return false;

            int mark = ship.TechGunGrade(partData, false);
            return mark > 0 && mark < topMark;
        }

        internal static void NormalizeShipgenGunCaliberModifiers(Ship ship, PartData partData = null, bool logChanges = true)
        {
            if (!IsShipgenGunNormalizationActive())
                return;

            if (ship == null || ship.shipGunCaliber == null)
                return;

            bool normalizeDiameter = Config.Param("taf_shipgen_whole_inch_gun_calibers", 1) != 0;
            bool normalizeLength = Config.Param("taf_shipgen_standard_gun_lengths", 1) != 0;

            if (!normalizeDiameter && !normalizeLength)
                return;

            foreach (var caliber in ship.shipGunCaliber)
            {
                if (caliber == null || caliber.turretPartData == null)
                    continue;

                if (partData != null && caliber.turretPartData != partData)
                    continue;

                if (normalizeDiameter && Math.Abs(caliber.diameter) > 0.001f)
                {
                    if (logChanges && IsShipgenGunNormalizationDebugEnabled())
                    {
                        float oldCaliber = caliber.turretPartData.GetCaliberInch(ship);
                        Melon<TweaksAndFixes>.Logger.Msg($"  Whole-inch guns: reset generated diameter modifier for {oldCaliber:0.##}\" {(caliber.isCasemateGun ? "casemate" : "turret")}");
                    }

                    ship.SetCaliberDiameter(caliber, 0f);
                }

                if (normalizeLength && Math.Abs(caliber.length) > 0.001f)
                {
                    if (logChanges && IsShipgenGunNormalizationDebugEnabled())
                    {
                        float caliberInch = caliber.turretPartData.GetCaliberInch(ship);
                        Melon<TweaksAndFixes>.Logger.Msg($"  Standard gun lengths: reset generated length modifier for {caliberInch:0.##}\" {(caliber.isCasemateGun ? "casemate" : "turret")}");
                    }

                    ship.SetCaliberLength(caliber, 0f);
                }
            }
        }

        internal static void RecordAcceptedMainGunCandidate(RandPart rp, int partCal)
        {
            var stats = CandidateStatsFor(rp);
            if (!stats.IsMainGun)
                return;

            stats.maxAcceptedCaliber = Math.Max(stats.maxAcceptedCaliber, partCal);
            _AttemptMainGunMaxAcceptedCaliber = Math.Max(_AttemptMainGunMaxAcceptedCaliber, partCal);
            if (partCal > 0)
                _AttemptMainGunMinAcceptedCaliber = _AttemptMainGunMinAcceptedCaliber > 0 ? Math.Min(_AttemptMainGunMinAcceptedCaliber, partCal) : partCal;
        }

        internal static void RecordAcceptedTowerCandidate(PartData partData)
        {
            int tier = ShipgenTowerTier(partData);
            if (tier > 0)
            {
                _AttemptTowerMaxAcceptedTier = Math.Max(_AttemptTowerMaxAcceptedTier, tier);
                string family = ShipgenTowerFamily(partData);
                if (!string.IsNullOrEmpty(family))
                    _AttemptTowerMaxAcceptedTierByFamily[family] = Math.Max(_AttemptTowerMaxAcceptedTierByFamily.GetValueOrDefault(family), tier);
            }

            float weight = ShipgenTowerWeight(partData);
            if (weight <= 0f)
                return;

            _AttemptTowerMaxAcceptedWeight = Math.Max(_AttemptTowerMaxAcceptedWeight, weight);
            _AttemptTowerMinAcceptedWeight = _AttemptTowerMinAcceptedWeight > 0f ? Math.Min(_AttemptTowerMinAcceptedWeight, weight) : weight;
        }

        internal static void RecordSeenTowerCandidate(PartData partData)
        {
            int tier = ShipgenTowerTier(partData);
            if (tier <= 0)
                return;

            _AttemptTowerMinSeenTier = _AttemptTowerMinSeenTier > 0 ? Math.Min(_AttemptTowerMinSeenTier, tier) : tier;
            string family = ShipgenTowerFamily(partData);
            if (!string.IsNullOrEmpty(family))
            {
                int current = _AttemptTowerMinSeenTierByFamily.GetValueOrDefault(family);
                _AttemptTowerMinSeenTierByFamily[family] = current > 0 ? Math.Min(current, tier) : tier;
            }
        }

        internal static void RecordShipgenAttemptMainGunResult(Ship ship)
        {
            if (!Config.ShipGenTweaks || ship == null || ship.hull == null || ship.hull.data == null)
                return;

            CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);
            bool missingRequiredMainGuns =
                (ship.hull.data.minMainTurrets > 0 && numMainTurrets < ship.hull.data.minMainTurrets) ||
                (ship.hull.data.minMainBarrels > 0 && numMainBarrels < ship.hull.data.minMainBarrels);
            bool overweight = ship.Weight() > ship.Tonnage();
            bool invalidWeightOffset = !IsWithinShipgenInstabilityTolerance(ship);
            bool invalidParts = false;
            try
            {
                ship.IsValidCostReqParts(
                    out _,
                    out _,
                    out Il2CppSystem.Collections.Generic.Dictionary<Part, string> badParts);
                invalidParts = badParts != null && badParts.Count > 0;
            }
            catch
            {
                invalidParts = false;
            }

            if (!missingRequiredMainGuns && !overweight && !invalidWeightOffset && !invalidParts)
            {
                _MainGunPlacementFailures = 0;
                return;
            }

            _MainGunPlacementFailures++;
            if (_AttemptMainGunMaxAcceptedCaliber > 0)
                _MainGunDownsizeReferenceCaliber = Math.Max(_MainGunDownsizeReferenceCaliber, _AttemptMainGunMaxAcceptedCaliber);
            if (_AttemptMainGunMinAcceptedCaliber > 0)
                _MainGunDownsizeMinimumCaliber = _MainGunDownsizeMinimumCaliber > 0 ? Math.Min(_MainGunDownsizeMinimumCaliber, _AttemptMainGunMinAcceptedCaliber) : _AttemptMainGunMinAcceptedCaliber;
            if (_AttemptTowerMaxAcceptedWeight > 0f)
                _TowerDownsizeReferenceWeight = Math.Max(_TowerDownsizeReferenceWeight, _AttemptTowerMaxAcceptedWeight);
            if (_AttemptTowerMinAcceptedWeight > 0f)
                _TowerDownsizeMinimumWeight = _TowerDownsizeMinimumWeight > 0f ? Math.Min(_TowerDownsizeMinimumWeight, _AttemptTowerMinAcceptedWeight) : _AttemptTowerMinAcceptedWeight;
            if (_AttemptTowerMaxAcceptedTier > 0)
                _TowerDownsizeReferenceTier = Math.Max(_TowerDownsizeReferenceTier, _AttemptTowerMaxAcceptedTier);
            if (_AttemptTowerMinSeenTier > 0)
                _TowerDownsizeMinimumTier = _TowerDownsizeMinimumTier > 0 ? Math.Min(_TowerDownsizeMinimumTier, _AttemptTowerMinSeenTier) : _AttemptTowerMinSeenTier;
            foreach (var kvp in _AttemptTowerMaxAcceptedTierByFamily)
                _TowerDownsizeReferenceTierByFamily[kvp.Key] = Math.Max(_TowerDownsizeReferenceTierByFamily.GetValueOrDefault(kvp.Key), kvp.Value);
            foreach (var kvp in _AttemptTowerMinSeenTierByFamily)
            {
                int current = _TowerDownsizeMinimumTierByFamily.GetValueOrDefault(kvp.Key);
                _TowerDownsizeMinimumTierByFamily[kvp.Key] = current > 0 ? Math.Min(current, kvp.Value) : kvp.Value;
            }

            int cap = CurrentShipgenMainGunDownsizeCap();
            int towerTierCap = CurrentShipgenTowerDownsizeTierCap();
            float towerCap = CurrentShipgenTowerDownsizeWeightCap();
            string towerTierSummary = CurrentShipgenTowerDownsizeTierSummary();
            if (IsShipgenDebugEnabled() && (cap > 0 || towerTierSummary.Length > 0 || towerTierCap > 0 || towerCap > 0f))
            {
                string reason = string.Join(", ", new[] { missingRequiredMainGuns ? "missing main guns" : string.Empty, overweight ? "overweight" : string.Empty, invalidWeightOffset ? "weight offset" : string.Empty, invalidParts ? "invalid parts" : string.Empty }.Where(s => s.Length > 0));
                string gun = cap > 0 ? $"main_gun_cap<={cap}\"{(_MainGunDownsizeMinimumCaliber > 0 ? $", main_gun_floor>={_MainGunDownsizeMinimumCaliber}\"" : string.Empty)}" : "main_gun_cap=none";
                string tower = towerTierSummary.Length > 0 ? $"tower_tier_caps={towerTierSummary}" :
                    towerTierCap > 0 ? $"tower_tier_cap<={towerTierCap}{(_TowerDownsizeMinimumTier > 0 ? $", tower_tier_floor>={_TowerDownsizeMinimumTier}" : string.Empty)}" :
                    towerCap > 0f ? $"tower_weight_cap<={towerCap:0.#}t{(_TowerDownsizeMinimumWeight > 0f ? $", tower_floor>={_TowerDownsizeMinimumWeight:0.#}t" : string.Empty)}" : "tower_cap=none";
                Melon<TweaksAndFixes>.Logger.Msg($"  Gun/tower downsize next attempt: failures={_MainGunPlacementFailures} ({reason}), {gun}, {tower}");
            }
        }

        internal static RandPartCandidateStats CandidateStatsFor(RandPart rp)
        {
            string name = rp?.name ?? "<null>";
            if (!_RandPartCandidateStats.TryGetValue(name, out var stats))
            {
                stats = new RandPartCandidateStats
                {
                    name = name,
                    type = rp?.type ?? string.Empty,
                    condition = rp?.condition ?? string.Empty
                };
                _RandPartCandidateStats[name] = stats;
            }

            return stats;
        }

        internal static RandPartCandidateStats CandidateStatsForName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "<unknown>";

            if (!_RandPartCandidateStats.TryGetValue(name, out var stats))
            {
                string[] parts = name.Split('/');
                stats = new RandPartCandidateStats
                {
                    name = name,
                    type = parts.Length > 1 ? parts[1] : string.Empty,
                    condition = name.Contains("main_cal") ? "main_cal" : string.Empty
                };
                _RandPartCandidateStats[name] = stats;
            }

            return stats;
        }

        internal static IntPtr ShipgenPartPointer(Part part)
        {
            try
            {
                return part?.Pointer ?? IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        internal static string ShipgenRandPartNameForPart(Part part, bool allowCurrentRandPart)
        {
            IntPtr pointer = ShipgenPartPointer(part);
            if (pointer != IntPtr.Zero && _ShipgenPartRandPartByPointer.TryGetValue(pointer, out string mappedName))
                return mappedName;

            string current = allowCurrentRandPart ? _LastRandPart?.name ?? string.Empty : string.Empty;
            if (!string.IsNullOrWhiteSpace(current))
            {
                if (pointer != IntPtr.Zero)
                    _ShipgenPartRandPartByPointer[pointer] = current;

                return current;
            }

            return string.Empty;
        }

        internal static string ShipgenFinalGunRandPartName(Part part, bool isMainGun)
        {
            string randPartName = ShipgenRandPartNameForPart(part, false);
            if (string.IsNullOrWhiteSpace(randPartName))
                return string.Empty;

            if (isMainGun && !IsMainGunRandPartName(randPartName))
                return string.Empty;

            if (!isMainGun && IsMainGunRandPartName(randPartName))
                return string.Empty;

            return randPartName;
        }

        internal static void RecordShipgenPartRandPart(Part part)
        {
            if (_GenerateRandomShipRoutine == null && _AddRandomPartsRoutine == null)
                return;

            IntPtr pointer = ShipgenPartPointer(part);
            string current = _LastRandPart?.name ?? string.Empty;
            if (pointer == IntPtr.Zero || string.IsNullOrWhiteSpace(current))
                return;

            if (part?.data?.isGun == true)
            {
                bool currentIsMainGunRandPart = IsMainGunRandPartName(current);
                if (part.ship != null && part.ship.IsMainCal(part) && !currentIsMainGunRandPart)
                    return;

                if (_ShipgenPartRandPartByPointer.TryGetValue(pointer, out string existing) && IsMainGunRandPartName(existing) && !currentIsMainGunRandPart)
                    return;
            }

            _ShipgenPartRandPartByPointer[pointer] = current;
        }

        internal static void RecordGunRandPartLifecycle(Part part, string phase, bool? canPlaceResult = null)
        {
            if (part == null || part.data == null || !part.data.isGun)
                return;

            string randPartName = ShipgenRandPartNameForPart(part, phase != "final");
            if (string.IsNullOrWhiteSpace(randPartName))
                return;

            var stats = CandidateStatsForName(randPartName);
            if (!stats.IsGun)
                return;

            switch (phase)
            {
                case "canplace":
                    stats.canPlaceChecks++;
                    if (canPlaceResult == true)
                        stats.canPlaceYes++;
                    else
                        stats.canPlaceNo++;
                    break;
                case "placed":
                    stats.placed++;
                    break;
                case "removed":
                    stats.removed++;
                    break;
                case "final":
                    stats.final++;
                    break;
            }
        }

        internal static void ClearRandPartCandidateStats()
        {
            _RandPartCandidateStats.Clear();
        }

        internal static void PrintAndClearRandPartCandidateStats()
        {
            if (!IsShipgenDebugEnabled() || _RandPartCandidateStats.Count == 0)
            {
                ClearRandPartCandidateStats();
                return;
            }

            bool printedHeader = false;
            foreach (var stats in _RandPartCandidateStats.Values)
            {
                if (!stats.IsMainGun)
                    continue;

                if (!printedHeader)
                {
                    printedHeader = true;
                    Melon<TweaksAndFixes>.Logger.Msg("  Main gun randpart candidate diagnostics:");
                }

                Melon<TweaksAndFixes>.Logger.Msg($"    {stats.name}: seen={stats.seen}, accepted={stats.accepted}, rejected_game={stats.rejectedByGame}, rejected_taf_caliber={stats.rejectedByTAFCaliber}");
            }

            ClearRandPartCandidateStats();
        }

        internal static string SummarizeAndClearRandPartCandidateStats()
        {
            int randParts = 0;
            int seen = 0;
            int accepted = 0;
            int rejectedByGame = 0;
            int rejectedByTAFCaliber = 0;
            int rejectedByTAFDownsize = 0;
            int rejectedByTAFTowerDownsize = 0;
            int rejectedByTAFNonWholeCaliber = 0;
            int rejectedByTAFLowerMark = 0;
            int rejectedByTAFSkippedRandPart = 0;
            int canPlaceChecks = 0;
            int canPlaceYes = 0;
            int canPlaceNo = 0;
            int placed = 0;
            int removed = 0;
            int final = 0;
            Dictionary<string, RandPartCandidateStats> layoutTotals = new();
            Dictionary<string, RandPartCandidateStats> gunRoleTotals = new();
            List<RandPartCandidateStats> gunStats = new();
            List<RandPartCandidateStats> mainGunStats = new();

            foreach (var stats in _RandPartCandidateStats.Values)
            {
                if (!stats.IsGun)
                    continue;

                gunStats.Add(stats);
                string role = stats.GunRole;
                if (!gunRoleTotals.TryGetValue(role, out var roleStats))
                {
                    roleStats = new RandPartCandidateStats { name = role };
                    gunRoleTotals[role] = roleStats;
                }

                roleStats.seen += stats.seen;
                roleStats.accepted += stats.accepted;
                roleStats.rejectedByGame += stats.rejectedByGame;
                roleStats.rejectedByTAFCaliber += stats.rejectedByTAFCaliber;
                roleStats.rejectedByTAFDownsize += stats.rejectedByTAFDownsize;
                roleStats.rejectedByTAFTowerDownsize += stats.rejectedByTAFTowerDownsize;
                roleStats.rejectedByTAFNonWholeCaliber += stats.rejectedByTAFNonWholeCaliber;
                roleStats.rejectedByTAFLowerMark += stats.rejectedByTAFLowerMark;
                roleStats.rejectedByTAFSkippedRandPart += stats.rejectedByTAFSkippedRandPart;
                roleStats.canPlaceChecks += stats.canPlaceChecks;
                roleStats.canPlaceYes += stats.canPlaceYes;
                roleStats.canPlaceNo += stats.canPlaceNo;
                roleStats.placed += stats.placed;
                roleStats.removed += stats.removed;
                roleStats.final += stats.final;

                if (stats.IsMainGun)
                {
                    mainGunStats.Add(stats);
                    randParts++;
                    seen += stats.seen;
                    accepted += stats.accepted;
                    rejectedByGame += stats.rejectedByGame;
                    rejectedByTAFCaliber += stats.rejectedByTAFCaliber;
                    rejectedByTAFDownsize += stats.rejectedByTAFDownsize;
                    rejectedByTAFTowerDownsize += stats.rejectedByTAFTowerDownsize;
                    rejectedByTAFNonWholeCaliber += stats.rejectedByTAFNonWholeCaliber;
                    rejectedByTAFLowerMark += stats.rejectedByTAFLowerMark;
                    rejectedByTAFSkippedRandPart += stats.rejectedByTAFSkippedRandPart;
                    canPlaceChecks += stats.canPlaceChecks;
                    canPlaceYes += stats.canPlaceYes;
                    canPlaceNo += stats.canPlaceNo;
                    placed += stats.placed;
                    removed += stats.removed;
                    final += stats.final;

                    string layout = stats.MainGunLayout;
                    if (!layoutTotals.TryGetValue(layout, out var layoutStats))
                    {
                        layoutStats = new RandPartCandidateStats { name = layout };
                        layoutTotals[layout] = layoutStats;
                    }

                    layoutStats.seen += stats.seen;
                    layoutStats.accepted += stats.accepted;
                    layoutStats.rejectedByGame += stats.rejectedByGame;
                    layoutStats.rejectedByTAFCaliber += stats.rejectedByTAFCaliber;
                    layoutStats.rejectedByTAFDownsize += stats.rejectedByTAFDownsize;
                    layoutStats.rejectedByTAFTowerDownsize += stats.rejectedByTAFTowerDownsize;
                    layoutStats.rejectedByTAFNonWholeCaliber += stats.rejectedByTAFNonWholeCaliber;
                    layoutStats.rejectedByTAFLowerMark += stats.rejectedByTAFLowerMark;
                    layoutStats.rejectedByTAFSkippedRandPart += stats.rejectedByTAFSkippedRandPart;
                    layoutStats.canPlaceChecks += stats.canPlaceChecks;
                    layoutStats.canPlaceYes += stats.canPlaceYes;
                    layoutStats.canPlaceNo += stats.canPlaceNo;
                    layoutStats.placed += stats.placed;
                    layoutStats.removed += stats.removed;
                    layoutStats.final += stats.final;
                }
            }

            if (IsShipgenDebugEnabled() && gunStats.Count > 0)
            {
                string gunRoleSummary = string.Join("; ", new[] { "main", "sec", "ter", "other" }
                    .Where(gunRoleTotals.ContainsKey)
                    .Select(role =>
                    {
                        var stats = gunRoleTotals[role];
                        return $"{role} seen={stats.seen}, accepted={stats.accepted}, canplace={stats.canPlaceYes}/{stats.canPlaceChecks}, placed={stats.placed}, removed={stats.removed}, final={stats.final}, rejected_game={stats.rejectedByGame}, rejected_downsize={stats.rejectedByTAFDownsize}, rejected_mark={stats.rejectedByTAFLowerMark}";
                    }));

                Melon<TweaksAndFixes>.Logger.Msg($"  all-gun candidates: randparts={gunStats.Count}, {gunRoleSummary}");

                int gunDetailLimit = Config.Param("taf_debug_shipgen_gun_randpart_details", 12);
                if (gunDetailLimit > 0)
                {
                    Melon<TweaksAndFixes>.Logger.Msg("  All-gun randpart details:");
                    foreach (var stats in gunStats
                        .OrderByDescending(stats => stats.final)
                        .ThenByDescending(stats => stats.placed)
                        .ThenByDescending(stats => stats.removed)
                        .ThenByDescending(stats => stats.accepted)
                        .ThenBy(stats => stats.name)
                        .Take(gunDetailLimit))
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"    {stats.name} [{stats.GunRole}]: seen={stats.seen}, accepted={stats.accepted}, canplace={stats.canPlaceYes}/{stats.canPlaceChecks}, placed={stats.placed}, removed={stats.removed}, final={stats.final}, rejected_game={stats.rejectedByGame}, rejected_downsize={stats.rejectedByTAFDownsize}, rejected_mark={stats.rejectedByTAFLowerMark}, rejected_nonwhole={stats.rejectedByTAFNonWholeCaliber}, rejected_skip_rp={stats.rejectedByTAFSkippedRandPart}, max_accepted_cal={stats.maxAcceptedCaliber}, condition={stats.condition}");
                    }
                }
            }

            if (randParts == 0)
            {
                ClearRandPartCandidateStats();
                return "main-gun candidates: none checked";
            }

            string layoutSummary = string.Join("; ", new[] { "center", "side", "other" }
                .Where(layoutTotals.ContainsKey)
                .Select(layout =>
                {
                    var stats = layoutTotals[layout];
                    return $"{layout} seen={stats.seen}, accepted={stats.accepted}, canplace={stats.canPlaceYes}/{stats.canPlaceChecks}, placed={stats.placed}, removed={stats.removed}, final={stats.final}, rejected_game={stats.rejectedByGame}, rejected_taf_caliber={stats.rejectedByTAFCaliber}, rejected_downsize={stats.rejectedByTAFDownsize}, rejected_mark={stats.rejectedByTAFLowerMark}, rejected_nonwhole={stats.rejectedByTAFNonWholeCaliber}, rejected_skip_rp={stats.rejectedByTAFSkippedRandPart}";
                }));

            int detailLimit = Config.Param("taf_debug_shipgen_main_gun_randpart_details", 12);
            if (IsShipgenDebugEnabled() && detailLimit > 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg("  Main-gun randpart details:");
                foreach (var stats in mainGunStats
                    .OrderByDescending(stats => stats.placed)
                    .ThenByDescending(stats => stats.final)
                    .ThenByDescending(stats => stats.accepted)
                    .ThenByDescending(stats => stats.seen)
                    .ThenBy(stats => stats.name)
                    .Take(detailLimit))
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    {stats.name} [{stats.MainGunLayout}]: seen={stats.seen}, accepted={stats.accepted}, canplace={stats.canPlaceYes}/{stats.canPlaceChecks}, placed={stats.placed}, removed={stats.removed}, final={stats.final}, rejected_game={stats.rejectedByGame}, rejected_taf_caliber={stats.rejectedByTAFCaliber}, rejected_downsize={stats.rejectedByTAFDownsize}, rejected_mark={stats.rejectedByTAFLowerMark}, rejected_nonwhole={stats.rejectedByTAFNonWholeCaliber}, rejected_skip_rp={stats.rejectedByTAFSkippedRandPart}, max_accepted_cal={stats.maxAcceptedCaliber}, condition={stats.condition}");
                }
            }

            ClearRandPartCandidateStats();

            string downsizeSummary = CurrentShipgenMainGunDownsizeCap() > 0 ? $", downsize_cap<={CurrentShipgenMainGunDownsizeCap()}\"{(_MainGunDownsizeMinimumCaliber > 0 ? $", downsize_floor>={_MainGunDownsizeMinimumCaliber}\"" : string.Empty)}" : string.Empty;
            string towerTierSummary = CurrentShipgenTowerDownsizeTierSummary();
            string towerDownsizeSummary = towerTierSummary.Length > 0 ? $", tower_tier_caps={towerTierSummary}" :
                CurrentShipgenTowerDownsizeTierCap() > 0 ? $", tower_tier_cap<={CurrentShipgenTowerDownsizeTierCap()}" :
                CurrentShipgenTowerDownsizeWeightCap() > 0f ? $", tower_weight_cap<={CurrentShipgenTowerDownsizeWeightCap():0.#}t" : string.Empty;
            string towerRejectedSummary = _AttemptTowerRejectedByDownsize > 0 ? $", tower_rejected_downsize={_AttemptTowerRejectedByDownsize}" : string.Empty;
            return $"main-gun candidates: randparts={randParts}, seen={seen}, accepted={accepted}, canplace={canPlaceYes}/{canPlaceChecks}, placed={placed}, removed={removed}, final={final}, rejected_game={rejectedByGame}, rejected_taf_caliber={rejectedByTAFCaliber}, rejected_downsize={rejectedByTAFDownsize}, rejected_mark={rejectedByTAFLowerMark}, rejected_nonwhole={rejectedByTAFNonWholeCaliber}, rejected_skip_rp={rejectedByTAFSkippedRandPart}{downsizeSummary}{towerDownsizeSummary}{towerRejectedSummary} | {layoutSummary}";
        }

        internal static void PrintRandPartDiagnosticsForShip(Ship ship)
        {
            if (!IsShipgenDebugEnabled() || ship == null || ship.hull == null || ship.hull.data == null)
                return;

            int applicable = 0;
            int gun = 0;
            int mainGun = 0;
            int secGun = 0;
            int terGun = 0;
            int bannedMainGun = 0;
            List<string> mainGunNames = new();
            List<string> gunNames = new();
            foreach (var rp in ship.hull.data.shipType.randParts)
            {
                if (!ShipM.CheckOperation(ship.hull.data, ship, rp, null))
                    continue;

                if (IsHardBannedShipgenRandPart(rp))
                {
                    bannedMainGun++;
                    continue;
                }

                applicable++;
                if (rp.type == "gun")
                {
                    gun++;
                    gunNames.Add(rp.name);
                    if ((rp.condition ?? string.Empty).Contains("sec_cal"))
                        secGun++;
                    if ((rp.condition ?? string.Empty).Contains("ter_cal"))
                        terGun++;
                }

                if (rp.type == "gun" && rp.condition.Contains("main_cal"))
                {
                    mainGun++;
                    mainGunNames.Add(rp.name);
                }
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  RandParts: applicable={applicable}, guns={gun}, main_guns={mainGun}, sec_guns={secGun}, ter_guns={terGun}{(bannedMainGun > 0 ? $", hard_banned_main_guns={bannedMainGun}" : string.Empty)}");
            if (mainGunNames.Count == 0)
                Melon<TweaksAndFixes>.Logger.Msg("  RandParts: no applicable main-gun randparts found.");
            else
                Melon<TweaksAndFixes>.Logger.Msg($"  RandParts main guns: {string.Join(", ", mainGunNames)}");

            int gunListLimit = Config.Param("taf_debug_shipgen_gun_randpart_list_limit", 0);
            if (gunListLimit > 0 && gunNames.Count > 0)
                Melon<TweaksAndFixes>.Logger.Msg($"  RandParts guns: {string.Join(", ", gunNames.Take(gunListLimit))}{(gunNames.Count > gunListLimit ? $", ... +{gunNames.Count - gunListLimit} more" : string.Empty)}");
        }

        internal static int ShipgenGunRandPartRoleOrder(RandPart rp)
        {
            string condition = rp?.condition ?? string.Empty;
            if (condition.Contains("main_cal"))
                return 0;
            if (condition.Contains("sec_cal"))
                return 1;
            if (condition.Contains("ter_cal"))
                return 2;
            return 3;
        }

        internal static int ShipgenGunRandPartLayoutOrder(RandPart rp)
        {
            if (rp == null)
                return 2;

            string source = $"{rp.name} {rp.group} {rp.effect} {rp.param}".ToLowerInvariant();
            bool side = rp.side || source.Contains("side") || source.Contains("/ms/") || source.Contains("/mg2") || source.Contains("/mg3");
            bool center = rp.center || source.Contains("center") || source.Contains("/mc/");

            if (side && !center)
                return 0;
            if (center && !side)
                return 1;
            if (side)
                return 0;
            return 2;
        }

        internal static string ShipgenGunRandPartOrderLabel(RandPart rp)
        {
            string role = ShipgenGunRandPartRoleOrder(rp) switch
            {
                0 => "main",
                1 => "sec",
                2 => "ter",
                _ => "other"
            };
            string layout = ShipgenGunRandPartLayoutOrder(rp) switch
            {
                0 => "side",
                1 => "center",
                _ => "other"
            };
            return $"{role}_{layout}";
        }

        internal static void ReorderShipgenRandPartsMainGunsFirst(Ship ship)
        {
            if (!Config.ShipGenTweaks || Config.Param("taf_shipgen_main_gun_rules_first", 1) == 0)
                return;

            var randParts = ship?.hull?.data?.shipType?.randParts;
            if (randParts == null || randParts.Count == 0)
                return;

            string shipTypeName = ship?.shipType?.name ?? ship?.hull?.data?.shipType?.name ?? "?";
            bool torpedoesBeforeMainGuns = shipTypeName == "tb" || shipTypeName == "dd";
            bool pruneTorpedoes = Config.Param("taf_shipgen_ban_torpedoes_above_cl", 1) != 0 && IsShipTypeAboveCL(ship);
            List<RandPart> original = new();
            int prunedBarbettes = 0;
            int prunedTorpedoes = 0;
            int prunedGunOther = 0;
            foreach (var rp in randParts)
            {
                if (rp == null)
                    continue;

                if (IsGunOtherRandPart(rp))
                {
                    prunedGunOther++;
                    continue;
                }

                if (Config.Param("taf_shipgen_skip_barbettes", 1) != 0 && rp.type == "barbette")
                {
                    prunedBarbettes++;
                    continue;
                }

                if (pruneTorpedoes && rp.type == "torpedo")
                {
                    prunedTorpedoes++;
                    continue;
                }

                original.Add(rp);
            }

            if (original.Count == 0)
                return;

            static bool IsTowerRandPart(RandPart rp)
                => rp != null && (rp.type == "tower_main" || rp.type == "tower_sec");

            static bool IsFunnelRandPart(RandPart rp)
                => rp != null && rp.type == "funnel";

            static bool IsTorpedoRandPart(RandPart rp)
                => rp != null && rp.type == "torpedo";

            List<RandPart> towers = original.Where(IsTowerRandPart).ToList();
            List<RandPart> funnels = original.Where(IsFunnelRandPart).ToList();
            List<RandPart> torpedoes = torpedoesBeforeMainGuns ? original.Where(IsTorpedoRandPart).ToList() : new List<RandPart>();
            List<RandPart> guns = original.Where(rp => rp?.type == "gun").ToList();

            List<RandPart> orderedGuns = guns
                .Select((rp, index) => new { rp, index })
                .OrderBy(x => ShipgenGunRandPartRoleOrder(x.rp))
                .ThenBy(x => ShipgenGunRandPartLayoutOrder(x.rp))
                .ThenBy(x => x.index)
                .Select(x => x.rp)
                .ToList();

            List<RandPart> rest = original
                .Where(rp => !IsTowerRandPart(rp) && !IsFunnelRandPart(rp) && rp?.type != "gun" && (!torpedoesBeforeMainGuns || !IsTorpedoRandPart(rp)))
                .ToList();

            List<RandPart> reordered = new();
            reordered.AddRange(towers);
            reordered.AddRange(funnels);
            reordered.AddRange(torpedoes);
            reordered.AddRange(orderedGuns);
            reordered.AddRange(rest);

            bool changed = prunedBarbettes > 0 || prunedTorpedoes > 0 || prunedGunOther > 0 || original.Count != reordered.Count;
            if (!changed)
            {
                for (int i = 0; i < original.Count; i++)
                {
                    if (!ReferenceEquals(original[i], reordered[i]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                randParts.Clear();
                foreach (var rp in reordered)
                    randParts.Add(rp);

                _ShipgenRandPartsReorderedForShipTypes.Add(shipTypeName);
            }

            if (IsShipgenDebugEnabled())
            {
                int limit = Config.Param("taf_debug_shipgen_gun_randpart_list_limit", 0);
                if (limit > 0)
                {
                    var gunNames = reordered.Where(rp => rp?.type == "gun").Select(rp => rp.name).Take(limit).ToList();
                    int gunCount = reordered.Count(rp => rp?.type == "gun");
                    string gunOrder = string.Join("; ", orderedGuns
                        .GroupBy(ShipgenGunRandPartOrderLabel)
                        .Select(g => $"{g.Key}={g.Count()}"));
                    var prefix = reordered
                        .Take(towers.Count + funnels.Count + torpedoes.Count)
                        .Select(rp => rp.name)
                        .Take(limit)
                        .ToList();
                    Melon<TweaksAndFixes>.Logger.Msg(
                        $"  RandParts reordered towers-funnels{(torpedoesBeforeMainGuns ? "-torpedoes" : string.Empty)}-guns: shipType={shipTypeName}, changed={changed}, towers={towers.Count}, funnels={funnels.Count}, torpedoes={torpedoes.Count}, guns={gunCount}, rest={rest.Count}, pruned_barbettes={prunedBarbettes}, pruned_torpedoes={prunedTorpedoes}, pruned_gun_other={prunedGunOther}, gun_order={gunOrder}");
                    Melon<TweaksAndFixes>.Logger.Msg(
                        $"  RandParts reordered prefix: {string.Join(", ", prefix)}{(towers.Count + funnels.Count + torpedoes.Count > limit ? $", ... +{towers.Count + funnels.Count + torpedoes.Count - limit} more" : string.Empty)}");
                    Melon<TweaksAndFixes>.Logger.Msg(
                        $"  RandParts reordered guns: {string.Join(", ", gunNames)}{(gunCount > limit ? $", ... +{gunCount - limit} more" : string.Empty)}");
                }
            }
        }

        internal static string ShipgenRandPartBucket(RandPart rp)
        {
            if (rp == null)
                return "unknown";

            string type = rp.type ?? string.Empty;
            string condition = rp.condition ?? string.Empty;

            if (type == "tower_main")
                return "tower_main";

            if (type == "tower_sec")
                return "tower_sec";

            if (type == "funnel")
                return "funnel";

            if (type == "gun")
            {
                if (condition.Contains("main_cal"))
                    return "gun_main";
                if (condition.Contains("sec_cal"))
                    return "gun_sec";
                if (condition.Contains("ter_cal"))
                    return "gun_ter";
                return "gun_other";
            }

            return string.IsNullOrWhiteSpace(type) ? "other" : type;
        }

        internal static int ShipgenRandPartBucketSuccessLimit(string bucket)
        {
            if (bucket == "tower_main" || bucket == "tower_sec")
                return 1;

            return 2;
        }

        internal static RandPartAttemptStats RandPartAttemptStatsForBucket(string bucket)
        {
            if (string.IsNullOrWhiteSpace(bucket))
                bucket = "unknown";

            if (!_AttemptRandPartStatsByBucket.TryGetValue(bucket, out var stats))
            {
                stats = new RandPartAttemptStats { bucket = bucket };
                _AttemptRandPartStatsByBucket[bucket] = stats;
            }

            return stats;
        }

        internal static void FinishCurrentRandPartTiming()
        {
            if (string.IsNullOrWhiteSpace(_AttemptCurrentRandPartName) || _AttemptCurrentRandPartStartedAt <= 0f)
                return;

            RandPartAttemptStatsForBucket(_AttemptCurrentRandPartBucket).elapsed += Math.Max(0f, Time.realtimeSinceStartup - _AttemptCurrentRandPartStartedAt);
            _AttemptCurrentRandPartName = string.Empty;
            _AttemptCurrentRandPartBucket = string.Empty;
            _AttemptCurrentRandPartStartedAt = 0f;
        }

        internal static void TrackShipgenRandPartStarted(RandPart rp)
        {
            if (!Config.ShipGenTweaks || rp == null)
                return;

            string name = rp.name ?? string.Empty;
            if (_AttemptCurrentRandPartName != name)
            {
                FinishCurrentRandPartTiming();
                _AttemptCurrentRandPartName = name;
                _AttemptCurrentRandPartBucket = ShipgenRandPartBucket(rp);
                _AttemptCurrentRandPartStartedAt = Time.realtimeSinceStartup;
            }

            if (_AttemptRandPartsStarted.Add(name))
            {
                RandPartAttemptStatsForBucket(ShipgenRandPartBucket(rp)).recipesStarted++;
            }
        }

        internal static void RecordShipgenRandPartPlacementSuccess(Part part)
        {
            if (!Config.ShipGenTweaks || _LastRandPart == null || part == null || part.data == null)
                return;

            string name = _LastRandPart.name ?? string.Empty;
            string bucket = ShipgenRandPartBucket(_LastRandPart);
            var stats = RandPartAttemptStatsForBucket(bucket);
            stats.partsPlaced++;

            if (_AttemptRandPartsSucceeded.Add(name))
            {
                stats.successfulRecipes++;
            }
        }

        internal static bool ShouldSkipShipgenRandPartBySuccessCap(RandPart rp, out string reason)
        {
            reason = string.Empty;
            if (!Config.ShipGenTweaks || rp == null)
                return false;

            string bucket = ShipgenRandPartBucket(rp);
            int limit = ShipgenRandPartBucketSuccessLimit(bucket);
            if (limit <= 0)
                return false;

            var stats = RandPartAttemptStatsForBucket(bucket);
            if (stats.successfulRecipes < limit)
                return false;

            string name = rp.name ?? string.Empty;
            if (_AttemptRandPartsSkipped.Add(name))
                stats.recipesSkipped++;

            reason = $"taf_randpart_bucket_satisfied:{bucket}";
            return true;
        }

        internal static void ResetShipgenRandPartAttemptStatsForAddPass()
        {
            if (!Config.ShipGenTweaks)
                return;

            ResetShipgenRandPartAttemptStats();
        }

        internal static void ResetShipgenRandPartAttemptStats()
        {
            FinishCurrentRandPartTiming();
            _AttemptRandPartStatsByBucket.Clear();
            _AttemptRandPartsStarted.Clear();
            _AttemptRandPartsSucceeded.Clear();
            _AttemptRandPartsSkipped.Clear();
            _AttemptCurrentRandPartName = string.Empty;
            _AttemptCurrentRandPartBucket = string.Empty;
            _AttemptCurrentRandPartStartedAt = 0f;
        }

        internal static void ResetShipgenPhaseStats()
        {
            _AttemptShipgenPhaseStats.Clear();
        }

        internal static void RecordShipgenPhase(string name, float elapsed)
        {
            if (!Config.ShipGenTweaks || string.IsNullOrWhiteSpace(name) || elapsed < 0f)
                return;

            if (!_AttemptShipgenPhaseStats.TryGetValue(name, out var stats))
            {
                stats = new ShipgenPhaseStats { name = name };
                _AttemptShipgenPhaseStats[name] = stats;
            }

            stats.calls++;
            stats.elapsed += elapsed;
        }

        internal static string ShipgenGeneratorStateLabel(int state)
        {
            return state switch
            {
                0 => "setup",
                1 => "remove_parts",
                2 => "beam_draught",
                3 => "tonnage",
                4 => "clamp_tonnage",
                5 => "pre_hull_adjust",
                6 => "initial_hull_adjust",
                7 => "update_hull",
                8 => "add_parts",
                9 => "wait_update_parts",
                10 => "post_parts_adjust",
                11 => "validate_guns",
                12 => "reduce_validate",
                13 => "validate_cost_req",
                14 => "validate_weight",
                15 => "validate_tonnage",
                16 => "retry_or_finish",
                17 => "finish",
                _ => $"state_{state}"
            };
        }

        internal static string ShipgenAddPartsStateLabel(int state)
        {
            return state switch
            {
                0 => "setup",
                1 => "select_randpart",
                2 => "place_parts",
                3 => "next_part",
                4 => "finish",
                _ => $"state_{state}"
            };
        }

        internal static void LogShipgenPhaseSummary(string indent = "  ")
        {
            if (_AttemptShipgenPhaseStats.Count == 0)
                return;

            float total = _AttemptShipgenPhaseStats.Values.Sum(s => s.elapsed);
            Melon<TweaksAndFixes>.Logger.Msg($"{indent}phase summary: time={total:F1}s");
            foreach (var stats in _AttemptShipgenPhaseStats.Values.OrderByDescending(s => s.elapsed).ThenBy(s => s.name))
                Melon<TweaksAndFixes>.Logger.Msg($"{indent}{stats.name}: calls={stats.calls}, time={stats.elapsed:F1}s");
        }

        internal static List<RandPartAttemptStats> OrderedShipgenRandPartAttemptStats()
        {
            string[] order =
            {
                "tower_main",
                "tower_sec",
                "funnel",
                "torpedo",
                "gun_main",
                "gun_sec",
                "gun_ter",
                "gun_other",
            };

            return _AttemptRandPartStatsByBucket.Values
                .OrderBy(s =>
                {
                    int index = Array.IndexOf(order, s.bucket);
                    return index >= 0 ? index : order.Length;
                })
                .ThenBy(s => s.bucket)
                .ToList();
        }

        internal static string ShipgenRandPartAttemptSummary()
        {
            FinishCurrentRandPartTiming();

            if (_AttemptRandPartStatsByBucket.Count == 0)
                return "randpart summary: none";

            int recipes = _AttemptRandPartStatsByBucket.Values.Sum(s => s.recipesStarted);
            int skipped = _AttemptRandPartStatsByBucket.Values.Sum(s => s.recipesSkipped);
            int successes = _AttemptRandPartStatsByBucket.Values.Sum(s => s.successfulRecipes);
            int placements = _AttemptRandPartStatsByBucket.Values.Sum(s => s.partsPlaced);
            float elapsed = _AttemptRandPartStatsByBucket.Values.Sum(s => s.elapsed);
            string buckets = string.Join("\n", OrderedShipgenRandPartAttemptStats()
                .Select(s => $"{s.bucket}: recipes={s.recipesStarted}, skipped={s.recipesSkipped}, success={s.successfulRecipes}, placed={s.partsPlaced}, time={s.elapsed:F1}s"));

            return $"randpart summary: recipes={recipes}, skipped={skipped}, success={successes}, placed={placements}, time={elapsed:F1}s\n{buckets}";
        }

        internal static void LogShipgenRandPartAttemptSummary(string indent = "  ")
        {
            foreach (string line in ShipgenRandPartAttemptSummary().Split(new[] { '\n' }, StringSplitOptions.None))
                Melon<TweaksAndFixes>.Logger.Msg($"{indent}{line}");
        }

        internal static bool UpdateRPGunCacheOrSkip(RandPart rp)
        {
            if (rp != _LastRandPart)
            {
                _LastRandPart = rp;
                _LastRPIsGun = rp.type == "gun";
                if (_LastRPIsGun)
                    _LastBattery = rp.condition.Contains("main_cal") ? ShipM.BatteryType.main : (rp.condition.Contains("sec_cal") ? ShipM.BatteryType.sec : ShipM.BatteryType.ter);

                TraceShipgenRandPartBegin(rp);
            }
            return !_LastRPIsGun;
        }

        internal static bool IsHardBannedShipgenRandPart(RandPart rp)
        {
            if (!Config.ShipGenTweaks || rp == null)
                return false;

            if (!IsMainGunRandPart(rp))
                return false;

            return _HardBannedShipgenMainGunRandParts.Contains(ShipgenRandPartId(rp.name));
        }

        internal static bool IsShipTypeAboveCL(Ship ship)
        {
            string shipTypeName = ship?.shipType?.name ?? ship?.hull?.data?.shipType?.name ?? string.Empty;
            return string.Equals(shipTypeName, "ca", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipTypeName, "bc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shipTypeName, "bb", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ShouldSkipShipgenTorpedoRandPart(Ship ship, RandPart rp, out string reason)
        {
            reason = string.Empty;
            if (!Config.ShipGenTweaks || rp == null || Config.Param("taf_shipgen_ban_torpedoes_above_cl", 1) == 0)
                return false;

            if (rp.type != "torpedo" || !IsShipTypeAboveCL(ship))
                return false;

            string name = rp.name ?? string.Empty;
            string bucket = ShipgenRandPartBucket(rp);
            var stats = RandPartAttemptStatsForBucket(bucket);
            if (_AttemptRandPartsSkipped.Add(name))
                stats.recipesSkipped++;

            reason = "taf_torpedo_above_cl_banned";
            return true;
        }

        internal static bool ShouldSkipShipgenBarbetteRandPart(RandPart rp, out string reason)
        {
            reason = string.Empty;
            if (!Config.ShipGenTweaks || rp == null || Config.Param("taf_shipgen_skip_barbettes", 1) == 0)
                return false;

            if (rp.type != "barbette")
                return false;

            string name = rp.name ?? string.Empty;
            string bucket = ShipgenRandPartBucket(rp);
            var stats = RandPartAttemptStatsForBucket(bucket);
            if (_AttemptRandPartsSkipped.Add(name))
                stats.recipesSkipped++;

            reason = "taf_barbette_skipped";
            return true;
        }

        internal static bool IsGunOtherRandPart(RandPart rp)
        {
            if (rp == null || rp.type != "gun")
                return false;

            string condition = rp.condition ?? string.Empty;
            return !condition.Contains("main_cal") && !condition.Contains("sec_cal") && !condition.Contains("ter_cal");
        }

        internal static bool ShouldSkipShipgenGunOtherRandPart(RandPart rp, out string reason)
        {
            reason = string.Empty;
            if (!Config.ShipGenTweaks || !IsGunOtherRandPart(rp))
                return false;

            string name = rp.name ?? string.Empty;
            string bucket = ShipgenRandPartBucket(rp);
            var stats = RandPartAttemptStatsForBucket(bucket);
            if (_AttemptRandPartsSkipped.Add(name))
                stats.recipesSkipped++;

            reason = "taf_gun_other_skipped";
            return true;
        }

        internal static bool ShouldSkipShipgenRandPart(Ship ship, RandPart rp, out string reason)
        {
            reason = string.Empty;
            if (ShouldSkipShipgenRandPartBySuccessCap(rp, out reason))
                return true;

            if (ShouldSkipShipgenTorpedoRandPart(ship, rp, out reason))
                return true;

            if (ShouldSkipShipgenBarbetteRandPart(rp, out reason))
                return true;

            if (ShouldSkipShipgenGunOtherRandPart(rp, out reason))
                return true;

            if (IsHardBannedShipgenRandPart(rp))
            {
                reason = "taf_hard_banned_randpart";
                return true;
            }

            return false;
        }

        internal static bool IsMainGunRandPart(RandPart rp)
        {
            return rp != null && rp.type == "gun" && (rp.condition ?? string.Empty).Contains("main_cal");
        }

        internal static string ShipgenRandPartId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            int slash = name.IndexOf('/');
            return slash >= 0 ? name.Substring(0, slash) : name;
        }

        internal static bool IsMainGunRandPartName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Contains("/gun/") && name.Contains("main_cal");
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
            float startedAt = Time.realtimeSinceStartup;
            ShipM.AddedAdditionalTonnageUsage(__instance);
            RecordShipgenPhase("call_added_tonnage_usage", Time.realtimeSinceStartup - startedAt);
            return false;
        }

        [HarmonyPatch(nameof(Ship.ReduceWeightByReducingCharacteristics))]
        [HarmonyPrefix]
        internal static bool Prefix_ReduceWeightByReducingCharacteristics(Ship __instance, Il2CppSystem.Random rnd, float tryN, float triesTotal, float randArmorRatio = 0, float speedLimit = 0)
        {
            float startedAt = Time.realtimeSinceStartup;
            float oldTempGoodWeight = __instance.tempGoodWeight;
            bool adjustedTempGoodWeight = false;

            if (Config.ShipGenTweaks && _GenerateRandomShipRoutine != null)
            {
                float tonnage = __instance.Tonnage();
                if (tonnage > 0f && (__instance.tempGoodWeight <= 0f || __instance.tempGoodWeight > tonnage))
                {
                    __instance.tempGoodWeight = tonnage;
                    adjustedTempGoodWeight = true;

                    if (Config.Param("taf_debug_shipgen_info", 0) != 0)
                        Melon<TweaksAndFixes>.Logger.Msg($"  Weight reducer target corrected: tempGood={oldTempGoodWeight:0.0}t, tonnage={tonnage:0.0}t");
                }
            }

            ShipM.ReduceWeightByReducingCharacteristics(__instance, rnd, tryN, triesTotal, randArmorRatio, speedLimit);
            RecordShipgenPhase("call_reduce_weight", Time.realtimeSinceStartup - startedAt);

            if (adjustedTempGoodWeight)
                __instance.tempGoodWeight = oldTempGoodWeight;

            NormalizeShipgenSpeed(__instance, true);
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

        [HarmonyPatch(nameof(Ship.SetCaliberDiameter))]
        [HarmonyPrefix]
        internal static void Prefix_SetCaliberDiameter(ref float __1)
        {
            if (!IsShipgenGunNormalizationActive())
                return;

            if (Config.Param("taf_shipgen_whole_inch_gun_calibers", 1) == 0)
                return;

            if (Math.Abs(__1) <= 0.001f)
                return;

            if (IsShipgenGunNormalizationDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"  Whole-inch guns: prevented generated diameter modifier {__1:0.###}");

            __1 = 0f;
        }

        [HarmonyPatch(nameof(Ship.SetCaliberLength))]
        [HarmonyPrefix]
        internal static void Prefix_SetCaliberLength(ref float __1)
        {
            if (!IsShipgenGunNormalizationActive())
                return;

            if (Config.Param("taf_shipgen_standard_gun_lengths", 1) == 0)
                return;

            if (Math.Abs(__1) <= 0.001f)
                return;

            if (IsShipgenGunNormalizationDebugEnabled())
                Melon<TweaksAndFixes>.Logger.Msg($"  Standard gun lengths: prevented generated length modifier {__1:0.###}");

            __1 = 0f;
        }

        public static void UpdateDeckClutter(Ship ship)
        {
            if (UiM.TAF_Settings.settings.deckPropCoverage == 0)
            {
                ship.hull.gameObject.GetChild("DeckProps").SetActive(false);
            }
            else
            {
                ship.hull.gameObject.GetChild("DeckProps").SetActive(true);

                var props = ship.hull.gameObject.GetChild("DeckProps").GetChildren();

                for (int i = 0; i < props.Count; i += 2)
                {
                    if ((i / 2) % 4 >= UiM.TAF_Settings.settings.deckPropCoverage / 25)
                    {
                        props[i].SetActive(false);
                        props[i + 1].SetActive(false);
                    }
                    else
                    {
                        props[i].SetActive(true);
                        props[i + 1].SetActive(true);
                    }
                }
            }
        }

        // SizeRatio
        // chosen.
        [HarmonyPatch(nameof(Ship.RefreshHull))]
        [HarmonyPostfix]
        internal static void Postfix_RefreshHull(Ship __instance)
        {
            UpdateDeckClutter(__instance);
        }

        // [HarmonyPatch(typeof(Ship.__c__DisplayClass870_1))]
        // [HarmonyPatch("_RefreshHull_b__21")]
        // [HarmonyPrefix]
        // internal static bool Prefix__RefreshHull_b__21(__c__DisplayClass870_1 __instance, DeckProp p, ref bool __result)
        // {
        //     __result = UiM.TAF_Settings.settings.deckPropCoverage == float.MaxValue ||
        //         Vector3.SqrMagnitude(p.transform.position - __instance.pos1) <= UiM.TAF_Settings.settings.deckPropCoverage * UiM.TAF_Settings.settings.deckPropCoverage;
        // 
        //     return false;
        // }
    }

    [HarmonyPatch]
    internal class Patch_Ship_AddShipTurretCaliber
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(Ship)))
                if (method.Name == nameof(Ship.AddShipTurretCaliber))
                    methods.Add(method);

            return methods;
        }

        internal static void Prefix(object[] __args)
        {
            if (!Config.ShipGenTweaks || (Patch_Ship._GenerateRandomShipRoutine == null && Patch_Ship._AddRandomPartsRoutine == null))
                return;

            Patch_Ship.MarkShipgenGunNormalizationActive();

            if (__args == null)
                return;

            bool normalizeDiameter = Config.Param("taf_shipgen_whole_inch_gun_calibers", 1) != 0;
            bool normalizeLength = Config.Param("taf_shipgen_standard_gun_lengths", 1) != 0;
            if (!normalizeDiameter && !normalizeLength)
                return;

            int floatIndex = 0;
            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is not float value)
                    continue;

                bool isDiameter = floatIndex == 0;
                bool isLength = floatIndex == 1;
                floatIndex++;

                if (isDiameter && normalizeDiameter && Math.Abs(value) > 0.001f)
                {
                    if (Patch_Ship.IsShipgenGunNormalizationDebugEnabled())
                        Melon<TweaksAndFixes>.Logger.Msg($"  Whole-inch guns: prevented generated turret caliber diameter modifier {value:0.###}");

                    __args[i] = 0f;
                }
                else if (isLength && normalizeLength && Math.Abs(value) > 0.001f)
                {
                    if (Patch_Ship.IsShipgenGunNormalizationDebugEnabled())
                        Melon<TweaksAndFixes>.Logger.Msg($"  Standard gun lengths: prevented generated turret caliber length modifier {value:0.###}");

                    __args[i] = 0f;
                }
            }
        }

        internal static void Postfix(Ship __instance, object[] __args)
        {
            Patch_Ship.MarkShipgenGunNormalizationActive();

            PartData partData = null;
            if (__args != null && __args.Length > 0)
                partData = __args[0] as PartData;

            Patch_Ship.NormalizeShipgenGunCaliberModifiers(__instance, partData, false);
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
            Patch_Ship.TrackShipgenRandPartStarted(__instance.randPart);
            var stats = Patch_Ship.CandidateStatsFor(__instance.randPart);
            stats.seen++;

            if (Patch_Ship.ShouldSkipShipgenRandPart(__instance.__4__this, __instance.randPart, out string skipReason))
            {
                stats.rejectedByTAFSkippedRandPart++;
                Patch_Ship.RecordMainGunRejectReason(skipReason, a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, skipReason);
                __result = false;
                return false;
            }

            Patch_Ship.RecordSeenTowerCandidate(a);

            if (Patch_Ship.ShouldRejectTowerByDownsize(a))
            {
                stats.rejectedByTAFTowerDownsize++;
                Patch_Ship._AttemptTowerRejectedByDownsize++;
                Patch_Ship.RecordMainGunRejectReason("taf_tower_downsize", a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, "taf_tower_downsize");
                __result = false;
                return false;
            }

            // Super annoying we can't prefix GetParts itself to do the RP caching
            if (Patch_Ship.UpdateRPGunCacheOrSkip(__instance.randPart))
                return true;

            int partCal = (int)((a.caliber + 1f) * (1f / 25.4f));
            if (Patch_Ship.ShouldRejectShipgenGunByNonWholeCaliber(a, Patch_Ship._LastBattery))
            {
                stats.rejectedByTAFNonWholeCaliber++;
                Patch_Ship.RecordMainGunRejectReason("taf_nonwhole_caliber", a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, "taf_nonwhole_caliber");
                __result = false;
                return false;
            }

            if (Patch_Ship.ShouldRejectShipgenGunByLowerMark(__instance.__4__this, a, Patch_Ship._LastBattery))
            {
                stats.rejectedByTAFLowerMark++;
                Patch_Ship._AttemptGunRejectedByLowerMark++;
                Patch_Ship.RecordMainGunRejectReason("taf_lower_gun_mark", a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, "taf_lower_gun_mark");
                __result = false;
                return false;
            }

            if (Patch_Ship.ShouldRejectMainGunByDownsize(Patch_Ship._LastBattery, partCal))
            {
                stats.rejectedByTAFDownsize++;
                Patch_Ship.RecordMainGunRejectReason("taf_downsize_cap", a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, "taf_downsize_cap");
                __result = false;
                return false;
            }

            if (Patch_Ship._GenGunInfo.isLimited && !Patch_Ship._GenGunInfo.CaliberOK(Patch_Ship._LastBattery, partCal))
            {
                stats.rejectedByTAFCaliber++;
                Patch_Ship.RecordMainGunRejectReason("taf_caliber_group", a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, "taf_caliber_group");
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Ship.__c__DisplayClass590_0._GetParts_b__0))]
        [HarmonyPostfix]
        internal static void Postfix_b0(Ship.__c__DisplayClass590_0 __instance, PartData a, bool __result)
        {
            var stats = Patch_Ship.CandidateStatsFor(__instance.randPart);
            if (__result)
            {
                stats.accepted++;
                Patch_Ship.RecordAcceptedTowerCandidate(a);

                int partCal = (int)((a.caliber + 1f) * (1f / 25.4f));
                Patch_Ship.RecordAcceptedMainGunCandidate(__instance.randPart, partCal);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, true, "accepted");
            }
            else if (stats.seen > stats.accepted + stats.rejectedByGame + stats.rejectedByTAFCaliber + stats.rejectedByTAFDownsize + stats.rejectedByTAFTowerDownsize + stats.rejectedByTAFNonWholeCaliber + stats.rejectedByTAFLowerMark + stats.rejectedByTAFSkippedRandPart)
            {
                stats.rejectedByGame++;
                Patch_Ship.RecordMainGunRejectReason("candidate_game_filter", a);
                Patch_Ship.TraceShipgenCandidate(a, __instance.randPart, false, "candidate_game_filter");
            }
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
            public int tryNum;
            public float speed;
            public float beam;
            public float beamMin;
            public float beamMax;
            public float draught;
            public float draughtMin;
            public float draughtMax;
            public float startedAt;
        }

        public static GRSData lastState = new();

        public static bool shipGenActive = false;

        public static float Reratio(float v, float a1, float a2, float b1, float b2)
        {
            // Mapping onto a single point range always gives the same answer
            //   Also prevents a devide-by-zero edge case
            if (b1 == b2) return b1;
            if (a1 == a2) return a1;

            return b1 + (b2 - b1) * ((v - a1) / (a2 - a1));
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
                            Melon<TweaksAndFixes>.Logger.Error($"Invalid `shipTypes.csv` `shipgen_clamp` param: `{stat}` for ID `{st.name}`. Unsuported stat. Can only be [speed_min, speed_max, beam_min, beam_max, draught_min, draught_max]");
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

            if (lastState.speed != ship.SpeedMax())
            {
                float val =
                    ModUtils.roundToInc(
                        Math.Clamp(ship.speedMax, speed_min * 0.5144444f, speed_max * 0.5144444f),
                        Config.Param("speed_step", 0.1f)
                    );

                // Melon<TweaksAndFixes>.Logger.Msg($"  {lastState.speed} != {ship.SpeedMax()}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {speed_min} / {st.speedMin} - {speed_max} / {st.speedMax}");
                // if (speed_min != float.MinValue)
                //     Melon<TweaksAndFixes>.Logger.Msg(
                //         $"  speed:     {ship.speedMax * 1.943844f,10} -> {val * 1.943844f}"
                //     );

                ship.SetSpeedMax(val);
                lastState.speed = ship.SpeedMax();
            }
            
            if (lastState.beam != ship.Beam())
            {
                float val =
                    ModUtils.roundToInc(
                        Reratio(ship.beam, sd.beamMin, sd.beamMax, beam_min, beam_max),
                        0.1f
                    );

                // Melon<TweaksAndFixes>.Logger.Msg($"  {lastState.beam} != {ship.Beam()}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {beam_min} / {sd.beamMin} - {beam_max} / {sd.beamMax}");
                // if (beam_min != float.MinValue)
                //     Melon<TweaksAndFixes>.Logger.Msg($"  beam:      {ship.beam,10} -> {val}");

                ship.SetBeam(val);
                lastState.beam = ship.Beam();
            }

            if (lastState.draught != ship.Draught())
            {
                float val =
                    ModUtils.roundToInc(
                        Reratio(ship.draught, sd.draughtMin, sd.draughtMax, draught_min, draught_max),
                        0.1f
                    );

                // Melon<TweaksAndFixes>.Logger.Msg($"  {lastState.draught} != {ship.Draught()}");
                // Melon<TweaksAndFixes>.Logger.Msg($"  {draught_min} / {sd.draughtMin} - {draught_max} / {sd.draughtMax}");
                // if (draught_min != float.MinValue)
                //     Melon<TweaksAndFixes>.Logger.Msg($"  draught:   {ship.draught,10} -> {val}");

                ship.SetDraught(val);
                lastState.draught = ship.Draught();
            }
        }

        private static bool CanModCompType(Ship ship, string key)
        {
            if (!G.GameData.compTypes.ContainsKey(key))
                return false;

            if (!ship.components.ContainsKey(G.GameData.compTypes[key]))
                return false;

            return true;
        }

        private static bool CanModComp(Ship ship, string key)
        {
            if (!G.GameData.components.ContainsKey(key))
                return false;

            if (!ship.components.ContainsKey(G.GameData.components[key].typex))
                return false;

            return true;
        }

        private static bool InstallFirstAvailableComponent(Ship ship, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (!G.GameData.components.TryGetValue(key, out var component))
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

        private static void OptimizeShellAndTorpedoComponents(Ship ship)
        {
            // DIP makes HE much less valuable, so generated ships should prefer AP-heavy,
            // penetration-oriented shell setups whenever the relevant tech is available.
            InstallFirstAvailableComponent(ship, "shell_ratio_main_2");
            InstallFirstAvailableComponent(ship, "shell_ratio_sec_2");
            InstallFirstAvailableComponent(ship, "ap_5", "ap_2", "ap_1", "ap_0", "ap_4", "ap_3");
            InstallFirstAvailableComponent(ship, "he_3", "he_2", "he_0", "he_1", "he_4", "he_5");

            InstallFirstAvailableComponent(
                ship,
                "torpedo_diameter_9",
                "torpedo_diameter_8",
                "torpedo_diameter_7",
                "torpedo_diameter_6",
                "torpedo_diameter_5",
                "torpedo_diameter_4",
                "torpedo_diameter_3",
                "torpedo_diameter_2",
                "torpedo_diameter_1",
                "torpedo_diameter_0");
        }

        public static void OptimizeComponents(Ship ship)
        {
            var _this = ship;

            OptimizeShellAndTorpedoComponents(ship);

            if (CanModCompType(ship, "boilers") && CanModCompType(ship, "engine") && CanModCompType(ship, "fuel"))
            {
                float bestWeight = _this.Weight();
                ComponentData bestEngine = _this.components[G.GameData.compTypes["boilers"]];
                ComponentData bestBoiler = _this.components[G.GameData.compTypes["engine"]];
                ComponentData bestFuel = _this.components[G.GameData.compTypes["fuel"]];

                // Melon<TweaksAndFixes>.Logger.Msg($"  Start: {bestEngine.name} x {bestBoiler.name} x {bestFuel.name}: {_this.weight} t. / {_this.Tonnage()}");

                foreach (var engine in G.GameData.technologies)
                {
                    if (engine.Value.componentx == null
                        || engine.Value.componentx.type != "engine"
                        || !_this.IsComponentAvailable(engine.Value.componentx)) continue;

                    foreach (var boiler in G.GameData.technologies)
                    {
                        if (boiler.Value.componentx == null
                            || boiler.Value.componentx.type != "boilers"
                            || !_this.IsComponentAvailable(boiler.Value.componentx)) continue;

                        foreach (var fuel in G.GameData.technologies)
                        {
                            if (fuel.Value.componentx == null
                                || fuel.Value.componentx.type != "fuel"
                                || !_this.IsComponentAvailable(fuel.Value.componentx)) continue;

                            // Melon<TweaksAndFixes>.Logger.Msg($"    {engine.Key} x {boiler.Key} x {fuel.Key}");

                            _this.InstallComponent(engine.Value.componentx);
                            _this.InstallComponent(boiler.Value.componentx);
                            _this.InstallComponent(fuel.Value.componentx);

                            if (bestWeight > _this.Weight())
                            {
                                bestWeight = _this.Weight();
                                bestEngine = engine.Value.componentx;
                                bestBoiler = boiler.Value.componentx;
                                bestFuel = fuel.Value.componentx;
                            }
                        }
                    }
                }

                _this.InstallComponent(bestEngine);
                _this.InstallComponent(bestBoiler);
                _this.InstallComponent(bestFuel);
                // Melon<TweaksAndFixes>.Logger.Msg($"  Best Combo: {bestEngine.name} x {bestBoiler.name} x {bestFuel.name}: {_this.weight} t. / {_this.Tonnage()}");
            }

            if (CanModCompType(ship, "torpedo_prop") &&
                CanModComp(ship, "torpedo_prop_fast") &&
                CanModComp(ship, "torpedo_prop_normal") &&
                _this.components[G.GameData.compTypes["torpedo_prop"]] == G.GameData.components["torpedo_prop_fast"])
            {
                _this.InstallComponent(G.GameData.components["torpedo_prop_normal"]);
            }

            if (CanModCompType(ship, "shell") &&
                CanModComp(ship, "shell_light") &&
                CanModComp(ship, "shell_normal") &&
                _this.components[G.GameData.compTypes["shell"]] == G.GameData.components["shell_light"])
            {
                _this.InstallComponent(G.GameData.components["shell_normal"]);
            }
        }

        public static void OnShipgenStart()
        {
            PrintShipgenStart(Patch_Ship._GenerateRandomShipRoutine, Patch_Ship._GenerateRandomShipRoutine.__4__this);
            shipGenActive = true;
        }

        public static void OnShipgenEnd()
        {
            var routine = Patch_Ship._GenerateRandomShipRoutine;
            if (Config.ShipGenTweaks && routine != null && routine._tryN_5__5 != routine._triesTotal_5__4)
                ShipM.FillUnusedShipgenTonnageWithArmor(routine.__4__this);

            PrintShipgenEnd(Patch_Ship._GenerateRandomShipRoutine);
            shipGenActive = false;
        }

        private static List<string> BuildShipgenIssueFlags(Ship ship)
        {
            List<string> flags = new();

            if (ship == null || ship.hull == null || ship.hull.data == null)
            {
                flags.Add("ship data unavailable");
                return flags;
            }

            Patch_Ship.CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);

            bool hasMinMainTurrets = ship.hull.data.minMainTurrets == -1 || ship.hull.data.minMainTurrets <= numMainTurrets;
            bool hasMinMainBarrels = ship.hull.data.minMainBarrels == -1 || ship.hull.data.minMainBarrels <= numMainBarrels;

            bool isValidCostReqParts = ship.IsValidCostReqParts(
                out string isValidCostReqPartsReason,
                out Il2CppSystem.Collections.Generic.List<ShipType.ReqInfo> notPassed,
                out Il2CppSystem.Collections.Generic.Dictionary<Part, string> badParts);

            bool isValidCostWeightBarbette = ship.IsValidCostWeightBarbette(
                out string isValidCostWeightBarbetteReason,
                out Il2CppSystem.Collections.Generic.List<Part> errorBarbettePart);

            bool isTonnageAllowedByTech = ship.player.IsTonnageAllowedByTech(ship.Tonnage(), ship.shipType);

            bool isValidWeightOffset = ship.IsValidWeightOffset();

            if (!hasMinMainTurrets)
                flags.Add($"main turrets {numMainTurrets}/{ship.hull.data.minMainTurrets}");

            if (!hasMinMainBarrels)
                flags.Add($"main barrels {numMainBarrels}/{ship.hull.data.minMainBarrels}");

            if (!isValidCostReqParts)
            {
                if (notPassed.Count > 0)
                {
                    List<string> reqs = new();
                    foreach (var req in notPassed)
                        reqs.Add($"{req.stat.name}={ship.stats[req.stat].total} ({req.min}~{req.max})");

                    flags.Add($"unmet reqs: {string.Join("; ", reqs)}");
                }

                if (badParts.Count > 0)
                    flags.Add($"invalid parts={badParts.Count}");

                if (notPassed.Count == 0 && badParts.Count == 0 && !string.IsNullOrWhiteSpace(isValidCostReqPartsReason))
                    flags.Add($"cost/req parts: {isValidCostReqPartsReason}");
            }

            if (ship.Weight() > ship.Tonnage())
                flags.Add($"overweight {(int)ship.Weight()}t/{(int)ship.Tonnage()}t");

            if (!isValidCostWeightBarbette)
            {
                if (errorBarbettePart.Count > 0)
                    flags.Add($"empty barbettes={errorBarbettePart.Count}");
                else if (!string.IsNullOrWhiteSpace(isValidCostWeightBarbetteReason))
                    flags.Add($"weight/barbette: {isValidCostWeightBarbetteReason}");
            }

            if (!isTonnageAllowedByTech)
                flags.Add("tonnage outside tech range");

            if (!isValidWeightOffset)
            {
                float inst_x = ship.stats_[G.GameData.stats["instability_x"]].total;
                float inst_z = ship.stats_[G.GameData.stats["instability_z"]].total;

                List<string> offsets = new();
                if (inst_x > 0) offsets.Add($"x={inst_x}");
                if (inst_z > 100) offsets.Add($"z={inst_z}");
                flags.Add($"invalid weight offset {string.Join(", ", offsets)}");
            }

            return flags;
        }

        private static string ShipgenIssueSummary(Ship ship, string fallback)
        {
            List<string> flags = BuildShipgenIssueFlags(ship);
            return flags.Count == 0 ? fallback : string.Join(" | ", flags);
        }

        private static bool IsShipgenSummaryOnly()
            => Config.Param("taf_debug_shipgen_summary_only", 0) != 0;

        private static string ShipgenHullSummary(Ship ship)
        {
            if (ship == null)
                return "shipType=?, hull=?, model=?";

            string shipType = ship.shipType?.name ?? ship.hull?.data?.shipType?.name ?? "?";
            string hull = ship.hull?.data?.name ?? "?";
            string model = ship.hull?.data?.model ?? "?";
            return $"shipType={shipType}, hull={hull}, model={model}";
        }

        private static void PrintShipgenIssues(Ship._GenerateRandomShip_d__573 __instance, Ship ship)
        {
            if (Config.Param("taf_debug_shipgen_info", 0) == 0) return;

            float elapsed = Patch_Ship._AttemptStartedAt > 0f ? Time.realtimeSinceStartup - Patch_Ship._AttemptStartedAt : 0f;
            string reason = ShipgenIssueSummary(ship, "intermediate retry");
            Melon<TweaksAndFixes>.Logger.Msg($"Shipgen retry: {ShipgenHullSummary(ship)}, attempt={__instance._tryN_5__5}/{__instance._triesTotal_5__4}, elapsed={elapsed:F1}s, reason={reason}");
            if (IsShipgenSummaryOnly())
            {
                Patch_Ship.LogShipgenRandPartAttemptSummary();
                Patch_Ship.LogShipgenPhaseSummary();
                Patch_Ship.ClearRandPartCandidateStats();
                return;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  {Patch_Ship.SummarizeAndClearRandPartCandidateStats()}");
            Patch_Ship.LogShipgenRandPartAttemptSummary();
            Patch_Ship.LogShipgenPhaseSummary();
            Patch_Ship.PrintShipgenLifecycleSummary(ship);
        }

        private static void PrintShipgenStart(Ship._GenerateRandomShip_d__573 __instance, Ship ship)
        {
            Patch_Ship._ShipgenStartedAt = Time.realtimeSinceStartup;
            Patch_Ship._AttemptStartedAt = Patch_Ship._ShipgenStartedAt;
            Patch_Ship._ShipgenAttemptTraceId = Math.Max(1, __instance?._tryN_5__5 ?? 1);
            Patch_Ship.ResetShipgenPhaseStats();
            Patch_Ship.ReorderShipgenRandPartsMainGunsFirst(ship);

            if (Config.Param("taf_debug_shipgen_info", 0) == 0) return;

            if (IsShipgenSummaryOnly())
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Shipgen begin: {ShipgenHullSummary(ship)}, nation={ship.player.data.name}, year={ship.dateCreated.AsDate().Year}, tonnage={ship.Tonnage():0}/{ship.TonnageMax():0}");
                return;
            }

            Melon<TweaksAndFixes>.Logger.Msg($"Begin shipgen:");
            Melon<TweaksAndFixes>.Logger.Msg($"  Hull   : {ship.hull.data.name} ({ship.hull.data.nameUi})");
            Melon<TweaksAndFixes>.Logger.Msg($"  Model  : {ship.hull.data.model}");
            Melon<TweaksAndFixes>.Logger.Msg($"  Nation : {ship.player.data.name} ({ship.player.data.nameUi})");
            Melon<TweaksAndFixes>.Logger.Msg($"  Year   : {ship.dateCreated.AsDate().Year}");
            Melon<TweaksAndFixes>.Logger.Msg($"  Tonnage: {ship.Tonnage():0}t / {ship.TonnageMax():0}t max");
            Patch_Ship.PrintRandPartDiagnosticsForShip(ship);
            Melon<TweaksAndFixes>.Logger.Msg($"First pass:");
        }

        private static void ApplyShipgenAttemptCap(Ship._GenerateRandomShip_d__573 __instance)
        {
            if (!Config.ShipGenTweaks || __instance == null)
                return;

            int maxAttempts = Config.Param("taf_shipgen_max_attempts", 20);
            if (maxAttempts <= 0)
                return;

            __instance._triesTotal_5__4 = Math.Min(__instance._triesTotal_5__4, Math.Max(1, maxAttempts));
        }

        private static void PrintShipgenFinalSummary(Ship ship)
        {
            Patch_Ship.NormalizeShipgenSpeed(ship, true);

            int numGuns = 0;

            foreach (var part in ship.parts)
            {
                if (!part.data.isGun) continue;

                numGuns++;
            }

            Patch_Ship.CountMainGuns(ship, out int numMainTurrets, out int numMainBarrels);

            float inst_x = ship.stats_[G.GameData.stats["instability_x"]].total;
            float inst_z = ship.stats_[G.GameData.stats["instability_z"]].total;
            float elapsed = Patch_Ship._ShipgenStartedAt > 0f ? Time.realtimeSinceStartup - Patch_Ship._ShipgenStartedAt : 0f;

            Melon<TweaksAndFixes>.Logger.Msg($"  Final   : {numMainTurrets} main turrets / {numMainBarrels} main barrels, guns={numGuns}");
            Patch_Ship.PrintShipgenFinalGunParts(ship);
            Melon<TweaksAndFixes>.Logger.Msg($"            weight={(int)ship.Weight()}t / {(int)ship.Tonnage()}t, speed={ship.SpeedMax() / ShipM.KnotsToMS:F1}kn, instability x={inst_x}, z={inst_z}, elapsed={elapsed:F1}s");
        }

        private static void PrintShipgenEnd(Ship._GenerateRandomShip_d__573 __instance)
        {
            if (Config.Param("taf_debug_shipgen_info", 0) == 0)
            {
                Patch_Ship.ClearRandPartCandidateStats();
                Patch_Ship.ResetShipgenRandPartAttemptStats();
                Patch_Ship.ResetShipgenPhaseStats();
                return;
            }

            if (!IsShipgenSummaryOnly())
                Melon<TweaksAndFixes>.Logger.Msg($"Shipgen halted");

            if (__instance == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Result   : Interrupted");
            }
            else
            {
                bool success = __instance._tryN_5__5 != __instance._triesTotal_5__4;
                if (IsShipgenSummaryOnly())
                {
                    string result = success ? "Success" : "Failure";
                    string rejected = success ? string.Empty : $", rejected={ShipgenIssueSummary(__instance.__4__this, "final validation failed")}";
                    float elapsed = Patch_Ship._ShipgenStartedAt > 0f ? Time.realtimeSinceStartup - Patch_Ship._ShipgenStartedAt : 0f;
                    Melon<TweaksAndFixes>.Logger.Msg($"Shipgen result: {ShipgenHullSummary(__instance.__4__this)}, result={result}, attempts={__instance._tryN_5__5}/{__instance._triesTotal_5__4}, elapsed={elapsed:F1}s{rejected}");
                    Patch_Ship.LogShipgenRandPartAttemptSummary();
                    Patch_Ship.LogShipgenPhaseSummary();
                    Patch_Ship.ClearRandPartCandidateStats();
                    Patch_Ship.ResetShipgenRandPartAttemptStats();
                    Patch_Ship.ResetShipgenPhaseStats();
                    return;
                }

                Melon<TweaksAndFixes>.Logger.Msg($"  Attempts : {__instance._tryN_5__5} / {__instance._triesTotal_5__4}");
                Melon<TweaksAndFixes>.Logger.Msg($"  Result   : {(success ? "Success" : "Failure")}");
                if (!success)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Rejected : {ShipgenIssueSummary(__instance.__4__this, "final validation failed")}");
                    Melon<TweaksAndFixes>.Logger.Msg($"  {Patch_Ship.SummarizeAndClearRandPartCandidateStats()}");
                    Patch_Ship.LogShipgenRandPartAttemptSummary();
                    Patch_Ship.LogShipgenPhaseSummary();
                    Patch_Ship.PrintShipgenLifecycleSummary(__instance.__4__this);
                }
                if (success)
                {
                    PrintShipgenFinalSummary(__instance.__4__this);
                    Melon<TweaksAndFixes>.Logger.Msg($"  {Patch_Ship.SummarizeAndClearRandPartCandidateStats()}");
                    Patch_Ship.LogShipgenRandPartAttemptSummary();
                    Patch_Ship.LogShipgenPhaseSummary();
                    Patch_Ship.PrintShipgenLifecycleSummary(__instance.__4__this);
                }
                Patch_Ship.ResetShipgenRandPartAttemptStats();
                Patch_Ship.ResetShipgenPhaseStats();
            }

        }

        [HarmonyPatch(nameof(Ship._GenerateRandomShip_d__573.MoveNext))]
        [HarmonyPrefix]
        internal static bool Prefix_MoveNext(Ship._GenerateRandomShip_d__573 __instance, out GRSData __state, ref bool __result)
        {
            Patch_Ship._GenerateRandomShipRoutine = __instance;
            Patch_Ship.MarkShipgenGunNormalizationActive();
            ApplyShipgenAttemptCap(__instance);

            // TODO:
            //   Remove whole screen blocker, block part selector & side menus
            //   Add pause/step buttons

            // So we know what state we started in.
            __state = new GRSData();
            __state.state = __instance.__1__state;
            __state.tryNum = __instance._tryN_5__5;
            __state.startedAt = Time.realtimeSinceStartup;
            Patch_Ship._GenerateShipState = __state.state;
            var ship = __instance.__4__this;
            var hd = ship.hull.data;
            __state.speed = ship.SpeedMax();
            __state.beam = ship.Beam();
            __state.beamMin = hd.beamMin;
            __state.beamMax = hd.beamMax;
            __state.draught = ship.Draught();
            __state.draughtMin = hd.draughtMin;
            __state.draughtMax = hd.draughtMax;
            lastState = __state;

            if (__instance.__1__state > 1)
            {
                Patch_Ship.NormalizeShipgenGunCaliberModifiers(ship);
                ClampShipStats(ship);
                OptimizeComponents(ship);
                Patch_Ship.NormalizeShipgenSpeed(ship);
            }

            switch (__state.state)
            {
                case 0:
                    ApplyShipgenAttemptCap(__instance);
                    Patch_Ship._ShipgenStartedAt = Time.realtimeSinceStartup;
                    Patch_Ship._AttemptStartedAt = Patch_Ship._ShipgenStartedAt;
                    Patch_Ship._ShipgenAttemptTraceId = Math.Max(1, __instance._tryN_5__5);
                    __instance.__4__this.TAFData().ResetAllGrades();
                    Patch_Ship.ClearRandPartCandidateStats();
                    Patch_Ship.ResetShipgenMainGunDownsize();
                    Patch_Ship.ResetShipgenAttemptGunLimiter(ship);
                    Patch_Ship._FastRetriedTowerBeforeMainGun = false;
                    Patch_Ship.ForceMaxShipgenDisplacement(ship);
                    break;

                case 6:
                    Patch_Ship.ForceMaxShipgenDisplacement(ship);

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

                    Patch_Ship.NormalizeShipgenSpeed(ship, true);

                    // We can't do the frame-wait thing easily, let's just advance straight-away
                    __instance.__1__state = 7;
                    break;

                case 8:
                    if (Patch_Ship.TryGenerateSpecialTbParts(ship))
                    {
                        __instance.__1__state = 10;
                        __result = true;
                        return false;
                    }
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
                    Patch_Ship.NormalizeShipgenSpeed(ship, true);

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
        internal static void Postfix_MoveNext(Ship._GenerateRandomShip_d__573 __instance, GRSData __state, ref bool __result)
        {
            if (Config.ShipGenTweaks)
                Patch_Ship.RecordShipgenPhase($"grs_state_{__state.state:00}_{Patch_Ship.ShipgenGeneratorStateLabel(__state.state)}", Time.realtimeSinceStartup - __state.startedAt);

            var ship = __instance.__4__this;
            var hd = ship.hull.data;
            hd.beamMin = __state.beamMin;
            hd.beamMax = __state.beamMax;
            hd.draughtMin = __state.draughtMin;
            hd.draughtMax = __state.draughtMax;
            // For now, we're going to reset all grades regardless.
            //if (__state == 1 && (!__instance._isRefitMode_5__2 || !__instance.isSimpleRefit))
            //    __instance.__4__this.TAFData().ResetAllGrades();

            if (__instance.__1__state > 1)
            {
                Patch_Ship.NormalizeShipgenGunCaliberModifiers(ship);
                ClampShipStats(ship);
                OptimizeComponents(ship);
                Patch_Ship.NormalizeShipgenSpeed(ship);
            }

            switch (__state.state)
            {
                case 0:
                    Patch_Ship.ForceMaxShipgenDisplacement(ship);

                    if (Config.ShipGenTweaks)
                    {
                        Patch_Ship.ResetShipgenAttemptGunLimiter(__instance.__4__this);

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

            if (__state.tryNum != __instance._tryN_5__5 && __instance._tryN_5__5 != __instance._triesTotal_5__4)
            {
                PrintShipgenIssues(__instance, ship);
                Patch_Ship.RecordShipgenAttemptMainGunResult(ship);
                Patch_Ship._ShipgenAttemptTraceId = Math.Max(1, __instance._tryN_5__5);
                Patch_Ship.ResetShipgenAttemptGunLimiter(ship);
                Patch_Ship._FastRetriedTowerBeforeMainGun = false;
                Patch_Ship._AttemptStartedAt = Time.realtimeSinceStartup;
                Patch_Ship.ResetShipgenPhaseStats();
            }
        }
    }


    [HarmonyPatch(typeof(Ship._AddRandomPartsNew_d__591))]
    internal class Patch_Ship_AddRandParts
    {
        internal struct ARPData
        {
            public int state;
            public float startedAt;
        }

        [HarmonyPatch(nameof(Ship._AddRandomPartsNew_d__591.MoveNext))]
        [HarmonyPrefix]
        internal static void Prefix_MoveNext(Ship._AddRandomPartsNew_d__591 __instance, out ARPData __state)
        {
            Patch_Ship._AddRandomPartsRoutine = __instance;
            Patch_Ship.MarkShipgenGunNormalizationActive();
            __state = new ARPData
            {
                state = __instance.__1__state,
                startedAt = Time.realtimeSinceStartup
            };
            if (__state.state == 0)
                Patch_Ship.ResetShipgenRandPartAttemptStatsForAddPass();
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
        internal static void Postfix_MoveNext(Ship._AddRandomPartsNew_d__591 __instance, ARPData __state, ref bool __result)
        {
            Patch_Ship.RecordShipgenPhase($"addparts_state_{__state.state:00}_{Patch_Ship.ShipgenAddPartsStateLabel(__state.state)}", Time.realtimeSinceStartup - __state.startedAt);

            if (Patch_Ship.ShouldFastRetryTowerBeforeMainGun(__instance.__4__this))
            {
                Patch_Ship._FastRetriedTowerBeforeMainGun = true;
                __instance.__1__state = -2;
                __result = false;

                if (Patch_Ship.IsShipgenDebugEnabled())
                {
                    Patch_Ship.CountMainGunCandidateStats(out int seen, out int accepted);
                    float elapsed = Patch_Ship._AttemptStartedAt > 0f ? Time.realtimeSinceStartup - Patch_Ship._AttemptStartedAt : 0f;
                    Melon<TweaksAndFixes>.Logger.Msg($"  Fast retry: main tower placed, no main guns after {seen} candidate checks ({accepted} accepted), elapsed={elapsed:F1}s.");
                }
            }

            if (!__result)
                Patch_Ship.FinishCurrentRandPartTiming();

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

    [HarmonyPatch(typeof(Ship))]
    internal class Patch_Ship_SetSpeedMax
    {
        [HarmonyPatch(nameof(Ship.SetSpeedMax))]
        [HarmonyPrefix]
        internal static void Prefix_SetSpeedMax(Ship __instance, ref float speedMax)
        {
            Patch_Ship.NormalizeShipgenSpeedArgument(__instance, ref speedMax);
        }
    }

    [HarmonyPatch(typeof(Ship))]
    internal class Patch_Ship_FindDeckAtPoint
    {
        [HarmonyPatch(nameof(Ship.FindDeckAtPoint))]
        [HarmonyPostfix]
        internal static void Postfix_FindDeckAtPoint(Ship __instance, Vector3 point, BoxCollider __result)
        {
            Patch_Ship.TraceShipgenDeckProbe(__instance, point, __result);
        }
    }
}
