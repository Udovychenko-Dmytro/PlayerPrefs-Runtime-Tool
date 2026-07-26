// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.Text;
using NUnit.Framework;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Tests the pure byte-level decoding of Unity PlayerPrefs registry values
    /// via <see cref="PlayerPrefsRuntimeRegistryValueDecoder"/> using synthetic fixtures.
    /// </summary>
    public class PlayerPrefsRuntimeRegistryValueDecoderTests
    {
        [Test]
        public void DecodeRegistryValue_Dword_ReturnsInt()
        {
            byte[] data = BitConverter.GetBytes(42);

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegDword, data, 4);

            Assert.That(result, Is.TypeOf<int>());
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void DecodeRegistryValue_DwordTooShort_ReturnsNull()
        {
            byte[] data = new byte[3];

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegDword, data, 3);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DecodeRegistryValue_DwordWithTrailingBytes_ReturnsNull()
        {
            byte[] data = new byte[5];
            Buffer.BlockCopy(BitConverter.GetBytes(42), 0, data, 0, sizeof(int));

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegDword,
                data,
                (uint)data.Length);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DecodeRegistryValue_Qword_ReturnsLong()
        {
            byte[] data = BitConverter.GetBytes(123456789012345L);

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegQword, data, 8);

            Assert.That(result, Is.TypeOf<long>());
            Assert.That(result, Is.EqualTo(123456789012345L));
        }

        [Test]
        public void DecodeRegistryValue_QwordWithTrailingBytes_ReturnsNull()
        {
            byte[] data = new byte[9];
            Buffer.BlockCopy(BitConverter.GetBytes(123L), 0, data, 0, sizeof(long));

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegQword,
                data,
                (uint)data.Length);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DecodeRegistryValue_UnicodeString_StripsTerminatorAndDecodes()
        {
            byte[] data = Encoding.Unicode.GetBytes("Hello\0");

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegSz, data, (uint)data.Length);

            Assert.That(result, Is.EqualTo("Hello"));
        }

        [Test]
        public void DecodeRegistryValue_UnicodeNumericString_RemainsString()
        {
            byte[] data = Encoding.Unicode.GetBytes("1.5\0");

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegSz, data, (uint)data.Length);

            Assert.That(result, Is.TypeOf<string>());
            Assert.That(result, Is.EqualTo("1.5"));
        }

        [Test]
        public void DecodeRegistryValue_UnicodeStringTooShort_ReturnsEmptyString()
        {
            byte[] data = new byte[2];

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegSz, data, 2);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DecodeRegistryValue_UnicodeStringWithoutTerminator_ReturnsRawBytes()
        {
            byte[] data = Encoding.Unicode.GetBytes("A");

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegSz,
                data,
                (uint)data.Length);

            Assert.That(result, Is.TypeOf<byte[]>());
            Assert.That((byte[])result, Is.EqualTo(data));
        }

        [Test]
        public void DecodeRegistryValue_UnicodeStringWithInvalidSurrogate_ReturnsRawBytes()
        {
            byte[] data = new byte[] { 0x00, 0xD8, 0x00, 0x00 };

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegSz,
                data,
                (uint)data.Length);

            Assert.That(result, Is.TypeOf<byte[]>());
            Assert.That((byte[])result, Is.EqualTo(data));
        }

        [Test]
        public void DecodeRegistryValue_ExpandSz_DecodesLikeSz()
        {
            byte[] data = Encoding.Unicode.GetBytes("World\0");

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegExpandSz, data, (uint)data.Length);

            Assert.That(result, Is.EqualTo("World"));
        }

        [Test]
        public void DecodeRegistryValue_BinaryFourBytes_ReturnsFloat()
        {
            byte[] data = BitConverter.GetBytes(1.25f);

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegBinary, data, 4);

            Assert.That(result, Is.TypeOf<float>());
            Assert.That(result, Is.EqualTo(1.25f));
        }

        [Test]
        public void DecodeRegistryValue_BinaryUtf8WithTrailingNul_ReturnsString()
        {
            byte[] text = Encoding.UTF8.GetBytes("World");
            byte[] data = new byte[text.Length + 1];
            Buffer.BlockCopy(text, 0, data, 0, text.Length);

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegBinary, data, (uint)data.Length);

            Assert.That(result, Is.EqualTo("World"));
        }

        [Test]
        public void DecodeRegistryValue_BinaryEmpty_ReturnsNull()
        {
            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegBinary, new byte[0], 0);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DecodeRegistryValue_BinarySingleNulByte_ReturnsRawBytes()
        {
            // Pins the fallback: data that decodes to an empty string is returned as a raw copy.
            byte[] data = new byte[] { 0 };

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegBinary, data, 1);

            Assert.That(result, Is.TypeOf<byte[]>());
            Assert.That((byte[])result, Is.EqualTo(new byte[] { 0 }));
        }

        [Test]
        public void DecodeRegistryValue_BinaryInvalidUtf8_ReturnsRawBytes()
        {
            byte[] data = new byte[] { 0xC3, 0x28, 0xFF, 0xFE, 0x01 };

            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegBinary,
                data,
                (uint)data.Length);

            Assert.That(result, Is.TypeOf<byte[]>());
            Assert.That((byte[])result, Is.EqualTo(data));
        }

        [Test]
        public void DecodeRegistryValue_UnknownType_ReturnsNull()
        {
            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                99, new byte[4], 4);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DecodeRegistryValue_SizeExceedsBuffer_ReturnsNull()
        {
            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegDword, new byte[2], 8);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DecodeRegistryValue_NullData_ReturnsNull()
        {
            object result = PlayerPrefsRuntimeRegistryValueDecoder.DecodeRegistryValue(
                PlayerPrefsRuntimeRegistryValueDecoder.RegDword, null, 4);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void NormalizeKey_StripsUnityHashSuffix()
        {
            string result = PlayerPrefsRuntimeRegistryValueDecoder.NormalizeKey("Score_h3282552528");

            Assert.That(result, Is.EqualTo("Score"));
        }

        [Test]
        public void NormalizeKey_NonNumericSuffix_IsKept()
        {
            string result = PlayerPrefsRuntimeRegistryValueDecoder.NormalizeKey("Score_hash");

            Assert.That(result, Is.EqualTo("Score_hash"));
        }

        [Test]
        public void NormalizeKey_EmptyOrNull_ReturnsUnnamedPlaceholder()
        {
            Assert.That(PlayerPrefsRuntimeRegistryValueDecoder.NormalizeKey(string.Empty),
                Is.EqualTo(PlayerPrefsRuntimeRegistryValueDecoder.UnnamedKey));
            Assert.That(PlayerPrefsRuntimeRegistryValueDecoder.NormalizeKey(null),
                Is.EqualTo(PlayerPrefsRuntimeRegistryValueDecoder.UnnamedKey));
        }
    }
}
#endif
