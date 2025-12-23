using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace TweaksAndFixes.Data
{
    class TAFGlobalCache
    {
        private static GameObject container;

        public static GameObject deck;

        public static GameObject boarder;

        public static GameObject cubeVisualizer;

        public static GameObject cyliderVisualizer;

        public static GameObject partModelTemplates;

        public static GameObject UITemplates;

        public static void Init()
        {
            // Slider: Campaign, settings
            // Scroll View
            // Button
            // Checkbox
            // Input box
            // Input field (bug report?)

            container = new GameObject();
            container.name = "TAF Global Cache";
            container.transform.SetParent(G.container);
            container.SetActive(false);

            Melon<TweaksAndFixes>.Logger.Msg($"Initalizing TAF Global Cache...");

            Melon<TweaksAndFixes>.Logger.Msg($" Loading references...");

            GameObject jap_tb_hull = Util.ResourcesLoad<GameObject>("jap_tb_hull");

            Melon<TweaksAndFixes>.Logger.Msg($" Caching templates...");

            partModelTemplates = new GameObject();
            partModelTemplates.name = "Part Model Templates";
            partModelTemplates.transform.SetParent(container);

            deck = GameObject.Instantiate(ModUtils.GetChildAtPath("Visual/Sections/Stern/Deck", jap_tb_hull));
            deck.name = "DeckPlace";
            deck.transform.SetParent(partModelTemplates);
            deck.GetChild("DeckBorderRight").transform.SetParent(null);
            deck.GetChild("DeckBorderLeft").transform.SetParent(null);

            boarder = GameObject.Instantiate(ModUtils.GetChildAtPath("Visual/Sections/Stern/Deck/DeckBorderRight", jap_tb_hull));
            boarder.name = "DeckBorder";
            boarder.transform.SetParent(partModelTemplates);

            CreateCube();
            CreateCylider();

            UITemplates = new GameObject();
            UITemplates.name = "UI Templates";
            UITemplates.transform.SetParent(container);

            // Mesh cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            // 
            // MeshFilter filter = cubeVisualizer.AddComponent<MeshFilter>();
            // filter.sharedMesh = cubeMesh;
            // 
            // MeshRenderer render = cubeVisualizer.AddComponent<MeshRenderer>();
            // render.material.color = new Color(125, 125, 125);

            Melon<TweaksAndFixes>.Logger.Msg($"  Done!");
        }

        private static void CreateCube()
        { 
            var template = Resources.Load<GameObject>("default_part"); //GameObject.Instantiate(ModUtils.GetChildAtPath("Visual/Sections/Stern/Deck/DeckBorderRight/BorderVisual", jap_tb_hull));

            cubeVisualizer = GameObject.Instantiate(template.GetChild("Visual").GetChild("Cube"));
            cubeVisualizer.name = "cubeVisualizer";
            cubeVisualizer.transform.SetScale(1, 1, 1);
            cubeVisualizer.transform.SetParent(partModelTemplates);
            cubeVisualizer.TryDestroyComponent<LODGroup>();
            cubeVisualizer.TryDestroyComponent<AutomaticLOD>();
            cubeVisualizer.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0.8f);
            cubeVisualizer.GetChild("LOD0").TryDestroy();
            cubeVisualizer.GetChild("LOD1").TryDestroy();
            cubeVisualizer.GetChild("LOD2").TryDestroy();

            Resources.UnloadAsset(template);
        }

        private static void CreateCylider()
        {
            var template = Resources.Load<GameObject>("tsesarevich_gun_152_x3");

            cyliderVisualizer = GameObject.Instantiate(ModUtils.GetChildAtPath("Visual/tsh_gun_152_x2_001/tsh_gun_152_barrel_001 1/Cylinder", template));
            cyliderVisualizer.name = "cyliderVisualizer";
            cyliderVisualizer.transform.SetScale(1, 1, 1);
            cyliderVisualizer.transform.SetParent(partModelTemplates);
            cyliderVisualizer.TryDestroyComponent<LODGroup>();
            cyliderVisualizer.TryDestroyComponent<AutomaticLOD>();

            var cyliderMat = new Material(Shader.Find("Standard"));
            cyliderMat.SetOverrideTag("RenderType", "Transparent");

            cyliderVisualizer.GetComponent<MeshRenderer>().material = cyliderMat;
            cyliderVisualizer.GetChild("LOD0").TryDestroy();
            cyliderVisualizer.GetChild("LOD1").TryDestroy();
            cyliderVisualizer.GetChild("LOD2").TryDestroy();

            Resources.UnloadAsset(template);
        }
    }
}
