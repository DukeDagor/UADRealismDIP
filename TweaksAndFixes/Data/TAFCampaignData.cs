using Harmony;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TweaksAndFixes.Harmony;
using static TweaksAndFixes.Data.TAFCampaignData.TAFSaveData.TAFSaveEntry;

namespace TweaksAndFixes.Data
{
    internal static class TAFCampaignData
    {
        public class TAFSaveData
        {
            public enum SaveType
            {
                main,
                auto
            }

            public class TAFSaveEntry : Serializer.IPostProcess
            {
                public enum SaveEntryDataType
                {
                    INT,
                    FLOAT,
                    BOOL,
                    STRING,
                    OBJECT,
                    NONE
                }

                [Serializer.Field] public string name = string.Empty;
                [Serializer.Field] public string data = string.Empty;
                public int dataAsInt = 0;
                public float dataAsFloat = 0;
                public bool dataAsBool = false;
                [Serializer.Field] public string type = string.Empty;
                public SaveEntryDataType DataType = SaveEntryDataType.NONE;
                [Serializer.Field] public string param = string.Empty;
                public List<string> Params { get; protected set; } = new();

                public void PostProcess()
                {
                    type = type.ToLower();

                    switch (type)
                    {
                        case "int":
                            DataType = SaveEntryDataType.INT;
                            if (!int.TryParse(data, out dataAsInt))
                                Melon<TweaksAndFixes>.Logger.Error(
                                    $"Save Entry `{name}`: Invalid {type} `{data}`"
                                );
                            break;

                        case "float":
                            DataType = SaveEntryDataType.FLOAT;
                            if (!float.TryParse(data, out dataAsFloat))
                                Melon<TweaksAndFixes>.Logger.Error(
                                    $"Save Entry `{name}`: Invalid {type} `{data}`"
                                );
                            break;

                        case "bool":
                            DataType = SaveEntryDataType.BOOL;
                            if (!bool.TryParse(data, out dataAsBool))
                                Melon<TweaksAndFixes>.Logger.Error(
                                    $"Save Entry `{name}`: Invalid {type} `{data}`"
                                );
                            break;

                        case "string":
                            DataType = SaveEntryDataType.STRING;
                            break;

                        case "object":
                            DataType = SaveEntryDataType.OBJECT;
                            if (!data.StartsWith('{'))
                                Melon<TweaksAndFixes>.Logger.Error(
                                    $"Save Entry `{name}`: Invalid {type} `{data}`"
                                );
                            break;

                        // TODO: Implement auto type detection?
                        case "":
                        default:
                            DataType = SaveEntryDataType.NONE;
                            Melon<TweaksAndFixes>.Logger.Error(
                                $"Save Entry `{name}`: Invalid `{type}` for `{data}`"
                            );
                            break;
                    }

                    Params = new(param.Split(';'));
                }

                public T? GetDataAsObject<T>()
                {
                    if (DataType != SaveEntryDataType.OBJECT)
                    {
                        Melon<TweaksAndFixes>.Logger.Error(
                            $"Save Entry `{name}`: Tried to get `{typeof(T).Name}`" +
                            $" but it is registered as a `{DataType}`." +
                            $" Set the type on creation or fetch as the correct type."
                        );
                        return default;
                    }

                    T? res = default;

                    try
                    {
                        res = System.Text.Json.JsonSerializer.Deserialize<T>(data);
                    }
                    catch (Exception e)
                    {
                        Melon<TweaksAndFixes>.Logger.Error(
                            $"Save Entry `{name}`: Tried to get `{typeof(T).Name}`" +
                            $" but data was invalid. Failed with error:\n{e.Message}"
                        );
                    }

                    return res;
                }

                public override string ToString()
                {
                    return $"{name},{data},{type},{param}\n";
                }
            }

            public Dictionary<string, TAFSaveEntry> entryData = new();
            public string saveName = string.Empty;
            public string filePath = string.Empty;
            public int saveIndex = -1;

            public TAFSaveData(string path)
            {
                filePath = path;
                var store = GetStore();

                // TODO: Handle this better
                if (store == null)
                    return;

                LoadData(store);
                saveIndex = entryData["TAF_SaveIndex"].dataAsInt;
            }

            public TAFSaveData(string path, int index)
            {
                filePath = path;
                saveIndex = index;
            }

            public CampaignController.Store? GetStore()
            {
                try
                {
                    var s = Util.DeserializeObjectByte<CampaignController.Store>(File.ReadAllBytes(filePath));

                    if (s == null)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"    Corrupted save: {Path.GetFileName(filePath)}");
                        return null;
                    }

                    Melon<TweaksAndFixes>.Logger.Msg($"    Found save: {Path.GetFileName(filePath)}");

