// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Tests the pure export/import JSON serialization, including lossless round-trips
    /// that distinguish int, float and numeric-string values.
    /// </summary>
    public class PlayerPrefsRuntimeJsonSerializerTests
    {
        [Test]
        public void Serialize_ProducesTypedEnvelopeSchema()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "Score", 42 },
                { "Volume", 0.75f },
                { "Name", "Player" }
            };

            string json = PlayerPrefsRuntimeJsonSerializer.Serialize(prefs);
            JObject root = JObject.Parse(json);

            Assert.That(root["version"].Value<int>(), Is.EqualTo(PlayerPrefsRuntimeJsonSerializer.FormatVersion));
            Assert.That(root["prefs"], Is.TypeOf<JObject>());
            Assert.That(root["prefs"]["Score"]["type"].Value<string>(), Is.EqualTo("Int32"));
            Assert.That(root["prefs"]["Score"]["value"].Value<int>(), Is.EqualTo(42));
            Assert.That(root["prefs"]["Volume"]["type"].Value<string>(), Is.EqualTo("Single"));
            Assert.That(root["prefs"]["Volume"]["value"].Value<float>(), Is.EqualTo(0.75f));
            Assert.That(root["prefs"]["Name"]["type"].Value<string>(), Is.EqualTo("String"));
            Assert.That(root["prefs"]["Name"]["value"].Value<string>(), Is.EqualTo("Player"));
        }

        [Test]
        public void RoundTrip_KeepsIntFloatAndNumericStringDistinct()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "asInt", 42 },
                { "asFloat", 42f },
                { "asString", "42" }
            };

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(
                PlayerPrefsRuntimeJsonSerializer.Serialize(prefs));

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Count, Is.EqualTo(3));

            PlayerPrefsRuntimeParsedEntry asInt = result.Entries.Single(entry => entry.Key == "asInt");
            PlayerPrefsRuntimeParsedEntry asFloat = result.Entries.Single(entry => entry.Key == "asFloat");
            PlayerPrefsRuntimeParsedEntry asString = result.Entries.Single(entry => entry.Key == "asString");

            Assert.That(asInt.ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.Int32));
            Assert.That(asInt.Value, Is.EqualTo(42));
            Assert.That(asFloat.ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.Single));
            Assert.That(asFloat.Value, Is.EqualTo(42f));
            Assert.That(asString.ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.String));
            Assert.That(asString.Value, Is.EqualTo("42"));
        }

        [Test]
        public void RoundTrip_Base64LookalikeStringSurvivesUntouched()
        {
            // The fetch-time base64 heuristic never runs on the import path: what is exported
            // is exactly what gets re-imported.
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "token", "dGVzdA==" }
            };

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(
                PlayerPrefsRuntimeJsonSerializer.Serialize(prefs));

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Single().Value, Is.EqualTo("dGVzdA=="));
        }

        [Test]
        public void RoundTrip_UnicodeStrings()
        {
            string unicodeValue = "Привіт 日本語 données";
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "Ключ 日本", unicodeValue }
            };

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(
                PlayerPrefsRuntimeJsonSerializer.Serialize(prefs));

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Single().Key, Is.EqualTo("Ключ 日本"));
            Assert.That(result.Entries.Single().Value, Is.EqualTo(unicodeValue));
        }

        [Test]
        public void RoundTrip_FloatPrecision()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "small", 0.1f },
                { "precise", 123.456f },
                { "large", 1.5e20f }
            };

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(
                PlayerPrefsRuntimeJsonSerializer.Serialize(prefs));

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Single(entry => entry.Key == "small").Value, Is.EqualTo(0.1f));
            Assert.That(result.Entries.Single(entry => entry.Key == "precise").Value, Is.EqualTo(123.456f));
            Assert.That(result.Entries.Single(entry => entry.Key == "large").Value, Is.EqualTo(1.5e20f));
        }

        [Test]
        public void Serialize_SkipsUnsupportedValueTypes()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "tooBig", long.MaxValue },
                { "flag", true },
                { "nothing", null },
                { "ok", 1 }
            };

            string json = PlayerPrefsRuntimeJsonSerializer.Serialize(prefs, out List<string> skippedKeys);
            JObject root = JObject.Parse(json);
            JObject prefsObject = (JObject)root["prefs"];

            Assert.That(prefsObject.Properties().Count(), Is.EqualTo(1));
            Assert.That(prefsObject["ok"], Is.Not.Null);
            Assert.That(skippedKeys, Is.EquivalentTo(new[] { "tooBig", "flag", "nothing" }));
        }

        [Test]
        public void Serialize_NullDictionary_ProducesEmptyEnvelope()
        {
            string json = PlayerPrefsRuntimeJsonSerializer.Serialize(null);
            JObject root = JObject.Parse(json);

            Assert.That(root["version"].Value<int>(), Is.EqualTo(PlayerPrefsRuntimeJsonSerializer.FormatVersion));
            Assert.That(((JObject)root["prefs"]).Properties(), Is.Empty);
        }

        [Test]
        public void Parse_MalformedJson_ReturnsDocumentLevelError()
        {
            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse("{ not json");

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].Key, Is.Empty);
        }

        [Test]
        public void Parse_EmptyString_ReturnsDocumentLevelError()
        {
            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse("   ");

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_UnknownType_ReportsErrorAndParsesSiblings()
        {
            string json = "{\"version\":1,\"prefs\":{\"bad\":{\"type\":\"Bool\",\"value\":true},\"good\":{\"type\":\"Int32\",\"value\":1}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].Key, Is.EqualTo("bad"));
            Assert.That(result.Entries.Single().Key, Is.EqualTo("good"));
        }

        [Test]
        public void Parse_MissingValue_ReportsError()
        {
            string json = "{\"version\":1,\"prefs\":{\"broken\":{\"type\":\"Int32\"}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.EqualTo("broken"));
        }

        [Test]
        public void Parse_EntryThatIsNotAnObject_ReportsError()
        {
            string json = "{\"version\":1,\"prefs\":{\"plain\":5}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.EqualTo("plain"));
        }

        [Test]
        public void Parse_IntOutOfRange_ReportsError()
        {
            string json = "{\"version\":1,\"prefs\":{\"huge\":{\"type\":\"Int32\",\"value\":99999999999}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.EqualTo("huge"));
        }

        [Test]
        public void Parse_FlatDocumentWithBothReservedScalarKeys_ImportsBoth()
        {
            string json = "{\"version\":1,\"prefs\":\"oops\"}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Count, Is.EqualTo(2));
            Assert.That(result.Entries.Single(entry => entry.Key == "version").Value, Is.EqualTo(1));
            Assert.That(result.Entries.Single(entry => entry.Key == "prefs").Value, Is.EqualTo("oops"));
        }

        [Test]
        public void Parse_FlatDocumentWithVersionKey_ImportsThatKey()
        {
            string json = "{\"version\":1}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Single().Key, Is.EqualTo("version"));
            Assert.That(result.Entries.Single().Value, Is.EqualTo(1));
        }

        [Test]
        public void Parse_FlatDocumentWithPrefsKey_ImportsThatKey()
        {
            string json = "{\"prefs\":\"literal value\"}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Single().Key, Is.EqualTo("prefs"));
            Assert.That(result.Entries.Single().Value, Is.EqualTo("literal value"));
        }

        [Test]
        public void Parse_ObjectPrefsWithoutVersion_RejectsWholeDocument()
        {
            string json = "{\"prefs\":{},\"other\":1}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.Empty);
            Assert.That(result.Errors.Single().Message, Does.Contain("version"));
        }

        [Test]
        public void Parse_ArrayPrefsWithVersion_RejectsWholeDocument()
        {
            string json = "{\"version\":1,\"prefs\":[],\"other\":1}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.Empty);
        }

        [Test]
        public void Parse_NullPrefsWithVersion_RejectsWholeDocument()
        {
            string json = "{\"version\":1,\"prefs\":null,\"other\":1}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors.Single().Key, Is.Empty);
            Assert.That(result.Errors.Single().Message, Does.Contain("'prefs' object"));
        }

        [Test]
        public void Parse_IntValueOutsideInt64Range_ReportsErrorWithoutThrowing()
        {
            string json = "{\"version\":1,\"prefs\":{\"huge\":{\"type\":\"Int32\",\"value\":999999999999999999999999999}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.EqualTo("huge"));
        }

        [Test]
        public void Parse_LegacyFlatObject_ParsesBestEffort()
        {
            string json = "{\"a\":1,\"b\":2.5,\"c\":\"x\",\"d\":true}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries.Count, Is.EqualTo(3));
            Assert.That(result.Entries.Single(entry => entry.Key == "a").ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.Int32));
            Assert.That(result.Entries.Single(entry => entry.Key == "b").ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.Single));
            Assert.That(result.Entries.Single(entry => entry.Key == "c").ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.String));
            Assert.That(result.Errors.Single().Key, Is.EqualTo("d"));
        }

        [Test]
        public void Parse_UnexpectedVersion_RejectsDocument()
        {
            string json = "{\"version\":99,\"prefs\":{\"a\":{\"type\":\"Int32\",\"value\":5}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.Empty);
            Assert.That(result.Errors.Single().Message, Does.Contain("version"));
        }

        [Test]
        public void RoundTrip_FloatNaNAndInfinity_Survive()
        {
            Dictionary<string, object> prefs = new Dictionary<string, object>
            {
                { "nan", float.NaN },
                { "posInf", float.PositiveInfinity },
                { "negInf", float.NegativeInfinity }
            };

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(
                PlayerPrefsRuntimeJsonSerializer.Serialize(prefs));

            Assert.That(result.Errors, Is.Empty);
            Assert.That((float)result.Entries.Single(entry => entry.Key == "nan").Value, Is.NaN);
            Assert.That((float)result.Entries.Single(entry => entry.Key == "posInf").Value, Is.EqualTo(float.PositiveInfinity));
            Assert.That((float)result.Entries.Single(entry => entry.Key == "negInf").Value, Is.EqualTo(float.NegativeInfinity));
        }

        [Test]
        public void Parse_SingleWithUnparseableString_ReportsError()
        {
            string json = "{\"version\":1,\"prefs\":{\"v\":{\"type\":\"Single\",\"value\":\"notanumber\"}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Errors.Single().Key, Is.EqualTo("v"));
        }

        [Test]
        public void Parse_EnvelopeSingleAcceptsIntegerToken()
        {
            string json = "{\"version\":1,\"prefs\":{\"v\":{\"type\":\"Single\",\"value\":42}}}";

            PlayerPrefsRuntimeParseResult result = PlayerPrefsRuntimeJsonSerializer.Parse(json);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Entries.Single().ValueType, Is.EqualTo(PlayerPrefsRuntimeValueType.Single));
            Assert.That(result.Entries.Single().Value, Is.EqualTo(42f));
        }
    }
}
#endif
