using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TweaksAndFixes.Modified
{
    internal class MainMenuM
    {
        public class VisualShip
        {
            public GameObject root;
            public ShipType shipType;
            public PartData hullData;
            public PlayerData nation;
            public List<List<MeshRenderer>> LODs = new();
            public float rollMax = 0;
            public float rollPeriod = -1;
            public float t = 0;
            public Vector2 size;

            public VisualShip(GameObject root, ShipType shipType, PartData hullData, PlayerData nation)
            {
                this.root = root;
                this.shipType = shipType;
                this.hullData = hullData;
                this.nation = nation;
            }

            public void Update()
            {
                var dt = Time.deltaTime;
                t = (t + dt) % rollPeriod;

                float rot = Mathf.Sin(t * 2 * Mathf.PI * (1.0f / rollPeriod)) * rollMax;

                root.transform.eulerAngles = new(
                    root.transform.eulerAngles.x,
                    root.transform.eulerAngles.y,
                    rot
                );
            }
        }

        public class CamLocation
        {
            public Vector3 pos;
            public Vector2 angle;
            public float distance;
            public PortLocation? require = null;

            public CamLocation(Vector3 pos, Vector2 angle, float distance, PortLocation? require)
            {
                this.pos = pos;
                this.angle = angle;
                this.distance = distance;
                this.require = require;
            }
        }

        public class PortLocation
        {
            public enum LocType
            {
                Tiny, Small, Medium, Large,
                LoadingDock, Anchorage,
                Path
            }

            public LocType type;
            public Vector3 pos1;
            public Vector3 pos2;
            public Vector2 maxSize;
            public float rot;
            public float skipChance = 0.1f;

            public PortLocation(LocType type, Vector3 pos, float rot, Vector2 maxSize, float skipChanmce = 0.1f)
            {
                this.type = type;
                this.pos1 = pos;
                this.rot = rot;
                this.maxSize = maxSize;
                this.skipChance = skipChanmce;
            }

            public PortLocation(LocType type, Vector3 pos1, Vector3 pos2, Vector2 maxSize, float skipChance = 0.1f)
            {
                this.type = type;
                this.pos1 = pos1;
                this.pos2 = pos2;
                this.maxSize = maxSize;
                this.skipChance = skipChance;
            }

            public bool Fits(VisualShip ship)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"Fits: {ship.root.name} | {maxSize} / {ship.size}");

                switch (type)
                {
                    case LocType.Tiny:
                    case LocType.Small:
                    case LocType.Medium:
                    case LocType.Large:
                    case LocType.LoadingDock:
                        if ((maxSize.x == -1 || maxSize.x >= ship.size.x)
                            && (maxSize.y == -1 || maxSize.y >= ship.size.y))
                            return true;
                        break;
                    default:
                        break;
                }

                return false;
            }

            public void Fill(VisualShip ship)
            {
                // Fixed pos
                if (type == LocType.Tiny
                    || type == LocType.Small
                    || type == LocType.Medium
                    || type == LocType.Large)
                {
                    // var c = ship.Clone(pos1, rot);
                    ship.root.transform.localPosition = pos1;
                    return;
                }

                ship.root.transform.localPosition = (pos1 + pos2) / 2;

                return;
            }
        }

        public static GameObject shipsContainer = new();
        public static List<VisualShip> ships = new();
        public static List<PortLocation> locations = new() {
            // "Wet" docks
            new(PortLocation.LocType.Tiny,      new Vector3(-250, 0, -27.5f),   0, new Vector2(17.5f, 165)),
            new(PortLocation.LocType.Small,     new Vector3(   0, 0, -10.0f),   0, new Vector2(35.0f, 180)),
            new(PortLocation.LocType.Medium,    new Vector3( 250, 0,  30.0f),   0, new Vector2(50.0f, 270)),
            new(PortLocation.LocType.Large,     new Vector3( 500, 0,  70.0f),   0, new Vector2(65.0f, 450)),

            // Loading Docks
            new(PortLocation.LocType.LoadingDock,   new Vector3(700, 0, 390), new Vector3(700, 0, -110), new Vector2(100, -1)),
        };
        public static List<CamLocation> cams = new() {
            new(new(-250, 0, -27.5f), new(20, 225), 180, locations[0]),
            new(new(   0, 0, -15.0f), new(20, 225), 200, locations[1]),
            new(new( 250, 0,  30.0f), new(20, 225), 220, locations[2]),
            new(new( 500, 0,  70.0f), new(20, 225), 250, locations[3]),
            new(new( 700, 0,  390  ), new(20, 225), 300, locations[4]),
        };

        private static VisualShip? MakeVisualShip(Ship.Store design, bool roll = true)
        {
            if (!G.GameData.parts.ContainsKey(design.hullName)
                || !G.GameData.shipTypes.ContainsKey(design.shipType)
                || !G.GameData.players.ContainsKey(design.playerName))
            {
                return null;
            }

            var root = new GameObject();
            root.name = $"{design.id}";
            root.transform.SetParent(shipsContainer, false);

            var hullData = G.GameData.parts[design.hullName];

            VisualShip container = new(
                root, G.GameData.shipTypes[design.shipType], hullData, G.GameData.players[design.playerName]
            );
            root.transform.localPosition = Vector3.zero;

            var hullTemplate = Util.ResourcesLoad<GameObject>(hullData.model);
            var hull = GameObject.Instantiate(hullTemplate, root.transform, false);

            hull.transform.SetScale(hullData.scale, hullData.scale, hullData.scale);
            
            // Melon<TweaksAndFixes>.Logger.Msg($"Section Bounds: {hullData.sectionsMin} / {hullData.sectionsMax}");

            // Melon<TweaksAndFixes>.Logger.Msg($"Tonnage: {design.tonnage}");

            var tonnageRatio =
                (design.tonnage - hullData.tonnageMin) / (hullData.tonnageMax - hullData.tonnageMin);

            // Melon<TweaksAndFixes>.Logger.Msg($"Tonnage ratio: {tonnageRatio}");

            int sectionsA = hullData.sectionsMin + (int)(tonnageRatio * (hullData.sectionsMax - hullData.sectionsMin) - 0.025f);

            int sections = Mathf.RoundToInt(Mathf.Lerp(
                hullData.sectionsMin - 0.499f, hullData.sectionsMax + 0.499f,
                Mathf.InverseLerp(hullData.tonnageMin, hullData.tonnageMax, design.tonnage)
            ));

            // Melon<TweaksAndFixes>.Logger.Msg($"Sections: {sectionsA} -> {sections}");

            // Obj, len
            List<Tuple<GameObject, Bounds>> sectionBounds = new();
            List<Tuple<GameObject, Bounds>> allSections = new();
            Tuple<GameObject, Bounds> bow = new(null, new());
            Tuple<GameObject, Bounds> stern = new(null, new());
            float totalLength = 0;
            Bounds shipBounds = new();
            bool firstBounds = true;

            var sectionsCont = hull.GetChild("Visual").GetChild("Sections");
            var children = sectionsCont.GetChildren();
            children.Reverse();

            foreach (var child in children)
            {
                var v = child.GetChild("Variation", true);

                if (v != null
                    && hullData.paramx.ContainsKey("var"))
                {
                    // Make into lists
                    List<GameObject> desired = new();
                    List<GameObject> active = new();

                    foreach (var varient in v.transform.GetChildren())
                    {
                        // Melon<TweaksAndFixes>.Logger.Msg($"Var: {varient.name}");
                        // 
                        // if (hullData.paramx["var"].Contains(varient.name))
                        //     Melon<TweaksAndFixes>.Logger.Msg($"  Desired");
                        // 
                        // if (varient.active)
                        //     Melon<TweaksAndFixes>.Logger.Msg($"  Active");

                        if (hullData.paramx["var"].Contains(varient.name))
                            desired.Add(varient);

                        if (varient.active)
                            active.Add(varient);
                    }

                    if (desired.Count > 0)
                    {
                        foreach (var a in active)
                            a.active = false;

                        foreach (var d in desired)
                            d.active = true;
                    }
                    else 
                    {
                        bool firstVar = true;

                        foreach (var varient in v.transform.GetChildren())
                        {
                            varient.active = firstVar;
                            firstVar = false;
                        }
                    }
                }
                else if (v != null)
                {
                    bool firstVar = true;

                    foreach (var varient in v.transform.GetChildren())
                    {
                        varient.active = firstVar;
                        firstVar = false;
                    }
                }

                bool isMid = child.name.Contains("Middle");

                Bounds total = new();
                bool first = true;
                child.transform.SetScale(1 + design.beam / 100, 1 + design.draught / 100, 1);

                Melon<TweaksAndFixes>.Logger.Msg($"{child.name}");
                ModUtils.ForeachRecursive(child, new((GameObject g) => {

                    if (g.activeSelf == false)
                        return -2;

                    if (g.name.ToLower().StartsWith("decor:"))
                        return -2;

                    if (!g.TryGetComponent<MeshRenderer>(out MeshRenderer r))
                        return 0;

                    var b = r.bounds;
                    // Util.InverseTransformBounds(
                    //     sectionsCont.transform, Util.TransformBounds(r.transform, r.bounds)
                    // );

                    b.center /= g.transform.lossyScale.z;
                    b.size /= g.transform.lossyScale.z;

                    // Melon<TweaksAndFixes>.Logger.Msg($"  {g.name}: {b.center.z} | {b.size.z}");

                    if (first)
                        total = b;
                    else
                        total.Encapsulate(b);

                    first = false;

                    // Ignore LODs
                    return -2;
                }));

                if (child.TryGetComponent<SectionInfo>(out SectionInfo info))
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"  SInfo: {info.sizeBackMod} | {info.sizeFrontMod}");
                    total.size += new Vector3(0, 0, info.sizeBackMod + info.sizeFrontMod);
                    total.center += new Vector3(0, 0, (info.sizeFrontMod - info.sizeBackMod) / 2);
                }

                if (firstBounds)
                    shipBounds = total;
                else
                    shipBounds.Encapsulate(total);

                firstBounds = false;

                sectionBounds.Add(new(child, total));

                if (isMid)
                    child.active = false;

                if (child.name.Contains("Bow"))
                {
                    totalLength += total.size.z;
                    bow = new(child, total);
                }

                if (child.name.Contains("Stern"))
                {
                    totalLength += total.size.z;
                    stern = new(child, total);
                }

                // Melon<TweaksAndFixes>.Logger.Msg($"  {isMid} | {total.center.z} | {total.size.z}");
            }

            for (int i = 0; i < sections; i++)
            {
                var entry = sectionBounds[^(i % (sectionBounds.Count - 2) + 2)];

                var newSec = GameObject.Instantiate(
                    entry.Item1,
                    sectionsCont.transform, false
                );

                totalLength += entry.Item2.size.z;
                allSections.Add(new(newSec, entry.Item2));

                // Melon<TweaksAndFixes>.Logger.Msg($"New: {i} | {entry.Item1.name}");
            }

            allSections.Add(new(stern.Item1, stern.Item2));
            allSections.Reverse();
            allSections.Add(new(bow.Item1, bow.Item2));

            container.size = new(shipBounds.size.x * hullData.scale, totalLength * hullData.scale);

            float curr = -totalLength / 2;

            // Melon<TweaksAndFixes>.Logger.Msg($"Tl: {totalLength} | Root: {curr}");

            foreach (var section in allSections)
            {
                GameObject child = section.Item1;
                Bounds secBounds = section.Item2;

                if (child.name == "Bow")
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Bow: {secBounds.center.z} | {secBounds.size.z} | {child.transform.localPosition.z}");
                    Util.SetLocalZ(
                        child.transform,
                        -secBounds.center.z + child.transform.localPosition.z + secBounds.size.z * 0.5f
                        + (curr)
                    );

                    // Util.SetLocalZ(
                    //     child.transform,
                    //     -(secBounds.center.z - child.transform.localPosition.z) + secBounds.size.z * 0.5f
                    //     + (curr)
                    // );
                }
                else if (child.name == "Stern")
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Stern: {secBounds.center.z} | {secBounds.size.z} | {child.transform.localPosition.z}");
                    Util.SetLocalZ(
                        child.transform,
                        -secBounds.center.z + child.transform.localPosition.z - secBounds.size.z * 0.5f
                        + (curr + secBounds.size.z)
                    );

                    // Util.SetLocalZ(
                    //     child.transform,
                    //     -(secBounds.center.z - child.transform.localPosition.z) - secBounds.size.z * 0.5f
                    //     + (curr + secBounds.size.z)
                    // );

                    curr += secBounds.size.z;
                }
                else
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"{child.name}: {secBounds.center.z} | {secBounds.size.z}");

                    child.active = true;

                    Util.SetLocalZ(
                        child.transform,
                        -secBounds.center.z + child.transform.localPosition.z - secBounds.size.z * 0.5f
                        + (curr + secBounds.size.z)
                    );
                    
                    curr += secBounds.size.z;
                }
            }

            List<GameObject> parts = new();

            foreach (var part in design.parts)
            {
                if (!G.GameData.parts.ContainsKey(part.name))
                {
                    continue;
                }

                var data = G.GameData.parts[part.name];
                string modelName = data.model;
                float modelScale = data.scale;

                if (data.isWeapon)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg($"Weapon: {part.name}");

                    PartModelData defaultWeaponModel = null;
                    PartModelData weaponModel = null;

                    foreach (var weaponData in G.GameData.partModels)
                    {
                        if (weaponData.Value.subName != part.name.Replace("_side", ""))
                            continue;

                        // Let defaults pass through
                        // Block all other countries
                        if (weaponData.Value.countriesx.Count != 0
                            && !weaponData.Value.countriesx.Contains(G.GameData.players[design.playerName]))
                            continue;

                        // Let defaults pass through
                        // Block all other types
                        if (weaponData.Value.shipTypesx.Count != 0
                            && !weaponData.Value.shipTypesx.Contains(G.GameData.shipTypes[design.shipType]))
                            continue;

                        if (defaultWeaponModel == null)
                            defaultWeaponModel = weaponData.Value;

                        weaponModel = weaponData.Value;

                        // Melon<TweaksAndFixes>.Logger.Msg($"  Model: {weaponData.Key}");
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg($"Chose: {weaponModel.name} | {ModUtils.toInt(data.GetCaliberInch())}");

                    int grade = 1;

                    if (data.isGun)
                        for (; grade < Config.MaxGunGrade; grade++)
                        {
                            var tech = Database.GetGunTech(ModUtils.toInt(data.GetCaliberInch()), grade);
                        
                            // Melon<TweaksAndFixes>.Logger.Msg($"Check tech: {grade} | {tech}");
                        
                            if (!design.techs.Contains(tech))
                                break;
                        }

                    if (data.isTorpedo)
                        for (; grade < Config.MaxTorpGrade; grade++)
                        {
                            var tech = Database.GetTorpGradeTech(grade);

                            if (!design.techs.Contains(tech))
                                break;
                        }

                    // Melon<TweaksAndFixes>.Logger.Msg($"Grade: {grade}");

                    grade--;

                    if (grade < 0 || grade >= weaponModel.models.Count)
                    {
                        Melon<TweaksAndFixes>.Logger.Msg($"Invalid grade: {grade}");
                        continue;
                    }

                    modelName = weaponModel.models[grade];
                    modelScale = weaponModel.scales[grade];

                    if (modelName.Length == 0)
                    {
                        modelName = defaultWeaponModel.models[grade];
                        modelScale = defaultWeaponModel.scales[grade];
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg($"Grade: {grade} | Name: {modelName}");
                }

                if (modelName == "")
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Invalid model: {modelName}");
                    continue;
                }

                var modelTemplate = Util.ResourcesLoad<GameObject>(modelName);
                var model = GameObject.Instantiate(modelTemplate, root.transform, false);

                model.transform.localPosition = part.position + new Vector3(0, 0, root.transform.position.z);
                model.transform.rotation = part.rotation;
                model.transform.SetScale(modelScale, modelScale, modelScale);

                parts.Add(model);
            }

            ModUtils.ForeachRecursive(root, new((GameObject g) => {
                if (g.name.StartsWith("Mount:"))
                {
                    g.TryDestroy(true);
                    return -2;
                }
                
                if (g.name.ToLower().StartsWith("decor:"))
                {
                    bool nearPart = false;
                    
                    foreach (var part in parts)
                    {
                        if (ModUtils.distance(g.transform.position, part.transform.position) < 20)
                        {
                            nearPart = true;
                            break;
                        }
                    }

                    g.active = !nearPart;
                    g.TryDestroyComponent<Decor>(true); // TODO: Refference distance from Decor
                    // Melon<TweaksAndFixes>.Logger.Msg($"Disable: {g.name}");
                }

                var lod0 = g.GetChild("LOD0", true);
                var lod1 = g.GetChild("LOD1", true);
                var lod2 = g.GetChild("LOD2", true);
                if (lod0 != null)
                {
                    container.LODs.Add(new() {
                        g.GetComponent<MeshRenderer>(),
                        lod0.GetComponent<MeshRenderer>(),
                        lod1.GetComponent<MeshRenderer>(),
                        lod2.GetComponent<MeshRenderer>()
                    });
                }

                if (g.name.StartsWith("LOD"))
                {
                    g.active = false;
                    // Melon<TweaksAndFixes>.Logger.Msg($"Disable: {g.name}");
                }

                g.TryDestroyComponent<LODGroup>(true);
                g.TryDestroyComponent<AutomaticLOD>(true);
                g.TryDestroyComponent<PartModel>(true);

                return 0;
            }));

            // root.transform.position = pos;
            // root.transform.eulerAngles = new(0, rot, 0);

            Util.ClearResourcesCache();

            return container;
        }

        private static bool inited = false;

        public static void ClearScene()
        {
            if (ships.Count == 1)
            {
                ships[0].root.TryDestroy(true);
                ships.Clear();
            }
        }

        public static void InitRandomScene()
        {
            /*
            Random Nation
            Filter positions by camera pos (optional)
            Grab 3-4 tiny, 2-3 small, 2 med, 1-2 large, 1-2 huge
            Loop over positions
             - Chance to skip
             - Randomly choose a ship that matches the size
             - Fill according to placement
            Update
             - Non-static: LODs
             - Static: Roll
             - Scene Elements:
               - Cranes
               - Lights?
               - Vehicles?
            */

            // Hide container in Ship.UnloadModel or EnterContstructor

            if (!inited)
            {
                shipsContainer.name = "DecorativeShips";
                shipsContainer.SetParent(
                    ModUtils.GetChildAtPath("Dock", Patch_SceneManager.LevelConstructor)
                );
                shipsContainer.transform.localPosition = Vector3.zero;
            }

            inited = true;

            if (ships.Count == 1)
            {
                ships[0].root.TryDestroy(true);
                ships.Clear();
            }

            string path = $"{Config._BasePath}/DecorativeShips";

            if (!Directory.Exists(path))
                return;

            var designs = Directory.GetFiles(path);

            if (designs.Length == 0)
                return;

            var dockyard = Patch_SceneManager.LevelConstructor.GetChild("Dock");
            var dockyardPos = dockyard.transform.localPosition;

            dockyard.transform.localPosition = Vector3.zero;

            VisualShip? s = null;
            int maxDepth = 10;

            do
            {
                if (maxDepth-- == 0)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Failed to find valid design after 10 attempts!");
                    return;
                }

                var file = designs[UnityEngine.Random.Range(0, designs.Length)];

                var design = Util.DeserializeObjectByte<Ship.Store>(File.ReadAllBytes(file));
                Melon<TweaksAndFixes>.Logger.Msg($"Attempting to display {design.vesselName}");

                s = MakeVisualShip(design);
            }
            while (s == null);

            ships.Add(s);

            // s.root.transform.localPosition += new Vector3(0, 0, 10);

            foreach (var loc in locations)
            {
                if (!loc.Fits(s))
                    continue;

                loc.Fill(s);

                break;
            }

            dockyard.transform.localPosition = dockyardPos;

            G.cam.enabled = false;
            G.cam.lookingAt = s.root.transform.position;
            G.cam.distance = 200;
            G.cam.distanceDesired = 200;
            G.cam.rotationX = 20;
            G.cam.rotationY = 225;
            UiM.SettupMainMenuCam(G.cam);

            // var design1 = G.GameData.sharedDesignsPerNation["usa"][0].Item1;
            // var design2 = G.GameData.sharedDesignsPerNation["usa"][1].Item1;
            // 
            // MakeVisualShip(design1, new(0,0,0), 0);
            // MakeVisualShip(design2, new(-50,0,-150), 90);
        }
    }
}
