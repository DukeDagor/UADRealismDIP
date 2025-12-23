using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Globalization;
using UnityEngine.UI;
using Il2CppTMPro;
using System.Text;
using System.Reflection.Metadata.Ecma335;
using System.IO.Compression;
using System.Data;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8625
#pragma warning disable CS8714

namespace TweaksAndFixes
{
    public static class ExtraGameData
    {
        public static Player? MainPlayer()
        {
            foreach (Player entry in CampaignController.Instance.CampaignData.Players)
            {
                if (entry.isMain)
                {
                    return entry;
                }
            }

            return null;
        }
    }

    public static class ModUtils
    {
        // Due to an Il2Cpp interop issue, you can't actually pass null nullables, you have to pass
        // nullables _with no value_. So we're going to just store a bunch of statics here we can use
        // instead of allocating each time.
        public static Il2CppSystem.Nullable<int>  _NullableEmpty_Int = new Il2CppSystem.Nullable<int>();

        public class SaveTextureToFileUtility
        {
            public enum SaveTextureFileFormat
            {
                JPG, PNG
            };

            static public void SaveTexture2DToFile(Texture2D tex, string filePath, SaveTextureFileFormat fileFormat, int jpgQuality = 95)
            {
                switch (fileFormat)
                {
                    case SaveTextureFileFormat.JPG:
                        System.IO.File.WriteAllBytes(filePath + ".jpg", tex.EncodeToJPG(jpgQuality));
                        break;
                    case SaveTextureFileFormat.PNG:
                        System.IO.File.WriteAllBytes(filePath + ".png", tex.EncodeToPNG());
                        break;
                }
            }

            static public void SaveRenderTextureToFile(RenderTexture renderTexture, string filePath, SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG, int jpgQuality = 95)
            {
                Texture2D tex;
                tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
                var oldRt = RenderTexture.active;
                RenderTexture.active = renderTexture;
                tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                tex.Apply();
                RenderTexture.active = oldRt;
                SaveTexture2DToFile(tex, filePath, fileFormat, jpgQuality);
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(tex);
                else
                    UnityEngine.Object.DestroyImmediate(tex);
            }

        }

        // Source: https://gist.github.com/corvus-mt/400ff945a5fb4f3bace20804cab8454a
        public static async Task<byte[]> CompressAsync(string source)
        {
            var bytes = Encoding.UTF8.GetBytes(source);

            await using var input = new MemoryStream(bytes);
            await using var output = new MemoryStream();
            await using var brotliStream = new BrotliStream(output, CompressionLevel.Fastest);

            await input.CopyToAsync(brotliStream);
            await brotliStream.FlushAsync();

            return output.ToArray();
        }

        public static async Task<string> DecompressAsync(byte[] compressed)
        {
            await using var input = new MemoryStream(compressed);
            await using var brotliStream = new BrotliStream(input, CompressionMode.Decompress);

            await using var output = new MemoryStream();

            await brotliStream.CopyToAsync(output);
            await brotliStream.FlushAsync();

            return Encoding.UTF8.GetString(output.ToArray());
        }

        public static byte[] CompressStr(string source)
        {
            var bytes = Encoding.UTF8.GetBytes(source);

            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            using var brotliStream = new BrotliStream(output, CompressionLevel.Fastest);

            input.CopyTo(brotliStream);
            brotliStream.Flush();

            return output.ToArray();
        }

        public static byte[] Compress(byte[] source)
        {
            using var input = new MemoryStream(source);
            using var output = new MemoryStream();
            using var brotliStream = new BrotliStream(output, CompressionLevel.Fastest);

            input.CopyTo(brotliStream);
            brotliStream.Flush();

            return output.ToArray();
        }

        public static string DecompressStr(byte[] compressed)
        {
            using var input = new MemoryStream(compressed);
            using var brotliStream = new BrotliStream(input, CompressionMode.Decompress);

            using var output = new MemoryStream();

            brotliStream.CopyTo(output);
            brotliStream.Flush();

            return Encoding.UTF8.GetString(output.ToArray());
        }

        public static byte[] Decompress(byte[] compressed)
        {
            using var input = new MemoryStream(compressed);
            using var brotliStream = new BrotliStream(input, CompressionMode.Decompress);

            using var output = new MemoryStream();

            brotliStream.CopyTo(output);
            brotliStream.Flush();

            return output.ToArray();
        }

        public static readonly CultureInfo _InvariantCulture = CultureInfo.InvariantCulture;

        private static string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public static string NumToMonth(int num)
        {
            if (num < 1 || num > 12) return $"ERR: {num} out of bounds";

            return months[num - 1];
        }

        // Reimplementation of stock function
        public static void FindChildrenStartsWith(GameObject obj, string str, List<GameObject> list)
        {
            if (obj.name.StartsWith(str))
                list.Add(obj);
            for (int i = 0; i < obj.transform.childCount; ++i)
                FindChildrenStartsWith(obj.transform.GetChild(i).gameObject, str, list);
        }

        // Reimplementation of stock function
        public static void FindChildrenContains(GameObject obj, string str, List<GameObject> list)
        {
            if (obj.name.Contains(str))
                list.Add(obj);
            for (int i = 0; i < obj.transform.childCount; ++i)
                FindChildrenContains(obj.transform.GetChild(i).gameObject, str, list);
        }

        public static double Lerp(double a, double b, double t, bool clamp = true)
        {
            if (clamp)
            {
                if (t <= 0)
                    return a;
                if (t >= 1)
                    return b;
            }

            return a + (b - a) * t;
        }

        public static float Lerp(float a, float b, float t, bool clamp = true)
        {
            if (clamp)
            {
                if (t <= 0)
                    return a;
                if (t >= 1)
                    return b;
            }

            return a + (b - a) * t;
        }

