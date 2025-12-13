// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// iOS implementation for retrieving PlayerPrefs at runtime.
    /// Uses native iOS APIs via P/Invoke to access UserDefaults.
    /// </summary>
    public class PlayerPrefsRuntimeFetcherIOS : IPlayerPrefsRuntimeFetcher
    {
        [DllImport("__Internal")]
        private static extern System.IntPtr GetPlayerPrefsJSON();

        [DllImport("__Internal")]
        private static extern void FreeMemory(System.IntPtr ptr);

        /// <summary>
        /// Retrieves all PlayerPrefs from iOS UserDefaults.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            try
            {
                Debug.Log("[PlayerPrefsRuntime] Calling native method GetPlayerPrefsJSON()");
                System.IntPtr jsonPtr = GetPlayerPrefsJSON();

                if (jsonPtr == System.IntPtr.Zero)
                {
                    Debug.LogError("[PlayerPrefsRuntime] Received null pointer for JSON from iOS native code.");
                    return prefs;
                }

                string jsonPrefs = Marshal.PtrToStringAnsi(jsonPtr);
                Debug.Log($"[PlayerPrefsRuntime] Received JSON from iOS native code: {jsonPrefs?.Substring(0, System.Math.Min(jsonPrefs.Length, 200))}...");

                FreeMemory(jsonPtr);

                if (!string.IsNullOrEmpty(jsonPrefs))
                {
                    prefs = DeserializeJSON(jsonPrefs);
                }
                else
                {
                    Debug.LogWarning("[PlayerPrefsRuntime] Received empty JSON from iOS PlayerPrefs.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerPrefsRuntime] Error fetching PlayerPrefs on iOS: {e.Message}\n{e.StackTrace}");
            }

            Debug.Log($"[PlayerPrefsRuntime] Successfully retrieved {prefs.Count} PlayerPrefs entries on iOS");
            return prefs;
        }

        /// <summary>
        /// Deserializes JSON string to dictionary and normalizes values.
        /// </summary>
        /// <param name="json">JSON string containing PlayerPrefs data</param>
        /// <returns>Dictionary with normalized PlayerPrefs data</returns>
        private Dictionary<string, object> DeserializeJSON(string json)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[PlayerPrefsRuntime] Received empty JSON for deserialization on iOS.");
                return dict;
            }

            try
            {
                dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                dict = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(dict);
                Debug.Log("[PlayerPrefsRuntime] JSON deserialization successful on iOS.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerPrefsRuntime] Error deserializing JSON on iOS: {e.Message}\nJSON: {json}");
            }

            return dict;
        }
    }
}
#endif
#endif
