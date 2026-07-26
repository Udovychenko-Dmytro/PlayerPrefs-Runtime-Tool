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

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Result of a <see cref="PlayerPrefsRuntime.ImportFromJson"/> operation.
    /// </summary>
    public sealed class PlayerPrefsRuntimeImportResult
    {
        /// <summary>
        /// Number of entries that were successfully written to PlayerPrefs.
        /// </summary>
        public int ImportedCount { get; }

        /// <summary>
        /// Errors collected during parsing and import. Document-level errors (e.g. malformed JSON)
        /// have an empty <see cref="PlayerPrefsRuntimeImportError.Key"/>.
        /// </summary>
        public IReadOnlyList<PlayerPrefsRuntimeImportError> Errors { get; }

        /// <summary>
        /// True when at least one error occurred.
        /// </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Entries that were actually written, with their typed values. Used by the viewer
        /// to update its cache in memory, because platform fetchers may lag behind writes.
        /// </summary>
        internal IReadOnlyList<PlayerPrefsRuntimeParsedEntry> AppliedEntries { get; }

        internal PlayerPrefsRuntimeImportResult(
            int importedCount,
            IReadOnlyList<PlayerPrefsRuntimeImportError> errors,
            IReadOnlyList<PlayerPrefsRuntimeParsedEntry> appliedEntries)
        {
            ImportedCount = importedCount;
            Errors = errors ?? Array.Empty<PlayerPrefsRuntimeImportError>();
            AppliedEntries = appliedEntries ?? Array.Empty<PlayerPrefsRuntimeParsedEntry>();
        }
    }

    /// <summary>
    /// A single error produced while importing PlayerPrefs from JSON.
    /// </summary>
    public readonly struct PlayerPrefsRuntimeImportError
    {
        /// <summary>
        /// The affected PlayerPrefs key, or an empty string for document-level errors.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Human-readable description of the problem.
        /// </summary>
        public string Message { get; }

        internal PlayerPrefsRuntimeImportError(string key, string message)
        {
            Key = key ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }
}
#endif
