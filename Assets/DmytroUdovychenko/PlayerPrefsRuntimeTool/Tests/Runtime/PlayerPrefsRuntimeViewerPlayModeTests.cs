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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// PlayMode smoke tests for <see cref="PlayerPrefsRuntimeViewer"/>: they build the real canvas
    /// in a running player loop and assert the lifecycle (show → visible + canvas exists → hide →
    /// gone). This catches NullReferenceExceptions and broken layout/build chains in the viewer and
    /// its builder that the EditMode logic tests cannot reach.
    /// </summary>
    public class PlayerPrefsRuntimeViewerPlayModeTests
    {
        private const string CanvasName = "PlayerPrefsRuntimeCanvas";

        private PlayerPrefsRuntimeViewer m_viewer;

        private static List<PlayerPrefsRuntimeEntry> SampleEntries()
        {
            return new List<PlayerPrefsRuntimeEntry>
            {
                new PlayerPrefsRuntimeEntry("score", 42),
                new PlayerPrefsRuntimeEntry("volume", 0.5f),
                new PlayerPrefsRuntimeEntry("player_name", "Alice")
            };
        }

        private static GameObject FindDescendant(GameObject root, string name)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == name)
                {
                    return descendants[i].gameObject;
                }
            }

            return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_viewer != null)
            {
                m_viewer.Hide();
                m_viewer = null;
            }

            // Force-remove any canvas the viewer may have left pending destruction so the next
            // test starts from a clean scene (Hide() uses deferred Destroy).
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (canvas != null && canvas.gameObject.name == CanvasName)
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShowEntries_MakesViewerVisibleAndBuildsCanvas()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();

            m_viewer.ShowEntries(SampleEntries());
            yield return null; // let the canvas build for a frame

            Assert.That(m_viewer.IsVisible, Is.True);
            Assert.That(GameObject.Find(CanvasName), Is.Not.Null, "Viewer canvas should exist in the scene.");
        }

        [UnityTest]
        public IEnumerator Hide_DestroysCanvasAndClearsVisibility()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;
            Assume.That(m_viewer.IsVisible, Is.True);

            m_viewer.Hide();
            yield return null; // deferred Destroy resolves at end of frame

            Assert.That(m_viewer.IsVisible, Is.False);
            Assert.That(GameObject.Find(CanvasName), Is.Null, "Viewer canvas should be gone after Hide.");
            m_viewer = null; // already hidden; skip the TearDown Hide
        }

        [UnityTest]
        public IEnumerator ShowEntries_EmptyList_StaysVisibleWithoutErrors()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();

            m_viewer.ShowEntries(new List<PlayerPrefsRuntimeEntry>());
            yield return null;

            Assert.That(m_viewer.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator ShowEntries_CalledTwice_RefreshesWithoutErrors()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();

            m_viewer.ShowEntries(SampleEntries());
            yield return null;
            m_viewer.ShowEntries(SampleEntries()); // refresh the already-open viewer
            yield return null;

            Assert.That(m_viewer.IsVisible, Is.True);
            Assert.That(GameObject.Find(CanvasName), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ShowEntries_AfterExternalCanvasDestroy_RebuildsVisibleRows()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject originalCanvas = GameObject.Find(CanvasName);
            Assume.That(originalCanvas, Is.Not.Null);
            UnityEngine.Object.Destroy(originalCanvas);
            yield return null;

            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject rebuiltCanvas = GameObject.Find(CanvasName);
            Assert.That(rebuiltCanvas, Is.Not.Null);

            Transform[] descendants = rebuiltCanvas.GetComponentsInChildren<Transform>(true);
            int activeRows = 0;
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == PlayerPrefsRuntimeViewConstants.RowName
                    && descendants[i].gameObject.activeSelf)
                {
                    activeRows++;
                }
            }

            Assert.That(activeRows, Is.GreaterThan(0), "The rebuilt Canvas should contain active entry rows.");
        }

        [UnityTest]
        public IEnumerator Header_TitleAndSubtitleBandsDoNotOverlap()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject canvas = GameObject.Find(CanvasName);
            Assume.That(canvas, Is.Not.Null);

            GameObject title = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.TitleName);
            GameObject subtitle = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.SubtitleName);
            Assert.That(title, Is.Not.Null);
            Assert.That(subtitle, Is.Not.Null);

            Canvas.ForceUpdateCanvases();

            Vector3[] titleCorners = new Vector3[4];
            Vector3[] subtitleCorners = new Vector3[4];
            ((RectTransform)title.transform).GetWorldCorners(titleCorners);
            ((RectTransform)subtitle.transform).GetWorldCorners(subtitleCorners);

            // corner 0 = bottom-left, corner 2 = top-right
            float subtitleRight = subtitleCorners[2].x;
            float titleLeft = titleCorners[0].x;

            Assert.That(
                subtitleRight,
                Is.LessThanOrEqualTo(titleLeft),
                "The subtitle band must end before the title band starts, otherwise long text overlaps.");

            Text titleText = title.GetComponent<Text>();
            Text subtitleText = subtitle.GetComponent<Text>();
            Assert.That(titleText.resizeTextForBestFit, Is.True, "The title must shrink instead of wrapping out of its band.");
            Assert.That(subtitleText.resizeTextForBestFit, Is.True, "The subtitle must shrink instead of wrapping out of its band.");
        }

        [UnityTest]
        public IEnumerator ToolsToggle_StartsCollapsedAndTogglesActionAndFilterBars()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject canvas = GameObject.Find(CanvasName);
            Assume.That(canvas, Is.Not.Null);

            GameObject toggleButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.ToolsToggleButtonName);
            GameObject actionBar = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.ActionBarName);
            GameObject filterBar = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.FilterBarName);
            GameObject header = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.HeaderName);

            Assert.That(toggleButton, Is.Not.Null);
            Assert.That(actionBar, Is.Not.Null);
            Assert.That(filterBar, Is.Not.Null);
            Assert.That(header, Is.Not.Null);

            RectTransform headerRect = (RectTransform)header.transform;

            Assert.That(actionBar.activeSelf, Is.False, "The action bar should be hidden by default.");
            Assert.That(filterBar.activeSelf, Is.False, "The filter bar should be hidden by default.");
            Assert.That(
                headerRect.sizeDelta.y,
                Is.EqualTo(PlayerPrefsRuntimeViewConstants.HeaderHeightCollapsed).Within(0.01f));

            toggleButton.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(actionBar.activeSelf, Is.True);
            Assert.That(filterBar.activeSelf, Is.True);
            Assert.That(
                headerRect.sizeDelta.y,
                Is.EqualTo(PlayerPrefsRuntimeViewConstants.HeaderHeightExpanded).Within(0.01f));

            toggleButton.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(actionBar.activeSelf, Is.False);
            Assert.That(filterBar.activeSelf, Is.False);
            Assert.That(
                headerRect.sizeDelta.y,
                Is.EqualTo(PlayerPrefsRuntimeViewConstants.HeaderHeightCollapsed).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator EntryDialog_ReopenAfterEditing_StartsInViewMode()
        {
            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject canvas = GameObject.Find(CanvasName);
            Assume.That(canvas, Is.Not.Null);

            PlayerPrefsRuntimeEntryDialog dialog = new PlayerPrefsRuntimeEntryDialog();
            dialog.Show(canvas.transform, new PlayerPrefsRuntimeEntry("score", 42), null);
            yield return null;

            GameObject editButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.EditButtonName);
            Assume.That(editButton, Is.Not.Null);
            editButton.GetComponent<Button>().onClick.Invoke();

            dialog.Close();
            yield return null;
            dialog.Show(canvas.transform, new PlayerPrefsRuntimeEntry("name", "Alice"), null);
            yield return null;

            GameObject reopenedEditButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.EditButtonName);
            GameObject saveButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.SaveButtonName);
            GameObject valueInput = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.ValueComponentName);
            GameObject valueText = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.ValueTextName);

            Assert.That(reopenedEditButton, Is.Not.Null);
            Assert.That(reopenedEditButton.activeSelf, Is.True);
            Assert.That(saveButton, Is.Not.Null);
            Assert.That(saveButton.activeSelf, Is.False);
            Assert.That(valueInput, Is.Not.Null);
            Assert.That(valueInput.activeSelf, Is.False);
            Assert.That(valueText, Is.Not.Null);
            Assert.That(valueText.activeSelf, Is.True);

            reopenedEditButton.GetComponent<Button>().onClick.Invoke();
            Assert.That(reopenedEditButton.activeSelf, Is.False);
            Assert.That(saveButton.activeSelf, Is.True);
            Assert.That(valueInput.activeSelf, Is.True);
            Assert.That(valueText.activeSelf, Is.False);

            dialog.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EntryDialog_CopyAllAfterSave_UsesCurrentNormalizedValue()
        {
            string key = "__pprt_dialog_copy_test_" + Guid.NewGuid().ToString("N");
            string originalClipboard = GUIUtility.systemCopyBuffer;

            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject canvas = GameObject.Find(CanvasName);
            Assume.That(canvas, Is.Not.Null);

            PlayerPrefsRuntimeEntryDialog dialog = new PlayerPrefsRuntimeEntryDialog();
            try
            {
                dialog.Show(canvas.transform, new PlayerPrefsRuntimeEntry(key, 1.5f), null);
                yield return null;

                GameObject editButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.EditButtonName);
                GameObject saveButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.SaveButtonName);
                GameObject copyButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.CopyButtonName);
                GameObject valueInput = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.ValueComponentName);

                Assert.That(editButton, Is.Not.Null);
                Assert.That(saveButton, Is.Not.Null);
                Assert.That(copyButton, Is.Not.Null);
                Assert.That(valueInput, Is.Not.Null);

                editButton.GetComponent<Button>().onClick.Invoke();
                valueInput.GetComponent<InputField>().text = "2.5000";
                saveButton.GetComponent<Button>().onClick.Invoke();
                copyButton.GetComponent<Button>().onClick.Invoke();

                Assert.That(PlayerPrefs.GetFloat(key), Is.EqualTo(2.5f).Within(0.0001f));
                Assert.That(
                    GUIUtility.systemCopyBuffer,
                    Is.EqualTo($"Key: {key}\nType: Single\nValue: 2.5"));
            }
            finally
            {
                dialog.Close();
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                GUIUtility.systemCopyBuffer = originalClipboard;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EntryDialog_Save_WhenChangeHandlerClosesDialog_DoesNotThrow()
        {
            string key = "__pprt_dialog_reentrant_test_" + Guid.NewGuid().ToString("N");
            m_viewer = new PlayerPrefsRuntimeViewer();
            m_viewer.ShowEntries(SampleEntries());
            yield return null;

            GameObject canvas = GameObject.Find(CanvasName);
            Assume.That(canvas, Is.Not.Null);

            PlayerPrefsRuntimeEntryDialog dialog = new PlayerPrefsRuntimeEntryDialog();
            Action<string> closeHandler = _ => dialog.Close();
            try
            {
                dialog.Show(canvas.transform, new PlayerPrefsRuntimeEntry(key, 1), null);
                yield return null;

                GameObject editButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.EditButtonName);
                GameObject saveButton = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.SaveButtonName);
                GameObject valueInput = FindDescendant(canvas, PlayerPrefsRuntimeViewConstants.ValueComponentName);
                Assert.That(editButton, Is.Not.Null);
                Assert.That(saveButton, Is.Not.Null);
                Assert.That(valueInput, Is.Not.Null);

                editButton.GetComponent<Button>().onClick.Invoke();
                valueInput.GetComponent<InputField>().text = "11";

                PlayerPrefsRuntime.OnEntryChanged += closeHandler;
                Assert.DoesNotThrow(() => saveButton.GetComponent<Button>().onClick.Invoke());
                Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(11));
            }
            finally
            {
                PlayerPrefsRuntime.OnEntryChanged -= closeHandler;
                dialog.Close();
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }

            yield return null;
        }
    }
}
#endif
