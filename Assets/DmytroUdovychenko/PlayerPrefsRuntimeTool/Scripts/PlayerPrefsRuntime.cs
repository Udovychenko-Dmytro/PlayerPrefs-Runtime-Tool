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
using System.Collections.Generic;
using System.Linq;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Provides runtime access to all PlayerPrefs across multiple platforms:
    /// retrieval, viewing, editing, export/import. Supports Windows, macOS, iOS,
    /// Android and WebGL platforms.
    /// </summary>
    public static class PlayerPrefsRuntime
    {
        /// <summary>
        /// Version of the PlayerPrefsRuntime Tool package.
        /// </summary>
        public const string Version = "3.1.0";

        private static IPlayerPrefsRuntimeFetcher s_runtimeFetcher;
        private static PlayerPrefsRuntimeViewer s_viewer;

        public static bool IsVisible => s_viewer != null && s_viewer.IsVisible;

        /// <summary>
        /// Raised after a PlayerPrefs entry is changed through this tool (set, deleted or imported).
        /// The argument is the affected key; it is null when all entries were removed via <see cref="DeleteAll"/>.
        /// </summary>
        public static event Action<string> OnEntryChanged;

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
#elif UNITY_WEBGL
            return new PlayerPrefsRuntimeFetcherWebGL();
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
            TryGetAllPlayerPrefs(out Dictionary<string, object> prefs);
            return prefs;
        }

        /// <summary>
        /// Retrieves all available entries and reports whether the platform fetcher
        /// proved that the snapshot is complete enough for a destructive operation.
        /// </summary>
        internal static bool TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            bool saveSucceeded = true;
            try
            {
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Failed to flush PlayerPrefs before enumeration: {exception.Message}");
                saveSucceeded = false;
            }

            if (s_runtimeFetcher == null)
            {
                s_runtimeFetcher = GetFetcher();
            }

            bool isComplete = false;
            if (s_runtimeFetcher is IPlayerPrefsRuntimeFetcherWithStatus statusFetcher)
            {
                isComplete = statusFetcher.TryGetAllPlayerPrefs(out prefs);
            }
            else if (s_runtimeFetcher != null)
            {
                // Preserve compatibility with third-party implementations of the original
                // public interface, but never trust an unverified snapshot for DeleteAll.
                prefs = s_runtimeFetcher.GetAllPlayerPrefs();
                Debug.LogWarning("[PlayerPrefsRuntime] Fetcher does not report snapshot completeness.");
            }
            else
            {
                Debug.LogWarning("[PlayerPrefsRuntime] PlayerPrefs fetcher does not exist for the current platform.");
                prefs = null;
            }

            if (prefs == null)
            {
                prefs = new Dictionary<string, object>();
                isComplete = false;
            }

            if (prefs.Remove(string.Empty))
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Fetcher returned an empty PlayerPrefs key. Entry skipped.");
                isComplete = false;
            }

            return isComplete && saveSucceeded;
        }

        /// <summary>
        /// Logs all PlayerPrefs key-value pairs to the Unity console.
        /// Also shows a Canvas with a scrollable list of entries.
        /// </summary>
        public static void LogAllPlayerPrefs()
        {
            Dictionary<string, object> allPrefs = GetAllPlayerPrefs();
            LogPlayerPrefs(allPrefs);
        }

        private static void LogPlayerPrefs(Dictionary<string, object> allPrefs)
        {
            Debug.Log($"[PlayerPrefsRuntime] Found {allPrefs.Count} PlayerPrefs entries:");

            foreach (KeyValuePair<string, object> kvp in allPrefs)
            {
                Debug.Log($"[PlayerPrefsRuntime] Key: {kvp.Key}, Value: {kvp.Value} (Type: {kvp.Value?.GetType().Name ?? "null"})");
            }
        }

        public static void ShowAllPlayerPrefs()
        {
            Dictionary<string, object> allPrefs = GetAllPlayerPrefs();
            LogPlayerPrefs(allPrefs);

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

        /// <summary>
        /// Sets an integer PlayerPrefs value and saves immediately.
        /// </summary>
        /// <param name="key">PlayerPrefs key. Must not be null or empty.</param>
        /// <param name="value">Value to store.</param>
        public static void SetInt(string key, int value)
        {
            PlayerPrefsRuntimeWriter.SetInt(key, value);
        }

        /// <summary>
        /// Sets a float PlayerPrefs value and saves immediately.
        /// </summary>
        /// <param name="key">PlayerPrefs key. Must not be null or empty.</param>
        /// <param name="value">Value to store.</param>
        public static void SetFloat(string key, float value)
        {
            PlayerPrefsRuntimeWriter.SetFloat(key, value);
        }

        /// <summary>
        /// Sets a string PlayerPrefs value and saves immediately. A null value is stored as an empty string.
        /// </summary>
        /// <param name="key">PlayerPrefs key. Must not be null or empty.</param>
        /// <param name="value">Value to store.</param>
        public static void SetString(string key, string value)
        {
            PlayerPrefsRuntimeWriter.SetString(key, value);
        }

        /// <summary>
        /// Deletes a single PlayerPrefs key and saves immediately.
        /// </summary>
        /// <param name="key">PlayerPrefs key. Must not be null or empty.</param>
        public static void DeleteKey(string key)
        {
            PlayerPrefsRuntimeWriter.DeleteKey(key);
        }

        /// <summary>
        /// Deletes ALL PlayerPrefs and saves immediately. On iOS/macOS this clears the application's
        /// entire UserDefaults domain, including engine-managed keys. Deletion is cancelled unless
        /// a complete JSON backup is written to <see cref="Application.persistentDataPath"/>.
        /// </summary>
        public static void DeleteAll()
        {
            TryDeleteAll();
        }

        /// <summary>
        /// Attempts to back up and delete all PlayerPrefs.
        /// </summary>
        /// <returns>
        /// True when a verified complete snapshot was backed up and deleted, or when the
        /// platform fetcher verified that the store was already empty.
        /// </returns>
        public static bool TryDeleteAll()
        {
            return PlayerPrefsRuntimeWriter.DeleteAll();
        }

        /// <summary>
        /// Exports all PlayerPrefs to a versioned JSON string that can be re-imported with
        /// <see cref="ImportFromJson"/>.
        /// </summary>
        /// <returns>JSON document with all exportable entries.</returns>
        public static string ExportToJson()
        {
            return PlayerPrefsRuntimeWriter.ExportToJson();
        }

        /// <summary>
        /// Imports PlayerPrefs entries from a JSON document produced by <see cref="ExportToJson"/>
        /// (a flat <c>{"key": value}</c> object is also accepted). Existing keys are overwritten,
        /// invalid entries are skipped and reported per key. A best-effort JSON backup is attempted
        /// in <see cref="Application.persistentDataPath"/> before any entry is applied.
        /// </summary>
        /// <param name="json">JSON document to import.</param>
        /// <returns>Import summary with the applied entry count and per-key errors.</returns>
        public static PlayerPrefsRuntimeImportResult ImportFromJson(string json)
        {
            return PlayerPrefsRuntimeWriter.ImportFromJson(json);
        }

        /// <summary>
        /// Enables the built-in device gesture that opens the viewer:
        /// hold three fingers on the screen for two seconds. Off by default.
        /// </summary>
        public static void EnableGestureTrigger()
        {
            PlayerPrefsRuntimeGestureTrigger.Enable();
        }

        /// <summary>
        /// Disables the gesture trigger previously enabled via <see cref="EnableGestureTrigger"/>.
        /// </summary>
        public static void DisableGestureTrigger()
        {
            PlayerPrefsRuntimeGestureTrigger.Disable();
        }

        /// <summary>
        /// True while the gesture trigger is active.
        /// </summary>
        public static bool IsGestureTriggerEnabled => PlayerPrefsRuntimeGestureTrigger.IsEnabled;

        /// <summary>
        /// Invokes <see cref="OnEntryChanged"/>, isolating the tool from exceptions in user handlers.
        /// </summary>
        internal static void RaiseEntryChanged(string key)
        {
            Action<string> handlers = OnEntryChanged;
            if (handlers == null)
            {
                return;
            }

            Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                Action<string> handler = (Action<string>)invocationList[i];
                try
                {
                    handler(key);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[PlayerPrefsRuntime] OnEntryChanged handler threw an exception: {exception}");
                }
            }
        }
    }
}
#endif
