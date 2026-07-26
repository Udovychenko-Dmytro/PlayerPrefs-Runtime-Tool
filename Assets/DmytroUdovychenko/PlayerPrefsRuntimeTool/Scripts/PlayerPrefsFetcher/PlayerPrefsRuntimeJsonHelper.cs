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
using System.Text;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Helper class for normalizing PlayerPrefs values across different platforms.
    /// Handles type conversions and data formatting consistency.
    /// </summary>
    internal static class PlayerPrefsRuntimeJsonHelper
    {
        private static readonly UTF8Encoding s_strictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Normalizes a dictionary of PlayerPrefs values to ensure consistent types across platforms.
        /// </summary>
        /// <param name="source">Source dictionary with raw PlayerPrefs data</param>
        /// <returns>Normalized dictionary with consistent types</returns>
        internal static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> source)
        {
            bool isComplete;
            return NormalizeDictionary(source, out isComplete);
        }

        /// <summary>
        /// Normalizes a dictionary and reports whether every source key was retained.
        /// </summary>
        internal static Dictionary<string, object> NormalizeDictionary(
            Dictionary<string, object> source,
            out bool isComplete)
        {
            if (source == null)
            {
                isComplete = false;
                return new Dictionary<string, object>();
            }

            isComplete = true;
            List<string> keys = new List<string>(source.Keys);
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    source.Remove(key);
                    isComplete = false;
                    continue;
                }

                source[key] = NormalizeValue(source[key], out bool valueComplete);
                isComplete &= valueComplete;
            }

            return source;
        }

        /// <summary>
        /// Normalizes a single PlayerPrefs value to ensure consistent types across platforms.
        /// </summary>
        /// <param name="value">Raw PlayerPrefs value</param>
        /// <returns>Normalized value with consistent type</returns>
        internal static object NormalizeValue(object value)
        {
            bool isComplete;
            return NormalizeValue(value, out isComplete);
        }

        /// <summary>
        /// Normalizes one value and reports whether its representation remained lossless.
        /// </summary>
        internal static object NormalizeValue(object value, out bool isComplete)
        {
            isComplete = true;
            switch (value)
            {
                case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                    return (int)longValue;
                case long longValue:
                    return longValue;
                case double doubleValue:
                    float floatValue = Convert.ToSingle(doubleValue);
                    isComplete = (double)floatValue == doubleValue;
                    return floatValue;
                case Newtonsoft.Json.Linq.JValue jValue:
                    return NormalizeValue(jValue.Value, out isComplete);
                case string stringValue when stringValue.StartsWith("$base64Binary;", StringComparison.Ordinal):
                    // Handle base64 encoded binary data from JSON
                    isComplete = false;
                    try
                    {
                        string base64Data = stringValue.Substring("$base64Binary;".Length);
                        byte[] binary = Convert.FromBase64String(base64Data);
                        return s_strictUtf8.GetString(binary);
                    }
                    catch
                    {
                        return stringValue;
                    }
                case string stringValue:
                    return stringValue;
                default:
                    return value;
            }
        }
    }
}
#endif
