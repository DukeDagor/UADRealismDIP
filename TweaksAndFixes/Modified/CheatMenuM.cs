using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2CppUiExt;

#pragma warning disable CS8600
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618

//Temp place for me to put code, will create their own file when they are ready

namespace TweaksAndFixes
{
    public class CheatMenu
    {
        public static void Start()
        {


            
            int cheatMenuEnabled = Config.Param("taf_cheatMenuEnabled", 0);
            if (cheatMenuEnabled == 1)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Cheat Menu Enabled - Dirty Cheater");
                SetupCheatMenu();
            }
        }
        //CheatMenu
        // Global/Ui/UiMain/Common/Options/ (For the location of the top right buttons)
        // Global/Ui/UiMain/Popup/Generic
        private static GameObject cheatMenuEvent;
        private static Button _cheatMenuButton;

        private static Player cheatPlayer;
        private static float currentAmount;
        private static float newAmount;

        public static void SetupCheatMenu()
        {
            // Global/Ui/UiMain/Common/Options/ (For the location of the top right buttons) — clone Help button style
            GameObject options = ModUtils.GetChildAtPath("Global/Ui/UiMain/Common/Options");
            GameObject helpButton = ModUtils.GetChildAtPath("Global/Ui/UiMain/Common/Options/Help");
            GameObject cheatMenuButton = GameObject.Instantiate(helpButton);
            cheatMenuButton.transform.SetParent(options.transform);
            cheatMenuButton.name = "CheatMenuButton";
            cheatMenuButton.SetActive(true);

            Sprite sprite = TryLoadCheatButtonSpriteFromGame();
            if (sprite == null)
            {
                string spritePath = Path.Combine(Config._BasePath, "Sprites", "cheat_btn.png");
                MelonLoader.MelonLogger.Msg("SpritePath: " + spritePath);
                if (File.Exists(spritePath))
                {
                    byte[] rawData = File.ReadAllBytes(spritePath);
                    var tex = new Texture2D(2, 2, TextureFormat.DXT5, true);
                    if (ImageConversion.LoadImage(tex, rawData))
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (sprite != null)
            {
                Outline outline = cheatMenuButton.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(1, 1);
                Transform imageChild = cheatMenuButton.transform.Find("Image");
                MelonLoader.MelonLogger.Msg("ImageChild: " + imageChild.name);
                MelonLoader.MelonLogger.Msg("Sprite: " + sprite.name);
                MelonLoader.MelonLogger.Msg("CheatMenuButton: " + cheatMenuButton.name);
                if (imageChild != null && imageChild.TryGetComponent<Image>(out var img))
                {
                    img.sprite = sprite;
                    img.preserveAspect = true;
                }
            }

            Button btn = cheatMenuButton.GetComponent<Button>();
            if (btn != null)
            {
                _cheatMenuButton = btn;
                btn.onClick.AddListener(new System.Action(() =>
                {
                    if (GameManager.IsCampaign)
                        CampaignCheatMenu();
                    else if (GameManager.IsConstructor)
                        ConstructorCheatMenu();
                    else if (GameManager.IsBattle)
                        BattleCheatMenu();
                }));
            }
        }

        /// <summary>Tries to load the cheat button sprite from the game's assets; returns null if not found.</summary>
        private static Sprite TryLoadCheatButtonSpriteFromGame()
        {

            var item = UnityEngine.Resources.Load<Sprite>("tabs/intel");
            if (item != null) {

                Sprite sprite = item.TryCast<Sprite>();
                if (sprite != null) return sprite;
            }
            return null;
        }

        public static void CampaignCheatMenu()
        {
            // Only open on world map to avoid NullRef in game's NewGameUI (event system hits null when popup opens during new-game flow).

            MelonLoader.MelonLogger.Msg("CampaignCheatMenu");
            cheatPlayer = ExtraGameData.MainPlayer();
            GameObject popupTemplate = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu");
            if (popupTemplate == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("CampaignCheatMenu: PopupMenu template not found.");
                return;
            }
            GameObject popWindows = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows");
            if (popWindows == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("CampaignCheatMenu: PopWindows not found.");
                return;
            }

            cheatMenuEvent = GameObject.Instantiate(popupTemplate);
            cheatMenuEvent.transform.SetParent(popWindows);
            cheatMenuEvent.name = "Cheat Menu";
            cheatMenuEvent.transform.SetScale(1, 1, 1);
            cheatMenuEvent.transform.localPosition = Vector3.zero;

            GameObject window = cheatMenuEvent.GetChild("Window");
            if (window == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("CampaignCheatMenu: Window not found on popup.");
                cheatMenuEvent.TryDestroy();
                cheatMenuEvent = null;
                return;
            }

            void SetButton(GameObject btn, string label, System.Action onPress)
            {
                if (btn == null) return;
                GameObject textObj = btn.GetChild("Text (TMP)");
                if (textObj != null)
                {
                    textObj.TryDestroyComponent<LocalizeText>(); //Fix Later to Add Localization
                    TMP_Text tmp = textObj.GetComponent<TMP_Text>();
                    if (tmp != null) tmp.text = label;
                }
                Button b = btn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    if (label == "Cheat Menu"){
                        b.transition = Button.Transition.None;
                        return;
                    }
                    if (label == "Close"){
                        b.onClick.AddListener(new System.Action(() =>
                        {
                            cheatMenuEvent.SetActive(false);
                            if (_cheatMenuButton != null) _cheatMenuButton.interactable = true;
                        }));

                    }
                    else{
                        b.onClick.AddListener(new System.Action(onPress));
                    }
                }
            }

            int idx = 0;
            var labelsAndActions = new (string label, System.Action onPress)[]
            {
                ("Cheat Menu", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat Menu pressed")),
                ("Give 10 Million", () => GivePlayerMoney(cheatPlayer, 10000000)),
                ("Instant Build Ships", () => InstantBuildShips(cheatPlayer)),
                ("Instant Repair", () =>   InstantRepair(cheatPlayer)),
                ("Research Focuses", () => ResearchFocuses(cheatPlayer)),
                ("Close", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat Menu closed")),
            };
            for (int i = 0; i < window.transform.childCount; i++)
            {
                GameObject child = window.transform.GetChild(i).gameObject;
                Button b = child.GetComponent<Button>();
                if (b == null) continue;
                if (idx < labelsAndActions.Length)
                {
                    var la = labelsAndActions[idx];
                    SetButton(child, la.label, la.onPress);
                    child.SetActive(true);
                    idx++;
                }
                else
                    child.SetActive(false);
            }

            GameObject header = window.GetChild("Header");
            if (header != null) header.GetComponent<TMP_Text>().text = "Campaign Cheats";

            cheatMenuEvent.SetActive(true);
            if (_cheatMenuButton != null) _cheatMenuButton.interactable = false;
        }

        public static void CampaignCheatMenu2()
        {
            // Global/Ui/UiMain/Popup/Generic — popup with three buttons
            MelonLoader.MelonLogger.Msg("CampaignCheatMenu");
            cheatMenuEvent = GameObject.Instantiate(ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/Generic"));
            cheatMenuEvent.transform.SetParent(ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/PopWindows"));
            cheatMenuEvent.name = "Cheat Menu";
            cheatMenuEvent.transform.SetScale(2, 2, 2);
            cheatMenuEvent.transform.localPosition = Vector3.zero;

            GameObject window = cheatMenuEvent.GetChild("Window");
            GameObject buttons = window.GetChild("Buttons");
            GameObject yesTemplate = buttons.GetChild("Yes");

            ModUtils.GetChildAtPath("Buttons/Ok", window).TryDestroy();
            ModUtils.GetChildAtPath("Buttons/No", window).TryDestroy();

            void SetButton(GameObject btn, string label, System.Action onPress)
            {
                GameObject textObj = btn.GetChild("Text (TMP)");
                if (textObj != null) textObj.TryDestroyComponent<LocalizeText>();
                TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) tmp.text = label;
                Button b = btn.GetComponent<Button>();
                if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(new System.Action(onPress)); }
            }

            SetButton(yesTemplate, "Give 1 Million", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat: Give 1 Million pressed"));
            GameObject btn2 = GameObject.Instantiate(yesTemplate);
            btn2.transform.SetParent(buttons.transform);
            btn2.name = "InstantBuildShips";
            SetButton(btn2, "Instant Build Ships", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat: Instant Build Ships pressed"));
            GameObject btn3 = GameObject.Instantiate(yesTemplate);
            btn3.transform.SetParent(buttons.transform);
            btn3.name = "InstantRepair";
            SetButton(btn3, "Instant Repair", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat: Instant Repair pressed"));

            GameObject header = window.GetChild("Header");
            if (header != null) header.GetComponent<TMP_Text>().text = "Campaign Cheats";

            cheatMenuEvent.SetActive(true);
        }
        public static void ConstructorCheatMenu()
        {
            // TODO: Constructor cheat menu
        }
        public static void BattleCheatMenu()
        {
            // TODO: Battle cheat menu
        }

        public static void ShowCheatMenuPopupForPlayer(Player player)
        {
            cheatPlayer = player;
            cheatMenuEvent.SetActive(true);
        }
        public static void GivePlayerMoney(Player player, int amount)
        {
            if (player == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("GivePlayerMoney: player is null.");
                return;
            }
            currentAmount = player.cash;
            newAmount = currentAmount + amount;
            player.cash = newAmount;
            MelonLoader.MelonLogger.Msg("Player given " + amount + " money from " + currentAmount + " to " + newAmount);
            RefreshFinancesUI();
        }

        /// <summary>
        /// Refreshes the Finances Window (e.g. Naval Funds under WorldEx/Finances Window/Root).
        /// Uses G.ui.RefreshCampaignUI() to refresh campaign UI; optionally can update Naval Funds text manually.
        /// </summary>
        public static void RefreshFinancesUI()
        {
            //Doesn't work?
            if (G.ui != null)
                G.ui.RefreshCampaignUI();
            // Manual update if needed: GameObject root = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/Finances Window/Root");
            // GameObject navalFunds = root?.GetChild("Naval Funds"); TMP_Text t = navalFunds?.GetComponent<TMP_Text>(); if (t != null && cheatPlayer != null) t.text = ...;
        }
        public static void InstantBuildShips(Player player)
        {
            //We can adjust progress, but can't set the cost without postfix
            if (player == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("InstantBuildShips: player is null.");
                return;
            }
        
            foreach (Ship ship in player.GetFleetAll()){
                if (ship.isDesign != null && ship.isBuilding == true){
                    ship.buildingProgress = 99.9f;
                    
                }
                else if (ship.isDesign != null) {
                   // MelonLoader.MelonLogger.Msg("Ship " + ship.name + " is a design and is not building " + ship.isBuilding + " and is design " + ship.isDesign + " id " + ship.id);
                }
            }


            MelonLoader.MelonLogger.Msg("Player instant built ships");
            RefreshFinancesUI();

        }
        public static void InstantRepair(Player player)
        {
            // Set to true to force-complete all repairs (isRepairing is readonly, so we only set progress).
            bool ignoreRepairTime = false;
            //We can adjust progress, but can't set the cost without postfix
            if (player == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("InstantRepair: player is null.");
                return;
            }
        
            foreach (Ship ship in player.GetFleetAll()){
                if (ship.isDesign != null && ship.isRepairing == true){
                    ship.repairingProgress = 99.9f;
                    
                }
                else if (ship.isDesign != null) {
                //    MelonLoader.MelonLogger.Msg("Ship " + ship.name + " is a design and is not repairing " + ship.isRepairing + " and is design " + ship.isDesign + " id " + ship.id);
                }
            }
            //FutureOptions??  isRepairing is readonly.
            if (ignoreRepairTime)
            {
                foreach (Ship ship in player.GetFleetAll())
                {
                    if (ship.repairingProgress < 100.0f || ship.isRepairing == true)
                    {
                    ship.repairingProgress = 100.0f;
                  //  ship.isRepairing = false;
                    }
                }
            }


            MelonLoader.MelonLogger.Msg("Player instant repaired ships");
            RefreshFinancesUI();
        }
        public static void ResearchFocuses(Player player)
        {
            if (player == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("ResearchFocuses: player is null.");
                return;
            }
            foreach (string priorityName in player.techPriorities)
            {
                var pattern = new Regex("^" + Regex.Escape(priorityName) + @"(_\d+)?$");
                foreach (Technology playerTech in player.technologies)
                {
                    var techData = playerTech.data;
                    if (techData == null || playerTech.progress == 100f || playerTech.IsEndTechResearched)
                        continue;
                    string techName = techData.name;
                    if (pattern.IsMatch(techName))
                    {
                        MelonLoader.MelonLogger.Msg(playerTech.ToString());
                        float curProg = playerTech.progress;
                        playerTech.progress = 99.9f;
                        MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                        break;
                    }
                }
            }
        }

        /// <summary>Log all public instance/static fields and methods for an object so we can see what to manipulate.</summary>
        public static void DumpTypeFieldsAndMethods(string label, object obj)
        {
            if (obj == null) { MelonLoader.MelonLogger.Msg($"[{label}] null"); return; }
            Type t = obj.GetType();
            MelonLoader.MelonLogger.Msg($"[{label}] Type: {t.FullName}");
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            foreach (FieldInfo f in t.GetFields(bf))
                MelonLoader.MelonLogger.Msg($"  Field: {f.FieldType.Name} {f.Name}");
            foreach (MethodInfo m in t.GetMethods(bf))
            {
                if (m.IsSpecialName && (m.Name.StartsWith("get_") || m.Name.StartsWith("set_"))) continue;
                var ps = m.GetParameters();
                string pstr = ps.Length == 0 ? "" : "(" + string.Join(", ", System.Linq.Enumerable.Select(ps, p => p.ParameterType.Name + " " + p.Name)) + ")";
                MelonLoader.MelonLogger.Msg($"  Method: {m.ReturnType.Name} {m.Name}{pstr}");
            }
        }
    }
}