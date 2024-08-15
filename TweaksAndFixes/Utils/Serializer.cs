﻿using System.Reflection;
using UnityEngine;
using System.Text;
using Il2Cpp;
using System.Collections;
using MelonLoader;

#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8625

namespace TweaksAndFixes
{
    public static class Serializer
    {
        public class CSV
        {
            private static readonly char[] _Buf = new char[1024];

            private static unsafe List<List<string>> ParseLines(string input)
            {
                var allLines = new List<List<string>>();
                var curLine = new List<string>();
                int len = input.Length;
                bool inQuote = false;
                bool escaped = false;
                int bufIdx = 0;
                fixed (char* pInput = input)
                {
                    for (int i = 0; i < len; ++i)
                    {
                        char c = pInput[i];
                        if (c == '\n' || c == '\r')
                        {
                            if (escaped)
                            {
                                escaped = false;
                                _Buf[bufIdx++] = '\\';
                            }

                            if (!inQuote)
                            {
                                if (curLine.Count > 0 || bufIdx > 0)
                                {
                                    curLine.Add(bufIdx == 0 ? string.Empty : new string(_Buf, 0, bufIdx));
                                    allLines.Add(curLine);
                                    curLine = new List<string>();
                                }
                                bufIdx = 0;
                                continue;
                            }
                        }
                        if (escaped)
                        {
                            escaped = false;
                            // I'm sure there's a real way to do this
                            // but this will do.
                            c = c switch
                            {
                                'a' => '\a',
                                'b' => '\b',
                                'f' => '\f',
                                'n' => '\n',
                                'r' => '\r',
                                't' => '\t',
                                'v' => '\v',
                                _ => c
                            };
                        }
                        else
                        {
                            switch (c)
                            {
                                case '\\':
                                    escaped = true;
                                    continue;

                                case '"':
                                    inQuote = !inQuote;
                                    if (!inQuote) // i.e. we were quoted before
                                    {
                                        curLine.Add(new string(_Buf, 0, bufIdx));
                                        bufIdx = 0;
                                        ++i; // skip the comma. If this is the end
                                        // of the line this is still safe because the
                                        // loop will terminate at len+1 instead of len.
                                    }
                                    continue;

                                case ',':
                                    if (!inQuote)
                                    {
                                        curLine.Add(new string(_Buf, 0, bufIdx));
                                        bufIdx = 0;
                                        continue;
                                    }
                                    break;
                            }
                        }
                        _Buf[bufIdx++] = c;
                    }
                    // We have three cases.
                    // In the first case, we end with an unterminated entry
                    if (bufIdx > 0)
                    {
                        curLine.Add(new string(_Buf, 0, bufIdx));
                        allLines.Add(curLine);
                    }
                    // In the second case, we wend with a comma, which means
                    // effectively this is an empty entry
                    else if (pInput[len - 1] == ',')
                    {
                        curLine.Add(string.Empty);
                        allLines.Add(curLine);
                    }
                    // In the third case, we end with a terminated entry or an escape start.
                    // In this case, we don't have anything to add.
                }

                return allLines;
            }

            public static bool Write<TColl, TItem>(TColl coll, List<string> output, string keyName = null, bool markKey = true) where TColl : ICollection<TItem>
            {
                var tc = GetOrCreate(typeof(TItem));
                if (tc == null)
                    return false;

                FieldData key = null;
                if (keyName != null)
                    tc._nameToField.TryGetValue(keyName, out key);

                var header = tc.WriteHeader(key, markKey);
                output.Add(header);

                bool allSucceeded = true;
                foreach (var item in coll)
                {
                    bool ok = tc.WriteType(item, out var s, key);
                    if (ok)
                        output.Add(s);

                    allSucceeded &= ok;
                }

                return allSucceeded;
            }

            public static bool Write<TKey, TItem>(IDictionary<TKey, TItem> coll, List<string> output, bool markKey = true, string keyName = null)
            {
                var tc = GetOrCreate(typeof(TItem));
                if (tc == null)
                    return false;

                FieldData key = null;
                if (keyName == null || !tc._nameToField.TryGetValue(keyName, out key))
                    key = tc.GetKey(coll as System.Collections.IDictionary);

                var header = tc.WriteHeader(key, markKey);
                output.Add(header);

                bool allSucceeded = true;
                foreach (var item in coll)
                {
                    bool ok = tc.WriteType(item, out var s, key);
                    if (ok)
                        output.Add(s);

                    allSucceeded &= ok;
                }

                return allSucceeded;
            }

            public static bool Read<TList, TItem>(string text, TList output, bool useComments = true, bool useDefault = false) where TList : IList<TItem>
            {
                var lines = ParseLines(text);
                int lLen = lines.Count;
                if (lLen < 2)
                {
                    Melon<TweaksAndFixes>.Logger.Error("[TweaksAndFixes] CSV: Tried to read csv for list but line count was less than 2");
                    return false;
                }

                bool create = output.Count == 0;

                var tc = GetOrCreate(typeof(TItem));
                if (tc == null)
                    return false;

                int firstLineIdx = 0;
                while (lines[firstLineIdx][0].StartsWith('#'))
                    ++firstLineIdx;

                List<string> header = lines[firstLineIdx++];
                if (useComments)
                {
                    for (int i = 0; i < header.Count; ++i)
                    {
                        if (header[i].StartsWith('@'))
                        {
                            header[i] = header[i].Substring(1);
                            break;
                        }
                    }
                }
                bool allSucceeded = true;
                List<string> defaults = null;
                for (int i = firstLineIdx; i < lLen; ++i)
                {
                    if (useComments && lines[i][0].StartsWith('#'))
                        continue;

                    var line = lines[i];
                    if (defaults == null && useDefault && line.Count > 0 && line[0] == "default")
                    {
                        defaults = line;
                        continue;
                    }
                    if (create)
                    {
                        var item = (TItem)GetNewInstance(typeof(TItem));
                        allSucceeded &= tc.ReadType(item, line, header, defaults, useComments);
                        output.Add(item);
                    }
                    else
                    {
                        var item = output[i - 1];
                        allSucceeded &= tc.ReadType(item, line, header, defaults, useComments);
                        output[i - 1] = item; // if valuetype, we need to do this
                    }
                }

                return allSucceeded;
            }

