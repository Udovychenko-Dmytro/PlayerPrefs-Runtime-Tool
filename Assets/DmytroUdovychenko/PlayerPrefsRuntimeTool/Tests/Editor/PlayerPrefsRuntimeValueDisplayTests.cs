// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Text;
using NUnit.Framework;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Tests
{
    /// <summary>
    /// Pins the value-presentation logic of <see cref="PlayerPrefsRuntimeValueDisplay"/>:
    /// JSON pretty-printing and — critically — the length cap that keeps a very long value
    /// from exceeding the legacy UI Text vertex limit (the tool's long-string display bug).
    /// </summary>
    public class PlayerPrefsRuntimeValueDisplayTests
    {
        private static int MaxLen => PlayerPrefsRuntimeViewConstants.DialogValueDisplayMaxLength;

        // ---- FormatForDisplay: empty / passthrough ----

        [Test]
        public void FormatForDisplay_Null_ReturnsEmptyLabel()
        {
            Assert.That(
                PlayerPrefsRuntimeValueDisplay.FormatForDisplay(null),
                Is.EqualTo(PlayerPrefsRuntimeViewConstants.EmptyValueLabel));
        }

        [Test]
        public void FormatForDisplay_Empty_ReturnsEmptyLabel()
        {
            Assert.That(
                PlayerPrefsRuntimeValueDisplay.FormatForDisplay(string.Empty),
                Is.EqualTo(PlayerPrefsRuntimeViewConstants.EmptyValueLabel));
        }

        [Test]
        public void FormatForDisplay_ShortPlainString_ReturnedAsIs()
        {
            const string value = "just a short value";

            Assert.That(PlayerPrefsRuntimeValueDisplay.FormatForDisplay(value), Is.EqualTo(value));
        }

        [Test]
        public void FormatForDisplay_NonJsonStartingWithLetter_ReturnedAsIs()
        {
            const string value = "hello {not json}";

            Assert.That(PlayerPrefsRuntimeValueDisplay.FormatForDisplay(value), Is.EqualTo(value));
        }

        // ---- FormatForDisplay: JSON pretty-printing ----

        [Test]
        public void FormatForDisplay_JsonObject_IsPrettyPrinted()
        {
            const string compact = "{\"a\":1,\"b\":2}";

            string result = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(compact);

            Assert.That(result, Is.Not.EqualTo(compact));
            Assert.That(result, Does.StartWith("{"));
            Assert.That(result, Does.Contain("\n"));
            Assert.That(result, Does.Contain("\"a\""));
        }

        [Test]
        public void FormatForDisplay_JsonArray_IsPrettyPrinted()
        {
            const string compact = "[1,2,3]";

            string result = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(compact);

            Assert.That(result, Is.Not.EqualTo(compact));
            Assert.That(result, Does.StartWith("["));
            Assert.That(result, Does.Contain("\n"));
        }

        [Test]
        public void FormatForDisplay_InvalidJsonStartingWithBrace_ReturnedAsIs()
        {
            const string value = "{not valid json";

            Assert.That(PlayerPrefsRuntimeValueDisplay.FormatForDisplay(value), Is.EqualTo(value));
        }

        // ---- FormatForDisplay: the length cap (long-string bug) ----

        [Test]
        public void FormatForDisplay_AtLimit_NotTruncated()
        {
            string value = new string('x', MaxLen);

            string result = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(value);

            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public void FormatForDisplay_OverLimit_IsTruncatedWithNote()
        {
            int rawLen = MaxLen + 500;
            string value = new string('x', rawLen);
            string expectedNote = string.Format(
                PlayerPrefsRuntimeViewConstants.DialogValueTruncatedNoteFormat, MaxLen, rawLen);

            string result = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(value);

            Assert.That(result.Length, Is.EqualTo(MaxLen + expectedNote.Length));
            Assert.That(result, Does.StartWith(new string('x', MaxLen)));
            Assert.That(result, Does.EndWith(expectedNote));
            // The reported original length must be the real one, so the note is honest.
            Assert.That(result, Does.Contain(rawLen.ToString()));
        }

        [Test]
        public void FormatForDisplay_JsonThatInflatesPastLimitWhenPrettyPrinted_IsStillCapped()
        {
            // Regression: a compact JSON blob under the pretty-print threshold gets pretty-printed,
            // which inflates it past the display limit. The final string must still be capped so the
            // legacy UI Text never receives an over-limit value.
            StringBuilder builder = new StringBuilder();
            builder.Append('{');
            for (int i = 0; i < 1000; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('"').Append('k').Append(i).Append('"').Append(':').Append(i);
            }
            builder.Append('}');
            string compactJson = builder.ToString();

            // Preconditions that make this scenario meaningful.
            Assert.That(compactJson.Length, Is.LessThanOrEqualTo(PlayerPrefsRuntimeViewConstants.JsonPrettyPrintMaxLength),
                "Test data must stay under the pretty-print threshold so pretty-printing is attempted.");

            string expectedNote = string.Format(
                PlayerPrefsRuntimeViewConstants.DialogValueTruncatedNoteFormat, MaxLen, compactJson.Length);

            string result = PlayerPrefsRuntimeValueDisplay.FormatForDisplay(compactJson);

            Assert.That(result.Length, Is.EqualTo(MaxLen + expectedNote.Length));
            Assert.That(result, Does.EndWith(expectedNote));
            Assert.That(result, Does.StartWith("{"));
        }

        // ---- IsEditableInline ----

        [Test]
        public void IsEditableInline_Null_IsEditable()
        {
            bool editable = PlayerPrefsRuntimeValueDisplay.IsEditableInline(null, out string message);

            Assert.That(editable, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void IsEditableInline_ShortValue_IsEditable()
        {
            bool editable = PlayerPrefsRuntimeValueDisplay.IsEditableInline("small", out string message);

            Assert.That(editable, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void IsEditableInline_AtLimit_IsEditable()
        {
            string value = new string('x', MaxLen);

            bool editable = PlayerPrefsRuntimeValueDisplay.IsEditableInline(value, out string message);

            Assert.That(editable, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void IsEditableInline_OverLimit_IsBlockedWithReason()
        {
            int length = MaxLen + 1;
            string value = new string('x', length);

            bool editable = PlayerPrefsRuntimeValueDisplay.IsEditableInline(value, out string message);

            Assert.That(editable, Is.False);
            Assert.That(message, Is.Not.Null.And.Not.Empty);
            Assert.That(message, Does.Contain(length.ToString()));
        }
    }
}
#endif
