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
using System.Globalization;
using System.IO;
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
        private readonly PlayerPrefsRuntimeConfirmDialog m_confirmDialog = new PlayerPrefsRuntimeConfirmDialog();
        private readonly PlayerPrefsRuntimeAddEntryDialog m_addEntryDialog = new PlayerPrefsRuntimeAddEntryDialog();
        private readonly PlayerPrefsRuntimeImportDialog m_importDialog = new PlayerPrefsRuntimeImportDialog();
        private readonly PlayerPrefsRuntimeRow m_rowBuilder = new PlayerPrefsRuntimeRow();

        private PlayerPrefsRuntimeViewerBuilder m_ui;
        private Coroutine m_debounceSearchCoroutine;
        private Coroutine m_statusMessageCoroutine;
        private SortMode m_currentSortMode = SortMode.Name;
        private string m_searchFilter = "";
        private bool m_suppressSearchChange;
        private List<PlayerPrefsRuntimeEntry> m_currentEntries;
        private int m_firstVisibleIndex = -1;
        private bool m_resetScrollOnNextRefresh;
        private bool m_toolsExpanded;
        private readonly HashSet<string> m_activeTypeFilters = new HashSet<string>(StringComparer.Ordinal);

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

            m_entryDialog.Close();
            m_confirmDialog.Close();
            m_addEntryDialog.Close();
            m_importDialog.Close();

            m_ui.Destroy();
            m_ui = null;
            m_entriesCache.Clear();
            m_filteredEntries.Clear();
            m_rowPool.Clear();
            m_searchFilter = "";
            m_debounceSearchCoroutine = null;
            m_statusMessageCoroutine = null;
            m_currentEntries = null;
            m_firstVisibleIndex = -1;
            m_resetScrollOnNextRefresh = false;
            m_toolsExpanded = false;
            m_activeTypeFilters.Clear();
        }

        private void EnsureUi()
        {
            bool rebuildAfterExternalDestroy = m_ui != null && !m_ui.IsVisible;
            if (m_ui == null)
            {
                PlayerPrefsRuntimeViewerCallbacks callbacks = new PlayerPrefsRuntimeViewerCallbacks
                {
                    OnSortModeButtonClicked = OnSortModeButtonClicked,
                    OnSearchValueChanged = OnSearchValueChanged,
                    OnClearSearchClicked = OnClearSearchClicked,
                    OnCloseButtonClicked = OnCloseButtonClicked,
                    OnToolsToggleClicked = OnToolsToggleClicked,
                    OnDeleteAllClicked = OnDeleteAllClicked,
                    OnAddClicked = OnAddClicked,
                    OnExportClicked = OnExportClicked,
                    OnImportClicked = OnImportClicked,
                    OnTypeFilterToggled = OnTypeFilterToggled,
                    OnScrollValueChanged = OnScrollValueChanged
                };

                m_ui = new PlayerPrefsRuntimeViewerBuilder(callbacks);
            }

            m_ui.EnsureVisible();
            m_ui.SetToolsExpanded(m_toolsExpanded);

            if (rebuildAfterExternalDestroy)
            {
                // Scene changes or another owner can destroy the generated Canvas without
                // calling Hide(). Unity then leaves fake-null row components in the pool.
                m_rowPool.Clear();
                m_firstVisibleIndex = -1;
                m_debounceSearchCoroutine = null;
                m_statusMessageCoroutine = null;

                m_suppressSearchChange = true;
                try
                {
                    m_ui.SetSearchText(m_searchFilter);
                }
                finally
                {
                    m_suppressSearchChange = false;
                }

                foreach (string typeName in m_activeTypeFilters)
                {
                    m_ui.SetTypeFilterActive(typeName, true);
                }
            }
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

            bool filtered = !string.IsNullOrEmpty(m_searchFilter) || m_activeTypeFilters.Count > 0;
            string noun = entryCount == 1 ? "entry" : "entries";
            string searchText = filtered ? $" (filtered from {m_entriesCache.Count})" : "";

            long totalBytes = 0;
            for (int i = 0; i < m_entriesCache.Count; i++)
            {
                totalBytes += m_entriesCache[i].EstimateValueSizeBytes();
            }

            string sizeText = PlayerPrefsRuntimeViewConstants.SubtitleSizeSeparator + PlayerPrefsRuntimeEntry.FormatByteSize(totalBytes);
            string subtitle = $"{entryCount} {noun}{searchText}{sizeText} | Updated {DateTime.Now:HH:mm:ss}";

            m_ui.UpdateSubtitleText(subtitle);
        }

        private void BuildOrRefreshList(List<PlayerPrefsRuntimeEntry> entries)
        {
            if (m_ui == null)
                return;

            RectTransform contentRoot = m_ui.ContentRoot;
            RectTransform viewport = m_ui.ViewportRect;
            if (contentRoot == null || viewport == null)
                return;

            Font font = m_ui.DefaultFont;
            if (font == null)
            {
                Debug.LogWarning("[PlayerPrefsRuntime] Failed to get font resource");
                return;
            }

            m_currentEntries = entries;

            int entryCount = entries.Count;
            float stride = PlayerPrefsRuntimeViewConstants.RowFixedHeight + PlayerPrefsRuntimeViewConstants.RowVirtualSpacing;
            float contentHeight = PlayerPrefsRuntimeViewConstants.ListPadding * 2f
                + entryCount * PlayerPrefsRuntimeViewConstants.RowFixedHeight
                + Mathf.Max(0, entryCount - 1) * PlayerPrefsRuntimeViewConstants.RowVirtualSpacing;

            m_ui.SetContentHeight(contentHeight);

            if (viewport.rect.height <= 0f)
            {
                // First frame after the canvas was built: force a layout pass so the
                // viewport has a real size before the pool is dimensioned.
                Canvas.ForceUpdateCanvases();
            }

            float viewportHeight = Mathf.Max(viewport.rect.height, PlayerPrefsRuntimeViewConstants.RowFixedHeight);

            Vector2 contentPosition = contentRoot.anchoredPosition;
            if (m_resetScrollOnNextRefresh)
            {
                contentPosition.y = 0f;
                m_resetScrollOnNextRefresh = false;
            }

            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            contentPosition.y = Mathf.Clamp(contentPosition.y, 0f, maxScroll);
            contentRoot.anchoredPosition = contentPosition;

            EnsureRowPoolSize(contentRoot, font, viewportHeight, stride);
            UpdateVisibleRows(true);
        }

        private void EnsureRowPoolSize(RectTransform contentRoot, Font font, float viewportHeight, float stride)
        {
            // CeilToInt + 1 covers the fractional scroll offset (a partial row at the top edge)
            // even if VirtualizationBufferRows is ever reduced to 0; the buffer adds recycling slack.
            for (int i = m_rowPool.Count - 1; i >= 0; i--)
            {
                if (m_rowPool[i] == null)
                {
                    m_rowPool.RemoveAt(i);
                }
            }

            int required = Mathf.CeilToInt(viewportHeight / stride) + 1 + PlayerPrefsRuntimeViewConstants.VirtualizationBufferRows;
            required = Mathf.Max(required, 1);

            for (int i = m_rowPool.Count; i < required; i++)
            {
                PlayerPrefsRuntimeRowView view = m_rowBuilder.Create(contentRoot, font, PlayerPrefsRuntimeViewConstants.BadgeColor);
                m_rowPool.Add(view);
            }
        }

        private void UpdateVisibleRows(bool force)
        {
            if (m_ui == null || !m_ui.IsVisible || m_currentEntries == null)
            {
                return;
            }

            RectTransform contentRoot = m_ui.ContentRoot;
            if (contentRoot == null)
            {
                return;
            }

            int entryCount = m_currentEntries.Count;
            float stride = PlayerPrefsRuntimeViewConstants.RowFixedHeight + PlayerPrefsRuntimeViewConstants.RowVirtualSpacing;
            float contentY = Mathf.Max(0f, contentRoot.anchoredPosition.y);

            int firstIndex = Mathf.FloorToInt((contentY - PlayerPrefsRuntimeViewConstants.ListPadding) / stride) - PlayerPrefsRuntimeViewConstants.VirtualizationBufferRows / 2;
            firstIndex = Mathf.Clamp(firstIndex, 0, Mathf.Max(0, entryCount - m_rowPool.Count));

            if (!force && firstIndex == m_firstVisibleIndex)
            {
                return;
            }

            m_firstVisibleIndex = firstIndex;

            for (int i = 0; i < m_rowPool.Count; i++)
            {
                PlayerPrefsRuntimeRowView rowView = m_rowPool[i];
                if (rowView == null)
                {
                    continue;
                }

                int entryIndex = firstIndex + i;
                if (entryIndex < entryCount)
                {
                    rowView.gameObject.SetActive(true);
                    PositionRow((RectTransform)rowView.transform, entryIndex, stride);

                    bool even = (entryIndex % 2) == 0;
                    m_rowBuilder.Update(rowView, m_currentEntries[entryIndex], even, PlayerPrefsRuntimeViewConstants.RowColorEven, PlayerPrefsRuntimeViewConstants.RowColorOdd, PlayerPrefsRuntimeViewConstants.BadgeColor, OnRowClicked);
                }
                else
                {
                    rowView.gameObject.SetActive(false);
                }
            }
        }

        private static void PositionRow(RectTransform rowTransform, int entryIndex, float stride)
        {
            rowTransform.sizeDelta = new Vector2(-2f * PlayerPrefsRuntimeViewConstants.ListPadding, PlayerPrefsRuntimeViewConstants.RowFixedHeight);
            rowTransform.anchoredPosition = new Vector2(0f, -(PlayerPrefsRuntimeViewConstants.ListPadding + entryIndex * stride));
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            UpdateVisibleRows(false);
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

            bool hasSearchFilter = !string.IsNullOrEmpty(m_searchFilter);
            bool hasTypeFilter = m_activeTypeFilters.Count > 0;

            if (!hasSearchFilter && !hasTypeFilter)
            {
                m_filteredEntries.AddRange(m_entriesCache);
                return m_filteredEntries;
            }

            string filter = m_searchFilter;
            int entryCount = m_entriesCache.Count;
            for (int i = 0; i < entryCount; i++)
            {
                PlayerPrefsRuntimeEntry entry = m_entriesCache[i];

                if (hasTypeFilter && !m_activeTypeFilters.Contains(entry.Type))
                {
                    continue;
                }

                if (hasSearchFilter && !ContainsFilter(entry.Name, filter) && !ContainsFilter(entry.Type, filter) && !ContainsFilter(entry.Value, filter))
                {
                    continue;
                }

                m_filteredEntries.Add(entry);
            }

            return m_filteredEntries;
        }

        private void OnTypeFilterToggled(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return;
            }

            bool active;
            if (m_activeTypeFilters.Contains(typeName))
            {
                m_activeTypeFilters.Remove(typeName);
                active = false;
            }
            else
            {
                m_activeTypeFilters.Add(typeName);
                active = true;
            }

            if (m_ui != null)
            {
                m_ui.SetTypeFilterActive(typeName, active);
            }

            m_resetScrollOnNextRefresh = true;
            RefreshRows();
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
            m_resetScrollOnNextRefresh = true;
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
            m_resetScrollOnNextRefresh = true;
            RefreshRows();
        }

        private void OnCloseButtonClicked()
        {
            Hide();
        }

        private void OnToolsToggleClicked()
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            m_toolsExpanded = !m_toolsExpanded;
            m_ui.SetToolsExpanded(m_toolsExpanded);

            // The scroll viewport grew or shrank with the header: re-dimension the recycled row
            // pool and re-place the visible rows for the new viewport height.
            RefreshRows();
        }

        private void OnDeleteAllClicked()
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            m_confirmDialog.Show(
                m_ui.PanelTransform,
                m_ui.DefaultFont,
                PlayerPrefsRuntimeViewConstants.DeleteAllConfirmTitle,
                PlayerPrefsRuntimeViewConstants.DeleteAllConfirmMessage,
                PlayerPrefsRuntimeViewConstants.DeleteLabel,
                OnDeleteAllConfirmed);
        }

        private void OnDeleteAllConfirmed()
        {
            if (!PlayerPrefsRuntimeWriter.DeleteAll())
            {
                ShowStatus("Delete All cancelled: a complete snapshot and backup could not be verified.");
                return;
            }

            m_entriesCache.Clear();
            RefreshRows();
        }

        private void OnAddClicked()
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            m_addEntryDialog.Show(m_ui.PanelTransform, m_ui.DefaultFont, KeyExistsInCache, OnEntryAdded);
        }

        private bool KeyExistsInCache(string key)
        {
            return m_entriesCache.Exists(e => string.Equals(e.Name, key, StringComparison.Ordinal));
        }

        private void OnEntryAdded(PlayerPrefsRuntimeEntry entry)
        {
            if (entry.Name == null)
            {
                return;
            }

            MergeEntryIntoCache(entry);
            RefreshRows();
        }

        private void MergeEntryIntoCache(PlayerPrefsRuntimeEntry entry)
        {
            int index = m_entriesCache.FindIndex(e => string.Equals(e.Name, entry.Name, StringComparison.Ordinal));
            if (index >= 0)
            {
                m_entriesCache[index] = entry;
            }
            else
            {
                m_entriesCache.Add(entry);
            }
        }

        private void OnExportClicked()
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            string json = PlayerPrefsRuntimeWriter.ExportToJson();
            GUIUtility.systemCopyBuffer = json;

            int entryCount = m_entriesCache.Count;
            string status;

            try
            {
                string timestamp = DateTime.Now.ToString(PlayerPrefsRuntimeViewConstants.ExportTimestampFormat, CultureInfo.InvariantCulture);
                string fileName = string.Format(CultureInfo.InvariantCulture, PlayerPrefsRuntimeViewConstants.ExportFileNameFormat, timestamp);
                string path = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(path, json);
                Debug.Log($"[PlayerPrefsRuntime] Exported PlayerPrefs to {path}");
                status = string.Format(PlayerPrefsRuntimeViewConstants.ExportStatusFormat, entryCount, fileName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayerPrefsRuntime] Export file write failed: {exception.Message}");
                status = string.Format(PlayerPrefsRuntimeViewConstants.ExportClipboardOnlyStatusFormat, entryCount);
            }

            ShowStatus(status);
        }

        private void OnImportClicked()
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            m_importDialog.Show(m_ui.PanelTransform, m_ui.DefaultFont, OnImportCompleted);
        }

        private void OnImportCompleted(PlayerPrefsRuntimeImportResult result)
        {
            // Merge the applied entries in memory instead of re-fetching: the platform
            // fetchers (registry/plist) may lag behind writes that just happened.
            foreach (PlayerPrefsRuntimeParsedEntry applied in result.AppliedEntries)
            {
                MergeEntryIntoCache(new PlayerPrefsRuntimeEntry(applied.Key, applied.Value));
            }

            RefreshRows();
        }

        private void ShowStatus(string message)
        {
            if (m_ui == null || !m_ui.IsVisible)
            {
                return;
            }

            m_ui.UpdateSubtitleText(message);

            if (m_statusMessageCoroutine != null)
            {
                m_ui.StopCoroutine(m_statusMessageCoroutine);
            }

            m_statusMessageCoroutine = m_ui.StartCoroutine(RestoreSubtitleRoutine());
        }

        private IEnumerator RestoreSubtitleRoutine()
        {
            yield return new WaitForSecondsRealtime(PlayerPrefsRuntimeViewConstants.StatusMessageDuration);
            m_statusMessageCoroutine = null;
            UpdateHeader(m_filteredEntries.Count);
        }

        private void OnSortModeButtonClicked()
        {
            m_currentSortMode = m_currentSortMode == SortMode.Name ? SortMode.Type : SortMode.Name;
            m_resetScrollOnNextRefresh = true;
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
