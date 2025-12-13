#if PLAYER_PREFS_RUNTIME_TOOL
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
    public class PlayerPrefsRuntimeFetcherMacOSEditor : IPlayerPrefsRuntimeFetcher
    {
        /// <summary>
        /// Retrieves all PlayerPrefs by parsing macOS plist files.
        /// </summary>
        /// <returns>A dictionary containing all PlayerPrefs keys and values.</returns>
        public Dictionary<string, object> GetAllPlayerPrefs()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>();

            try
            {
                string companyName = string.IsNullOrEmpty(Application.companyName) ? "UnityDefaultCompany" : Application.companyName;
                string productName = string.IsNullOrEmpty(Application.productName) ? "UnnamedProduct" : Application.productName;

                string plistPath = ResolvePlayerPrefsPath(companyName, productName);
                UnityDebug.Log($"[PlayerPrefsRuntime] Looking for PlayerPrefs at path: {plistPath}");
                
                if (!File.Exists(plistPath))
                {
                    UnityDebug.LogWarning($"[PlayerPrefsRuntime] PlayerPrefs plist not found at path: {plistPath}");
                    return prefs;
                }

                if (IsBinaryPlist(plistPath))
                {
                    UnityDebug.Log("[PlayerPrefsRuntime] Detected binary plist format");
                    string json = ConvertBinaryPlistToJson(plistPath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        prefs = DeserializeJsonPrefs(json);
                    }
                }
                else
                {
                    UnityDebug.Log("[PlayerPrefsRuntime] Detected XML plist format");
                    prefs = ParseXmlPlist(plistPath);
                }
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Error fetching PlayerPrefs on macOS editor: {e.Message}\n{e.StackTrace}");
            }

            UnityDebug.Log($"[PlayerPrefsRuntime] Successfully retrieved {prefs.Count} PlayerPrefs entries on macOS editor");
            return prefs ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Resolves the path to the PlayerPrefs plist file.
        /// </summary>
        /// <param name="companyName">Unity company name</param>
        /// <param name="productName">Unity product name</param>
        /// <returns>Path to the plist file</returns>
        private static string ResolvePlayerPrefsPath(string companyName, string productName)
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string preferencesDirectory = Path.Combine(homeDirectory, "Library/Preferences");
            string primaryFileName = $"unity.{companyName}.{productName}.plist";
            string primaryPath = Path.Combine(preferencesDirectory, primaryFileName);

            if (File.Exists(primaryPath))
            {
                return primaryPath;
            }

            string legacyFileName = $"unity.{companyName}.{productName}.playerprefs";
            string legacyPath = Path.Combine(preferencesDirectory, legacyFileName);

            return legacyPath;
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
        private static Dictionary<string, object> ParseXmlPlist(string path)
        {
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
                    return new Dictionary<string, object>();
                }

                return ParseDictionaryNode(dictionaryNode);
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Failed to parse XML plist: {e.Message}");
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Parses a dictionary node in an XML plist.
        /// </summary>
        /// <param name="dictNode">The dictionary XML node</param>
        /// <returns>Dictionary containing the parsed data</returns>
        private static Dictionary<string, object> ParseDictionaryNode(XmlNode dictNode)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < dictNode.ChildNodes.Count; i++)
            {
                XmlNode keyNode = dictNode.ChildNodes[i];
                if (!string.Equals(keyNode.Name, "key", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string key = keyNode.InnerText;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                XmlNode valueNode = ++i < dictNode.ChildNodes.Count ? dictNode.ChildNodes[i] : null;
                if (valueNode == null)
                {
                    continue;
                }

                result[key] = ParseValueNode(valueNode);
            }

            return result;
        }

        /// <summary>
        /// Parses a value node in an XML plist.
        /// </summary>
        /// <param name="valueNode">The value XML node</param>
        /// <returns>Parsed value</returns>
        private static object ParseValueNode(XmlNode valueNode)
        {
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
                    return 0;
                case "real":
                    if (double.TryParse(valueNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        return Convert.ToSingle(doubleValue);
                    }
                    return 0f;
                case "true":
                    return true;
                case "false":
                    return false;
                case "data":
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
                    return ParseDictionaryNode(valueNode);
                case "array":
                    List<object> list = new List<object>();
                    foreach (XmlNode child in valueNode.ChildNodes)
                    {
                        list.Add(ParseValueNode(child));
                    }
                    return list;
                default:
                    return valueNode.InnerText;
            }
        }

        /// <summary>
        /// Deserializes JSON string to dictionary and normalizes values.
        /// </summary>
        /// <param name="json">JSON string containing PlayerPrefs data</param>
        /// <returns>Dictionary with normalized PlayerPrefs data</returns>
        private static Dictionary<string, object> DeserializeJsonPrefs(string json)
        {
            try
            {
                // Ensure proper UTF-8 handling
                Dictionary<string, object> rawPrefs = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
                });
                return PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(rawPrefs);
            }
            catch (Exception e)
            {
                UnityDebug.LogError($"[PlayerPrefsRuntime] Failed to deserialize PlayerPrefs JSON: {e.Message}\nJSON: {json}");
                return new Dictionary<string, object>();
            }
        }
    }
}
#endif
