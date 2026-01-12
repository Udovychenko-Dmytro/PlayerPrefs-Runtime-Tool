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
    /// Responsible for building a single entry row inside the runtime viewer.
    /// </summary>
    internal class PlayerPrefsRuntimeRow
    {
        public PlayerPrefsRuntimeRowView Create(
            Transform parent,
            Font font,
            Color32 badgeColor)
        {
            GameObject row = new GameObject(PlayerPrefsRuntimeViewConstants.RowName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);

            Image rowImage;
            Button rowButton;
            ConfigureRoot(row, out rowImage, out rowButton);

            Text nameText = BuildName(row.transform, font);
            Image badgeImage;
            Text badgeLabel;
            CreateTypeBadge(row.transform, font, badgeColor, out badgeImage, out badgeLabel);
            Text valueText = BuildValue(row.transform, font);

            AnimateEntry(row);

            PlayerPrefsRuntimeRowView view = row.AddComponent<PlayerPrefsRuntimeRowView>();
            view.Initialize(nameText, valueText, rowImage, badgeImage, badgeLabel, rowButton);
            return view;
        }

        public void Update(
            PlayerPrefsRuntimeRowView view,
            PlayerPrefsRuntimeEntry entry,
            bool even,
            Color32 evenColor,
            Color32 oddColor,
            Color32 badgeColor,
            Action<PlayerPrefsRuntimeEntry> onClick)
        {
            if (view == null)
            {
                return;
            }

            Color baseColor = even ? evenColor : oddColor;
            if (view.RowImage != null)
            {
                view.RowImage.color = baseColor;
            }

            if (view.BadgeImage != null)
            {
                view.BadgeImage.color = badgeColor;
            }

            string entryName = entry.Name;
            string entryValue = entry.Value;
            string entryType = entry.Type;

            if (view.NameText != null)
            {
                view.NameText.text = TruncateText(entryName, PlayerPrefsRuntimeViewConstants.MaxNameTextLength);
            }

            if (view.ValueText != null)
            {
                view.ValueText.text = string.IsNullOrEmpty(entryValue) ? PlayerPrefsRuntimeViewConstants.EmptyValueLabel : TruncateText(entryValue, PlayerPrefsRuntimeViewConstants.MaxValueTextLength);
            }

            if (view.BadgeLabel != null)
            {
                view.BadgeLabel.text = string.IsNullOrEmpty(entryType) ? PlayerPrefsRuntimeViewConstants.UnknownTypeLabel : entryType;
            }

            ApplyHoverEffect(view.Button, baseColor);
            view.Bind(entry, onClick);
        }

        private void ConfigureRoot(GameObject row, out Image rowImage, out Button rowButton)
        {
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.pivot = new Vector2(0.5f, 1f);

            Image image = row.GetComponent<Image>();
            image.color = Color.clear;

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = PlayerPrefsRuntimeViewConstants.RowMinHeight;

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = false;
            hlg.spacing = PlayerPrefsRuntimeViewConstants.RowSpacing;
            hlg.padding = new RectOffset(PlayerPrefsRuntimeViewConstants.RowPaddingHorizontal, PlayerPrefsRuntimeViewConstants.RowPaddingHorizontal, PlayerPrefsRuntimeViewConstants.RowPaddingVertical, PlayerPrefsRuntimeViewConstants.RowPaddingVertical);

            Button button = row.GetComponent<Button>();
            if (button == null)
            {
                button = row.AddComponent<Button>();
            }

            button.transition = Selectable.Transition.None;

            rowImage = image;
            rowButton = button;
        }

        private Text BuildName(Transform parent, Font font)
        {
            Text nameText = CreateText(PlayerPrefsRuntimeViewConstants.NameName, parent, PlayerPrefsRuntimeViewConstants.NameFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, font);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = PlayerPrefsRuntimeViewConstants.NameFlexibleWidth;
            nameLayout.minWidth = PlayerPrefsRuntimeViewConstants.NameMinWidth;
            nameLayout.preferredWidth = 0f;

            return nameText;
        }

        private Text BuildValue(Transform parent, Font font)
        {
            Text valueText = CreateText(PlayerPrefsRuntimeViewConstants.ValueName, parent, PlayerPrefsRuntimeViewConstants.ValueFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.MiddleRight, font);
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            valueText.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = PlayerPrefsRuntimeViewConstants.ValueFlexibleWidth;
            valueLayout.minWidth = PlayerPrefsRuntimeViewConstants.ValueMinWidth;
            valueLayout.preferredWidth = 0f;

            return valueText;
        }

        private Text CreateText(string name, Transform parent, int size, FontStyle style, Color color, TextAnchor anchor, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return text;
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            int suffixLength = PlayerPrefsRuntimeViewConstants.TextOverflowSuffix.Length;
            if (maxLength <= suffixLength)
            {
                return PlayerPrefsRuntimeViewConstants.TextOverflowSuffix;
            }

            return text.Substring(0, maxLength - suffixLength) + PlayerPrefsRuntimeViewConstants.TextOverflowSuffix;
        }

        private void CreateTypeBadge(Transform parent, Font font, Color32 badgeColor, out Image badgeImage, out Text badgeLabel)
        {
            GameObject badge = new GameObject(PlayerPrefsRuntimeViewConstants.TypeBadgeName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(parent, false);
            Image badgeImageComponent = badge.GetComponent<Image>();
            badgeImageComponent.color = badgeColor;
            badgeImageComponent.raycastTarget = false;

            Outline outline = badge.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.BadgeOutlineColor;
            outline.effectDistance = new Vector2(1f, -1f);

            LayoutElement badgeLayout = badge.GetComponent<LayoutElement>();
            badgeLayout.preferredWidth = PlayerPrefsRuntimeViewConstants.BadgePreferredWidth;
            badgeLayout.minWidth = PlayerPrefsRuntimeViewConstants.BadgeMinWidth;
            badgeLayout.preferredHeight = PlayerPrefsRuntimeViewConstants.BadgePreferredHeight;
            badgeLayout.minHeight = PlayerPrefsRuntimeViewConstants.BadgeMinHeight;
            badgeLayout.flexibleWidth = 0;
            badgeLayout.flexibleHeight = 0;

            RectTransform badgeRT = badge.GetComponent<RectTransform>();
            Vector2 middlePosition = new Vector2(0.5f, 0.5f);
            badgeRT.anchorMin = middlePosition;
            badgeRT.anchorMax = middlePosition;
            badgeRT.pivot = middlePosition;

            Text label = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, badge.transform, PlayerPrefsRuntimeViewConstants.BadgeFontSize, FontStyle.Bold, PlayerPrefsRuntimeViewConstants.BadgeLabelColor, TextAnchor.MiddleCenter, font);

            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = PlayerPrefsRuntimeViewConstants.BadgeShadowColor;
            shadow.effectDistance = new Vector2(1f, -1f);

            badgeImage = badgeImageComponent;
            badgeLabel = label;
        }

        private void ApplyHoverEffect(Button button, Color baseColor)
        {
            if (button == null)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.RowColorEven;
            colors.pressedColor = baseColor;
            colors.selectedColor = baseColor;
            colors.disabledColor = baseColor;
            colors.fadeDuration = 0.1f;

            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }

        private void AnimateEntry(GameObject row)
        {
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.localScale = new Vector2(0.95f, 0.95f);
        }
    }
}
#endif
