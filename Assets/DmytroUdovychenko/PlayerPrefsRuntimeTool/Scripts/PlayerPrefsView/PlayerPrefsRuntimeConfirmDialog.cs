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
    /// Generic modal confirmation dialog for destructive operations.
    /// Backdrop click and the Cancel button dismiss without confirming.
    /// </summary>
    internal class PlayerPrefsRuntimeConfirmDialog
    {
        private GameObject m_dialogRoot;

        public void Show(Transform parent, Font font, string title, string message, string confirmLabel, Action onConfirm)
        {
            if (parent == null)
            {
                return;
            }

            Close();

            Font resolvedFont = font != null ? font : PlayerPrefsRuntimeUiFactory.ResolveDefaultFont();

            GameObject overlay = PlayerPrefsRuntimeUiFactory.CreateOverlay(PlayerPrefsRuntimeViewConstants.ConfirmOverlayName, parent, Close, 0.85f);
            GameObject dialog = PlayerPrefsRuntimeUiFactory.CreateDialogPanel(overlay.transform, PlayerPrefsRuntimeViewConstants.ConfirmDialogName, new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.68f));

            GameObject contentRoot = new GameObject(PlayerPrefsRuntimeViewConstants.ContentName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentRoot.transform.SetParent(dialog.transform, false);

            RectTransform contentRT = contentRoot.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(25f, 20f);
            contentRT.offsetMax = new Vector2(-25f, -20f);

            VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.spacing = 20f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;

            Text titleText = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.TitleName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.ConfirmTitleFontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, true, resolvedFont, out _);
            if (titleText != null)
            {
                titleText.text = title;
                LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
                titleLayout.preferredHeight = 44f;
                titleLayout.flexibleHeight = 0f;
            }

            Text messageText = PlayerPrefsRuntimeUiFactory.CreateText(PlayerPrefsRuntimeViewConstants.LabelName, contentRoot.transform, PlayerPrefsRuntimeViewConstants.ConfirmMessageFontSize, FontStyle.Normal, PlayerPrefsRuntimeViewConstants.ValueTextColor, TextAnchor.UpperLeft, false, resolvedFont, out _);
            if (messageText != null)
            {
                messageText.text = message;
                LayoutElement messageLayout = messageText.gameObject.AddComponent<LayoutElement>();
                messageLayout.flexibleHeight = 1f;
                messageLayout.flexibleWidth = 1f;
            }

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

            Button cancelButton = PlayerPrefsRuntimeUiFactory.CreateActionButton(PlayerPrefsRuntimeViewConstants.CancelButtonName, actions.transform, PlayerPrefsRuntimeViewConstants.CancelLabel, resolvedFont, out _, out _);
            cancelButton.onClick.AddListener(Close);

            Button confirmButton = PlayerPrefsRuntimeUiFactory.CreateActionButton(PlayerPrefsRuntimeViewConstants.ConfirmButtonName, actions.transform, confirmLabel, resolvedFont, out _, out _);
            PlayerPrefsRuntimeUiFactory.ApplyDangerColors(confirmButton);
            confirmButton.onClick.AddListener(() =>
            {
                Close();
                onConfirm?.Invoke();
            });

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
    }
}
#endif
