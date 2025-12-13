// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Provides runtime access to all PlayerPrefs across multiple platforms.
    /// Supports Windows, macOS, iOS, and Android platforms.
    /// </summary>
    public static class PlayerPrefsRuntime
    {
        private static IPlayerPrefsRuntimeFetcher s_runtimeFetcher;
        private static PlayerPrefsRuntimeViewer s_viewer;
        
        public static bool IsVisible => s_viewer != null && s_viewer.IsVisible;

        /// <summary>
        /// Initializes and returns the appropriate fetcher for the current platform.
        /// </summary>
        /// <returns>Platform-specific PlayerPrefs fetcher implementation</returns>
        private static IPlayerPrefsRuntimeFetcher GetFetcher()
        {
            Debug.Log("[PlayerPrefsRuntime] Initializing fetcher for current platform");

#if UNITY_EDITOR
            Debug.Log("[PlayerPrefsRuntime] Running in Unity Editor");
#if UNITY_EDITOR_WIN
            return new PlayerPrefsRuntimeFetcherWindowsEditor();
#elif UNITY_EDITOR_OSX
            return new PlayerPrefsRuntimeFetcherMacOSEditor();
#else
            Debug.LogWarning("[PlayerPrefsRuntime] PlayerPrefs fetcher is not implemented for this editor platform.");
            return null;
#endif
#else
#if UNITY_ANDROID
            return new PlayerPrefsRuntimeFetcherAndroid();
#elif UNITY_IOS
            return new PlayerPrefsRuntimeFetcherIOS();
#elif UNITY_STANDALONE_WIN
            return new PlayerPrefsRuntimeFetcherWindows();
#elif UNITY_STANDALONE_OSX
            return new PlayerPrefsRuntimeFetcherMacOS();
#else
            Debug.LogWarning("[PlayerPrefsRuntime] PlayerPrefs fetcher is not supported for the current platform.");
            return null;
#endif
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Retrieves all PlayerPrefs as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        public static Dictionary<string, object> GetAllPlayerPrefs()
        {
            if (s_runtimeFetcher == null)
            {
                s_runtimeFetcher = GetFetcher();
            }

            if (s_runtimeFetcher != null)
            {
                return s_runtimeFetcher.GetAllPlayerPrefs();
            }
            else
            {
                Debug.LogWarning("[PlayerPrefsRuntime] PlayerPrefs fetcher does not exist for the current platform.");
                return new Dictionary<string, object>();
            }

        }

        /// <summary>
        /// Logs all PlayerPrefs key-value pairs to the Unity console.
        /// Also shows a Canvas with a scrollable list of entries.
        /// </summary>
        public static void LogAllPlayerPrefs()
        {
            Dictionary<string, object> allPrefs = GetAllPlayerPrefs();
            Debug.Log($"[PlayerPrefsRuntime] Found {allPrefs.Count} PlayerPrefs entries:");
            
            foreach (KeyValuePair<string, object> kvp in allPrefs)
            {
                Debug.Log($"[PlayerPrefsRuntime] Key: {kvp.Key}, Value: {kvp.Value} (Type: {kvp.Value?.GetType().Name ?? "null"})");
            }
        }
        
        public static void ShowAllPlayerPrefs()
        {
            // Log all PlayerPrefs to console
            LogAllPlayerPrefs();

            // Retrieve all PlayerPrefs as Dictionary <key, value>
            Dictionary<string, object> allPrefs = GetAllPlayerPrefs();
            
            if (s_viewer == null)
            {
                s_viewer = new PlayerPrefsRuntimeViewer();
            }

            if (s_viewer.IsVisible)
            {
                s_viewer.Hide();
            }
            
            // Check if allPrefs is null or empty before proceeding
            if (allPrefs == null || allPrefs.Count == 0)
            {
                Debug.Log("[PlayerPrefsRuntime] No PlayerPrefs entries to show.");

                // Pass an empty list to ensure the viewer is in a clean state
                s_viewer.ShowEntries(new List<PlayerPrefsRuntimeEntry>());
            }
            else
            {
                List<PlayerPrefsRuntimeEntry> entries = allPrefs
                    .Select(kvp => new PlayerPrefsRuntimeEntry(kvp.Key, kvp.Value))
                    .ToList();
            
                s_viewer.ShowEntries(entries);

                Debug.Log($"[PlayerPrefsRuntimeExample] Example completed. Found {allPrefs.Count} PlayerPrefs entries.");
            }
        }
    }
}
#endif