// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using UnityEngine;
using UnityEngine.UI;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Builds and shows a dialog with full PlayerPrefs entry details.
    /// </summary>
    internal class PlayerPrefsRuntimeEntryDialog
    {
        private GameObject m_dialogRoot;

        public void Show(Transform parent, PlayerPrefsRuntimeEntry entry, Font font, System.Action<PlayerPrefsRuntimeEntry> onEntryRemoved = null)
        {
            if (parent == null)
            {
                return;
            }

            Close();

            Font resolvedFont = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject overlay = new GameObject("EntryDetailsOverlay", typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsLastSibling();

            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
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

            GameObject dialog = new GameObject("EntryDetailsDialog", typeof(RectTransform), typeof(Image), typeof(Outline));
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

            GameObject contentRoot = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
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

            Text title = CreateText("Title", contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, true, resolvedFont, out _);
            if (title != null)
            {
                title.text = "PlayerPrefs Entry Details:";
                LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
                titleLayout.preferredHeight = 40f;
                titleLayout.flexibleHeight = 0;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = PlayerPrefsRuntimeViewConstants.DialogTitleResizeMinSize;
                title.resizeTextMaxSize = PlayerPrefsRuntimeViewConstants.DialogTitleResizeMaxSize;
            }

            Text keyText = CreateText("Key", contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogKeyFontSize, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, false, resolvedFont, out _);
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

            Text typeText = CreateText("Type", contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogTypeFontSize, FontStyle.BoldAndItalic, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.UpperLeft, false, resolvedFont, out _);
            if (typeText != null)
            {
                typeText.text = $"Type: {entry.Type}";
                LayoutElement typeLayout = typeText.gameObject.AddComponent<LayoutElement>();
                typeLayout.preferredHeight = 30f;
                typeLayout.flexibleHeight = 0;
            }

            Text valueLabel = CreateText("ValueLabel", contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogValueLabelFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, false, resolvedFont, out _);
            if (valueLabel != null)
            {
                valueLabel.text = "Value:";
                LayoutElement labelLayout = valueLabel.gameObject.AddComponent<LayoutElement>();
                labelLayout.preferredHeight = 30f;
                labelLayout.flexibleHeight = 0;
            }

            GameObject valueScrollGo = new GameObject("ValueScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            valueScrollGo.transform.SetParent(contentRoot.transform, false);

            RectTransform valueScrollRT = valueScrollGo.GetComponent<RectTransform>();
            valueScrollRT.anchorMin = new Vector2(0f, 0f);
            valueScrollRT.anchorMax = new Vector2(1f, 1f);
            valueScrollRT.pivot     = new Vector2(0.5f, 0.5f);
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

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
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

            GameObject valueContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
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

            Text valueText = CreateText("ValueText", valueContent.transform, PlayerPrefsRuntimeViewConstants.DialogValueFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.UpperLeft, false, resolvedFont, out _);
            if (valueText != null)
            {
                valueText.raycastTarget = false;
                valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
                valueText.verticalOverflow = VerticalWrapMode.Overflow;
                valueText.text = string.IsNullOrEmpty(entry.Value) ? "(empty)" : entry.Value;
                valueText.alignment = TextAnchor.UpperLeft;

                LayoutElement valueTextLayout = valueText.gameObject.AddComponent<LayoutElement>();
                valueTextLayout.flexibleWidth = 1f;
                valueTextLayout.flexibleHeight = 1f;
            }

            valueScroll.viewport = viewportRT;
            valueScroll.content = valueContentRT;

            GameObject actions = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
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

            Button copyButton = CreateActionButton("CopyButton", actions.transform, "Copy", resolvedFont, out _, out _);
            copyButton.onClick.AddListener(() => CopyEntryToClipboard(entry));

            Button removeButton = CreateActionButton("RemoveButton", actions.transform, "Remove", resolvedFont, out _, out GameObject removeButtonGoText);
            removeButton.onClick.AddListener(() => RemoveEntry(entry, onEntryRemoved));
            removeButtonGoText.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 100f);

            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
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

            Text closeText = CreateText("Label", closeGo.transform, PlayerPrefsRuntimeViewConstants.DialogCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, resolvedFont, out _);
            if (closeText != null)
            {
                closeText.text = "X";
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

            Text labelText = CreateText("Label", buttonGo.transform, PlayerPrefsRuntimeViewConstants.DialogCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, font, out textGo);
            labelText.text = label;

            return button;
        }

        private void CopyEntryToClipboard(PlayerPrefsRuntimeEntry entry)
        {
            string key = string.IsNullOrEmpty(entry.Name) ? "(Unnamed)" : entry.Name;
            string value = string.IsNullOrEmpty(entry.Value) ? "(empty)" : entry.Value;
            string type = string.IsNullOrEmpty(entry.Type) ? "Unknown" : entry.Type;

            GUIUtility.systemCopyBuffer = $"Key: {key}\nType: {type}\nValue: {value}";
        }

        private void RemoveEntry(PlayerPrefsRuntimeEntry entry, System.Action<PlayerPrefsRuntimeEntry> onEntryRemoved)
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
    }
}
#endif