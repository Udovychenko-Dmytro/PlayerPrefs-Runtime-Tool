// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Represents a normalized PlayerPrefs entry containing name, type, and value.
    /// </summary>
    public readonly struct PlayerPrefsRuntimeEntry
    {
        public string Name { get; }
        public string Type { get; }
        public string Value { get; }

        public PlayerPrefsRuntimeEntry(string name, object rawValue)
        {
            Name = string.IsNullOrEmpty(name) ? "(Unnamed)" : name;

            if (rawValue == null)
            {
                Type = "null";
                Value = "(null)";
            }
            else
            {
                Type = rawValue.GetType().Name;
                Value = rawValue.ToString();
            }
        }
    }
}
#endif