using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using UnityEngine.UI;
using UnityEngine;
using Il2CppTMPro;
using static MelonLoader.MelonLogger;
using TweaksAndFixes.Data;

namespace TweaksAndFixes.Harmony
{
    [HarmonyPatch(typeof(UISaveLoadWindow))]
    public class Patch_UISaveLoadWindow
    {
        private static GridLayoutGroup group;
        private static UISaveBattleSlot template;
        public static UISaveLoadWindow _this;

        public static UISaveBattleSlot GetSaveSlot(int index)
        {
            if (_this.SaveSlots.Count < index)
            {
                _this.SaveSlots[index].isEmpty = false;
                return _this.SaveSlots[index];
            }

            EnsureSaveSlots(index);

            _this.SaveSlots[index].isEmpty = false;
            return _this.SaveSlots[index];
        }

        internal static void EnsureSaveSlots(int upTo)
        {
            // TODO: Paramiterize the min number of slots
            // Multiple of 3 to fill the rows
            //  Add an extra blank row beneath it too
            int desired = Math.Max(((upTo / 3) + 2) * 3, 9);
        
            if (_this.SaveSlots.Count == desired)
                return;

            // if (_this.SaveSlots.Count > desired)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg($"Deleting {_this.SaveSlots.Count - desired} slots...");
            // 
            //     while (desired < _this.SaveSlots.Count)
            //     {
            //         var slot = _this.SaveSlots[^0];
            //         Melon<TweaksAndFixes>.Logger.Msg($"  Deleting: #{slot.SlotIndex}");
            //         slot.gameObject.TryDestroy(true);
            //         _this.SaveSlots.Remove(_this.SaveSlots[^0]);
            //     }
            // }
            // else
            if (_this.SaveSlots.Count < desired)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Creating {desired - _this.SaveSlots.Count} slots...");
        
                while (desired > _this.SaveSlots.Count)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Creating: #{_this.SaveSlots.Count}");
                    var go = GameObject.Instantiate(template.gameObject, group.transform);
                    go.active = true;
                    go.name = $"Slot_{_this.SaveSlots.Count}";
                    var slot = go.GetComponent<UISaveBattleSlot>();
                    _this.SaveSlots.Add(slot);
                    slot.SlotIndex = _this.SaveSlots.Count - 1;

                    string name = ModUtils.LocalizeF("$Ui_EmptySlot0", $"{slot.SlotIndex}");
                    slot.isEmpty = true;
                    slot.SaveName.text = name;
                    slot.Action.onClick.RemoveAllListeners();
                    slot.Action.onClick.AddListener(new System.Action(() => {
                        Melon<TweaksAndFixes>.Logger.Msg($"  Clicked slot {slot.SlotIndex}!");
                        if (_this.selectedSlotIndex != -1
                            && _this.selectedSlotIndex < _this.SaveSlots.Count)
                            _this.SaveSlots[_this.selectedSlotIndex].Select(false);
                        slot.Select(true);
                        _this.selectedSlotIndex = slot.SlotIndex;
                        _this.CheckButtons();
                    }));
                }
            }
        
