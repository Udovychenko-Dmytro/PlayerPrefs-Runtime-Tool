#if PLAYER_PREFS_RUNTIME_TOOL
#if UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using UnityDebug = UnityEngine.Debug;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// macOS editor implementation for retrieving PlayerPrefs at runtime.
    /// Parses plist files directly from the file system in the Unity editor.
    /// </summary>
    public class PlayerPrefsRuntimeFetcherMacOSEditor :
        IPlayerPrefsRuntimeFetcher,
        IPlayerPrefsRuntimeFetcherWithStatus
    {
        /// <summary>
        /// Retrieves all PlayerPrefs by parsing macOS plist files.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            TryGetAllPlayerPrefs(out Dictionary<string, object> prefs);
            return prefs;
        }

        bool IPlayerPrefsRuntimeFetcherWithStatus.TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            return TryGetAllPlayerPrefs(out prefs);
        }

        private static bool TryGetAllPlayerPrefs(out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>();
            try
            {
                string companyName = string.IsNullOrEmpty(Application.companyName) ? "UnityDefaultCompany" : Application.companyName;
                string productName = string.IsNullOrEmpty(Application.productName) ? "UnnamedProduct" : Application.productName;

                if (!TryResolvePlayerPrefsPath(companyName, productName, out string plistPath, out bool fileExists))
                {
                    return false;
                }

                UnityDebug.Log($"[PlayerPrefsRuntime] Looking for PlayerPrefs at path: {plistPath}");
                
                if (!fileExists)
                {
                    UnityDebug.Log($"[PlayerPrefsRuntime] PlayerPrefs plist not found; the store is empty: {plistPath}");
                    return true;
                }

                if (IsBinaryPlist(plistPath))
                {
                    UnityDebug.Log("[PlayerPrefsRuntime] Detected binary plist format");
                    string json = ConvertBinaryPlistToJson(plistPath);
                    
                    if (!string.IsNullOrEmpty(json))
                    {
                        return TryDeserializeJsonPrefs(json, out prefs);
                    }

                    return false;
                }

                UnityDebug.Log("[PlayerPrefsRuntime] Detected XML plist format");
                return TryParseXmlPlist(plistPath, out prefs);
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Error fetching PlayerPrefs on macOS editor: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Resolves the path to the PlayerPrefs plist file.
        /// </summary>
        /// <param name="companyName">Unity company name</param>
        /// <param name="productName">Unity product name</param>
        private static bool TryResolvePlayerPrefsPath(
            string companyName,
            string productName,
            out string path,
            out bool fileExists)
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string preferencesDirectory = Path.Combine(homeDirectory, "Library/Preferences");
            string primaryFileName = $"unity.{companyName}.{productName}.plist";
            string primaryPath = Path.Combine(preferencesDirectory, primaryFileName);

            if (!TryGetFileExists(primaryPath, out bool primaryExists))
            {
                path = primaryPath;
                fileExists = false;
                return false;
            }

            if (primaryExists)
            {
                path = primaryPath;
                fileExists = true;
                return true;
            }

            string legacyFileName = $"unity.{companyName}.{productName}.playerprefs";
            string legacyPath = Path.Combine(preferencesDirectory, legacyFileName);
            if (!TryGetFileExists(legacyPath, out bool legacyExists))
            {
                path = legacyPath;
                fileExists = false;
                return false;
            }

            path = legacyPath;
            fileExists = legacyExists;
            return true;
        }

        private static bool TryGetFileExists(string path, out bool exists)
        {
            try
            {
                File.GetAttributes(path);
                exists = true;
                return true;
            }
            catch (FileNotFoundException)
            {
                exists = false;
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                exists = false;
                return true;
            }
            catch (Exception exception)
            {
                UnityDebug.LogWarning($"[PlayerPrefsRuntime] Failed to inspect plist path '{path}': {exception.Message}");
                exists = false;
                return false;
            }
        }

        /// <summary>
        /// Determines if a plist file is in binary format.
        /// </summary>
        /// <param name="path">Path to the plist file</param>
        /// <returns>True if binary format, false if XML</returns>
        private static bool IsBinaryPlist(string path)
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    byte[] header = new byte[6];
                    int bytesRead = stream.Read(header, 0, header.Length);
                    return bytesRead == header.Length && Encoding.ASCII.GetString(header).StartsWith("bplist", StringComparison.Ordinal);
                }
            }
            catch (Exception e)
            {
                UnityDebug.LogWarning($"[PlayerPrefsRuntime] Failed to determine plist format: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts a binary plist to JSON format using the plutil command.
        /// </summary>
        /// <param name="path">Path to the binary plist file</param>
        /// <returns>JSON string representation of the plist data</returns>
        private static string ConvertBinaryPlistToJson(string path)
        {
            try
            {
                if (!File.Exists("/usr/bin/plutil"))
                {
                    UnityDebug.LogWarning("[PlayerPrefsRuntime] plutil tool is not available. Unable to convert binary plist.");
                    return string.Empty;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/plutil",
                    Arguments = $"-convert json -o - \"{path}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        UnityDebug.LogError("[PlayerPrefsRuntime] Failed to start plutil process.");
                        return string.Empty;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        UnityDebug.LogError($"[PlayerPrefsRuntime] plutil failed with exit code {process.ExitCode}. Error: {error}");
                        return string.Empty;
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityDebug.LogWarning($"[PlayerPrefsRuntime] plutil reported: {error}");
                    }

                    return output;
                }
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Failed to convert plist to JSON: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses an XML plist file.
        /// </summary>
        /// <param name="path">Path to the XML plist file</param>
        /// <returns>Dictionary containing the plist data</returns>
        internal static bool TryParseXmlPlist(
            string path,
            out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>();
            try
            {
                XmlDocument document = new XmlDocument();
                document.XmlResolver = null;
                
                // Load with proper encoding to handle international characters
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        document.Load(reader);
                    }
                }

                XmlNode dictionaryNode = document.SelectSingleNode("plist/dict");
                if (dictionaryNode == null)
                {
                    UnityDebug.LogWarning("[PlayerPrefsRuntime] plist file does not contain a root dictionary.");
                    return false;
                }

                return TryParseDictionaryNode(dictionaryNode, out prefs);
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Failed to parse XML plist: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parses a dictionary node in an XML plist.
        /// </summary>
        /// <param name="dictNode">The dictionary XML node</param>
        /// <returns>Dictionary containing the parsed data</returns>
        private static bool TryParseDictionaryNode(
            XmlNode dictNode,
            out Dictionary<string, object> result)
        {
            result = new Dictionary<string, object>();
            bool isComplete = true;

            for (int i = 0; i < dictNode.ChildNodes.Count; i++)
            {
                XmlNode keyNode = dictNode.ChildNodes[i];
                if (!string.Equals(keyNode.Name, "key", StringComparison.OrdinalIgnoreCase))
                {
                    if (keyNode.NodeType == XmlNodeType.Element)
                    {
                        isComplete = false;
                    }
                    continue;
                }

                string key = keyNode.InnerText;
                if (string.IsNullOrEmpty(key))
                {
                    isComplete = false;
                    continue;
                }

                XmlNode valueNode = ++i < dictNode.ChildNodes.Count ? dictNode.ChildNodes[i] : null;
                if (valueNode == null)
                {
                    isComplete = false;
                    continue;
                }

                object value = ParseValueNode(valueNode, out bool valueComplete);
                if (result.ContainsKey(key))
                {
                    isComplete = false;
                }

                result[key] = value;
                isComplete &= valueComplete;
            }

            return isComplete;
        }

        /// <summary>
        /// Parses a value node in an XML plist.
        /// </summary>
        /// <param name="valueNode">The value XML node</param>
        /// <returns>Parsed value</returns>
        private static object ParseValueNode(XmlNode valueNode, out bool isComplete)
        {
            isComplete = true;
            switch (valueNode.Name.ToLowerInvariant())
            {
                case "string":
                    // Ensure proper UTF-8 handling for international characters
                    return valueNode.InnerText ?? string.Empty;
                case "integer":
                    if (long.TryParse(valueNode.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
                    {
                        if (longValue >= int.MinValue && longValue <= int.MaxValue)
                        {
                            return (int)longValue;
                        }

                        return longValue;
                    }
                    isComplete = false;
                    return 0;
                case "real":
                    string realText = (valueNode.InnerText ?? string.Empty).Trim();
                    if (string.Equals(realText, "nan", StringComparison.OrdinalIgnoreCase))
                    {
                        return float.NaN;
                    }
                    if (string.Equals(realText, "+infinity", StringComparison.OrdinalIgnoreCase))
                    {
                        return float.PositiveInfinity;
                    }
                    if (string.Equals(realText, "-infinity", StringComparison.OrdinalIgnoreCase))
                    {
                        return float.NegativeInfinity;
                    }
                    if (double.TryParse(realText, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        float floatValue = Convert.ToSingle(doubleValue);
                        isComplete = (double)floatValue == doubleValue;
                        return floatValue;
                    }
                    isComplete = false;
                    return 0f;
                case "true":
                    return true;
                case "false":
                    return false;
                case "data":
                    // Preserve the best-effort display value, but never treat a binary plist
                    // value as a lossless PlayerPrefs string for destructive backup purposes.
                    isComplete = false;
                    try
                    {
                        byte[] binary = Convert.FromBase64String(valueNode.InnerText);
                        // Use UTF-8 encoding to properly handle international characters
                        string decodedString = Encoding.UTF8.GetString(binary);
                        // If the decoded string contains invalid UTF-8 sequences, fall back to the original
                        if (decodedString.Contains("\uFFFD"))
                        {
                            return valueNode.InnerText;
                        }
                        return decodedString;
                    }
                    catch
                    {
                        return valueNode.InnerText;
                    }
                case "dict":
                    bool dictionaryComplete = TryParseDictionaryNode(valueNode, out Dictionary<string, object> dictionary);
                    isComplete = dictionaryComplete;
                    return dictionary;
                case "array":
                    List<object> list = new List<object>();
                    foreach (XmlNode child in valueNode.ChildNodes)
                    {
                        object childValue = ParseValueNode(child, out bool childComplete);
                        list.Add(childValue);
                        isComplete &= childComplete;
                    }
                    return list;
                default:
                    isComplete = false;
                    return valueNode.InnerText;
            }
        }

        /// <summary>
        /// Deserializes JSON string to dictionary and normalizes values.
        /// </summary>
        /// <param name="json">JSON string containing PlayerPrefs data</param>
        /// <returns>Dictionary with normalized PlayerPrefs data</returns>
        private static bool TryDeserializeJsonPrefs(
            string json,
            out Dictionary<string, object> prefs)
        {
            prefs = new Dictionary<string, object>();
            try
            {
                // Ensure proper UTF-8 handling
                Dictionary<string, object> rawPrefs = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
                });
                prefs = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(rawPrefs, out bool isComplete);
                return isComplete;
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Failed to deserialize PlayerPrefs JSON: {e.Message}\nJSON: {json}");
                return false;
            }
        }
    }
}
#endif
#endif
