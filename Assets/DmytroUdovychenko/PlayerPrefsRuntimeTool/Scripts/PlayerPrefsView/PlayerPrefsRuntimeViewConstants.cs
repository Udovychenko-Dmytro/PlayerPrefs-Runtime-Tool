// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Centralized constants for the PlayerPrefsRuntime UI.
    /// Provides consistent values for colors,
    /// sizes, and other UI parameters across all UI components.
    /// </summary>
    internal static class PlayerPrefsRuntimeViewConstants
    {
        // Panel colors
        public static readonly Color PanelColor = new Color(0.15f, 0.20f, 0.25f, 0.95f);
        public static readonly Color HeaderColor = new Color(0.10f, 0.20f, 0.30f, 1f);
        public static readonly Color AccentColor = new Color(0f, 0.78f, 1f, 1f);
        public static readonly Color RowColorEven = new Color(0.31f, 0.37f, 0.46f, 0.90f);
        public static readonly Color RowColorOdd = new Color(0.1f, 0.15f, 0.20f, 0.90f);
        public static readonly Color BadgeColor = new Color(0f, 0.70f, 1f, 0.60f);

        // Text colors
        public static readonly Color ValueTextColor = new Color(0.90f, 0.94f, 1f, 1f);
        public static readonly Color BadgeLabelColor = new Color(0.04f, 0.12f, 0.20f, 1f);

        // Control colors
        public static readonly Color ControlNormalColor = new Color(0.15f, 0.22f, 0.33f, 1f);
        public static readonly Color ControlHighlightedColor = new Color(0.20f, 0.30f, 0.40f, 1f);
        public static readonly Color ControlPressedColor = new Color(0.12f, 0.20f, 0.30f, 1f);
        public static readonly Color CloseButtonNormalColor = new Color(0.78f, 0.20f, 0.20f, 0.78f);
        public static readonly Color CloseButtonHighlightedColor = new Color(0.85f, 0.27f, 0.27f, 1f);
        public static readonly Color CloseButtonPressedColor = new Color(0.70f, 0.15f, 0.15f, 1f);
        public static readonly Color SearchFieldHighlightedColor = new Color(0.18f, 0.25f, 0.37f, 1f);
        public static readonly Color SearchFieldColor = new Color(0.10f, 0.10f, 0.10f, 1f);

        // Effect colors
        public static readonly Color OutlineEffectColor = new Color(0f, 0f, 0f, 0.47f);
        public static readonly Color SeparatorColor = new Color(0f, 0.60f, 0.78f, 0.40f);
        public static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color ScrollBackgroundColor = new Color(1f, 1f, 1f, 0.04f);
        public static readonly Color ViewportBackgroundColor = new Color(0, 0, 0, 0.2f);
        public static readonly Color PlaceholderTextColor = new Color(0.78f, 0.78f, 0.78f, 0.59f);
        public static readonly Color TextShadowColor = new Color(0, 0, 0, 0.65f);
        public static readonly Color BadgeShadowColor = new Color(0, 0, 0, 0.5f);
        public static readonly Color BadgeOutlineColor = new Color(0f, 0f, 0f, 0.31f);
        public static readonly Color InnerShadowColor = new Color(0, 0, 0, 0.3f);

        // Common UI element names
        public const string ViewerCanvasName = "PlayerPrefsRuntimeCanvas";
        public const string PanelName = "ViewerPanel";
        public const string BackdropName = "Backdrop";
        public const string SortModeButtonName = "SortModeButton";
        public const string ToolName = "PlayerPrefs Runtime Tool (Viewer and Editor)";
        public const float  SortModeButtonWidth = 210f;
        public const float  SortModeButtonHeight = 150f;
        public const int    SortModeButtonLabelFontSize = 30;

        // UI element names
        public const string HeaderName = "Header";
        public const string SubtitleName = "Subtitle";
        public const string TitleName = "Title";
        public const string LabelName = "Label";
        public const string SearchFieldName = "SearchField";
        public const string ScrollViewName = "ScrollView";
        public const string ViewportName = "Viewport";
        public const string ContentName = "Content";
        public const string TopBarName = "TopBar";
        public const string AccentBarName = "AccentBar";
        public const string SeparatorName = "Separator";
        public const string TextAreaName = "TextArea";
        public const string PlaceholderName = "Placeholder";
        public const string TextComponentName = "Text";
        public const string ClearButtonName = "ClearButton";
        public const string CloseButtonName = "CloseButton";
        public const string EventSystemName = "EventSystem";
        public const string LegacyFontName = "LegacyRuntime.ttf";
        public const string ArialFontName = "Arial.ttf";
        public const string DefaultFontName = "Arial";
        public const string CloseButtonText = "X";
        public const string RowName = "Row";
        public const string TypeBadgeName = "TypeBadge";
        public const string EmptyValueLabel = "(empty)";
        public const string UnknownTypeLabel = "Unknown";
        public const string NameName = "Name";
        public const string ValueName = "Value";
        public const string ValueComponentName = "ValueInputField";
        public const string OverlayName = "EntryDetailsOverlay";
        public const string DialogName = "EntryDetailsDialog";
        public const string KeyName = "Key";
        public const string ValueLabelName = "ValueLabel";
        public const string ValueScrollName = "ValueScroll";
        public const string ValueTextName = "ValueText";
        public const string ErrorTextName = "ErrorText";
        public const string ActionsName = "Actions";
        public const string EditButtonName = "EditButton";
        public const string SaveButtonName = "SaveButton";
        public const string CopyButtonName = "CopyButton";
        public const string RemoveButtonName = "RemoveButton";
        public const string UnnamedLabel = "(Unnamed)";
        public const string DialogTitleText = "PlayerPrefs Entry Details:";
        public const string ValueLabelText = "Value:";
        public const string EditLabel = "Edit";
        public const string SaveLabel = "Save";
        public const string CopyLabel = "Copy";
        public const string RemoveLabel = "Remove";

        // UI Constants for PlayerPrefsRuntimeViewer
        public const float ScrollSensitivity = 45f;
        public const float ScrollDecelerationRate = 0.13f;
        public const float CanvasMatchWidthOrHeight = 0.5f;
        public const float SearchDebounceTime = 0.5f;

        public const float PanelAnchorMinX = 0f;
        public const float PanelAnchorMinY = 0f;
        public const float PanelAnchorMaxX = 1f;
        public const float PanelAnchorMaxY = 1f;

        // Row constants for PlayerPrefsRuntimeRow
        public const float RowMinHeight = 90f;
        public const float RowSpacing = 20f;
        public const int   RowPaddingHorizontal = 26;
        public const int   RowPaddingVertical = 18;

        public const int   NameFontSize = 30;
        public const float NameFlexibleWidth = 3f;
        public const float NameMinWidth = 260f;

        public const int   ValueFontSize = 28;
        public const float ValueFlexibleWidth = 3f;
        public const float ValueMinWidth = 280f;

        public const float BadgePreferredWidth = 0f;
        public const float BadgeMinWidth = 100f;
        public const float BadgePreferredHeight = 60f;
        public const float BadgeMinHeight = 60f;
        public const int   BadgeFontSize = 24;

        // Font sizes for PlayerPrefsRuntimeViewer
        public const int ViewerTitleFontSize = 28;
        public const int ViewerSubtitleFontSize = 28;
        public const int ViewerCloseButtonFontSize = 32;
        public const int SearchInputFontSize = 34;
        public const int SearchPlaceholderFontSize = 24;
        public const int ClearButtonFontSize = 24;

        // Font sizes for PlayerPrefsRuntimeEntryDialog
        public const int DialogTitleFontSize = 32;
        public const int DialogTitleResizeMinSize = 12;
        public const int DialogTitleResizeMaxSize = 40;
        public const int DialogKeyFontSize = 32;
        public const int DialogTypeFontSize = 28;
        public const int DialogValueLabelFontSize = 26;
        public const int DialogValueFontSize = 30;
        public const int DialogCloseButtonFontSize = 28;

        // Text length limits for PlayerPrefs entries
        public const int MaxNameTextLength = 100;
        public const int MaxValueTextLength = 200;
        public const string TextOverflowSuffix = "...";
    }
}
#endif