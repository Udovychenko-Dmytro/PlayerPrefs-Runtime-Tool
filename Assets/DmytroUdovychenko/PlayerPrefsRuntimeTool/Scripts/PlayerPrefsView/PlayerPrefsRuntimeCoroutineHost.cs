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
    /// Empty MonoBehaviour whose only job is to own the viewer's coroutines (search debounce,
    /// status messages). It lives on the generated viewer panel and dies with it, so the
    /// coroutines stop automatically when the viewer is destroyed.
    /// </summary>
    internal sealed class PlayerPrefsRuntimeCoroutineHost : MonoBehaviour
    {
    }
}
#endif
