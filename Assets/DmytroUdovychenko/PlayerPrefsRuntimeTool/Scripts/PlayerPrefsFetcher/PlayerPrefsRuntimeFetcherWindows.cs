// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public class PlayerPrefsRuntimeFetcherWindows :
        IPlayerPrefsRuntimeFetcher,
        IPlayerPrefsRuntimeFetcherWithStatus
    {
        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            TryGetAllPlayerPrefs(out Dictionary<string, object> prefs);
            return prefs;
        }

        bool IPlayerPrefsRuntimeFetcherWithStatus.TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            return TryGetAllPlayerPrefs(out prefs);
        }

        private static bool TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>(StringComparer.Ordinal);
            try
            {
                string companyName = string.IsNullOrWhiteSpace(Application.companyName) ? "UnityDefaultCompany" : Application.companyName;
                string productName = string.IsNullOrWhiteSpace(Application.productName) ? "UnnamedProduct" : Application.productName;
                string registryPath = $@"Software\{companyName}\{productName}";

                Debug.Log($"[PlayerPrefsRuntime] Fetching Windows PlayerPrefs from registry path: {registryPath}");
                return PlayerPrefsRuntimeWindowsRegistryReader.TryReadPlayerPrefs(registryPath, out prefs);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsRuntime] Error fetching PlayerPrefs on Windows: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}
#endif
#endif
