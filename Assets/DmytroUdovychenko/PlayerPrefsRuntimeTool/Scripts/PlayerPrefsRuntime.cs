// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using UnityEngine;
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public static class PlayerPrefsRuntime
    {
        private static IPlayerPrefsRuntimeFetcher _sRuntimeFetcher;

        private static IPlayerPrefsRuntimeFetcher GetFetcher()
        {
            Debug.Log("InitializeFetcher");

#if UNITY_ANDROID && !UNITY_EDITOR
            return new PlayerPrefsRuntimeFetcherAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
            return new PlayerPrefsRuntimeFetcheriOS();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            return new PlayerPrefsRuntimeFetcherWindows();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            return new PlayerPrefsRuntimeFetcherMacOS();
#else
            Debug.LogWarning("PlayerPrefsFetcher is not support for current platform.");
            return null;
#endif
        }

        /// <summary>
        /// Retrieves all PlayerPrefs as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        public static Dictionary<string, object> GetAllPlayerPrefs()
        {
            if (_sRuntimeFetcher == null)
            {
                _sRuntimeFetcher = GetFetcher();
            }

            if (_sRuntimeFetcher != null)
            {
                return _sRuntimeFetcher.GetAllPlayerPrefs();
            }
            else
            {
                Debug.LogWarning("PlayerPrefsFetcher is not exists for current platform.");
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Show all player prefs data in Debug.Log.
        /// </summary>
        public static void LogAllPlayerPrefs()
        {
            Dictionary<string, object> allPrefs = GetAllPlayerPrefs();
            foreach (var kvp in allPrefs)
            {
                Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
            }
        }

        public static void AddTestPlayerPrefs()
        {
            PlayerPrefs.SetString("PLAYER PREFS RUNTIME", "HELLO w@rлД?!");
            PlayerPrefs.Save();
        }
    }
}
#endif