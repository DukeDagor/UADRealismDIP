using Il2Cpp;
using Il2CppTMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace TweaksAndFixes.Data
{
    internal class TAFUI
    {
        public class TAF_InputField
        {
            public GameObject root;

            public GameObject BG;
            public GameObject Edit;
            public InputField EditField;
            public GameObject Static;
            public Text StaticText;
            public int staticTextCharLimit;

            public TAF_InputField(GameObject from, int staticTextCharLimit)
            {
                this.staticTextCharLimit = staticTextCharLimit;

                root = from;

                BG = root.GetChild("EditName").GetChild("Bg");
                root.GetChild("EditName").GetChild("Edit").TryDestroy(true);

                Edit = GameObject.Instantiate(ModUtils.GetChildAtPath(
                    "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/" +
                    "FoldShipSettings/ShipSettings/ShipName/EditName/Edit"
                ));
                Edit.SetParent(root.GetChild("EditName"), false);
                Edit.name = "Edit";
                EditField = Edit.GetComponent<InputField>();
                EditField.characterLimit = 0;
                Static = root.GetChild("EditName").GetChild("Static");
                StaticText = Static.GetChild("Text").GetComponent<Text>();

                GameObject placeholder = ModUtils.GetChildAtPath("EditName/Edit/Placeholder", root);

                Text placeholderText = placeholder.GetComponent<Text>();
                string placeholderString = placeholderText.text;

                Button btn = root.GetChild("EditName").GetComponent<Button>();
                btn.onClick.AddListener(new System.Action(() =>
                {
                    BG.SetActive(true);
                    Edit.SetActive(true);
                    EditField.ActivateInputField();
                    Static.SetActive(false);
                    placeholderText.text = placeholderString;
                }));
            }

            public TAF_InputField(GameObject parent, string name, Vector2 offsetMax, Vector2 offsetMin, string defaultString = "", string placeholderString = "", bool alignRight = false, int staticTextCharLimit = 0)
            {
                this.staticTextCharLimit = staticTextCharLimit;

                root = GameObject.Instantiate(ModUtils.GetChildAtPath(
                    "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/FoldShipSettings/ShipSettings/ShipName"
                ));
                root.SetParent(parent);
                root.transform.SetScale(1f, 1f, 1f);
                root.transform.localPosition = Vector3.zero;
                root.name = name;
                root.GetComponent<LayoutElement>().preferredHeight = 40;
                LayoutGroup RootLayout = root.GetComponent<LayoutGroup>();
                if (alignRight) RootLayout.childAlignment = TextAnchor.MiddleRight;


                RectTransform transform = root.GetComponent<RectTransform>();
                transform.offsetMax = offsetMax;
                transform.offsetMin = offsetMin;

                GameObject EditName = root.GetChild("EditName");
                LayoutGroup EditNameLayout = EditName.GetComponent<LayoutGroup>();
                EditName.GetComponent<LayoutElement>().preferredHeight = 40;
                if (alignRight) EditNameLayout.childAlignment = TextAnchor.MiddleRight;

                BG = root.GetChild("EditName").GetChild("Bg");

                Edit = root.GetChild("EditName").GetChild("Edit");
                Edit.TryDestroyComponent<CheckShipName>();
                Edit.transform.SetScale(1.3f, 1.3f, 1.3f);
                EditField = Edit.GetComponent<InputField>();
                EditField.text = defaultString;
                EditField.characterLimit = 0;
                //InputChooseYearEditField.textComponent.fontSize += 5;

                Static = root.GetChild("EditName").GetChild("Static");
                Static.GetChild("Header").TryDestroy();
                Static.transform.SetScale(1.3f, 1.3f, 1.3f);
                StaticText = Static.GetChild("Text").GetComponent<Text>();
                StaticText.text = ModUtils.StringOrSubstring(defaultString, staticTextCharLimit);
                //InputChooseYearStaticText.fontSize += 5;

                // GameObject spacer = GameObject.Instantiate(root.GetChild("EditName").GetChild("Static").GetChild("EditIcon"));
                // spacer.transform.SetParent(Static.transform);
                // spacer.transform.SetScale(1f, 1f, 1f);
                // spacer.transform.SetSiblingIndex(0);
                // spacer.TryDestroyComponent<Image>();
                // spacer.TryDestroyComponent<CanvasGroup>();
                // spacer.TryDestroyComponent<Outline>();

                GameObject placeholder = ModUtils.GetChildAtPath("EditName/Edit/Placeholder", root);
                Text placeholderText = placeholder.GetComponent<Text>();

                Button btn = root.GetChild("EditName").AddComponent<Button>();
                btn.onClick.AddListener(new System.Action(() =>
                {
                    BG.SetActive(true);
                    Edit.SetActive(true);
                    EditField.ActivateInputField();
                    Static.SetActive(false);
                    placeholderText.text = placeholderString;
                }));
            }

            public void SetOnSubmit(System.Action<string> onSubmit)
            {
                EditField.onValidateInput = null;

                EditField.onEndEdit.AddListener(new System.Action<string>((string input) =>
                {
                    BG.SetActive(false);
                    Edit.SetActive(false);
                    EditField.DeactivateInputField();
                    Static.SetActive(true);

                    onSubmit.Invoke(input);
                }));
            }

            public void SetOnValueChange(System.Action<string>? custom = null, bool intOnly = false, bool floatOnly = false)
            {
                if (custom != null)
                    EditField.onValueChange.AddListener(custom);
                else if (intOnly)
                    EditField.onValueChange.AddListener(new System.Action<string>((string value) =>
                    {
                        int _ = 0;

                        if (value.Length > 0 && !int.TryParse("" + value[^1], out _))
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"  Invalid: `{value[^1]}`");
                            EditField.text = EditField.text.Substring(0, EditField.text.Length - 1);
                        }
                    }));

                else if (floatOnly)
                    EditField.onValueChange.AddListener(new System.Action<string>((string value) =>
                    {
                        float _ = 0;

                        if (value.Length > 0 && !float.TryParse("" + value[^1], out _))
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg($"  Invalid: `{value[^1]}`");
                            EditField.text = EditField.text.Substring(0, EditField.text.Length - 1);
                        }
                    }));


            }

            public void SetText(string text = "")
            {
                StaticText.text = ModUtils.StringOrSubstring(text, staticTextCharLimit);
                EditField.text = text;
            }
        }

        public class TAF_Text
        {
            public GameObject root;
            public Text textComp;

            public TAF_Text(GameObject from)
            {
                root = from;
                textComp = root.GetComponent<Text>();
            }

            public TAF_Text(GameObject parent, string name, string text, Vector2 offsetMax, Vector2 offsetMin)
            {
                root = GameObject.Instantiate(ModUtils.GetChildAtPath(
                    "Global/Ui/UiMain/Constructor/Left/Scroll View/Viewport/Cont/" +
                    "FoldShipSettings/ShipSettings/ShipName/EditName/Static/Text"
                ));
                root.transform.SetParent(parent);
                root.name = name;
                root.transform.localPosition = Vector3.zero;
                root.transform.SetScale(1.3f, 1.3f, 1.3f);
                textComp = root.GetComponent<Text>();
                textComp.text = text;
                textComp.alignment = TextAnchor.MiddleLeft;
                RectTransform transform = root.GetComponent<RectTransform>();
                transform.offsetMax = offsetMax;
                transform.offsetMin = offsetMin;
            }
        }

        public class TAF_Button
        {
            public enum BUTTON_STYLE
            {
                DARK_PLATE,
                LIGHT_PLATE
            }

            public GameObject root;
            public TMP_Text textComp;
            public Button buttonComp;

            public TAF_Button(GameObject from)
            {
                root = from;
                textComp = root.GetChild("Text (TMP)").GetComponent<TMP_Text>();
                buttonComp = root.GetComponent<Button>();
                buttonComp.onClick.RemoveAllListeners();
            }

            public TAF_Button(GameObject parent, string name, string text, Vector2 offsetMax, Vector2 offsetMin)
            {
                // Global/Ui/UiMain/MainMenu/Layout/MenuButtons/Continue
                // Global/Ui/UiMain/Popup/PopupMenu/Window/ButtonBase

                root = GameObject.Instantiate(ModUtils.GetChildAtPath(
                    "Global/Ui/UiMain/Popup/PopupMenu/Window/ButtonBase"
                ));
                root.transform.SetParent(parent);
                root.name = name;
                root.SetActive(true);
                root.transform.localPosition = Vector3.zero;
                root.transform.SetScale(1f, 1f, 1f);
                textComp = root.GetChild("Text (TMP)").GetComponent<TMP_Text>();
                textComp.text = text;
                buttonComp = root.GetComponent<Button>();
                buttonComp.onClick.RemoveAllListeners();
                RectTransform transform = root.GetComponent<RectTransform>();
                transform.offsetMax = offsetMax;
                transform.offsetMin = offsetMin;
            }

            public void SetOnClick(System.Action onClick)
            {
                buttonComp.onClick.RemoveAllListeners();
                buttonComp.onClick.AddListener(onClick);
            }
        }
    }
}
