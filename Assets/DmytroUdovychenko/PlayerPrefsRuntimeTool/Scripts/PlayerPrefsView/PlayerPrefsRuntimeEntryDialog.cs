// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Builds and shows a dialog with full PlayerPrefs entry details.
    /// </summary>
    internal class PlayerPrefsRuntimeEntryDialog
    {
        private bool m_isEditMode;
        private GameObject m_dialogRoot;
        private InputField m_valueInputField;
        private GameObject m_valueTextObject;
        private GameObject m_editButton;
        private GameObject m_saveButton;
        private GameObject m_errorTextObject;
        private PlayerPrefsRuntimeEntry m_currentEntry;
        private Action<PlayerPrefsRuntimeEntry> m_onEntryUpdated;

        public void Show(Transform parent, PlayerPrefsRuntimeEntry entry, Font font, Action<PlayerPrefsRuntimeEntry> onEntryRemoved = null, Action<PlayerPrefsRuntimeEntry> onEntryUpdated = null)
        {
            if (parent == null)
            {
                return;
            }

            Close();

            m_currentEntry = entry;
            m_onEntryUpdated = onEntryUpdated;

            Font resolvedFont = font != null ? font : Resources.GetBuiltinResource<Font>(PlayerPrefsRuntimeViewConstants.ArialFontName);

            GameObject overlay = new GameObject(PlayerPrefsRuntimeViewConstants.OverlayName, typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsLastSibling();

            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            GameObject backdrop = new GameObject(PlayerPrefsRuntimeViewConstants.BackdropName, typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(overlay.transform, false);

            RectTransform backdropRT = backdrop.GetComponent<RectTransform>();
            backdropRT.anchorMin = Vector2.zero;
            backdropRT.anchorMax = Vector2.one;
            backdropRT.offsetMin = Vector2.zero;
            backdropRT.offsetMax = Vector2.zero;

            Image backdropImage = backdrop.GetComponent<Image>();
            backdropImage.color = new Color(PlayerPrefsRuntimeViewConstants.BackdropColor.r, PlayerPrefsRuntimeViewConstants.BackdropColor.g, PlayerPrefsRuntimeViewConstants.BackdropColor.b, 0.85f);

            Button backdropButton = backdrop.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(Close);

            GameObject dialog = new GameObject(PlayerPrefsRuntimeViewConstants.DialogName, typeof(RectTransform), typeof(Image), typeof(Outline));
            dialog.transform.SetParent(overlay.transform, false);
            dialog.transform.SetAsLastSibling();

            RectTransform dialogRT = dialog.GetComponent<RectTransform>();
            dialogRT.anchorMin = new Vector2(0.02f, 0.02f);
            dialogRT.anchorMax = new Vector2(0.98f, 0.98f);
            dialogRT.offsetMin = Vector2.zero;
            dialogRT.offsetMax = Vector2.zero;
            dialogRT.pivot = new Vector2(0.5f, 0.5f);

            Image dialogImage = dialog.GetComponent<Image>();
            dialogImage.color = PlayerPrefsRuntimeViewConstants.PanelColor;

            Outline dialogOutline = dialog.GetComponent<Outline>();
            dialogOutline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            dialogOutline.effectDistance = new Vector2(3f, -3f);

            GameObject contentRoot = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentRoot.transform.SetParent(dialog.transform, false);
            RectTransform contentRT = contentRoot.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(15f, 15f);
            contentRT.offsetMax = new Vector2(-15f, -30f);

            VerticalLayoutGroup vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 25f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            Text title = CreateText(PlayerPrefsRuntimeViewConstants.TitleName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, true, resolvedFont, out _);
            if (title != null)
            {
                title.text = PlayerPrefsRuntimeViewConstants.DialogTitleText;
                LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
                titleLayout.preferredHeight = 40f;
                titleLayout.flexibleHeight = 0;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = PlayerPrefsRuntimeViewConstants.DialogTitleResizeMinSize;
                title.resizeTextMaxSize = PlayerPrefsRuntimeViewConstants.DialogTitleResizeMaxSize;
            }

            Text keyText = CreateText(PlayerPrefsRuntimeViewConstants.KeyName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogKeyFontSize, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, false, resolvedFont, out _);
            if (keyText != null)
            {
                keyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                keyText.verticalOverflow = VerticalWrapMode.Overflow;
                keyText.text = $"{entry.Name}";
                LayoutElement keyLayout = keyText.gameObject.AddComponent<LayoutElement>();
                keyLayout.preferredHeight = 15f;
                keyLayout.flexibleHeight = 0;
                keyLayout.layoutPriority = -1;
            }

            Text typeText = CreateText(PlayerPrefsRuntimeViewConstants.TypeBadgeName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogTypeFontSize, FontStyle.BoldAndItalic, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.UpperLeft, false, resolvedFont, out _);
            if (typeText != null)
            {
                typeText.text = $"Type: {entry.Type}";
                LayoutElement typeLayout = typeText.gameObject.AddComponent<LayoutElement>();
                typeLayout.preferredHeight = 30f;
                typeLayout.flexibleHeight = 0;
            }

            Text valueLabel = CreateText(PlayerPrefsRuntimeViewConstants.ValueLabelName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogValueLabelFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false, resolvedFont, out _);
            if (valueLabel != null)
            {
                valueLabel.text = PlayerPrefsRuntimeViewConstants.ValueLabelText;
                LayoutElement labelLayout = valueLabel.gameObject.AddComponent<LayoutElement>();
                labelLayout.preferredHeight = 30f;
                labelLayout.flexibleHeight = 0;
            }

            GameObject valueScrollGo = new GameObject(PlayerPrefsRuntimeViewConstants.ValueScrollName, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            valueScrollGo.transform.SetParent(contentRoot.transform, false);

            RectTransform valueScrollRT = valueScrollGo.GetComponent<RectTransform>();
            valueScrollRT.anchorMin = new Vector2(0f, 0f);
            valueScrollRT.anchorMax = new Vector2(1f, 1f);
            valueScrollRT.pivot = new Vector2(0.5f, 0.5f);
            valueScrollRT.offsetMin = new Vector2(0f, 0f);
            valueScrollRT.offsetMax = new Vector2(0f, -135f);

            Image valueScrollBg = valueScrollGo.GetComponent<Image>();
            valueScrollBg.color = PlayerPrefsRuntimeViewConstants.ScrollBackgroundColor;

            LayoutElement valueScrollLayout = valueScrollGo.GetComponent<LayoutElement>();
            valueScrollLayout.flexibleHeight = 1;
            valueScrollLayout.minHeight = 150f;

            ScrollRect valueScroll = valueScrollGo.GetComponent<ScrollRect>();
            valueScroll.horizontal = false;
            valueScroll.vertical = true;
            valueScroll.movementType = ScrollRect.MovementType.Clamped;
            valueScroll.scrollSensitivity = PlayerPrefsRuntimeViewConstants.ScrollSensitivity;
            valueScroll.inertia = true;
            valueScroll.decelerationRate = PlayerPrefsRuntimeViewConstants.ScrollDecelerationRate;

            GameObject viewportGo = new GameObject(PlayerPrefsRuntimeViewConstants.ViewportName, typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(valueScrollGo.transform, false);

            RectTransform viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(4f, 4f);
            viewportRT.offsetMax = new Vector2(-4f, -4f);

            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = PlayerPrefsRuntimeViewConstants.ViewportBackgroundColor;

            Mask viewportMask = viewportGo.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject valueContent = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            valueContent.transform.SetParent(viewportGo.transform, false);

            RectTransform valueContentRT = valueContent.GetComponent<RectTransform>();
            valueContentRT.anchorMin = new Vector2(0f, 1f);
            valueContentRT.anchorMax = new Vector2(1f, 1f);
            valueContentRT.pivot = new Vector2(0.5f, 1f);
            valueContentRT.anchoredPosition = Vector2.zero;
            valueContentRT.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup valueVLG = valueContent.GetComponent<VerticalLayoutGroup>();
            valueVLG.childAlignment = TextAnchor.UpperLeft;
            valueVLG.padding = new RectOffset(4, 4, 4, 4);
            valueVLG.childControlWidth = true;
            valueVLG.childForceExpandWidth = true;
            valueVLG.childControlHeight = true;
            valueVLG.childForceExpandHeight = false;
            valueVLG.spacing = 4;

            ContentSizeFitter valueContentFitter = valueContent.GetComponent<ContentSizeFitter>();
            valueContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            valueContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            Text valueText = CreateText(PlayerPrefsRuntimeViewConstants.ValueTextName, valueContent.transform, PlayerPrefsRuntimeViewConstants.DialogValueFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.UpperLeft, false, resolvedFont, out m_valueTextObject);
            if (valueText != null)
            {
                valueText.raycastTarget = false;
                valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
                valueText.verticalOverflow = VerticalWrapMode.Overflow;
                valueText.text = string.IsNullOrEmpty(entry.Value) ? PlayerPrefsRuntimeViewConstants.EmptyValueLabel : entry.Value;
                valueText.alignment = TextAnchor.UpperLeft;

                LayoutElement valueTextLayout = valueText.gameObject.AddComponent<LayoutElement>();
                valueTextLayout.flexibleWidth = 1f;
                valueTextLayout.flexibleHeight = 1f;
            }

            GameObject inputFieldGo = new GameObject(PlayerPrefsRuntimeViewConstants.ValueComponentName, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            inputFieldGo.transform.SetParent(valueContent.transform, false);
            inputFieldGo.SetActive(false);

            RectTransform inputFieldRT = inputFieldGo.GetComponent<RectTransform>();
            inputFieldRT.sizeDelta = new Vector2(0f, 300f);

            Image inputFieldImage = inputFieldGo.GetComponent<Image>();
            inputFieldImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            m_valueInputField = inputFieldGo.GetComponent<InputField>();
            m_valueInputField.lineType = InputField.LineType.MultiLineNewline;
            m_valueInputField.textComponent = CreateText(PlayerPrefsRuntimeViewConstants.TextComponentName, inputFieldGo.transform, PlayerPrefsRuntimeViewConstants.DialogValueFontSize, FontStyle.Normal, Color.white, TextAnchor.UpperLeft, false, resolvedFont, out _);
            m_valueInputField.text = string.IsNullOrEmpty(entry.Value) ? "" : entry.Value;
            m_valueInputField.onValueChanged.AddListener(OnInputValueChanged);

            LayoutElement inputFieldLayout = inputFieldGo.GetComponent<LayoutElement>();
            inputFieldLayout.minHeight = 300f;
            inputFieldLayout.preferredHeight = 300f;
            inputFieldLayout.flexibleWidth = 1f;
            inputFieldLayout.flexibleHeight = 1f;

            RectTransform inputTextRT = m_valueInputField.textComponent.GetComponent<RectTransform>();
            inputTextRT.anchorMin = Vector2.zero;
            inputTextRT.anchorMax = Vector2.one;
            inputTextRT.offsetMin = new Vector2(10f, 10f);
            inputTextRT.offsetMax = new Vector2(-10f, -10f);

            Text inputTextComponent = m_valueInputField.textComponent;
            inputTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            Text errorText = CreateText(PlayerPrefsRuntimeViewConstants.ErrorTextName, valueContent.transform, 24, FontStyle.Bold, new Color(1f, 0.3f, 0.3f, 1f), TextAnchor.UpperLeft, false, resolvedFont, out m_errorTextObject);
            if (errorText != null)
            {
                errorText.horizontalOverflow = HorizontalWrapMode.Wrap;
                errorText.verticalOverflow = VerticalWrapMode.Overflow;
                errorText.text = "";

                LayoutElement errorLayout = errorText.gameObject.AddComponent<LayoutElement>();
                errorLayout.preferredHeight = 0f;
                errorLayout.flexibleWidth = 1f;

                m_errorTextObject.SetActive(false);
            }

            valueScroll.viewport = viewportRT;
            valueScroll.content = valueContentRT;

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

            Button editButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.EditButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.EditLabel, resolvedFont, out m_editButton, out _);
            editButton.onClick.AddListener(ToggleEditMode);

            Button saveButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.SaveButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.SaveLabel, resolvedFont, out m_saveButton, out _);
            saveButton.onClick.AddListener(SaveEntry);
            m_saveButton.SetActive(false);

            Button copyButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.CopyButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.CopyLabel, resolvedFont, out _, out _);
            copyButton.onClick.AddListener(() => CopyEntryToClipboard(entry));

            Button removeButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.RemoveButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.RemoveLabel, resolvedFont, out _, out GameObject removeButtonGoText);
            removeButton.onClick.AddListener(() => RemoveEntry(entry, onEntryRemoved));
            removeButtonGoText.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 100f);

            GameObject closeGo = new GameObject(PlayerPrefsRuntimeViewConstants.CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(dialog.transform, false);
            RectTransform closeRT = closeGo.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot = new Vector2(1f, 1f);
            closeRT.sizeDelta = new Vector2(64f, 64f);
            closeRT.anchoredPosition = new Vector2(-18f, -18f);

            Image closeImage = closeGo.GetComponent<Image>();
            closeImage.color = PlayerPrefsRuntimeViewConstants.CloseButtonNormalColor;

            Button closeButton = closeGo.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);

            ColorBlock closeColors = closeButton.colors;
            closeColors.highlightedColor = PlayerPrefsRuntimeViewConstants.CloseButtonHighlightedColor;
            closeColors.pressedColor = PlayerPrefsRuntimeViewConstants.CloseButtonPressedColor;
            closeColors.fadeDuration = 0.1f;
            closeButton.colors = closeColors;

            Text closeText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, closeGo.transform, PlayerPrefsRuntimeViewConstants.DialogCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, resolvedFont, out _);
            if (closeText != null)
            {
                closeText.text = PlayerPrefsRuntimeViewConstants.CloseButtonText;
            }

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
        }

        private Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color, TextAnchor anchor, bool emphasize, Font font, out GameObject gameObject)
        {
            gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);

            Text text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            if (emphasize)
            {
                Shadow shadow = gameObject.AddComponent<Shadow>();
                shadow.effectColor = PlayerPrefsRuntimeViewConstants.TextShadowColor;
                shadow.effectDistance = new Vector2(1.8f, -1.8f);
            }

            return text;
        }

        private Button CreateActionButton(string name, Transform parent, string label, Font font, out GameObject buttonGo, out GameObject textGo)
        {
            buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(parent, false);

            RectTransform buttonRT = buttonGo.GetComponent<RectTransform>();
            buttonRT.sizeDelta = new Vector2(0f, 60f);

            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;

            ColorBlock colors = button.colors;
            colors.normalColor = PlayerPrefsRuntimeViewConstants.ControlNormalColor;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            LayoutElement layout = buttonGo.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredHeight = 60f;
            layout.minWidth = 0f;

            Text labelText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, buttonGo.transform, PlayerPrefsRuntimeViewConstants.DialogCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, font, out textGo);
            labelText.text = label;

            return button;
        }

        private void CopyEntryToClipboard(PlayerPrefsRuntimeEntry entry)
        {
            string key = string.IsNullOrEmpty(entry.Name) ? PlayerPrefsRuntimeViewConstants.UnnamedLabel : entry.Name;
            string value = string.IsNullOrEmpty(entry.Value) ? PlayerPrefsRuntimeViewConstants.EmptyValueLabel : entry.Value;
            string type = string.IsNullOrEmpty(entry.Type) ? PlayerPrefsRuntimeViewConstants.UnknownTypeLabel : entry.Type;

            GUIUtility.systemCopyBuffer = $"Key: {key}\nType: {type}\nValue: {value}";
        }

        private void RemoveEntry(PlayerPrefsRuntimeEntry entry, Action<PlayerPrefsRuntimeEntry> onEntryRemoved)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Cannot remove PlayerPref with empty key.");
                return;
            }

            PlayerPrefs.DeleteKey(entry.Name);
            PlayerPrefs.Save();

            onEntryRemoved?.Invoke(entry);
            Close();
        }

        private void ToggleEditMode()
        {
            m_isEditMode = !m_isEditMode;

            if (m_valueTextObject != null)
            {
                m_valueTextObject.SetActive(!m_isEditMode);
            }

            if (m_valueInputField != null)
            {
                m_valueInputField.gameObject.SetActive(m_isEditMode);
            }

            if (m_editButton != null)
            {
                m_editButton.SetActive(!m_isEditMode);
            }

            if (m_saveButton != null)
            {
                m_saveButton.SetActive(m_isEditMode);
            }

            if (m_errorTextObject != null)
            {
                m_errorTextObject.SetActive(false);
            }
        }

        private void SaveEntry()
        {
            if (m_valueInputField == null)
            {
                return;
            }

            string newValue = m_valueInputField.text;
            string key = m_currentEntry.Name;

            if (string.IsNullOrEmpty(key) || key == PlayerPrefsRuntimeViewConstants.UnnamedLabel)
            {
                ShowError("Cannot save PlayerPref with empty key.");
                return;
            }

            switch (m_currentEntry.Type)
            {
                case "Int32":
                    if (int.TryParse(newValue, out int intValue))
                    {
                        PlayerPrefs.SetInt(key, intValue);
                        PlayerPrefs.Save();
                        m_currentEntry = new PlayerPrefsRuntimeEntry(key, intValue);
                    }
                    else
                    {
                        ShowError($"Invalid Int32 value: '{newValue}'. Please enter a whole number (e.g., 42, -10).");
                        return;
                    }
                    break;

                case "Single":
                    if (float.TryParse(newValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                    {
                        PlayerPrefs.SetFloat(key, floatValue);
                        PlayerPrefs.Save();
                        m_currentEntry = new PlayerPrefsRuntimeEntry(key, floatValue);
                    }
                    else
                    {
                        ShowError($"Invalid Float value: '{newValue}'. Please enter a decimal number (e.g., 3.14, -2.5).");
                        return;
                    }
                    break;

                case "String":
                    PlayerPrefs.SetString(key, newValue);
                    PlayerPrefs.Save();
                    m_currentEntry = new PlayerPrefsRuntimeEntry(key, newValue);
                    break;

                default:
                    ShowError($"Unsupported type: {m_currentEntry.Type}");
                    return;
            }

            if (m_valueTextObject != null)
            {
                Text valueText = m_valueTextObject.GetComponent<Text>();
                if (valueText != null)
                {
                    valueText.text = string.IsNullOrEmpty(newValue) ? PlayerPrefsRuntimeViewConstants.EmptyValueLabel : newValue;
                }
            }

            m_onEntryUpdated?.Invoke(m_currentEntry);

            ToggleEditMode();
        }

        private void ShowError(string errorMessage)
        {
            if (m_errorTextObject != null)
            {
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
        }

        private void OnInputValueChanged(string value)
        {
            if (m_errorTextObject != null && m_errorTextObject.activeSelf)
            {
                m_errorTextObject.SetActive(false);
            }
        }
    }
}
#endif