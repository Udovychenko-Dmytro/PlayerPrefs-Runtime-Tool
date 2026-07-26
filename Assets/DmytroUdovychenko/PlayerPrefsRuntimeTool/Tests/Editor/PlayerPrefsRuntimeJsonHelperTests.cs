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
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Pins the cross-platform value normalization behavior of <see cref="PlayerPrefsRuntimeJsonHelper"/>.
    /// </summary>
    public class PlayerPrefsRuntimeJsonHelperTests
    {
        [Test]
        public void NormalizeValue_LongWithinIntRange_ReturnsInt()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(5L);

            Assert.That(result, Is.TypeOf<int>());
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void NormalizeValue_LongOutsideIntRange_StaysLong()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(long.MaxValue);

            Assert.That(result, Is.TypeOf<long>());
            Assert.That(result, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void NormalizeValue_Double_ReturnsFloat()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(3.5d);

            Assert.That(result, Is.TypeOf<float>());
            Assert.That(result, Is.EqualTo(3.5f));
        }

        [Test]
        public void NormalizeValue_JValue_UnwrapsAndNormalizes()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(new JValue(7L));

            Assert.That(result, Is.TypeOf<int>());
            Assert.That(result, Is.EqualTo(7));
        }

        [Test]
        public void NormalizeValue_Base64BinaryPrefix_DecodesToUtf8String()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue("$base64Binary;dGVzdA==");

            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void NormalizeValue_Base64BinaryPrefixWithInvalidPayload_ReturnsOriginal()
        {
            string original = "$base64Binary;%%%";

            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(original);

            Assert.That(result, Is.EqualTo(original));
        }

        [Test]
        public void NormalizeValue_PlainStringThatLooksLikeBase64_IsPreserved()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue("dGVzdA==");

            Assert.That(result, Is.EqualTo("dGVzdA=="));
        }

        [Test]
        public void NormalizeValue_AnotherPlainBase64String_IsPreserved()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue("abcd");

            Assert.That(result, Is.EqualTo("abcd"));
        }

        [Test]
        public void NormalizeValue_StringLengthNotMultipleOfFour_ReturnsOriginal()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue("Hello");

            Assert.That(result, Is.EqualTo("Hello"));
        }

        [Test]
        public void NormalizeValue_EmptyString_ReturnsOriginal()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(string.Empty);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void NormalizeValue_Null_ReturnsNull()
        {
            object result = PlayerPrefsRuntimeJsonHelper.NormalizeValue(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void NormalizeDictionary_Null_ReturnsEmptyDictionary()
        {
            Dictionary<string, object> result = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void NormalizeDictionary_NormalizesValuesInPlace()
        {
            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { "intLike", 5L },
                { "floatLike", 2.5d },
                { "text", "Hello" }
            };

            Dictionary<string, object> result = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(source);

            Assert.That(result, Is.SameAs(source));
            Assert.That(result["intLike"], Is.TypeOf<int>());
            Assert.That(result["floatLike"], Is.TypeOf<float>());
            Assert.That(result["text"], Is.EqualTo("Hello"));
        }

        [Test]
        public void NormalizeDictionary_EmptyKey_RemovesEntry()
        {
            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { string.Empty, 1L },
                { "valid", 2L }
            };

            Dictionary<string, object> result = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(source);

            Assert.That(result.ContainsKey(string.Empty), Is.False);
            Assert.That(result["valid"], Is.EqualTo(2));
        }

        [Test]
        public void NormalizeDictionary_EmptyKey_ReportsIncomplete()
        {
            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { string.Empty, 1L },
                { "valid", 2L }
            };

            Dictionary<string, object> result = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(
                source,
                out bool isComplete);

            Assert.That(isComplete, Is.False);
            Assert.That(result.ContainsKey(string.Empty), Is.False);
            Assert.That(result["valid"], Is.EqualTo(2));
        }

        [Test]
        public void NormalizeDictionary_ValidSource_ReportsComplete()
        {
            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { "score", 1L }
            };

            Dictionary<string, object> result = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(
                source,
                out bool isComplete);

            Assert.That(isComplete, Is.True);
            Assert.That(result["score"], Is.EqualTo(1));
        }

        [Test]
        public void NormalizeDictionary_ExplicitBinaryMarker_ReportsIncomplete()
        {
            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { "blob", "$base64Binary;dGVzdA==" }
            };

            Dictionary<string, object> result = PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(
                source,
                out bool isComplete);

            Assert.That(isComplete, Is.False);
            Assert.That(result["blob"], Is.EqualTo("test"));
        }

        [Test]
        public void NormalizeDictionary_LossyDoubleConversion_ReportsIncomplete()
        {
            Dictionary<string, object> source = new Dictionary<string, object>
            {
                { "precise", 0.1d }
            };

            PlayerPrefsRuntimeJsonHelper.NormalizeDictionary(source, out bool isComplete);

            Assert.That(isComplete, Is.False);
            Assert.That(source["precise"], Is.TypeOf<float>());
        }

#if UNITY_EDITOR_OSX
        [Test]
        public void MacEditorXmlPlist_DataValue_ReportsIncomplete()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "pprt_data_value_" + Guid.NewGuid().ToString("N") + ".plist");
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<plist version=\"1.0\"><dict><key>blob</key><data>aGVsbG8=</data></dict></plist>";

            try
            {
                File.WriteAllText(path, xml);

                bool isComplete = PlayerPrefsRuntimeFetcherMacOSEditor.TryParseXmlPlist(
                    path,
                    out Dictionary<string, object> prefs);

                Assert.That(isComplete, Is.False);
                Assert.That(prefs["blob"], Is.EqualTo("hello"));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void MacEditorXmlPlist_SpecialRealValues_ReportComplete()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "pprt_special_reals_" + Guid.NewGuid().ToString("N") + ".plist");
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<plist version=\"1.0\"><dict>"
                + "<key>nan</key><real>nan</real>"
                + "<key>positive</key><real>+infinity</real>"
                + "<key>negative</key><real>-infinity</real>"
                + "</dict></plist>";

            try
            {
                File.WriteAllText(path, xml);

                bool isComplete = PlayerPrefsRuntimeFetcherMacOSEditor.TryParseXmlPlist(
                    path,
                    out Dictionary<string, object> prefs);

                Assert.That(isComplete, Is.True);
                Assert.That(float.IsNaN((float)prefs["nan"]), Is.True);
                Assert.That((float)prefs["positive"], Is.EqualTo(float.PositiveInfinity));
                Assert.That((float)prefs["negative"], Is.EqualTo(float.NegativeInfinity));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void MacEditorXmlPlist_LossyDoubleValue_ReportsIncomplete()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "pprt_lossy_real_" + Guid.NewGuid().ToString("N") + ".plist");
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<plist version=\"1.0\"><dict><key>precise</key><real>0.1</real></dict></plist>";

            try
            {
                File.WriteAllText(path, xml);

                bool isComplete = PlayerPrefsRuntimeFetcherMacOSEditor.TryParseXmlPlist(
                    path,
                    out Dictionary<string, object> prefs);

                Assert.That(isComplete, Is.False);
                Assert.That(prefs["precise"], Is.TypeOf<float>());
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
#endif
    }
}
#endif
