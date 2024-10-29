// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public class PlayerPrefsRuntimeFetcherAndroid : IPlayerPrefsRuntimeFetcher
    {
        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            try
            {
                Debug.Log("Attempting to fetch PlayerPrefs on Android");

                // Get the UnityPlayer class
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        string bundleId = Application.identifier;
                        Debug.Log($"Bundle ID (Package Name): {bundleId}");

                        string prefsName = $"{bundleId}.v2.playerprefs";
                        Debug.Log($"SharedPreferences Name: {prefsName}");

                        using (AndroidJavaObject playerPrefs = currentActivity.Call<AndroidJavaObject>("getSharedPreferences", prefsName, 0))
                        {
                            using (AndroidJavaObject allEntries = playerPrefs.Call<AndroidJavaObject>("getAll"))
                            {
                                using (AndroidJavaObject entrySet = allEntries.Call<AndroidJavaObject>("entrySet"))
                                {
                                    AndroidJavaObject[] entries = entrySet.Call<AndroidJavaObject[]>("toArray");

                                    foreach (AndroidJavaObject entry in entries)
                                    {
                                        using (entry)
                                        {
                                            // Get the key and value object
                                            string key = entry.Call<string>("getKey");
                                            string formatedKey = Uri.UnescapeDataString(key);
                                            AndroidJavaObject valueObject = entry.Call<AndroidJavaObject>("getValue");

                                            // Determine the type of the value
                                            string valueType = valueObject.Call<AndroidJavaObject>("getClass").Call<string>("getSimpleName");

                                            switch (valueType)
                                            {
                                                case "Integer":
                                                    int intValue = playerPrefs.Call<int>("getInt", key, 0);
                                                    prefs[formatedKey] = intValue;
                                                    break;
                                                case "Float":
                                                    float floatValue = playerPrefs.Call<float>("getFloat", key, 0f);
                                                    prefs[formatedKey] = floatValue;
                                                    break;
                                                case "String":
                                                    string stringValue = playerPrefs.Call<string>("getString", key, "");
                                                    prefs[formatedKey] = Uri.UnescapeDataString(stringValue);
                                                    break;
                                                default:
                                                    // For other types, log a warning and convert to string
                                                    Debug.LogWarning($"Unsupported type for key: {key}, type: {valueType}");
                                                    string rawString = valueObject.Call<string>("toString");
                                                    prefs[formatedKey] = Uri.UnescapeDataString(rawString);
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
            catch (System.Exception e)
            {
                Debug.LogError($"Error fetching PlayerPrefs on Android: {e.Message}\nStack Trace: {e.StackTrace}");
            }

            return prefs;
        }
    }
}
#endif
#endif