using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;
using System.Text;
using static Il2Cpp.Ship;
using static MelonLoader.MelonLogger;

namespace TweaksAndFixes
{
    internal class MountOverrideData : Serializer.IPostProcess
    {
        public static readonly Dictionary<string, MountOverrideData> _Data = new();
        public static readonly Dictionary<string, Dictionary<int, MountOverrideData>> _ParentToData = new();
        private static readonly Dictionary<string, List<MountOverrideData>> _ParentToNewData = new();
        private static readonly HashSet<string> _ModelsWithOverrides = new();
        public static GameObject MountTemplate = new GameObject();
        public static GameObject canary = new();
        public static Dictionary<string, GameObject> localCache = new();

        [Serializer.Field] public int index = -1;
        [Serializer.Field] public int enabled = 0;
        [Serializer.Field] public string parent = string.Empty;
        [Serializer.Field] public float rotation = 0;
        [Serializer.Field] public string position = string.Empty;
        [Serializer.Field] public string mount_pos_type = string.Empty;
        [Serializer.Field] public string accepts = string.Empty;
        [Serializer.Field] public float caliber_min = 0;
        [Serializer.Field] public float caliber_max = 0;
        [Serializer.Field] public int barrels_min = 0;
        [Serializer.Field] public int barrels_max = 0;
        [Serializer.Field] public string collision = string.Empty;
        [Serializer.Field] public float angle_left = 0;
        [Serializer.Field] public float angle_right = 0;
        [Serializer.Field] public string orientation = string.Empty;
        [Serializer.Field] public int rotate_same = 0;

        public enum MountPositionType { ANY, CENTER, SIDE }

        public Vector3 positionParsed = Vector3.zero;
        public MountPositionType mountPositionTypeParsed = MountPositionType.ANY;
        public string[] acceptsParsed = Array.Empty<string>();
        public string[] collisionParsed = Array.Empty<string>();

        public static string[] VALID_ACCEPT_TYPES = { "any", "tower_main", "tower_sec", "funnel", "si_barbette", "barbette", "casemate", "sub_torpedo", "deck_torpedo", "special" };
        public static string[] VALID_COLLISION_TYPES = { "check_all", "ignore_collision_check", "ignore_parent", "ignore_expand", "ignore_height", "ignore_fire_angle_check", "casemate_ignore_collision" };

        public static bool HasEntries()
        {
            return _Data.Count > 0;
        }

        // Check values
        public void PostProcess()
        {
            if (rotation > 360) rotation %= 360;
            else if (rotation < 0) rotation = 360 - ((-rotation) % 360);

            if (angle_right - angle_left > 365)
            {
                Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Invalid angle_left/angle_right. Sum of absolute value of angles is greter than 360: `{angle_left} + {-angle_right}`");
            }

            var posData = position[1..^1].Replace(" ", "").Split(",");
            if (posData.Length == 3)
            {
                positionParsed = new Vector3(float.Parse(posData[0]), float.Parse(posData[1]), float.Parse(posData[2]));
            }
            else
            {
                Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Invalid position `{position}`");
            }

            if (mount_pos_type == "center") mountPositionTypeParsed = MountPositionType.CENTER;
            else if (mount_pos_type == "side") mountPositionTypeParsed = MountPositionType.SIDE;
            else if (mount_pos_type == "any") mountPositionTypeParsed = MountPositionType.ANY;
            else
            {
                Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Invalid mount position type `{mount_pos_type}`");
            }

            acceptsParsed = accepts.Replace(" ", "").Split(",");

            foreach (string acceptsValue in acceptsParsed)
            {
                if (!VALID_ACCEPT_TYPES.Contains(acceptsValue))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Invalid mountable part type `{acceptsValue}`");
                }
            }

            collisionParsed = collision.Replace(" ", "").Split(",");

            foreach (string collisionValue in collisionParsed)
            {
                if (!VALID_COLLISION_TYPES.Contains(collisionValue))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Invalid collision type `{collisionValue}`");
                }
            }

            if (orientation.Length > 0 && orientation != "fore/aft" && orientation != "starboard/port")
            {
                Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Invalid orientation type `{orientation}`");
            }

