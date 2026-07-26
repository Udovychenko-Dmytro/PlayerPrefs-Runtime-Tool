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
    /// Builds and manages the runtime viewer UI hierarchy.
    /// </summary>
    internal class PlayerPrefsRuntimeViewerBuilder
    {
        private readonly PlayerPrefsRuntimeViewerCallbacks m_callbacks;
        private readonly System.Collections.Generic.Dictionary<string, Button> m_typeFilterButtons = new System.Collections.Generic.Dictionary<string, Button>(StringComparer.Ordinal);

        private GameObject m_panelInstance;
        private RectTransform m_contentRoot;
        private ScrollRect m_scrollRect;
        private RectTransform m_viewportRect;
        private InputField m_searchInputField;
        private Text m_subtitleText;
        private Text m_sortModeLabel;
        private Font m_defaultFont;
        private RectTransform m_headerRect;
        private RectTransform m_separatorRect;
        private RectTransform m_scrollAreaRect;
        private GameObject m_actionBar;
        private GameObject m_filterBar;
        private Button m_toolsToggleButton;
        private PlayerPrefsRuntimeCoroutineHost m_coroutineHost;
        private bool m_toolsExpanded;

        public bool IsVisible => m_panelInstance != null;

        public RectTransform ContentRoot => m_contentRoot;

        public ScrollRect ScrollView => m_scrollRect;

        public RectTransform ViewportRect => m_viewportRect;

        public Transform PanelTransform => m_panelInstance != null ? m_panelInstance.transform : null;

        public Font DefaultFont => GetFont();

        public PlayerPrefsRuntimeViewerBuilder(PlayerPrefsRuntimeViewerCallbacks callbacks)
        {
            m_callbacks = callbacks ?? new PlayerPrefsRuntimeViewerCallbacks();
        }

        public void EnsureVisible()
        {
            if (m_panelInstance != null)
            {
                RectTransform panelTransform = m_panelInstance.GetComponent<RectTransform>();
                ApplySafeArea(panelTransform);
                return;
            }

            PlayerPrefsRuntimeUiFactory.EnsureEventSystem();
            BuildViewerPanel();
        }

        public void Destroy()
        {
            if (m_panelInstance == null)
                return;

            Transform parentTransform = m_panelInstance.transform.parent;
            if (parentTransform != null)
            {
                UnityEngine.Object.Destroy(parentTransform.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(m_panelInstance);
            }

            m_panelInstance = null;
            m_contentRoot = null;
            m_scrollRect = null;
            m_viewportRect = null;
            m_searchInputField = null;
            m_subtitleText = null;
            m_sortModeLabel = null;
            m_headerRect = null;
            m_separatorRect = null;
            m_scrollAreaRect = null;
            m_actionBar = null;
            m_filterBar = null;
            m_toolsToggleButton = null;
            m_coroutineHost = null;
            m_typeFilterButtons.Clear();
        }

        public void SetSearchText(string text)
        {
            if (m_searchInputField == null)
                return;

            m_searchInputField.text = text ?? string.Empty;
        }

        public Coroutine StartCoroutine(System.Collections.IEnumerator routine)
        {
            if (m_coroutineHost == null)
                return null;

            return m_coroutineHost.StartCoroutine(routine);
        }

        public void StopCoroutine(Coroutine routine)
        {
            if (m_coroutineHost == null || routine == null)
                return;

            m_coroutineHost.StopCoroutine(routine);
        }

        public void UpdateSubtitleText(string text)
        {
            if (m_subtitleText == null)
                return;

            m_subtitleText.text = text;
        }

        public void UpdateSortModeLabel(string text)
        {
            if (m_sortModeLabel == null)
                return;

            m_sortModeLabel.text = text;
        }

        /// <summary>
        /// Shows or hides the action bar (Add / Delete All / Export / Import) and the type filter
        /// bar, resizing the header and the scroll area so the hidden rows do not waste screen space.
        /// </summary>
        public void SetToolsExpanded(bool expanded)
        {
            m_toolsExpanded = expanded;
            ApplyToolsExpandedState();
        }

        private void ApplyToolsExpandedState()
        {
            if (m_actionBar != null)
            {
                m_actionBar.SetActive(m_toolsExpanded);
            }

            if (m_filterBar != null)
            {
                m_filterBar.SetActive(m_toolsExpanded);
            }

            float headerHeight = m_toolsExpanded
                ? PlayerPrefsRuntimeViewConstants.HeaderHeightExpanded
                : PlayerPrefsRuntimeViewConstants.HeaderHeightCollapsed;

            if (m_headerRect != null)
            {
                m_headerRect.sizeDelta = new Vector2(m_headerRect.sizeDelta.x, headerHeight);
            }

            if (m_separatorRect != null)
            {
                m_separatorRect.anchoredPosition = new Vector2(m_separatorRect.anchoredPosition.x, -headerHeight);
            }

            if (m_scrollAreaRect != null)
            {
                m_scrollAreaRect.offsetMax = new Vector2(
                    m_scrollAreaRect.offsetMax.x,
                    -(headerHeight + PlayerPrefsRuntimeViewConstants.ScrollAreaTopGap));
            }

            ApplyToggleColors(m_toolsToggleButton, m_toolsExpanded);
        }

        // Every call builds a brand-new Canvas: Destroy() tears the previous one down, and an
        // externally destroyed Canvas may still linger until the end of the frame, so nothing here
        // ever looks for or reuses existing objects.
        private void BuildViewerPanel()
        {
            GameObject canvasGo = CreateCanvas();
            CreateBackdrop(canvasGo.transform);

            GameObject panel = new GameObject(
                PlayerPrefsRuntimeViewConstants.PanelName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(PlayerPrefsRuntimeCoroutineHost));
            panel.transform.SetParent(canvasGo.transform, false);
            panel.transform.SetAsLastSibling();

            m_coroutineHost = panel.GetComponent<PlayerPrefsRuntimeCoroutineHost>();

            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(PlayerPrefsRuntimeViewConstants.PanelAnchorMinX, PlayerPrefsRuntimeViewConstants.PanelAnchorMinY);
            panelRT.anchorMax = new Vector2(PlayerPrefsRuntimeViewConstants.PanelAnchorMaxX, PlayerPrefsRuntimeViewConstants.PanelAnchorMaxY);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            ApplySafeArea(panelRT);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = PlayerPrefsRuntimeViewConstants.PanelColor;

            Outline outline = panel.GetComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.PanelOutlineDistance, -PlayerPrefsRuntimeViewConstants.PanelOutlineDistance);

            CreateHeader(panel.transform);
            CreateScrollArea(panel.transform);
            ApplyToolsExpandedState();

            m_panelInstance = panel;
        }

        private GameObject CreateCanvas()
        {
            GameObject canvasGo = new GameObject(PlayerPrefsRuntimeViewConstants.ViewerCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PlayerPrefsRuntimeViewConstants.CanvasReferenceWidth, PlayerPrefsRuntimeViewConstants.CanvasReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = PlayerPrefsRuntimeViewConstants.CanvasMatchWidthOrHeight;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return canvasGo;
        }

        private void CreateBackdrop(Transform canvasTransform)
        {
            GameObject backdrop = new GameObject(PlayerPrefsRuntimeViewConstants.BackdropName, typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasTransform, false);
            backdrop.transform.SetAsFirstSibling();

            RectTransform rt = backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = backdrop.GetComponent<Image>();
            img.color = PlayerPrefsRuntimeViewConstants.BackdropColor;
            img.raycastTarget = false;
        }

        private void ApplySafeArea(RectTransform panelRT)
        {
            if (panelRT == null)
                return;

            Rect safeArea = Screen.safeArea;

            if (safeArea.width <= 0f || safeArea.height <= 0f || Screen.width <= 0 || Screen.height <= 0)
            {
                ResetPanelRect(panelRT);
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            panelRT.anchorMin = anchorMin;
            panelRT.anchorMax = anchorMax;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
        }

        private void ResetPanelRect(RectTransform panelRT)
        {
            panelRT.anchorMin = new Vector2(PlayerPrefsRuntimeViewConstants.PanelAnchorMinX, PlayerPrefsRuntimeViewConstants.PanelAnchorMinY);
            panelRT.anchorMax = new Vector2(PlayerPrefsRuntimeViewConstants.PanelAnchorMaxX, PlayerPrefsRuntimeViewConstants.PanelAnchorMaxY);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
        }

        private void CreateHeader(Transform panel)
        {
            GameObject header = new GameObject(PlayerPrefsRuntimeViewConstants.HeaderName, typeof(RectTransform), typeof(Image));
            header.transform.SetParent(panel, false);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.sizeDelta = new Vector2(0, PlayerPrefsRuntimeViewConstants.HeaderHeightExpanded);
            headerRT.anchoredPosition = Vector2.zero;

            m_headerRect = headerRT;

            Image headerImage = header.GetComponent<Image>();
            headerImage.color = PlayerPrefsRuntimeViewConstants.HeaderColor;

            GameObject topBar = new GameObject(PlayerPrefsRuntimeViewConstants.TopBarName, typeof(RectTransform));
            topBar.transform.SetParent(header.transform, false);
            RectTransform topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.anchorMin = new Vector2(0f, 1f);
            topBarRT.anchorMax = new Vector2(1f, 1f);
            topBarRT.pivot = new Vector2(0.5f, 1f);
            topBarRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderBarLeftInset, -(PlayerPrefsRuntimeViewConstants.HeaderTopOffset + PlayerPrefsRuntimeViewConstants.ToolbarRowHeight));
            topBarRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.HeaderBarRightInset, -PlayerPrefsRuntimeViewConstants.HeaderTopOffset);

            CreateSortModeButton(topBar.transform);
            CreateSearchField(topBar.transform);
            CreateCloseButton(topBar.transform);
            CreateToolsToggleButton(topBar.transform);
            CreateActionBar(header.transform);
            CreateFilterBar(header.transform);

            Text titleText = CreateText(PlayerPrefsRuntimeViewConstants.TitleName, header.transform, PlayerPrefsRuntimeViewConstants.ViewerTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleRight, true, out _);
            if (titleText != null)
            {
                titleText.text = PlayerPrefsRuntimeViewConstants.ToolName;
            }

            Text subtitle = CreateText(PlayerPrefsRuntimeViewConstants.SubtitleName, header.transform, PlayerPrefsRuntimeViewConstants.ViewerSubtitleFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.MiddleLeft, true, out _);
            if (subtitle != null)
            {
                m_subtitleText = subtitle;
            }

            ConfigureHeaderTextLayout(header.transform);

            GameObject accent = new GameObject(PlayerPrefsRuntimeViewConstants.AccentBarName, typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(header.transform, false);
            RectTransform accentRT = (RectTransform)accent.transform;
            accentRT.anchorMin = new Vector2(0, 0);
            accentRT.anchorMax = new Vector2(1, 0);
            accentRT.pivot = new Vector2(0.5f, 0);
            accentRT.sizeDelta = new Vector2(0f, PlayerPrefsRuntimeViewConstants.AccentBarHeight);
            accentRT.anchoredPosition = new Vector2(0f, -PlayerPrefsRuntimeViewConstants.AccentBarHeight * 0.5f);
            accent.GetComponent<Image>().color = PlayerPrefsRuntimeViewConstants.AccentColor;
        }

        private void CreateActionBar(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for action bar");
                return;
            }

            GameObject actionBar = new GameObject(PlayerPrefsRuntimeViewConstants.ActionBarName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionBar.transform.SetParent(header, false);
            m_actionBar = actionBar;

            RectTransform actionBarRT = actionBar.GetComponent<RectTransform>();
            actionBarRT.anchorMin = new Vector2(0f, 1f);
            actionBarRT.anchorMax = new Vector2(1f, 1f);
            actionBarRT.pivot = new Vector2(0.5f, 1f);
            actionBarRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderBarLeftInset, -(PlayerPrefsRuntimeViewConstants.ActionBarTopOffset + PlayerPrefsRuntimeViewConstants.ActionBarHeight));
            actionBarRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.HeaderBarRightInset, -PlayerPrefsRuntimeViewConstants.ActionBarTopOffset);

            HorizontalLayoutGroup layoutGroup = actionBar.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = PlayerPrefsRuntimeViewConstants.ActionBarSpacing;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;

            CreateToolbarButton(PlayerPrefsRuntimeViewConstants.AddButtonName, actionBar.transform, PlayerPrefsRuntimeViewConstants.AddButtonLabel, HandleAddClicked, false);
            CreateToolbarButton(PlayerPrefsRuntimeViewConstants.DeleteAllButtonName, actionBar.transform, PlayerPrefsRuntimeViewConstants.DeleteAllButtonLabel, HandleDeleteAllClicked, true);
            CreateToolbarButton(PlayerPrefsRuntimeViewConstants.ExportButtonName, actionBar.transform, PlayerPrefsRuntimeViewConstants.ExportButtonLabel, HandleExportClicked, false);
            CreateToolbarButton(PlayerPrefsRuntimeViewConstants.ImportButtonName, actionBar.transform, PlayerPrefsRuntimeViewConstants.ImportButtonLabel, HandleImportClicked, false);
        }

        private void CreateToolbarButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool danger)
        {
            GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(parent, false);

            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Outline outline = buttonGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.ControlOutlineDistance, -PlayerPrefsRuntimeViewConstants.ControlOutlineDistance);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = PlayerPrefsRuntimeViewConstants.ControlNormalColor;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = PlayerPrefsRuntimeViewConstants.ControlFadeDuration;
            button.colors = colors;

            if (danger)
            {
                PlayerPrefsRuntimeUiFactory.ApplyDangerColors(button);
            }

            LayoutElement layout = buttonGo.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.minWidth = 0f;

            Text labelText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, buttonGo.transform, PlayerPrefsRuntimeViewConstants.ActionBarButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        private void CreateFilterBar(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for filter bar");
                return;
            }

            GameObject filterBar = new GameObject(PlayerPrefsRuntimeViewConstants.FilterBarName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            filterBar.transform.SetParent(header, false);
            m_filterBar = filterBar;

            RectTransform filterBarRT = filterBar.GetComponent<RectTransform>();
            filterBarRT.anchorMin = new Vector2(0f, 1f);
            filterBarRT.anchorMax = new Vector2(1f, 1f);
            filterBarRT.pivot = new Vector2(0.5f, 1f);
            filterBarRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderBarLeftInset, -(PlayerPrefsRuntimeViewConstants.FilterBarTopOffset + PlayerPrefsRuntimeViewConstants.FilterBarHeight));
            filterBarRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.HeaderBarRightInset, -PlayerPrefsRuntimeViewConstants.FilterBarTopOffset);

            HorizontalLayoutGroup layoutGroup = filterBar.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = PlayerPrefsRuntimeViewConstants.ActionBarSpacing;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;

            Text filterLabel = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, filterBar.transform, PlayerPrefsRuntimeViewConstants.FilterChipFontSize, FontStyle.Bold, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.MiddleLeft, false, out GameObject filterLabelGo);
            if (filterLabel != null)
            {
                filterLabel.text = PlayerPrefsRuntimeViewConstants.FilterBarLabelText;
            }

            if (filterLabelGo != null)
            {
                LayoutElement labelLayout = filterLabelGo.AddComponent<LayoutElement>();
                labelLayout.preferredWidth = PlayerPrefsRuntimeViewConstants.FilterBarLabelWidth;
                labelLayout.flexibleWidth = 0f;
            }

            CreateTypeFilterChip("Int32", PlayerPrefsRuntimeViewConstants.TypeIntLabel, filterBar.transform);
            CreateTypeFilterChip("Single", PlayerPrefsRuntimeViewConstants.TypeFloatLabel, filterBar.transform);
            CreateTypeFilterChip("String", PlayerPrefsRuntimeViewConstants.TypeStringLabel, filterBar.transform);
        }

        private void CreateTypeFilterChip(string typeName, string label, Transform parent)
        {
            GameObject chipGo = new GameObject(typeName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            chipGo.transform.SetParent(parent, false);

            Image chipImage = chipGo.GetComponent<Image>();
            chipImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Outline outline = chipGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.ControlOutlineDistance, -PlayerPrefsRuntimeViewConstants.ControlOutlineDistance);

            Button chipButton = chipGo.GetComponent<Button>();
            chipButton.targetGraphic = chipImage;
            chipButton.onClick.AddListener(() => m_callbacks.OnTypeFilterToggled?.Invoke(typeName));

            ColorBlock colors = chipButton.colors;
            colors.normalColor = PlayerPrefsRuntimeViewConstants.ControlNormalColor;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = PlayerPrefsRuntimeViewConstants.ControlFadeDuration;
            chipButton.colors = colors;

            LayoutElement layout = chipGo.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.minWidth = 0f;

            Text labelText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, chipGo.transform, PlayerPrefsRuntimeViewConstants.FilterChipFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            if (labelText != null)
            {
                labelText.text = label;
            }

            m_typeFilterButtons[typeName] = chipButton;
        }

        public void SetTypeFilterActive(string typeName, bool active)
        {
            if (!m_typeFilterButtons.TryGetValue(typeName, out Button chipButton))
            {
                return;
            }

            ApplyToggleColors(chipButton, active);
        }

        /// <summary>
        /// Paints a toggle-style button (type filter chip, tools toggle) in its on/off colors.
        /// </summary>
        private static void ApplyToggleColors(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Image buttonImage = button.targetGraphic as Image;
            if (buttonImage != null)
            {
                buttonImage.color = active ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlNormalColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = active ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlNormalColor;
            colors.highlightedColor = active ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = active ? PlayerPrefsRuntimeViewConstants.AccentColor : PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            button.colors = colors;

            Transform labelTransform = button.transform.Find(PlayerPrefsRuntimeViewConstants.LabelName);
            Text labelText = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
            if (labelText != null)
            {
                labelText.color = active ? PlayerPrefsRuntimeViewConstants.BadgeLabelColor : Color.white;
            }
        }

        private void ConfigureHeaderTextLayout(Transform header)
        {
            ConfigureTitleLayout(header.Find(PlayerPrefsRuntimeViewConstants.TitleName)?.GetComponent<Text>());
            ConfigureSubtitleLayout(header.Find(PlayerPrefsRuntimeViewConstants.SubtitleName)?.GetComponent<Text>());
        }

        /// <summary>
        /// Lets a header label shrink to fit its band instead of wrapping past it. The title and the
        /// subtitle sit side by side in one row, so an unshrunk long string (the tool name on a narrow
        /// window, or a long status message) would otherwise be drawn over its neighbour.
        /// </summary>
        private static void ApplyHeaderTextBestFit(Text text, int maxFontSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = PlayerPrefsRuntimeViewConstants.HeaderTextResizeMinSize;
            text.resizeTextMaxSize = maxFontSize;
        }

        private void ConfigureTitleLayout(Text titleText)
        {
            if (titleText == null)
                return;

            RectTransform titleRT = titleText.rectTransform;
            titleText.alignment = TextAnchor.MiddleRight;
            ApplyHeaderTextBestFit(titleText, PlayerPrefsRuntimeViewConstants.ViewerTitleFontSize);
            titleRT.anchorMin = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderTitleAnchorMinX, 0f);
            titleRT.anchorMax = new Vector2(1f, 0f);
            titleRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderTextInnerInset, PlayerPrefsRuntimeViewConstants.HeaderTextBottomMargin);
            titleRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.HeaderTextOuterInset, PlayerPrefsRuntimeViewConstants.HeaderTextRowHeight);
        }

        private void ConfigureSubtitleLayout(Text subtitleText)
        {
            if (subtitleText == null)
                return;

            RectTransform subtitleRT = subtitleText.rectTransform;
            subtitleText.alignment = TextAnchor.MiddleLeft;
            ApplyHeaderTextBestFit(subtitleText, PlayerPrefsRuntimeViewConstants.ViewerSubtitleFontSize);
            subtitleRT.anchorMin = new Vector2(0f, 0f);
            subtitleRT.anchorMax = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderSubtitleAnchorMaxX, 0f);
            subtitleRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.HeaderTextOuterInset, PlayerPrefsRuntimeViewConstants.HeaderTextBottomMargin);
            subtitleRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.HeaderTextInnerInset, PlayerPrefsRuntimeViewConstants.HeaderTextRowHeight);
        }

        private void CreateScrollArea(Transform panel)
        {
            GameObject separator = new GameObject(PlayerPrefsRuntimeViewConstants.SeparatorName, typeof(RectTransform), typeof(Image));
            separator.transform.SetParent(panel, false);

            RectTransform separatorRT = separator.GetComponent<RectTransform>();
            separatorRT.anchorMin = new Vector2(0, 1);
            separatorRT.anchorMax = new Vector2(1, 1);
            separatorRT.pivot = new Vector2(0.5f, 1f);
            separatorRT.sizeDelta = new Vector2(0f, PlayerPrefsRuntimeViewConstants.SeparatorHeight);
            separatorRT.anchoredPosition = new Vector2(0, -PlayerPrefsRuntimeViewConstants.HeaderHeightExpanded);

            m_separatorRect = separatorRT;

            Image separatorImage = separator.GetComponent<Image>();
            separatorImage.color = PlayerPrefsRuntimeViewConstants.SeparatorColor;

            GameObject scrollGo = new GameObject(PlayerPrefsRuntimeViewConstants.ScrollViewName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);

            RectTransform scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.ScrollAreaSideInset, PlayerPrefsRuntimeViewConstants.ScrollAreaBottomInset);
            scrollRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.ScrollAreaSideInset, -(PlayerPrefsRuntimeViewConstants.HeaderHeightExpanded + PlayerPrefsRuntimeViewConstants.ScrollAreaTopGap));
            scrollRT.pivot = new Vector2(0.5f, 0.5f);

            m_scrollAreaRect = scrollRT;

            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = PlayerPrefsRuntimeViewConstants.ScrollBackgroundColor;

            GameObject viewportGo = new GameObject(PlayerPrefsRuntimeViewConstants.ViewportName, typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);

            RectTransform viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;

            m_viewportRect = viewportRT;

            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = PlayerPrefsRuntimeViewConstants.ViewportBackgroundColor;

            Mask viewportMask = viewportGo.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Content is a bare RectTransform: rows are positioned absolutely and the
            // content height is set manually by the viewer (virtualized list).
            GameObject contentGo = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);

            RectTransform contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);

            m_contentRoot = contentRT;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = PlayerPrefsRuntimeViewConstants.ScrollSensitivity;
            scroll.inertia = true;
            scroll.decelerationRate = PlayerPrefsRuntimeViewConstants.ScrollDecelerationRate;
            scroll.onValueChanged.AddListener(HandleScrollValueChanged);

            m_scrollRect = scroll;
        }

        public void SetContentHeight(float height)
        {
            if (m_contentRoot == null)
            {
                return;
            }

            m_contentRoot.sizeDelta = new Vector2(m_contentRoot.sizeDelta.x, height);
        }

        private void CreateSortModeButton(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for sort mode button");
                return;
            }

            GameObject buttonGo = new GameObject(PlayerPrefsRuntimeViewConstants.SortModeButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(header, false);

            RectTransform buttonRT = buttonGo.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0f, 0f);
            buttonRT.anchorMax = new Vector2(0f, 1f);
            buttonRT.pivot = new Vector2(0f, 0.5f);
            buttonRT.sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.SortModeButtonWidth, 0f);
            buttonRT.anchoredPosition = new Vector2(0f, 0f);

            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Outline outline = buttonGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.ControlOutlineDistance, -PlayerPrefsRuntimeViewConstants.ControlOutlineDistance);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(HandleSortModeButtonClicked);

            ColorBlock colors = button.colors;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = PlayerPrefsRuntimeViewConstants.ControlFadeDuration;
            button.colors = colors;

            Text label = CreateText(
                PlayerPrefsRuntimeViewConstants.LabelName,
                buttonGo.transform,
                PlayerPrefsRuntimeViewConstants.SortModeButtonLabelFontSize,
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter,
                false,
                out GameObject gameObject);

            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
                m_sortModeLabel = label;
            }

            if (gameObject != null)
            {
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.SortModeButtonWidth, PlayerPrefsRuntimeViewConstants.SortModeButtonHeight);
            }
        }

        private void CreateSearchField(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for search field");
                return;
            }

            GameObject inputFieldGo = new GameObject(PlayerPrefsRuntimeViewConstants.SearchFieldName, typeof(RectTransform), typeof(Image));
            inputFieldGo.transform.SetParent(header, false);

            RectTransform inputFieldRT = inputFieldGo.GetComponent<RectTransform>();
            inputFieldRT.anchorMin = new Vector2(0.5f, 0f);
            inputFieldRT.anchorMax = new Vector2(0.5f, 1f);
            inputFieldRT.pivot = new Vector2(0.5f, 0.5f);
            inputFieldRT.sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.SearchFieldWidth, 0f);
            inputFieldRT.anchoredPosition = new Vector2(0f, 0f);

            Image inputFieldImage = inputFieldGo.GetComponent<Image>();
            inputFieldImage.color = PlayerPrefsRuntimeViewConstants.SearchFieldColor;

            Outline outline = inputFieldGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.ControlOutlineDistance, -PlayerPrefsRuntimeViewConstants.ControlOutlineDistance);

            Shadow innerShadow = inputFieldGo.AddComponent<Shadow>();
            innerShadow.effectColor = PlayerPrefsRuntimeViewConstants.InnerShadowColor;
            innerShadow.effectDistance = new Vector2(0f, -PlayerPrefsRuntimeViewConstants.InnerShadowDistance);

            GameObject textArea = new GameObject(PlayerPrefsRuntimeViewConstants.TextAreaName, typeof(RectTransform));
            textArea.transform.SetParent(inputFieldGo.transform, false);
            RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(PlayerPrefsRuntimeViewConstants.SearchTextAreaPaddingX, PlayerPrefsRuntimeViewConstants.SearchTextAreaPaddingY);
            textAreaRT.offsetMax = new Vector2(-PlayerPrefsRuntimeViewConstants.SearchTextAreaPaddingX, -PlayerPrefsRuntimeViewConstants.SearchTextAreaPaddingY);

            Text placeholderText = CreateText(PlayerPrefsRuntimeViewConstants.PlaceholderName, textArea.transform, PlayerPrefsRuntimeViewConstants.SearchPlaceholderFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.PlaceholderTextColor, TextAnchor.MiddleLeft, false, out _);
            if (placeholderText != null)
            {
                placeholderText.text = "Search...";
            }

            Text inputText = CreateText(PlayerPrefsRuntimeViewConstants.TextComponentName, textArea.transform, PlayerPrefsRuntimeViewConstants.SearchInputFontSize, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft, false, out GameObject textGameObject);
            if (inputText != null)
            {
                inputText.text = "";
            }

            if (textGameObject != null)
            {
                textGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.SearchTextWidth, PlayerPrefsRuntimeViewConstants.SearchTextHeight);
            }

            InputField inputField = inputFieldGo.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.onValueChanged.AddListener(HandleSearchValueChanged);

            m_searchInputField = inputField;

            // No separate Button on this GameObject: InputField is itself a Selectable and Unity
            // allows only one Selectable per GameObject. Adding a Button here failed at runtime
            // ("A GameObject can only contain one 'Selectable' component"), so the styling below it
            // never ran. If hover/press feedback on the field is ever wanted, configure the
            // InputField's own transition/colors instead of adding a second component.

            GameObject clearButtonGo = new GameObject(PlayerPrefsRuntimeViewConstants.ClearButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            clearButtonGo.transform.SetParent(header, false);

            RectTransform clearButtonRT = clearButtonGo.GetComponent<RectTransform>();
            clearButtonRT.anchorMin = new Vector2(0.5f, 0f);
            clearButtonRT.anchorMax = new Vector2(0.5f, 1f);
            clearButtonRT.pivot = new Vector2(0f, 0.5f);
            clearButtonRT.sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.ClearButtonWidth, 0f);
            clearButtonRT.anchoredPosition = new Vector2(PlayerPrefsRuntimeViewConstants.ClearButtonOffsetX, 0f);

            Image clearButtonImage = clearButtonGo.GetComponent<Image>();
            clearButtonImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Button clearButton = clearButtonGo.GetComponent<Button>();
            clearButton.targetGraphic = clearButtonImage;
            clearButton.onClick.AddListener(HandleClearSearchClicked);

            ColorBlock clearColors = clearButton.colors;
            clearColors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            clearColors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            clearColors.fadeDuration = PlayerPrefsRuntimeViewConstants.ControlFadeDuration;
            clearButton.colors = clearColors;

            Text clearButtonText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, clearButtonGo.transform, PlayerPrefsRuntimeViewConstants.ClearButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            if (clearButtonText != null)
            {
                clearButtonText.text = PlayerPrefsRuntimeViewConstants.CloseButtonText;
            }
        }

        private void CreateCloseButton(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for close button");
                return;
            }

            GameObject closeGo = new GameObject(PlayerPrefsRuntimeViewConstants.CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(header, false);

            RectTransform closeRT = closeGo.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 0f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot = new Vector2(1f, 0.5f);
            closeRT.sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.CloseButtonWidth, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 0f);

            Image closeImage = closeGo.GetComponent<Image>();
            closeImage.color = PlayerPrefsRuntimeViewConstants.CloseButtonNormalColor;

            Button closeButton = closeGo.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(HandleCloseButtonClicked);

            ColorBlock closeColors = closeButton.colors;
            closeColors.highlightedColor = PlayerPrefsRuntimeViewConstants.CloseButtonHighlightedColor;
            closeColors.pressedColor = PlayerPrefsRuntimeViewConstants.CloseButtonPressedColor;
            closeColors.fadeDuration = PlayerPrefsRuntimeViewConstants.ControlFadeDuration;
            closeButton.colors = closeColors;

            Text closeText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, closeGo.transform, PlayerPrefsRuntimeViewConstants.ViewerCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            if (closeText != null)
            {
                closeText.text = PlayerPrefsRuntimeViewConstants.CloseButtonText;
            }
        }

        private void CreateToolsToggleButton(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for tools toggle button");
                return;
            }

            GameObject toggleGo = new GameObject(PlayerPrefsRuntimeViewConstants.ToolsToggleButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            toggleGo.transform.SetParent(header, false);

            RectTransform toggleRT = toggleGo.GetComponent<RectTransform>();
            toggleRT.anchorMin = new Vector2(1f, 0f);
            toggleRT.anchorMax = new Vector2(1f, 1f);
            toggleRT.pivot = new Vector2(1f, 0.5f);
            toggleRT.sizeDelta = new Vector2(PlayerPrefsRuntimeViewConstants.ToolsToggleButtonWidth, 0f);
            toggleRT.anchoredPosition = new Vector2(
                -(PlayerPrefsRuntimeViewConstants.CloseButtonWidth + PlayerPrefsRuntimeViewConstants.ToolsToggleButtonSpacing),
                0f);

            Image toggleImage = toggleGo.GetComponent<Image>();
            toggleImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Outline outline = toggleGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.ControlOutlineDistance, -PlayerPrefsRuntimeViewConstants.ControlOutlineDistance);

            Button toggleButton = toggleGo.GetComponent<Button>();
            toggleButton.targetGraphic = toggleImage;
            toggleButton.onClick.AddListener(HandleToolsToggleClicked);

            ColorBlock colors = toggleButton.colors;
            colors.normalColor = PlayerPrefsRuntimeViewConstants.ControlNormalColor;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = PlayerPrefsRuntimeViewConstants.ControlFadeDuration;
            toggleButton.colors = colors;

            Text toggleText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, toggleGo.transform, PlayerPrefsRuntimeViewConstants.ToolsToggleButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            if (toggleText != null)
            {
                toggleText.text = PlayerPrefsRuntimeViewConstants.ToolsToggleButtonLabel;
            }

            m_toolsToggleButton = toggleButton;
        }

        private void HandleSortModeButtonClicked()
        {
            m_callbacks.OnSortModeButtonClicked?.Invoke();
        }

        private void HandleSearchValueChanged(string value)
        {
            m_callbacks.OnSearchValueChanged?.Invoke(value);
        }

        private void HandleClearSearchClicked()
        {
            m_callbacks.OnClearSearchClicked?.Invoke();
        }

        private void HandleCloseButtonClicked()
        {
            m_callbacks.OnCloseButtonClicked?.Invoke();
        }

        private void HandleToolsToggleClicked()
        {
            m_callbacks.OnToolsToggleClicked?.Invoke();
        }

        private void HandleAddClicked()
        {
            m_callbacks.OnAddClicked?.Invoke();
        }

        private void HandleDeleteAllClicked()
        {
            m_callbacks.OnDeleteAllClicked?.Invoke();
        }

        private void HandleExportClicked()
        {
            m_callbacks.OnExportClicked?.Invoke();
        }

        private void HandleImportClicked()
        {
            m_callbacks.OnImportClicked?.Invoke();
        }

        private void HandleScrollValueChanged(Vector2 value)
        {
            m_callbacks.OnScrollValueChanged?.Invoke(value);
        }

        private Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color, TextAnchor anchor, bool emphasize, out GameObject gameObject)
        {
            gameObject = null;
            if (parent == null)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Parent is null for text '{name}'");
                return null;
            }

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            gameObject = go;

            Font font = GetFont();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            if (emphasize)
            {
                Shadow shadow = go.AddComponent<Shadow>();
                shadow.effectColor = PlayerPrefsRuntimeViewConstants.TextShadowColor;
                shadow.effectDistance = new Vector2(PlayerPrefsRuntimeViewConstants.TextShadowDistance, -PlayerPrefsRuntimeViewConstants.TextShadowDistance);
            }

            return text;
        }

        private Font GetFont()
        {
            if (m_defaultFont == null)
            {
                m_defaultFont = PlayerPrefsRuntimeUiFactory.ResolveDefaultFont();
            }

            return m_defaultFont;
        }
    }
}
#endif