            public static bool Read<TDict, TKey, TValue>(string text, TDict output, string keyName = null, bool useComments = true, bool useDefault = false) where TDict : IDictionary<TKey, TValue>
            {
                var lines = ParseLines(text);
                int lLen = lines.Count;
                if (lLen < 2)
                {
                    Melon<TweaksAndFixes>.Logger.Error("[TweaksAndFixes] CSV: Tried to read csv for dictionary but line count was less than 2");
                    return false;
                }

                var tc = GetOrCreate(typeof(TValue));
                if (tc == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error("[TweaksAndFixes] CSV: Could not fetch type info for " + (typeof(TValue).Name));
                    return false;
                }

                bool create = output.Count == 0;

                int firstLineIdx = 0;
                while (lines[firstLineIdx][0].StartsWith('#'))
                    ++firstLineIdx;

                List<string> header = lines[firstLineIdx++];
                int keyIdx;
                if (keyName != null)
                {
                    if (useComments)
                    {
                        for (int i = 0; i < header.Count; ++i)
                        {
                            if (header[i].StartsWith('@'))
                            {
                                header[i] = header[i].Substring(1);
                                break;
                            }
                        }
                    }
                    keyIdx = header.IndexOf(keyName);
                }
                else
                {
                    keyIdx = -1;
                    for (int i = 0; i < header.Count; ++i)
                    {
                        if (header[i].StartsWith('@'))
                        {
                            keyIdx = i;
                            header[i] = header[i].Substring(1);
                            keyName = header[i];
                            break;
                        }
                    }
                }
                if (keyIdx < 0)
                {
                    Melon<TweaksAndFixes>.Logger.Error("[TweaksAndFixes] CSV: Could not find key in header");
                    return false;
                }

                if (!tc._nameToField.TryGetValue(keyName, out var keyField))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"[TweaksAndFixes] CSV: Could not find field named {keyName} on {typeof(TValue).Name}");
                    return false;
                }

                bool allSucceeded = true;
                List<string> defaults = null;
                for (int i = firstLineIdx; i < lLen; ++i)
                {
                    if (useComments && lines[i][0].StartsWith('#'))
                        continue;

                    var line = lines[i];
                    if (defaults == null && useDefault && line.Count > 0 && line[0] == "default")
                    {
                        defaults = line;
                        continue;
                    }

                    var keyObj = keyField.ReadValue(line[keyIdx]);
                    if (keyObj == null)
                    {
                        // we can't insert with null key
                        allSucceeded = false;
                        continue;
                    }
                    var key = (TKey)keyObj;

                    if (!create && output.TryGetValue(key, out var existing))
                    {
                        allSucceeded &= tc.ReadType(existing, line, header, defaults, useComments);
                        output[key] = existing; // if valuetype, we need to do this
                        continue;
                    }

                    // in the not-create case, we know the key isn't found
                    // so no need to test.
                    if (create && output.ContainsKey(key))
                    {
                        Melon<TweaksAndFixes>.Logger.Error("[TweaksAndFixes] CSV: Tried to add object to dictionary with duplicate key " + key.ToString());
                        continue;
                    }

                    var item = (TValue)GetNewInstance(typeof(TValue));
                    allSucceeded &= tc.ReadType(item, line, header, defaults, useComments);
                    output.Add(key, item);
                }

                return allSucceeded;
            }

            // It would be better to do these line by line. But
            // (a) this is faster, and (b) the alloc isn't too
            // bad given actual use cases.
            public static bool Write<TColl, TItem>(TColl coll, string path, string keyName = null, bool markKey = true) where TColl : ICollection<TItem>
            {
                var lines = new List<string>();
                bool ok = Write<TColl, TItem>(coll, lines, keyName, markKey);
                File.WriteAllLines(path, lines);
                return ok;
            }

            public static bool Write<TKey, TItem>(IDictionary<TKey, TItem> dict, string path, bool markKey = true, string keyName = null)
            {
                var lines = new List<string>();
                bool ok = Write<TKey, TItem>(dict, lines, markKey, keyName);
                File.WriteAllLines(path, lines);
                return ok;
            }


            public static bool Read<TList, TItem>(TList output, string path, bool useComments = true) where TList : IList<TItem>
            {
                var text = File.ReadAllText(path);
                return Read<TList, TItem>(text, output, useComments);
            }

            public static bool Read<TDict, TKey, TValue>(TDict output, string path, string keyName = null, bool useComments = true) where TDict : IDictionary<TKey, TValue>
            {
                var text = File.ReadAllText(path);
                return Read<TDict, TKey, TValue>(text, output, keyName, useComments);
            }

            public static string? GetTextFromFileOrAsset(string assetName)
            {
                string text = GetTextFromFile(assetName);
                if (text == null)
                {
                    var textA = Util.ResourcesLoad<TextAsset>(assetName, false);
                    if (textA != null)
                        text = textA.text;
                }
                if (text == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Could not find or load asset `{assetName}`");
                    return null;
                }

                return text;
            }

            public static string? GetTextFromFile(string assetName)
            {
                if (!Directory.Exists(Config._BasePath))
                    return null;

                string filePath = Path.Combine(Config._BasePath, assetName + ".csv");
                if (!File.Exists(filePath))
                    return null;

                return File.ReadAllText(filePath);
            }

            private static readonly string _TempTextAssetName = "tafTempTA";

            private static TextAsset __TempTextAsset = null;
            private static TextAsset _TempTextAsset
            {
                get
                {
                    if (__TempTextAsset == null)
                    {
                        __TempTextAsset = new TextAsset(Il2CppInterop.Runtime.IL2CPP.il2cpp_object_new(Il2CppInterop.Runtime.Il2CppClassPointerStore<TextAsset>.NativeClassPtr));
                        Util.resCache[_TempTextAssetName] = __TempTextAsset;
                    }
                    return __TempTextAsset;
                }
            }

            private static GameData.LoadInfo __TempLoadInfo = null;
            private static GameData.LoadInfo _TempLoadInfo
            {
                get
                {
                    if (__TempLoadInfo == null)
                    {
                        __TempLoadInfo = new GameData.LoadInfo();
                        __TempLoadInfo.forceLocal = true;
                        __TempLoadInfo.name = _TempTextAssetName;
                    }
                    return __TempLoadInfo;
                }
            }

