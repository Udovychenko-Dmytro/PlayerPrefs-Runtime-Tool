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
using System.IO;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Centralized mutation core for PlayerPrefs. All writes performed by the tool
    /// (runtime viewer, editor window, public API) go through this class, which owns
    /// the save policy, change notifications and automatic backups before destructive operations.
    /// </summary>
    internal static class PlayerPrefsRuntimeWriter
    {
        private const string BackupFileNameFormat = "playerprefs_backup_{0}_{1}.json";
        private const string BackupTimestampFormat = "yyyyMMdd_HHmmss_fff";

        internal static void SetInt(string key, int value)
        {
            if (!ValidateKey(key))
            {
                return;
            }

            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            PlayerPrefsRuntime.RaiseEntryChanged(key);
        }

        internal static void SetFloat(string key, float value)
        {
            if (!ValidateKey(key))
            {
                return;
            }

            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            PlayerPrefsRuntime.RaiseEntryChanged(key);
        }

        internal static void SetString(string key, string value)
        {
            if (!ValidateKey(key))
            {
                return;
            }

            PlayerPrefs.SetString(key, value ?? string.Empty);
            PlayerPrefs.Save();
            PlayerPrefsRuntime.RaiseEntryChanged(key);
        }

        internal static void DeleteKey(string key)
        {
            if (!ValidateKey(key))
            {
                return;
            }

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            PlayerPrefsRuntime.RaiseEntryChanged(key);
        }

        internal static bool DeleteAll()
        {
            bool isComplete = PlayerPrefsRuntime.TryGetAllPlayerPrefs(out Dictionary<string, object> prefs);
            return DeleteAll(prefs, isComplete);
        }

        /// <summary>
        /// Completes the destructive half of DeleteAll only for a verified snapshot.
        /// Split from enumeration so the cancellation contract can be unit-tested
        /// without replacing the active platform fetcher.
        /// </summary>
        internal static bool DeleteAll(Dictionary<string, object> prefs, bool isComplete)
        {
            if (!isComplete || prefs == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] DeleteAll cancelled: the PlayerPrefs snapshot could not be verified as complete.");
                return false;
            }

            if (prefs.Count == 0)
            {
                Debug.Log("[PlayerPrefsRuntime] DeleteAll: the verified PlayerPrefs store is already empty.");
                return true;
            }

            string backupPath = TryWriteBackup("DeleteAll", prefs, true, out List<string> skippedKeys);
            if (string.IsNullOrEmpty(backupPath) || skippedKeys.Count > 0)
            {
                Debug.LogWarning(
                    $"[PlayerPrefsRuntime] DeleteAll cancelled: a complete backup could not be created"
                    + (skippedKeys.Count > 0 ? $" ({skippedKeys.Count} unsupported entries)." : "."));
                return false;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            PlayerPrefsRuntime.RaiseEntryChanged(null);
            return true;
        }

        internal static string ExportToJson()
        {
            Dictionary<string, object> prefs = PlayerPrefsRuntime.GetAllPlayerPrefs();
            return PlayerPrefsRuntimeJsonSerializer.Serialize(prefs);
        }

        internal static PlayerPrefsRuntimeImportResult ImportFromJson(string json)
        {
            string backupPath;
            return ImportFromJson(json, out backupPath);
        }

        internal static PlayerPrefsRuntimeImportResult ImportFromJson(string json, out string backupPath)
        {
            PlayerPrefsRuntimeParseResult parsed = PlayerPrefsRuntimeJsonSerializer.Parse(json);
            List<PlayerPrefsRuntimeParsedEntry> applied = new List<PlayerPrefsRuntimeParsedEntry>(parsed.Entries.Count);
            backupPath = null;

            if (parsed.Entries.Count > 0)
            {
                bool backupSnapshotComplete = PlayerPrefsRuntime.TryGetAllPlayerPrefs(out Dictionary<string, object> existingPrefs);
                backupPath = TryWriteBackup("Import", existingPrefs, false, out List<string> skippedKeys);
                if (string.IsNullOrEmpty(backupPath))
                {
                    parsed.Errors.Add(new PlayerPrefsRuntimeImportError(
                        string.Empty,
                        "Automatic backup could not be created; import continued without a backup."));
                }
                else if (!backupSnapshotComplete || skippedKeys.Count > 0)
                {
                    parsed.Errors.Add(new PlayerPrefsRuntimeImportError(
                        string.Empty,
                        "Automatic backup could not be verified as complete"
                        + (skippedKeys.Count > 0 ? $" and skipped {skippedKeys.Count} unsupported entries" : string.Empty)
                        + "; import continued with a partial backup."));
                }
            }

            foreach (PlayerPrefsRuntimeParsedEntry entry in parsed.Entries)
            {
                if (!ValidateKey(entry.Key))
                {
                    parsed.Errors.Add(new PlayerPrefsRuntimeImportError(entry.Key, "Invalid PlayerPrefs key."));
                    continue;
                }

                try
                {
                    switch (entry.ValueType)
                    {
                        case PlayerPrefsRuntimeValueType.Int32:
                            PlayerPrefs.SetInt(entry.Key, (int)entry.Value);
                            break;
                        case PlayerPrefsRuntimeValueType.Single:
                            PlayerPrefs.SetFloat(entry.Key, (float)entry.Value);
                            break;
                        case PlayerPrefsRuntimeValueType.String:
                            PlayerPrefs.SetString(entry.Key, (string)entry.Value);
                            break;
                    }
                }
                catch (Exception exception)
                {
                    parsed.Errors.Add(new PlayerPrefsRuntimeImportError(entry.Key, $"Failed to write PlayerPrefs value: {exception.Message}"));
                    continue;
                }

                applied.Add(entry);
            }

            if (applied.Count > 0)
            {
                try
                {
                    PlayerPrefs.Save();
                }
                catch (Exception exception)
                {
                    parsed.Errors.Add(new PlayerPrefsRuntimeImportError(string.Empty, $"Failed to save imported PlayerPrefs: {exception.Message}"));
                }
            }

            foreach (PlayerPrefsRuntimeParsedEntry entry in applied)
            {
                PlayerPrefsRuntime.RaiseEntryChanged(entry.Key);
            }

            return new PlayerPrefsRuntimeImportResult(applied.Count, parsed.Errors, applied);
        }

        /// <summary>
        /// Writes a JSON snapshot of all current PlayerPrefs to Application.persistentDataPath.
        /// Failures are logged; callers decide whether the requested operation may continue.
        /// </summary>
        private static string TryWriteBackup(
            string reason,
            Dictionary<string, object> prefs,
            bool requireComplete,
            out List<string> skippedKeys)
        {
            skippedKeys = new List<string>();
            try
            {
                string json = PlayerPrefsRuntimeJsonSerializer.Serialize(prefs, out skippedKeys);
                if (requireComplete && skippedKeys.Count > 0)
                {
                    return null;
                }

                string timestamp = DateTime.UtcNow.ToString(BackupTimestampFormat, CultureInfo.InvariantCulture);
                string uniqueSuffix = Guid.NewGuid().ToString("N");
                string fileName = string.Format(CultureInfo.InvariantCulture, BackupFileNameFormat, timestamp, uniqueSuffix);
                string path = Path.Combine(Application.persistentDataPath, fileName);

                File.WriteAllText(path, json);
                Debug.Log($"[PlayerPrefsRuntime] {reason}: backup saved to {path}");
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] {reason}: failed to write backup ({exception.Message}). Continuing without backup.");
                return null;
            }
        }

        private static bool ValidateKey(string key)
        {
            if (!IsValidKey(key))
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Invalid PlayerPrefs key '{key}'. Operation skipped.");
                return false;
            }

            return true;
        }

        internal static bool IsValidKey(string key)
        {
            return !string.IsNullOrEmpty(key);
        }
    }
}
#endif
