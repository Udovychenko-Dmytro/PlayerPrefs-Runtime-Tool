// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR 
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public class PlayerPrefsRuntimeFetcherMacOS : IPlayerPrefsRuntimeFetcher
    {
        // Import the native method to get PlayerPrefs as JSON
        [DllImport("__Internal")]
        private static extern System.IntPtr GetPlayerPrefsJSON();

        // Import the native method to free allocated memory
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

                // Convert the native pointer to a C# string
                string jsonPrefs = Marshal.PtrToStringAnsi(jsonPtr);
                Debug.Log($"Received JSON: {jsonPrefs}");

                // Free the allocated memory in the native plugin
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
                Debug.LogError($"Error fetching PlayerPrefs on macOS: {e.Message}");
            }

            return prefs;
        }

        /// <summary>
        /// Deserializes the JSON string into a dictionary.
        /// </summary>
        /// <param name="json">The JSON string representing PlayerPrefs.</param>
        /// <returns>A dictionary with PlayerPrefs keys and values.</returns>
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

                // Handle data type conversions
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