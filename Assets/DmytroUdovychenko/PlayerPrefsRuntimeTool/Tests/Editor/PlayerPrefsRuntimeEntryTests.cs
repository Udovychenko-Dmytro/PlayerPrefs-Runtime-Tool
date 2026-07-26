// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Globalization;
using NUnit.Framework;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Pins the normalization and size/formatting behavior of the <see cref="PlayerPrefsRuntimeEntry"/>
    /// value model used throughout the viewer, editor window and entry dialog.
    /// </summary>
    public class PlayerPrefsRuntimeEntryTests
    {
        // ---- Constructor: type + value normalization ----

        [Test]
        public void Constructor_IntValue_TypeIsInt32AndValueStringified()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("score", 42);

            Assert.That(entry.Name, Is.EqualTo("score"));
            Assert.That(entry.Type, Is.EqualTo("Int32"));
            Assert.That(entry.Value, Is.EqualTo("42"));
        }

        [Test]
        public void Constructor_FloatValue_TypeIsSingle()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("volume", 3.5f);

            Assert.That(entry.Type, Is.EqualTo("Single"));
            Assert.That(entry.Value, Is.EqualTo("3.5"));
        }

        [Test]
        public void Constructor_FloatValue_UsesInvariantCulture()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("volume", 3.5f);

                Assert.That(entry.Value, Is.EqualTo("3.5"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void Constructor_FloatValue_RoundTripsWithoutPrecisionLoss()
        {
            const float original = 1.2345678f;
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("volume", original);

            bool parsed = float.TryParse(
                entry.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float roundTripped);

            Assert.That(parsed, Is.True);
            Assert.That(roundTripped, Is.EqualTo(original));
        }

        [Test]
        public void Constructor_StringValue_TypeIsStringAndValuePreserved()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("name", "Alice");

            Assert.That(entry.Type, Is.EqualTo("String"));
            Assert.That(entry.Value, Is.EqualTo("Alice"));
        }

        [Test]
        public void Constructor_NullName_FallsBackToUnnamed()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry(null, 1);

            Assert.That(entry.Name, Is.EqualTo("(Unnamed)"));
        }

        [Test]
        public void Constructor_EmptyName_FallsBackToUnnamed()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry(string.Empty, 1);

            Assert.That(entry.Name, Is.EqualTo("(Unnamed)"));
        }

        [Test]
        public void Constructor_NullValue_TypeAndValueMarkedNull()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("key", null);

            Assert.That(entry.Type, Is.EqualTo("null"));
            Assert.That(entry.Value, Is.EqualTo("(null)"));
        }

        // ---- EstimateValueSizeBytes ----

        [Test]
        public void EstimateValueSizeBytes_Int_ReturnsFourBytes()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", 123456);

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(4L));
        }

        [Test]
        public void EstimateValueSizeBytes_Float_ReturnsFourBytes()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", 1.25f);

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(4L));
        }

        [Test]
        public void EstimateValueSizeBytes_AsciiString_ReturnsCharCount()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", "test");

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(4L));
        }

        [Test]
        public void EstimateValueSizeBytes_MultiByteString_ReturnsUtf8ByteCount()
        {
            // Two CJK code points, 3 UTF-8 bytes each = 6 bytes.
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", "日本");

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(6L));
        }

        [Test]
        public void EstimateValueSizeBytes_EmptyString_ReturnsZero()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", string.Empty);

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(0L));
        }

        [Test]
        public void EstimateValueSizeBytes_Long_ReturnsEightBytes()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", 123L);

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(8L));
        }

        [Test]
        public void EstimateValueSizeBytes_ByteArray_ReturnsRawLength()
        {
            PlayerPrefsRuntimeEntry entry = new PlayerPrefsRuntimeEntry("k", new byte[] { 1, 2, 3 });

            Assert.That(entry.EstimateValueSizeBytes(), Is.EqualTo(3L));
        }

        // ---- FormatByteSize ----

        [Test]
        public void FormatByteSize_Zero_Bytes()
        {
            Assert.That(PlayerPrefsRuntimeEntry.FormatByteSize(0L), Is.EqualTo("0 B"));
        }

        [Test]
        public void FormatByteSize_UnderOneKilobyte_Bytes()
        {
            Assert.That(PlayerPrefsRuntimeEntry.FormatByteSize(1023L), Is.EqualTo("1023 B"));
        }

        [Test]
        public void FormatByteSize_ExactlyOneKilobyte_Kilobytes()
        {
            Assert.That(PlayerPrefsRuntimeEntry.FormatByteSize(1024L), Is.EqualTo("1 KB"));
        }

        [Test]
        public void FormatByteSize_OneAndAHalfKilobytes_OneDecimal()
        {
            Assert.That(PlayerPrefsRuntimeEntry.FormatByteSize(1536L), Is.EqualTo("1.5 KB"));
        }

        [Test]
        public void FormatByteSize_ExactlyOneMegabyte_Megabytes()
        {
            Assert.That(PlayerPrefsRuntimeEntry.FormatByteSize(1024L * 1024L), Is.EqualTo("1 MB"));
        }

        [Test]
        public void FormatByteSize_OneAndAHalfMegabytes_TwoDecimalsMax()
        {
            Assert.That(PlayerPrefsRuntimeEntry.FormatByteSize(1024L * 1024L + 512L * 1024L), Is.EqualTo("1.5 MB"));
        }
    }
}
#endif
