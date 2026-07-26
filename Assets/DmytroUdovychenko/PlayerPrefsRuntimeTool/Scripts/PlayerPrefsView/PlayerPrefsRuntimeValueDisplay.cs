// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Pure, UI-independent value-presentation logic for the entry dialog: pretty-prints JSON
    /// and caps what is handed to the legacy <c>UnityEngine.UI.Text</c> so a very long value
    /// cannot exceed its ~65k vertex limit (past which the text fails to build its mesh and
    /// renders as nothing). Extracted from the dialog so the behavior can be unit tested; the
    /// untruncated value always stays available through Copy Value, Export and the public API.
    /// </summary>
    internal static class PlayerPrefsRuntimeValueDisplay
    {
        /// <summary>
        /// Returns the value for read-mode display: JSON objects/arrays are pretty-printed,
        /// anything else (including invalid JSON) is shown as-is. The result is always capped
        /// to <see cref="PlayerPrefsRuntimeViewConstants.DialogValueDisplayMaxLength"/> characters,
        /// appending a note when the value is truncated.
        /// </summary>
        internal static string FormatForDisplay(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return PlayerPrefsRuntimeViewConstants.EmptyValueLabel;
            }

            string display = PrettyPrintJsonOrRaw(rawValue);
            return ClampToDisplayLimit(display, rawValue.Length);
        }

        /// <summary>
        /// Decides whether a value is short enough to edit inline in the dialog's InputField.
        /// Values above the display limit are edited through Import or the public API instead,
        /// so the tail is never silently lost. <paramref name="message"/> carries the reason
        /// when the result is <c>false</c> and is <c>null</c> otherwise.
        /// </summary>
        internal static bool IsEditableInline(string value, out string message)
        {
            message = null;

            if (!string.IsNullOrEmpty(value)
                && value.Length > PlayerPrefsRuntimeViewConstants.DialogValueDisplayMaxLength)
            {
                message = string.Format(PlayerPrefsRuntimeViewConstants.EditValueTooLargeFormat, value.Length);
                return false;
            }

            return true;
        }

        private static string PrettyPrintJsonOrRaw(string rawValue)
        {
            if (rawValue.Length > PlayerPrefsRuntimeViewConstants.JsonPrettyPrintMaxLength)
            {
                return rawValue;
            }

            string trimmed = rawValue.TrimStart();
            if (trimmed.Length == 0)
            {
                return rawValue;
            }

            char firstChar = trimmed[0];
            if (firstChar != '{' && firstChar != '[')
            {
                return rawValue;
            }

            try
            {
                JToken token = JToken.Parse(rawValue);
                if (token is JObject || token is JArray)
                {
                    return token.ToString(Formatting.Indented);
                }
            }
            catch (JsonException)
            {
                // Not valid JSON: fall through to the raw value.
            }

            return rawValue;
        }

        /// <summary>
        /// Caps the string handed to the legacy UI Text so it stays under the ~65k vertex limit.
        /// The note reports the original character count so the user knows the value was cut.
        /// </summary>
        private static string ClampToDisplayLimit(string display, int rawLength)
        {
            int max = PlayerPrefsRuntimeViewConstants.DialogValueDisplayMaxLength;
            if (display.Length <= max)
            {
                return display;
            }

            return display.Substring(0, max)
                + string.Format(PlayerPrefsRuntimeViewConstants.DialogValueTruncatedNoteFormat, max, rawLength);
        }
    }
}
#endif