            _this.selectedSlotIndex = -1;
        }

        // CheckButtons
        [HarmonyPatch(nameof(UISaveLoadWindow.CheckButtons))]
        [HarmonyPrefix]
        internal static bool Prefix_CheckButtons()
        {
            _this.LoadButton.interactable = true;
            _this.DeleteButton.interactable = true;
            nameInputField.interactable = true;
            nameInputField.text = "Enter name...";

            Melon<TweaksAndFixes>.Logger.Msg($"Selected: {_this.selectedSlotIndex}");
            if (_this.selectedSlotIndex != -1)
                Melon<TweaksAndFixes>.Logger.Msg($"IsEmpty: {_this.SaveSlots[_this.selectedSlotIndex].isEmpty}");

            if (_this.selectedSlotIndex == -1 || _this.SaveSlots[_this.selectedSlotIndex].isEmpty)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"No save selected or selected save is empty");

                if (!_this.allowLoadOnEmpty)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"In save mode, disabling load button");
                    _this.LoadButton.interactable = false;
                }
                
                if(!_this.allowLoadOnEmpty)
                {
                    Melon<TweaksAndFixes>.Logger.Msg($"Can't load empty slot, disabling name field");
                    nameInputField.interactable = false;
                    nameInputField.text = "Select valid save slot...";
                }

                _this.DeleteButton.interactable = false;
            }

            return false;
        }


        // Refresh
        [HarmonyPatch(nameof(UISaveLoadWindow.Init))]
        [HarmonyPrefix]
        internal static bool Prefix_Init()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Init");

            foreach (var slot in _this.SaveSlots)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"  For slot {slot.SlotIndex}");

                string name = ModUtils.LocalizeF("$Ui_EmptySlot0", $"{slot.SlotIndex}");
                slot.SaveName.text = name;
                slot.Action.onClick.RemoveAllListeners();
                slot.Action.onClick.AddListener(new System.Action(() => {
                    Melon<TweaksAndFixes>.Logger.Msg($"  Clicked slot {slot.SlotIndex}!");
                    if (_this.selectedSlotIndex != -1
                        && _this.selectedSlotIndex < _this.SaveSlots.Count)
                        _this.SaveSlots[_this.selectedSlotIndex].Select(false);
                    slot.Select(true);
                    _this.selectedSlotIndex = slot.SlotIndex;
                    _this.CheckButtons();
                }));
            }

            return false;
        }

        // Refresh
        [HarmonyPatch(nameof(UISaveLoadWindow.Refresh))]
        [HarmonyPrefix]
        internal static bool Prefix_Refresh()
        {
            if (!isInited)
                return false;

            Melon<TweaksAndFixes>.Logger.Msg($"Prefix_Refresh");

            // Fill to minimum
            EnsureSaveSlots(0);

            if (_this.saveSlotLimit <= 0)
                return false;

            if (_this.currentType == UISaveLoadWindow.SaveWindowType.Campaign)
            {
                foreach (var saveData in TAFCampaignData.Data)
                {
                    var save = saveData.GetStore();
                    var slot = GetSaveSlot(saveData.entryData["TAF_SaveIndex"].dataAsInt);

                    if (_this.selectedSlotIndex == slot.SlotIndex)
                        slot.Highlight.active = true;
                    else
                        slot.Highlight.active = false;

                    if (save == null || !save.IsValid())
                    {
                        slot.SetAsEmpty();
                        continue;
                    }

                    var info = slot.gameObject.GetChild("Info");
                    var date =
                        new System.DateTime(save.LastSaveTicks)
                        .ToString("MM/dd/yyyy | HH:mm:ss tt");
                    var gameDate =
                        new System.DateTime(save.StartYear, 1, 1)
                        .AddMonths(save.CurrentDate.turn);

                    slot.SaveName.text = saveData.entryData["TAF_SaveName"].data;
                    slot.isEmpty = false;
                    slot.SaveDate.text = 
                        $"1. {save.StartYear} ~ " +
                        $"{gameDate.Month}. {gameDate.Year}\n" +
                        $"{saveData.entryData["TAF_VersionName"].data}";
                    
                    info.GetComponent<TextMeshProUGUI>().text = 
                        $"{saveData.entryData["TAF_PlayerName"].data}\n" +
                        $"{date}\n";
                }
            }
            else
            {
                var battles =
                    _this.currentType == UISaveLoadWindow.SaveWindowType.Custom ?
                    GameManager.Instance.BattleStorage.CustomBattles
                    : GameManager.Instance.BattleStorage.Missions;

                for (int i = 0; i < _this.saveSlotLimit; i++)
                {
                    var battle = battles[i];
                    var slot = _this.SaveSlots[i];

                    if (battle == null || !battle.IsValid())
                    {
                        slot.SetAsEmpty();
                        continue;
                    }

                    var date = new System.DateTime(battle.DateTicks);
                    slot.IsCustomBattle = !battle.IsAcademyMission;
                    slot.SaveName.text = battle.FriendlyName;
                    slot.isEmpty = false;
                    slot.SaveDate.text = date.ToString("MM/dd/yyyy HH:mm:ss tt");
                }
            }

            _this.CheckButtons();

            return false;
        }

        private static bool isInited = false;

        // Start
        [HarmonyPatch(nameof(UISaveLoadWindow.Start))]
        [HarmonyPrefix]
        internal static bool Prefix_Start(UISaveLoadWindow __instance)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Start");

            _this = __instance;

            var saveList = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/SaveWindow/Root/Scroll View/Viewport/List");
            saveList.TryDestroyComponent<VerticalLayoutGroup>(true);
            group = saveList.AddComponent<GridLayoutGroup>();
            group.cellSize = new(440,120);
            group.spacing = new(5,5);
            group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            group.constraintCount = 3;
            group.childAlignment = UnityEngine.TextAnchor.UpperCenter;
            Melon<TweaksAndFixes>.Logger.Msg($"  Inited group");

            for (int i = _this.SaveSlots.Count - 1; i >= 1; i--)
            {
                _this.SaveSlots[i].gameObject.TryDestroy(true);
                _this.SaveSlots.RemoveAt(i);
            }

            Melon<TweaksAndFixes>.Logger.Msg($"  Deleted base slots");

            template = _this.SaveSlots[0];
            _this.SaveSlots.Clear();
            template.gameObject.TryDestroyComponent<HorizontalLayoutGroup>(true);
            template.gameObject.active = false;

            var highlight = template.gameObject.GetChild("Highlight");
            highlight.transform.SetScale(1, 1, 1);
            var highlightRt = highlight.GetComponent<RectTransform>();
            highlightRt.offsetMax = Vector2.zero;
            highlightRt.offsetMin = Vector2.zero;
            highlight.active = false;

            var bg = GameObject.Instantiate(highlight, template.transform);
            bg.name = "bg";
            bg.active = true;

            template.SaveName.alignment = Il2CppTMPro.TextAlignmentOptions.TopLeft;
            var saveNameRt = template.SaveName.gameObject.GetComponent<RectTransform>();
            saveNameRt.offsetMax = new(425, -5);
            saveNameRt.offsetMin = new(5, -50);

            template.SaveDate.alignment = Il2CppTMPro.TextAlignmentOptions.BottomRight;
            var saveDateRt = template.SaveDate.gameObject.GetComponent<RectTransform>();
            saveDateRt.offsetMax = new(425, -55);
            saveDateRt.offsetMin = new(100f, -110);

            var saveInfo = GameObject.Instantiate(template.SaveDate.gameObject, template.transform);
            saveInfo.name = "Info";
            saveInfo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
            var saveInfoRt = saveInfo.GetComponent<RectTransform>();
            saveInfoRt.offsetMax = new(300f, -55);
            saveInfoRt.offsetMin = new(5, -110);

            Melon<TweaksAndFixes>.Logger.Msg($"  Created template");

            isInited = true;

            return true;
        }

        public static TMP_InputField nameInputField;
        public static string nextCampaignName = string.Empty;

        public static void InitUi()
        {
            var saveWindow = ModUtils.GetChildAtPath("Global/Ui/UiMain/Popup/SaveWindow");

            var root = saveWindow.GetChild("Root");
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.offsetMax = new(683, 385);
            rootRt.offsetMin = new(-683, -385);

            root.GetChild("Border").active = false;

            var headerRt = root.GetChild("Header").GetComponent<RectTransform>();
            headerRt.offsetMax = new(1336, 0);

            var buttonsRt = root.GetChild("Buttons").GetComponent<RectTransform>();
            buttonsRt.offsetMax = new(1336, -700);
            buttonsRt.offsetMin = new(0, -750);

            var scrollRt = root.GetChild("Scroll View").GetComponent<RectTransform>();
            scrollRt.offsetMax = new(1336, -50);
            scrollRt.offsetMin = new(0, -650);

            // Global/Ui/UiMain/WorldEx/Windows/Fleet Design Window/Root/Fleet Ships/Viewport/Content/Fleet Template/NameInputField

            var nameInput = GameObject.Instantiate(ModUtils.GetChildAtPath(
                "Global/Ui/UiMain/WorldEx/Windows/Fleet Design Window/Root/Fleet Ships/Viewport/Content/Fleet Template/NameInputField"),
                root.transform
            );
            nameInput.TryDestroyComponent<LayoutElement>();
            nameInput.name = "Name Input";
            nameInput.active = true;
            nameInputField = nameInput.GetComponent<TMP_InputField>();
            nameInputField.textComponent.fontSizeMin = 18;
            nameInputField.caretColor = Color.white;
            nameInputField.text = "Enter name...";
            nameInputField.onValidateInput = null;

            var nameInputRt = nameInput.GetComponent<RectTransform>();
            nameInputRt.drivenByObject = null;
            nameInputRt.drivenProperties = DrivenTransformProperties.None;
            nameInputRt.offsetMax = new(1136, 120);
            nameInputRt.offsetMin = new(200, 95);

            nameInputField.onEndEdit.RemoveAllListeners();
            nameInputField.onEndEdit.AddListener(new System.Action<string>((string s) => {
                Melon<TweaksAndFixes>.Logger.Msg($"Entered: {s}");
                nextCampaignName = s;
            }));
        }
    }
}
