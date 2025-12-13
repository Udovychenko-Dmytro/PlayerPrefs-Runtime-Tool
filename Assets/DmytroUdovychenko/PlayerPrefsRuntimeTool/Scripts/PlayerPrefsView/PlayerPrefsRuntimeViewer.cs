// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Responsible for rendering PlayerPrefs entries via a stylized scrollable Canvas.
    /// </summary>
    internal class PlayerPrefsRuntimeViewer
    {
        private enum SortMode
        {
            Name,
            Type
        }

        private readonly List<PlayerPrefsRuntimeEntry> m_entriesCache = new List<PlayerPrefsRuntimeEntry>();
        private SortMode m_currentSortMode = SortMode.Name;
        private GameObject m_panelInstance;
        private string m_searchFilter = "";
        private InputField m_searchInputField;
        private readonly PlayerPrefsRuntimeEntryDialog m_entryDialog = new PlayerPrefsRuntimeEntryDialog();
        public bool IsVisible => m_panelInstance != null;

        private Font m_defaultFont;
        private readonly PlayerPrefsRuntimeRow m_rowBuilder = new PlayerPrefsRuntimeRow();

        public void ShowEntries(IReadOnlyList<PlayerPrefsRuntimeEntry> entries)
        {
            if (entries == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] No entries provided for viewer");
                return;
            }

            try
            {
                EnsureEventSystemExists();
                EnsureViewerPanel();

                m_entriesCache.Clear();
                m_entriesCache.AddRange(entries);

                RefreshRows();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Failed to render UI list: {e.Message}");
            }
        }

        private GameObject EnsureViewerPanel()
        {
            GameObject canvasGo = EnsureCanvas();
            EnsureBackdrop(canvasGo.transform);

            Transform existingPanel = canvasGo.transform.Find(PlayerPrefsRuntimeViewConstants.PanelName);
            if (existingPanel != null)
            {
                m_panelInstance = existingPanel.gameObject;
                ApplySafeArea(existingPanel.GetComponent<RectTransform>());
                return m_panelInstance;
            }

            Transform legacyScroll = canvasGo.transform.Find("ScrollView");
            if (legacyScroll != null)
            {
                UnityEngine.Object.Destroy(legacyScroll.gameObject);
            }

            GameObject panel = new GameObject(PlayerPrefsRuntimeViewConstants.PanelName, typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(canvasGo.transform, false);
            panel.transform.SetAsLastSibling();

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
            outline.effectDistance = new Vector2(4f, -4f);

            CreateHeader(panel.transform);
            CreateScrollArea(panel.transform);

            m_panelInstance = panel;
            return panel;
        }

        private GameObject EnsureCanvas()
        {
            GameObject canvasGo = new GameObject(PlayerPrefsRuntimeViewConstants.ViewerCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = PlayerPrefsRuntimeViewConstants.CanvasMatchWidthOrHeight;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return canvasGo;
        }

        private void EnsureBackdrop(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find(PlayerPrefsRuntimeViewConstants.BackdropName);
            if (existing != null)
            {
                existing.SetAsFirstSibling();
                return;
            }

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
            Transform existingHeader = panel.Find("Header");
            if (existingHeader != null)
            {
                CreateSortModeButton(existingHeader);
                CreateSearchField(existingHeader);
                CreateCloseButton(existingHeader);
                ConfigureHeaderTextLayout(existingHeader);
                return;
            }

            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(panel, false);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.sizeDelta = new Vector2(0, 160);
            headerRT.anchoredPosition = Vector2.zero;

            Image headerImage = header.GetComponent<Image>();
            headerImage.color = PlayerPrefsRuntimeViewConstants.HeaderColor;

            // Create a top bar for controls
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform));
            topBar.transform.SetParent(header.transform, false);
            RectTransform topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.anchorMin = new Vector2(0, 0.55f); // Position in upper part
            topBarRT.anchorMax = new Vector2(1, 1);
            topBarRT.pivot = new Vector2(0.5f, 1f);
            topBarRT.offsetMin = new Vector2(30f, 0f);
            topBarRT.offsetMax = new Vector2(-20f, -15f);

            // Add controls to top bar
            CreateSortModeButton(topBar.transform);
            CreateSearchField(topBar.transform);
            CreateCloseButton(topBar.transform);

            Text titleText = CreateText("Title", header.transform, PlayerPrefsRuntimeViewConstants.ViewerTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleRight, true, out _);
            titleText.text = "PlayerPrefs Runtime Viewer";

            Text subtitle = CreateText("Subtitle", header.transform, PlayerPrefsRuntimeViewConstants.ViewerSubtitleFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.MiddleLeft, true, out _);

            ConfigureHeaderTextLayout(header.transform);

            GameObject accent = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(header.transform, false);
            RectTransform accentRT = (RectTransform)accent.transform;
            accentRT.anchorMin = new Vector2(0, 0);
            accentRT.anchorMax = new Vector2(1, 0);
            accentRT.pivot = new Vector2(0.5f, 0);
            accentRT.sizeDelta = new Vector2(0, 6f);
            accentRT.anchoredPosition = new Vector2(0, -3f);
            accent.GetComponent<Image>().color = PlayerPrefsRuntimeViewConstants.AccentColor;
        }

        private void ConfigureHeaderTextLayout(Transform header)
        {
            ConfigureTitleLayout(header.Find("Title")?.GetComponent<Text>());
            ConfigureSubtitleLayout(header.Find("Subtitle")?.GetComponent<Text>());
        }

        private void ConfigureTitleLayout(Text titleText)
        {
            RectTransform titleRT = titleText.rectTransform;
            titleText.alignment = TextAnchor.MiddleRight;
            titleRT.anchorMin = new Vector2(0.45f, 0.05f);
            titleRT.anchorMax = new Vector2(1f, 0.45f);
            titleRT.offsetMin = new Vector2(20f, 0f);
            titleRT.offsetMax = new Vector2(-30f, 0f);
        }

        private void ConfigureSubtitleLayout(Text subtitleText)
        {
            RectTransform subtitleRT = subtitleText.rectTransform;
            subtitleText.alignment = TextAnchor.MiddleLeft;
            subtitleRT.anchorMin = new Vector2(0f, 0.05f);
            subtitleRT.anchorMax = new Vector2(0.55f, 0.45f);
            subtitleRT.offsetMin = new Vector2(30f, 0f);
            subtitleRT.offsetMax = new Vector2(-20f, 0f);
        }

        private void CreateScrollArea(Transform panel)
        {
            if (panel.Find("ScrollView") != null)
                return;

            // Add a separator line between header and scroll area
            GameObject separator = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            separator.transform.SetParent(panel, false);
            
            RectTransform separatorRT = separator.GetComponent<RectTransform>();
            separatorRT.anchorMin = new Vector2(0, 1);
            separatorRT.anchorMax = new Vector2(1, 1);
            separatorRT.pivot = new Vector2(0.5f, 1f);
            separatorRT.sizeDelta = new Vector2(0, 2f);
            separatorRT.anchoredPosition = new Vector2(0, -160);
            
            Image separatorImage = separator.GetComponent<Image>();
            separatorImage.color = PlayerPrefsRuntimeViewConstants.SeparatorColor;

            GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            
            RectTransform scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = new Vector2(20, 20);
            scrollRT.offsetMax = new Vector2(-20, -170);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);

            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = PlayerPrefsRuntimeViewConstants.ScrollBackgroundColor;

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            
            RectTransform viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = PlayerPrefsRuntimeViewConstants.ViewportBackgroundColor;
            
            Mask viewportMask = viewportGo.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            
            RectTransform contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
                vlg.spacing = 6f;
                vlg.padding = new RectOffset(12, 12, 12, 12);
            }

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = PlayerPrefsRuntimeViewConstants.ScrollSensitivity;
            scroll.inertia = true;
            scroll.decelerationRate = PlayerPrefsRuntimeViewConstants.ScrollDecelerationRate;
        }

        private void UpdateHeader(GameObject panel, int entryCount)
        {
            if (panel == null)
                return;

            Transform header = panel.transform.Find("Header");
            if (header == null)
                return;

            Text subtitle = header.Find("Subtitle")?.GetComponent<Text>();
            if (subtitle != null)
            {
                string noun = entryCount == 1 ? "entry" : "entries";
                string searchText = !string.IsNullOrEmpty(m_searchFilter) ? $" (filtered from {m_entriesCache.Count})" : "";
                subtitle.text = $"{entryCount} {noun}{searchText} | Updated {DateTime.Now:HH:mm:ss}";
            }
        }

        private void BuildOrRefreshList(GameObject panel, IReadOnlyList<PlayerPrefsRuntimeEntry> entries)
        {
            if (panel == null)
                return;

            Transform scrollView = panel.transform.Find("ScrollView");
            if (scrollView == null)
                return;
            Transform viewport = scrollView.Find("Viewport");
            if (viewport == null)
                return;
            Transform content = viewport.Find("Content");
            if (content == null)
                return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            }

            bool even = true;
            Font font = GetFont();
            
            // Add null check for font
            if (font == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Failed to get font resource");
                return;
            }
            
            foreach (PlayerPrefsRuntimeEntry entry in entries)
            {
                GameObject row = m_rowBuilder.Create(content, entry, even, font, PlayerPrefsRuntimeViewConstants.RowColorEven, PlayerPrefsRuntimeViewConstants.RowColorOdd, PlayerPrefsRuntimeViewConstants.BadgeColor, OnRowClicked);
                // Check if row creation was successful
                if (row != null)
                {
                    even = !even;
                }
            }
        }

        private void RefreshRows()
        {
            if (m_panelInstance == null)
                return;

            // Add null check for entries cache
            if (m_entriesCache == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Entries cache is null");
                return;
            }

            List<PlayerPrefsRuntimeEntry> sortedEntries = SortEntries(m_entriesCache);
            UpdateHeader(m_panelInstance, sortedEntries.Count);
            BuildOrRefreshList(m_panelInstance, sortedEntries);
            UpdateSortModeButtonLabel();
        }

        private List<PlayerPrefsRuntimeEntry> SortEntries(IEnumerable<PlayerPrefsRuntimeEntry> entries)
        {
            if (entries == null)
                return new List<PlayerPrefsRuntimeEntry>();

            // Apply search filter first
            IEnumerable<PlayerPrefsRuntimeEntry> filteredEntries = entries;
            if (!string.IsNullOrEmpty(m_searchFilter))
            {
                string filter = m_searchFilter.ToLowerInvariant();
                filteredEntries = entries.Where(e => 
                    (e.Name != null && e.Name.ToLowerInvariant().Contains(filter)) || 
                    (e.Type != null && e.Type.ToLowerInvariant().Contains(filter)) || 
                    (e.Value != null && e.Value.ToLowerInvariant().Contains(filter)));
            }

            switch (m_currentSortMode)
            {
                case SortMode.Name:
                    return filteredEntries
                        .OrderBy(e => e.Name, StringComparer.Ordinal)
                        .ThenBy(e => e.Type, StringComparer.Ordinal)
                        .ToList();
                case SortMode.Type:
                    return filteredEntries
                        .OrderBy(e => e.Type, StringComparer.Ordinal)
                        .ThenBy(e => e.Name, StringComparer.Ordinal)
                        .ToList();
                default:
                    return filteredEntries.ToList();
            }
        }

        private void CreateSortModeButton(Transform header)
        {
            if (header.Find(PlayerPrefsRuntimeViewConstants.SortModeButtonName) != null)
                return;

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
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(OnSortModeButtonClicked);
                
            // Add hover effect
            ColorBlock colors = button.colors;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            Text label = CreateText(
                "Label",
                buttonGo.transform,
                PlayerPrefsRuntimeViewConstants.SortModeButtonLabelFontSize,
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter,
                false,
                out GameObject gameObject);
            
            label.alignment = TextAnchor.MiddleCenter;
            gameObject.GetComponent<RectTransform>().sizeDelta = 
                new Vector2(PlayerPrefsRuntimeViewConstants.SortModeButtonWidth, PlayerPrefsRuntimeViewConstants.SortModeButtonHeight);

            UpdateSortModeButtonLabel(header);
        }

        private void CreateSearchField(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for search field");
                return;
            }
            
            string searchFieldName = "SearchField";

            GameObject inputFieldGo = new GameObject(searchFieldName, typeof(RectTransform), typeof(Image));
            inputFieldGo.transform.SetParent(header, false);

            RectTransform inputFieldRT = inputFieldGo.GetComponent<RectTransform>();
            inputFieldRT.anchorMin = new Vector2(0.5f, 0f);
            inputFieldRT.anchorMax = new Vector2(0.5f, 1f);
            inputFieldRT.pivot = new Vector2(0.5f, 0.5f);
            inputFieldRT.sizeDelta = new Vector2(360f, 0f);
            inputFieldRT.anchoredPosition = new Vector2(0f, 0f);

            Image inputFieldImage = inputFieldGo.GetComponent<Image>();
            inputFieldImage.color = PlayerPrefsRuntimeViewConstants.SearchFieldColor;

            Outline outline = inputFieldGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(2f, -2f);
            
            // Add inner shadow for depth
            Shadow innerShadow = inputFieldGo.AddComponent<Shadow>();
            innerShadow.effectColor = PlayerPrefsRuntimeViewConstants.InnerShadowColor;
            innerShadow.effectDistance = new Vector2(0, -1f);

            GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
            textArea.transform.SetParent(inputFieldGo.transform, false);
            RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10, 4);
            textAreaRT.offsetMax = new Vector2(-10, -4);

            Text placeholderText = CreateText("Placeholder", textArea.transform, PlayerPrefsRuntimeViewConstants.SearchPlaceholderFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.PlaceholderTextColor, TextAnchor.MiddleLeft, false, out _);
            placeholderText.text = "Search...";

            Text inputText = CreateText("Text", textArea.transform, PlayerPrefsRuntimeViewConstants.SearchInputFontSize, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft, false, out GameObject textGameObject);
            inputText.text = "";
            textGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 100);

            // Add input field component
            InputField inputField = inputFieldGo.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.onValueChanged.AddListener(OnSearchValueChanged);

            m_searchInputField = inputField;

            // Add hover effect to input field
            Button inputButton = inputFieldGo.AddComponent<Button>();
            if (inputButton != null)
            {
                inputButton.transition = Selectable.Transition.ColorTint;
                ColorBlock inputColors = inputButton.colors;
                inputColors.normalColor = PlayerPrefsRuntimeViewConstants.ControlNormalColor;
                inputColors.highlightedColor = PlayerPrefsRuntimeViewConstants.SearchFieldHighlightedColor;
                inputColors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlNormalColor;
                inputColors.fadeDuration = 0.1f;
                inputButton.colors = inputColors;
            }

            // Add clear button
            GameObject clearButtonGo = new GameObject("ClearButton", typeof(RectTransform), typeof(Image), typeof(Button));
            clearButtonGo.transform.SetParent(header, false);

            RectTransform clearButtonRT = clearButtonGo.GetComponent<RectTransform>();
            clearButtonRT.anchorMin = new Vector2(0.5f, 0f);
            clearButtonRT.anchorMax = new Vector2(0.5f, 1f);
            clearButtonRT.pivot = new Vector2(0f, 0.5f);
            clearButtonRT.sizeDelta = new Vector2(60f, 0f);
            clearButtonRT.anchoredPosition = new Vector2(190f, 0f);

            Image clearButtonImage = clearButtonGo.GetComponent<Image>();
            clearButtonImage.color = PlayerPrefsRuntimeViewConstants.ControlNormalColor;

            Button clearButton = clearButtonGo.GetComponent<Button>();
            clearButton.targetGraphic = clearButtonImage;
            clearButton.onClick.AddListener(OnClearSearchClicked);

            // Add hover effect to clear button
            ColorBlock clearColors = clearButton.colors;
            clearColors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            clearColors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            clearColors.fadeDuration = 0.1f;
            clearButton.colors = clearColors;

            Text clearButtonText = CreateText("Label", clearButtonGo.transform, PlayerPrefsRuntimeViewConstants.ClearButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            clearButtonText.text = "X";
        }
        
        private void OnSearchValueChanged(string value)
        {
            m_searchFilter = value ?? "";
            RefreshRows();
        }
        
        private void OnClearSearchClicked()
        {
            if (m_searchInputField != null)
            {
                m_searchInputField.text = "";
                m_searchFilter = "";
                RefreshRows();
            }
        }
        
        private void CreateCloseButton(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for close button");
                return;
            }
            
            string closeName = "CloseButton";
            if (header.Find(closeName) != null)
                return;

            GameObject closeGo = new GameObject(closeName, typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(header, false);

            RectTransform closeRT = closeGo.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 0f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot = new Vector2(1f, 0.5f);
            closeRT.sizeDelta = new Vector2(70f, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 0f);

            Image closeImage = closeGo.GetComponent<Image>();
            closeImage.color = PlayerPrefsRuntimeViewConstants.CloseButtonNormalColor;

            Button closeButton = closeGo.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(OnCloseButtonClicked);
                
            // Add hover effect
            ColorBlock closeColors = closeButton.colors;
            closeColors.highlightedColor = PlayerPrefsRuntimeViewConstants.CloseButtonHighlightedColor;
            closeColors.pressedColor = PlayerPrefsRuntimeViewConstants.CloseButtonPressedColor;
            closeColors.fadeDuration = 0.1f;
            closeButton.colors = closeColors;

            Text closeText = CreateText("Label", closeGo.transform, PlayerPrefsRuntimeViewConstants.ViewerCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            closeText.text = "X";
        }
        
        public void Hide()
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
            m_entriesCache.Clear();
            m_searchFilter = "";
            m_searchInputField = null;
        }

        private void OnCloseButtonClicked()
        {
            Hide();
        }

        private void OnSortModeButtonClicked()
        {
            m_currentSortMode = m_currentSortMode == SortMode.Name ? SortMode.Type : SortMode.Name;
            RefreshRows();
        }

        private void UpdateSortModeButtonLabel()
        {
            if (m_panelInstance == null)
                return;

            Transform header = m_panelInstance.transform.Find("Header");
            UpdateSortModeButtonLabel(header);
        }

        private void UpdateSortModeButtonLabel(Transform header)
        {
            Text label = GetSortModeButtonLabel(header);

            if (label == null)
                return;

            label.text = m_currentSortMode == SortMode.Name ? "Sort: Name" : "Sort: Type";
        }

        private Text GetSortModeButtonLabel(Transform root)
        {
            if (root == null)
                return null;

            Transform buttonTransform = root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == PlayerPrefsRuntimeViewConstants.SortModeButtonName);

            if (buttonTransform == null)
                return null;

            return buttonTransform.Find("Label")?.GetComponent<Text>();
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
                shadow.effectDistance = new Vector2(1.8f, -1.8f);
            }

            return text;
        }

        private Font GetFont()
        {
            if (m_defaultFont == null)
            {
                m_defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                // Fallback to default font if Arial is not available
                if (m_defaultFont == null)
                {
                    Debug.LogWarning("[PlayerPrefsRuntime] Arial font not found, using default font");
                    m_defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
                    
                    // Final fallback
                    if (m_defaultFont == null)
                    {
                        m_defaultFont = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Helvetica", "Sans-serif" }, 16);
                    }
                }
            }

            return m_defaultFont;
        }

        private void EnsureEventSystemExists()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (es == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Failed to create EventSystem");
                return;
            }
            
            UnityEngine.Object.DontDestroyOnLoad(es);
        }
        
        private void OnRowClicked(PlayerPrefsRuntimeEntry entry)
        {
            if (m_panelInstance == null)
                return;
                
            m_entryDialog.Show(m_panelInstance.transform, entry, GetFont(), OnEntryRemoved);
        }

        private void OnEntryRemoved(PlayerPrefsRuntimeEntry entry)
        {
            if (entry.Name == null)
            {
                return;
            }

            m_entriesCache.RemoveAll(e => string.Equals(e.Name, entry.Name, StringComparison.Ordinal));
            RefreshRows();
        }
    }
}
#endif