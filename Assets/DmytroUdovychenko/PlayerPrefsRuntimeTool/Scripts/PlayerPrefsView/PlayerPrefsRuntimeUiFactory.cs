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
    /// Shared factory for the UI primitives used across the viewer and its dialogs:
    /// themed texts, action buttons, modal overlays, dialog panels, fonts and the EventSystem.
    /// </summary>
    internal static class PlayerPrefsRuntimeUiFactory
    {
        private static Font s_defaultFont;

        /// <summary>
        /// Resolves (and caches) the default font used by the tool's UI.
        /// </summary>
        public static Font ResolveDefaultFont()
        {
            if (s_defaultFont == null)
            {
#if UNITY_2022_2_OR_NEWER
                s_defaultFont = Resources.GetBuiltinResource<Font>(PlayerPrefsRuntimeViewConstants.LegacyFontName);
#else
                s_defaultFont = Resources.GetBuiltinResource<Font>(PlayerPrefsRuntimeViewConstants.ArialFontName);
#endif

                if (s_defaultFont == null)
                {
                    Debug.LogWarning("[PlayerPrefsRuntime] Built-in font not found, using default font");
                    s_defaultFont = Font.CreateDynamicFontFromOSFont(PlayerPrefsRuntimeViewConstants.DefaultFontName, 16);

                    if (s_defaultFont == null)
                    {
                        s_defaultFont = Font.CreateDynamicFontFromOSFont(new string[] { PlayerPrefsRuntimeViewConstants.DefaultFontName, "Helvetica", "Sans-serif" }, 16);
                    }
                }
            }

            return s_defaultFont;
        }

        /// <summary>
        /// Makes sure an EventSystem with the input module matching the active input backend exists.
        /// </summary>
        public static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
#else
            EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
#endif
            if (eventSystem != null)
            {
                // Respect a host EventSystem that already has a working input module: never disable
                // it (that would break the host UI, e.g. a StandaloneInputModule kept in "Both" mode).
                BaseInputModule existingModule = eventSystem.GetComponent<BaseInputModule>();
                if (existingModule != null && existingModule.enabled)
                {
                    return;
                }

                EnsureEventSystemInputModule(eventSystem);
                return;
            }

            GameObject eventSystemGo = new GameObject(PlayerPrefsRuntimeViewConstants.EventSystemName, typeof(EventSystem));
            if (eventSystemGo == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Failed to create EventSystem");
                return;
            }

            UnityEngine.Object.DontDestroyOnLoad(eventSystemGo);
            EnsureEventSystemInputModule(eventSystemGo.GetComponent<EventSystem>());
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
        public static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color, TextAnchor anchor, bool emphasize, Font font, out GameObject gameObject)
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

        public static Button CreateActionButton(string name, Transform parent, string label, Font font, out GameObject buttonGo, out GameObject textGo)
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

        /// <summary>
        /// Recolors a button (created via <see cref="CreateActionButton"/>) into the red "danger" family.
        /// </summary>
        public static void ApplyDangerColors(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = PlayerPrefsRuntimeViewConstants.CloseButtonNormalColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = PlayerPrefsRuntimeViewConstants.CloseButtonNormalColor;
            colors.highlightedColor = PlayerPrefsRuntimeViewConstants.CloseButtonHighlightedColor;
            colors.pressedColor = PlayerPrefsRuntimeViewConstants.CloseButtonPressedColor;
            button.colors = colors;
        }

        /// <summary>
        /// Creates a full-screen overlay with a click-to-dismiss backdrop. Returns the overlay root.
        /// </summary>
        public static GameObject CreateOverlay(string overlayName, Transform parent, Action onBackdropClick, float backdropAlpha)
        {
            GameObject overlay = new GameObject(overlayName, typeof(RectTransform));
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
            Color backdropColor = PlayerPrefsRuntimeViewConstants.BackdropColor;
            backdropImage.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, backdropAlpha);

            Button backdropButton = backdrop.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            if (onBackdropClick != null)
            {
                backdropButton.onClick.AddListener(() => onBackdropClick());
            }

            return overlay;
        }

        /// <summary>
        /// Creates a themed dialog panel (image + outline) inside an overlay.
        /// </summary>
        public static GameObject CreateDialogPanel(Transform overlay, string dialogName, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject dialog = new GameObject(dialogName, typeof(RectTransform), typeof(Image), typeof(Outline));
            dialog.transform.SetParent(overlay, false);
            dialog.transform.SetAsLastSibling();

            RectTransform dialogRT = dialog.GetComponent<RectTransform>();
            dialogRT.anchorMin = anchorMin;
            dialogRT.anchorMax = anchorMax;
            dialogRT.offsetMin = Vector2.zero;
            dialogRT.offsetMax = Vector2.zero;
            dialogRT.pivot = new Vector2(0.5f, 0.5f);

            Image dialogImage = dialog.GetComponent<Image>();
            dialogImage.color = PlayerPrefsRuntimeViewConstants.PanelColor;

            Outline dialogOutline = dialog.GetComponent<Outline>();
            dialogOutline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            dialogOutline.effectDistance = new Vector2(3f, -3f);

            return dialog;
        }

        /// <summary>
        /// Creates a themed input field with placeholder. Sizing/layout is left to the caller.
        /// </summary>
        public static InputField CreateInputField(string name, Transform parent, string placeholder, bool multiline, Font font, out GameObject fieldGo)
        {
            fieldGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(parent, false);

            Image fieldImage = fieldGo.GetComponent<Image>();
            fieldImage.color = PlayerPrefsRuntimeViewConstants.SearchFieldColor;

            Outline outline = fieldGo.AddComponent<Outline>();
            outline.effectColor = PlayerPrefsRuntimeViewConstants.OutlineEffectColor;
            outline.effectDistance = new Vector2(2f, -2f);

            Text placeholderText = CreateText(PlayerPrefsRuntimeViewConstants.PlaceholderName, fieldGo.transform, PlayerPrefsRuntimeViewConstants.DialogValueFontSize, FontStyle.Italic, PlayerPrefsRuntimeViewConstants.PlaceholderTextColor, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, false, font, out GameObject placeholderGo);
            placeholderText.text = placeholder;

            Text inputText = CreateText(PlayerPrefsRuntimeViewConstants.TextComponentName, fieldGo.transform, PlayerPrefsRuntimeViewConstants.DialogValueFontSize, FontStyle.Normal, Color.white, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, false, font, out GameObject inputTextGo);
            inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputText.verticalOverflow = VerticalWrapMode.Overflow;

            StretchWithPadding(placeholderGo.GetComponent<RectTransform>(), 10f, 6f);
            StretchWithPadding(inputTextGo.GetComponent<RectTransform>(), 10f, 6f);

            InputField inputField = fieldGo.GetComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;

            return inputField;
        }

        private static void StretchWithPadding(RectTransform rectTransform, float horizontalPadding, float verticalPadding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        /// <summary>
        /// Creates the round red close button pinned to the dialog's top-right corner.
        /// </summary>
        public static Button CreateDialogCloseButton(Transform dialog, Font font, Action onClick)
        {
            GameObject closeGo = new GameObject(PlayerPrefsRuntimeViewConstants.CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(dialog, false);

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
            if (onClick != null)
            {
                closeButton.onClick.AddListener(() => onClick());
            }

            ColorBlock closeColors = closeButton.colors;
            closeColors.highlightedColor = PlayerPrefsRuntimeViewConstants.CloseButtonHighlightedColor;
            closeColors.pressedColor = PlayerPrefsRuntimeViewConstants.CloseButtonPressedColor;
            closeColors.fadeDuration = 0.1f;
            closeButton.colors = closeColors;

            Text closeText = CreateText(PlayerPrefsRuntimeViewConstants.LabelName, closeGo.transform, PlayerPrefsRuntimeViewConstants.DialogCloseButtonFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, false, font, out _);
            if (closeText != null)
            {
                closeText.text = PlayerPrefsRuntimeViewConstants.CloseButtonText;
            }

            return closeButton;
        }
    }
}
#endif
