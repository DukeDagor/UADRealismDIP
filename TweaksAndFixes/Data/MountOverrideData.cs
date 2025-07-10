using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppSystem.Linq;
using static TweaksAndFixes.Serializer;

namespace TweaksAndFixes
{
    internal class MountOverrideData : Serializer.IPostProcess
    {
        private static readonly Dictionary<string, MountOverrideData> _Data = new();
        private static readonly Dictionary<string, Dictionary<int, MountOverrideData>> _ParentToData = new();
        private static readonly Dictionary<string, List<MountOverrideData>> _ParentToNewData = new();
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

            if (angle_right - angle_left > 360)
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

            // Melon<TweaksAndFixes>.Logger.Msg($" Loaded: {parent + " | " + index}");
            if (index != -1)
            {
                _Data[parent + "_" + index] = this;

                if (!_ParentToData.ContainsKey(parent)) _ParentToData[parent] = new Dictionary<int, MountOverrideData>();
                if (!_ParentToData.ContainsKey(parent.Split("/")[0])) _ParentToData[parent.Split("/")[0]] = new Dictionary<int, MountOverrideData>();
                _ParentToData[parent][index] = this;
            }
            else
            {
                if (!_ParentToNewData.ContainsKey(parent)) _ParentToNewData[parent] = new List< MountOverrideData>();
                if (!_ParentToNewData.ContainsKey(parent.Split("/")[0])) _ParentToNewData[parent.Split("/")[0]] = new List<MountOverrideData>();
                _ParentToNewData[parent].Add(this);
            }
        }

