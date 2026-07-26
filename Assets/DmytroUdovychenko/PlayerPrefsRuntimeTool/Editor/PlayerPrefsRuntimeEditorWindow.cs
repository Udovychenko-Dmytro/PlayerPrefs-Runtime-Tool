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
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Editor
{
    /// <summary>
    /// Editor window for viewing and editing PlayerPrefs without entering Play Mode.
    /// Deliberately consumes only the public PlayerPrefsRuntime API.
    /// Note: on macOS the plist-based fetcher can lag slightly behind fresh writes
    /// (cfprefsd flushes lazily) — press Refresh again if a value looks stale.
    /// </summary>
    public sealed class PlayerPrefsRuntimeEditorWindow : EditorWindow
    {
        private enum SortColumn
        {
            Key,
            Type,
            Value
        }

        private const float TypeColumnWidth = 70f;
        private const float ActionButtonWidth = 55f;

        private static readonly string[] s_newTypeOptions = { "Int", "Float", "String" };

        private readonly List<PlayerPrefsRuntimeEntry> m_entries = new List<PlayerPrefsRuntimeEntry>();

        private string m_searchQuery = string.Empty;
        private Vector2 m_scrollPosition;
        private SortColumn m_sortColumn = SortColumn.Key;
        private bool m_sortAscending = true;
        private string m_editKey;
        private string m_editValueText = string.Empty;
        private string m_newKey = string.Empty;
        private int m_newTypeIndex = 2;
        private string m_newValueText = string.Empty;

        [MenuItem("Tools/PlayerPrefs Runtime Viewer")]
        private static void ShowWindow()
        {
            PlayerPrefsRuntimeEditorWindow window = GetWindow<PlayerPrefsRuntimeEditorWindow>("PlayerPrefs Runtime");
            window.minSize = new Vector2(560f, 320f);
        }

        private void OnEnable()
        {
            RefreshEntries();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSearchBar();
            DrawColumnHeaders();
            DrawEntryList();
            DrawAddRow();
        }

        private void RefreshEntries()
        {
            m_entries.Clear();

            Dictionary<string, object> prefs = PlayerPrefsRuntime.GetAllPlayerPrefs();
            foreach (KeyValuePair<string, object> pair in prefs)
            {
                m_entries.Add(new PlayerPrefsRuntimeEntry(pair.Key, pair.Value));
            }

            SortEntries();
        }

        private void SortEntries()
        {
            Comparison<PlayerPrefsRuntimeEntry> comparison;
            switch (m_sortColumn)
            {
                case SortColumn.Type:
                    comparison = (x, y) => StringComparer.Ordinal.Compare(x.Type, y.Type);
                    break;
                case SortColumn.Value:
                    comparison = (x, y) => StringComparer.Ordinal.Compare(x.Value, y.Value);
                    break;
                default:
                    comparison = (x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name);
                    break;
            }

            m_entries.Sort((x, y) => m_sortAscending ? comparison(x, y) : comparison(y, x));
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RefreshEntries();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export...", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                ExportToFile();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Import...", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                ImportFromFile();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                DeleteAllEntries();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(55f));
            m_searchQuery = EditorGUILayout.TextField(m_searchQuery);
            if (GUILayout.Button("x", GUILayout.Width(22f)))
            {
                m_searchQuery = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal();
            DrawColumnHeader("Key", SortColumn.Key, GUILayout.MinWidth(150f));
            DrawColumnHeader("Type", SortColumn.Type, GUILayout.Width(TypeColumnWidth));
            DrawColumnHeader("Value", SortColumn.Value, GUILayout.MinWidth(150f));
            GUILayout.Space(2f * ActionButtonWidth + 8f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnHeader(string title, SortColumn column, GUILayoutOption sizing)
        {
            string arrow = m_sortColumn == column ? (m_sortAscending ? " ▲" : " ▼") : string.Empty;
            if (GUILayout.Button(title + arrow, EditorStyles.boldLabel, sizing))
            {
                if (m_sortColumn == column)
                {
                    m_sortAscending = !m_sortAscending;
                }
                else
                {
                    m_sortColumn = column;
                    m_sortAscending = true;
                }

                SortEntries();
            }
        }

        private void DrawEntryList()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            int visibleCount = 0;
            for (int i = 0; i < m_entries.Count; i++)
            {
                PlayerPrefsRuntimeEntry entry = m_entries[i];
                if (!MatchesSearch(entry))
                {
                    continue;
                }

                visibleCount++;
                DrawEntryRow(entry);
            }

            if (visibleCount == 0)
            {
                EditorGUILayout.HelpBox("No PlayerPrefs entries found.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntryRow(PlayerPrefsRuntimeEntry entry)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.SelectableLabel(entry.Name, GUILayout.MinWidth(150f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField(entry.Type, GUILayout.Width(TypeColumnWidth));

            bool isEditing = string.Equals(m_editKey, entry.Name, StringComparison.Ordinal);
            if (isEditing)
            {
                m_editValueText = EditorGUILayout.TextField(m_editValueText, GUILayout.MinWidth(150f));

                if (GUILayout.Button("Save", GUILayout.Width(ActionButtonWidth)))
                {
                    ApplyEdit(entry, m_editValueText);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(ActionButtonWidth)))
                {
                    m_editKey = null;
                    GUI.FocusControl(null);
                }
            }
            else
            {
                EditorGUILayout.SelectableLabel(entry.Value, GUILayout.MinWidth(150f), GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (GUILayout.Button("Edit", GUILayout.Width(ActionButtonWidth)))
                {
                    m_editKey = entry.Name;
                    m_editValueText = entry.Value;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Delete", GUILayout.Width(ActionButtonWidth)))
                {
                    DeleteEntry(entry.Name);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddRow()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Add:", GUILayout.Width(35f));
            m_newKey = EditorGUILayout.TextField(m_newKey, GUILayout.MinWidth(120f));
            m_newTypeIndex = EditorGUILayout.Popup(m_newTypeIndex, s_newTypeOptions, GUILayout.Width(70f));
            m_newValueText = EditorGUILayout.TextField(m_newValueText, GUILayout.MinWidth(120f));

            if (GUILayout.Button("Add", GUILayout.Width(ActionButtonWidth)))
            {
                AddNewEntry();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private bool MatchesSearch(PlayerPrefsRuntimeEntry entry)
        {
            if (string.IsNullOrEmpty(m_searchQuery))
            {
                return true;
            }

            return Contains(entry.Name, m_searchQuery) || Contains(entry.Type, m_searchQuery) || Contains(entry.Value, m_searchQuery);
        }

        private static bool Contains(string source, string filter)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplyEdit(PlayerPrefsRuntimeEntry entry, string newValueText)
        {
            switch (entry.Type)
            {
                case "Int32":
                    if (int.TryParse(newValueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        PlayerPrefsRuntime.SetInt(entry.Name, intValue);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid value", $"'{newValueText}' is not a valid Int32.", "OK");
                        return;
                    }
                    break;

                case "Single":
                    if (float.TryParse(newValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        PlayerPrefsRuntime.SetFloat(entry.Name, floatValue);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid value", $"'{newValueText}' is not a valid Float.", "OK");
                        return;
                    }
                    break;

                case "String":
                    PlayerPrefsRuntime.SetString(entry.Name, newValueText);
                    break;

                default:
                    EditorUtility.DisplayDialog("Unsupported type", $"Editing values of type '{entry.Type}' is not supported.", "OK");
                    return;
            }

            m_editKey = null;
            RefreshEntries();
        }

        private void DeleteEntry(string key)
        {
            if (!EditorUtility.DisplayDialog("Remove entry", $"Remove PlayerPrefs entry '{key}'?", "Delete", "Cancel"))
            {
                return;
            }

            PlayerPrefsRuntime.DeleteKey(key);
            RefreshEntries();
        }

        private void DeleteAllEntries()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete ALL PlayerPrefs?",
                "This removes every PlayerPrefs entry, including engine-managed keys. If entries exist, the operation is cancelled unless a verified complete JSON backup is saved to persistentDataPath first. This cannot be undone.",
                "Delete All",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            if (!PlayerPrefsRuntime.TryDeleteAll())
            {
                EditorUtility.DisplayDialog(
                    "Delete All cancelled",
                    "A complete PlayerPrefs snapshot and backup could not be verified. No entries were deleted; see the Console for details.",
                    "OK");
                return;
            }

            m_editKey = null;
            m_editValueText = string.Empty;
            RefreshEntries();
        }

        private void ExportToFile()
        {
            string path = EditorUtility.SaveFilePanel("Export PlayerPrefs", string.Empty, "playerprefs_export.json", "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                File.WriteAllText(path, PlayerPrefsRuntime.ExportToJson());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Export file write failed: {exception}");
                EditorUtility.DisplayDialog("Export failed", $"Could not write to:\n{path}\n\n{exception.Message}", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Export complete", $"PlayerPrefs exported to:\n{path}", "OK");
        }

        private void ImportFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Import PlayerPrefs", string.Empty, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Import file read failed: {exception}");
                EditorUtility.DisplayDialog("Import failed", $"Could not read:\n{path}\n\n{exception.Message}", "OK");
                return;
            }

            PlayerPrefsRuntimeImportResult result = PlayerPrefsRuntime.ImportFromJson(json);

            StringBuilder message = new StringBuilder();
            message.AppendFormat("Imported {0} entries, {1} errors.", result.ImportedCount, result.Errors.Count);

            int displayedErrors = Mathf.Min(result.Errors.Count, 10);
            for (int i = 0; i < displayedErrors; i++)
            {
                message.AppendLine();
                message.Append(string.IsNullOrEmpty(result.Errors[i].Key) ? "-" : result.Errors[i].Key);
                message.Append(": ");
                message.Append(result.Errors[i].Message);
            }

            if (result.Errors.Count > displayedErrors)
            {
                message.AppendLine();
                message.AppendFormat("...and {0} more.", result.Errors.Count - displayedErrors);
            }

            EditorUtility.DisplayDialog("Import finished", message.ToString(), "OK");
            RefreshEntries();
        }

        private void AddNewEntry()
        {
            string key = m_newKey != null ? m_newKey.Trim() : string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                EditorUtility.DisplayDialog("Invalid key", "Key cannot be empty.", "OK");
                return;
            }

            bool keyAlreadyExists = PlayerPrefs.HasKey(key) || m_entries.Exists(entry => string.Equals(entry.Name, key, StringComparison.Ordinal));
            if (keyAlreadyExists)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Key already exists",
                    $"Key '{key}' already exists. Overwrite it (its type may change)?",
                    "Overwrite",
                    "Cancel");

                if (!overwrite)
                {
                    return;
                }
            }

            switch (m_newTypeIndex)
            {
                case 0:
                    if (int.TryParse(m_newValueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        PlayerPrefsRuntime.SetInt(key, intValue);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid value", $"'{m_newValueText}' is not a valid Int32.", "OK");
                        return;
                    }
                    break;

                case 1:
                    if (float.TryParse(m_newValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        PlayerPrefsRuntime.SetFloat(key, floatValue);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid value", $"'{m_newValueText}' is not a valid Float.", "OK");
                        return;
                    }
                    break;

                default:
                    PlayerPrefsRuntime.SetString(key, m_newValueText ?? string.Empty);
                    break;
            }

            m_newKey = string.Empty;
            m_newValueText = string.Empty;
            GUI.FocusControl(null);
            RefreshEntries();
        }
    }
}
#endif
