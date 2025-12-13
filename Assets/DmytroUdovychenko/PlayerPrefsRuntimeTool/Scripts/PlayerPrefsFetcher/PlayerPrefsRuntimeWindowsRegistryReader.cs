#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Shared Windows registry reader that normalizes Unity PlayerPrefs data.
    /// </summary>
    internal static class PlayerPrefsRuntimeWindowsRegistryReader
    {
        private const uint HKeyCurrentUser = 0x80000001;
        private const uint KeyRead = 0x20019;

        private const uint RegSz = 1;
        private const uint RegExpandSz = 2;
        private const uint RegBinary = 3;
        private const uint RegDword = 4;
        private const uint RegQword = 11;

        private const int ErrorSuccess = 0;
        private const int ErrorMoreData = 234;
        private const int ErrorNoMoreItems = 259;

        private const int InitialValueNameCapacity = 256;
        private const int InitialDataCapacity = 1024;

        private static readonly Regex s_hashSuffixRegex = new Regex(@"_h\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(uint hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegEnumValue(
            IntPtr hKey,
            uint dwIndex,
            StringBuilder lpValueName,
            ref uint lpcchValueName,
            IntPtr lpReserved,
            out uint lpType,
            byte[] lpData,
            ref uint lpcbData);

        internal static Dictionary<string, object> ReadPlayerPrefs(string registryPath)
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>(StringComparer.Ordinal);

            IntPtr hKey;
            int openResult = RegOpenKeyEx(HKeyCurrentUser, registryPath, 0, KeyRead, out hKey);
            
            if (openResult != ErrorSuccess)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Windows registry key not found or inaccessible: {registryPath}");
                return prefs;
            }

            try
            {
                EnumerateValues(hKey, prefs);
            }
            finally
            {
                RegCloseKey(hKey);
            }

            return prefs;
        }

        private static void EnumerateValues(IntPtr hKey, Dictionary<string, object> prefs)
        {
            uint index = 0;
            byte[] dataBuffer = new byte[InitialDataCapacity];
            uint dataBufferSize = (uint)dataBuffer.Length;

            while (true)
            {
                StringBuilder valueName = new StringBuilder(InitialValueNameCapacity);
                uint valueNameSize = (uint)valueName.Capacity;
                uint type;
                uint dataSize;

                int result = ReadValue(hKey, index, valueName, ref valueNameSize, ref dataBuffer, ref dataBufferSize, out type, out dataSize);

                if (result == ErrorNoMoreItems)
                {
                    break;
                }

                if (result != ErrorSuccess)
                {
                    Debug.LogWarning($"[PlayerPrefsRuntime] Failed to enumerate registry value at index {index}. Error: {result}");
                    break;
                }

                string rawKey = valueName.ToString(0, (int)valueNameSize);
                string normalizedKey = NormalizeKey(rawKey);

                object decodedValue = DecodeRegistryValue(type, dataBuffer, dataSize);
                if (decodedValue != null)
                {
                    prefs[normalizedKey] = decodedValue;
                }

                index++;
            }
        }

        private static int ReadValue(
            IntPtr hKey,
            uint index,
            StringBuilder valueName,
            ref uint valueNameSize,
            ref byte[] dataBuffer,
            ref uint dataBufferSize,
            out uint type,
            out uint dataSize)
        {
            while (true)
            {
                uint currentNameSize = valueNameSize;
                uint currentDataSize = dataBufferSize;

                int result = RegEnumValue(
                    hKey,
                    index,
                    valueName,
                    ref currentNameSize,
                    IntPtr.Zero,
                    out type,
                    dataBuffer,
                    ref currentDataSize);

                if (result == ErrorMoreData)
                {
                    if (currentNameSize > valueName.Capacity)
                    {
                        valueName.EnsureCapacity((int)(currentNameSize + 1));
                    }

                    if (currentDataSize > dataBuffer.Length)
                    {
                        dataBufferSize = currentDataSize;
                        Array.Resize(ref dataBuffer, (int)dataBufferSize);
                    }

                    valueNameSize = (uint)valueName.Capacity;
                    continue;
                }

                if (result == ErrorSuccess)
                {
                    valueNameSize = currentNameSize;
                    dataSize = currentDataSize;
                }
                else
                {
                    dataSize = 0;
                }

                return result;
            }
        }

        private static string NormalizeKey(string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                return "(Unnamed)";
            }

            return s_hashSuffixRegex.Replace(rawKey, string.Empty);
        }

        private static object DecodeRegistryValue(uint type, byte[] data, uint dataSize)
        {
            switch (type)
            {
                case RegSz:
                case RegExpandSz:
                    return DecodeUnicodeString(data, dataSize);
                case RegDword:
                    return dataSize >= 4 ? BitConverter.ToInt32(data, 0) : null;
                case RegQword:
                    return dataSize >= 8 ? BitConverter.ToInt64(data, 0) : null;
                case RegBinary:
                    return DecodeBinaryValue(data, dataSize);
                default:
                    Debug.LogWarning($"[PlayerPrefsRuntime] Unsupported registry value type {type}");
                    return null;
            }
        }

        private static object DecodeUnicodeString(byte[] data, uint dataSize)
        {
            if (data == null || dataSize <= 2)
            {
                return string.Empty;
            }

            int bytes = Math.Max(0, (int)dataSize - 2); // Remove terminator.
            string stringValue = Encoding.Unicode.GetString(data, 0, bytes);

            if (float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return floatValue;
            }

            return stringValue;
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

            string stringValue = Encoding.UTF8.GetString(data, 0, length);
            if (!string.IsNullOrEmpty(stringValue))
            {
                return stringValue;
            }

            byte[] copy = new byte[dataSize];
            Buffer.BlockCopy(data, 0, copy, 0, (int)dataSize);
            return copy;
        }
    }
}
#endif
#endif
