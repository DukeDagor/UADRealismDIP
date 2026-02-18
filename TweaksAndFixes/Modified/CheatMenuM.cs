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
using TweaksAndFixes.Data;

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
        private static bool _isInitialized = false;

        public static void SetupCheatMenu()
        {
            // Global/Ui/UiMain/Common/Options/ (For the location of the top right buttons) — clone Help button style
            GameObject options = ModUtils.GetChildAtPath("Global/Ui/UiMain/Common/Options");
            GameObject helpButton = ModUtils.GetChildAtPath("Global/Ui/UiMain/Common/Options/Help");
            GameObject cheatMenuButton = GameObject.Instantiate(helpButton);
            cheatMenuButton.transform.SetParent(options.transform);
            cheatMenuButton.name = "CheatMenuButton";
            cheatMenuButton.SetActive(true);
            // Remove Help button's tooltip so hover doesn't run its logic (causes NullReferenceException on our clone)
            cheatMenuButton.TryDestroyComponent<OnEnter>();
            cheatMenuButton.TryDestroyComponent<OnLeave>();
            UiM.AddTooltip(cheatMenuButton, "$TAF_UI_CheatMod_CheatMenu_Tooltip");

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
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.7f, 0.7f));
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
                    if (!GameManager.IsCampaign && !GameManager.IsConstructor)
                        return;
                    if (GameManager.IsCampaign)
                        CampaignCheatMenu();
                    else if (GameManager.IsConstructor)
                        ConstructorCheatMenu();
                }));
                UpdateCheatButtonInteractable();
            }
        }

        /// <summary>Updates the cheat menu button so it is only clickable in Campaign or Constructor.</summary>
        public static void UpdateCheatButtonInteractable()
        {
            if (_cheatMenuButton != null)
                _cheatMenuButton.interactable = GameManager.IsCampaign || GameManager.IsConstructor;
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

            // Fix for multiple menus — reuse existing, bring to front
            if (_isInitialized) {
                cheatMenuEvent.transform.SetAsLastSibling();
                cheatMenuEvent.SetActive(true);
                if (_cheatMenuButton != null) _cheatMenuButton.interactable = false;
                return;
            };


            MelonLoader.MelonLogger.Msg("CampaignCheatMenu");
            cheatPlayer = ExtraGameData.MainPlayer();
            GameObject popupTemplate = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu");
            GameObject popupBg = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu/Bg");
            GameObject worldEx = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/");
            if (popupTemplate == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("CampaignCheatMenu: PopupMenu template not found.");
                return;
            }
            GameObject popWindows = ModUtils.GetChildAtPath("Global/Ui/UiMain/WorldEx/");
            if (popWindows == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("CampaignCheatMenu: PopWindows not found.");
                return;
            }           

            cheatMenuEvent = GameObject.Instantiate(popupTemplate);
            //cheatMenuEvent.transform.SetParent(popupBg);
            //cheatMenuEvent.transform.SetParent(popWindows);
            cheatMenuEvent.transform.SetParent(worldEx);
            cheatMenuEvent.name = "Cheat Menu";
            cheatMenuEvent.transform.SetScale(1, 1, 1);
            cheatMenuEvent.transform.localPosition = Vector3.zero;
            // Root popup full-screen so overlay and blocking work
            RectTransform rootRect = cheatMenuEvent.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }

            // Bg: full-screen, visible dim, blocks clicks (options-menu style)
            GameObject bg = cheatMenuEvent.GetChild("Bg");
            if (bg != null)
            {
                bg.transform.SetAsFirstSibling();
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;
                }
                Image bgImage = bg.GetComponent<Image>();
                if (bgImage == null) bgImage = bg.AddComponent<Image>();
                bgImage.color = new Color(0f, 0f, 0f, 0.6f);
                bgImage.raycastTarget = true;
            }

            GameObject window = cheatMenuEvent.GetChild("Window");
            if (window == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("CampaignCheatMenu: Window not found on popup.");
                cheatMenuEvent.TryDestroy();
                cheatMenuEvent = null;
                return;
            }

            void MakeAndConfigButton(GameObject window, string label, string tag, string tooltip, System.Action onPress)
            {
                string displayText = LocalizeManager.Localize(tag);
                GameObject spacerTemplate = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu/Window/Spacer");
                GameObject buttonTemplate = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/PopupMenu/Window/SaveCampaign");
                if (buttonTemplate == null) return;

                GameObject btn = GameObject.Instantiate(buttonTemplate);
                btn.transform.SetParent(window, false);
                btn.name = "TAF_CheatMenu_" + label.Replace(" ", "_");
                btn.SetActive(true);
                btn.transform.localPosition = Vector3.zero;
                btn.transform.localScale = Vector3.one;

                UiM.SetLocalizedTextTag(btn.GetChild("Text (TMP)"), tag);
                if (tooltip != "") {
                    UiM.AddTooltip(btn, tooltip);
                }
                Button b = btn.GetComponent<Button>();
                if (label == "Cheat Menu")
                {  

                    if (b != null) b.transition = Button.Transition.None;
                    // UI.Image -> Graphic.color reference (1, 0.8, 0.55, 1)
                    if (btn != null && btn.TryGetComponent<Image>(out var headerImg))
                        headerImg.color = new Color(0.9f, 0.2f, 0.2f, 1f);

                    // Spacer after header
                    if (spacerTemplate != null)
                    {
                        GameObject spacer = GameObject.Instantiate(spacerTemplate);
                        spacer.transform.SetParent(window.transform, false);
                        spacer.transform.SetSiblingIndex(b.transform.GetSiblingIndex() + 1);
                        spacer.SetActive(true);
                    }
                }
                else if (label == "Close")
                {
                    if (spacerTemplate != null)
                    {
                        GameObject spacer = GameObject.Instantiate(spacerTemplate);
                        spacer.transform.SetParent(window.transform, false);
                        spacer.transform.SetSiblingIndex(b.transform.GetSiblingIndex() + 0);
                        spacer.SetActive(true);
                    }
                   b.onClick.AddListener(new System.Action(() => 
                   { 
                    cheatMenuEvent.SetActive(false); 
                    if (_cheatMenuButton != null) {
                        _cheatMenuButton.interactable = true;
                     } 
                    }
                   )
                   ); 
                }
                else 
                {
                    b.onClick.AddListener(new System.Action(onPress));
                }
            }

            int idx = 0;
            
            var labelsAndActions = new (string label, string tag, string tooltip, System.Action onPress)[]
            {
                ("Cheat Menu", "$TAF_UI_CheatMod_CheatMenu", "", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat Menu pressed")),
                ("Give 100 Million", "$TAF_UI_CheatMod_Give100Million", "$TAF_UI_CheatMod_Give100Million_Tooltip", () => GivePlayerMoney(cheatPlayer, 100000000)),
                ("Give 1 Billion", "$TAF_UI_CheatMod_Give1Billion", "$TAF_UI_CheatMod_Give1Billion_Tooltip", () => GivePlayerMoney(cheatPlayer, 1000000000)),
                ("Instant Build Ships", "$TAF_UI_CheatMod_InstantBuildShips", "$TAF_UI_CheatMod_InstantBuildShips_Tooltip", () => InstantBuildShips(cheatPlayer)),
                ("Instant Repair", "$TAF_UI_CheatMod_InstantRepair", "$TAF_UI_CheatMod_InstantRepair_Tooltip", () => InstantRepair(cheatPlayer)),
                ("Instant Research", "$TAF_UI_CheatMod_InstantResearch", "$TAF_UI_CheatMod_InstantResearch_Tooltip", () => ResearchFocuses(cheatPlayer)),
                ("Close", "$TAF_UI_CheatMod_Close", "", () => Melon<TweaksAndFixes>.Logger.Msg("Cheat Menu closed")),
            };
            for (int i = 0; i < window.transform.childCount; i++)
            {
                GameObject child = window.transform.GetChild(i).gameObject;
                Button b = child.GetComponent<Button>();
                if (b == null) continue;
                //Delete Buttons, then create new ones
                child.TryDestroy();
            }
            for (int i = 0; i < labelsAndActions.Length; i++)
            {
                var la = labelsAndActions[i];
                MakeAndConfigButton(window, la.label, la.tag, la.tooltip, la.onPress);

            }


            // Bring popup to front so it receives raycasts and blocks game input (options menu style)
            cheatMenuEvent.transform.SetAsLastSibling();
            cheatMenuEvent.SetActive(true);
            //cheatMenuEvent.transform.SetSiblingIndex(0);
            _isInitialized = true;

            if (_cheatMenuButton != null) _cheatMenuButton.interactable = false;
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
            //Convert to readable format
            string amountString = amount.ToString("C2");
            string newAmountString = newAmount.ToString("C2");
            string currentAmountString = currentAmount.ToString("C2");
            MelonLoader.MelonLogger.Msg("Player given " + amountString + " money from " + currentAmountString + " to " + newAmountString);
            RefreshFinancesUI();
        }

        /// <summary>
        /// Refreshes the Finances Window (e.g. Naval Funds under WorldEx/Finances Window/Root).
        /// Uses G.ui.RefreshCampaignUI() to refresh campaign UI; optionally can update Naval Funds text manually.
        /// Doesn't work.
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
        
        public static void ListChildren(GameObject obj)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                MelonLoader.MelonLogger.Msg(obj.transform.GetChild(i).name);
            }
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
                MelonLoader.MelonLogger.Msg("Research Item: " + priorityName);

                var pattern = new Regex("^" + Regex.Escape(priorityName) + @"(_\d+)?$");
                foreach (Technology playerTech in player.technologies)
                {
                    if (playerTech.progress == 100f || playerTech.IsEndTechResearched)
                        continue;
                    var techData = playerTech.data;
                    if (techData == null || playerTech.progress == 100f || playerTech.IsEndTechResearched)
                        continue;
                    string techName = techData.name;
                    // Category fallback: priority "gun_main" can match tech "gun_large" (same category "gun_")
                    string priorityCategory = priorityName.Split('_')[0];
                    bool categoryMatch = priorityCategory.Length > 0 && techName.StartsWith(priorityCategory + "_", System.StringComparison.OrdinalIgnoreCase);
                    if (pattern.IsMatch(techName) || categoryMatch)
                    {
                        MelonLoader.MelonLogger.Msg(playerTech.ToString());
                        float curProg = playerTech.progress;
                        playerTech.progress = 99.9f;
                        MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                        break;
                    }
                    //DIP Mod Fix (its not fixed lol)
                    if (!pattern.IsMatch(techName) && categoryMatch) {
                        if (techName == "gun_sec" && priorityName == "gun_small") {
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") is a small gun, setting to 99.9%");
                            float curProg = playerTech.progress;
                            playerTech.progress = 99.9f;
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                            break;
                        }
                        else if (techName == "gun_sec" && priorityName == "gun_medium") {
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") is a medium gun, setting to 99.9%");
                            float curProg = playerTech.progress;
                            playerTech.progress = 99.9f;
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                            break;
                        }
                        else if (techName == "gun_main" && priorityName == "gun_large") {
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") is a large gun, setting to 99.9%");
                            float curProg = playerTech.progress;
                            playerTech.progress = 99.9f;
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                            break;
                        }
                        else if (techName == "gun_main" && priorityName == "gun_verylarge") {
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") is a xlarge gun, setting to 99.9%");
                            float curProg = playerTech.progress;
                            playerTech.progress = 99.9f;
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                            break;
                        }
                        else if (techName == "gun_main" && priorityName == "gun_xlarge") {
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") is a verylarge gun, setting to 99.9%");
                            float curProg = playerTech.progress;
                            playerTech.progress = 99.9f;
                            MelonLoader.MelonLogger.Msg("Tech " + techName + " (priority " + priorityName + ") progress set to 99.9% from " + curProg);
                            break;
                        }
                    }
                }
            }
        }
    }
}