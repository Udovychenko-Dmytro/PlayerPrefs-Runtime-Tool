// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public class PlayerPrefsRuntimeFetcherWindows : IPlayerPrefsRuntimeFetcher
    {
        // Constants for registry access
        private const uint HKEY_CURRENT_USER = 0x80000001;
        private const uint KEY_READ = 0x20019;

        // Registry value types
        private const uint REG_SZ = 1;
        private const uint REG_EXPAND_SZ = 2;
        private const uint REG_BINARY = 3;
        private const uint REG_DWORD = 4;
        private const uint REG_QWORD = 11;

        // Import necessary functions from advapi32.dll
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

        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            try
            {
                Debug.Log("Attempting to fetch PlayerPrefs on Windows using P/Invoke");

                string companyName = Application.companyName;
                string productName = Application.productName;
                string registryPath = $@"Software\{companyName}\{productName}";

                Debug.Log($"Registry Path: {registryPath}");

                IntPtr hKey;
                int result = RegOpenKeyEx(HKEY_CURRENT_USER, registryPath, 0, KEY_READ, out hKey);

                if (result != 0)
                {
                    Debug.LogWarning("Registry key not found or cannot be opened.");
                    return prefs;
                }

                uint index = 0;

                while (true)
                {
                    uint valueNameCapacity = 16383; // Maximum value name length
                    uint dataCapacity = 1024; // Adjust as needed

                    StringBuilder valueName = new StringBuilder((int) valueNameCapacity);
                    byte[] data = new byte[dataCapacity];

                    uint valueNameSize = valueNameCapacity;
                    uint dataSize = dataCapacity;
                    uint type;

                    int enumResult = RegEnumValue(hKey, index, valueName, ref valueNameSize, IntPtr.Zero, out type, data, ref dataSize);

                    if (enumResult != 0)
                        break; // No more values

                    string fullKey = valueName.ToString(0, (int) valueNameSize);

                    // Remove hash suffix from key name
                    string key = Regex.Replace(fullKey, @"_h\d+$", "");

                    object value = null;

                    switch (type)
                    {
                        case REG_SZ:
                        case REG_EXPAND_SZ:
                        {
                            int stringLength = (int) dataSize - 2; // Subtract 2 bytes for the null terminator (Unicode)
                            if (stringLength > 0)
                            {
                                value = Encoding.Unicode.GetString(data, 0, stringLength);
  
                                if (float.TryParse(value.ToString(), out float floatValue))
                                {
                                    value = floatValue;
                                }
                            }
                            else
                            {
                                value = string.Empty;
                            }

                            break;
                        }
                        case REG_DWORD:
                        {
                            if (dataSize >= 4)
                            {
                                value = BitConverter.ToInt32(data, 0);
                            }

                            break;
                        }
                        case REG_QWORD:
                        {
                            if (dataSize >= 8)
                            {
                                value = BitConverter.ToInt64(data, 0);
                            }

                            break;
                        }
                        case REG_BINARY:
                        {
                            if (dataSize > 0)
                            {
                                string stringValue = Encoding.UTF8.GetString(data, 0, (int) dataSize);
                                value = stringValue;
                            }
                            else
                            {
                                value = null;
                            }

                            break;
                        }
                        default:
                        {
                            Debug.LogWarning($"Unsupported registry value type {type} for key: {key}");
                            break;
                        }
                    }

                    if (value != null)
                    {
                        prefs[key] = value;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to read value for key: {key}");
                    }

                    index++;
                }

                RegCloseKey(hKey);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching PlayerPrefs on Windows: {e.Message}\nStack Trace: {e.StackTrace}");
            }

            return prefs;
        }
    }
}
#endif
#endif