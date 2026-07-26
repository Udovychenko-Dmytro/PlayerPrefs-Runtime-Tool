// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Platform-agnostic decoder for Unity PlayerPrefs values stored in the Windows registry.
    /// Kept free of Windows-only defines so the pure byte parsing is unit-testable on any platform.
    /// </summary>
    internal static class PlayerPrefsRuntimeRegistryValueDecoder
    {
        internal const uint RegSz = 1;
        internal const uint RegExpandSz = 2;
        internal const uint RegBinary = 3;
        internal const uint RegDword = 4;
        internal const uint RegQword = 11;

        internal const string UnnamedKey = "(Unnamed)";

        private static readonly Regex s_hashSuffixRegex = new Regex(@"_h\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly UTF8Encoding s_strictUtf8 = new UTF8Encoding(false, true);
        private static readonly UnicodeEncoding s_strictUtf16 = new UnicodeEncoding(false, false, true);

        /// <summary>
        /// Strips Unity's hashed suffix (e.g. "_h3282552528") from a registry value name.
        /// </summary>
        internal static string NormalizeKey(string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                return UnnamedKey;
            }

            return s_hashSuffixRegex.Replace(rawKey, string.Empty);
        }

        /// <summary>
        /// Decodes a raw registry value into int, long, float, string or byte[] following Unity's PlayerPrefs storage format.
        /// Returns null when the value cannot be decoded.
        /// </summary>
        internal static object DecodeRegistryValue(uint type, byte[] data, uint dataSize)
        {
            // Guard against a size that exceeds the actual buffer: the extracted decoder is now a
            // general-purpose internal utility (and unit-tested with arbitrary byte[]), so it must
            // not trust dataSize blindly and let BitConverter/Encoding throw on a mismatch.
            if (data == null || dataSize > (uint)data.Length)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Registry value size {dataSize} exceeds buffer length {(data?.Length ?? 0)}. Skipping.");
                return null;
            }

            switch (type)
            {
                case RegSz:
                case RegExpandSz:
                    return DecodeUnicodeString(data, dataSize);
                case RegDword:
                    return dataSize == sizeof(int) ? BitConverter.ToInt32(data, 0) : null;
                case RegQword:
                    return dataSize == sizeof(long) ? BitConverter.ToInt64(data, 0) : null;
                case RegBinary:
                    return DecodeBinaryValue(data, dataSize);
                default:
                    Debug.LogWarning($"[PlayerPrefsRuntime] Unsupported registry value type {type}");
                    return null;
            }
        }

        private static object DecodeUnicodeString(byte[] data, uint dataSize)
        {
            if (data == null)
            {
                return null;
            }

            int length = (int)dataSize;
            bool hasValidTerminator = length >= 2
                && length % 2 == 0
                && data[length - 2] == 0
                && data[length - 1] == 0;
            if (!hasValidTerminator)
            {
                return CopyBytes(data, dataSize);
            }

            try
            {
                return s_strictUtf16.GetString(data, 0, length - 2);
            }
            catch (DecoderFallbackException)
            {
                return CopyBytes(data, dataSize);
            }
        }

        private static object DecodeBinaryValue(byte[] data, uint dataSize)
        {
            if (data == null || dataSize == 0)
            {
                return null;
            }

            if (dataSize == sizeof(float))
            {
                return BitConverter.ToSingle(data, 0);
            }

            int length = (int)dataSize;
            if (data[length - 1] == 0)
            {
                length--;
            }

            try
            {
                string stringValue = s_strictUtf8.GetString(data, 0, length);
                if (!string.IsNullOrEmpty(stringValue))
                {
                    return stringValue;
                }
            }
            catch (DecoderFallbackException)
            {
                // Preserve undecodable bytes as an unsupported value instead of replacing
                // them with U+FFFD and falsely treating a lossy backup as complete.
            }

            return CopyBytes(data, dataSize);
        }

        private static byte[] CopyBytes(byte[] data, uint dataSize)
        {
            byte[] copy = new byte[dataSize];
            Buffer.BlockCopy(data, 0, copy, 0, (int)dataSize);
            return copy;
        }
    }
}
#endif
