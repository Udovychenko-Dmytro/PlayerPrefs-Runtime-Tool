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
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Parser for Unity's binary PlayerPrefs file format ("UPP") used on WebGL,
    /// where prefs are persisted as a file in the Emscripten /idbfs mount.
    /// Layout: "UnityPrf" magic, int32 version, int32 max size, then repeated entries.
    /// Keys and string values use the same encoding: a marker &lt; 0x80 is an inline UTF-8
    /// byte length and 0x80 is followed by an int32 byte length. Value markers 0xFD and 0xFE
    /// represent float and int32 respectively.
    /// Kept platform-agnostic (no WebGL define) so it is unit-testable in the editor.
    /// </summary>
    internal static class PlayerPrefsRuntimeUppParser
    {
        internal const string HeaderMagic = "UnityPrf";
        internal const int ExpectedVersion = 0x10000;

        private const int MagicLength = 8;
        private const int HeaderLength = 16;
        private const byte LongStringMarker = 0x80;
        private const byte FloatMarker = 0xFD;
        private const byte IntMarker = 0xFE;
        private static readonly UTF8Encoding s_strictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Parses a UPP document. Never throws: structural problems are logged and the
        /// entries parsed before the problem are returned (partial success beats total failure).
        /// </summary>
        internal static Dictionary<string, object> Parse(byte[] data)
        {
            TryParse(data, out Dictionary<string, object> prefs);
            return prefs;
        }

        /// <summary>
        /// Parses a UPP document and reports whether the complete document was understood.
        /// Partial entries remain available to non-destructive callers.
        /// </summary>
        internal static bool TryParse(byte[] data, out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>(StringComparer.Ordinal);

            if (data == null || data.Length < HeaderLength)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] PlayerPrefs file is missing or too short to contain a UPP header.");
                return false;
            }

            string magic = Encoding.ASCII.GetString(data, 0, MagicLength);
            if (!string.Equals(magic, HeaderMagic, StringComparison.Ordinal))
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Unexpected PlayerPrefs file magic '{magic}'. Enumeration unavailable.");
                return false;
            }

            int version = BitConverter.ToInt32(data, MagicLength);
            bool isComplete = true;
            if (version != ExpectedVersion)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Unexpected UPP version 0x{version:X}. Trying to parse anyway.");
                isComplete = false;
            }

            int offset = HeaderLength;
            while (offset < data.Length)
            {
                if (!TryReadEncodedString(data, ref offset, out string key))
                {
                    LogTruncated(offset);
                    isComplete = false;
                    break;
                }

                if (offset >= data.Length)
                {
                    LogTruncated(offset);
                    isComplete = false;
                    break;
                }

                byte marker = data[offset];
                offset += 1;

                if (marker < LongStringMarker)
                {
                    offset -= 1;
                    if (!TryReadEncodedString(data, ref offset, out string value))
                    {
                        LogTruncated(offset);
                        isComplete = false;
                        break;
                    }

                    if (!StoreValue(prefs, key, value))
                    {
                        isComplete = false;
                    }
                }
                else if (marker == LongStringMarker)
                {
                    offset -= 1;
                    if (!TryReadEncodedString(data, ref offset, out string value))
                    {
                        LogTruncated(offset);
                        isComplete = false;
                        break;
                    }

                    if (!StoreValue(prefs, key, value))
                    {
                        isComplete = false;
                    }
                }
                else if (marker == FloatMarker)
                {
                    if (!HasRemaining(data, offset, 4))
                    {
                        LogTruncated(offset);
                        isComplete = false;
                        break;
                    }

                    if (!StoreValue(prefs, key, BitConverter.ToSingle(data, offset)))
                    {
                        isComplete = false;
                    }
                    offset += 4;
                }
                else if (marker == IntMarker)
                {
                    if (!HasRemaining(data, offset, 4))
                    {
                        LogTruncated(offset);
                        isComplete = false;
                        break;
                    }

                    if (!StoreValue(prefs, key, BitConverter.ToInt32(data, offset)))
                    {
                        isComplete = false;
                    }
                    offset += 4;
                }
                else
                {
                    Debug.LogWarning($"[PlayerPrefsRuntime] Unknown UPP type marker 0x{marker:X2} at offset {offset - 1}. Returning entries parsed so far.");
                    isComplete = false;
                    break;
                }
            }

            return isComplete;
        }

        private static bool StoreValue(Dictionary<string, object> prefs, string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[PlayerPrefsRuntime] PlayerPrefs file contains an empty key. Entry skipped.");
                return false;
            }

            bool isUnique = !prefs.ContainsKey(key);
            if (!isUnique)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] PlayerPrefs file contains duplicate key '{key}'. Last value retained.");
            }

            prefs[key] = value;
            return isUnique;
        }

        private static bool TryReadEncodedString(byte[] data, ref int offset, out string value)
        {
            value = null;
            if (!HasRemaining(data, offset, 1))
            {
                return false;
            }

            byte marker = data[offset];
            offset += 1;

            int length;
            if (marker < LongStringMarker)
            {
                length = marker;
            }
            else if (marker == LongStringMarker)
            {
                if (!HasRemaining(data, offset, sizeof(int)))
                {
                    return false;
                }

                length = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);
            }
            else
            {
                return false;
            }

            if (!HasRemaining(data, offset, length))
            {
                return false;
            }

            try
            {
                value = s_strictUtf8.GetString(data, offset, length);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            offset += length;
            return true;
        }

        /// <summary>
        /// True when <paramref name="count"/> bytes are available from <paramref name="offset"/>.
        /// Uses subtraction (data.Length - offset) to stay safe against int overflow of offset + count.
        /// </summary>
        private static bool HasRemaining(byte[] data, int offset, int count)
        {
            return count >= 0 && offset >= 0 && offset <= data.Length && count <= data.Length - offset;
        }

        private static void LogTruncated(int offset)
        {
            Debug.LogWarning($"[PlayerPrefsRuntime] PlayerPrefs file is truncated near offset {offset}. Returning entries parsed so far.");
        }
    }
}
#endif
