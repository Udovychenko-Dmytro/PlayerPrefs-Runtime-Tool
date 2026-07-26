// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Globalization;
using System.Text;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Represents a normalized PlayerPrefs entry containing name, type, and value.
    /// </summary>
    public readonly struct PlayerPrefsRuntimeEntry
    {
        private readonly long m_estimatedValueSizeBytes;

        public string Name { get; }
        public string Type { get; }
        public string Value { get; }

        public PlayerPrefsRuntimeEntry(string name, object rawValue)
        {
            Name = string.IsNullOrEmpty(name) ? "(Unnamed)" : name;

            if (rawValue == null)
            {
                Type = "null";
                Value = "(null)";
                m_estimatedValueSizeBytes = 0L;
            }
            else
            {
                Type = rawValue.GetType().Name;
                if (rawValue is float floatValue)
                {
                    Value = floatValue.ToString("R", CultureInfo.InvariantCulture);
                }
                else if (rawValue is double doubleValue)
                {
                    Value = doubleValue.ToString("R", CultureInfo.InvariantCulture);
                }
                else
                {
                    IFormattable formattable = rawValue as IFormattable;
                    Value = formattable != null
                        ? formattable.ToString(null, CultureInfo.InvariantCulture)
                        : rawValue.ToString();
                }

                switch (rawValue)
                {
                    case int _:
                    case float _:
                        m_estimatedValueSizeBytes = 4L;
                        break;
                    case long _:
                    case double _:
                        m_estimatedValueSizeBytes = 8L;
                        break;
                    case byte[] bytes:
                        m_estimatedValueSizeBytes = bytes.LongLength;
                        break;
                    default:
                        m_estimatedValueSizeBytes = string.IsNullOrEmpty(Value)
                            ? 0L
                            : Encoding.UTF8.GetByteCount(Value);
                        break;
                }
            }
        }

        /// <summary>
        /// Approximate storage size of this entry's value: the native numeric width,
        /// raw length for byte arrays, and UTF-8 byte count for displayed text.
        /// </summary>
        public long EstimateValueSizeBytes()
        {
            return m_estimatedValueSizeBytes;
        }

        /// <summary>
        /// Formats a byte count as a compact human-readable string (B / KB / MB).
        /// </summary>
        public static string FormatByteSize(long bytes)
        {
            if (bytes < 1024L)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} B", bytes);
            }

            if (bytes < 1024L * 1024L)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.#} KB", bytes / 1024f);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} MB", bytes / (1024f * 1024f));
        }
    }
}
#endif