        public static double InverseLerp(double a, double b, double value, bool clamp = true)
        {
            if (clamp)
            {
                if (value <= a)
                    return 0d;
                if (value >= b)
                    return 1d;
            }

            return (value - a) / (b - a);
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static float distance(Vector3 a, Vector3 b)
        {
            return (float)Math.Sqrt(
                (a.x - b.x) * (a.x - b.x) +
                (a.y - b.y) * (a.y - b.y) +
                (a.z - b.z) * (a.z - b.z)
            );
        }

        public static int toInt(float x)
        {
            return (int)(x + 0.05f);
        }

        public static bool NearlyEqual(float a, float b)
        {
            return Math.Abs(a - b) < 0.01f;
        }

        public static bool NearlyEqual(Vector2 a, Vector2 b)
        {
            return NearlyEqual(a.x, b.x) && NearlyEqual(a.y, b.y);
        }

        public static bool NearlyEqual(Vector3 a, Vector3 b)
        {
            return NearlyEqual(a.x, b.x) && NearlyEqual(a.y, b.y) && NearlyEqual(a.z, b.z);
        }

        public static string Vec3ToCSV(Vector3 vector)
        {
            return $"\"({vector.x:0.0000}, {vector.y:0.0000}, {vector.z:0.0000})\"";
        }

        public static string ColliderObjToCSV(int count, string path, float rotation, Vector3 pos, GameObject decor)
        {
            if (decor == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"    Null Object!");

                return "";
            }

            if (decor.name.StartsWith("DeckBorder")) decor.name = "DeckBorder";
            if (decor.name.StartsWith("DeckWall")) decor.name = "DeckWall";

            string output = "\n" +
                $"{count},,\"{path}\",\"{decor.name}\",{rotation:0.00},{Vec3ToCSV(pos)},{Vec3ToCSV(decor.transform.localScale)}";

            return output;
        }

        public static string DecorObjToCSV(int count, string path, float rotation, Vector3 pos, GameObject decor)
        {
            if (decor == null)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"    Null Object!");

                return "";
            }

            Decor data = decor.GetComponent<Decor>();

            /*
                public unsafe Bounds bounds

                public unsafe bool check

                public unsafe List<GameObject> children

            
                public unsafe Vector3 minOverlapSizeBox

                public unsafe Vector3 maxOverlapSizeBox

                public unsafe string forceOverlap

                public unsafe string forceIgnore


                public unsafe HashSet<string> forceOverlapX

                public unsafe HashSet<string> forceIgnoreX


                public unsafe Part part

             */


