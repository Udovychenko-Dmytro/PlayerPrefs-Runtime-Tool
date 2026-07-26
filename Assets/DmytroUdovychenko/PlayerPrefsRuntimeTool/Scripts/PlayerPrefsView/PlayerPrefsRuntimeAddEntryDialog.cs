// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Modal dialog for creating a new PlayerPrefs entry: key input, Int/Float/String
    /// type selector, value input with validation and a two-step overwrite guard
    /// for keys that already exist.
    /// </summary>
    internal class PlayerPrefsRuntimeAddEntryDialog
    {
        private const string TypeInt32 = "Int32";
        private const string TypeSingle = "Single";
        private const string TypeString = "String";

        private readonly Dictionary<string, Button> m_typeButtons = new Dictionary<string, Button>(StringComparer.Ordinal);

        private GameObject m_dialogRoot;
        private InputField m_keyInputField;
        private InputField m_valueInputField;
        private GameObject m_errorTextObject;
        private Text m_saveButtonLabel;
        private string m_selectedType = TypeString;
        private bool m_overwriteArmed;
        private Action<PlayerPrefsRuntimeEntry> m_onEntryAdded;
        private Func<string, bool> m_keyExists;

        public void Show(Transform parent, Font font, Func<string, bool> keyExists, Action<PlayerPrefsRuntimeEntry> onEntryAdded)
        {
            if (parent == null)
            {
                return;
            }

            Close();

            m_onEntryAdded = onEntryAdded;
            m_keyExists = keyExists;
            m_selectedType = TypeString;
            m_overwriteArmed = false;
            m_typeButtons.Clear();

            Font resolvedFont = font != null ? font : PlayerPrefsRuntimeUiFactory.ResolveDefaultFont();

            GameObject overlay = PlayerPrefsRuntimeUiFactory.CreateOverlay(PlayerPrefsRuntimeViewConstants.AddEntryOverlayName, parent, Close, 0.85f);
            GameObject dialog = PlayerPrefsRuntimeUiFactory.CreateDialogPanel(overlay.transform, PlayerPrefsRuntimeViewConstants.AddEntryDialogName, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));

            GameObject contentRoot = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentRoot.transform.SetParent(dialog.transform, false);

            RectTransform contentRT = contentRoot.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(25f, 20f);
            contentRT.offsetMax = new Vector2(-25f, -25f);

            VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.spacing = 14f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;

            Text title = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.TitleName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, true, resolvedFont, out _);
            title.text = PlayerPrefsRuntimeViewConstants.AddEntryTitleText;
            AddFixedHeight(title.gameObject, 44f);

            Text keyLabel = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.ValueLabelName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogValueLabelFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false, resolvedFont, out _);
            keyLabel.text = PlayerPrefsRuntimeViewConstants.KeyLabelText;
            AddFixedHeight(keyLabel.gameObject, 30f);

            m_keyInputField = PlayerPrefsRuntimeUiFactory.CreateInputField(PlayerPrefsRuntimeViewConstants.KeyInputFieldName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.KeyPlaceholderText, false, resolvedFont, out GameObject keyFieldGo);
            AddFixedHeight(keyFieldGo, PlayerPrefsRuntimeViewConstants.AddDialogInputHeight);
            m_keyInputField.onValueChanged.AddListener(OnKeyInputChanged);

            Text typeLabel = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.ValueLabelName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogValueLabelFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false, resolvedFont, out _);
            typeLabel.text = PlayerPrefsRuntimeViewConstants.TypeLabelText;
            AddFixedHeight(typeLabel.gameObject, 30f);

            GameObject typeSelector = new GameObject(PlayerPrefsRuntimeViewConstants.TypeSelectorName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            typeSelector.transform.SetParent(contentRoot.transform, false);

            LayoutElement typeSelectorLayout = typeSelector.GetComponent<LayoutElement>();
            typeSelectorLayout.preferredHeight = PlayerPrefsRuntimeViewConstants.AddDialogInputHeight;
            typeSelectorLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup typeSelectorHlg = typeSelector.GetComponent<HorizontalLayoutGroup>();
            typeSelectorHlg.spacing = 14f;
            typeSelectorHlg.childControlWidth = true;
            typeSelectorHlg.childControlHeight = true;
            typeSelectorHlg.childForceExpandWidth = true;
            typeSelectorHlg.childForceExpandHeight = true;

            CreateTypeButton(TypeInt32, PlayerPrefsRuntimeViewConstants.TypeIntLabel, typeSelector.transform, resolvedFont);
            CreateTypeButton(TypeSingle, PlayerPrefsRuntimeViewConstants.TypeFloatLabel, typeSelector.transform, resolvedFont);
            CreateTypeButton(TypeString, PlayerPrefsRuntimeViewConstants.TypeStringLabel, typeSelector.transform, resolvedFont);
            UpdateTypeButtonVisuals();

            Text valueLabel = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.ValueLabelName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogValueLabelFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false, resolvedFont, out _);
            valueLabel.text = PlayerPrefsRuntimeViewConstants.ValueLabelText;
            AddFixedHeight(valueLabel.gameObject, 30f);

            m_valueInputField = PlayerPrefsRuntimeUiFactory.CreateInputField(PlayerPrefsRuntimeViewConstants.ValueComponentName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.ValuePlaceholderText, true, resolvedFont, out GameObject valueFieldGo);
            LayoutElement valueFieldLayout = valueFieldGo.AddComponent<LayoutElement>();
            valueFieldLayout.minHeight = 150f;
            valueFieldLayout.flexibleHeight = 1f;
            m_valueInputField.onValueChanged.AddListener(OnValueInputChanged);

            Text errorText = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.ErrorTextName, contentRoot.transform, 24, FontStyle.Bold, new Color(1f, 0.3f, 0.3f, 1f), TextAnchor.UpperLeft, false, resolvedFont, out m_errorTextObject);
            errorText.horizontalOverflow = HorizontalWrapMode.Wrap;
            errorText.verticalOverflow = VerticalWrapMode.Overflow;
            errorText.text = "";
            LayoutElement errorLayout = m_errorTextObject.AddComponent<LayoutElement>();
            errorLayout.preferredHeight = 0f;
            errorLayout.flexibleWidth = 1f;
            m_errorTextObject.SetActive(false);

            GameObject actions = new GameObject(PlayerPrefsRuntimeViewConstants.ActionsName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actions.transform.SetParent(contentRoot.transform, false);

            LayoutElement actionsLayout = actions.GetComponent<LayoutElement>();
            actionsLayout.preferredHeight = 70f;
            actionsLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup actionsHlg = actions.GetComponent<HorizontalLayoutGroup>();
            actionsHlg.childAlignment = TextAnchor.MiddleRight;
            actionsHlg.childControlWidth = true;
            actionsHlg.childForceExpandWidth = true;
            actionsHlg.childControlHeight = true;
            actionsHlg.childForceExpandHeight = true;
            actionsHlg.spacing = 18f;

            Button cancelButton = PlayerPrefsRuntimeUiFactory.CreateActionButton(PlayerPrefsRuntimeViewConstants.CancelButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.CancelLabel, resolvedFont, out _, out _);
            cancelButton.onClick.AddListener(Close);

            Button saveButton = PlayerPrefsRuntimeUiFactory.CreateActionButton(PlayerPrefsRuntimeViewConstants.SaveButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.SaveLabel, resolvedFont, out _, out GameObject saveLabelGo);
            saveButton.onClick.AddListener(OnSaveClicked);
            m_saveButtonLabel = saveLabelGo.GetComponent<Text>();

            PlayerPrefsRuntimeUiFactory.CreateDialogCloseButton(dialog.transform, resolvedFont, Close);

            m_dialogRoot = overlay;
        }

        public void Close()
        {
            if (m_dialogRoot == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(m_dialogRoot);
            m_dialogRoot = null;
            m_keyInputField = null;
            m_valueInputField = null;
            m_errorTextObject = null;
            m_saveButtonLabel = null;
            m_overwriteArmed = false;
            m_keyExists = null;
            m_typeButtons.Clear();
        }

        private void CreateTypeButton(string typeName, string label, Transform parent, Font font)
        {
            Button button = PlayerPrefsRuntimeUiFactory.CreateActionButton(typeName, parent, label, font, out _, out _);
            button.onClick.AddListener(() => OnTypeSelected(typeName));
            m_typeButtons[typeName] = button;
        }

        private void OnTypeSelected(string typeName)
        {
            if (string.Equals(m_selectedType, typeName, StringComparison.Ordinal))
            {
                return;
            }

            m_selectedType = typeName;
            DisarmOverwrite();
            HideError();
            UpdateTypeButtonVisuals();
        }

        private void UpdateTypeButtonVisuals()
        {
            foreach (KeyValuePair<string, Button> pair in m_typeButtons)
            {
                bool selected = string.Equals(pair.Key, m_selectedType, StringComparison.Ordinal);
                Button button = pair.Value;

                Image image = button.targetGraphic as Image;
                if (image != null)
                {
                    image.color = selected ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlNormalColor;
                }

                ColorBlock colors = button.colors;
                colors.normalColor = selected ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlNormalColor;
                colors.highlightedColor = selected ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
                colors.pressedColor = selected ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlPressedColor;
                button.colors = colors;

                Transform labelTransform = button.transform.Find(PlayerPrefsRuntimeViewConstants.LabelName);
                Text labelText = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
                if (labelText != null)
                {
                    labelText.color = selected ? PlayerPrefsRuntimeViewConstants.BadgeLabelColor : Color.white;
                }
            }
        }

        private void OnSaveClicked()
        {
            if (m_keyInputField == null || m_valueInputField == null)
            {
                return;
            }

            string key = m_keyInputField.text ?? string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                ShowError("Key cannot be empty.");
                return;
            }

            string rawValue = m_valueInputField.text ?? string.Empty;
            object typedValue;

            switch (m_selectedType)
            {
                case TypeInt32:
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        typedValue = intValue;
                    }
                    else
                    {
                        ShowError($"Invalid Int32 value: '{rawValue}'. Please enter a whole number (e.g., 42, -10).");
                        return;
                    }
                    break;

                case TypeSingle:
                    if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        typedValue = floatValue;
                    }
                    else
                    {
                        ShowError($"Invalid Float value: '{rawValue}'. Please enter a decimal number (e.g., 3.14, -2.5).");
                        return;
                    }
                    break;

                default:
                    typedValue = rawValue;
                    break;
            }

            // Warn if the key exists in the viewer's displayed list (which can include keys the
            // standard PlayerPrefs.HasKey API doesn't resolve) or in PlayerPrefs itself.
            bool keyAlreadyExists = (m_keyExists != null && m_keyExists(key)) || PlayerPrefs.HasKey(key);
            if (keyAlreadyExists && !m_overwriteArmed)
            {
                m_overwriteArmed = true;
                if (m_saveButtonLabel != null)
                {
                    m_saveButtonLabel.text = PlayerPrefsRuntimeViewConstants.OverwriteLabel;
                }

                ShowError(string.Format(PlayerPrefsRuntimeViewConstants.KeyExistsWarningFormat, key));
                return;
            }

            switch (m_selectedType)
            {
                case TypeInt32:
                    PlayerPrefsRuntimeWriter.SetInt(key, (int)typedValue);
                    break;
                case TypeSingle:
                    PlayerPrefsRuntimeWriter.SetFloat(key, (float)typedValue);
                    break;
                default:
                    PlayerPrefsRuntimeWriter.SetString(key, (string)typedValue);
                    break;
            }

            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry(key, typedValue);
            Action<PlayerPrefsRuntimeEntry> onEntryAdded = m_onEntryAdded;

            Close();
            onEntryAdded?.Invoke(entry);
        }

        private void OnKeyInputChanged(string value)
        {
            DisarmOverwrite();
            HideError();
        }

        private void OnValueInputChanged(string value)
        {
            HideError();
        }

        private void DisarmOverwrite()
        {
            m_overwriteArmed = false;
            if (m_saveButtonLabel != null)
            {
                m_saveButtonLabel.text = PlayerPrefsRuntimeViewConstants.SaveLabel;
            }
        }

        private void ShowError(string errorMessage)
        {
            if (m_errorTextObject == null)
            {
                return;
            }

            Text errorText = m_errorTextObject.GetComponent<Text>();
            if (errorText != null)
            {
                errorText.text = errorMessage;

                LayoutElement errorLayout = m_errorTextObject.GetComponent<LayoutElement>();
                if (errorLayout != null)
                {
                    errorLayout.preferredHeight = -1f;
                }
            }

            m_errorTextObject.SetActive(true);
        }

        private void HideError()
        {
            if (m_errorTextObject != null && m_errorTextObject.activeSelf)
            {
                m_errorTextObject.SetActive(false);
            }
        }

        private static void AddFixedHeight(GameObject target, float height)
        {
            LayoutElement layout = target.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }
    }
}
#endif