        private static void UpdateMountParamiters(Mount mount, MountOverrideData mountOverride)
        {
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

        public static void OverrideMountData()
        {
            HashSet<string> parsedModels = new HashSet<string>();

            MountTemplate = new GameObject();
            MountTemplate.name = "TAF Mount Template";
            MountTemplate.transform.SetParent(null);
            MountTemplate.transform.position = Vector3.zero;
            MountTemplate.transform.rotation = new Quaternion();
            MountTemplate.AddComponent<Mount>();

            foreach (var data in G.GameData.parts)
            {
                bool isAutoOverrideType = data.Value.type == "barbette" || data.Value.type == "tower_main" || data.Value.type == "tower_sec";
                bool hasManualOverride = _ParentToData.ContainsKey(data.Value.model);
                bool hasNewMounts = _ParentToNewData.ContainsKey(data.Value.model);

                if ((!isAutoOverrideType && !hasManualOverride && !hasNewMounts) || parsedModels.Contains(data.Value.model))
                {
                    continue;
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"Loading: {data.Key}...");
                GameObject obj = Util.ResourcesLoad<GameObject>(data.Value.model);

                parsedModels.Add(data.Value.model);

                if (obj == null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                    continue;
                }

                var children = obj.GetChildren();
                int count = 0;

                // Melon<TweaksAndFixes>.Logger.Msg($"Checking type: {data.Value.type}");

                if (data.Value.type == "hull")
                {
                    // Hierarchy:
                    //  Visual:
                    //    Sections:
                    //      Stern/Middle/Bow

                    // Melon<TweaksAndFixes>.Logger.Msg($"\n{ModUtils.DumpHierarchy(obj)}");

                    children = obj.GetChild("Visual").GetChild("Sections").GetChildren();
                }

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    GameObject child = children[i];

                    if (child == null) continue;

                    if (data.Value.type == "hull")
                    {
                        int subCount = 0;
                        var subChildren = child.GetChildren();

                        for (int j = subChildren.Count - 1; j >= 0; j--)
                        {
                            GameObject subChild = subChildren[j];

                            if (subChild == null) continue;

                            if (subChild.name.StartsWith("mount:TAF_"))
                            {
                                subChild.transform.SetParent(null);
                                subChild.TryDestroy();
                                continue;
                            }

                            if (!subChild.name.StartsWith("Mount")) continue;

                            subCount++;

                            bool hasSubOverride = hasManualOverride && _ParentToData.ContainsKey(data.Value.model + "/" + child.name) && _ParentToData[data.Value.model + "/" + child.name].ContainsKey(count);

                            if (!hasSubOverride)
                            {
                                if (subChild.name.StartsWith("Mount:tower_main") || subChild.name.StartsWith("Mount:tower_sec") || subChild.name.StartsWith("Mount:funnel") || subChild.name.StartsWith("Mount:si_barbette"))
                                {
                                    // Melon<TweaksAndFixes>.Logger.Msg($"Banishing: {data.Value.model + "/" + child.name + "/" + subCount} to the shadow relm");
                                    // subChild.transform.SetParent(null);
                                    // subChild.TryDestroy();
                                    // subChild.transform.position = new Vector3(-100000,-100000,-100000);
                                    continue;
                                }

                                if ((int)subChild.transform.position.x == 0)
                                {
                                    // Melon<TweaksAndFixes>.Logger.Msg($"Banishing: {data.Value.model + "/" + child.name + "/" + subCount} to the shadow relm");
                                    // subChild.transform.position = new Vector3(-100000, -100000, -100000);
                                    continue;
                                }

                                continue;
                            }

                            // Check if the part has a Mount
                            Mount subMount = subChild.GetComponent<Mount>();

                            if (subMount == null)
                            {
                                continue;
                            }

                            // child.TryDestroyComponent<Mount>();
                            // Mount subMount = child.AddComponent<Mount>();

                            if (_ParentToData[data.Value.model + "/" + child.name][count].enabled != 0)
                            {
                                subChild.transform.SetParent(null);
                                subChild.TryDestroy();
                                continue;
                            }

                            // Melon<TweaksAndFixes>.Logger.Msg($"Overriding: {data.Value.model + "/" + child.name + "/" + subCount}");

                            // Update paramiters
                            UpdateMountParamiters(subMount, _ParentToData[data.Value.model + "/" + child.name][count]);
                            // subMount.packNumber = subMount.packNumber;
                        }

                        if (_ParentToNewData.ContainsKey(data.Value.model + "/" + child.name))
                        {
                            foreach (MountOverrideData newData in _ParentToNewData[data.Value.model + "/" + child.name])
                            {
                                GameObject newMount = GameObject.Instantiate(MountTemplate);

                                newMount.transform.SetParent(child);
                                newMount.name = $"mount:TAF_{child.GetChildren().Count}";
                                
                                UpdateMountParamiters(newMount.GetComponent<Mount>(), newData);

                                canary = newMount;
                            }
                        }

                        continue;
                    }

                    if (child.name.StartsWith("mount:TAF_"))
                    {
                        child.transform.SetParent(null);
                        child.TryDestroy();
                        continue;
                    }

                    if (!child.name.StartsWith("Mount")) continue;

                    count++;

                    bool hasOverride = hasManualOverride && _ParentToData[data.Value.model].ContainsKey(count);

                    if (!hasOverride)
                    {
                        if (data.Value.type == "barbette")
                        {
                            if (!child.name.StartsWith("Mount:barbette"))
                            {
                                child.transform.position = new Vector3(-100000, -100000, -100000);
                                // child.transform.SetParent(null);
                                // child.TryDestroy();
                                //if (!_ParentToDeletedMounts.ContainsKey(data.Value.model)) _ParentToDeletedMounts[data.Value.model] = new();
                                //_ParentToDeletedMounts[data.Value.model].Add(count);
                                continue;
                            }
                        }
                        else if (data.Value.type == "tower_main")
                        {
                            if (child.name.StartsWith("Mount:tower_sec") || child.name.StartsWith("Mount:si_barbette"))
                            {
                                child.transform.position = new Vector3(-100000, -100000, -100000);
                                // child.transform.SetParent(null);
                                // child.TryDestroy();
                                //if (!_ParentToDeletedMounts.ContainsKey(data.Value.model)) _ParentToDeletedMounts[data.Value.model] = new();
                                //_ParentToDeletedMounts[data.Value.model].Add(count);
                                continue;
                            }
                        }
                        else if (data.Value.type == "tower_sec")
                        {
                            if (child.name.StartsWith("Mount:tower_main") || child.name.StartsWith("Mount:si_barbette"))
                            {
                                child.transform.position = new Vector3(-100000, -100000, -100000);
                                // child.transform.SetParent(null);
                                // child.TryDestroy();
                                //if (!_ParentToDeletedMounts.ContainsKey(data.Value.model)) _ParentToDeletedMounts[data.Value.model] = new();
                                //_ParentToDeletedMounts[data.Value.model].Add(count);
                                continue;
                            }
                        }

                        continue;
                    }

                    // Check if the part has a Mount
                    Mount mount = child.GetComponent<Mount>();

                    if (mount == null)
                    {
                        continue;
                    }

                    // child.TryDestroyComponent<Mount>();
                    // Mount mount = child.AddComponent<Mount>();

                    if (_ParentToData[data.Value.model][count].enabled == 0)
                    {
                        child.transform.position = new Vector3(-100000, -100000, -100000);
                        // child.transform.SetParent(null);
                        // child.TryDestroy();
                        continue;
                    }

                    // Update paramiters
                    UpdateMountParamiters(mount, _ParentToData[data.Value.model][count]);
                    // mount.packNumber = mount.packNumber;
                }
                
                if (hasNewMounts)
                {
                    foreach (MountOverrideData newData in _ParentToNewData[data.Value.model])
                    {
                        GameObject newMount = GameObject.Instantiate(MountTemplate);

                        newMount.transform.SetParent(obj);
                        newMount.name = $"mount:TAF_{obj.GetChildren().Count}";

                        UpdateMountParamiters(newMount.GetComponent<Mount>(), newData);

                        canary = newMount;
                    }
                    // Melon<TweaksAndFixes>.Logger.Msg($"New Higherarchy:\n{ModUtils.DumpHierarchy(obj)}");
                }
            }

            foreach (var data in G.GameData.partModels)
            {
                foreach (var model in data.Value.models)
                {
                    // if (data.value.type != "barbette") continue;

                    bool hasManualOverride = _ParentToData.ContainsKey(model.Value);
                    bool hasNewMounts = _ParentToNewData.ContainsKey(model.Value);

                    if (!hasManualOverride || hasNewMounts || parsedModels.Contains(model.Value))
                    {
                        continue;
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg($"Loading: {data.Key}...");
                    GameObject obj = Util.ResourcesLoad<GameObject>(model.Value);

                    parsedModels.Add(model.Value);

                    if (obj == null)
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                        continue;
                    }

                    var children = obj.GetChildren();
                    int count = 0;

                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        GameObject child = children[i];

                        if (child == null) continue;

                        if (child.name.StartsWith("mount:TAF_"))
                        {
                            child.transform.SetParent(null);
                            child.TryDestroy();
                            continue;
                        }

                        if (!child.name.StartsWith("Mount")) continue;

                        // Melon<TweaksAndFixes>.Logger.Msg($"  Checking: {model.Value} - {count}...");

                        count++;

                        if (!_ParentToData[model.Value].ContainsKey(count))
                        {
                            continue;
                        }

                        // Check if the part has a Mount
                        Mount mount = child.GetComponent<Mount>();

                        if (mount == null)
                        {
                            continue;
                        }

                        // child.TryDestroyComponent<Mount>();
                        // Mount mount = child.AddComponent<Mount>();

                        if (_ParentToData[model.Value][count].enabled == 0)
                        {
                            child.transform.position = new Vector3(-100000, -100000, -100000);
                            // child.transform.SetParent(null);
                            // child.TryDestroy();
                            continue;
                        }

                        // Update paramiters
                        UpdateMountParamiters(mount, _ParentToData[model.Value][count]);
                        mount.packNumber = mount.packNumber;
                    }

                    if (hasNewMounts)
                    {
                        foreach (MountOverrideData newData in _ParentToNewData[model.Value])
                        {
                            GameObject newMount = GameObject.Instantiate(MountTemplate);

                            newMount.transform.SetParent(obj);
                            newMount.name = $"mount:TAF_{obj.GetChildren().Count}";

                            UpdateMountParamiters(newMount.GetComponent<Mount>(), newData);

                            canary = newMount;
                        }
                        // Melon<TweaksAndFixes>.Logger.Msg($"New Higherarchy:\n{ModUtils.DumpHierarchy(obj)}");
                    }
                }
            }

        }

        // Load CSV with comment lines and a default line.
        public static void LoadData()
        {
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

            Serializer.CSV.Read<List<MountOverrideData>, MountOverrideData>(text, list, true, true);

            Melon<TweaksAndFixes>.Logger.Msg($"Loaded {list.Count} mount overrides.");
        }
    }

    [HarmonyPatch(typeof(Util))]
    internal class Util_Clear_Resource_Cache
    {
        [HarmonyPatch(nameof(Util.ClearResourcesCache))]
        [HarmonyPostfix]
        internal static void Postfix_ClearResourcesCache()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Reloading Mount Overrides after cache clear...");
            MountOverrideData.OverrideMountData();
            Melon<TweaksAndFixes>.Logger.Msg($"Done!");
        }
    }
}
