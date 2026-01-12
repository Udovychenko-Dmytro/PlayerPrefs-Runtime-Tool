// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Responsible for rendering PlayerPrefs entries via a stylized scrollable Canvas.
    /// </summary>
    internal class PlayerPrefsRuntimeViewer
    {
        private enum SortMode
        {
            Name,
            Type
        }

        private static readonly IComparer<PlayerPrefsRuntimeEntry> s_sortByNameComparer = new SortByNameComparer();
        private static readonly IComparer<PlayerPrefsRuntimeEntry> s_sortByTypeComparer = new SortByTypeComparer();

        private readonly List<PlayerPrefsRuntimeEntry> m_entriesCache = new List<PlayerPrefsRuntimeEntry>();
        private readonly List<PlayerPrefsRuntimeEntry> m_filteredEntries = new List<PlayerPrefsRuntimeEntry>();
        private readonly List<PlayerPrefsRuntimeRowView> m_rowPool = new List<PlayerPrefsRuntimeRowView>();
        private readonly PlayerPrefsRuntimeEntryDialog m_entryDialog = new PlayerPrefsRuntimeEntryDialog();
        private readonly PlayerPrefsRuntimeRow m_rowBuilder = new PlayerPrefsRuntimeRow();

        private PlayerPrefsRuntimeViewerBuilder m_ui;
        private Coroutine m_debounceSearchCoroutine;
        private SortMode m_currentSortMode = SortMode.Name;
        private string m_searchFilter = "";
        private bool m_suppressSearchChange;

        public bool IsVisible => m_ui != null && m_ui.IsVisible;

        public void ShowEntries(IReadOnlyList<PlayerPrefsRuntimeEntry> entries)
        {
            if (entries == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] No entries provided for viewer");
                return;
            }

            try
            {
                EnsureUi();

                m_entriesCache.Clear();
                m_entriesCache.AddRange(entries);

                RefreshRows();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Failed to render UI list: {e.Message}");
            }
        }

        public void Hide()
        {
            if (m_ui == null)
            {
                return;
            }

            m_ui.Destroy();
            m_ui = null;
            m_entriesCache.Clear();
            m_filteredEntries.Clear();
            m_rowPool.Clear();
            m_searchFilter = "";
        }

        private void EnsureUi()
        {
            if (m_ui == null)
            {
                m_ui = new PlayerPrefsRuntimeViewerBuilder(OnSortModeButtonClicked, OnSearchValueChanged, OnClearSearchClicked, OnCloseButtonClicked);
            }

            m_ui.EnsureVisible();
        }

        private void RefreshRows()
        {
            if (m_ui == null || !m_ui.IsVisible)
                return;

            if (m_entriesCache == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Entries cache is null");
                return;
            }

            List<PlayerPrefsRuntimeEntry> sortedEntries = GetSortedEntries();
            UpdateHeader(sortedEntries.Count);
            BuildOrRefreshList(sortedEntries);
            UpdateSortModeButtonLabel();
        }

        private void UpdateHeader(int entryCount)
        {
            if (m_ui == null)
                return;

            string noun = entryCount == 1 ? "entry" : "entries";
            string searchText = !string.IsNullOrEmpty(m_searchFilter) ? $" (filtered from {m_entriesCache.Count})" : "";
            string subtitle = $"{entryCount} {noun}{searchText} | Updated {DateTime.Now:HH:mm:ss}";

            m_ui.UpdateSubtitleText(subtitle);
        }

        private void BuildOrRefreshList(IReadOnlyList<PlayerPrefsRuntimeEntry> entries)
        {
            if (m_ui == null)
                return;

            RectTransform contentRoot = m_ui.ContentRoot;
            if (contentRoot == null)
                return;

            EnsureRowPool(contentRoot);

            Font font = m_ui.DefaultFont;
            if (font == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Failed to get font resource");
                return;
            }

            EnsureRowCount(contentRoot, entries.Count, font);

            int entryCount = entries.Count;
            for (int i = 0; i < entryCount; i++)
            {
                PlayerPrefsRuntimeRowView rowView = m_rowPool[i];
                if (rowView == null)
                {
                    continue;
                }

                rowView.gameObject.SetActive(true);

                bool even = (i % 2) == 0;
                PlayerPrefsRuntimeEntry entry = entries[i];
                m_rowBuilder.Update(rowView, entry, even, PlayerPrefsRuntimeViewConstants.RowColorEven, PlayerPrefsRuntimeViewConstants.RowColorOdd, PlayerPrefsRuntimeViewConstants.BadgeColor, OnRowClicked);
            }

            for (int i = entryCount; i < m_rowPool.Count; i++)
            {
                PlayerPrefsRuntimeRowView rowView = m_rowPool[i];
                if (rowView != null)
                {
                    rowView.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureRowPool(RectTransform contentRoot)
        {
            if (m_rowPool.Count > 0)
            {
                return;
            }

            int childCount = contentRoot.childCount;
            if (childCount == 0)
            {
                return;
            }

            bool hasUnknownChildren = false;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                PlayerPrefsRuntimeRowView view = child.GetComponent<PlayerPrefsRuntimeRowView>();
                if (view == null)
                {
                    hasUnknownChildren = true;
                    break;
                }
            }

            if (hasUnknownChildren)
            {
                for (int i = childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(contentRoot.GetChild(i).gameObject);
                }

                return;
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                PlayerPrefsRuntimeRowView view = child.GetComponent<PlayerPrefsRuntimeRowView>();
                if (view != null)
                {
                    m_rowPool.Add(view);
                }
            }
        }

        private void EnsureRowCount(RectTransform contentRoot, int required, Font font)
        {
            if (m_rowPool.Count >= required)
            {
                return;
            }

            int startIndex = m_rowPool.Count;
            for (int i = startIndex; i < required; i++)
            {
                PlayerPrefsRuntimeRowView view = m_rowBuilder.Create(contentRoot, font, PlayerPrefsRuntimeViewConstants.BadgeColor);
                m_rowPool.Add(view);
            }
        }

        private List<PlayerPrefsRuntimeEntry> GetSortedEntries()
        {
            List<PlayerPrefsRuntimeEntry> filteredEntries = FilterEntries();
            IComparer<PlayerPrefsRuntimeEntry> comparer = m_currentSortMode == SortMode.Name ? s_sortByNameComparer : s_sortByTypeComparer;
            filteredEntries.Sort(comparer);
            return filteredEntries;
        }

        private List<PlayerPrefsRuntimeEntry> FilterEntries()
        {
            m_filteredEntries.Clear();

            if (m_entriesCache == null)
            {
                return m_filteredEntries;
            }

            if (string.IsNullOrEmpty(m_searchFilter))
            {
                m_filteredEntries.AddRange(m_entriesCache);
                return m_filteredEntries;
            }

            string filter = m_searchFilter;
            int entryCount = m_entriesCache.Count;
            for (int i = 0; i < entryCount; i++)
            {
                PlayerPrefsRuntimeEntry entry = m_entriesCache[i];
                if (ContainsFilter(entry.Name, filter) || ContainsFilter(entry.Type, filter) || ContainsFilter(entry.Value, filter))
                {
                    m_filteredEntries.Add(entry);
                }
            }

            return m_filteredEntries;
        }

        private void UpdateSortModeButtonLabel()
        {
            if (m_ui == null)
            {
                return;
            }

            string label = m_currentSortMode == SortMode.Name ? "Sort: Name" : "Sort: Type";
            m_ui.UpdateSortModeLabel(label);
        }

        private void OnSearchValueChanged(string value)
        {
            if (m_suppressSearchChange)
            {
                return;
            }

            m_searchFilter = value ?? "";

            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            if (m_debounceSearchCoroutine != null)
            {
                m_ui.StopCoroutine(m_debounceSearchCoroutine);
                m_debounceSearchCoroutine = null;
            }

            m_debounceSearchCoroutine = m_ui.StartCoroutine(DebounceSearchRoutine());
        }

        private IEnumerator DebounceSearchRoutine()
        {
            yield return new WaitForSecondsRealtime(PlayerPrefsRuntimeViewConstants.SearchDebounceTime);
            RefreshRows();
            m_debounceSearchCoroutine = null;
        }

        private void OnClearSearchClicked()
        {
            if (m_ui == null)
            {
                return;
            }

            if (m_debounceSearchCoroutine != null)
            {
                m_ui.StopCoroutine(m_debounceSearchCoroutine);
                m_debounceSearchCoroutine = null;
            }

            m_suppressSearchChange = true;
            try
            {
                m_ui.SetSearchText(string.Empty);
            }
            finally
            {
                m_suppressSearchChange = false;
            }
            m_searchFilter = "";
            RefreshRows();
        }

        private void OnCloseButtonClicked()
        {
            Hide();
        }

        private void OnSortModeButtonClicked()
        {
            m_currentSortMode = m_currentSortMode == SortMode.Name ? SortMode.Type : SortMode.Name;
            RefreshRows();
        }

        private void OnRowClicked(PlayerPrefsRuntimeEntry entry)
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            m_entryDialog.Show(m_ui.PanelTransform, entry, m_ui.DefaultFont, OnEntryRemoved, OnEntryUpdated);
        }

        private void OnEntryRemoved(PlayerPrefsRuntimeEntry entry)
        {
            if (entry.Name == null)
            {
                return;
            }

            m_entriesCache.RemoveAll(e => string.Equals(e.Name, entry.Name, StringComparison.Ordinal));
            RefreshRows();
        }

        private void OnEntryUpdated(PlayerPrefsRuntimeEntry entry)
        {
            if (entry.Name == null)
            {
                return;
            }

            int index = m_entriesCache.FindIndex(e => string.Equals(e.Name, entry.Name, StringComparison.Ordinal));
            if (index >= 0)
            {
                m_entriesCache[index] = entry;
                RefreshRows();
            }
        }

        private static bool ContainsFilter(string source, string filter)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            return source.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class SortByNameComparer : IComparer<PlayerPrefsRuntimeEntry>
        {
            public int Compare(PlayerPrefsRuntimeEntry x, PlayerPrefsRuntimeEntry y)
            {
                string xName = x.Name;
                string yName = y.Name;
                int nameCompare = StringComparer.Ordinal.Compare(xName, yName);
                if (nameCompare != 0)
                {
                    return nameCompare;
                }

                string xType = x.Type;
                string yType = y.Type;
                return StringComparer.Ordinal.Compare(xType, yType);
            }
        }

        private sealed class SortByTypeComparer : IComparer<PlayerPrefsRuntimeEntry>
        {
            public int Compare(PlayerPrefsRuntimeEntry x, PlayerPrefsRuntimeEntry y)
            {
                string xType = x.Type;
                string yType = y.Type;
                int typeCompare = StringComparer.Ordinal.Compare(xType, yType);
                if (typeCompare != 0)
                {
                    return typeCompare;
                }

                string xName = x.Name;
                string yName = y.Name;
                return StringComparer.Ordinal.Compare(xName, yName);
            }
        }
    }
}
#endif
