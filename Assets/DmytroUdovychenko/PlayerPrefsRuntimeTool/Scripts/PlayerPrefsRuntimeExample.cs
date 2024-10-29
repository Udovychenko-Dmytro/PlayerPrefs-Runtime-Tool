// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================

using System.Collections.Generic;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    public class PlayerPrefsRuntimeExample : MonoBehaviour
    {
        private void Start()
        {
#if PLAYER_PREFS_RUNTIME_TOOL
            PlayerPrefsRuntime.AddTestPlayerPrefs(); // add playerpref data
            PlayerPrefsRuntime.LogAllPlayerPrefs();  // log all playerpref data to console
            
            // retrieve all PlayerPrefs as Dictionary <key, value>
            Dictionary<string, object> allPrefs = PlayerPrefsRuntime.GetAllPlayerPrefs();
#endif
        }
    }
}