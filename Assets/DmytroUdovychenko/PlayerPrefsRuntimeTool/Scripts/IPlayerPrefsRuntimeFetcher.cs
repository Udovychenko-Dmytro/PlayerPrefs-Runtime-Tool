// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public interface IPlayerPrefsRuntimeFetcher
    {
        /// <summary>
        /// Retrieves all PlayerPrefs as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        Dictionary<string, object> GetAllPlayerPrefs();
    }
}
#endif