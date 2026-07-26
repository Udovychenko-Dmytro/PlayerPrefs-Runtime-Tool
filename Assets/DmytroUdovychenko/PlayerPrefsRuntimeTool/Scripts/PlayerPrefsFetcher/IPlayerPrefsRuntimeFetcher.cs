// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Interface for platform-specific PlayerPrefs fetcher implementations.
    /// </summary>
    public interface IPlayerPrefsRuntimeFetcher
    {
        /// <summary>
        /// Retrieves all PlayerPrefs as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        Dictionary<string, object> GetAllPlayerPrefs();
    }

    /// <summary>
    /// Internal capability implemented by built-in fetchers that can report whether
    /// enumeration completed without dropping or transforming unknown entries.
    /// Public reads may still use partial results, but destructive operations require
    /// this stronger completeness guarantee.
    /// </summary>
    internal interface IPlayerPrefsRuntimeFetcherWithStatus
    {
        bool TryGetAllPlayerPrefs(out Dictionary<string, object> prefs);
    }
}
#endif
