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
        private readonly PlayerPrefsRuntimeConfirmDialog m_confirmDialog = new PlayerPrefsRuntimeConfirmDialog();

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

            Font resolvedFont = font != null ? font : PlayerPrefsRuntimeUiFactory.ResolveDefaultFont();

            GameObject overlay = PlayerPrefsRuntimeUiFactory.CreateOverlay(PlayerPrefsRuntimeViewConstants.OverlayName, parent, Close, 0.85f);
            GameObject dialog = PlayerPrefsRuntimeUiFactory.CreateDialogPanel(overlay.transform, PlayerPrefsRuntimeViewConstants.DialogName, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));

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
                typeText.text = string.Format(PlayerPrefsRuntimeViewConstants.EntrySizeFormat, entry.Type, PlayerPrefsRuntimeEntry.FormatByteSize(entry.EstimateValueSizeBytes()));
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
                valueText.text = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(entry.Value);
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

            Button copyButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.CopyButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.CopyAllLabel, resolvedFont, out _, out _);
            copyButton.onClick.AddListener(CopyEntryToClipboard);

            Button copyValueButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.CopyValueButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.CopyValueLabel, resolvedFont, out _, out _);
            copyValueButton.onClick.AddListener(CopyCurrentValueToClipboard);

            Button removeButton = CreateActionButton(PlayerPrefsRuntimeViewConstants.RemoveButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.RemoveLabel, resolvedFont, out _, out GameObject removeButtonGoText);
            removeButton.onClick.AddListener(() => m_confirmDialog.Show(
                overlay.transform,
                resolvedFont,
                string.Format(PlayerPrefsRuntimeViewConstants.RemoveConfirmTitleFormat, string.IsNullOrEmpty(entry.Name) ? PlayerPrefsRuntimeViewConstants.UnnamedLabel : entry.Name),
                PlayerPrefsRuntimeViewConstants.RemoveConfirmMessage,
                PlayerPrefsRuntimeViewConstants.DeleteLabel,
                () => RemoveEntry(entry, onEntryRemoved)));
            removeButtonGoText.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 100f);

            PlayerPrefsRuntimeUiFactory.CreateDialogCloseButton(dialog.transform, resolvedFont, Close);

            m_dialogRoot = overlay;
        }

        public void Close()
        {
            if (m_dialogRoot != null)
            {
                UnityEngine.Object.Destroy(m_dialogRoot);
            }

            m_dialogRoot = null;
            m_valueInputField = null;
            m_valueTextObject = null;
            m_editButton = null;
            m_saveButton = null;
            m_errorTextObject = null;
            m_isEditMode = false;
            m_currentEntry = default;
            m_onEntryUpdated = null;
        }

        private Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color, TextAnchor anchor, bool emphasize, Font font, out GameObject gameObject)
        {
            return PlayerPrefsRuntimeUiFactory.CreateText(name, parent, fontSize, style, color, anchor, emphasize, font, out gameObject);
        }

        private Button CreateActionButton(string name, Transform parent, string label, Font font, out GameObject buttonGo, out GameObject textGo)
        {
            return PlayerPrefsRuntimeUiFactory.CreateActionButton(name, parent, label, font, out buttonGo, out textGo);
        }

        private void CopyEntryToClipboard()
        {
            PlayerPrefsRuntimeEntry entry = m_currentEntry;
            string key = string.IsNullOrEmpty(entry.Name) ? PlayerPrefsRuntimeViewConstants.UnnamedLabel : entry.Name;
            string value = string.IsNullOrEmpty(entry.Value) ? PlayerPrefsRuntimeViewConstants.EmptyValueLabel : entry.Value;
            string type = string.IsNullOrEmpty(entry.Type) ? PlayerPrefsRuntimeViewConstants.UnknownTypeLabel : entry.Type;

            GUIUtility.systemCopyBuffer = $"Key: {key}\nType: {type}\nValue: {value}";
        }

        private void CopyCurrentValueToClipboard()
        {
            // Copies the raw value (not the pretty-printed form) so it round-trips exactly.
            GUIUtility.systemCopyBuffer = string.IsNullOrEmpty(m_currentEntry.Value) ? string.Empty : m_currentEntry.Value;
        }

        private void RemoveEntry(PlayerPrefsRuntimeEntry entry, Action<PlayerPrefsRuntimeEntry> onEntryRemoved)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Cannot remove PlayerPref with empty key.");
                return;
            }

            PlayerPrefsRuntimeWriter.DeleteKey(entry.Name);

            onEntryRemoved?.Invoke(entry);
            Close();
        }

        private void ToggleEditMode()
        {
            // Block entering edit mode for values too large for the legacy UI Text-backed
            // InputField to render; the tail would be lost or the field would fail to draw.
            // Such values are edited through Import or the public API instead.
            if (!m_isEditMode && !PlayerPrefsRuntimeValueDisplay.IsEditableInline(m_currentEntry.Value, out string editBlockedMessage))
            {
                ShowError(editBlockedMessage);
                return;
            }

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

            GameObject dialogRoot = m_dialogRoot;
            InputField valueInputField = m_valueInputField;
            string newValue = valueInputField.text;
            string key = m_currentEntry.Name;
            string type = m_currentEntry.Type;

            if (string.IsNullOrEmpty(key))
            {
                ShowError("Cannot save PlayerPref with empty key.");
                return;
            }

            PlayerPrefsRuntimeEntry updatedEntry;
            switch (type)
            {
                case "Int32":
                    if (int.TryParse(newValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int intValue))
                    {
                        PlayerPrefsRuntimeWriter.SetInt(key, intValue);
                        updatedEntry = new PlayerPrefsRuntimeEntry(key, intValue);
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
                        PlayerPrefsRuntimeWriter.SetFloat(key, floatValue);
                        updatedEntry = new PlayerPrefsRuntimeEntry(key, floatValue);
                    }
                    else
                    {
                        ShowError($"Invalid Float value: '{newValue}'. Please enter a decimal number (e.g., 3.14, -2.5).");
                        return;
                    }
                    break;

                case "String":
                    PlayerPrefsRuntimeWriter.SetString(key, newValue);
                    updatedEntry = new PlayerPrefsRuntimeEntry(key, newValue);
                    break;

                default:
                    ShowError($"Unsupported type: {type}");
                    return;
            }

            // Writer notifications are synchronous and user handlers may close or rebuild the
            // viewer. Do not touch stale Unity objects if that happened during the write.
            if (m_dialogRoot == null
                || !ReferenceEquals(m_dialogRoot, dialogRoot)
                || m_valueInputField == null
                || !ReferenceEquals(m_valueInputField, valueInputField))
            {
                return;
            }

            m_currentEntry = updatedEntry;
            if (m_valueTextObject != null)
            {
                Text valueText = m_valueTextObject.GetComponent<Text>();
                if (valueText != null)
                {
                    valueText.text = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(m_currentEntry.Value);
                }
            }

            valueInputField.text = m_currentEntry.Value ?? string.Empty;
            Action<PlayerPrefsRuntimeEntry> onEntryUpdated = m_onEntryUpdated;
            ToggleEditMode();
            onEntryUpdated?.Invoke(m_currentEntry);
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
