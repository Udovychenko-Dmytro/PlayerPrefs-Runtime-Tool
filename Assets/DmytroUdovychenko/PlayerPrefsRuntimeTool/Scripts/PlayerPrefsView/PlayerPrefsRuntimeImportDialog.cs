// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Modal dialog for importing PlayerPrefs from a pasted JSON document.
    /// Stays open after import so the per-key error report remains readable;
    /// the viewer refreshes behind it via the completion callback.
    /// </summary>
    internal class PlayerPrefsRuntimeImportDialog
    {
        private GameObject m_dialogRoot;
        private InputField m_jsonInputField;
        private Text m_resultText;
        private Action<PlayerPrefsRuntimeImportResult> m_onImportCompleted;

        public void Show(Transform parent, Font font, Action<PlayerPrefsRuntimeImportResult> onImportCompleted)
        {
            if (parent == null)
            {
                return;
            }

            Close();

            m_onImportCompleted = onImportCompleted;

            Font resolvedFont = font != null ? font : PlayerPrefsRuntimeUiFactory.ResolveDefaultFont();

            GameObject overlay = PlayerPrefsRuntimeUiFactory.CreateOverlay(PlayerPrefsRuntimeViewConstants.ImportOverlayName, parent, Close, 0.85f);
            GameObject dialog = PlayerPrefsRuntimeUiFactory.CreateDialogPanel(overlay.transform, PlayerPrefsRuntimeViewConstants.ImportDialogName, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));

            GameObject contentRoot = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentRoot.transform.SetParent(dialog.transform, false);

            RectTransform contentRT = contentRoot.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(25f, 20f);
            contentRT.offsetMax = new Vector2(-25f, -25f);

            VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.spacing = 14f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;

            Text title = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.TitleName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, true, resolvedFont, out _);
            title.text = PlayerPrefsRuntimeViewConstants.ImportDialogTitleText;
            AddFixedHeight(title.gameObject, 44f);

            Text hint = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.LabelName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.DialogValueLabelFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.MiddleLeft, false, resolvedFont, out _);
            hint.text = PlayerPrefsRuntimeViewConstants.ImportHintText;
            AddFixedHeight(hint.gameObject, 30f);

            m_jsonInputField = PlayerPrefsRuntimeUiFactory.CreateInputField(PlayerPrefsRuntimeViewConstants.ValueComponentName, contentRoot.transform, "{ ... }", true, resolvedFont, out GameObject jsonFieldGo);
            LayoutElement jsonFieldLayout = jsonFieldGo.AddComponent<LayoutElement>();
            jsonFieldLayout.minHeight = PlayerPrefsRuntimeViewConstants.ImportTextAreaMinHeight;
            jsonFieldLayout.flexibleHeight = 1f;

            Text resultText = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.ErrorTextName, contentRoot.transform, 24, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.UpperLeft, false, resolvedFont, out GameObject resultTextGo);
            resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultText.verticalOverflow = VerticalWrapMode.Truncate;
            resultText.text = "";
            LayoutElement resultLayout = resultTextGo.AddComponent<LayoutElement>();
            resultLayout.preferredHeight = 0f;
            resultLayout.flexibleWidth = 1f;
            resultTextGo.SetActive(false);
            m_resultText = resultText;

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

            Button pasteButton = PlayerPrefsRuntimeUiFactory.CreateActionButton(PlayerPrefsRuntimeViewConstants.ClearButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.PasteLabel, resolvedFont, out _, out _);
            pasteButton.onClick.AddListener(OnPasteClicked);

            Button importButton = PlayerPrefsRuntimeUiFactory.CreateActionButton(PlayerPrefsRuntimeViewConstants.ImportButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.ImportLabel, resolvedFont, out _, out _);
            importButton.onClick.AddListener(OnImportClicked);

            PlayerPrefsRuntimeUiFactory.CreateDialogCloseButton(dialog.transform, resolvedFont, Close);

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
            m_jsonInputField = null;
            m_resultText = null;
        }

        private void OnPasteClicked()
        {
            if (m_jsonInputField == null)
            {
                return;
            }

            // Programmatic paste: clipboard access inside uGUI InputFields is unreliable on mobile.
            m_jsonInputField.text = GUIUtility.systemCopyBuffer ?? string.Empty;
        }

        private void OnImportClicked()
        {
            if (m_jsonInputField == null)
            {
                return;
            }

            PlayerPrefsRuntimeImportResult result = PlayerPrefsRuntimeWriter.ImportFromJson(m_jsonInputField.text);
            ShowResult(result);

            if (result.ImportedCount > 0)
            {
                m_onImportCompleted?.Invoke(result);
            }
        }

        private void ShowResult(PlayerPrefsRuntimeImportResult result)
        {
            if (m_resultText == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(PlayerPrefsRuntimeViewConstants.ImportResultFormat, result.ImportedCount, result.Errors.Count);

            int displayedErrors = Mathf.Min(result.Errors.Count, PlayerPrefsRuntimeViewConstants.ImportMaxDisplayedErrors);
            for (int i = 0; i < displayedErrors; i++)
            {
                PlayerPrefsRuntimeImportError error = result.Errors[i];
                builder.AppendLine();
                builder.Append(string.IsNullOrEmpty(error.Key) ? "-" : error.Key);
                builder.Append(": ");
                builder.Append(error.Message);
            }

            if (result.Errors.Count > displayedErrors)
            {
                builder.AppendLine();
                builder.AppendFormat(PlayerPrefsRuntimeViewConstants.ImportMoreErrorsFormat, result.Errors.Count - displayedErrors);
            }

            m_resultText.color = result.HasErrors ? new Color(1f, 0.5f, 0.4f, 1f) : PlayerPrefsRuntimeViewConstants.AccentColor;
            m_resultText.text = builder.ToString();

            LayoutElement resultLayout = m_resultText.GetComponent<LayoutElement>();
            if (resultLayout != null)
            {
                resultLayout.preferredHeight = -1f;
            }

            m_resultText.gameObject.SetActive(true);
        }

        private static void AddFixedHeight(GameObject target, float height)
        {
            LayoutElement layout = target.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }
    }
}
#endif
