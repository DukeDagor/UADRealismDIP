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

        public static readonly CultureInfo _InvariantCulture = CultureInfo.InvariantCulture;

        // Reimplementation of stock function
        public static void FindChildrenStartsWith(GameObject obj, string str, List<GameObject> list)
        {
            if (obj.name.StartsWith(str))
                list.Add(obj);
            for (int i = 0; i < obj.transform.childCount; ++i)
                FindChildrenStartsWith(obj.transform.GetChild(i).gameObject, str, list);
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

        public static bool NearlyEqual(float a, float b)
        {
            return (int)(Il2CppSystem.Math.Round(a * 1.0f) + 0.01) == (int)(Il2CppSystem.Math.Round(b * 1.0f) + 0.01);
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