            // Only used for base game bug hunting
            // if (collisionParsed.Contains("check_all") && ((int)-angle_left != (int)angle_right) && mountPositionTypeParsed == MountPositionType.CENTER)
            // {
            //     Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Asymetric angle override `{angle_left}` / `{angle_right}`");
            // }

            // Melon<TweaksAndFixes>.Logger.Msg($" Loaded: {parent + " | " + index}");
            if (index != -1)
            {
                _Data[parent + "_" + index] = this;

                if (!_ParentToData.ContainsKey(parent)) _ParentToData[parent] = new Dictionary<int, MountOverrideData>();
                // if (!_ParentToData.ContainsKey(parent.Split("/")[0])) _ParentToData[parent.Split("/")[0]] = new Dictionary<int, MountOverrideData>();
                if (_ParentToData[parent].ContainsKey(index))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"MountOverrideData: [{parent + " | " + index}] Duplicate indexed override!");
                }
                _ParentToData[parent][index] = this;
                _ModelsWithOverrides.Add(parent.Split("/")[0]);
            }
            else
            {
                if (!_ParentToNewData.ContainsKey(parent)) _ParentToNewData[parent] = new List< MountOverrideData>();
                // if (!_ParentToNewData.ContainsKey(parent.Split("/")[0])) _ParentToNewData[parent.Split("/")[0]] = new List<MountOverrideData>();
                _ParentToNewData[parent].Add(this);
                _ModelsWithOverrides.Add(parent.Split("/")[0]);
            }
        }

        private static void UpdateMountParamitersRelitive(Mount mount, MountOverrideData mountOverride, GameObject root)
        {
            if (mountOverride.enabled == 0)
            {
                mount.transform.position = new Vector3(-10000, -10000, -10000);
                return;
            }

            // Melon<TweaksAndFixes>.Logger.Msg($"      {mount.gameObject.name}");
            // Melon<TweaksAndFixes>.Logger.Msg($"      {mount.transform.position} -> {mount.transform.localPosition} -> {mountOverride.positionParsed}");

            mount.transform.localPosition = mountOverride.positionParsed;
            mount.transform.localEulerAngles = new Vector3(mount.transform.localEulerAngles.x, mountOverride.rotation, mount.transform.localEulerAngles.z);
            mount.center = mountOverride.mountPositionTypeParsed == MountPositionType.CENTER;
            mount.side = mountOverride.mountPositionTypeParsed == MountPositionType.SIDE;

            mount.towerMain = mountOverride.acceptsParsed.Contains("tower_main");
            mount.towerSec = mountOverride.acceptsParsed.Contains("tower_sec");
            mount.funnel = mountOverride.acceptsParsed.Contains("funnel");
            mount.siBarbette = mountOverride.acceptsParsed.Contains("si_barbette");
            mount.barbette = mountOverride.acceptsParsed.Contains("barbette");
            mount.casemate = mountOverride.acceptsParsed.Contains("casemate");
            mount.subTorpedo = mountOverride.acceptsParsed.Contains("sub_torpedo");
            mount.deckTorpedo = mountOverride.acceptsParsed.Contains("deck_torpedo");
            mount.special = mountOverride.acceptsParsed.Contains("special");
            
            mount.caliberMin = mountOverride.caliber_min;
            mount.caliberMax = mountOverride.caliber_max;

            mount.barrelsMin = mountOverride.barrels_min;
            mount.barrelsMax = mountOverride.barrels_max;

            if (!mountOverride.collisionParsed.Contains("check_all"))
            {
                mount.ignoreCollisionCheck = mountOverride.collisionParsed.Contains("ignore_collision_check");
                mount.ignoreParent = mountOverride.collisionParsed.Contains("ignore_parent");
                mount.ignoreExpand = mountOverride.collisionParsed.Contains("ignore_expand");
                mount.ignoreHeight = mountOverride.collisionParsed.Contains("ignore_height");
                mount.ignoreFireAngleCheck = mountOverride.collisionParsed.Contains("ignore_fire_angle_check");
                mount.casemateIgnoreCollision = mountOverride.collisionParsed.Contains("casemate_ignore_collision");
            }
            else
            {
                mount.ignoreCollisionCheck = false;
                mount.ignoreParent = false;
                mount.ignoreExpand = false;
                mount.ignoreHeight = false;
                mount.ignoreFireAngleCheck = false;
                mount.casemateIgnoreCollision = false;
            }

            mount.angleLeft = mountOverride.angle_left;
            mount.angleRight = mountOverride.angle_right;

            mount.rotateForwardBack = mountOverride.orientation == "fore/aft";
            mount.rotateLeftRight = mountOverride.orientation == "starboard/port";

            mount.rotateSame = mountOverride.rotate_same == 1;
        }

        private static void UpdateMountParamiters(Mount mount, MountOverrideData mountOverride)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"      {mount.gameObject.name}");
            // Melon<TweaksAndFixes>.Logger.Msg($"      {mount.transform.localPosition} -> {mountOverride.positionParsed}");

            if (mountOverride.enabled == 0)
            {
                mount.transform.position = new Vector3(-10000, -10000, -10000);
                return;
            }

            mount.transform.position = mountOverride.positionParsed;
            mount.transform.eulerAngles = new Vector3(mount.transform.eulerAngles.x, mountOverride.rotation, mount.transform.eulerAngles.z);
            mount.center = mountOverride.mountPositionTypeParsed == MountPositionType.CENTER;
            mount.side = mountOverride.mountPositionTypeParsed == MountPositionType.SIDE;

            mount.towerMain = mountOverride.acceptsParsed.Contains("tower_main");
            mount.towerSec = mountOverride.acceptsParsed.Contains("tower_sec");
            mount.funnel = mountOverride.acceptsParsed.Contains("funnel");
            mount.siBarbette = mountOverride.acceptsParsed.Contains("si_barbette");
            mount.barbette = mountOverride.acceptsParsed.Contains("barbette");
            mount.casemate = mountOverride.acceptsParsed.Contains("casemate");
            mount.subTorpedo = mountOverride.acceptsParsed.Contains("sub_torpedo");
            mount.deckTorpedo = mountOverride.acceptsParsed.Contains("deck_torpedo");
            mount.special = mountOverride.acceptsParsed.Contains("special");

            mount.caliberMin = mountOverride.caliber_min;
            mount.caliberMax = mountOverride.caliber_max;

            mount.barrelsMin = mountOverride.barrels_min;
            mount.barrelsMax = mountOverride.barrels_max;

            if (!mountOverride.collisionParsed.Contains("check_all"))
            {
                mount.ignoreCollisionCheck = mountOverride.collisionParsed.Contains("ignore_collision_check");
                mount.ignoreParent = mountOverride.collisionParsed.Contains("ignore_parent");
                mount.ignoreExpand = mountOverride.collisionParsed.Contains("ignore_expand");
                mount.ignoreHeight = mountOverride.collisionParsed.Contains("ignore_height");
                mount.ignoreFireAngleCheck = mountOverride.collisionParsed.Contains("ignore_fire_angle_check");
                mount.casemateIgnoreCollision = mountOverride.collisionParsed.Contains("casemate_ignore_collision");
            }
            else
            {
                mount.ignoreCollisionCheck = false;
                mount.ignoreParent = false;
                mount.ignoreExpand = false;
                mount.ignoreHeight = false;
                mount.ignoreFireAngleCheck = false;
                mount.casemateIgnoreCollision = false;
            }

            mount.angleLeft = mountOverride.angle_left;
            mount.angleRight = mountOverride.angle_right;

            mount.rotateForwardBack = mountOverride.orientation == "fore/aft";
            mount.rotateLeftRight = mountOverride.orientation == "starboard/port";

            mount.rotateSame = mountOverride.rotate_same == 1;
        }

        public static void ApplyMountOverride(Part part, GameObject model, string root, bool relitive = false, bool force = false)
        {
            if (!force)
            {
                if (model.gameObject.GetChild("TAF_HAS_MOUNT_OVERRIDE", true) != null)
                {
                    return;
                }

                GameObject tag = new();
                tag.name = "TAF_HAS_MOUNT_OVERRIDE";
                tag.transform.parent = model.transform;
            }

            bool DisableStickyMounts = Config.Param("taf_mounts_sticky_disable", 1) == 1;
            bool DisableHullMounts = Config.Param("taf_mounts_hull_disable", 0) == 1;

            Dictionary<string, int> depthToIndex = new Dictionary<string, int>();

            if (model == null)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                return;
            }

            Patch_Ship.LastCreatedShip = ShipM.GetActiveShip();

            float mountParamMin = Patch_Part.GetMountMinParam(part);
            float mountParamMax = Patch_Part.GetMountMaxParam(part);
            float mountParamMult = Patch_Part.GetMountMultParam(part);


            Stack<GameObject> stack = new();

            stack.Push(model);

            //foreach (GameObject child in model.GetChildren())
            //{
            //    stack.Push(child);
            //}

            string partName = part.gameObject.GetChildren()[0].name;

            if (partName == "Effects") partName = part.gameObject.GetChildren()[1].name;

            if (relitive) partName = partName.Replace("(Clone)", "");

            if (!BaseGamePartModelData._Data.ContainsKey(partName))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Error! Key for {partName} not found! Failed to override {part.Name()}!");
                Melon<TweaksAndFixes>.Logger.Msg($"{ModUtils.DumpHierarchy(part.gameObject)}");
                return;
            }

            bool isHull     = BaseGamePartModelData._Data[partName].isHull;
            bool isTower    = BaseGamePartModelData._Data[partName].isTowerMain || BaseGamePartModelData._Data[partName].isTowerSec;
            bool isBarbette = BaseGamePartModelData._Data[partName].isBarbette;

            StringBuilder path = new(4096);

            // Melon<TweaksAndFixes>.Logger.Msg($"Parsing: {partName}...");
            // Melon<TweaksAndFixes>.Logger.Msg($"  {model.name} : {model.transform.position}");

            while (stack.Count > 0)
            {
                GameObject obj = stack.Pop();

                if (obj == null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                    continue;
                }

                var children = obj.GetChildren();

                foreach (GameObject child in children)
                {
                    stack.Push(child);
                }

                // Update path
                path.Clear();
                GameObject head = obj.GetParent();
                int stop = 10;
                bool isFirst = true;

                while (true)
                {
                    if (!relitive && head == null) break;
                    if (relitive && head == model.GetParent()) break;

                    path.Insert(0, head.name.Replace("(Clone)", "") + (isFirst ? "" : "/"));
                    isFirst = false;
                    head = head.GetParent();
                    if (stop-- == 0) break;
                }

                string concatPath = root + path.ToString();

                string trueConcatPath = concatPath + (concatPath.Length == 0 ? obj.name.Replace("(Clone)", "") : $"/{obj.name.Replace("(Clone)", "")}");

                if (_ParentToNewData.ContainsKey(trueConcatPath))
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"    Found new overrides at {trueConcatPath}");

                    foreach (MountOverrideData newData in _ParentToNewData[trueConcatPath])
                    {
                        GameObject newMount = new GameObject();
                        newMount.AddComponent<Mount>();
                        newMount.transform.SetParent(obj);
                        newMount.transform.position = obj.transform.position;
                        newMount.transform.localPosition = new Vector3();
                        newMount.transform.rotation = obj.transform.rotation;
                        newMount.transform.localRotation = new Quaternion();
                        newMount.name = $"Mount:TAF_{obj.GetChildren().Count}";

                        // Melon<TweaksAndFixes>.Logger.Msg($"      Mount:TAF_{obj.GetChildren().Count}");

                        Mount newMountMount = newMount.GetComponent<Mount>();

                        if (relitive) UpdateMountParamitersRelitive(newMountMount, newData, part.gameObject);
                        else UpdateMountParamiters(newMountMount, newData);

                        if (relitive && Patch_Ship.LastCreatedShip != null)
                        {
                            // Patch_Ship.LastCreatedShip.allowedMountsInternal.Add(newMount.GetComponent<Mount>());
                            Patch_Ship.LastCreatedShip.mounts.Add(newMountMount);

                            // Melon<TweaksAndFixes>.Logger.Msg($"    {(objPart != null ? objPart.Name() : "NULL")}");
                            part.mountsInside.Add(newMountMount);
                        }

                        if (mountParamMin != -1) newMountMount.caliberMin = mountParamMin;
                        else if (mountParamMult != -1) newMountMount.caliberMin *= mountParamMult;
                        if (mountParamMax != -1) newMountMount.caliberMax = mountParamMax;
                        else if (mountParamMult != -1) newMountMount.caliberMax *= mountParamMult;

                        // part.mountsInside.Add(newMount.GetComponent<Mount>());

                        // part.ship.mounts.Add(newMount.GetComponent<Mount>());

                        // canary = newMount;
                    }
                }
                    
                // if (obj.name.Contains("Lifeboat") && !obj.name.StartsWith("Decor:"))
                // {
                //     if (!obj.GetParent().name.StartsWith("Decor:"))
                //     {
                //         Melon<TweaksAndFixes>.Logger.Msg($"  STRUCTUAL BOAT: {obj.name}");
                //         obj.transform.SetParent(null);
                //         obj.TryDestroy();
                //         continue;
                //     }
                // }

                if (!obj.name.StartsWith("Mount:")) continue;

                if (obj.name.StartsWith("Mount:TAF"))
                {
                    Mount tafMount = obj.GetComponent<Mount>();

                    // Melon<TweaksAndFixes>.Logger.Msg($"Found TaF Mount!");

                    if (relitive && Patch_Ship.LastCreatedShip != null)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"  Deleting {obj.name}");

                        // Patch_Ship.LastCreatedShip.allowedMountsInternal.Add(newMount.GetComponent<Mount>());
                        if (Patch_Ship.LastCreatedShip.mounts.Contains(tafMount)) Patch_Ship.LastCreatedShip.mounts.Remove(tafMount);

                        Part objPart = model.GetParent().GetComponent<Part>();
                        if (objPart != null)
                        {
                            if (objPart.mountsInside.Contains(tafMount)) objPart.mountsInside.Remove(tafMount);
                        }

                        obj.TryDestroy();
                    }

                    obj.transform.SetParent(null);

                    continue;
                }

                if (!depthToIndex.ContainsKey(concatPath)) depthToIndex[concatPath] = 0;
                depthToIndex[concatPath]++;

                // Melon<TweaksAndFixes>.Logger.Msg($"  Found mount {obj.name} at index {depthToIndex[concatPath]}...");

                if (isBarbette && DisableStickyMounts)
                {
                    if (!obj.name.StartsWith("Mount:barbette"))
                    {
                        obj.transform.position = new Vector3(-100000, -100000, -100000);
                        continue;
                    }
                }
                else if (isTower && DisableStickyMounts)
                {
                    if (obj.name.StartsWith("Mount:tower_main") || obj.name.StartsWith("Mount:tower_sec") || obj.name.StartsWith("Mount:si_barbette"))
                    {
                        obj.transform.position = new Vector3(-100000, -100000, -100000);
                        continue;
                    }
                }
                else if (isHull && DisableHullMounts)
                {
                    if (obj.name.StartsWith("Mount:tower_main") || obj.name.StartsWith("Mount:tower_sec") || obj.name.StartsWith("Mount:funnel") || obj.name.StartsWith("Mount:si_barbette"))
                    {
                        obj.transform.position = new Vector3(-100000, -100000, -100000);
                        continue;
                    }
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"    Checking: {concatPath} for mount overrides...");

                if (_ParentToData.ContainsKey(concatPath))
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"    Found override for path...");

                    if (!_ParentToData[concatPath].ContainsKey(depthToIndex[concatPath])) continue;

                    // Melon<TweaksAndFixes>.Logger.Msg($"      Found mount at index {depthToIndex[concatPath]}");

                    Mount objMount = obj.GetComponent<Mount>();

                    if (relitive) UpdateMountParamitersRelitive(objMount, _ParentToData[concatPath][depthToIndex[concatPath]], part.gameObject);
                    else UpdateMountParamiters(objMount, _ParentToData[concatPath][depthToIndex[concatPath]]);

                    if (mountParamMin != -1) objMount.caliberMin = mountParamMin;
                    else if (mountParamMult != -1) objMount.caliberMin *= mountParamMult;
                    if (mountParamMax != -1) objMount.caliberMax = mountParamMax;
                    else if (mountParamMult != -1) objMount.caliberMax *= mountParamMult;
                }
            }
        }

        public static void OverrideMountData()
        {
            foreach (var data in BaseGamePartModelData._Data)
            {
                if (data.Value.hasStickyMounts)
                {
                    _ModelsWithOverrides.Add(data.Key);
                }
            }

            foreach (var data in _ModelsWithOverrides)
            {
                GameObject obj = Util.ResourcesLoad<GameObject>(data);

                if (obj == null)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data}");
                    continue;
                }

                // ApplyMountOverride(obj);

                // Melon<TweaksAndFixes>.Logger.Msg($"Part: {(part == null ? "NULL" : part.)}");
            }

            return;
        }

        // Load CSV with comment lines and a default line.
        public static void LoadData()
        {
            BaseGamePartModelData.LoadData();

            FilePath fp = Config._MountsFile;
            if (!fp.Exists)
            {
                return;
            }

            List<MountOverrideData> list = new List<MountOverrideData>();
            string? text = Serializer.CSV.GetTextFromFile(fp.path);

            if (text == null)
            {
                Melon<TweaksAndFixes>.Logger.Error($"Failed to load `mounts.csv`.");
                return;
            }

            _Data.Clear();
            _ParentToData.Clear();
            _ParentToNewData.Clear();

            Serializer.CSV.Read<List<MountOverrideData>, MountOverrideData>(text, list, true, true);

            Melon<TweaksAndFixes>.Logger.Msg($"Loaded {list.Count} mount overrides.");
        }

        public class BaseGamePartModelData : Serializer.IPostProcess
        {
            public static readonly Dictionary<string, BaseGamePartModelData> _Data = new();
            private static bool loaded = false;

            [Serializer.Field] public string model = string.Empty;
            [Serializer.Field] public string type = string.Empty;

            public bool isTorpedo = false;
            public bool isBarbette = false;
            public bool isHull = false;
            public bool isFunnel = false;
            public bool isTowerMain = false;
            public bool isTowerSec = false;
            public bool isGun = false;
            public bool isSpecial = false;

            public bool hasStickyMounts = false;

            public void PostProcess()
            {
                isTorpedo = type == "torpedo";
                isBarbette = type == "barbette";
                isHull = type == "hull";
                isFunnel = type == "funnel";
                isTowerMain = type == "tower_main";
                isTowerSec = type == "tower_sec";
                isGun = type == "gun";
                isSpecial = type == "special";

                hasStickyMounts = isBarbette || isTowerMain || isTowerSec;

                if (!isTorpedo && !isBarbette && !isHull && !isFunnel && !isTowerMain && !isTowerSec && !isGun && !isSpecial)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Built-In asset data type for `{model}` is invalid!");
                }

                _Data[model] = this;
            }

            // Load CSV with comment lines and a default line.
            public static void LoadData()
            {
                if (loaded) return;

                FilePath fp = Config._BaseGamePartModelDataFile;
                if (!fp.Exists)
                {
                    return;
                }

                List<BaseGamePartModelData> list = new List<BaseGamePartModelData>();
                string? text = Serializer.CSV.GetTextFromFile(fp.path);

                if (text == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Failed to load `TAFData/baseGamePartModelData.csv`.");
                    return;
                }

                Serializer.CSV.Read<List<BaseGamePartModelData>, BaseGamePartModelData>(text, list, true, true);

                loaded = true;
            }

        }
    }

    // [HarmonyPatch(typeof(Util))]
    // internal class Util_Clear_Resource_Cache
    // {
    //     [HarmonyPatch(nameof(Util.ClearResourcesCache))]
    //     [HarmonyPostfix]
    //     internal static void Postfix_ClearResourcesCache()
    //     {
    //         Melon<TweaksAndFixes>.Logger.Msg($"Reloading Mount Overrides after cache clear...");
    //         MountOverrideData.OverrideMountData();
    //         Melon<TweaksAndFixes>.Logger.Msg($"Done!");
    //     }
    // }
}