            // Accidents:
            if (data == null)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"\n{ModUtils.DumpHierarchy(decor)}");

                return "";
            }

            var children = decor.GetChildren();

            // if (data.children != null)
            {

                // Melon<TweaksAndFixes>.Logger.Msg($"    Children: {children.Count} {(children.Count > 0 ? children[0].name : "NONE")}");
            }

            string output = "\n" +
                $"{count},,\"{path}\",{rotation:0.00},{Vec3ToCSV(pos)}," +
                $"{(data.check ? "1" : "0")},{(children.Count != 1 || children[0] == null ? "" : children[0].name)}," +
                $"{Vec3ToCSV(data.bounds.center)},{Vec3ToCSV(data.bounds.size)}," +
                $"{Vec3ToCSV(data.minOverlapSizeBox)},{Vec3ToCSV(data.maxOverlapSizeBox)}," +
                $"\"{data.forceOverlap}\",\"{data.forceIgnore}\"";

            return output;
        }

        public static string MountObjToCSV(int count, string path, float rotation, Vector3 pos, GameObject mount)
        {
            Mount data = mount.GetComponent<Mount>();

            string gunType = data.center ? "center" : (data.side ? "side" : "");

            string ParamOrNone(float param, float pair = 0)
            {
                return ((int)(param + 0.01) == 0) && ((int)(pair + 0.01) == 0) ? "" : param.ToString("0");
            }

            string validMounts = string.Empty;

            if (data.towerMain) validMounts += ",tower_main";
            if (data.towerSec) validMounts += ",tower_sec";
            if (data.funnel) validMounts += ",funnel";
            if (data.siBarbette) validMounts += ",si_barbette";
            if (data.barbette) validMounts += ",barbette";
            if (data.casemate) validMounts += ",casemate";
            if (data.subTorpedo) validMounts += ",sub_torpedo";
            if (data.deckTorpedo) validMounts += ",deck_torpedo";
            if (data.special) validMounts += ",special";
            validMounts = validMounts.TrimStart(',');

            if (validMounts.Length > 0) validMounts = $"\"{validMounts}\"";

            string collisionChecks = string.Empty;

            if (data.ignoreCollisionCheck) collisionChecks += ",ignore_collision_check";
            if (data.ignoreParent) collisionChecks += ",ignore_parent";
            if (data.ignoreExpand) collisionChecks += ",ignore_expand";
            if (data.ignoreHeight) collisionChecks += ",ignore_height";
            if (data.ignoreFireAngleCheck) collisionChecks += ",ignore_fire_angle_check";
            if (data.casemateIgnoreCollision) collisionChecks += ",casemate_ignore_collision";
            collisionChecks = collisionChecks.TrimStart(',');

            if (collisionChecks.Length > 0) collisionChecks = $"\"{collisionChecks}\"";

            string firingAngleOrientation = data.rotateLeftRight ? "starboard/port" : (data.rotateForwardBack ? "fore/aft" : "");

            string posString = $"({pos.x:0.0000}, {pos.y:0.0000}, {pos.z:0.0000})";

            string output = "\n" +
                $"{count},,\"{path}\",{rotation:0.00},\"{posString}\"," +
                $"{gunType},{validMounts},{ParamOrNone(data.caliberMin)},{ParamOrNone(data.caliberMax)},{ParamOrNone(data.barrelsMin)},{ParamOrNone(data.barrelsMax)}," +
                $"{collisionChecks},{ParamOrNone(data.angleLeft, data.angleRight)},{ParamOrNone(data.angleRight, data.angleLeft)},{firingAngleOrientation},{(data.rotateSame ? 1 : "")}" +
                ",,";

            return output;
        }

        public static string GeneratePartMountListCSV(GameObject model)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"Parent: {model.name}");

            StringBuilder finalCount = new StringBuilder(2 ^ 24);

            Stack<Tuple<GameObject, int>> stack = new Stack<Tuple<GameObject, int>>();

            Dictionary<string, int> depthToIndex = new Dictionary<string, int>();

            foreach (var child in model.GetChildren())
            {
                stack.Push(new Tuple<GameObject, int>(child, 0));
            }

            bool isHull = model.name.Contains("_hull_") || model.name.StartsWith("hull_") || model.name.EndsWith("_hull");
            bool isTower = model.name.Contains("_tower_") || model.name.StartsWith("tower_") || model.name.EndsWith("_tower");
            bool isBarbette = model.name.Contains("_barbette_") || model.name.StartsWith("barbette_") || model.name.EndsWith("_barbette");

            finalCount.Append($"\n# {model.name},,,,,,,,,,,,,,,,,");

            List<string> path = new();

            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                GameObject obj = pair.Item1;
                int depth = pair.Item2;

                if (obj == null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                    continue;
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"  Sub-parsing {obj.name} : {obj.Pointer}...");

                Il2CppSystem.Collections.Generic.List<GameObject> children;

                try
                {
                    children = obj.GetChildren();
                }
                catch
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Error while parsing {obj.name}...");
                    continue;
                }

                foreach (GameObject child in children)
                {
                    stack.Push(new Tuple<GameObject, int>(child, depth + 1));
                }

                // Update path:
                //   Remove path elements based on current depth
                //   Add current obj to path
                path.Clear();
                GameObject head = obj;
                int stop = 10;

                while (head != null && head.name != model.name)
                {
                    head = head.GetParent();
                    path.Insert(0, head.name.Replace("(Clone)", "") + "/");
                    if (stop-- <= 0) break;
                }

                if (path.Count > 0) path[^1] = path[^1].Replace("/", "");

                string concatPath = string.Concat(path);

                if (!obj.name.StartsWith("Mount"))
                {
                    if (isHull)
                    {
                        GameObject parent = obj.GetParent();

                        if (parent == null) continue;

                        if (parent.name == "Sections" || parent.name == "Variation")
                        {
                            finalCount.Append($"\n# {string.Concat(path)},,,,,,,,,,,,,,,,,");
                        }
                    }

                    continue;
                }

                if (!depthToIndex.ContainsKey(concatPath)) depthToIndex[concatPath] = 0;
                depthToIndex[concatPath]++;

                if (isBarbette)
                {
                    if (!obj.name.StartsWith("Mount:barbette"))
                    {
                        continue;
                    }
                }
                else if (isTower)
                {
                    if (obj.name.StartsWith("Mount:tower_main") || obj.name.StartsWith("Mount:si_barbette"))
                    {
                        continue;
                    }
                }
                else if (isHull)
                {
                    if (obj.name.StartsWith("Mount:tower_main") || obj.name.StartsWith("Mount:tower_sec") || obj.name.StartsWith("Mount:funnel") || obj.name.StartsWith("Mount:si_barbette"))
                    {
                        continue;
                    }
                }

                // else Melon<TweaksAndFixes>.Logger.Msg($"  Ignored: {string.Concat(path)} + #{count} : {obj.name}");

                finalCount.Append(MountObjToCSV(depthToIndex[concatPath], string.Concat(path), obj.transform.localEulerAngles.y, obj.transform.localPosition, obj));
            }

            return finalCount.ToString();
        }

        public static string GenerateMountCSV(Mount mount)
        {
            GameObject model = mount.gameObject;
            int mountStop = 10;
            StringBuilder mountPath = new();
            bool isFirst = true;

            // Melon<TweaksAndFixes>.Logger.Msg($"Selected Mount: {mount.name} | {mount.transform.position} | {mount.transform.eulerAngles.y}");

            while (model != null && !model.name.EndsWith("(Clone)") || model.name.StartsWith("Middle"))
            {
                model = model.GetParent();
                mountPath.Insert(0, model.name.Replace("(Clone)", "") + (isFirst ? "" : "/"));
                isFirst = false;
                if (mountStop-- <= 0) break;
            }

            // Melon<TweaksAndFixes>.Logger.Msg($"  Parent: {model.name} | {mountPath.ToString()}");

            Stack<Tuple<GameObject, int>> stack = new Stack<Tuple<GameObject, int>>();
            Dictionary<string, int> depthToIndex = new Dictionary<string, int>();

            foreach (var child in model.GetChildren())
            {
                stack.Push(new Tuple<GameObject, int>(child, 0));
            }

            bool isHull = model.name.Contains("_hull_") || model.name.StartsWith("hull_") || model.name.EndsWith("_hull");

            // finalCount.Append($"\n# {model.name},,,,,,,,,,,,,,,,,");

            List<string> path = new();

            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                GameObject obj = pair.Item1;
                int depth = pair.Item2;

                if (obj == null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Failed to load: {data.Value.nameUi}");
                    continue;
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"  Sub-parsing {obj.name} : {obj.Pointer}...");

                Il2CppSystem.Collections.Generic.List<GameObject> children;

                try
                {
                    children = obj.GetChildren();
                }
                catch
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Error while parsing {obj.name}...");
                    continue;
                }

                foreach (GameObject child in children)
                {
                    stack.Push(new Tuple<GameObject, int>(child, depth + 1));
                }

                // Update path:
                //   Remove path elements based on current depth
                //   Add current obj to path
                path.Clear();
                GameObject head = obj;
                int stop = 10;

                while (head != null && head.name != model.name)
                {
                    head = head.GetParent();
                    path.Insert(0, head.name.Replace("(Clone)", "") + "/");
                    if (stop-- <= 0) break;
                }

                if (path.Count > 0) path[^1] = path[^1].Replace("/", "");

                if (!obj.name.StartsWith("Mount"))
                {
                    if (isHull)
                    {
                        GameObject parent = obj.GetParent();

                        if (parent == null) continue;

                        if (parent.name == "Sections" || parent.name == "Variation")
                        {
                            // finalCount.Append($"\n# {string.Concat(path)},,,,,,,,,,,,,,,,,");
                        }
                    }

                    continue;
                }

                string concatPath = string.Concat(path);

                if (!depthToIndex.ContainsKey(concatPath)) depthToIndex[concatPath] = 0;
                depthToIndex[concatPath]++;

                if (obj != mount.gameObject) continue;

                if (string.Concat(path) == mountPath.ToString())
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: {string.Concat(path)} + #{count} : {obj.name}");
                    return MountObjToCSV(depthToIndex[concatPath], string.Concat(path), obj.transform.localEulerAngles.y, obj.transform.localPosition, obj);
                }

                // else Melon<TweaksAndFixes>.Logger.Msg($"  Ignored: {string.Concat(path)} + #{count} : {obj.name}");

                // finalCount.Append(MountObjToCSV(count, string.Concat(path), obj.transform.eulerAngles.y, obj.transform.position, obj));
            }

            return "Failed to find mount!";
        }

        public static string ColorNumber(float val, string prefix = "", string affix = "", bool invert = false, bool asInt = false)
        {
            Color color = (val >= 0 && !invert) || (val < 0 && invert) ? Color.green : Color.red;

            int r = (int)(color.r * 255);
            int g = (int)(color.g * 255);
            int b = (int)(color.b * 255);

            // Format the hexadecimal string
            string hex = $"#{r:X2}{g:X2}{b:X2}";

            string num = asInt ? ($"{val:N0}") : ($"{val}");

            if (val >= 0)
            {
                return $"<color={hex}>+{prefix}{num}{affix}</color>";
            }
            else
            {
                return $"<color={hex}>{prefix}{num}{affix}</color>";
            }
        }

        public static string DumpSelectedPartData(Part SelectedPart)
        {
            string print = "";

            print += $"\nSelected Part";
            print += $"\n  Type : {SelectedPart.Name()}";
            print += $"\n  Type : {SelectedPart.data.type}";

            var pos = SelectedPart.transform.position;
            string posString = $"({pos.x:0.0000}, {pos.y:0.0000}, {pos.z:0.0000})";

            print += $"\n  Pos  : [Global] {posString}";
            print += $"\n  Rot  : [Global] {SelectedPart.transform.eulerAngles.y}";

            if (SelectedPart.data.type == "gun" || SelectedPart.data.type == "torpedo")
            {
                print += $"\n  Weapon Data";
                if (SelectedPart.data.type == "gun") print += $"\n    Diameter   : {SelectedPart.data.GetCaliberInch()}";
                if (SelectedPart.data.type == "torpedo") print += $"\n    Diameter   : {14 + Patch_Ship.LastCreatedShip.TechTorpedoGrade(SelectedPart.data)}";

                Part.FireSectorInfo info = new Part.FireSectorInfo();
                SelectedPart.CalcFireSectorNonAlloc(info);

                if (info != null)
                {
                    print += $"\n    Fire Sector: {info.shootableAngleTotal}";
                }
                else
                {
                    print += $"\n    Fire Sector: Failed to calculate!";
                }
            }

            if (SelectedPart.mount != null)
            {
                Mount mount = SelectedPart.mount;
                print += $"\n  Mount";

                var pos2 = mount.transform.localPosition;
                string posString2 = $"({pos.x:0.0000}, {pos.y:0.0000}, {pos.z:0.0000})";

                print += $"\n    Pos   : [Local] {posString2}";
                print += $"\n    Rot   : [Local] {mount.transform.localEulerAngles.y}";
                print += $"\n    Cal   : {mount.caliberMin}/{mount.caliberMax}";
                print += $"\n    Barrel: {mount.barrelsMin}/{mount.barrelsMax}";
                print += $"\n    CSV   :{ModUtils.GenerateMountCSV(mount)}";

                // Melon<TweaksAndFixes>.Logger.Msg($"  Mount csv:\n{mountCsv}");
            }
            else
            {
                print += $"  Mount: None";
            }

            return print;
        }

        public static string DumpPartData(Part SelectedPart)
        {
            string print = "";

            print += $"\nSelected Part";
            print += $"\n  Name  : {SelectedPart.Name()}";
            print += $"\n  Type  : {SelectedPart.data.type}";

            var pos = SelectedPart.transform.position;
            string posString = $"({pos.x:0.0000}, {pos.y:0.0000}, {pos.z:0.0000})";

            print += $"\n  Pos   : [Global] {posString}";
            print += $"\n  Rot   : [Global] {SelectedPart.transform.eulerAngles.y}";

            print += $"\n  Parent: {(SelectedPart.mount != null ? SelectedPart.mount.parentPart.Name() : "None")}";

            if (SelectedPart.data.type == "gun" || SelectedPart.data.type == "torpedo")
            {
                print += $"\n  Weapon Data";
                if (SelectedPart.data.type == "gun") print += $"\n    Diameter   : {SelectedPart.data.GetCaliberInch()}";
                if (SelectedPart.data.type == "torpedo") print += $"\n    Diameter   : {14 + Patch_Ship.LastCreatedShip.TechTorpedoGrade(SelectedPart.data)}";

                Part.FireSectorInfo info = new Part.FireSectorInfo();
                SelectedPart.CalcFireSectorNonAlloc(info);

                if (info != null)
                {
                    print += $"\n    Fire Sector: {info.shootableAngleTotal}";
                }
                else
                {
                    print += $"\n    Fire Sector: Failed to calculate!";
                }
            }

            GameObject part = Util.ResourcesLoad<GameObject>(SelectedPart.gameObject.GetChildren()[0].name.Replace("(Clone)", ""));

            print += $"\n  Mounts";
            print += $"\n    CSV   :{ModUtils.GeneratePartMountListCSV(part)}";

            return print;
        }

        public static string DumpHullData(Ship ship)
        {
            string print = "";

            Part hull = ship.hull;

            string name = hull.gameObject.GetChildren()[0].name.Replace("(Clone)", "");

            print += $"Hull:";
            print += $"\n  Name    : {hull.Name()}";
            print += $"\n  Sections: {ship.SectionsForTonnage(ship.Tonnage())}";

            GameObject hullClone = Util.ResourcesLoad<GameObject>(name);

            if (hullClone == null) return print + "\n  Failed to parse hull!";

            string mountCsv = ModUtils.GeneratePartMountListCSV(hullClone);

            print += $"\n  Mount CSVs:\n{mountCsv}";

            return print;
        }

        public static string DumpPlayerData(Player player)
        {
            string dump = "";

            int lpad = 35;
            string sepearator = " : ";

            void AppendHeading(string name)
            {
                dump += "\n\n\n" + "".PadLeft(lpad - name.Length / 2 - 4, '#') + $" ]=[ {name} ]=[ " + "".PadRight(lpad - (name.Length + 1) / 2 - 4, '#') + "\n";
            }

            void AppendTitle(string name)
            {
                dump += "\n\n" + "".PadLeft(lpad - name.Length / 2, '[') + $" {name} " + "".PadRight(lpad - (name.Length + 1) / 2, ']') + "\n";
            }

            void AppendSubTitle(string name)
            {
                dump += "\n" + "".PadLeft(lpad - name.Length / 2 - 2, '-') + $" [ {name} ] " + "".PadRight(lpad - (name.Length + 1) / 2 - 2, '-') + "\n";
            }

            void AppendNumericEntry(string name, float value, bool usePercent = false, float percent = 0)
            {
                dump += name.PadLeft(lpad) + sepearator + value.ToString("N0") + " " + (usePercent ? ($"({percent.ToString("N3")}%)") : "") + "\n";
            }

            void AppendPercentEntry(string name, float value, bool usePercent = false, float percent = 0)
            {
                dump += name.PadLeft(lpad) + sepearator + value.ToString("N3") + "% " + (usePercent ? ($"({percent.ToString("N3")}%)") : "") + "\n";
            }

            void AppendStringEntry(string name, string value)
            {
                dump += name.PadLeft(lpad) + sepearator + value + "\n";
            }

            AppendHeading(player.Name(false));

            AppendTitle("OVERVIEW");

            AppendSubTitle("Stats");
            AppendPercentEntry("Transport Capacity", player.transportCapacity * 100);
            AppendNumericEntry("Shipyard Size", player.shipyard);
            AppendNumericEntry("Shipbuilding Capacity", player.GetTotalPortCapacity());
            AppendNumericEntry("Unrest", player.unrest);
            AppendNumericEntry("Prestige", player.reputation);

            AppendTitle("FINANCES");

            AppendSubTitle("GDP");
            AppendNumericEntry("Total GDP", player.StateBudget());
            AppendNumericEntry("Base Growth", player.StateBudget() * player.WealthGrowthEffective(), true, player.WealthGrowthEffective() * 100);
            AppendNumericEntry("Event Modifier", player.StateBudget() * Patch_Player.GetRequestedChangePlayerGDP(player), true, Patch_Player.GetRequestedChangePlayerGDP(player) * 100);
            AppendNumericEntry("Net Growth",
                player.StateBudget() * (Patch_Player.GetRequestedChangePlayerGDP(player) + player.WealthGrowthEffective()),
                true, (Patch_Player.GetRequestedChangePlayerGDP(player) + player.WealthGrowthEffective()) * 100);

            AppendSubTitle("Army");
            AppendNumericEntry("Budget", player.yearlyArmyBudget);

            AppendSubTitle("Navy");
            AppendNumericEntry("Funds", player.cash);
            AppendNumericEntry("Budget", player.Budget());
            AppendPercentEntry("Percent of GDP", player.NavalBudgetPercent());
            AppendNumericEntry("Expenses", player.Expenses());
            AppendNumericEntry("Net Budget", player.Budget());

            AppendSubTitle("Navy Expenses");
            AppendNumericEntry("Shipyard Expansions", player.ExpensesShipyardBudget());
            AppendNumericEntry("Training", player.ExpensesTrainingBudget(), true, player.trainingBudget * 100);
            AppendNumericEntry("Tech", player.ExpensesTechBudget(), true, player.techBudget + 50);
            AppendNumericEntry("Transport", player.ExpensesTransportCapacity(), true, player.transportCapacityBudget * 100);

            AppendTitle("DESIGNS");

            foreach (Ship design in new Il2CppSystem.Collections.Generic.List<Ship>(player.designs))
            {
                AppendSubTitle(design.Name(false, false, false, false, true));
                AppendStringEntry("Type", $"{design.shipType.nameFull} ({design.shipType.name.ToUpper()})");
                AppendStringEntry("Design Year", $"{design.dateCreated.AsDate().Month}. {design.dateCreated.AsDate().Year}");
                AppendNumericEntry("Cost", design.Cost());

                int afloat = 0;
                int building = 0;
                int built = 0;
                int repairing = 0;

                foreach (Ship ship in player.GetFleetAll())
                {
                    if (ship.design != design) continue;

                    built++;
                    if (ship.isAlive && !ship.isBuilding && !ship.isCommissioning && !ship.isRefit) afloat++;
                    if (ship.isRepairing) repairing++;
                    if (ship.isBuilding | ship.isCommissioning | ship.isRefit) building++;
                }

                AppendStringEntry("Total/Active", $"{built} / {afloat}");
                AppendStringEntry("Building/Repairing", $"{building} / {repairing}");
            }

            AppendTitle("PROVINCES");

            foreach (Province province in CampaignMap.Instance.Provinces.Provinces)
            {
                if (province.ControllerPlayer != player) continue;

                AppendSubTitle(province.Name);
                AppendNumericEntry("Income", province.Income());
                AppendNumericEntry("Income Multiplier", province.incomeMultiplier);
                AppendNumericEntry("Income Growth", province.IncomeGrowth);
                AppendNumericEntry("Population", province.GetPopulation());
                AppendNumericEntry("Population Growth", province.PopulationGrowth);
                AppendNumericEntry("Port Tonnage", province.Port);
                AppendNumericEntry("Oil", province.oilCapacity);
            }

            // Melon<TweaksAndFixes>.Logger.Msg($"           Transport Cap | {player.transportCapacity * 100}%");

            return dump;
        }

        public static string LocalizeF(string tag)
        {
            return LocalizeManager.Localize(tag);
        }

        public static string LocalizeF(string tag, params string[] arg0)
        {
            return String.Format(LocalizeManager.Localize(tag), arg0);
        }

        public static string StringOrSubstring(string s, int len)
        {
            if (s.Length < len || len == 0) return s;
            else return s.Substring(0, len) + "...";
        }

        private struct ObjectStack
        {
            public GameObject obj;
            public int depth;

            public ObjectStack(GameObject go, int d)
            {
                obj = go;
                depth = d;
            }
        }

        private static readonly List<ObjectStack> _ObjectHierarchyStack = new List<ObjectStack>();

        public static string DumpHierarchy(GameObject obj)
        {

            _ObjectHierarchyStack.Add(new ObjectStack(obj, 0));
            string hierarchy = "hierarchy:";
            while (_ObjectHierarchyStack.Count > 0)
            {
                int max = _ObjectHierarchyStack.Count - 1;
                var tuple = _ObjectHierarchyStack[max];
                var go = tuple.obj;
                int depth = tuple.depth;
                _ObjectHierarchyStack.RemoveAt(max);
                hierarchy += "\n";
                for (int i = 0; i < depth; ++i)
                {
                    hierarchy += "--";
                }
                hierarchy += " " + go.name;

                var rends = go.GetComponentsInChildren<Renderer>();
                Bounds goBounds = new Bounds();
                bool needBounds = true;
                for (int i = 0; i < rends.Length; ++i)
                {
                    if (rends[i] == null || !rends[i].enabled)
                        continue;

                    if (needBounds)
                    {
                        goBounds = rends[i].bounds;
                        needBounds = false;
                    }
                    else
                    {
                        goBounds.Encapsulate(rends[i].bounds);
                    }
                }

                if (!needBounds)
                    hierarchy += ": " + goBounds.min + "-" + goBounds.max;

                hierarchy += $" {go.transform.position}x{go.transform.localScale}";

                T AddComponentText<T> (string text) {
                    var comp = go.GetComponent<T>();

                    if (comp == null) { return default(T); }
                    
                    hierarchy += "\n";

                    for (int i = 0; i < depth; ++i)
                    {
                        hierarchy += "  ";
                    }

                    hierarchy += " |-> " + text;
                    return comp;
                };

                // AddComponentText<Transform>();
                AddComponentText<Button>("Button");
                AddComponentText<KeyButton>("KeyButton");
                AddComponentText<TMP_Text>("TMP_Text");
                Text txt = AddComponentText<Text>("Text");
                if (txt != null) hierarchy += ": " + txt.text.Replace("\n", "\\n");
                AddComponentText<Image>("Image");
                AddComponentText<Texture2D>("Texture2D");
                AddComponentText<LayoutGroup>("LayoutGroup");
                AddComponentText<LayoutElement>("LayoutElement");
                AddComponentText<Mount>("Mount");

                // int rCount = go.GetComponents<Renderer>().Length;
                // int mCount = go.GetComponents<MeshFilter>().Length;
                // if (rCount > 0 || mCount > 0)
                //     hierarchy += ". R.";
                //hierarchy += $". R={rCount}, M={mCount}";

                ++depth;
                for (int i = 0; i < go.transform.childCount; ++i)
                {
                    var subT = go.transform.GetChild(i);
                    if (subT == null)
                        continue;
                    var sub = subT.gameObject;
                    if (sub == null)
                        continue;
                    if (sub.activeSelf)
                        _ObjectHierarchyStack.Add(new ObjectStack(sub, depth));
                }
            }

            return hierarchy;
        }

        public static GameObject GetChildAtPath(string path, GameObject root = null)
        {
            // Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/ShipName
        
            if (root == null) root = G.ui.gameObject;

            if (root == G.ui.gameObject)
            {
                if (path.StartsWith("Global/Ui/UiMain/"))
                {
                    path = path.Replace("Global/Ui/UiMain/", "");
                }
                else
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Invalid path {path}. Default root is `Global/Ui/UiMain/`, specify a different root if this is not used.");
                }
            }

            string[] entries = path.Split('/');
            GameObject lastValid = root;
        
            foreach (string entry in entries)
            {
                lastValid = root;
                root = root.GetChild(entry, true);

                if (root == null)
                {
                    root = new GameObject();
                    Melon<TweaksAndFixes>.Logger.Error($"GetChildAtPath: Failed to find `{entry}` in path `{path}`, valid children at `{lastValid.name}`:");

                    foreach (var child in lastValid.GetChildren())
                    {
                        
                    }

                    break;
                }
            }

            return root;
        }

        // public static GameObject GetChildrenOfName(string name, GameObject root)
        // {
        //     foreach (var child in root.GetChildren())
        //     {
        //         
        //     }
        // }

        public static GameObject FindDeepChild(this GameObject obj, string name, bool allowInactive = true)
        {
            if (obj.name == name)
                return obj;

            for (int i = 0; i < obj.transform.childCount; ++i)
            {
                var go = obj.transform.GetChild(i).gameObject;
                if (!allowInactive && !go.active)
                    continue;

                var test = go.FindDeepChild(name);
                if (test != null)
                    return test;
            }

            return null;
        }


        public static void DestroyChild(GameObject child, bool tryDestroy = true)
        {
            if (child == null) return;

            if (tryDestroy)
            {
                child.transform.SetParent(null);
                child.TryDestroy();
            }
            else
            {
                child.SetActive(false);
            }
        }
        
        // Returns a smoothed distribution, i.e.
        // Random.Range(-range, range) + Random.Range(-range, range)...
        // divided by steps
        public static float DistributedRange(float range, int steps = 2, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
        {
            float val = 0f;
            for (int i = steps; i-- > 0;)
            {
                val += Range(-range, range, rnd, nativeRnd);
            }
            return val / steps;
        }

        public static float DistributedRange(float range, Il2CppSystem.Random rnd)
            => DistributedRange(range, 2, null, rnd);

        public static float DistributedRange(int steps, Il2CppSystem.Random rnd)
            => DistributedRange(1f, steps, null, rnd);

        // Biases a random number in the range [-1, 1]
        // so the midpoint is at the bias
        public static float BiasRange(float randomNum, float bias)
        {
            if (randomNum > 0f)
                randomNum *= 1f - bias;
            else
                randomNum *= 1f + bias;

            return randomNum + bias;
        }

        public static int RangeToInt(float input, int size)
        {
            input += 1f;
            input *= 0.5f;
            // Now in range 0-1
            int result = (int)(input * size);
            // Catch if it was -1 to 1 _inclusive_
            if (result >= size)
                result = size - 1;

            return result;
        }

        // Returns a smoothed distribution across an integer range, i.e.
        // Random.Range(-range, range) + Random.Range(-range, range)...
        // divided by steps. Note: done as float and remapped.
        public static int DistributedRange(int range, int steps = 2, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
            => RangeToInt(DistributedRange(1f, steps, rnd, nativeRnd), range * 2 + 1) - range;

        public static float DistributedRangeWithStepSize(float range, float stepSize, int steps, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
        {
            float numSteps = range / stepSize;
            int intSteps = (int)numSteps;
            if (numSteps - intSteps < 0.001f) // catch float imprecision
                ++intSteps;

            int val = DistributedRange(intSteps, steps, rnd, nativeRnd);
            return val * stepSize;
        }

        public static float Range(float a, float b, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
        {
            if (nativeRnd != null)
                return (float)nativeRnd.NextDouble() * (b - a) + a;

            if (rnd == null)
                return UnityEngine.Random.Range(a, b);

            return (float)rnd.NextDouble() * (b - a) + a;
        }

        public static int Range(int minInclusive, int maxInclusive, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
        {
            if (minInclusive == maxInclusive)
                return minInclusive;

            if (nativeRnd != null)
                return (int)(nativeRnd.NextDouble() * (maxInclusive - minInclusive + 1)) + minInclusive;

            if (rnd == null)
                return UnityEngine.Random.Range(minInclusive, maxInclusive + 1);

            return (int)(rnd.NextDouble() * (maxInclusive - minInclusive + 1)) + minInclusive;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;

            return value;
        }

        public static float ClampWithStep(float val, float stepSize, float minVal, float maxVal)
        {
            int stepCount = Mathf.RoundToInt(val / stepSize);
            float steppedVal = stepCount * stepSize;
            if (steppedVal < minVal)
                ++stepCount;
            else if (steppedVal > maxVal)
                --stepCount;
            return stepCount * stepSize;
        }

        public static T RandomOrNull<T>(this List<T> items, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null) where T : class
        {
            int iC = items.Count;
            if (iC == 0)
                return null;
            return items[Range(0, iC - 1, rnd, nativeRnd)];
        }

        public static T Random<T>(this Il2CppSystem.Collections.Generic.List<T> items, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
        {
            return items[Range(0, items.Count - 1, rnd, nativeRnd)];
        }

        public static T RandomOrNull<T>(this Il2CppSystem.Collections.Generic.List<T> items, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null) where T : class
        {
            int iC = items.Count;
            if (iC == 0)
                return null;
            return items[Range(0, iC - 1, rnd, nativeRnd)];
        }

        public static T Random<T>(this List<T> items, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null)
        {
            return items[Range(0, items.Count - 1, rnd, nativeRnd)];
        }

        public static T RandomByWeights<T>(Dictionary<T, float> dictionary, System.Random rnd = null, Il2CppSystem.Random nativeRnd = null) where T : notnull
        {
            if (dictionary.Count == 0)
                return default(T);
            float sum = 0f;
            foreach (var kvp in dictionary)
            {
                if (kvp.Value < 0f)
                    continue;

                sum += kvp.Value;
            }
            if (sum == 0f)
                return default(T);

            float selector = Range(0f, sum, rnd, nativeRnd);
            float curSum = 0f;
            foreach (var kvp in dictionary)
            {
                float val = kvp.Value;
                if (val < 0f)
                    val = 0f;
                curSum += val;

                if (selector > curSum)
                    continue;

                return kvp.Key;
            }

            // will never hit this, because selector can't be > sum.
            // But VS complains not all paths return a value without it, heh.
            return default(T);
        }

        private readonly static List<int> _ShuffleIndices = new List<int>();
        private readonly static List<int> _ShuffleRemainingOptions = new List<int>();
        public static void Shuffle<T>(this List<T> list)
        {
            int iC = list.Count;
            for (int i = 0; i < iC; ++i)
                _ShuffleRemainingOptions.Add(i);

            for (int i = 0; i < iC; ++i)
            {
                int idx = UnityEngine.Random.Range(0, _ShuffleRemainingOptions.Count - 1);
                _ShuffleIndices.Add(_ShuffleRemainingOptions[idx]);
                _ShuffleRemainingOptions.RemoveAt(idx);
            }

            // Slightly wasteful, but this ensures
            // we hit all elements.
            for (int i = 0; i < iC; ++i)
                ShuffleEx(list, i);

            _ShuffleIndices.Clear();
            _ShuffleRemainingOptions.Clear();
        }

        private static void ShuffleEx<T>(List<T> list, int idx)
        {
            if (_ShuffleIndices[idx] == -1)
                return;

            if (_ShuffleIndices[idx] == idx)
            {
                _ShuffleIndices[idx] = -1;
                return;
            }

            int desired = _ShuffleIndices[idx];
            _ShuffleIndices[idx] = -1;
            T elem = list[idx];
            ShuffleEx(list, desired);
            list[desired] = elem;
        }

        public static float RoundToStep(float val, float step)
            => Mathf.RoundToInt(val / step) * step;

        public static string ArmorString(Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> armor)
        {
            //return $"{armor[Ship.A.Belt]:F1}/{armor[Ship.A.BeltBow]:F1}/{armor[Ship.A.BeltStern]:F1}, {armor[Ship.A.Deck]:F1}/{armor[Ship.A.DeckBow]:F1}/{armor[Ship.A.DeckStern]:F1} "
            //    + $"{armor[Ship.A.ConningTower]:F1}/{armor[Ship.A.Superstructure]:F1}, {armor[Ship.A.TurretSide]:F1}/{armor[Ship.A.TurretTop]:F1}/{armor[Ship.A.Barbette]:F1}, "
            //    + $"{armor[Ship.A.InnerBelt_1st]:F1}/{armor[Ship.A.InnerBelt_2nd]:F1}/{armor[Ship.A.InnerBelt_3rd]:F1}, {armor[Ship.A.InnerDeck_1st]:F1}/{armor[Ship.A.InnerDeck_2nd]:F1}/{armor[Ship.A.InnerDeck_3rd]:F1}";
            string s = "Armor:";
            bool first = true;
            foreach (var kvp in armor)
            {
                if (first)
                    first = false;
                else
                    s += ",";
                s += $" {kvp.Key}={kvp.Value:F1}";
            }
            return s;
        }

        public static float ArmorValue(this Il2CppSystem.Collections.Generic.Dictionary<Ship.A, float> armor, Ship.A key)
        {
            armor.TryGetValue(key, out var val);
            return val;
        }

        public static float ArmorValue(this Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<Ship.A, float>> armor, Ship.A key)
        {
            foreach (var kvp in armor)
                if (kvp.Key == key)
                    return kvp.Value;

            return 0f;
        }

        public static void SetValue(this Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<Ship.A, float>> armor, Ship.A key, float value)
        {
            for (int i = armor.Count; i-- > 0;)
            {
                if (armor[i].Key == key)
                {
                    armor[i] = new Il2CppSystem.Collections.Generic.KeyValuePair<Ship.A, float>(key, value);
                    return;
                }
            }
        }

        public static List<T> ToManaged<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            var ret = new List<T>(list.Count);
            foreach (var item in list)
                ret.Add(item);

            return ret;
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToNative<T>(this List<T> list)
        {
            var ret = new Il2CppSystem.Collections.Generic.List<T>(list.Count);
            foreach (var item in list)
                ret.Add(item);

            return ret;
        }

        public static HashSet<T> ToManaged<T>(this Il2CppSystem.Collections.Generic.HashSet<T> set)
        {
            var ret = new HashSet<T>(set.Count);
            foreach (var item in set)
                ret.Add(item);

            return ret;
        }

        public static Il2CppSystem.Collections.Generic.HashSet<T> ToNative<T>(this HashSet<T> set)
        {
            var ret = new Il2CppSystem.Collections.Generic.HashSet<T>();
            ret.SetCapacity(set.Count);
            foreach (var item in set)
                ret.Add(item);

            return ret;
        }

        public static Dictionary<TKey, TValue> ToManaged<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> dict) where TKey : notnull
        {
            var ret = new Dictionary<TKey, TValue>(dict.Count);
            foreach (var kvp in dict)
                ret.Add(kvp.Key, kvp.Value);

            return ret;
        }

        public static Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> ToNative<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            var ret = new Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue>(dict.Count);
            foreach (var kvp in dict)
                ret.Add(kvp.Key, kvp.Value);

            return ret;
        }

        public static TValue ValueOrNew<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = System.Activator.CreateInstance<TValue>();
                dict[key] = value;
            }

            return value;
        }

        public static TValue ValueOrNew<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = System.Activator.CreateInstance<TValue>();
                dict[key] = value;
            }

            return value;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var value);
            return value;
        }

        public static int IncrementValueFor<TKey>(this Dictionary<TKey, int> dict, TKey key)
            => ChangeValueFor(dict, key, 1);

        public static int ChangeValueFor<TKey>(this Dictionary<TKey, int> dict, TKey key, int delta)
        {
            dict.TryGetValue(key, out int val);
            val += delta;
            dict[key] = val;
            return val;
        }

        public static int IncrementValueFor<TKey>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, int> dict, TKey key)
            => ChangeValueFor(dict, key, 1);

        public static int ChangeValueFor<TKey>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, int> dict, TKey key, int delta)
        {
            dict.TryGetValue(key, out int val);
            val += delta;
            dict[key] = val;
            return val;
        }

        public static float ChangeValueFor<TKey>(this Dictionary<TKey, float> dict, TKey key, float delta)
        {
            dict.TryGetValue(key, out float val);
            val += delta;
            dict[key] = val;
            return val;
        }

        public static float ChangeValueFor<TKey>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, float> dict, TKey key, float delta)
        {
            dict.TryGetValue(key, out float val);
            val += delta;
            dict[key] = val;
            return val;
        }

        public static bool DictsEqual<TKey, TValue>(Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var bVal))
                    return false;

                if (kvp.Value == null ? bVal != null : !kvp.Value.Equals(bVal))
                    return false;
            }

            return true;
        }

        public static bool DictsEqual<TKey, TValue>(Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> a, Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var bVal))
                    return false;

                if (kvp.Value == null ? bVal != null : !kvp.Value.Equals(bVal))
                    return false;
            }

            return true;
        }

        public static bool SetsEqual<T>(HashSet<T> a, HashSet<T> b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var item in a)
                if (!b.Contains(item))
                    return false;

            return true;
        }

        public static bool OrderedListsEqual<T>(List<T> a, List<T> b)
        {
            int ac = a.Count;
            if (ac != b.Count)
                return false;
            for (int i = ac; i-- > 0;)
                if (!a[i].Equals(b[i]))
                    return false;

            return true;
        }

        // Managed reimplementation of List.RemoveAll
        public static int RemoveAllManaged<T>(this Il2CppSystem.Collections.Generic.List<T> list, Predicate<T> match)
        {
            int freeIndex = 0;   // the first free slot in items array
            int size = list._size;

            // Find the first item which needs to be removed.
            while (freeIndex < size && !match(list._items[freeIndex])) freeIndex++;
            if (freeIndex >= size) return 0;

            int current = freeIndex + 1;
            while (current < size)
            {
                // Find the first item which needs to be kept.
                while (current < size && match(list._items[current])) current++;

                if (current < size)
                {
                    // copy item to the free slot.
                    list._items[freeIndex++] = list._items[current++];
                }
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(list._items, freeIndex, size - freeIndex); // Clear the elements so that the gc can reclaim the references.
            }

            int result = size - freeIndex;
            list._size = freeIndex;
            list._version++;
            return result;
        }

        public static void FillGradeData<T>(Il2CppSystem.Collections.Generic.Dictionary<int, T> dict, int max)
        {
            int maxGradeFound = 5;
            for (int grade = 6; grade <= max; ++grade)
            {
                if (dict.ContainsKey(grade))
                    maxGradeFound = grade;
                else
                    dict[grade] = dict[maxGradeFound];
            }
        }

        public static string GetHullModelKey(PartData data)
        {
            string key = data.model;
            if (data.shipType.name == "dd" || data.shipType.name == "tb")
                key += "%";
            if (data.paramx.TryGetValue("var", out var desiredVars))
            {
                key += "$";
                for (int i = 0; i < desiredVars.Count - 1; ++i)
                    key += desiredVars[i] + ";";
                key += desiredVars[desiredVars.Count - 1];
            }

            return key;
        }
    }

    [RegisterTypeInIl2Cpp]
    public class LogMB : MonoBehaviour
    {
        public LogMB(IntPtr ptr) : base(ptr) { }

        public void OnDestroy()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"$$$$ Destroying {gameObject.name}. Stack trace:\n{Environment.StackTrace}");
        }
    }
}
