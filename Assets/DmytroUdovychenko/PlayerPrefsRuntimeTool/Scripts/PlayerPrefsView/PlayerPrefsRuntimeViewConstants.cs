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

        // Header layout (v3.1: toolbar rows with actions and type filters)
        public const float HeaderTopOffset = 15f;
        public const float ToolbarRowHeight = 72f;
        public const float ActionBarHeight = 58f;
        public const float ActionBarSpacing = 16f;
        public const float ActionBarTopOffset = HeaderTopOffset + ToolbarRowHeight + 10f;
        public const float FilterBarHeight = 50f;
        public const float FilterBarTopOffset = ActionBarTopOffset + ActionBarHeight + 10f;

        // The title/subtitle row is pinned to the bottom of the header with a fixed height so the
        // header can shrink when the action/filter bars are collapsed without clipping the text.
        public const float HeaderTextHeight = 63f;
        public const float HeaderTextBottomMargin = 9f;
        public const float HeaderContentBottomGap = 13f;
        public const float HeaderTextRowHeight = HeaderTextBottomMargin + HeaderTextHeight;

        // Header height depends on whether the action/filter bars are shown (see the tools toggle).
        public const float HeaderHeightExpanded = FilterBarTopOffset + FilterBarHeight + HeaderContentBottomGap + HeaderTextRowHeight;
        public const float HeaderHeightCollapsed = HeaderTopOffset + ToolbarRowHeight + HeaderContentBottomGap + HeaderTextRowHeight;
        public const float ScrollAreaTopGap = 10f;

        // Insets shared by the header bars (top bar, action bar, filter bar)
        public const float HeaderBarLeftInset = 30f;
        public const float HeaderBarRightInset = 20f;

        // Title/subtitle split the header text row; "outer" is the panel edge, "inner" the center.
        // The two bands must not overlap, otherwise long text from one draws over the other.
        public const float HeaderSubtitleAnchorMaxX = 0.5f;
        public const float HeaderTitleAnchorMinX = 0.5f;
        public const float HeaderTextOuterInset = 30f;
        public const float HeaderTextInnerInset = 20f;

        // Header title/subtitle shrink instead of wrapping out of their band on narrow screens
        public const int HeaderTextResizeMinSize = 14;

        public const float AccentBarHeight = 6f;
        public const float SeparatorHeight = 2f;
        public const float FilterBarLabelWidth = 90f;

        // Scroll area insets inside the panel
        public const float ScrollAreaSideInset = 20f;
        public const float ScrollAreaBottomInset = 20f;

        // Canvas scaler reference resolution
        public const float CanvasReferenceWidth = 1920f;
        public const float CanvasReferenceHeight = 1080f;

        // Shared control styling
        public const float PanelOutlineDistance = 4f;
        public const float ControlOutlineDistance = 2f;
        public const float ControlFadeDuration = 0.1f;
        public const float TextShadowDistance = 1.8f;
        public const float InnerShadowDistance = 1f;

        // Search field layout
        public const float SearchFieldWidth = 360f;
        public const float SearchTextAreaPaddingX = 10f;
        public const float SearchTextAreaPaddingY = 4f;
        public const float SearchTextWidth = 350f;
        public const float SearchTextHeight = 100f;
        public const float ClearButtonWidth = 60f;
        public const float ClearButtonOffsetX = 190f;

        public const string FilterBarName = "FilterBar";
        public const string FilterBarLabelText = "Filter:";
        public const int   FilterChipFontSize = 24;
        public const string ActionBarName = "ActionBar";
        public const string AddButtonName = "AddButton";
        public const string DeleteAllButtonName = "DeleteAllButton";
        public const string ExportButtonName = "ExportButton";
        public const string ImportButtonName = "ImportButton";
        public const string AddButtonLabel = "+ Add";
        public const string DeleteAllButtonLabel = "Delete All";
        public const string ExportButtonLabel = "Export";
        public const string ImportButtonLabel = "Import";
        public const int   ActionBarButtonFontSize = 28;

        // Tools toggle (shows/hides the action and filter bars); collapsed by default.
        public const string ToolsToggleButtonName = "ToolsToggleButton";
        public const string ToolsToggleButtonLabel = "Tools";
        public const float  ToolsToggleButtonWidth = 140f;
        public const float  ToolsToggleButtonSpacing = 10f;
        public const int    ToolsToggleButtonFontSize = 26;
        public const float  CloseButtonWidth = 70f;

        // Confirm dialog
        public const string ConfirmOverlayName = "ConfirmOverlay";
        public const string ConfirmDialogName = "ConfirmDialog";
        public const string ConfirmButtonName = "ConfirmButton";
        public const string CancelButtonName = "CancelButton";
        public const string CancelLabel = "Cancel";
        public const string DeleteLabel = "Delete";
        public const int   ConfirmTitleFontSize = 32;
        public const int   ConfirmMessageFontSize = 26;
        public const string DeleteAllConfirmTitle = "Delete ALL PlayerPrefs?";
        public const string DeleteAllConfirmMessage = "This removes every PlayerPrefs entry, including engine-managed keys. If entries exist, the operation is cancelled unless a verified complete JSON backup is saved to persistentDataPath first. This cannot be undone.";
        public const string RemoveConfirmTitleFormat = "Remove '{0}'?";
        public const string RemoveConfirmMessage = "This entry will be deleted permanently.";

        // Add entry dialog
        public const string AddEntryOverlayName = "AddEntryOverlay";
        public const string AddEntryDialogName = "AddEntryDialog";
        public const string AddEntryTitleText = "Add PlayerPrefs Entry";
        public const string KeyInputFieldName = "KeyInputField";
        public const string KeyLabelText = "Key:";
        public const string TypeLabelText = "Type:";
        public const string TypeSelectorName = "TypeSelector";
        public const string TypeIntLabel = "Int";
        public const string TypeFloatLabel = "Float";
        public const string TypeStringLabel = "String";
        public const string OverwriteLabel = "Overwrite";
        public const string KeyExistsWarningFormat = "Key '{0}' already exists. Press Overwrite to replace it (its type may change).";
        public const string KeyPlaceholderText = "Enter key...";
        public const string ValuePlaceholderText = "Enter value...";
        public const float AddDialogInputHeight = 70f;

        // Entry dialog additions
        public const string CopyValueButtonName = "CopyValueButton";
        public const string CopyValueLabel = "Copy Value";
        public const string CopyAllLabel = "Copy All";
        public const int   JsonPrettyPrintMaxLength = 16000;

        // Legacy UnityEngine.UI.Text has a ~65k vertex limit (~16k glyphs at 4 verts each):
        // a very long string fails to build its mesh and renders as nothing. Cap what the
        // entry dialog displays and lets you edit inline; the full value stays available
        // through Copy Value, Export and the public API.
        public const int    DialogValueDisplayMaxLength = 8000;
        public const string DialogValueTruncatedNoteFormat = "\n\n[ Showing first {0} of {1} characters — use \"Copy Value\" for the full value ]";
        public const string EditValueTooLargeFormat = "Value is too large to edit inline ({0} characters). Edit it via Import or the PlayerPrefsRuntime API; the full value is available through Copy Value.";

        // Size indicator
        public const string SubtitleSizeSeparator = " · ~";
        public const string EntrySizeFormat = "Type: {0} · ~{1}";

        // Export / import
        public const string ImportOverlayName = "ImportOverlay";
        public const string ImportDialogName = "ImportDialog";
        public const string ImportDialogTitleText = "Import PlayerPrefs JSON";
        public const string ImportHintText = "Paste JSON produced by Export:";
        public const string PasteLabel = "Paste";
        public const string ImportLabel = "Import";
        public const string ImportResultFormat = "Imported {0}, failed {1}.";
        public const string ImportMoreErrorsFormat = "...and {0} more.";
        public const float ImportTextAreaMinHeight = 300f;
        public const int   ImportMaxDisplayedErrors = 20;
        public const string ExportFileNameFormat = "playerprefs_export_{0}.json";
        public const string ExportTimestampFormat = "yyyyMMdd_HHmmss";
        public const string ExportStatusFormat = "Exported {0} entries to clipboard and {1}";
        public const string ExportClipboardOnlyStatusFormat = "Exported {0} entries to clipboard (file write failed)";
        public const float StatusMessageDuration = 3f;

        // List virtualization
        public const float RowFixedHeight = 90f;
        public const float RowVirtualSpacing = 6f;
        public const float ListPadding = 12f;
        public const int   VirtualizationBufferRows = 4;

        // Gesture trigger
        public const string GestureTriggerObjectName = "PlayerPrefsRuntimeGestureTrigger";
        public const int   GestureTouchCount = 3;
        public const float GestureHoldDuration = 2f;
    }
}
#endif
