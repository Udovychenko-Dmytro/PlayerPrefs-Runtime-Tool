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

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Callback bundle wiring the viewer controller to the UI builder.
    /// Unassigned callbacks are simply ignored by the builder.
    /// </summary>
    internal sealed class PlayerPrefsRuntimeViewerCallbacks
    {
        public Action OnSortModeButtonClicked;
        public Action<string> OnSearchValueChanged;
        public Action OnClearSearchClicked;
        public Action OnCloseButtonClicked;
        public Action OnToolsToggleClicked;
        public Action OnAddClicked;
        public Action OnDeleteAllClicked;
        public Action OnExportClicked;
        public Action OnImportClicked;
        public Action<string> OnTypeFilterToggled;
        public Action<Vector2> OnScrollValueChanged;
    }
}
#endif
