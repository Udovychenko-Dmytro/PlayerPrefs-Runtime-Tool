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
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Builds and manages the runtime viewer UI hierarchy.
    /// </summary>
    internal class PlayerPrefsRuntimeViewerBuilder
    {
        private readonly Action<string> m_onSearchValueChanged;
        private readonly Action m_onSortModeButtonClicked;
        private readonly Action m_onClearSearchClicked;
        private readonly Action m_onCloseButtonClicked;

        private GameObject m_panelInstance;
        private RectTransform m_contentRoot;
        private InputField m_searchInputField;
        private Text m_subtitleText;
        private Text m_sortModeLabel;
        private Font m_defaultFont;

        public bool IsVisible => m_panelInstance != null;

        public RectTransform ContentRoot => m_contentRoot;

        public Transform PanelTransform => m_panelInstance != null ? m_panelInstance.transform : null;

        public Font DefaultFont => GetFont();

        public PlayerPrefsRuntimeViewerBuilder(
            Action onSortModeButtonClicked,
            Action<string> onSearchValueChanged,
            Action onClearSearchClicked,
            Action onCloseButtonClicked)
        {
            m_onSortModeButtonClicked = onSortModeButtonClicked;
            m_onSearchValueChanged = onSearchValueChanged;
            m_onClearSearchClicked = onClearSearchClicked;
            m_onCloseButtonClicked = onCloseButtonClicked;
        }

        public void EnsureVisible()
        {
            if (m_panelInstance != null)
            {
                RectTransform panelTransform = m_panelInstance.GetComponent<RectTransform>();
                ApplySafeArea(panelTransform);
                return;
            }

            EnsureEventSystemExists();
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
            m_searchInputField = null;
            m_subtitleText = null;
            m_sortModeLabel = null;
        }

        public void SetSearchText(string text)
        {
            if (m_searchInputField == null)
                return;

            m_searchInputField.text = text ?? string.Empty;
        }

        public Coroutine StartCoroutine(System.Collections.IEnumerator routine)
        {
            if (m_panelInstance == null)
                return null;

            return m_panelInstance.GetComponent<Image>().StartCoroutine(routine);
        }

        public void StopCoroutine(Coroutine routine)
        {
            if (m_panelInstance == null || routine == null)
                return;

            m_panelInstance.GetComponent<Image>().StopCoroutine(routine);
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

        private void BuildViewerPanel()
        {
            GameObject canvasGo = EnsureCanvas();
            EnsureBackdrop(canvasGo.transform);

            Transform existingPanel = canvasGo.transform.Find(PlayerPrefsRuntimeViewConstants.PanelName);
            if (existingPanel != null)
            {
                m_panelInstance = existingPanel.gameObject;
                ApplySafeArea(existingPanel.GetComponent<RectTransform>());
                CacheExistingReferences(existingPanel);
                return;
            }

            Transform legacyScroll = canvasGo.transform.Find(PlayerPrefsRuntimeViewConstants.ScrollViewName);
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
        }

        private void CacheExistingReferences(Transform panel)
        {
            if (panel == null)
                return;

            Transform header = panel.Find(PlayerPrefsRuntimeViewConstants.HeaderName);
            if (header != null)
            {
                Text subtitle = header.Find(PlayerPrefsRuntimeViewConstants.SubtitleName)?.GetComponent<Text>();
                if (subtitle != null)
                {
                    m_subtitleText = subtitle;
                }

                Text sortLabel = FindSortModeButtonLabel(header);
                if (sortLabel != null)
                {
                    m_sortModeLabel = sortLabel;
                }

                InputField searchField = FindSearchInputField(header);
                if (searchField != null)
                {
                    m_searchInputField = searchField;
                }
            }

            Transform scrollView = panel.Find(PlayerPrefsRuntimeViewConstants.ScrollViewName);
            if (scrollView == null)
                return;

            Transform viewport = scrollView.Find(PlayerPrefsRuntimeViewConstants.ViewportName);
            if (viewport == null)
                return;

            RectTransform content = viewport.Find(PlayerPrefsRuntimeViewConstants.ContentName) as RectTransform;
            if (content != null)
            {
                m_contentRoot = content;
            }
        }

        private Text FindSortModeButtonLabel(Transform root)
        {
            if (root == null)
                return null;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current = transforms[i];
                if (!string.Equals(current.name, PlayerPrefsRuntimeViewConstants.SortModeButtonName, StringComparison.Ordinal))
                {
                    continue;
                }

                Transform labelTransform = current.Find(PlayerPrefsRuntimeViewConstants.LabelName);
                if (labelTransform != null)
                {
                    return labelTransform.GetComponent<Text>();
                }
            }

            return null;
        }

        private InputField FindSearchInputField(Transform root)
        {
            if (root == null)
                return null;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current = transforms[i];
                if (!string.Equals(current.name, PlayerPrefsRuntimeViewConstants.SearchFieldName, StringComparison.Ordinal))
                {
                    continue;
                }

                return current.GetComponent<InputField>();
            }

            return null;
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
            Transform existingHeader = panel.Find(PlayerPrefsRuntimeViewConstants.HeaderName);
            if (existingHeader != null)
            {
                Transform controlsRoot = GetHeaderControlsRoot(existingHeader);
                CreateSortModeButton(controlsRoot);
                CreateSearchField(controlsRoot);
                CreateCloseButton(controlsRoot);
                ConfigureHeaderTextLayout(existingHeader);
                return;
            }

            GameObject header = new GameObject(PlayerPrefsRuntimeViewConstants.HeaderName, typeof(RectTransform), typeof(Image));
            header.transform.SetParent(panel, false);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.sizeDelta = new Vector2(0, 160);
            headerRT.anchoredPosition = Vector2.zero;

            Image headerImage = header.GetComponent<Image>();
            headerImage.color = PlayerPrefsRuntimeViewConstants.HeaderColor;

            GameObject topBar = new GameObject(PlayerPrefsRuntimeViewConstants.TopBarName, typeof(RectTransform));
            topBar.transform.SetParent(header.transform, false);
            RectTransform topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.anchorMin = new Vector2(0, 0.55f);
            topBarRT.anchorMax = new Vector2(1, 1);
            topBarRT.pivot = new Vector2(0.5f, 1f);
            topBarRT.offsetMin = new Vector2(30f, 0f);
            topBarRT.offsetMax = new Vector2(-20f, -15f);

            CreateSortModeButton(topBar.transform);
            CreateSearchField(topBar.transform);
            CreateCloseButton(topBar.transform);

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
            accentRT.sizeDelta = new Vector2(0, 6f);
            accentRT.anchoredPosition = new Vector2(0, -3f);
            accent.GetComponent<Image>().color = PlayerPrefsRuntimeViewConstants.AccentColor;
        }

        private Transform GetHeaderControlsRoot(Transform header)
        {
            if (header == null)
                return null;

            Transform topBar = header.Find(PlayerPrefsRuntimeViewConstants.TopBarName);
            if (topBar != null)
                return topBar;

            return header;
        }

        private void ConfigureHeaderTextLayout(Transform header)
        {
            ConfigureTitleLayout(header.Find(PlayerPrefsRuntimeViewConstants.TitleName)?.GetComponent<Text>());
            ConfigureSubtitleLayout(header.Find(PlayerPrefsRuntimeViewConstants.SubtitleName)?.GetComponent<Text>());
        }

        private void ConfigureTitleLayout(Text titleText)
        {
            if (titleText == null)
                return;

            RectTransform titleRT = titleText.rectTransform;
            titleText.alignment = TextAnchor.MiddleRight;
            titleRT.anchorMin = new Vector2(0.45f, 0.05f);
            titleRT.anchorMax = new Vector2(1f, 0.45f);
            titleRT.offsetMin = new Vector2(20f, 0f);
            titleRT.offsetMax = new Vector2(-30f, 0f);
        }

        private void ConfigureSubtitleLayout(Text subtitleText)
        {
            if (subtitleText == null)
                return;

            RectTransform subtitleRT = subtitleText.rectTransform;
            subtitleText.alignment = TextAnchor.MiddleLeft;
            subtitleRT.anchorMin = new Vector2(0f, 0.05f);
            subtitleRT.anchorMax = new Vector2(0.55f, 0.45f);
            subtitleRT.offsetMin = new Vector2(30f, 0f);
            subtitleRT.offsetMax = new Vector2(-20f, 0f);
        }

        private void CreateScrollArea(Transform panel)
        {
            if (panel.Find(PlayerPrefsRuntimeViewConstants.ScrollViewName) != null)
                return;

            GameObject separator = new GameObject(PlayerPrefsRuntimeViewConstants.SeparatorName, typeof(RectTransform), typeof(Image));
            separator.transform.SetParent(panel, false);

            RectTransform separatorRT = separator.GetComponent<RectTransform>();
            separatorRT.anchorMin = new Vector2(0, 1);
            separatorRT.anchorMax = new Vector2(1, 1);
            separatorRT.pivot = new Vector2(0.5f, 1f);
            separatorRT.sizeDelta = new Vector2(0, 2f);
            separatorRT.anchoredPosition = new Vector2(0, -160);

            Image separatorImage = separator.GetComponent<Image>();
            separatorImage.color = PlayerPrefsRuntimeViewConstants.SeparatorColor;

            GameObject scrollGo = new GameObject(PlayerPrefsRuntimeViewConstants.ScrollViewName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);

            RectTransform scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = new Vector2(20, 20);
            scrollRT.offsetMax = new Vector2(-20, -170);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);

            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = PlayerPrefsRuntimeViewConstants.ScrollBackgroundColor;

            GameObject viewportGo = new GameObject(PlayerPrefsRuntimeViewConstants.ViewportName, typeof(RectTransform), typeof(Image), typeof(Mask));
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

            GameObject contentGo = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);

            RectTransform contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);

            m_contentRoot = contentRT;

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

        private void CreateSortModeButton(Transform header)
        {
            if (header == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Header transform is null for sort mode button");
                return;
            }

            Transform existingButton = header.Find(PlayerPrefsRuntimeViewConstants.SortModeButtonName);
            if (existingButton != null)
            {
                if (m_sortModeLabel == null)
                {
                    Transform labelTransform = existingButton.Find(PlayerPrefsRuntimeViewConstants.LabelName);
                    if (labelTransform != null)
                    {
                        m_sortModeLabel = labelTransform.GetComponent<Text>();
                    }
                }
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
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(HandleSortModeButtonClicked);

            ColorBlock colors = button.colors;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            colors.fadeDuration = 0.1f;
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

            string searchFieldName = PlayerPrefsRuntimeViewConstants.SearchFieldName;
            Transform existingSearchField = header.Find(searchFieldName);
            if (existingSearchField != null)
            {
                InputField existingInput = existingSearchField.GetComponent<InputField>();
                if (existingInput != null)
                {
                    m_searchInputField = existingInput;
                }
                return;
            }

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

            Shadow innerShadow = inputFieldGo.AddComponent<Shadow>();
            innerShadow.effectColor = PlayerPrefsRuntimeViewConstants.InnerShadowColor;
            innerShadow.effectDistance = new Vector2(0, -1f);

            GameObject textArea = new GameObject(PlayerPrefsRuntimeViewConstants.TextAreaName, typeof(RectTransform));
            textArea.transform.SetParent(inputFieldGo.transform, false);
            RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10, 4);
            textAreaRT.offsetMax = new Vector2(-10, -4);

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
                textGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 100);
            }

            InputField inputField = inputFieldGo.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.onValueChanged.AddListener(HandleSearchValueChanged);

            m_searchInputField = inputField;

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

            GameObject clearButtonGo = new GameObject(PlayerPrefsRuntimeViewConstants.ClearButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
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
            clearButton.onClick.AddListener(HandleClearSearchClicked);

            ColorBlock clearColors = clearButton.colors;
            clearColors.highlightedColor = PlayerPrefsRuntimeViewConstants.ControlHighlightedColor;
            clearColors.pressedColor = PlayerPrefsRuntimeViewConstants.ControlPressedColor;
            clearColors.fadeDuration = 0.1f;
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

            string closeName = PlayerPrefsRuntimeViewConstants.CloseButtonName;
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
            closeButton.onClick.AddListener(HandleCloseButtonClicked);

            ColorBlock closeColors = closeButton.colors;
            closeColors.highlightedColor = PlayerPrefsRuntimeViewConstants.CloseButtonHighlightedColor;
            closeColors.pressedColor = PlayerPrefsRuntimeViewConstants.CloseButtonPressedColor;
            closeColors.fadeDuration = 0.1f;
            closeButton.colors = closeColors;

            Text closeText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, closeGo.transform, PlayerPrefsRuntimeViewConstants.ViewerCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, out _);
            if (closeText != null)
            {
                closeText.text = PlayerPrefsRuntimeViewConstants.CloseButtonText;
            }
        }

        private void HandleSortModeButtonClicked()
        {
            if (m_onSortModeButtonClicked != null)
            {
                m_onSortModeButtonClicked();
            }
        }

        private void HandleSearchValueChanged(string value)
        {
            if (m_onSearchValueChanged != null)
            {
                m_onSearchValueChanged(value);
            }
        }

        private void HandleClearSearchClicked()
        {
            if (m_onClearSearchClicked != null)
            {
                m_onClearSearchClicked();
            }
        }

        private void HandleCloseButtonClicked()
        {
            if (m_onCloseButtonClicked != null)
            {
                m_onCloseButtonClicked();
            }
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
#if UNITY_2022_2_OR_NEWER
                m_defaultFont = Resources.GetBuiltinResource<Font>(PlayerPrefsRuntimeViewConstants.LegacyFontName);
#else
                m_defaultFont = Resources.GetBuiltinResource<Font>(PlayerPrefsRuntimeViewConstants.ArialFontName);
#endif

                if (m_defaultFont == null)
                {
                    Debug.LogWarning("[PlayerPrefsRuntime] Built-in font not found, using default font");
                    m_defaultFont = Font.CreateDynamicFontFromOSFont(PlayerPrefsRuntimeViewConstants.DefaultFontName, 16);

                    if (m_defaultFont == null)
                    {
                        m_defaultFont = Font.CreateDynamicFontFromOSFont(new string[] { PlayerPrefsRuntimeViewConstants.DefaultFontName, "Helvetica", "Sans-serif" }, 16);
                    }
                }
            }

            return m_defaultFont;
        }

        private void EnsureEventSystemExists()
        {
#if UNITY_2023_1_OR_NEWER
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
#else
            EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
#endif
            if (eventSystem == null)
            {
                GameObject es = new GameObject(PlayerPrefsRuntimeViewConstants.EventSystemName, typeof(EventSystem));
                if (es == null)
                {
                    Debug.LogWarning("[PlayerPrefsRuntime] Failed to create EventSystem");
                    return;
                }

                UnityEngine.Object.DontDestroyOnLoad(es);
                eventSystem = es.GetComponent<EventSystem>();
            }

            EnsureEventSystemInputModule(eventSystem);
        }

        private static void EnsureEventSystemInputModule(EventSystem eventSystem)
        {
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputSystemModule != null)
            {
                inputSystemModule.enabled = true;
            }

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                // Disable legacy module if we are specifically using the new Input System for UI.
                legacyModule.enabled = false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }
}
#endif