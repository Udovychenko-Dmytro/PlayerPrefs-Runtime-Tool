#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Windows editor implementation for retrieving PlayerPrefs directly from the registry.
    /// Mirrors the standalone Windows reader but targets the editor-specific registry hive.
    /// </summary>
    public sealed class PlayerPrefsRuntimeFetcherWindowsEditor :
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
                string registryPath = $@"Software\Unity\UnityEditor\{companyName}\{productName}";

                Debug.Log($"[PlayerPrefsRuntime] Fetching Windows editor PlayerPrefs from registry path: {registryPath}");
                bool isComplete = PlayerPrefsRuntimeWindowsRegistryReader.TryReadPlayerPrefs(registryPath, out prefs);
                Debug.Log($"[PlayerPrefsRuntime] Retrieved {prefs.Count} PlayerPrefs entries from Windows editor registry.");
                return isComplete;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsRuntime] Error while reading Windows editor PlayerPrefs: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}
#endif
#endif
