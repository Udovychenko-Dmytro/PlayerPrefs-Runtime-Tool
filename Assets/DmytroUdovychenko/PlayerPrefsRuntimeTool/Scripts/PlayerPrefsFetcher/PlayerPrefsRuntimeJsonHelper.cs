// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Collections.Generic;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Helper class for normalizing PlayerPrefs values across different platforms.
    /// Handles type conversions and data formatting consistency.
    /// </summary>
    internal static class PlayerPrefsRuntimeJsonHelper
    {
        /// <summary>
        /// Normalizes a dictionary of PlayerPrefs values to ensure consistent types across platforms.
        /// </summary>
        /// <param name="source">Source dictionary with raw PlayerPrefs data</param>
        /// <returns>Normalized dictionary with consistent types</returns>
        internal static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> source)
        {
            if (source == null)
            {
                return new Dictionary<string, object>();
            }

            List<string> keys = new List<string>(source.Keys);
            foreach (string key in keys)
            {
                source[key] = NormalizeValue(source[key]);
            }

            return source;
        }

        /// <summary>
        /// Normalizes a single PlayerPrefs value to ensure consistent types across platforms.
        /// </summary>
        /// <param name="value">Raw PlayerPrefs value</param>
        /// <returns>Normalized value with consistent type</returns>
        private static object NormalizeValue(object value)
        {
            switch (value)
            {
                case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                    return (int)longValue;
                case long longValue:
                    return longValue;
                case double doubleValue:
                    return Convert.ToSingle(doubleValue);
                case Newtonsoft.Json.Linq.JValue jValue:
                    return NormalizeValue(jValue.Value);
                case string stringValue when stringValue.StartsWith("$base64Binary;"):
                    // Handle base64 encoded binary data from JSON
                    try
                    {
                        string base64Data = stringValue.Substring("$base64Binary;".Length);
                        byte[] binary = Convert.FromBase64String(base64Data);
                        return System.Text.Encoding.UTF8.GetString(binary);
                    }
                    catch
                    {
                        return stringValue;
                    }
                case string stringValue:
                    // Check if this might be base64 encoded data
                    if (stringValue.Length > 0 && stringValue.Length % 4 == 0)
                    {
                        try
                        {
                            byte[] binary = Convert.FromBase64String(stringValue);
                            string decodedString = System.Text.Encoding.UTF8.GetString(binary);
                            // If the decoded string contains invalid UTF-8 sequences, return original
                            if (!decodedString.Contains("\uFFFD"))
                            {
                                return decodedString;
                            }
                        }
                        catch
                        {
                            // Not base64, return original
                        }
                    }
                    return stringValue;
                default:
                    return value;
            }
        }
    }
}
#endif
