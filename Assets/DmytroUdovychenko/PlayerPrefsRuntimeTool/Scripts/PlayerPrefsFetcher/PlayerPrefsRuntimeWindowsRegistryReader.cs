#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Shared Windows registry reader that normalizes Unity PlayerPrefs data.
    /// Value decoding is delegated to <see cref="PlayerPrefsRuntimeRegistryValueDecoder"/>.
    /// </summary>
    internal static class PlayerPrefsRuntimeWindowsRegistryReader
    {
        private const uint HKeyCurrentUser = 0x80000001;
        private const uint KeyRead = 0x20019;

        private const int ErrorSuccess = 0;
        private const int ErrorFileNotFound = 2;
        private const int ErrorPathNotFound = 3;
        private const int ErrorMoreData = 234;
        private const int ErrorNoMoreItems = 259;

        private const int InitialValueNameCapacity = 256;
        private const int InitialDataCapacity = 1024;

        // Windows caps registry value names at 16,383 characters and PlayerPrefs values are far
        // smaller than 16 MB. Needing more than this means the retry loop is not converging, so
        // the entry is skipped instead of being retried forever.
        private const int MaxValueNameCapacity = 16384;
        private const int MaxDataCapacity = 16 * 1024 * 1024;

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
            TryReadPlayerPrefs(registryPath, out Dictionary<string, object> prefs);
            return prefs;
        }

        internal static bool TryReadPlayerPrefs(
            string registryPath,
            out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>(StringComparer.Ordinal);
            IntPtr hKey;
            int openResult = RegOpenKeyEx(HKeyCurrentUser, registryPath, 0, KeyRead, out hKey);

            if (openResult != ErrorSuccess)
            {
                if (openResult == ErrorFileNotFound || openResult == ErrorPathNotFound)
                {
                    Debug.Log($"[PlayerPrefsRuntime] Windows PlayerPrefs registry key does not exist; the store is empty: {registryPath}");
                    return true;
                }

                Debug.LogWarning($"[PlayerPrefsRuntime] Windows registry key is inaccessible: {registryPath}. Error: {openResult}");
                return false;
            }

            try
            {
                return EnumerateValues(hKey, prefs);
            }
            finally
            {
                RegCloseKey(hKey);
            }
        }

        private static bool EnumerateValues(IntPtr hKey, Dictionary<string, object> prefs)
        {
            uint index = 0;
            bool isComplete = true;
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
                    return isComplete;
                }

                if (result == ErrorMoreData)
                {
                    // ReadValue already logged why it gave up on this value. Skip it rather than
                    // abandoning the remaining entries, but never report the snapshot as complete.
                    isComplete = false;
                    index++;
                    continue;
                }

                if (result != ErrorSuccess)
                {
                    Debug.LogWarning($"[PlayerPrefsRuntime] Failed to enumerate registry value at index {index}. Error: {result}");
                    return false;
                }

                string rawKey = valueName.ToString(0, (int)valueNameSize);
                if (string.IsNullOrEmpty(rawKey))
                {
                    // PlayerPrefs.DeleteAll may still remove an assigned default value.
                    // Keep ordinary reads focused on named PlayerPrefs, but do not claim
                    // that a destructive backup covers the complete registry key.
                    isComplete = false;
                    index++;
                    continue;
                }

                string normalizedKey = PlayerPrefsRuntimeRegistryValueDecoder.NormalizeKey(rawKey);
                if (string.IsNullOrEmpty(normalizedKey))
                {
                    Debug.LogWarning($"[PlayerPrefsRuntime] Registry value at index {index} normalized to an empty key.");
                    isComplete = false;
                    index++;
                    continue;
                }

                object decodedValue = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(type, dataBuffer, dataSize);
                if (decodedValue != null)
                {
                    if (prefs.ContainsKey(normalizedKey))
                    {
                        prefs[normalizedKey] = decodedValue;
                        Debug.LogWarning($"[PlayerPrefsRuntime] Multiple registry values normalize to key '{normalizedKey}'. Last value retained.");
                        isComplete = false;
                    }
                    else
                    {
                        prefs[normalizedKey] = decodedValue;
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayerPrefsRuntime] Registry value '{rawKey}' could not be decoded losslessly.");
                    isComplete = false;
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
                    // RegEnumValue reports the required size only for the data buffer; when the
                    // *name* buffer is the short one it leaves lpcchValueName untouched (the docs
                    // point at RegQueryInfoKey for that). Growing purely on the reported sizes
                    // therefore spins forever on a long value name, so every retry has to enlarge
                    // at least one buffer or bail out.
                    if (currentDataSize > MaxDataCapacity)
                    {
                        Debug.LogWarning($"[PlayerPrefsRuntime] Registry value at index {index} needs {currentDataSize} data bytes, above the {MaxDataCapacity} byte limit. Entry skipped.");
                        dataSize = 0;
                        return ErrorMoreData;
                    }

                    bool grew = false;

                    if (currentDataSize > dataBuffer.Length)
                    {
                        dataBufferSize = currentDataSize;
                        Array.Resize(ref dataBuffer, (int)dataBufferSize);
                        grew = true;
                    }
                    else if (valueName.Capacity < MaxValueNameCapacity)
                    {
                        // The data buffer was big enough, so the name buffer is what fell short.
                        // Double it (honouring a reported size if the API did supply one).
                        int nextCapacity = Math.Min(
                            MaxValueNameCapacity,
                            Math.Max((int)currentNameSize + 1, valueName.Capacity * 2));

                        if (nextCapacity > valueName.Capacity)
                        {
                            valueName.EnsureCapacity(nextCapacity);
                            grew = true;
                        }
                    }

                    if (!grew)
                    {
                        Debug.LogWarning($"[PlayerPrefsRuntime] Registry value at index {index} exceeds the supported name length ({MaxValueNameCapacity} characters). Entry skipped.");
                        dataSize = 0;
                        return ErrorMoreData;
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
    }
}
#endif
#endif
