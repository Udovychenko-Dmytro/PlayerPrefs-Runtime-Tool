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
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Exercises the central mutation core <see cref="PlayerPrefsRuntimeWriter"/> against real
    /// PlayerPrefs: typed writes, deletion, key validation and the <see cref="PlayerPrefsRuntime.OnEntryChanged"/>
    /// notification contract. All written keys use a unique prefix and are removed in TearDown.
    /// Import-created backup files are tracked and removed by the test. DeleteAll cancellation
    /// and verified-empty guards are tested without invoking the global destructive path.
    /// </summary>
    public class PlayerPrefsRuntimeWriterTests
    {
        private readonly string m_keyPrefix = "__pprt_writer_test_" + Guid.NewGuid().ToString("N") + "_";
        private readonly HashSet<string> m_touchedKeys = new HashSet<string>();

        /// <summary>Returns a test-scoped key and remembers it for cleanup.</summary>
        private string Key(string suffix)
        {
            string key = m_keyPrefix + suffix;
            m_touchedKeys.Add(key);
            return key;
        }

        /// <summary>Runs <paramref name="action"/> while recording every OnEntryChanged key it raises.</summary>
        private static List<string> CaptureEvents(Action action)
        {
            List<string> received = new List<string>();
            Action<string> handler = key => received.Add(key);

            PlayerPrefsRuntime.OnEntryChanged += handler;
            try
            {
                action();
            }
            finally
            {
                PlayerPrefsRuntime.OnEntryChanged -= handler;
            }

            return received;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string key in m_touchedKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            PlayerPrefs.Save();
            m_touchedKeys.Clear();
        }

        // ---- Typed writes ----

        [Test]
        public void SetInt_StoresValue()
        {
            string key = Key("int");

            PlayerPrefsRuntimeWriter.SetInt(key, 123);

            Assert.That(PlayerPrefs.HasKey(key), Is.True);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(123));
        }

        [Test]
        public void SetFloat_StoresValue()
        {
            string key = Key("float");

            PlayerPrefsRuntimeWriter.SetFloat(key, 1.5f);

            Assert.That(PlayerPrefs.HasKey(key), Is.True);
            Assert.That(PlayerPrefs.GetFloat(key), Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void SetString_StoresValue()
        {
            string key = Key("string");

            PlayerPrefsRuntimeWriter.SetString(key, "hello");

            Assert.That(PlayerPrefs.HasKey(key), Is.True);
            Assert.That(PlayerPrefs.GetString(key), Is.EqualTo("hello"));
        }

        [Test]
        public void SetString_NullValue_StoredAsEmptyString()
        {
            string key = Key("null_string");

            PlayerPrefsRuntimeWriter.SetString(key, null);

            Assert.That(PlayerPrefs.HasKey(key), Is.True);
            // GetString returns the supplied fallback only when the key is absent; an empty
            // string proves the null was normalized and actually written.
            Assert.That(PlayerPrefs.GetString(key, "FALLBACK"), Is.EqualTo(string.Empty));
        }

        [Test]
        public void DeleteKey_RemovesExistingKey()
        {
            string key = Key("to_delete");
            PlayerPrefsRuntimeWriter.SetInt(key, 1);
            Assume.That(PlayerPrefs.HasKey(key), Is.True);

            PlayerPrefsRuntimeWriter.DeleteKey(key);

            Assert.That(PlayerPrefs.HasKey(key), Is.False);
        }

        // ---- OnEntryChanged notifications ----

        [Test]
        public void SetInt_RaisesOnEntryChangedWithKey()
        {
            string key = Key("evt_set");

            List<string> events = CaptureEvents(() => PlayerPrefsRuntimeWriter.SetInt(key, 7));

            Assert.That(events, Is.EqualTo(new[] { key }));
        }

        [Test]
        public void DeleteKey_RaisesOnEntryChangedWithKey()
        {
            string key = Key("evt_delete");
            PlayerPrefsRuntimeWriter.SetInt(key, 1);

            List<string> events = CaptureEvents(() => PlayerPrefsRuntimeWriter.DeleteKey(key));

            Assert.That(events, Is.EqualTo(new[] { key }));
        }

        // ---- Key validation ----

        [TestCase(null)]
        [TestCase("")]
        public void SetInt_NullOrEmptyKey_DoesNotWriteOrNotify(string key)
        {
            List<string> events = CaptureEvents(() => PlayerPrefsRuntimeWriter.SetInt(key, 5));

            Assert.That(events, Is.Empty);
            if (!string.IsNullOrEmpty(key))
            {
                Assert.That(PlayerPrefs.HasKey(key), Is.False);
            }
        }

        [Test]
        public void IsValidKey_LiteralUnnamedLabel_ReturnsTrue()
        {
            string sentinel = PlayerPrefsRuntimeRegistryValueDecoder.UnnamedKey;

            Assert.That(PlayerPrefsRuntimeWriter.IsValidKey(sentinel), Is.True);
        }

        // ---- Destructive-operation safety ----

        [Test]
        public void DeleteAll_IncompleteSnapshot_CancelsWithoutDeleting()
        {
            string key = Key("delete_all_guard");
            PlayerPrefsRuntimeWriter.SetInt(key, 17);
            Dictionary<string, object> partialSnapshot = new Dictionary<string, object>
            {
                { key, 17 }
            };

            bool deleted = PlayerPrefsRuntimeWriter.DeleteAll(partialSnapshot, false);

            Assert.That(deleted, Is.False);
            Assert.That(PlayerPrefs.HasKey(key), Is.True);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(17));
        }

        [Test]
        public void DeleteAll_CompleteEmptySnapshot_SucceedsWithoutMutation()
        {
            string key = Key("unrelated");
            PlayerPrefsRuntimeWriter.SetInt(key, 23);

            bool deleted = PlayerPrefsRuntimeWriter.DeleteAll(
                new Dictionary<string, object>(),
                true);

            Assert.That(deleted, Is.True);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(23));
        }

        [Test]
        public void DeleteAll_UnsupportedSnapshotValue_CancelsWithoutDeleting()
        {
            string key = Key("delete_all_unsupported");
            PlayerPrefsRuntimeWriter.SetInt(key, 31);
            Dictionary<string, object> snapshot = new Dictionary<string, object>
            {
                { key, new byte[] { 1, 2, 3 } }
            };

            bool deleted = PlayerPrefsRuntimeWriter.DeleteAll(snapshot, true);

            Assert.That(deleted, Is.False);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(31));
        }

        // ---- Handler isolation ----

        [Test]
        public void Set_WhenHandlerThrows_WriteSucceedsAndExceptionIsSwallowed()
        {
            string key = Key("iso");
            Action<string> throwingHandler = _ => throw new InvalidOperationException("boom");

            PlayerPrefsRuntime.OnEntryChanged += throwingHandler;
            try
            {
                Assert.DoesNotThrow(() => PlayerPrefsRuntimeWriter.SetInt(key, 99));
            }
            finally
            {
                PlayerPrefsRuntime.OnEntryChanged -= throwingHandler;
            }

            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(99));
        }

        [Test]
        public void Set_WhenFirstHandlerThrows_LaterHandlersStillRun()
        {
            string key = Key("iso_multiple");
            List<string> received = new List<string>();
            Action<string> throwingHandler = _ => throw new InvalidOperationException("boom");
            Action<string> recordingHandler = changedKey => received.Add(changedKey);

            PlayerPrefsRuntime.OnEntryChanged += throwingHandler;
            PlayerPrefsRuntime.OnEntryChanged += recordingHandler;
            try
            {
                Assert.DoesNotThrow(() => PlayerPrefsRuntimeWriter.SetInt(key, 99));
            }
            finally
            {
                PlayerPrefsRuntime.OnEntryChanged -= throwingHandler;
                PlayerPrefsRuntime.OnEntryChanged -= recordingHandler;
            }

            Assert.That(received, Is.EqualTo(new[] { key }));
        }

        // ---- Import ----

        [Test]
        public void ImportFromJson_ValidTypedEntries_AppliesAllAndNotifiesPerKey()
        {
            string intKey = Key("imp_int");
            string floatKey = Key("imp_float");
            string stringKey = Key("imp_string");

            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { intKey, 42 },
                { floatKey, 1.5f },
                { stringKey, "hello" }
            };
            string json = PlayerPrefsRuntimeJsonSerializer.Serialize(source);
            string backupPath = null;

            try
            {
                PlayerPrefsRuntimeImportResult result = null;
                List<string> events = CaptureEvents(() =>
                    result = PlayerPrefsRuntimeWriter.ImportFromJson(json, out backupPath));

                Assert.That(result, Is.Not.Null);
                Assert.That(backupPath, Is.Not.Null.And.Not.Empty);
                Assert.That(File.Exists(backupPath), Is.True);
                Assert.That(result.ImportedCount, Is.EqualTo(3));
                Assert.That(result.HasErrors, Is.False);
                Assert.That(PlayerPrefs.GetInt(intKey), Is.EqualTo(42));
                Assert.That(PlayerPrefs.GetFloat(floatKey), Is.EqualTo(1.5f).Within(0.0001f));
                Assert.That(PlayerPrefs.GetString(stringKey), Is.EqualTo("hello"));
                Assert.That(events, Is.EquivalentTo(new[] { intKey, floatKey, stringKey }));
            }
            finally
            {
                if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }
    }
}
#endif
