// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// WebGL implementation: Unity persists PlayerPrefs as a binary "UPP" file inside the
    /// Emscripten /idbfs mount (IndexedDB-backed). The virtual file system is readable with
    /// regular System.IO, so no JavaScript plugin is required. The shared retrieval path
    /// flushes PlayerPrefs before this fetcher reads the file.
    /// </summary>
    public sealed class PlayerPrefsRuntimeFetcherWebGL :
        IPlayerPrefsRuntimeFetcher,
        IPlayerPrefsRuntimeFetcherWithStatus
    {
        private const string PlayerPrefsFileName = "PlayerPrefs";

        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            try
            {
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Failed to flush PlayerPrefs before WebGL enumeration: {exception.Message}");
            }

            TryGetAllPlayerPrefs(out Dictionary<string, object> prefs);
            return prefs;
        }

        bool IPlayerPrefsRuntimeFetcherWithStatus.TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            return TryGetAllPlayerPrefs(out prefs);
        }

        private static bool TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>();
            string path = Path.Combine(Application.persistentDataPath, PlayerPrefsFileName);
            try
            {
                byte[] data = File.ReadAllBytes(path);
                return PlayerPrefsRuntimeUppParser.TryParse(data, out prefs);
            }
            catch (FileNotFoundException)
            {
                Debug.Log("[PlayerPrefsRuntime] WebGL PlayerPrefs file not found; the persisted store is empty.");
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                Debug.Log("[PlayerPrefsRuntime] WebGL PlayerPrefs directory not found; the persisted store is empty.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Failed to read WebGL PlayerPrefs: {exception.Message}");
                return false;
            }
        }
    }
}
#endif
#endif
