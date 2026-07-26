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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Value types supported by the PlayerPrefs export/import format.
    /// Names intentionally match <c>value.GetType().Name</c> used across the tool.
    /// </summary>
    internal enum PlayerPrefsRuntimeValueType
    {
        Int32,
        Single,
        String
    }

    /// <summary>
    /// A single typed entry parsed from an export document.
    /// </summary>
    internal sealed class PlayerPrefsRuntimeParsedEntry
    {
        internal string Key { get; }
        internal PlayerPrefsRuntimeValueType ValueType { get; }
        internal object Value { get; }

        internal PlayerPrefsRuntimeParsedEntry(string key, PlayerPrefsRuntimeValueType valueType, object value)
        {
            Key = key;
            ValueType = valueType;
            Value = value;
        }
    }

    /// <summary>
    /// Result of parsing an export document: successfully parsed entries plus per-key errors.
    /// </summary>
    internal sealed class PlayerPrefsRuntimeParseResult
    {
        internal List<PlayerPrefsRuntimeParsedEntry> Entries { get; } = new List<PlayerPrefsRuntimeParsedEntry>();
        internal List<PlayerPrefsRuntimeImportError> Errors { get; } = new List<PlayerPrefsRuntimeImportError>();
    }

    /// <summary>
    /// Pure JSON serialization for PlayerPrefs export/import. Performs no PlayerPrefs or file I/O.
    /// Export format is a typed envelope so int/float/numeric-string values survive a round-trip unambiguously:
    /// <code>{ "version": 1, "prefs": { "Score": { "type": "Int32", "value": 42 } } }</code>
    /// </summary>
    internal static class PlayerPrefsRuntimeJsonSerializer
    {
        internal const int FormatVersion = 1;

        private const string VersionPropertyName = "version";
        private const string PrefsPropertyName = "prefs";
        private const string TypePropertyName = "type";
        private const string ValuePropertyName = "value";

        /// <summary>
        /// Serializes the given prefs dictionary into the typed-envelope JSON format.
        /// Keys are sorted for deterministic output. Values that are not int/float/string are skipped with a warning.
        /// </summary>
        internal static string Serialize(Dictionary<string, object> prefs)
        {
            List<string> skippedKeys;
            return Serialize(prefs, out skippedKeys);
        }

        internal static string Serialize(Dictionary<string, object> prefs, out List<string> skippedKeys)
        {
            JObject prefsObject = new JObject();
            skippedKeys = new List<string>();

            if (prefs != null)
            {
                List<string> keys = new List<string>(prefs.Keys);
                keys.Sort(StringComparer.Ordinal);

                foreach (string key in keys)
                {
                    object value = prefs[key];
                    JObject entryObject = CreateEntryObject(value);

                    if (entryObject == null)
                    {
                        skippedKeys.Add(key);
                        Debug.LogWarning($"[PlayerPrefsRuntime] Export skipped key '{key}': unsupported value type '{value?.GetType().Name ?? "null"}'.");
                        continue;
                    }

                    prefsObject[key] = entryObject;
                }
            }

            JObject root = new JObject
            {
                [VersionPropertyName] = FormatVersion,
                [PrefsPropertyName] = prefsObject
            };

            return root.ToString(Formatting.Indented);
        }

        /// <summary>
        /// Parses an export document. Accepts the typed envelope produced by <see cref="Serialize"/> and,
        /// as a best effort, a legacy flat <c>{"key": value}</c> object. Never throws: problems are
        /// reported per key (or with an empty key for document-level errors).
        /// </summary>
        internal static PlayerPrefsRuntimeParseResult Parse(string json)
        {
            PlayerPrefsRuntimeParseResult result = new PlayerPrefsRuntimeParseResult();

            if (string.IsNullOrWhiteSpace(json))
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(string.Empty, "JSON document is empty."));
                return result;
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException exception)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(string.Empty, $"Malformed JSON: {exception.Message}"));
                return result;
            }

            // A prefs property that cannot be a legacy PlayerPrefs scalar identifies an
            // attempted typed envelope, including malformed shapes such as null or an array.
            // Valid legacy flat documents may still contain scalar keys named "version"
            // or "prefs".
            JProperty prefsProperty = root.Property(PrefsPropertyName);
            JToken prefsToken = prefsProperty?.Value;
            bool looksLikeEnvelope = prefsProperty != null && !IsSupportedFlatValue(prefsToken);
            if (looksLikeEnvelope)
            {
                if (!(prefsToken is JObject prefsObject))
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(string.Empty, "Envelope is missing a 'prefs' object."));
                    return result;
                }

                JToken versionToken = root[VersionPropertyName];
                if (versionToken == null || versionToken.Type != JTokenType.Integer || !TryGetInt(versionToken, out int version) || version != FormatVersion)
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(
                        string.Empty,
                        $"Unsupported export format version '{versionToken}'. Expected version {FormatVersion}."));
                    return result;
                }

                ParseEnvelopeEntries(prefsObject, result);
            }
            else
            {
                ParseFlatEntries(root, result);
            }

            return result;
        }

        private static bool IsSupportedFlatValue(JToken token)
        {
            return token != null
                && (token.Type == JTokenType.Integer
                    || token.Type == JTokenType.Float
                    || token.Type == JTokenType.String);
        }

        private static bool TryGetInt(JToken token, out int value)
        {
            try
            {
                value = token.Value<int>();
                return true;
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
        }

        private static JObject CreateEntryObject(object value)
        {
            switch (value)
            {
                case int intValue:
                    return new JObject
                    {
                        [TypePropertyName] = nameof(PlayerPrefsRuntimeValueType.Int32),
                        [ValuePropertyName] = intValue
                    };
                case float floatValue:
                    return new JObject
                    {
                        [TypePropertyName] = nameof(PlayerPrefsRuntimeValueType.Single),
                        [ValuePropertyName] = floatValue
                    };
                case string stringValue:
                    return new JObject
                    {
                        [TypePropertyName] = nameof(PlayerPrefsRuntimeValueType.String),
                        [ValuePropertyName] = stringValue
                    };
                default:
                    return null;
            }
        }

        private static void ParseEnvelopeEntries(JObject prefsObject, PlayerPrefsRuntimeParseResult result)
        {
            foreach (JProperty property in prefsObject.Properties())
            {
                string key = property.Name;

                if (string.IsNullOrEmpty(key))
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Key is empty."));
                    continue;
                }

                if (!(property.Value is JObject entryObject))
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Entry is not an object with 'type' and 'value'."));
                    continue;
                }

                JToken typeToken = entryObject[TypePropertyName];
                JToken valueToken = entryObject[ValuePropertyName];

                if (typeToken == null || typeToken.Type != JTokenType.String || valueToken == null)
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Entry is missing 'type' or 'value'."));
                    continue;
                }

                string typeName = typeToken.Value<string>();
                switch (typeName)
                {
                    case nameof(PlayerPrefsRuntimeValueType.Int32):
                        ParseIntEntry(key, valueToken, result);
                        break;
                    case nameof(PlayerPrefsRuntimeValueType.Single):
                        ParseFloatEntry(key, valueToken, result);
                        break;
                    case nameof(PlayerPrefsRuntimeValueType.String):
                        ParseStringEntry(key, valueToken, result);
                        break;
                    default:
                        result.Errors.Add(new PlayerPrefsRuntimeImportError(key, $"Unknown value type '{typeName}'."));
                        break;
                }
            }
        }

        private static void ParseFlatEntries(JObject root, PlayerPrefsRuntimeParseResult result)
        {
            foreach (JProperty property in root.Properties())
            {
                string key = property.Name;

                if (string.IsNullOrEmpty(key))
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Key is empty."));
                    continue;
                }

                JToken valueToken = property.Value;
                switch (valueToken.Type)
                {
                    case JTokenType.Integer:
                        ParseIntEntry(key, valueToken, result);
                        break;
                    case JTokenType.Float:
                        ParseFloatEntry(key, valueToken, result);
                        break;
                    case JTokenType.String:
                        ParseStringEntry(key, valueToken, result);
                        break;
                    default:
                        result.Errors.Add(new PlayerPrefsRuntimeImportError(key, $"Unsupported value type '{valueToken.Type}'."));
                        break;
                }
            }
        }

        private static void ParseIntEntry(string key, JToken valueToken, PlayerPrefsRuntimeParseResult result)
        {
            if (valueToken.Type != JTokenType.Integer)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is not an integer."));
                return;
            }

            // Value<long>() throws for integers outside Int64 (JSON.NET stores them as BigInteger);
            // guard so an over-range value becomes a per-key error instead of an uncaught exception.
            long longValue;
            try
            {
                longValue = valueToken.Value<long>();
            }
            catch (Exception)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is out of Int32 range."));
                return;
            }

            if (longValue < int.MinValue || longValue > int.MaxValue)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is out of Int32 range."));
                return;
            }

            result.Entries.Add(new PlayerPrefsRuntimeParsedEntry(key, PlayerPrefsRuntimeValueType.Int32, (int)longValue));
        }

        private static void ParseFloatEntry(string key, JToken valueToken, PlayerPrefsRuntimeParseResult result)
        {
            // NaN/Infinity are valid PlayerPrefs floats but JSON.NET serializes them as strings
            // ("NaN"/"Infinity"), so a string token is accepted here to keep the round-trip lossless.
            if (valueToken.Type == JTokenType.String)
            {
                string raw = valueToken.Value<string>();
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat))
                {
                    result.Entries.Add(new PlayerPrefsRuntimeParsedEntry(key, PlayerPrefsRuntimeValueType.Single, parsedFloat));
                }
                else
                {
                    result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is not a valid Single."));
                }
                return;
            }

            if (valueToken.Type != JTokenType.Float && valueToken.Type != JTokenType.Integer)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is not a number."));
                return;
            }

            float floatValue;
            try
            {
                floatValue = valueToken.Value<float>();
            }
            catch (Exception)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is not a valid Single."));
                return;
            }

            result.Entries.Add(new PlayerPrefsRuntimeParsedEntry(key, PlayerPrefsRuntimeValueType.Single, floatValue));
        }

        private static void ParseStringEntry(string key, JToken valueToken, PlayerPrefsRuntimeParseResult result)
        {
            if (valueToken.Type != JTokenType.String)
            {
                result.Errors.Add(new PlayerPrefsRuntimeImportError(key, "Value is not a string."));
                return;
            }

            result.Entries.Add(new PlayerPrefsRuntimeParsedEntry(key, PlayerPrefsRuntimeValueType.String, valueToken.Value<string>()));
        }
    }
}
#endif
