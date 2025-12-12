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
    /// Responsible for building a single entry row inside the runtime viewer.
    /// </summary>
    internal class PlayerPrefsRuntimeRow
    {

        public GameObject Create(
            Transform parent,
            PlayerPrefsRuntimeEntry entry,
            bool even,
            Font font,
            Color32 evenColor,
            Color32 oddColor,
            Color32 badgeColor,
            System.Action<PlayerPrefsRuntimeEntry> onClick = null)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);

            ConfigureRoot(row, even ? evenColor : oddColor);
            BuildName(entry, row.transform, font);
            CreateTypeBadge(row.transform, entry.Type, font, badgeColor);
            BuildValue(entry, row.transform, font);
            
            AddHoverEffect(row, even ? evenColor : oddColor);
            AddClickHandler(row, entry, onClick);
            AnimateEntry(row);

            return row;
        }

        private void ConfigureRoot(GameObject row, Color color)
        {
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.pivot = new Vector2(0.5f, 1f);

            Image image = row.GetComponent<Image>();
            image.color = color;

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
            
            // Add a subtle hover effect
            Button button = row.AddComponent<Button>();
            button.transition = Selectable.Transition.None; // Disable default transition
        }

        private void BuildName(PlayerPrefsRuntimeEntry entry, Transform parent, Font font)
        {
            Text nameText = CreateText("Name", parent, PlayerPrefsRuntimeViewConstants.NameFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, font);
            nameText.text = TruncateText(entry.Name, PlayerPrefsRuntimeViewConstants.MaxNameTextLength);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Overflow; // Allow text to expand vertically
            
            LayoutElement nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = PlayerPrefsRuntimeViewConstants.NameFlexibleWidth;
            nameLayout.minWidth = PlayerPrefsRuntimeViewConstants.NameMinWidth;
            nameLayout.preferredWidth = 0f;
        }

        private void BuildValue(PlayerPrefsRuntimeEntry entry, Transform parent, Font font)
        {
            Text valueText = CreateText("Value", parent, PlayerPrefsRuntimeViewConstants.ValueFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.MiddleRight, font);
            valueText.text = string.IsNullOrEmpty(entry.Value) ? "(empty)" : TruncateText(entry.Value, PlayerPrefsRuntimeViewConstants.MaxValueTextLength);
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            valueText.verticalOverflow = VerticalWrapMode.Overflow; // Allow text to expand vertically
            
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = PlayerPrefsRuntimeViewConstants.ValueFlexibleWidth;
            valueLayout.minWidth = PlayerPrefsRuntimeViewConstants.ValueMinWidth;
            valueLayout.preferredWidth = 0f;
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

        /// <summary>
        /// Truncates text to the specified maximum length, adding an ellipsis if truncated.
        /// </summary>
        /// <param name="text">The text to truncate</param>
        /// <param name="maxLength">The maximum length of the text</param>
        /// <returns>Truncated text with ellipsis if needed</returns>
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
                
            // Ensure we have enough space for the suffix
            int suffixLength = PlayerPrefsRuntimeViewConstants.TextOverflowSuffix.Length;
            if (maxLength <= suffixLength)
                return PlayerPrefsRuntimeViewConstants.TextOverflowSuffix;
                
            return text.Substring(0, maxLength - suffixLength) + PlayerPrefsRuntimeViewConstants.TextOverflowSuffix;
        }

        private void CreateTypeBadge(Transform parent, string type, Font font, Color32 badgeColor)
        {
            GameObject badge = new GameObject("TypeBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(parent, false);
            Image badgeImage = badge.GetComponent<Image>();
            badgeImage.color = badgeColor;
            badgeImage.raycastTarget = false;
            
            // Add rounded corners effect
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

            Text label = CreateText("Label", badge.transform, PlayerPrefsRuntimeViewConstants.BadgeFontSize, FontStyle.Bold, PlayerPrefsRuntimeViewConstants.BadgeLabelColor, TextAnchor.MiddleCenter, font);
            label.text = string.IsNullOrEmpty(type) ? "Unknown" : type;
            
            // Add shadow effect to badge text
            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = PlayerPrefsRuntimeViewConstants.BadgeShadowColor;
            shadow.effectDistance = new Vector2(1f, -1f);
        }
        
        private void AddHoverEffect(GameObject row, Color baseColor)
        {
            Button button = row.GetComponent<Button>();
            if (button == null) return;
            
            // Create color blocks for normal and highlighted states
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
        
        private void AddClickHandler(GameObject row, PlayerPrefsRuntimeEntry entry, System.Action<PlayerPrefsRuntimeEntry> onClick)
        {
            if (onClick == null) return;
            
            Button button = row.GetComponent<Button>();
            if (button == null) return;
            
            button.onClick.AddListener(() => onClick(entry));
        }
        
        private void AnimateEntry(GameObject row)
        {
            // TODO: Add a subtle scale animation when the row is created
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.localScale = new Vector2(0.95f, 0.95f);
        }
    }
}
#endif