            private static List<FieldInfo> GetCopyFields(Type type, string text)
            {
                var allLines = ParseLines(text);
                List<string> header = null;
                for (int i = 0; i < allLines.Count; ++i)
                {
                    if (allLines[i].Count > 0 && allLines[i][0].StartsWith('@'))
                    {
                        header = allLines[i];
                        break;
                    }
                }
                if (header == null)
                    header = new List<string>();

                HashSet<string> okNames = new HashSet<string>();
                foreach (var s in header)
                {
                    var s2 = s.Trim();
                    if (s2.Length == 0)
                        continue;
                    if (s2.StartsWith('@'))
                        okNames.Add(s2.Substring(1));
                    else if (!s2.StartsWith('#'))
                        okNames.Add(s2);
                }

                var ret = new List<FieldInfo>();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var fi in fields)
                {
                    if (okNames.Contains(fi.Name))
                        ret.Add(fi);
                }

                return ret;
            }

            // If this takes an existing collection, it must be updated BEFORE PostProcess runs.
            public static Il2CppSystem.Collections.Generic.Dictionary<string, T> ProcessCSV<T>(string text, bool fillCustom, Il2CppSystem.Collections.Generic.Dictionary<string, T> existing = null) where T : BaseData
            {
                TextAsset.Internal_CreateInstance(_TempTextAsset, text);
                var newDict = G.GameData.ProcessCsv<T>(_TempLoadInfo, fillCustom);
                if (existing == null)
                    return newDict;

                // Lazy-fill this, because if we don't need to update
                // existing records, no point in doing this.
                List<FieldInfo> fieldsToCopy = null;

                int lastID = 0;
                float lastOrder = 0f;
                foreach (var item in existing.Values)
                {
                    if (item.Id > lastID)
                        lastID = item.Id;
                    if (item.order > lastOrder)
                        lastOrder = item.order;
                }

                foreach (var kvp in newDict)
                {
                    if (existing.TryGetValue(kvp.Key, out var oldData))
                    {
                        if (fieldsToCopy == null)
                            fieldsToCopy = GetCopyFields(typeof(T), text);
                        foreach (var fi in fieldsToCopy)
                            fi.SetValue(oldData, fi.GetValue(kvp.Value));

                        // Reprocess this since input has changed
                        oldData.PostProcess();
                    }
                    else
                    {
                        kvp.Value.order = ++lastOrder;
                        kvp.value.Id = ++lastID;
                        existing[kvp.Key] = kvp.Value;
                    }
                }
                return existing;
            }

            // If this takes an existing collection, it must be updated BEFORE PostProcess runs.
            public static Il2CppSystem.Collections.Generic.Dictionary<string, T> ProcessCSV<T>(bool fillCustom, string path, Il2CppSystem.Collections.Generic.Dictionary<string, T> existing = null) where T : BaseData
                => ProcessCSV<T>(File.ReadAllText(path), fillCustom, existing);

            static readonly Dictionary<string, int> _IndexCache = new Dictionary<string, int>();
            // If this takes an existing collection, it must be updated BEFORE PostProcess runs.
            public static Il2CppSystem.Collections.Generic.List<T> ProcessCSVToList<T>(string text, bool fillCustom, Il2CppSystem.Collections.Generic.List<T> existing = null) where T : BaseData
            {
                TextAsset.Internal_CreateInstance(_TempTextAsset, text);
                var list = new Il2CppSystem.Collections.Generic.List<T>();
                G.GameData.ProcessCsv<T>(_TempLoadInfo, list, null, fillCustom);
                if (existing == null)
                    return list;

                // Lazy-fill this, because if we don't need to update
                // existing records, no point in doing this.
                List<FieldInfo> fieldsToCopy = null;

                // Find last ID and cache indices
                int lastID = 0;
                float lastOrder = 0f;
                for (int i = existing.Count; i-- > 0;)
                {
                    var item = existing[i];
                    _IndexCache[item.name] = i;
                    if (item.Id > lastID)
                        lastID = item.Id;
                    if (item.order > lastOrder)
                        lastOrder = item.order;
                }

                foreach (var item in list)
                {
                    if (_IndexCache.TryGetValue(item.name, out var i))
                    {
                        var oldData = existing[i];
                        if (fieldsToCopy == null)
                            fieldsToCopy = GetCopyFields(typeof(T), text.Substring(0, text.IndexOf('\n')));
                        foreach (var fi in fieldsToCopy)
                            fi.SetValue(oldData, fi.GetValue(item));

                        // Reprocess this since input has changed
                        oldData.PostProcess();
                    }
                    else
                    {
                        item.order = ++lastOrder;
                        item.Id = ++lastID;
                        existing.Add(item);
                    }
                }
                _IndexCache.Clear();
                return existing;
            }

            // If this takes an existing collection, it must be updated BEFORE PostProcess runs.
            public static Il2CppSystem.Collections.Generic.List<T> ProcessCSVToList<T>(bool fillCustom, string path, Il2CppSystem.Collections.Generic.List<T> existing = null) where T : BaseData
                => ProcessCSVToList<T>(File.ReadAllText(path), fillCustom);

            // This is an expanded version of System.TypeCode
            public enum DataType : uint
            {
                INVALID = 0,
                ValueString,
                ValueGuid,
                ValueBool,
                ValueByte,
                ValueSByte,
                ValueChar,
                ValueDecimal,
                ValueDouble,
                ValueFloat,
                ValueInt,
                ValueUInt,
                ValueLong,
                ValueULong,
                ValueShort,
                ValueUShort,
                ValueVector2,
                ValueVector3,
                ValueVector4,
                ValueQuaternion,
                ValueMatrix4x4,
                ValueColor,
                ValueColor32,
                ValueEnum,
            }

            public class FieldData
            {
                private static readonly System.Globalization.CultureInfo _Invariant = System.Globalization.CultureInfo.InvariantCulture;

                public string _fieldName = null;
                public Type _fieldType = null;
                public FieldInfo _fieldInfo = null;
                public Field _attrib = null;
                public DataType _dataType = DataType.INVALID;

                public FieldData(MemberInfo memberInfo, Field attrib)
                {
                    this._attrib = attrib;

                    string pName = attrib.name;
                    _fieldName = pName != null && pName.Length > 0 ? pName : memberInfo.Name;

                    _fieldInfo = memberInfo.DeclaringType.GetField(memberInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _fieldType = _fieldInfo.FieldType;

                    SetDataType(true);
                }

                public bool Read(string value, object host)
                {
                    if (_dataType != DataType.ValueString && value.Length == 0)
                        return true;

                    object val = ReadValue(value, _dataType, _fieldType);

                    if (val == null)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"[TweaksAndFixes] CSV: Failed to parse {value} to type {_dataType} on field type {_fieldType}");
                        return false;
                    }

                    _fieldInfo.SetValue(host, val);
                    return true;
                }

                public object ReadValue(string value)
                    => ReadValue(value, _dataType, _fieldType);

                public static object ReadValue(string value, DataType dataType, Type fieldType)
                {
                    switch (dataType)
                    {
                        case DataType.ValueString:
                            return value;
                        case DataType.ValueGuid:
                            return new Guid(value);
                        case DataType.ValueBool:
                            if (bool.TryParse(value, out var b))
                                return b;
                            return null;
                        case DataType.ValueDouble:
                            if (double.TryParse(value, out var d))
                                return d;
                            return null;
                        case DataType.ValueFloat:
                            if (float.TryParse(value, out var f))
                                return f;
                            return null;
                        case DataType.ValueDecimal:
                            if (decimal.TryParse(value, out var dc))
                                return dc;
                            return null;
                        case DataType.ValueInt:
                            if (int.TryParse(value, out var i))
                                return i;
                            return null;
                        case DataType.ValueUInt:
                            if (uint.TryParse(value, out var ui))
                                return ui;
                            return null;
                        case DataType.ValueChar:
                            return value.Length > 0 ? value[0] : '\0';
                        case DataType.ValueShort:
                            if (short.TryParse(value, out var s))
                                return s;
                            return null;
                        case DataType.ValueUShort:
                            if (ushort.TryParse(value, out var us))
                                return us;
                            return null;
                        case DataType.ValueLong:
                            if (long.TryParse(value, out var l))
                                return l;
                            return null;
                        case DataType.ValueULong:
                            if (ulong.TryParse(value, out var ul))
                                return ul;
                            return null;
                        case DataType.ValueByte:
                            if (byte.TryParse(value, out var by))
                                return by;
                            return null;
                        case DataType.ValueSByte:
                            if (sbyte.TryParse(value, out var sb))
                                return sb;
                            return null;
                        case DataType.ValueEnum:
                            try
                            {
                                return Enum.Parse(fieldType, value);
                            }
                            catch
                            {
                                string[] enumNames = fieldType.GetEnumNames();
                                string defaultName = enumNames.Length > 0 ? enumNames[0] : string.Empty;
                                Debug.LogWarning($"[TweaksAndFixes] CSV: Couldn't parse value '{value}' for enum '{fieldType.Name}', default value '{defaultName}' will be used.\nValid values are {string.Join(", ", enumNames)}");
                                return null;
                            }
                        case DataType.ValueVector2:
                            return ParseVector2(value);
                        case DataType.ValueVector3:
                            return ParseVector3(value);
                        case DataType.ValueVector4:
                            return ParseVector4(value);
                        case DataType.ValueQuaternion:
                            return ParseQuaternion(value);
                        case DataType.ValueMatrix4x4:
                            return ParseMatrix4x4(value);
                        case DataType.ValueColor:
                            return ParseColor(value);
                        case DataType.ValueColor32:
                            return ParseColor32(value);
                    }
                    return null;
                }

                public unsafe bool Write(object value, out string output)
                {
                    output = WriteValue(value, _dataType);
                    if (output == null)
                        return false;

                    if (_dataType != DataType.ValueString)
                    {
                        if (_dataType >= DataType.ValueVector2 && _dataType <= DataType.ValueColor32)
                            output = "\"" + output + "\"";

                        return true;
                    }

                    int count = 0;
                    int len = output.Length;
                    bool hasComma = false;
                    while (true)
                    {
                        fixed (char* pszO = output)
                        {
                            for (int i = len; i-- > 0;)
                            {
                                char c = pszO[i];
                                switch (c)
                                {

                                    case '\a':
                                    case '\b':
                                    case '\f':
                                    case '\n':
                                    case '\r':
                                    case '\t':
                                    case '\v':
                                    case '"':
                                        ++count;
                                        break;

                                    case ',':
                                        hasComma = true;
                                        break;
                                }
                            }
                        }
                        if (count == 0)
                            break;

                        string oldStr = output;
                        output = new string('x', count + len + 2);
                        fixed (char* pszNew = output)
                        {
                            fixed (char* pszOld = oldStr)
                            {
                                int j = 0;
                                pszNew[j++] = '"';
                                for (int i = 0; i < len; ++i)
                                {
                                    char c = pszOld[i];
                                    char c2 = c switch
                                    {
                                        '\a' => 'a',
                                        '\b' => 'b',
                                        '\f' => 'f',
                                        '\n' => 'n',
                                        '\r' => 'r',
                                        '\t' => 't',
                                        '\v' => 'v',
                                        _ => c
                                    };
                                    if (c2 != c || c == '"')
                                        pszNew[j++] = '\\';
                                    pszNew[j++] = c2;
                                }
                                pszNew[j] = '"';
                            }
                        }
                        break;
                    }
                    if (hasComma)
                        output = '"' + output + '"';

                    return true;
                }

                public static string WriteValue(object value, DataType dataType)
                {
                    switch (dataType)
                    {
                        case DataType.ValueString:
                            return (string)value;
                        case DataType.ValueGuid:
                            return ((Guid)value).ToString();
                        case DataType.ValueBool:
                            return ((bool)value).ToString(_Invariant);
                        case DataType.ValueDouble:
                            return ((double)value).ToString("G17");
                        case DataType.ValueFloat:
                            return ((float)value).ToString("G9");
                        case DataType.ValueDecimal:
                            return ((decimal)value).ToString(_Invariant);
                        case DataType.ValueInt:
                            return ((int)value).ToString(_Invariant);
                        case DataType.ValueUInt:
                            return ((uint)value).ToString(_Invariant);
                        case DataType.ValueChar:
                            return ((char)value).ToString(_Invariant);
                        case DataType.ValueShort:
                            return ((short)value).ToString(_Invariant);
                        case DataType.ValueUShort:
                            return ((ushort)value).ToString(_Invariant);
                        case DataType.ValueLong:
                            return ((long)value).ToString(_Invariant);
                        case DataType.ValueULong:
                            return ((ulong)value).ToString(_Invariant);
                        case DataType.ValueByte:
                            return ((byte)value).ToString(_Invariant);
                        case DataType.ValueSByte:
                            return ((sbyte)value).ToString(_Invariant);
                        case DataType.ValueEnum:
                            return ((System.Enum)value).ToString();
                        case DataType.ValueVector2:
                            return WriteVector((Vector2)value);
                        case DataType.ValueVector3:
                            return WriteVector((Vector3)value);
                        case DataType.ValueVector4:
                            return WriteVector((Vector4)value);
                        case DataType.ValueQuaternion:
                            return WriteQuaternion((Quaternion)value);
                        case DataType.ValueMatrix4x4:
                            return WriteMatrix4x4((Matrix4x4)value);
                        case DataType.ValueColor:
                            return WriteColor((Color)value);
                        case DataType.ValueColor32:
                            return WriteColor((Color32)value);
                    }
                    return null;
                }

                public static DataType ValueDataType(Type fieldType)
                {
                    if (!fieldType.IsValueType)
                    {
                        if (fieldType == typeof(string))
                            return DataType.ValueString;

                        return DataType.INVALID;
                    }
                    if (fieldType == typeof(Guid))
                        return DataType.ValueGuid;
                    if (fieldType == typeof(bool))
                        return DataType.ValueBool;
                    if (fieldType == typeof(byte))
                        return DataType.ValueByte;
                    if (fieldType == typeof(sbyte))
                        return DataType.ValueSByte;
                    if (fieldType == typeof(char))
                        return DataType.ValueChar;
                    if (fieldType == typeof(decimal))
                        return DataType.ValueDecimal;
                    if (fieldType == typeof(double))
                        return DataType.ValueDouble;
                    if (fieldType == typeof(float))
                        return DataType.ValueFloat;
                    if (fieldType == typeof(int))
                        return DataType.ValueInt;
                    if (fieldType == typeof(uint))
                        return DataType.ValueUInt;
                    if (fieldType == typeof(long))
                        return DataType.ValueLong;
                    if (fieldType == typeof(ulong))
                        return DataType.ValueULong;
                    if (fieldType == typeof(short))
                        return DataType.ValueShort;
                    if (fieldType == typeof(ushort))
                        return DataType.ValueUShort;
                    if (fieldType == typeof(Vector2))
                        return DataType.ValueVector2;
                    if (fieldType == typeof(Vector3))
                        return DataType.ValueVector3;
                    if (fieldType == typeof(Vector4))
                        return DataType.ValueVector4;
                    if (fieldType == typeof(Quaternion))
                        return DataType.ValueQuaternion;
                    if (fieldType == typeof(Matrix4x4))
                        return DataType.ValueMatrix4x4;
                    if (fieldType == typeof(Color))
                        return DataType.ValueColor;
                    if (fieldType == typeof(Color32))
                        return DataType.ValueColor32;
                    if (fieldType.IsEnum)
                        return DataType.ValueEnum;

                    return DataType.INVALID;
                }

                private void SetDataType(bool allowArrays)
                {
                    _dataType = ValueDataType(_fieldType);
                }
            }


            private static readonly StringBuilder _StringBuilder = new StringBuilder();
            private static readonly Dictionary<Type, CSV> cache = new Dictionary<Type, CSV>();
            //private static readonly Dictionary<Type, ConstructorInfo> _typeToConstrutor = new Dictionary<Type, ConstructorInfo>();

            private List<FieldData> _fields = new List<FieldData>();
            private Dictionary<string, FieldData> _nameToField = new Dictionary<string, FieldData>();
            bool _isPostProcess;

            public CSV(Type t)
            {
                MemberInfo[] members = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                for (int i = 0, iC = members.Length; i < iC; ++i)
                {
                    var attrib = (Field)members[i].GetCustomAttribute(typeof(Field), inherit: true);
                    if (attrib == null)
                        continue;

                    var data = new FieldData(members[i], attrib);
                    if (data._dataType != DataType.INVALID)
                    {
                        _fields.Add(data);
                        _nameToField[data._fieldName] = data;
                    }
                }

                _isPostProcess = typeof(IPostProcess).IsAssignableFrom(t);
            }

            public bool ReadType(object host, List<string> line, List<string> header, List<string> defaults, bool useComments)
            {
                bool allSucceeded = true;
                int count = line.Count;
                int hCountTrue = header.Count;
                if (useComments)
                {
                    for (int i = hCountTrue; i-- > 0;)
                    {
                        if (header[i].StartsWith('#'))
                            --hCountTrue;
                        else
                            break;
                    }
                }
                if (count < hCountTrue)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"[TweaksAndFixes] CSV: Count mismatch between header line: {header.Count} and line: {count}\n{string.Join(",", header)}\n{string.Join(",", line)}");
                    return false;
                }
                if (defaults != null && defaults.Count < hCountTrue)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"[TweaksAndFixes] CSV: Count mismatch between header line: {header.Count} and defaults line: {defaults.Count}\n{string.Join(",", header)}\n{string.Join(",", defaults)}");
                    return false;
                }
                for (int i = 0; i < hCountTrue; ++i)
                {
                    if (!_nameToField.TryGetValue(header[i], out FieldData fieldItem))
                        continue;

                    string value = line[i];
                    if (value == string.Empty && defaults != null)
                        value = defaults[i];

                    allSucceeded &= fieldItem.Read(value, host);
                }

                if (_isPostProcess && host is IPostProcess ipp)
                    ipp.PostProcess();

                return allSucceeded;
            }

            public bool WriteType(object obj, out string output, FieldData key = null)
            {
                int num = _fields.Count;
                bool allSucceeded = true;
                bool isNotFirst = false;

                if (key != null)
                {
                    isNotFirst = true;
                    object value = key._fieldInfo.GetValue(obj);
                    if (value != null)
                    {
                        allSucceeded &= key.Write(value, out string val);
                        if (val != null)
                            _StringBuilder.Append(val);
                    }
                }

                foreach (var fieldData in _fields)
                {
                    if (fieldData == key || !fieldData._attrib.writeable)
                        continue;

                    if (isNotFirst)
                        _StringBuilder.Append(',');
                    else
                        isNotFirst = true;

                    object value = fieldData._fieldInfo.GetValue(obj);
                    if (value == null)
                        continue;

                    bool success = fieldData.Write(value, out string val);
                    allSucceeded &= success;
                    if (val == null)
                        continue;
                    if (success)
                        _StringBuilder.Append(val);
                }
                output = _StringBuilder.ToString();
                _StringBuilder.Clear();

                return allSucceeded;
            }

            public FieldData GetKey(IDictionary dict)
            {
                if (dict == null)
                    return null;

                HashSet<FieldData> confirmedCandidates = null;
                HashSet<FieldData> candidates = new HashSet<FieldData>();
                int checks = 0;
                foreach (var keyObj in dict.Keys)
                {
                    if (checks++ > 10)
                        break;

                    object key = keyObj;
                    object obj = dict[key];
                    
                    string keyStr = key.ToString(); // it might already be a string, but eh.
                    foreach (var fieldData in _fields)
                    {
                        object value = fieldData._fieldInfo.GetValue(obj);
                        if (value == null || !fieldData.Write(value, out string val))
                            continue;

                        if (val == keyStr)
                            candidates.Add(fieldData);
                    }
                    foreach (var fd in candidates)
                        if (fd._fieldName.ToLower() == "name")
                            return fd;

                    foreach (var fd in candidates)
                        if (fd._fieldName.ToLower() == "_name")
                            return fd;

                    if (confirmedCandidates == null)
                        confirmedCandidates = new HashSet<FieldData>(candidates);
                    else
                        confirmedCandidates.IntersectWith(candidates);

                    if (confirmedCandidates.Count == 0)
                        return null;
                    if (confirmedCandidates.Count == 1)
                        return candidates.First();
                }

                return null;
            }

            public string WriteHeader(FieldData key = null, bool markKey = false)
            {
                int num = _fields.Count;
                bool isNotFirst = false;

                if (key != null)
                {
                    isNotFirst = true;
                    if (markKey)
                        _StringBuilder.Append('@');
                    _StringBuilder.Append(key._fieldName);
                }

                foreach(var fieldData in _fields)
                {
                    if (fieldData == key || !fieldData._attrib.writeable)
                        continue;

                    if (isNotFirst)
                        _StringBuilder.Append(',');
                    else
                        isNotFirst = true;

                    _StringBuilder.Append(fieldData._fieldName);
                }
                var output = _StringBuilder.ToString();
                _StringBuilder.Clear();
                return output;
            }

            public static CSV GetOrCreate(Type t)
            {
                if (cache.TryGetValue(t, out var tc))
                    return tc;

                return CreateAndAdd(t);
            }

            public static CSV CreateAndAdd(Type t)
            {
                var tc = new CSV(t);
                if (tc._fields.Count == 0)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"[TweaksAndFixes] CSV: No serializing fields on object of type {t.Name} that is referenced in persistent field, adding as null to TypeCache.");
                    tc = null;
                }

                cache[t] = tc;
                return tc;
            }

            public static object GetNewInstance(Type t)
            {
                //if (!_typeToConstrutor.TryGetValue(t, out var cons))
                //{
                //    cons = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                //    _typeToConstrutor[t] = cons; // yes this might be null, but we're caching that it's null
                //}
                //if (cons == null)
                //    return null;

                //return cons.Invoke(null);
                return Activator.CreateInstance(t, true);
            }

            internal static string Test()
            {
                string basePath = Path.Combine(Config._BasePath, "Tests");
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                string pathL = Path.Combine(basePath, "testL.csv");
                string pathD = Path.Combine(basePath, "testD.csv");

                List<CSVTest> list = new List<CSVTest>();
                Dictionary<string, CSVTest> dict = new Dictionary<string, CSVTest>();
                for (int i = 0; i < 500; ++i)
                {
                    var t = new CSVTest();
                    t.name = "item" + i;
                    t.x = 0.5f + i * 0.1f;
                    t.y = 1000f + i * -0.5f;
                    t.SetVec(new Vector2(i * 100f, i));
                    t.SetGuid();
                    t.untouched = i;
                    t.test = false;
                    list.Add(t);
                    dict.Add(t.name, t);
                }

                Serializer.CSV.Write<List<CSVTest>, CSVTest>(list, pathL);
                Serializer.CSV.Write<Dictionary<string, CSVTest>.ValueCollection, CSVTest>(dict.Values, pathD);

                List<CSVTest> list2 = new List<CSVTest>();
                Serializer.CSV.Read<List<CSVTest>, CSVTest>(list2, pathL);

                Dictionary<string, CSVTest> dict2 = new Dictionary<string, CSVTest>();
                Serializer.CSV.Read<Dictionary<string, CSVTest>, string, CSVTest>(dict2, pathD, "name");
                if (list2.Count == 0 || dict2.Count == 0)
                    return "Count zero";

                for (int i = 0; i < 500; ++i)
                {
                    if (list[i].name != list2[i].name)
                        return $"Name mismatch on list: {list[i].name} vs {list2[i].name}";
                    if (!list2[i].test)
                        return "Test is false";
                    if (list[i].x != list2[i].x)
                        return $"x mismatch: {list[i].x} vs {list2[i].x}";
                    if (!dict2.TryGetValue("item" + i, out var itm))
                        return "Failed to find item" + i;
                    if (list[i].y != itm.y)
                        return $"y mismatch: {list[i].y} vs {itm.y}";
                    if (list2[i].untouched != 0)
                        return "Untouched is nonzero: " + list2[i].untouched;

                    list[i].x = 0;
                    itm.y = 0;
                }
                Serializer.CSV.Read<List<CSVTest>, CSVTest>(list, pathL);
                Serializer.CSV.Read<Dictionary<string, CSVTest>, string, CSVTest>(dict2, pathL, "name");
                for (int i = 0; i < 500; ++i)
                    if (list[i].x == 0 || dict2["item" + i].y == 0)
                        return $"x is {list[i].x} or y is {dict2["item" + i].y}";

                return "Yes";
            }
            public static void TestNative()
            {
                var dictA = Serializer.CSV.ProcessCSV<PartData>(false, Path.Combine(Config._BasePath, "Tests", "test1.csv"));
                var dictB = Serializer.CSV.ProcessCSV<PartData>(false, Path.Combine(Config._BasePath, "Tests", "test2.csv"));
                foreach (var v in dictA.Values)
                    Debug.Log($"{v.name}: {v.Id} / {v.order}");
                foreach (var v in dictB.Values)
                    Debug.Log($"{v.name}: {v.Id} / {v.order}");

                int id = 0;
                foreach (var v in G.GameData.parts.Values)
                {
                    id = v.Id;
                }
                ++id;

                foreach (var v in dictB.Values)
                {
                    v.Id = id++;
                    v.order = v.Id;
                    G.GameData.parts.Add(v.name, v);
                }
            }

            public static void TestNativePost()
            {
                foreach (var v in G.GameData.parts.Values)
                {
                    if (v.name.StartsWith("xbb"))
                    {
                        List<string> t = new List<string>();
                        foreach (var kvp in v.paramx)
                            t.Add(kvp.Key + "(" + Il2CppSystem.String.Join(";", kvp.Value.ToArray()) + ")");

                        Debug.Log($"Found {v.name}: {v.Id}. Params {v.param}: {string.Join("/", t)}");
                        break;
                    }
                }
            }


            private class CSVTest
            {
                [Serializer.Field]
                public string name;
                [Serializer.Field]
                public float x;
                [Serializer.Field]
                public float y;
                [Serializer.Field]
                private Vector2 vec;
                [Serializer.Field]
                System.Guid guid;

                public int untouched = 0;

                [Serializer.Field(writeable = false)]
                public bool test = true;


                public void SetVec(Vector2 v) { vec = v; }
                public void SetGuid() { guid = System.Guid.NewGuid(); }
            }
        }

        public class Human
        {
            public abstract class MemberData
            {
                public abstract string Name { get; }
                public abstract Type Type { get; }
                public abstract object GetValue(object host);
                public abstract void SetValue(object host, object value);
            }

            private class FieldData : MemberData
            {
                private FieldInfo _fi;
                public FieldData(FieldInfo f) { _fi = f; }
                public override string Name => _fi.Name;
                public override Type Type => _fi.FieldType;
                public override object GetValue(object host)
                    => _fi.GetValue(host);
                public override void SetValue(object host, object value)
                    => _fi.SetValue(host, value);
            }

            private class PropertyData : MemberData
            {
                private PropertyInfo _pi;
                public PropertyData(PropertyInfo p) { _pi = p; }
                public override string Name => _pi.Name;
                public override Type Type => _pi.PropertyType;
                public override object GetValue(object host)
                    => _pi.GetValue(host);
                public override void SetValue(object host, object value)
                    => _pi.SetValue(host, value);
            }


            private static readonly Dictionary<Type, Dictionary<string, MemberData>> _DictTypeCache = new Dictionary<Type, Dictionary<string, MemberData>>();

            private static Dictionary<string, MemberData> CreateCacheFor(Type type)
            {
                var dict = new Dictionary<string, MemberData>();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var f in fields)
                    dict[f.Name] = new FieldData(f);
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (var p in props)
                    dict[p.Name] = new PropertyData(p);

                _DictTypeCache[type] = dict;
                return dict;
            }

            public static Dictionary<string, MemberData> GetCache(Type type)
            {
                if (!_DictTypeCache.TryGetValue(type, out var dict))
                    return CreateCacheFor(type);
                return dict;
            }

            public static Il2CppSystem.Collections.Generic.Dictionary<int, T> HumanListToIndexedDictParsed<T>(string input, Il2CppSystem.Collections.Generic.Dictionary<int, T> dict = null)
            {
                if(dict == null)
                    dict = new Il2CppSystem.Collections.Generic.Dictionary<int, T>();

                int parenL = input.IndexOf('(');
                int parenR = input.IndexOf(')');
                if (parenL == -1 || parenR == -1)
                    return dict;

                var dataType = Serializer.CSV.FieldData.ValueDataType(typeof(T));
                var items = input.Substring(parenL + 1, parenR - parenL - 1);
                var split = items.Split(';');
                for (int i = 1; i < split.Length; i += 2)
                {
                    if (!int.TryParse(split[i - 1], out var idx))
                        continue;
                    if (string.IsNullOrEmpty(split[i]))
                        dict[idx] = default(T);
                    else
                        dict[idx] = (T)Serializer.CSV.FieldData.ReadValue(split[i], dataType, typeof(T));
                }

                return dict;
            }

            public static Il2CppSystem.Collections.Generic.Dictionary<int, T> HumanListToIndexedDict<T>(string input, Il2CppSystem.Collections.Generic.Dictionary<int, T> dict = null)
            {
                if (dict == null)
                    dict = new Il2CppSystem.Collections.Generic.Dictionary<int, T>();
                int parenL = input.IndexOf('(');
                int parenR = input.IndexOf(')');
                if (parenL == -1 || parenR == -1)
                    return dict;

                var dataType = Serializer.CSV.FieldData.ValueDataType(typeof(T));
                var items = input.Substring(parenL + 1, parenR - parenL - 1);
                var split = items.Split(';');
                for (int i = 1; i < split.Length; i += 2)
                {
                    if (!int.TryParse(split[i - 1], out var idx))
                        continue;

                    // this is INSANE but it won't let me cast string to T, so...
                    // (note we know that this is only called when T is string,
                    // but due to C# being not fast-and-loose like C, we can't
                    // do what we would in a low-level language.)
                    object o = split[i];
                    dict[idx] = (T)o;
                }

                return dict;
            }

            public static Dictionary<string, Il2CppSystem.Collections.Generic.Dictionary<int, T>> HumanModsToIndexedDicts<T>(string input)
            {
                var dict = new Dictionary<string, Il2CppSystem.Collections.Generic.Dictionary<int, T>>();
                var dataType = Serializer.CSV.FieldData.ValueDataType(typeof(T));
                bool isString = dataType == Serializer.CSV.DataType.ValueString;
                var split = input.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in split)
                {
                    int parenL = input.IndexOf('(');
                    if (parenL < 0)
                        continue;
                    var key = s.Substring(0, parenL);
                    var sub = isString ? HumanListToIndexedDict<T>(s) : HumanListToIndexedDictParsed<T>(s);
                    if (sub == null)
                        continue;
                    dict[key] = sub;
                }

                return dict;
            }

            public static bool FillIndexedDicts(object host, string input, bool update = false)
            {
                if (host == null)
                    return false;

                var split = input.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 0)
                    return true;

                var cache = GetCache(host.GetType());

                bool allSucceeded = true;
                foreach (var s in split)
                {
                    int parenL = input.IndexOf('(');
                    if (parenL < 0)
                        continue;
                    var fieldName = s.Substring(0, parenL);
                    if (!cache.TryGetValue(fieldName, out var data))
                    {
                        allSucceeded = false;
                        continue;
                    }

                    Il2CppSystem.Object dict = null;
                    if (data.Type == typeof(Il2CppSystem.Collections.Generic.Dictionary<int, int>))
                        dict = HumanListToIndexedDictParsed<int>(s, update ? (Il2CppSystem.Collections.Generic.Dictionary<int, int>)data.GetValue(host) : null);
                    else if (data.Type == typeof(Il2CppSystem.Collections.Generic.Dictionary<int, float>))
                        dict = HumanListToIndexedDictParsed<float>(s, update ? (Il2CppSystem.Collections.Generic.Dictionary<int, float>)data.GetValue(host) : null);
                    else if (data.Type == typeof(Il2CppSystem.Collections.Generic.Dictionary<int, string>))
                        dict = HumanListToIndexedDict<string>(s, update ? (Il2CppSystem.Collections.Generic.Dictionary<int, string>)data.GetValue(host) : null);
                    else
                    {
                        allSucceeded = false;
                        continue;
                    }
                    if (!update)
                        data.SetValue(host, dict);
                }
                return allSucceeded;
            }

            private static List<string> HumanModToList(string input, out string key, HashSet<string> discard)
            {
                key = null;
                input.Trim();
                int parenA = input.IndexOf('(');
                if (parenA < 0)
                {
                    if (discard != null && discard.Contains(input))
                        return null;

                    key = input;
                    return new List<string>();
                }


                key = input.Substring(0, parenA);
                if (discard != null && discard.Contains(key))
                    return null;

                int parenB = input.LastIndexOf(')');
                if (parenB < parenA)
                    parenB = input.Length;

                ++parenA;
                string val = input.Substring(parenA, parenB - parenA);
                val.Trim();
                var split = val.Split(';');
                if (split == null || split.Length == 0)
                    return new List<string>();

                var lst = new List<string>();
                foreach (var s in split)
                    lst.Add(s.Trim());

                return lst;
            }

            public static Dictionary<string, List<string>> HumanModToDictionary1D(string input, HashSet<string> discard = null)
            {
                var dict = new Dictionary<string, List<string>>();
                var split = input.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in split)
                {
                    var list = HumanModToList(s, out var key, discard);
                    if (list == null)
                        continue;
                    dict[key] = list;
                }
                return dict;
            }

            public static Dictionary<string, List<List<string>>> HumanModToDictionary2D(string input, HashSet<string> discard = null)
            {
                var dict = new Dictionary<string, List<List<string>>>();
                var split = input.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in split)
                {
                    var list = HumanModToList(s, out var key, discard);
                    if (list == null)
                        continue;

                    if (dict.TryGetValue(key, out var l))
                    {
                        l.Add(list);
                    }
                    else
                    {
                        l = new List<List<string>>();
                        l.Add(list);
                        dict[key] = l;
                    }
                }
                return dict;
            }

            static readonly char[] _SplitChars = new char[] { ' ', ',', '\t', ';' };

            public static List<string> HumanListToList(string input)
            {
                var arr = input.Split(_SplitChars, StringSplitOptions.RemoveEmptyEntries);
                return arr.ToList();
            }

            public static HashSet<string> HumanListToSet(string input)
            {
                var arr = input.Split(_SplitChars, StringSplitOptions.RemoveEmptyEntries);
                return arr.ToHashSet();
            }
        }

        [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
        public class Field : System.Attribute
        {
            public string name;
            public bool writeable;

            public Field()
            {
                name = string.Empty;
                writeable = true;
            }
        }

        public interface IPostProcess
        {
            public void PostProcess();
        }

        public static Vector2 ParseVector2(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length != 2)
                return Vector2.zero;

            return new Vector2(float.Parse(data[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(data[1], System.Globalization.CultureInfo.InvariantCulture));
        }

        public static Vector3 ParseVector3(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length != 3)
                return Vector3.zero;

            return new Vector3(float.Parse(data[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(data[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(data[2], System.Globalization.CultureInfo.InvariantCulture));
        }

        public static Vector4 ParseVector4(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length != 4)
                return Vector4.zero;

            return new Vector4(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
        }

        public static Quaternion ParseQuaternion(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length != 4)
                return Quaternion.identity;

            return new Quaternion(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
        }

        public static Matrix4x4 ParseMatrix4x4(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length != 16)
                return Matrix4x4.identity;

            Matrix4x4 matrix = Matrix4x4.identity;

            matrix.m00 = float.Parse(data[0]);
            matrix.m01 = float.Parse(data[1]);
            matrix.m02 = float.Parse(data[2]);
            matrix.m03 = float.Parse(data[3]);

            matrix.m10 = float.Parse(data[4]);
            matrix.m11 = float.Parse(data[5]);
            matrix.m11 = float.Parse(data[6]);
            matrix.m12 = float.Parse(data[7]);

            matrix.m20 = float.Parse(data[8]);
            matrix.m21 = float.Parse(data[9]);
            matrix.m22 = float.Parse(data[10]);
            matrix.m23 = float.Parse(data[11]);

            matrix.m30 = float.Parse(data[12]);
            matrix.m31 = float.Parse(data[13]);
            matrix.m32 = float.Parse(data[14]);
            matrix.m33 = float.Parse(data[15]);

            return matrix;
        }

        public static Color ParseColor(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 3 || data.Length > 4)
                return Color.white;

            if (data.Length == 3)
                return new Color(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
            else
                return new Color(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
        }

        public static Color32 ParseColor32(string val)
        {
            var data = val.Split(new char[] { ',', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 3 || data.Length > 4)
                return Color.white;

            if (data.Length == 3)
                return new Color32(byte.Parse(data[0]), byte.Parse(data[1]), byte.Parse(data[2]), 255);
            else
                return new Color32(byte.Parse(data[0]), byte.Parse(data[1]), byte.Parse(data[2]), byte.Parse(data[3]));
        }

        public static string WriteVector(Vector2 vector)
        {
            return vector.x.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + vector.y.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string WriteVector(Vector3 vector)
        {
            return vector.x.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + vector.y.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + vector.z.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string WriteVector(Vector4 vector)
        {
            //if (vector == null) return "";
            return vector.x.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + vector.y.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + vector.z.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + vector.w.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string WriteQuaternion(Quaternion quaternion)
        {
            return quaternion.x.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + quaternion.y.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + quaternion.z.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + quaternion.w.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string WriteMatrix4x4(Matrix4x4 matrix)
        {
            return
                matrix.m00.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m01.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m02.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m03.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","

                + matrix.m10.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m11.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m12.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m13.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","

                + matrix.m20.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m21.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m22.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m23.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","

                + matrix.m30.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m31.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m32.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + ","
                + matrix.m33.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string WriteColor(Color color)
        {
            return color.r.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + color.g.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + color.b.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "," + color.a.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string WriteColor(Color32 color)
        {
            return color.r.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + color.g.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + color.b.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + color.a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}