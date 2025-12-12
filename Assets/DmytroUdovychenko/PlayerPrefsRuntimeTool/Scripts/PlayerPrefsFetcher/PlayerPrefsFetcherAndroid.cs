// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Android implementation for retrieving PlayerPrefs at runtime.
    /// Accesses SharedPreferences using Unity's AndroidJavaObject interface.
    /// </summary>
    public class PlayerPrefsRuntimeFetcherAndroid : IPlayerPrefsRuntimeFetcher
    {
        /// <summary>
        /// Retrieves all PlayerPrefs from Android SharedPreferences.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            try
            {
                Debug.Log("[PlayerPrefsRuntime] Attempting to fetch PlayerPrefs on Android");

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        string bundleId = Application.identifier;
                        if (string.IsNullOrEmpty(bundleId))
                        {
                            Debug.LogError("[PlayerPrefsRuntime] Bundle ID is null or empty");
                            return prefs;
                        }
                        
                        Debug.Log($"[PlayerPrefsRuntime] Bundle ID (Package Name): {bundleId}");

                        string prefsName = $"{bundleId}.v2.playerprefs";
                        Debug.Log($"[PlayerPrefsRuntime] SharedPreferences Name: {prefsName}");

                        using (AndroidJavaObject playerPrefs = currentActivity.Call<AndroidJavaObject>("getSharedPreferences", prefsName, 0))
                        {
                            if (playerPrefs == null)
                            {
                                Debug.LogWarning("[PlayerPrefsRuntime] Failed to access SharedPreferences");
                                return prefs;
                            }

                            using (AndroidJavaObject allEntries = playerPrefs.Call<AndroidJavaObject>("getAll"))
                            {
                                if (allEntries == null)
                                {
                                    Debug.LogWarning("[PlayerPrefsRuntime] Failed to get all SharedPreferences entries");
                                    return prefs;
                                }

                                using (AndroidJavaObject entrySet = allEntries.Call<AndroidJavaObject>("entrySet"))
                                {
                                    if (entrySet == null)
                                    {
                                        Debug.LogWarning("[PlayerPrefsRuntime] Failed to get SharedPreferences entry set");
                                        return prefs;
                                    }

                                    AndroidJavaObject[] entries = entrySet.Call<AndroidJavaObject[]>("toArray");
                                    if (entries == null)
                                    {
                                        Debug.LogWarning("[PlayerPrefsRuntime] Failed to convert entry set to array");
                                        return prefs;
                                    }

                                    foreach (AndroidJavaObject entry in entries)
                                    {
                                        using (entry)
                                        {
                                            if (entry == null) continue;

                                            string key = entry.Call<string>("getKey");
                                            if (string.IsNullOrEmpty(key)) continue;

                                            string formattedKey = Uri.UnescapeDataString(key);
                                            using (AndroidJavaObject valueObject = entry.Call<AndroidJavaObject>("getValue"))
                                            {

                                                if (valueObject == null)
                                                {
                                                    prefs[formattedKey] = null;
                                                    continue;
                                                }

                                                // Determine the type of the value
                                                using (AndroidJavaObject classObject = valueObject.Call<AndroidJavaObject>("getClass"))
                                                {
                                                    string valueType = classObject.Call<string>("getSimpleName");

                                                    switch (valueType)
                                                    {
                                                        case "Integer":
                                                            int intValue = playerPrefs.Call<int>("getInt", key, 0);
                                                            prefs[formattedKey] = intValue;
                                                            break;
                                                        case "Float":
                                                            float floatValue = playerPrefs.Call<float>("getFloat", key, 0f);
                                                            prefs[formattedKey] = floatValue;
                                                            break;
                                                        case "String":
                                                            string stringValue = playerPrefs.Call<string>("getString", key, "");
                                                            prefs[formattedKey] = Uri.UnescapeDataString(stringValue);
                                                            break;
                                                        default:
                                                            // For other types, log a warning and convert to string
                                                            Debug.LogWarning($"[PlayerPrefsRuntime] Unsupported type for key: {key}, type: {valueType}");
                                                            string rawString = valueObject.Call<string>("toString");
                                                            prefs[formattedKey] = Uri.UnescapeDataString(rawString);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsRuntime] Error fetching PlayerPrefs on Android: {e.Message}\nStack Trace: {e.StackTrace}");
            }

            Debug.Log($"[PlayerPrefsRuntime] Successfully retrieved {prefs.Count} PlayerPrefs entries on Android");
            return prefs;
        }
    }
}
#endif
#endif