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
    internal class PlayerPrefsRuntimeRowView : MonoBehaviour
    {
        public Text NameText { get; private set; }
        public Text ValueText { get; private set; }
        public Image RowImage { get; private set; }
        public Image BadgeImage { get; private set; }
        public Text BadgeLabel { get; private set; }
        public Button Button { get; private set; }

        private PlayerPrefsRuntimeEntry m_entry;
        private Action<PlayerPrefsRuntimeEntry> m_onClick;
        private bool m_isInitialized;

        public void Initialize(Text nameText, Text valueText, Image rowImage, Image badgeImage, Text badgeLabel, Button button)
        {
            NameText = nameText;
            ValueText = valueText;
            RowImage = rowImage;
            BadgeImage = badgeImage;
            BadgeLabel = badgeLabel;
            Button = button;

            if (!m_isInitialized && Button != null)
            {
                Button.onClick.AddListener(OnButtonClicked);
                m_isInitialized = true;
            }
        }

        public void Bind(PlayerPrefsRuntimeEntry entry, Action<PlayerPrefsRuntimeEntry> onClick)
        {
            m_entry = entry;
            m_onClick = onClick;
        }

        private void OnButtonClicked()
        {
            if (m_onClick == null)
                return;

            m_onClick(m_entry);
        }
    }
}
#endif
