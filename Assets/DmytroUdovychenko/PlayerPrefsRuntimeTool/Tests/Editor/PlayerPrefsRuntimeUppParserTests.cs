// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Tests the WebGL "UPP" binary PlayerPrefs parser against synthetic fixtures
    /// built to the codec used by Unity's WebGL runtime.
    /// </summary>
    public class PlayerPrefsRuntimeUppParserTests
    {
        private const byte LongStringMarker = 0x80;
        private const byte FloatMarker = 0xFD;
        private const byte IntMarker = 0xFE;

        [Test]
        public void Parse_FullDocument_ReturnsAllTypedEntries()
        {
            string longValue = new string('x', 300);
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "score", 42);
                WriteFloatEntry(writer, "volume", 0.75f);
                WriteShortStringEntry(writer, "name", "Player");
                WriteLongStringEntry(writer, "blob", longValue);
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs.Count, Is.EqualTo(4));
            Assert.That(prefs["score"], Is.TypeOf<int>());
            Assert.That(prefs["score"], Is.EqualTo(42));
            Assert.That(prefs["volume"], Is.TypeOf<float>());
            Assert.That(prefs["volume"], Is.EqualTo(0.75f));
            Assert.That(prefs["name"], Is.EqualTo("Player"));
            Assert.That(prefs["blob"], Is.EqualTo(longValue));
        }

        [Test]
        public void TryParse_FullDocument_ReportsComplete()
        {
            byte[] data = BuildDocument(writer => WriteIntEntry(writer, "score", 42));

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                data,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.True);
            Assert.That(prefs["score"], Is.EqualTo(42));
        }

        [Test]
        public void Parse_WrongMagic_ReturnsEmpty()
        {
            byte[] data = BuildDocument(writer => WriteIntEntry(writer, "score", 1));
            byte[] corrupted = (byte[])data.Clone();
            corrupted[0] = (byte)'X';

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(corrupted);

            Assert.That(prefs, Is.Empty);
        }

        [Test]
        public void Parse_UnexpectedVersion_StillParsesEntries()
        {
            byte[] data = BuildDocument(writer => WriteIntEntry(writer, "score", 7), version: 0x20000);

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs["score"], Is.EqualTo(7));
        }

        [Test]
        public void TryParse_UnexpectedVersion_ReportsIncomplete()
        {
            byte[] data = BuildDocument(writer => WriteIntEntry(writer, "score", 7), version: 0x20000);

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                data,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.False);
            Assert.That(prefs["score"], Is.EqualTo(7));
        }

        [Test]
        public void Parse_NullOrShortInput_ReturnsEmpty()
        {
            Assert.That(PlayerPrefsRuntimeUppParser.Parse(null), Is.Empty);
            Assert.That(PlayerPrefsRuntimeUppParser.Parse(new byte[0]), Is.Empty);
            Assert.That(PlayerPrefsRuntimeUppParser.Parse(new byte[7]), Is.Empty);
        }

        [Test]
        public void Parse_TruncatedEntry_ReturnsEntriesParsedBefore()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "ok", 5);
                WriteIntEntry(writer, "cut", 9);
            });

            byte[] truncated = new byte[data.Length - 2];
            System.Array.Copy(data, truncated, truncated.Length);

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(truncated);

            Assert.That(prefs.Count, Is.EqualTo(1));
            Assert.That(prefs["ok"], Is.EqualTo(5));
        }

        [Test]
        public void TryParse_TruncatedEntry_ReportsIncomplete()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "ok", 5);
                WriteIntEntry(writer, "cut", 9);
            });
            byte[] truncated = new byte[data.Length - 2];
            System.Array.Copy(data, truncated, truncated.Length);

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                truncated,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.False);
            Assert.That(prefs["ok"], Is.EqualTo(5));
        }

        [Test]
        public void Parse_UnknownTypeMarker_StopsAndKeepsEarlierEntries()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "ok", 5);

                byte[] keyBytes = Encoding.UTF8.GetBytes("bad");
                writer.Write((byte)keyBytes.Length);
                writer.Write(keyBytes);
                writer.Write((byte)0xAA);
                writer.Write(123);
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs.Count, Is.EqualTo(1));
            Assert.That(prefs["ok"], Is.EqualTo(5));
        }

        [Test]
        public void TryParse_UnknownTypeMarker_ReportsIncomplete()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "ok", 5);
                WriteKey(writer, "bad");
                writer.Write((byte)0xAA);
            });

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                data,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.False);
            Assert.That(prefs["ok"], Is.EqualTo(5));
        }

        [Test]
        public void Parse_LongStringWithOverflowingLength_ReturnsEntriesParsedBeforeWithoutThrowing()
        {
            // A corrupted long-string length near int.MaxValue must not overflow the bounds check
            // (offset + length) and must not throw — earlier entries are returned instead.
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "ok", 5);

                byte[] keyBytes = Encoding.UTF8.GetBytes("bad");
                writer.Write((byte)keyBytes.Length);
                writer.Write(keyBytes);
                writer.Write(LongStringMarker);
                writer.Write(int.MaxValue);
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs.Count, Is.EqualTo(1));
            Assert.That(prefs["ok"], Is.EqualTo(5));
        }

        [Test]
        public void Parse_NegativeLongStringLength_ReturnsEntriesParsedBefore()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "ok", 5);

                byte[] keyBytes = Encoding.UTF8.GetBytes("bad");
                writer.Write((byte)keyBytes.Length);
                writer.Write(keyBytes);
                writer.Write(LongStringMarker);
                writer.Write(-1);
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs.Count, Is.EqualTo(1));
            Assert.That(prefs["ok"], Is.EqualTo(5));
        }

        [Test]
        public void Parse_UnicodeKeyAndValue_RoundTrips()
        {
            byte[] data = BuildDocument(writer => WriteShortStringEntry(writer, "Ключ日本", "Привіт 日本語"));

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs["Ключ日本"], Is.EqualTo("Привіт 日本語"));
        }

        [Test]
        public void Parse_LongAsciiKey_UsesLongStringCodec()
        {
            string key = new string('k', 140);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] data = BuildDocument(writer =>
            {
                // Keep this fixture explicit so it independently pins Unity's
                // 0x80 + int32-length key representation.
                writer.Write(LongStringMarker);
                writer.Write(keyBytes.Length);
                writer.Write(keyBytes);
                writer.Write(IntMarker);
                writer.Write(17);
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs[key], Is.EqualTo(17));
        }

        [Test]
        public void Parse_LongUnicodeKey_UsesUtf8ByteLength()
        {
            string key = new string('界', 50);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] data = BuildDocument(writer =>
            {
                writer.Write(LongStringMarker);
                writer.Write(keyBytes.Length);
                writer.Write(keyBytes);
                writer.Write((byte)1);
                writer.Write(Encoding.UTF8.GetBytes("x"));
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs[key], Is.EqualTo("x"));
        }

        [Test]
        public void Parse_EmptyKey_SkipsEntryAndContinues()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, string.Empty, 1);
                WriteIntEntry(writer, "valid", 2);
            });

            Dictionary<string, object> prefs = PlayerPrefsRuntimeUppParser.Parse(data);

            Assert.That(prefs.Count, Is.EqualTo(1));
            Assert.That(prefs["valid"], Is.EqualTo(2));
        }

        [Test]
        public void TryParse_EmptyKey_ReportsIncomplete()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, string.Empty, 1);
                WriteIntEntry(writer, "valid", 2);
            });

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                data,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.False);
            Assert.That(prefs["valid"], Is.EqualTo(2));
        }

        [Test]
        public void TryParse_DuplicateKey_ReportsIncompleteAndKeepsLastValue()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteIntEntry(writer, "same", 1);
                WriteIntEntry(writer, "same", 2);
            });

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                data,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.False);
            Assert.That(prefs["same"], Is.EqualTo(2));
        }

        [Test]
        public void TryParse_InvalidUtf8_ReportsIncomplete()
        {
            byte[] data = BuildDocument(writer =>
            {
                WriteKey(writer, "bad");
                writer.Write((byte)2);
                writer.Write(new byte[] { 0xC3, 0x28 });
            });

            bool isComplete = PlayerPrefsRuntimeUppParser.TryParse(
                data,
                out Dictionary<string, object> prefs);

            Assert.That(isComplete, Is.False);
            Assert.That(prefs, Is.Empty);
        }

        private static byte[] BuildDocument(System.Action<BinaryWriter> writeEntries, int version = PlayerPrefsRuntimeUppParser.ExpectedVersion)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes(PlayerPrefsRuntimeUppParser.HeaderMagic));
                writer.Write(version);
                writer.Write(0x100000);
                writeEntries(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void WriteKey(BinaryWriter writer, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < LongStringMarker)
            {
                writer.Write((byte)keyBytes.Length);
            }
            else
            {
                writer.Write(LongStringMarker);
                writer.Write(keyBytes.Length);
            }

            writer.Write(keyBytes);
        }

        private static void WriteIntEntry(BinaryWriter writer, string key, int value)
        {
            WriteKey(writer, key);
            writer.Write(IntMarker);
            writer.Write(value);
        }

        private static void WriteFloatEntry(BinaryWriter writer, string key, float value)
        {
            WriteKey(writer, key);
            writer.Write(FloatMarker);
            writer.Write(value);
        }

        private static void WriteShortStringEntry(BinaryWriter writer, string key, string value)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            Assert.That(valueBytes.Length, Is.LessThan(LongStringMarker), "fixture must fit a short string");

            WriteKey(writer, key);
            writer.Write((byte)valueBytes.Length);
            writer.Write(valueBytes);
        }

        private static void WriteLongStringEntry(BinaryWriter writer, string key, string value)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);

            WriteKey(writer, key);
            writer.Write(LongStringMarker);
            writer.Write(valueBytes.Length);
            writer.Write(valueBytes);
        }
    }
}
#endif
