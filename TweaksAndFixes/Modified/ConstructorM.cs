using Il2Cpp;
using TweaksAndFixes.Data;
using UnityEngine.SceneManagement;
using UnityEngine;
using MelonLoader;
using UnityEngine.UI;
using System.Text.Json;
using Il2CppTMPro;
using static TweaksAndFixes.Modified.ConstructorM;
using Harmony;
using System.Threading;

namespace TweaksAndFixes.Modified
{
    internal class ConstructorM
    {
        public class Vector2Store
        {
            public float x { get; set; }
            public float y { get; set; }

            public Vector2Store() {}


            public Vector2Store(Vector2 from)
            {
                x = from.x;
                y = from.y;
            }

            public Vector2 from()
            {
                return new(x, y);
            }
        }

        public class Vector3Store
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }

            public Vector3Store() { }

            public Vector3Store(Vector3 from)
            {
                x = from.x;
                y = from.y;
                z = from.z;
            }

            public Vector3 from()
            {
                return new(x,y,z);
            }
        }

        public class Store
        {
            public List<PartModelVisual.Store> PartModelVisuals { get; set; }
            public List<CylinderVisual.Store> CylinderVisuals { get; set; }
            public string saveFile { get; set; }
            public Vector2Store gridSize { get; set; }

            public Store()
            {
                PartModelVisuals = new();
                CylinderVisuals = new();
                saveFile = string.Empty;
                gridSize = new();
            }

            public Store(bool isCreating)
            {
                PartModelVisuals = new();
                CylinderVisuals = new();
                
                foreach (var visual in ConstructorM.Visuals)
                {
                    switch (visual.type)
                    {
                        case 1:
                            PartModelVisuals.Add(new PartModelVisual.Store((PartModelVisual)visual));
                            break;

                        case 2:
                            CylinderVisuals.Add(new CylinderVisual.Store((CylinderVisual)visual));
                            break;

                        default:
                            break;
                    }
                }

                saveFile = ConstructorM.saveFile;
                gridSize = new(ConstructorM.gridSize);
            }
        }

        public class Visual
        {
            public enum Type
            {
                NONE,
                PART_MODEL,
                CYLINDER
            }

            public int id;
            public GameObject root;
            public GameObject ui;
            public bool isDestroyed = false;
            public bool makeCopy = false;
            public int type = 0;

            public Visual()
            {
                id = -1;
                root = null;
                ui = null;
            }

            public virtual void Delete() { }

            public virtual void AddToScene() { }

            public virtual void Update(List<Visual> Visuals) { }
        }

        public class PartModelVisual : Visual
        {
            public static List<string> VALID_AXES = new() { "model", "mark", "length", "diameter" };
            public static string VALID_AXIS_STRING = "model, mark, length, diameter";

            public class Store
            {
                public int id { get; set; }
                public int type { get; set; }
                public Vector3Store position { get; set; }
                public Vector2Store spacing { get; set; }
                public Vector3Store rotation { get; set; }
                public List<string> models { get; set; }
                public List<int> modelIndexes { get; set; }
                public List<int> markIndexes { get; set; }
                public List<float> diameters { get; set; }
                public List<int> lengths { get; set; }
                public string xAxis { get; set; }
                public string yAxis { get; set; }

                public Store()
                {
                    id = new();
                    type = new();
                    position = new();
                    spacing = new();
                    rotation = new();
                    models = new();
                    modelIndexes = new();
                    markIndexes = new();
                    diameters = new();
                    lengths = new();
                    xAxis = string.Empty;
                    yAxis = string.Empty;
                }

                public Store(PartModelVisual from)
                {
                    id = from.id;
                    type = from.type;
                    position = new(from.position);
                    spacing = new(from.spacing);
                    rotation = new(from.rotation);

                    models = new(from.models);
                    modelIndexes = new(from.modelIndexes);
                    markIndexes = new(from.markIndexes);
                    diameters = new(from.diameters);
                    lengths = new(from.lengths);

                    xAxis = from.xAxis;
                    yAxis = from.yAxis;
                }
            }

            public List<string> models = new();
            public Vector2 spacing;
            public Vector3 rotation;
            public Vector3 position;
            public List<int> modelIndexes = new();
            public List<int> markIndexes = new();
            public List<float> diameters = new();
            public List<int> lengths = new();
            public string xAxis;
            public string yAxis;

            public PartModelVisual(Store from)
            {
                id = from.id;
                type = from.type;
                position = from.position.from();
                spacing = from.spacing.from();
                rotation = from.rotation.from();
                type = 1;

                xAxis = from.xAxis;
                yAxis = from.yAxis;

                foreach (var model in from.models)
                {
                    models.Add(model);
                }

                foreach (var index in from.modelIndexes)
                {
                    modelIndexes.Add(index);
                }

                foreach (var mark in from.markIndexes)
                {
                    markIndexes.Add(mark);
                }

                foreach (var diameter in from.diameters)
                {
                    diameters.Add(diameter);
                }

                foreach (var length in from.lengths)
                {
                    lengths.Add(length);
                }

                root = new("Gun_Visual");
                root.transform.SetParent(ConstructorM.models);
                root.transform.localPosition = position;

                ui = GameObject.Instantiate(PartModelVisualTemplate);
                MakeUI();

                AddToScene();
            }

            public PartModelVisual(int id)
            {
                this.id = id;
                position = new();
                spacing = new Vector2(25, 25);
                rotation = new();
                type = 1;

                Melon<TweaksAndFixes>.Logger.Msg($"Checking: {id} : {type}");

                modelIndexes.Add(-1);

                markIndexes.Add(1);
                markIndexes.Add(2);
                markIndexes.Add(3);
                markIndexes.Add(4);
                markIndexes.Add(5);

                diameters.Add(0);

                lengths.Add(0);

                xAxis = "mark";
                yAxis = "model";

                root = new("Gun_Visual");
                root.transform.SetParent(ConstructorM.models);
                root.transform.localPosition = position;

                ui = GameObject.Instantiate(PartModelVisualTemplate);
                MakeUI();
                // gun_12_x2,gun_12_x2_germany_bb_bc,gun_12_x2_britain_bb_bc,gun_12_x2_italy_bb_bc,gun_12_x2_japan_bb_bc,gun_12_x2_usa_bb_bc,gun_12_x2_france_bb_bc,gun_12_x2_russia_bb_bc,gun_12_x2_austria_bb_bc,gun_12_x2_spain_bb_bc,gun_12_x2_china_bb_bc,gun_12_x2_greece_bb_bc,gun_12_x2_scandinavia_bb_bc,gun_12_x2_ottoman_bb_bc,gun_12_x2_brazil_bb_bc

                AddToScene();
            }

            public PartModelVisual(int id, PartModelVisual copy)
            {
                this.id = id;
                position = copy.position;
                spacing = copy.spacing;
                rotation = copy.rotation;
                type = 1;

                xAxis = copy.xAxis;
                yAxis = copy.yAxis;

                foreach (var model in copy.models)
                {
                    models.Add(model);
                }

                foreach (var index in copy.modelIndexes)
                {
                    modelIndexes.Add(index);
                }

                foreach (var mark in copy.markIndexes)
                {
                    markIndexes.Add(mark);
                }

                foreach (var diameter in copy.diameters)
                {
                    diameters.Add(diameter);
                }

                foreach (var length in copy.lengths)
                {
                    lengths.Add(length);
                }

                root = new("Gun_Visual");
                root.transform.SetParent(ConstructorM.models);
                root.transform.localPosition = position;

                ui = GameObject.Instantiate(PartModelVisualTemplate);
                MakeUI();

                AddToScene();
            }

            private void MakeUI()
            {
                ui.SetParent(VisualsList, false);
                ui.SetActive(true);

                var modelsInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_List_Input"), 25);
                modelsInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    this.models.Clear();

                    char separator = ',';
                    var split = value.Split(separator);

                    foreach (string key in split)
                    {
                        if (!G.GameData.partModels.ContainsKey(key))
                        {
                            if (key.Length <= 5)
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"Error: Could not find `{key}` in partModels.csv! Ensure your keys only use `{separator}` as a separator! (can be changed in params.csv)");
                            }
                            else
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"Error: Could not find `{key}` in partModels.csv!");
                            }

                            continue;
                        }

                        this.models.Add(key);
                    }

                    AddToScene();

                    modelsInputField.SetText(value);
                }));
                modelsInputField.SetOnValueChange(new System.Action<string>((string value) => {
                    modelsInputField.EditField.text = modelsInputField.EditField.text
                        .Replace("\n", ",")
                        .Replace("\r", "");
                }));
                string modelsText = "";
                foreach (var elem in models) modelsText += elem + (models.IndexOf(elem) == models.Count - 1 ? "" : ",");
                modelsInputField.SetText(modelsText);

                var positionInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_Position_Input"), 8);
                positionInputField.SetOnSubmit(new System.Action<string>((string value) => {

                    char separator = ',';
                    var split = value.Split(separator);

                    if (split.Length != 3)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid position `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[0], out float x))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid x in `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[1], out float y))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid y in `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[2], out float z))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid z in `{value}`!");
                        return;
                    }

                    root.transform.localPosition = new(x, y, z);
                    this.position = new(x, y, z);

                    positionInputField.StaticText.text = value;
                }));
                positionInputField.SetText($"{position.x},{position.y},{position.z}");

                var spacingInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_Spacing_Input"), 8);
                spacingInputField.SetOnSubmit(new System.Action<string>((string value) => {

                    char separator = ',';
                    var split = value.Split(separator);

                    if (split.Length != 2)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid Spacing `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[0], out float x))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid x in `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[1], out float y))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid y in `{value}`!");
                        return;
                    }

                    this.spacing = new(x, y);

                    AddToScene();

                    spacingInputField.StaticText.text = value;
                }));
                spacingInputField.SetText($"{spacing.x},{spacing.y}");

                var rotationInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_Rotation_Input"), 8);
                rotationInputField.SetOnSubmit(new System.Action<string>((string value) => {

                    char separator = ',';
                    var split = value.Split(separator);

                    if (split.Length != 3)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid rotation `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[0], out float x))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid x in `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[1], out float y))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid y in `{value}`!");
                        return;
                    }

                    if (!float.TryParse(split[2], out float z))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid z in `{value}`!");
                        return;
                    }

                    this.rotation = new(x, y, z);

                    AddToScene();

                    rotationInputField.StaticText.text = value;
                }));
                rotationInputField.SetText($"{rotation.x},{rotation.y},{rotation.z}");

                var modelIndexesInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_Indexes_Input"), 8);
                modelIndexesInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    if (value == "all")
                    {
                        this.modelIndexes.Clear();

                        this.modelIndexes.Add(-1);
                    }
                    else
                    {
                        char separator = ',';
                        var split = value.Split(separator);

                        this.modelIndexes.Clear();

                        foreach (string key in split)
                        {
                            if (!int.TryParse(key, out int num))
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid model index `{key}`!");
                                return;
                            }

                            if (num > this.models.Count)
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"Error: Model index `{key}` out of range! (1 ~ {this.models.Count})");
                                return;
                            }

                            this.modelIndexes.Add(num);
                        }
                    }

                    AddToScene();

                    modelIndexesInputField.SetText(value);
                }));
                if (modelIndexes.Count == 1 && modelIndexes[0] == -1)
                {
                    modelIndexesInputField.SetText("all");
                }
                else
                {
                    string indexesText = "";
                    foreach (var elem in modelIndexes) indexesText += elem + (modelIndexes.IndexOf(elem) == modelIndexes.Count - 1 ? "" : ",");
                    modelIndexesInputField.SetText(indexesText);
                }


                var markIndexesInputField = new TAFUI.TAF_InputField(ui.GetChild("Mark_Indexes_Input"), 8);
                markIndexesInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    char separator = ',';
                    var split = value.Split(separator);

                    this.markIndexes.Clear();

                    foreach (string key in split)
                    {
                        if (!int.TryParse(key, out int num))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid mark index `{key}`!");
                            return;
                        }

                        if (num > 5)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Mark index `{key}` out of range! (1 ~ 5)");
                            return;
                        }

                        this.markIndexes.Add(num);
                    }

                    AddToScene();

                    markIndexesInputField.SetText(value);
                }));
                string marksText = "";
                foreach (var elem in markIndexes) marksText += elem + (markIndexes.IndexOf(elem) == markIndexes.Count - 1 ? "" : ",");
                markIndexesInputField.SetText(marksText);

                var diametersInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_Diameter_Input"), 8);
                diametersInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    char separator = ',';
                    var split = value.Split(separator);

                    this.diameters.Clear();

                    foreach (string key in split)
                    {
                        if (!float.TryParse(key, out float num))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid gun diameter `{key}`!");
                            return;
                        }

                        if (num < -0.9 || num > 0.9)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Gun diameter `{key}` out of range! (-0.9 ~ 0.9)");
                            return;
                        }

                        this.diameters.Add(num);
                    }

                    AddToScene();

                    diametersInputField.SetText(value);
                }));
                string diametersText = "";
                foreach (var elem in diameters) diametersText += elem + (diameters.IndexOf(elem) == diameters.Count - 1 ? "" : ",");
                diametersInputField.SetText(diametersText);

                var lengthsInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_Length_Input"), 8);
                lengthsInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    char separator = ',';
                    var split = value.Split(separator);

                    this.lengths.Clear();

                    foreach (string key in split)
                    {
                        if (!int.TryParse(key, out int num))
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid gun length `{key}`!");
                            return;
                        }

                        if (num < -25 || num > 25)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Gun length `{key}` out of range! (-20 ~ 20)");
                            return;
                        }

                        this.lengths.Add(num);
                    }

                    AddToScene();

                    lengthsInputField.SetText(value);
                }));
                string lengthsText = "";
                foreach (var elem in lengths) lengthsText += elem + (lengths.IndexOf(elem) == lengths.Count - 1 ? "" : ",");
                lengthsInputField.SetText(lengthsText);

                var xAxisInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_XAxis_Input"), 8);
                xAxisInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    if (!VALID_AXES.Contains(value))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: `{value}` is not a valid axis! ({VALID_AXIS_STRING})");
                        return;
                    }

                    xAxis = (value);

                    AddToScene();

                    xAxisInputField.SetText(value);
                }));
                xAxisInputField.SetText(xAxis);

                var yAxisInputField = new TAFUI.TAF_InputField(ui.GetChild("Model_YAxis_Input"), 8);
                yAxisInputField.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    if (!VALID_AXES.Contains(value))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: `{value}` is not a valid axis! ({VALID_AXIS_STRING})");
                        return;
                    }

                    yAxis = value;

                    AddToScene();

                    yAxisInputField.SetText(value);
                }));
                yAxisInputField.SetText(yAxis);

                var deleteButton = new TAFUI.TAF_Button(ui.GetChild("Model_Delete_Button"));
                deleteButton.SetOnClick(new Action(() => {
                    Delete();
                }));

                var copyButton = new TAFUI.TAF_Button(ui.GetChild("Model_Copy_Button"));
                copyButton.SetOnClick(new Action(() => {
                    makeCopy = true;
                }));
            }

            public override void Delete()
            {
                foreach (var child in root.GetChildren())
                {
                    child.TryDestroy();
                }

                root.TryDestroy();

                ui.TryDestroy();

                isDestroyed = true;
                dirtyChanged = true;
            }

            public override void AddToScene()
            {
                foreach (var child in root.GetChildren())
                {
                    child.TryDestroy(true);
                }

                dirtyChanged = true;

                if (xAxis.Length == 0 || yAxis.Length == 0)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"One axis was left blank or invalid!");
                    return;
                }

                bool xAxisHasModels = xAxis == "model";
                bool yAxisHasModels = yAxis == "model";

                if (!xAxisHasModels && !yAxisHasModels)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"At least one axis must be `model`!");
                    return;
                }

                List<int> realModelIndexes = new(modelIndexes);

                if (realModelIndexes.Count > 0 && realModelIndexes[0] == -1)
                {
                    realModelIndexes.Clear();

                    for (int i = 1; i <= models.Count; i++)
                    {
                        realModelIndexes.Add(i);
                    }
                }

                int xAxisLength = -1;
                bool foundModels = false;

                // Get X-Axis length and ensure interlaced data sets are same length.
                switch (xAxis)
                {
                    case "model":
                        if (!foundModels)
                        {
                            foundModels = true;
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Can only have one `models` entry!");
                            return;
                        }

                        xAxisLength = realModelIndexes.Count;
                        break;
                    case "mark":
                        xAxisLength = markIndexes.Count;
                        break;
                    case "length":
                        xAxisLength = lengths.Count;
                        break;
                    case "diameter":
                        xAxisLength = diameters.Count;
                        break;
                }

                int yAxisLength = -1;

                // Get Y-Axis length and ensure interlaced data sets are same length.
                switch (yAxis)
                {
                    case "model":
                        if (!foundModels)
                        {
                            foundModels = true;
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"Error: Can only have one `models` entry!");
                            return;
                        }
                        yAxisLength = realModelIndexes.Count;
                        break;
                    case "mark":
                        yAxisLength = markIndexes.Count;
                        break;
                    case "length":
                        yAxisLength = lengths.Count;
                        break;
                    case "diameter":
                        yAxisLength = diameters.Count;
                        break;
                }

                PartModelData lastData = null;

                Melon<TweaksAndFixes>.Logger.Msg($"Adding to scene: {xAxis} : {xAxisLength} | {yAxis} : {yAxisLength}");

                // Loop over x and y axes
                for (int x = 0; x < xAxisLength; x++)
                {
                    if (xAxisHasModels) lastData = G.GameData.partModels[models[realModelIndexes[x] - 1]];

                    for (int y = 0; y < yAxisLength; y++)
                    {
                        if (yAxisHasModels) lastData = G.GameData.partModels[models[realModelIndexes[y] - 1]];

                        if (lastData == null)
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"  Error: Data was null for {x}, {y}!");
                            continue;
                        }

                        int mark;

                        if (xAxis == "mark") mark = markIndexes[x];
                        else if (yAxis == "mark") mark = markIndexes[y];
                        else if (markIndexes.Count == 0) mark = 0;
                        else mark = markIndexes[0];

                        float diameter;

                        if (xAxis == "diameter") diameter = diameters[x];
                        else if (yAxis == "diameter") diameter = diameters[y];
                        else if (diameters.Count == 0) diameter = 0;
                        else diameter = diameters[0];

                        float length;

                        if (xAxis == "length") length = lengths[x];
                        else if (yAxis == "length") length = lengths[y];
                        else if (lengths.Count == 0) length = 0;
                        else length = lengths[0];

                        bool hasModel = lastData.models[mark] != String.Empty;

                        string model = hasModel ? lastData.models[mark] : G.GameData.partModels[lastData.subName].models[mark];

                        float scale = hasModel ? lastData.scales[mark] : G.GameData.partModels[lastData.subName].scales[mark];

                        float maxScale = hasModel ? lastData.maxScales[mark] : G.GameData.partModels[lastData.subName].maxScales[mark];

                        Vector3 position = new(spacing.x * x, 0, spacing.y * y);

                        Melon<TweaksAndFixes>.Logger.Msg($"  Mark {mark} gun with diameter {diameter} and length {length} has model {model} (default == {!hasModel}) with scale {ModUtils.Lerp(scale, maxScale, diameter)}");

                        AddModelToScene(root, model, position, rotation, ModUtils.Lerp(scale, maxScale, diameter));
                    }
                }

                dirtyChanged = true;
            }
        }

        public class CylinderVisual : Visual
        {
            public class Store
            {
                public int id { get; set; }
                public int type { get; set; }
                public float diameter { get; set; }
                public string unit { get; set; }
                public string binding { get; set; }

                public Store()
                {
                    id = new();
                    type = new();
                    diameter = new();
                    models = new();
                    unit = string.Empty;
                    binding = string.Empty;
                }

                public Store(CylinderVisual from)
                {
                    id = from.id;
                    type = from.type;
                    diameter = from.diameter;

                    unit = from.unit;
                    binding = from.binding;
                }
            }


            public string binding = "turret";
            public float diameter;
            public string unit;
            public Dictionary<GameObject, GameObject> VisualToCylider = new();

            public static Dictionary<string, CylinderOverride> CylinderOverrides = new();

            public class CylinderOverride : Serializer.IPostProcess
            {

                [Serializer.Field] public string name = string.Empty;
                [Serializer.Field] public string barrel_1 = string.Empty;
                [Serializer.Field] public string barrel_2 = string.Empty;
                [Serializer.Field] public string barrel_3 = string.Empty;
                [Serializer.Field] public string barrel_4 = string.Empty;
                [Serializer.Field] public float barrel_length = 0;
                [Serializer.Field] public float turret_height = 0;

                public Vector3 barrelOffset1 = Vector3.zero;
                public Vector3 barrelOffset2 = Vector3.zero;
                public Vector3 barrelOffset3 = Vector3.zero;
                public Vector3 barrelOffset4 = Vector3.zero;
                public List<Vector3> barrelOffsets = new();

                public void PostProcess()
                {
                    if (barrel_1.Length >= 7 && barrel_1.StartsWith('(') && barrel_1.EndsWith(')'))
                    {
                        var posData = barrel_1[1..^1].Replace(" ", "").Split(",");

                        if (posData.Length == 3)
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;

                            if (float.TryParse(posData[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(posData[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out y) && float.TryParse(posData[2], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out z))
                            {
                                barrelOffset1 = new Vector3(x, y, z);
                                barrelOffsets.Add(barrelOffset1);
                            }
                            else
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid position `{barrel_1}`");
                            }
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid position `{barrel_1}`");
                        }
                    }
                    else if (barrel_1.Length > 0)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid position `{barrel_1}`");
                    }

                    if (barrel_2.Length >= 7 && barrel_2.StartsWith('(') && barrel_2.EndsWith(')'))
                    {
                        var posData = barrel_2[1..^1].Replace(" ", "").Split(",");

                        if (posData.Length == 3)
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;

                            if (float.TryParse(posData[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(posData[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out y) && float.TryParse(posData[2], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out z))
                            {
                                barrelOffset2 = new Vector3(x, y, z);
                                barrelOffsets.Add(barrelOffset2);
                            }
                            else
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_2}`");
                            }
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_2}`");
                        }
                    }
                    else if (barrel_2.Length > 0)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_2}`");
                    }

                    if (barrel_3.Length >= 7 && barrel_3.StartsWith('(') && barrel_3.EndsWith(')'))
                    {
                        var posData = barrel_3[1..^1].Replace(" ", "").Split(",");

                        if (posData.Length == 3)
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;

                            if (float.TryParse(posData[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(posData[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out y) && float.TryParse(posData[2], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out z))
                            {
                                barrelOffset3 = new Vector3(x, y, z);
                                barrelOffsets.Add(barrelOffset3);
                            }
                            else
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_3}`");
                            }
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_3}`");
                        }
                    }
                    else if (barrel_3.Length > 0)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_3}`");
                    }

                    if (barrel_4.Length >= 7 && barrel_4.StartsWith('(') && barrel_4.EndsWith(')'))
                    {
                        var posData = barrel_4[1..^1].Replace(" ", "").Split(",");

                        if (posData.Length == 3)
                        {
                            float x = 0;
                            float y = 0;
                            float z = 0;

                            if (float.TryParse(posData[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(posData[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out y) && float.TryParse(posData[2], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out z))
                            {
                                barrelOffset4 = new Vector3(x, y, z);
                                barrelOffsets.Add(barrelOffset4);
                            }
                            else
                            {
                                Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_4}`");
                            }
                        }
                        else
                        {
                            Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_4}`");
                        }
                    }
                    else if (barrel_4.Length > 0)
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"BarrelOffsetOverrides: [{name}] Invalid offset `{barrel_4}`");
                    }

                    CylinderOverrides.Add(name, this);
                }
            }

            public static void LoadCylinderOverrideData()
            {
                List<CylinderOverride> list = new List<CylinderOverride>();
                string? text = Serializer.CSV.GetTextFromFile(Path.Combine(Config._BasePath, "DebugSaves", "cylinderOverrides.csv"));

                if (text == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Failed to load `cylinderOverrides.csv`.");
                    return;
                }

                Serializer.CSV.Read<List<CylinderOverride>, CylinderOverride>(text, list, true, true);
            }

            public CylinderVisual(int id)
            {
                this.id = id;
                diameter = 1f;
                unit = "meter";
                type = 2;

                root = new("Cylinder_Visual");
                root.transform.SetParent(ConstructorM.models);
                root.transform.localPosition = Vector3.zero;

                ui = GameObject.Instantiate(CylinderVisualTemplate);
                MakeUI();

                AddToScene();
            }

            public CylinderVisual(Store from)
            {
                id = from.id;
                type = from.type;
                diameter = from.diameter;

                unit = from.unit;
                binding = from.binding;

                root = new("Cylinder_Visual");
                root.transform.SetParent(ConstructorM.models);
                root.transform.localPosition = Vector3.zero;

                ui = GameObject.Instantiate(CylinderVisualTemplate);
                MakeUI();

                AddToScene();
            }

            public CylinderVisual(int id, CylinderVisual copy)
            {
                this.id = id;
                type = copy.type;
                diameter = copy.diameter;

                unit = copy.unit;
                binding = copy.binding;

                root = new("Cylinder_Visual");
                root.transform.SetParent(ConstructorM.models);
                root.transform.localPosition = Vector3.zero;

                ui = GameObject.Instantiate(CylinderVisualTemplate);
                MakeUI();

                AddToScene();
            }

            private void MakeUI()
            {
                ui.SetParent(VisualsList, false);
                ui.SetActive(true);

                var CVTTargetListInput = new TAFUI.TAF_InputField(ui.GetChild("Cylider_Target_Input"), 25);
                CVTTargetListInput.SetOnSubmit(new System.Action<string>((string value) => {
                    Melon<TweaksAndFixes>.Logger.Msg($"Entered: `{value}`");

                    binding = value;

                    AddToScene();

                    CVTTargetListInput.SetText(value);
                }));
                CVTTargetListInput.SetText(binding);
                
                var CVTDiameterInput = new TAFUI.TAF_InputField(ui.GetChild("Cylinder_Diameter_Input"), 8);
                CVTDiameterInput.SetOnSubmit(new System.Action<string>((string value) => {

                    bool inch = unit == "inch";

                    if (!float.TryParse(value, out float d))
                    {
                        Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid diameter `{value}`!");
                        return;
                    }

                    diameter = d;

                    AddToScene();

                    CVTDiameterInput.StaticText.text = value;
                }));
                CVTDiameterInput.SetText($"{diameter}");

                var CVTUnitInput = new TAFUI.TAF_InputField(ui.GetChild("Cylinder_Unit_Input"), 8);
                CVTUnitInput.SetOnSubmit(new System.Action<string>((string value) => {

                    unit = value;

                    AddToScene();

                    CVTUnitInput.StaticText.text = value;
                }));
                CVTUnitInput.SetText($"{unit}");

                var deleteButton = new TAFUI.TAF_Button(ui.GetChild("Cylinder_Delete_Button"));
                deleteButton.SetOnClick(new Action(() => {
                    Delete();
                }));

                var copyButton = new TAFUI.TAF_Button(ui.GetChild("Cylinder_Copy_Button"));
                copyButton.SetOnClick(new Action(() => {
                    makeCopy = true;
                }));
            }

            public override void Delete()
            {
                foreach (var child in root.GetChildren())
                {
                    child.TryDestroy();
                }

                root.TryDestroy();

                ui.TryDestroy();

                isDestroyed = true;
                dirtyChanged = true;
            }

            public override void AddToScene()
            {
                dirtyChanged = true;
            }

            public float GetDiameter()
            {
                if (unit == "meter")
                    return diameter;
                else if (unit == "inch")
                    return diameter / 39.37008f;
                else if (unit == "feet")
                    return diameter / 3.28084f;
                else
                    return diameter;
            }

            public override void Update(List<Visual> Visuals)
            {
                if (dirtyChanged)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Dirty changed, deleting cylinders!");

                    foreach (var child in root.GetChildren())
                    {
                        child.TryDestroy(true);
                    }

                    VisualToCylider.Clear();
                }

                foreach (var visual in Visuals)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Checking: {visual.id} : {type}");

                    if (visual.type != 1) continue; // if (type == Type.CYLINDER) continue;

                    // Melon<TweaksAndFixes>.Logger.Msg($"Parsing: {visual.id}");

                    foreach (var obj in visual.root.GetChildren())
                    {
                        if (VisualToCylider.ContainsKey(obj))
                        {
                            continue;
                        }

                        // Create

                        if (binding == "turret")
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"Add turret: {obj.name} at {obj.transform.position}");

                            GameObject turretCylider = GameObject.Instantiate(TAFGlobalCache.cyliderVisualizer);
                            turretCylider.SetParent(root);
                            turretCylider.transform.position = obj.transform.position;
                            turretCylider.transform.eulerAngles = new Vector3(0, 0, 0);

                            float maxHeight = 0;

                            foreach (var child in obj.GetChildren())
                            {
                                if (child.name.Contains("barrel")) continue;

                                if (child.GetComponent<MeshRenderer>() == null) continue;

                                var renderer = child.GetComponent<MeshRenderer>();

                                if (!renderer) continue;

                                var bounds = renderer.bounds;

                                float height = bounds.extents.y + bounds.center.y;

                                if (height > maxHeight)
                                {
                                    maxHeight = height;
                                }
                            }

                            // var bounds = obj.GetComponent<MeshRenderer>().bounds;
                            turretCylider.transform.SetScale(GetDiameter(), maxHeight + 0.1f, GetDiameter());//bounds.extents.x, 10f, bounds.extents.x);

                            VisualToCylider.Add(obj, null);
                        }
                        
                        else if (binding == "barrel" && CylinderOverrides.ContainsKey(obj.name))
                        {
                            foreach (var offset in CylinderOverrides[obj.name].barrelOffsets)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"Add barrel: {obj.transform.position + offset}");
                                
                                GameObject barrelCylider = GameObject.Instantiate(TAFGlobalCache.cyliderVisualizer);
                                barrelCylider.SetParent(root);
                                barrelCylider.transform.position = obj.transform.position + offset * obj.transform.localScale.x;
                                barrelCylider.transform.eulerAngles = new Vector3(90, 0, 0);
                                barrelCylider.transform.SetScale(
                                    GetDiameter(),
                                    CylinderOverrides[obj.name].barrel_length * obj.transform.localScale.x + 0.1f,
                                    GetDiameter()
                                );
                            }

                            VisualToCylider.Add(obj, null);
                        }
                        
                        else if (binding == "barrel")
                        {
                            List<GameObject> barrels = new();

                            ModUtils.FindChildrenContains(obj, "barrel_", barrels);

                            foreach (var barrel in barrels)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"Add barrel: {barrel.name} at {obj.transform.position}");

                                if (barrel.GetComponent<MeshRenderer>() == null) continue;

                                GameObject barrelCylider = GameObject.Instantiate(TAFGlobalCache.cyliderVisualizer);
                                barrelCylider.SetParent(root);
                                var bounds = barrel.GetComponent<MeshRenderer>().bounds;
                                barrelCylider.transform.position = bounds.center;
                                barrelCylider.transform.eulerAngles = new Vector3(90, 0, 0);
                                barrelCylider.transform.SetScale(GetDiameter(), bounds.extents.z + 0.1f, GetDiameter());
                            }

                            VisualToCylider.Add(obj, null);
                        }
                    }
                }
            }
        }

        public static GameObject container;
        public static GameObject floor;
        public static GameObject models;

        public static GameObject PartModelVisualTemplate;
        public static GameObject CylinderVisualTemplate;
        public static GameObject VisualsList;

        public static Material gridMat;

        public static int makeNew = 0;

        public static string saveFile = "";
        public static Vector2 gridSize = new(10_000, 10_000);

        public static bool active = false;
        public static bool dirtyChanged = false;

        public static readonly List<Visual> Visuals = new();

        public static TAFUI.TAF_InputField gridSizeInput;

        public static List<PartModelData> GetMatchingModels(string nations, string types, string caliber)
        {
            List<PartModelData> models = new();

            foreach (var model in G.GameData.partModels)
            {
                if (!model.Value.subName.Contains("gun")) continue;

                // gun_{caliber}_x{barrels}
                if (caliber != String.Empty && model.Value.subName.Split("_")[1] != caliber) continue;

                if (nations != String.Empty && model.Value.countries != String.Empty)
                {
                    var nationsList = nations.Replace(" ", "").Split(",");

                    bool found = false;

                    foreach (var option in model.Value.countries.Replace(" ", "").Split(","))
                    {
                        if (nationsList.Contains(option))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found) continue;
                }

                if (types != String.Empty && model.Value.shipTypes != String.Empty)
                {
                    var typesList = types.Replace(" ", "").Split(",");

                    bool found = false;

                    foreach (var option in model.Value.shipTypes.Replace(" ", "").Split(","))
                    {
                        if (typesList.Contains(option))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found) continue;
                }

                Melon<TweaksAndFixes>.Logger.Msg($"Found: {model.key}");

                models.Add(model.Value);
            }

            return models;
        }

        public static void Init()
        {
            active = true;

            G.ui.GetChild("Constructor").SetActive(false);
            G.ui.GetChild("Common").SetActive(false);
            G.cam.Bloom(false);
            G.cam.DepthOfField(false);
            G.cam.fogEndDistanceOrig = new Il2CppSystem.Nullable<float>(float.MaxValue);
            G.cam.fogStartDistanceOrig = new Il2CppSystem.Nullable<float>(float.MaxValue);

            RenderSettings.fog = false;

            ShipM.GetActiveShip().gameObject.SetActive(false);

            var scene = SceneManager.GetActiveScene();
            var sceneObjs = scene.GetRootGameObjects();
            GameObject consructorRoot = null;

            foreach (var obj in sceneObjs)
            {
                if (obj == null) continue;

                if (obj.name == "LevelConstructor")
                {
                    obj.GetChild("Probes").SetActive(false);
                    obj.GetChild("MoveWithShip").SetActive(false);
                    obj.GetChild("Dock").SetActive(false);
                    obj.GetChild("Other").SetActive(false);

                    consructorRoot = obj;
                }
            }

            if (!consructorRoot) return;

            CylinderVisual.LoadCylinderOverrideData();

            container = new GameObject("Part_Debug");
            container.SetParent(consructorRoot);
            container.transform.position = Vector3.zero;

            floor = new GameObject("Floor");
            floor.SetParent(container);
            floor.transform.position = Vector3.zero;

            models = new GameObject("Models");
            models.SetParent(container);
            models.transform.position = Vector3.zero;

            TAFGlobalCache.Init();

            Material cubeMat = TAFGlobalCache.cubeVisualizer.GetComponent<MeshRenderer>().material;
            
            gridMat = new(Shader.Find("Standard"));
            gridMat.color = new Color(1, 1, 1, 1);

            var rawData = File.ReadAllBytes(Path.Combine(Config._BasePath, "TAFData", "BW_Grid.png"));
            var tex = new Texture2D(512, 512, TextureFormat.DXT1, true);
            if (!ImageConversion.LoadImage(tex, rawData))
            {
                Melon<TweaksAndFixes>.Logger.Error("Failed to load grid image file");
            }

            tex.wrapMode = TextureWrapMode.Repeat;

            gridMat.SetTexture("_MainTex", tex);
            gridMat.SetFloat("_Glossiness", 0);
            gridMat.SetFloat("_SpecularHighlights", 0);
            gridMat.SetFloat("_GlossyReflections", 0);

            MakeGridCube(new(gridSize.x, -1, gridSize.y), new(0,-0.5f,0));

            MakeUI();
        }

        public static void Update()
        {
            if (!active) return;

            if(G.ui.GetChild("Constructor").active) G.ui.GetChild("Constructor").SetActive(false);
            if(G.ui.GetChild("Common").active) G.ui.GetChild("Common").SetActive(false);

            if (makeNew != 0)
            {
                switch (makeNew)
                {
                    case 1:
                        Visuals.Add(new PartModelVisual(Visuals.Count));
                        break;
                    
                    case 2:
                        Visuals.Add(new CylinderVisual(Visuals.Count));
                        break;

                    default:
                        break;
                }

                makeNew = 0;
            }

            for (int i = Visuals.Count - 1; i >= 0; i--)
            {
                Visuals[i].Update(Visuals);

                if (Visuals[i].isDestroyed)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Destroying");
                    Visuals.Remove(Visuals[i]);
                    continue;
                }

                if (Visuals[i].makeCopy)
                {
                    Melon<TweaksAndFixes>.Logger.Msg("Making copy");

                    switch (Visuals[i].type)
                    {
                        case 1:
                            Visuals.Add(new PartModelVisual(Visuals.Count, (PartModelVisual)Visuals[i]));
                            break;

                        case 2:
                            Visuals.Add(new CylinderVisual(Visuals.Count, (CylinderVisual)Visuals[i]));
                            break;

                        default:
                            break;
                    }

                    Visuals[i].makeCopy = false;
                }
            }

            dirtyChanged = false;
        }

        public static void ReloadModels()
        {
            foreach (var visual in Visuals)
            {
                visual.AddToScene();
            }
        }

        public static void FromStore(Store from)
        {
            saveFile = from.saveFile;
            var newGridSize = from.gridSize.from();
            MakeGridCube(new(newGridSize.x, -1, newGridSize.y), new(0,-0.5f,0));
            gridSizeInput.SetText($"{gridSize.x},{gridSize.y}");

            foreach (var visual in Visuals) 
            {
                visual.Delete();
            }

            Visuals.Clear();

            foreach (var visual in from.PartModelVisuals)
            {
                Visuals.Add(new PartModelVisual(visual));
            }

            foreach (var visual in from.CylinderVisuals)
            {
                Visuals.Add(new CylinderVisual(visual));
            }
        }

        public static void MakeUI()
        {
            // ../UIMain/

            GameObject ui = G.ui.gameObject;

            // ../UIMain/Debug_Map_UI/

            GameObject sideWindow = GameObject.Instantiate(ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/SaveWindow"));
            sideWindow.name = "Debug_Map_UI";
            sideWindow.SetActive(true);
            sideWindow.transform.parent = ui.transform;
            sideWindow.transform.SetScale(1, 1, 1);
            sideWindow.transform.localPosition = Vector3.zero;
            sideWindow.TryDestroyComponent<Image>();
            sideWindow.TryDestroyComponent<UISaveLoadWindow>();
            RectTransform sideWindowTransform = sideWindow.GetComponent<RectTransform>();
            sideWindowTransform.offsetMin = Vector3.zero;
            sideWindowTransform.offsetMax = Vector3.zero;

            // ../UIMain/Debug_Map_UI/Root/

            GameObject root = sideWindow.GetChild("Root");
            RectTransform rootTransform = root.GetComponent<RectTransform>();
            rootTransform.offsetMax = new Vector2(675, 350);
            rootTransform.offsetMin = new Vector2(300, -350);

            // ../UIMain/Debug_Map_UI/Root/Buttons/

            GameObject buttons = root.GetChild("Buttons");
            HorizontalLayoutGroup buttonsLayout = buttons.GetComponent<HorizontalLayoutGroup>();
            buttonsLayout.childForceExpandWidth = true;
            buttonsLayout.childScaleWidth = true;
            buttonsLayout.spacing = 10;
            buttons.GetChild("Load").SetActive(false);
            buttons.GetChild("Delete").SetActive(false);
            buttons.GetChild("Close").SetActive(false);
            RectTransform buttonsTransform = buttons.GetComponent<RectTransform>();
            buttonsTransform.offsetMax = new Vector2(360, -650);
            buttonsTransform.offsetMin = new Vector2(15, -685);

            // Global/Ui/UiMain/Popup/List Selection Popup

            GameObject createMenu = GameObject.Instantiate(ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/List Selection Popup"));
            createMenu.name = "Create_Menu";
            createMenu.SetActive(false);
            createMenu.transform.parent = sideWindow.transform;
            createMenu.transform.SetScale(1, 1, 1);
            createMenu.transform.localPosition = Vector3.zero;
            createMenu.TryDestroyComponent<Image>();
            createMenu.TryDestroyComponent<ListSelectionPopupUI>();

            GameObject createMenuRoot = createMenu.GetChild("Root");

            createMenuRoot.GetChild("Buttons").GetChild("Select").SetActive(false);

            GameObject createMenuButtonCancel = createMenuRoot.GetChild("Buttons").GetChild("Cancel");
            Button createMenuButtonCancelComp = createMenuButtonCancel.GetComponent<Button>();
            createMenuButtonCancelComp.onClick.RemoveAllListeners();
            createMenuButtonCancelComp.onClick.AddListener(new Action(() => {
                createMenu.SetActive(false);
            }));

            GameObject CreateMenuScrollView = ModUtils.GetChildAtPath("Scroll/Viewport/Content", createMenuRoot);

            GameObject CreateMenuItemTemplate = CreateMenuScrollView.GetChild("Template");
            CreateMenuItemTemplate.TryDestroyComponent<SelectionPopup_elementUI>();
            CreateMenuItemTemplate.GetChild("FavoritePort").TryDestroy(true);

            GameObject CreateMenuItemPartModel = GameObject.Instantiate(CreateMenuItemTemplate);
            CreateMenuItemPartModel.SetParent(CreateMenuScrollView);
            CreateMenuItemPartModel.SetActive(true);
            CreateMenuItemPartModel.transform.SetScale(1, 1, 1);
            GameObject CreateMenuItemPartModelText = CreateMenuItemPartModel.GetChild("Text (TMP)");
            TMP_Text CreateMenuItemPartModelTextComp = CreateMenuItemPartModelText.GetComponent<TMP_Text>();
            CreateMenuItemPartModelTextComp.text = "Part Model Visual";
            Button CreateMenuItemPartModelButton = CreateMenuItemPartModel.GetComponent<Button>();
            CreateMenuItemPartModelButton.onClick.RemoveAllListeners();
            CreateMenuItemPartModelButton.onClick.AddListener(new Action(() => {
                makeNew = 1;
                createMenu.SetActive(false);
            }));

            GameObject CreateMenuItemCylinder = GameObject.Instantiate(CreateMenuItemTemplate);
            CreateMenuItemCylinder.SetParent(CreateMenuScrollView);
            CreateMenuItemCylinder.SetActive(true);
            CreateMenuItemCylinder.transform.SetScale(1, 1, 1);
            GameObject CreateMenuItemCylinderText = CreateMenuItemCylinder.GetChild("Text (TMP)");
            TMP_Text CreateMenuItemCylinderComp = CreateMenuItemCylinderText.GetComponent<TMP_Text>();
            CreateMenuItemCylinderComp.text = "Cylinder Visual";
            Button CreateMenuItemCylinderButton = CreateMenuItemCylinder.GetComponent<Button>();
            CreateMenuItemCylinderButton.onClick.RemoveAllListeners();
            CreateMenuItemCylinderButton.onClick.AddListener(new Action(() => {
                makeNew = 2;
                createMenu.SetActive(false);
            }));

            var buttonsCreateButton = new TAFUI.TAF_Button(
                buttons, "Create_Button", "Create", Vector3.one, Vector3.one
            );
            buttonsCreateButton.root.transform.SetParent(buttonsLayout.transform);
            buttonsCreateButton.SetOnClick(new Action(() => {
                createMenu.SetActive(true);
            }));

            var buttonsHideButton = new TAFUI.TAF_Button(
                buttons, "Hide_Button", "Hide UI", Vector3.one, Vector3.one
            );
            buttonsHideButton.root.transform.SetParent(buttonsLayout.transform);

            var buttonsShowButton = new TAFUI.TAF_Button(
                sideWindow, "Show_Button", "Show", new Vector2(1350, -684), new Vector2(1170, -720)
            );
            buttonsShowButton.root.SetActive(false);

            buttonsHideButton.SetOnClick(new Action(() => {
                root.SetActive(false);
                buttonsShowButton.root.SetActive(true);
            }));

            buttonsShowButton.SetOnClick(new Action(() => {
                root.SetActive(true);
                buttonsShowButton.root.SetActive(false);
            }));

            // ../UIMain/Debug_Map_UI/Root/Header/

            root.GetChild("Header").SetActive(false);

            // ../UIMain/Debug_Map_UI/Root/Scroll View/

            GameObject scrollView = root.GetChild("Scroll View");
            RectTransform scrollViewTransform = scrollView.GetComponent<RectTransform>();
            scrollViewTransform.offsetMax = new Vector2(365, -200);
            scrollViewTransform.offsetMin = new Vector2(10, -650);

            GameObject scrollViewScrollBar = scrollView.GetChild("Scrollbar Vertical");
            RectTransform scrollViewScrollBarTransform = scrollViewScrollBar.GetComponent<RectTransform>();
            scrollViewScrollBarTransform.offsetMax = new(6, 0);
            scrollViewScrollBarTransform.offsetMin = new(-3, 17);

            MakePartModelVisualUI(scrollView);

            MakeCylinderVisualUI(scrollView);

            // ../UIMain/Debug_Map_UI/Root/Grid_Size_Input/

            gridSizeInput = new TAFUI.TAF_InputField(
                root, "Grid_Size_Input", new Vector2(350, -20), new Vector2(200, -40),
                "100000,100000", "x,y"
            );
            gridSizeInput.SetOnSubmit(new Action<string>((string value) => {
                if (!int.TryParse(value.Split(",")[0], out int x))
                {
                    gridSizeInput.EditField.text = gridSizeInput.StaticText.text;
                    return;
                }

                if (!int.TryParse(value.Split(",")[1], out int y))
                {
                    gridSizeInput.EditField.text = gridSizeInput.StaticText.text;
                    return;
                }

                Melon<TweaksAndFixes>.Logger.Msg($"  Parsed: `{x}, {y}`");
                gridSizeInput.StaticText.text = value;

                MakeGridCube(new(x,-1,y), new(0,-0.5f,0));
            }));

            // ../UIMain/Debug_Map_UI/Root/Grid_Size_Header/

            var gridSizeHeader = new TAFUI.TAF_Text(
                root, "Grid_Size_Header", "Grid Size", new Vector2(150, -20), new Vector2(70, -40)
            );

            // ../UIMain/Debug_Map_UI/Root/Save_Button/

            var saveButton = new TAFUI.TAF_Button(
                root, "Save_Button", "Save", new Vector2(120, -160), new Vector2(10, -195)
            );
            saveButton.SetOnClick(new Action(() => {
                if (saveFile == string.Empty)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error: No save file provided!");
                    return;
                }

                Store store = new(true);

                string saveJson = JsonSerializer.Serialize(store);

                if (!Directory.Exists($"{Config._BasePath}\\DebugSaves\\"))
                {
                    Directory.CreateDirectory($"{Config._BasePath}\\DebugSaves\\");
                }

                File.WriteAllText($"{Config._BasePath}\\DebugSaves\\{saveFile}", saveJson);

                Melon<TweaksAndFixes>.Logger.Msg($"Saved data to {$"{Config._BasePath}\\DebugSaves\\{saveFile}"}");
            }));

            // ../UIMain/Debug_Map_UI/Root/Load_Button/

            var loadButton = new TAFUI.TAF_Button(
                root, "Load_Button", "Load", new Vector2(240, -160), new Vector2(130, -195)
            );
            loadButton.SetOnClick(new Action(() => {
                if (!Directory.Exists($"{Config._BasePath}\\DebugSaves\\"))
                {
                    Directory.CreateDirectory($"{Config._BasePath}\\DebugSaves\\");
                }

                if (!File.Exists($"{Config._BasePath}\\DebugSaves\\{saveFile}"))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error: No file at path `{Config._BasePath}\\DebugSaves\\{saveFile}`!");
                    return;
                }

                Store? store = null;

                try
                {
                    string saveJson = File.ReadAllText($"{Config._BasePath}\\DebugSaves\\{saveFile}");
                    store = JsonSerializer.Deserialize<Store>(saveJson);
                }
                catch
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error: Could not read file!");
                    return;
                }

                if (store == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid save format!");
                    return;
                }

                FromStore(store);

                Melon<TweaksAndFixes>.Logger.Msg($"Loaded data from {$"{Config._BasePath}\\DebugSaves\\{saveFile}"}");
            }));

            // ../UIMain/Debug_Map_UI/Root/Save_Name_Input/

            var saveNameInput = new TAFUI.TAF_InputField(
                root, "Save_Name_Input", new Vector2(360, -160), new Vector2(250, -195),
                "", "filename.json"
            );
            saveNameInput.SetOnSubmit(new Action<string>((string value) => {
                Melon<TweaksAndFixes>.Logger.Msg($"Entered `{value}`");

                if (!value.EndsWith(".json"))
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Error: Invalid save name: `{value}`. Must end with `.json`!");
                    return;
                }

                saveFile = value;
                saveNameInput.SetText(value);
            }));

        }

        public static void MakePartModelVisualUI(GameObject scrollView)
        {
            // ../UIMain/Debug_Map_UI/Root/Scroll View/Viewport/List

            VisualsList = scrollView.GetChild("Viewport").GetChild("List");

            // ../UIMain/Debug_Map_UI/Root/Scroll View/Viewport/List/Template

            GameObject refference = scrollView.GetChild("Viewport").GetChild("List").GetChildren()[0];
            PartModelVisualTemplate = new GameObject("Template");
            PartModelVisualTemplate.SetActive(false);
            PartModelVisualTemplate.SetParent(VisualsList, false);
            PartModelVisualTemplate.transform.localPosition = Vector3.zero;
            PartModelVisualTemplate.transform.SetScale(1, 1, 1);
            RectTransform PartModelVisualTemplateTransform = PartModelVisualTemplate.AddComponent<RectTransform>();
            PartModelVisualTemplateTransform.offsetMax = new Vector2(635, 0);
            PartModelVisualTemplateTransform.offsetMin = new Vector2(-280, -200);
            GameObject PartModelVisualTemplateBG = GameObject.Instantiate(refference.GetChild("Highlight"));
            PartModelVisualTemplateBG.SetParent(PartModelVisualTemplate, false);
            PartModelVisualTemplateBG.name = "BG";
            PartModelVisualTemplateBG.SetActive(true);

            // foreach (var child in scrollView.GetChild("Viewport").GetChild("List").GetChildren())
            // {
            //     if (child == CyliderVisualTemplate) continue;
            //     child.TryDestroy();
            // }



            Vector2 origin = new(300, -10);
            Vector2 lf = new(0, -25);
            int lCnt = 0;
            Vector2 tab = new((635.0f - 300.0f) / 4, 0);

            var MDETemplateModelListHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_List_Header", "Model List", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var MDETemplateModelListInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_List_Input", origin + tab * 3.75f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "gun_6_x2, ...", "gun_6_x2, ...", true, 25
            );
            lCnt++;

            var MDETemplateModelPositionHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_Position_Header", "Position", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var MDETemplateModelPositionInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_Position_Input", origin + tab * 1.9f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "0,0,0", "x,y,z", true, 8
            );

            var MDETemplateModelSpacingHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_Spacing_Header", "Spacing", origin + tab * 3 + lf * lCnt, origin + tab * 2.1f + lf * (lCnt + 1)
            );
            var MDETemplateModelSpacingInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_Spacing_Input", origin + tab * 4 + lf * lCnt, origin + tab * 2.5f + lf * (lCnt + 1),
                "0,0", "x,y", true, 8
            );
            lCnt++;

            var MDETemplateModelRotationHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_Rotation_Header", "Rotation", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var MDETemplateModelRotationInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_Rotation_Input", origin + tab * 1.9f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "0,0,0", "p,y,r", true, 8
            );
            lCnt++;

            var MDETemplateModelIndexesHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_Indexes_Header", "Model Idx", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var MDETemplateModelIndexesInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_Indexes_Input", origin + tab * 1.9f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "all", "1,2,...", true, 8
            );

            var MDETemplateMarkIndexesHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Mark_Indexes_Header", "Mark Idx", origin + tab * 3 + lf * lCnt, origin + tab * 2.1f + lf * (lCnt + 1)
            );
            var MDETemplateMarkIndexesInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Mark_Indexes_Input", origin + tab * 4 + lf * lCnt, origin + tab * 2.5f + lf * (lCnt + 1),
                "1,2,3,4,5", "1,2,...", true, 8
            );
            lCnt++;

            var MDETemplateModelDiameterHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_Indexes_Header", "Diameter", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var MDETemplateModelDiameterInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_Diameter_Input", origin + tab * 1.9f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "0", "0,0.1,...", true, 8
            );

            var MDETemplateModelLengthHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_Length_Header", "Length", origin + tab * 3 + lf * lCnt, origin + tab * 2.1f + lf * (lCnt + 1)
            );
            var MDETemplateModelLengthInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_Length_Input", origin + tab * 4 + lf * lCnt, origin + tab * 2.5f + lf * (lCnt + 1),
                "0", "1,2,...", true, 8
            );
            lCnt++;

            var MDETemplateModelXAxisHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_XAxis_Header", "X-Axis", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var MDETemplateModelXAxisInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_XAxis_Input", origin + tab * 1.9f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "mark", "mark", true, 8
            );

            var MDETemplateModelYAxisHeader = new TAFUI.TAF_Text(
                PartModelVisualTemplate, "Model_YAxis_Header", "Y-Axis", origin + tab * 3 + lf * lCnt, origin + tab * 2.1f + lf * (lCnt + 1)
            );
            var MDETemplateModelYAxisInput = new TAFUI.TAF_InputField(
                PartModelVisualTemplate, "Model_YAxis_Input", origin + tab * 4 + lf * lCnt, origin + tab * 2.5f + lf * (lCnt + 1),
                "model", "model", true, 8
            );
            lCnt++;

            var MDETemplateDeleteButton = new TAFUI.TAF_Button(
                PartModelVisualTemplate, "Model_Delete_Button", "Delete", origin + tab * 1.8f + lf * lCnt, origin + tab * -0.2f + lf * (lCnt) + lf * 1.25f
            );

            var MDETemplateCopyButton = new TAFUI.TAF_Button(
                PartModelVisualTemplate, "Model_Copy_Button", "Copy", origin + tab * 4f + lf * lCnt, origin + tab * 2f + lf * (lCnt) + lf * 1.25f
            );
            lCnt++;
        }

        public static void MakeCylinderVisualUI(GameObject scrollView)
        {

            // ../UIMain/Debug_Map_UI/Root/Scroll View/Viewport/List

            VisualsList = scrollView.GetChild("Viewport").GetChild("List");

            // ../UIMain/Debug_Map_UI/Root/Scroll View/Viewport/List/Template

            GameObject refference = scrollView.GetChild("Viewport").GetChild("List").GetChildren()[0];
            CylinderVisualTemplate = new GameObject("Template");
            CylinderVisualTemplate.SetActive(false);
            CylinderVisualTemplate.SetParent(VisualsList, false);
            CylinderVisualTemplate.transform.localPosition = Vector3.zero;
            CylinderVisualTemplate.transform.SetScale(1, 1, 1);
            RectTransform CyliderVisualTemplateTransform = CylinderVisualTemplate.AddComponent<RectTransform>();
            CyliderVisualTemplateTransform.offsetMax = new Vector2(635, 0);
            CyliderVisualTemplateTransform.offsetMin = new Vector2(-280, -100);
            GameObject CyliderVisualTemplateBG = GameObject.Instantiate(refference.GetChild("Highlight"));
            CyliderVisualTemplateBG.SetParent(CylinderVisualTemplate, false);
            CyliderVisualTemplateBG.name = "BG";
            CyliderVisualTemplateBG.SetActive(true);

            foreach (var child in scrollView.GetChild("Viewport").GetChild("List").GetChildren())
            {
                if (child.name.Contains("Template")) continue;
                child.TryDestroy();
            }



            Vector2 origin = new(300, -10);
            Vector2 lf = new(0, -25);
            int lCnt = 0;
            Vector2 tab = new((635.0f - 300.0f) / 4, 0);

            var CVTTargetHeader = new TAFUI.TAF_Text(
                CylinderVisualTemplate, "Cylider_Target_Header", "Target", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var CVTTargetListInput = new TAFUI.TAF_InputField(
                CylinderVisualTemplate, "Cylider_Target_Input", origin + tab * 3.75f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "PartModels", "PartModels", true, 25
            );
            lCnt++;

            var CVTDiameterHeader = new TAFUI.TAF_Text(
                CylinderVisualTemplate, "Cylinder_Diameter_Header", "Diameter", origin + tab * 1 + lf * lCnt, origin + tab * 0 + lf * (lCnt + 1)
            );
            var CVTDiameterInput = new TAFUI.TAF_InputField(
                CylinderVisualTemplate, "Cylinder_Diameter_Input", origin + tab * 1.9f + lf * lCnt, origin + tab * 0.5f + lf * (lCnt + 1),
                "1", "2/5.25\"", true, 8
            );

            var CVTUnitHeader = new TAFUI.TAF_Text(
                CylinderVisualTemplate, "Cylinder_Unit_Header", "Unit", origin + tab * 3 + lf * lCnt, origin + tab * 2.1f + lf * (lCnt + 1)
            );
            var CVTUnitInput = new TAFUI.TAF_InputField(
                CylinderVisualTemplate, "Cylinder_Unit_Input", origin + tab * 4 + lf * lCnt, origin + tab * 2.5f + lf * (lCnt + 1),
                "meter", "meter/inch/feet", true, 8
            );
            lCnt++;

            var CVTDeleteButton = new TAFUI.TAF_Button(
                CylinderVisualTemplate, "Cylinder_Delete_Button", "Delete", origin + tab * 1.8f + lf * lCnt, origin + tab * -0.2f + lf * (lCnt) + lf * 1.25f
            );

            var CVTCopyButton = new TAFUI.TAF_Button(
                CylinderVisualTemplate, "Cylinder_Copy_Button", "Copy", origin + tab * 4f + lf * lCnt, origin + tab * 2f + lf * (lCnt) + lf * 1.25f
            );
            lCnt++;
        }

        public static GameObject AddModelToScene(GameObject parent, string name, Vector3 pos, Vector3 rotation, float scale)
        {
            var template = Resources.Load<GameObject>(name);

            if (template == null)
            {
                template = Resources.Load<GameObject>("default_part");

                Melon<TweaksAndFixes>.Logger.Error($"Error: PartModel `{name}` could not be found!");
            }

            var model = GameObject.Instantiate(ModUtils.FindDeepChild(template, "Visual"));
            model.name = name;
            model.transform.SetParent(parent);
            model.transform.localPosition = pos;
            model.transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
            model.transform.SetScale(scale, scale, scale);

            List<GameObject> stack = new() { model };

            foreach (var child in model.GetChildren())
            {
                stack.Add(child);
            }

            while (stack.Count > 0)
            {
                var child = stack.First();
                stack.Remove(child);

                if (child.name.StartsWith("LOD"))
                {
                    child.TryDestroy();
                }

                foreach (var subChild in child.GetChildren())
                {
                    stack.Add(subChild);
                }
            }

            Resources.UnloadAsset(template);

            return model;
        }

        public static GameObject MakeGridCube(Vector3 size, Vector3 position)
        {
            foreach (var child in floor.GetChildren()) child.TryDestroy();

            gridSize = new(size.x, size.z);

            Patch_Cam.overrideCamBounds = true;
            Patch_Cam.camBounds = size + new Vector3(500, 0,500);

            GameObject gridCube = GameObject.Instantiate(TAFGlobalCache.cubeVisualizer);
            gridCube.transform.SetParent(floor);
            gridCube.transform.localPosition = Vector3.zero;
            gridCube.transform.position = position;
            gridCube.transform.localScale = size;
            gridCube.GetComponent<MeshRenderer>().material = gridMat;

            gridMat.SetTextureScale("_MainTex", new(size.x / 2, size.z / 2));

            return gridCube;
        }
    }
}
