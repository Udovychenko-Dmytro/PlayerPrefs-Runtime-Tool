// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public class PlayerPrefsRuntimeFetcheriOS : IPlayerPrefsRuntimeFetcher
    {
        // Import native methods
        [DllImport("__Internal")]
        private static extern System.IntPtr GetPlayerPrefsJSON();

        [DllImport("__Internal")]
        private static extern void FreeMemory(System.IntPtr ptr);

        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            try
            {
                Debug.Log("Calling native method GetPlayerPrefsJSON()");
                System.IntPtr jsonPtr = GetPlayerPrefsJSON();

                if (jsonPtr == System.IntPtr.Zero)
                {
                    Debug.LogError("Received null pointer for JSON.");
                    return prefs;
                }

                string jsonPrefs = Marshal.PtrToStringAnsi(jsonPtr);
                Debug.Log($"Received JSON: {jsonPrefs}");

                // Free the allocated memory in the native code
                FreeMemory(jsonPtr);

                if (!string.IsNullOrEmpty(jsonPrefs))
                {
                    prefs = DeserializeJSON(jsonPrefs);
                }
                else
                {
                    Debug.LogWarning("Received empty JSON from PlayerPrefs.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error fetching PlayerPrefs on iOS: {e.Message}");
            }

            return prefs;
        }

        private Dictionary<string, object> DeserializeJSON(string json)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Received empty JSON for deserialization.");
                return dict;
            }

            try
            {
                dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                Debug.Log("JSON deserialization successful.");

                // Additional data type handling
                var keys = new List<string>(dict.Keys);
                foreach (var key in keys)
                {
                    object value = dict[key];
                    if (value is long)
                    {
                        dict[key] = (int) (long) value;
                    }
                    else if (value is double)
                    {
                        dict[key] = (float) (double) value;
                    }
                    // bool and string are already correct
                    else
                    {
                        dict[key] = value.ToString();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deserializing JSON: {e.Message}\nJSON: {json}");
            }

            return dict;
        }
    }
}
#endif
#endif