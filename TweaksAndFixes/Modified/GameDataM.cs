using MelonLoader;
using UnityEngine;
using Il2Cpp;
using TweaksAndFixes.Data;
using TweaksAndFixes.Modified;

#pragma warning disable CS8600
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    public static class GameDataM
    {
        public static void LoadData(GameData _this)
        {
            if (!_this.forceUseLocal)
            {
                //MonoBehaviourExt.Fail();
                return;
            }

            string loadingText = null;
            // It is unclear why stock does this, since it doesn't yield or fail, so why check for remaining loaders.
            // But sure, let's replciate it.
            var newList = new Il2CppSystem.Collections.Generic.List<GameData.LoadInfo>();
            foreach (var l in _this.loaders)
                newList.Add(l);
            foreach (var l in newList)
            {
                _this.loaders.Remove(l);
                ProcessLoadInfo(_this, l);
            }
            if (_this.loaders.Count > 0)
            {
                string loc = LocalizeManager.Localize("$Ui_Loading");
                string name = Util.CamelCaseSplit(_this.loaders[0].name, ";", true);
                loadingText = loc + ";" + name;
            }
            G.ui._loadingText_k__BackingField = loadingText;
        }

        public static string? GetText(string name)
        {
            string filename = name + ".csv";
            string fileOver = name + "_override.csv";
            string? text = Serializer.CSV.GetTextFromFile(filename);
            string? textOverride = Serializer.CSV.GetTextFromFile(fileOver);
            if (text == null && textOverride == null)
            {
                return null;
            }

            string oText;
            if (text != null)
            {
                oText = $"Replacing built-in asset {name} with {filename}";
                if (textOverride != null)
                    oText += $" and overriding with {fileOver}";
            }
            else
                oText = $"Overriding built-in asset {name} with {fileOver}";

            Melon<TweaksAndFixes>.Logger.Msg(oText);

            if (text != null && textOverride != null)
                return Serializer.CSV.MergeCSV(text, textOverride);
            else if (textOverride != null)
                return Serializer.CSV.MergeCSV(Util.ResourcesLoad<TextAsset>(name).text, textOverride);
            else
                return text;
        }

        private static void ProcessLoadInfo(GameData _this, GameData.LoadInfo l)
        {
            string? text = GetText(l.name);
            if (text == null)
            {
                l.process.Invoke(l);
                return;
            }

            Serializer.CSV.SetTempTextAssetText(text);
            string oName = l.name;
            l.name = Serializer.CSV._TempTextAssetName;
            l.process.Invoke(l);
            l.name = oName;
        }
    }

    [RegisterTypeInIl2Cpp]
    public class GameDataReloader : MonoBehaviour
    {
        public GameDataReloader(IntPtr ptr) : base(ptr) { }

        public static void Create()
        {

            var go = new GameObject("GameDataReloader");
            go.AddComponent<GameDataReloader>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void Update()
        {
            if (UnityEngine.Input.GetKeyUp(KeyCode.F9))
            {
                Melon<TweaksAndFixes>.Logger.Msg("Performing minimal hot-reloading. Note that no items may be added or removed from [parts, components, randParts, randPartsRefit, or technologies] or this will break! For [mounts and accuraciesExInfo], reload the battle/hull to see effects.");
                var text = GameDataM.GetText("parts");
                if (text != null)
                {
                    Serializer.CSV.ProcessCSV<PartData>(text, false, G.GameData.parts);
                }

                text = GameDataM.GetText("components");
                if (text != null)
                {
                    Serializer.CSV.ProcessCSV<ComponentData>(text, false, G.GameData.components);
                }

                text = GameDataM.GetText("technologies");
                if (text != null)
                {
                    Serializer.CSV.ProcessCSV<TechnologyData>(text, false, G.GameData.technologies);
                }

                if (ConstructorM.active)
                {
                    text = GameDataM.GetText("partModels");
                    if (text != null)
                    {
                        Serializer.CSV.ProcessCSV<PartModelData>(text, false, G.GameData.partModels);
                    }

                    ConstructorM.ReloadModels();
                }

                AccuraciesExInfo.LoadData();

                MountOverrideData.LoadData();

                // Patch_Ship.LastCreatedShip = ShipM.GetActiveShip();
                // 
                // if (Patch_Ship.LastCreatedShip != null)
                // {
                //     MountOverrideData.ApplyMountOverridesToShip(Patch_Ship.LastCreatedShip, true);
                // }

                text = GameDataM.GetText("randParts");
                if (text != null)
                {
                    foreach (var st in G.GameData.shipTypes.Values)
                        st.randParts.Clear();
                    Serializer.CSV.ProcessCSV<RandPart>(text, false, G.GameData.randParts);
                    foreach (var kvp in G.GameData.randParts)
                    {
                        var stList = Util.HumanListToList(kvp.Value.shipTypes);
                        foreach (var stStr in stList)
                        {
                            if (G.GameData.shipTypes.TryGetValue(stStr, out var st))
                                st.randParts.Add(kvp.Value);
                        }
                    }
                }

                text = GameDataM.GetText("randPartsRefit");
                if (text != null)
                {
                    foreach (var st in G.GameData.shipTypes.Values)
                        st.randPartsRefit.Clear();
                    Serializer.CSV.ProcessCSV<RandPart>(text, false, G.GameData.randPartsRefit);
                    foreach (var kvp in G.GameData.randPartsRefit)
                    {
                        var stList = Util.HumanListToList(kvp.Value.shipTypes);
                        foreach (var stStr in stList)
                        {
                            if (G.GameData.shipTypes.TryGetValue(stStr, out var st))
                                st.randPartsRefit.Add(kvp.Value);
                        }
                    }
                }
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.F10) && GameManager.IsConstructor && PlayerController.Instance != null && PlayerController.Instance.Ship != null)
            {
                ShipM.LogRPsForHull(G.ui.mainShip.hull.data);
            }
        }
    }
}