                    return s;
                }
                catch (Exception e)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"    Corrupted save: {Path.GetFileName(filePath)}");
                }

                return null;
            }

            public void LoadData(CampaignController.Store store)
            {
                if (store.FriendlyName.StartsWith("["))
                {
                    // Error!
                    // GetDefaultCampaignStore(this, store);
                    return;
                }

                List<TAFSaveEntry> list = new();
                string? text = store.FriendlyName;

                Melon<TweaksAndFixes>.Logger.Error($"Loading: {store.FriendlyName}");

                if (text == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Failed to load `mounts.csv`.");
                    return;
                }

                Serializer.CSV.Read<List<TAFSaveEntry>, TAFSaveEntry>(text, list, true, true);

                foreach (var entry in list)
                {
                    if (entryData.ContainsKey(entry.name))
                    {
                        Melon<TweaksAndFixes>.Logger.Error(
                            $"Save Entry `{entry.name}`: An entry of the same name already exists!"
                        );

                        continue;
                    }

                    entryData.Add(entry.name, entry);
                }
            }

            public void AddData(
                string name, string data, SaveEntryDataType type, string param = "")
            {
                if (data.Contains(name))
                {
                    return;
                }

                TAFSaveEntry newEntry = new();
                newEntry.name = name;
                newEntry.data = data;
                newEntry.type = type.ToString();
                newEntry.param = param;
                newEntry.PostProcess();

                entryData.Add(name, newEntry);
            }

            public void SetData(string name, string data)
            {
                if (!data.Contains(name))
                {
                    return;
                }

                var entry = entryData[name];
                entry.data = data;

                // If the type needs to be converted from string -> type, call PostProcess
                if (entry.DataType == SaveEntryDataType.INT
                    || entry.DataType == SaveEntryDataType.FLOAT
                    || entry.DataType == SaveEntryDataType.BOOL)
                    entry.PostProcess();
            }

            public override string ToString()
            {
                string str = $"@name,data,type,param\n";

                foreach (var data in entryData)
                {
                    str += data.Value.ToString();
                }

                Melon<TweaksAndFixes>.Logger.Msg($"TAF Campaign Store ToString:\n{str}");

                return str;
            }
        }

        public static List<System.Action<TAFSaveData>> DefaultGenerators = new();

        public static List<TAFSaveData> Data = new();

        public static TAFSaveData GetDefaultCampaignStore(TAFSaveData data)
        {
            foreach (var gen in DefaultGenerators)
            {
                gen.Invoke(data);
            }

            return data;
        }

        public static void MakeNewDataStore(int startingYear, PlayerData mainPlayer)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Making new data store...");
            int saveIndex = GameManager.Instance.currentCampaignSlotIndex;

            var path = Storage.prefix + $"Saves/Save_{saveIndex}.bin";

            Melon<TweaksAndFixes>.Logger.Msg($"  Creating");
            TAFSaveData data = new(path, saveIndex);

            Melon<TweaksAndFixes>.Logger.Msg($"  Initing");
            GetDefaultCampaignStore(data);
            
            data.entryData["TAF_SaveName"].data = 
                Patch_UISaveLoadWindow.nextCampaignName;

            data.entryData["TAF_PlayerName"].data =
                Player.GetNameUI(null, mainPlayer, startingYear);

            data.entryData["TAF_SaveIndex"].data =
                GameManager.Instance.currentCampaignSlotIndex.ToString();

            Melon<TweaksAndFixes>.Logger.Msg($"  Adding");
            Data.Add(data);
        }

        public static void DeleteStoreByIndex(int index)
        {
            foreach (var data in Data.ToArray())
            {
                if (data.saveIndex == index)
                {
                    Data.Remove(data);
                }
            }
        }

        public static TAFSaveData? GetStoreByIndex(int index)
        {
            foreach (var data in Data)
            {
                if (data.saveIndex == index)
                {
                    return data;
                }
            }

            return null;
        }

        public static void LoadCampaignStores()
        {
            Data.Clear();

            Melon<TweaksAndFixes>.Logger.Msg($"Loading save stores...");

            var prefix = Storage.prefix + $"Saves/";
            if (!Directory.Exists(prefix))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  Creating save folder...");
                Directory.CreateDirectory(prefix);
            }
            Melon<TweaksAndFixes>.Logger.Msg($"  At: {prefix}");

            foreach (var f in Directory.GetFiles(prefix, "*.bin"))
            {
                Data.Add(new(f));
            }
        }

        static TAFCampaignData()
        {
            InitTAFGenerators();
        }

        public static void InitTAFGenerators()
        {
            DefaultGenerators.Add(new System.Action<TAFSaveData>(
            (TAFSaveData data) => {
                // var mainPlayer = store.Players[0];
                // 
                // foreach (var player in store.Players)
                // {
                //     if (!player.isMain)
                //         continue;
                // 
                //     mainPlayer = player;
                //     break;
                // }

                data.AddData(
                    "TAF_SaveName",
                    "Unamed Save",
                    SaveEntryDataType.STRING
                );
                
                // if (G.GameData.players.ContainsKey(mainPlayer.name))
                //     playerName = Player.GetNameUI(null, G.GameData.players[mainPlayer.name], store.StartYear);
                data.AddData(
                    "TAF_PlayerName",
                    "--",
                    SaveEntryDataType.STRING
                );
                
                data.AddData(
                    "TAF_VersionName",
                    Config.ParamS("taf_versiontext", "UAD 1.7.0.0") ?? "ERROR",
                    SaveEntryDataType.STRING
                );

                data.AddData(
                    "TAF_SaveIndex",
                    "-1",
                    SaveEntryDataType.INT
                );
            }));
        }
    }
}
