// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
    using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Example script demonstrating how to use the PlayerPrefsRuntime tool.
    /// </summary>
    public class PlayerPrefsRuntimeExample : MonoBehaviour
    {
        private PlayerPrefsRuntimeViewer viewer;

        private const string TestValue = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. HELLO w@rлД?! こんにちはプレイヤー！ [1,2,3,4,5,6,7,8,9,0]";

        /// <summary>
        /// Called when the script is started.
        /// Demonstrates adding test data and retrieving all PlayerPrefs.
        /// </summary>
        private void Start()
        {
            Debug.Log("[PlayerPrefsRuntimeExample] Starting example");

            // Add test PlayerPrefs data
            AddTestPlayerPrefs();
            Invoke(nameof(ToggleViewer), 0.5f);
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                ToggleViewer();
            }
        }

        private void ToggleViewer()
        {
            if (!PlayerPrefsRuntime.IsVisible)
            {
                PlayerPrefsRuntime.ShowAllPlayerPrefs();
            }
        }
        
        /// <summary>
        /// Adds test PlayerPrefs data for verification purposes.
        /// </summary>
        private void AddTestPlayerPrefs()
        {
            string verylong = string.Empty;

            for (int i = 0; i < 100; i++)
            {
                verylong += TestValue + "\n";
            }

            PlayerPrefs.SetInt("TEST_INT", 42);
            PlayerPrefs.SetInt("TEST_INT_ZERO", 0);
            PlayerPrefs.SetInt("TEST_INT_NEGATIVE", -777);
            PlayerPrefs.SetFloat("TEST_FLOAT", 3.14159f);
            PlayerPrefs.SetFloat("TEST_FLOAT_SMALL", 0.0001f);
            PlayerPrefs.SetFloat("TEST_FLOAT_LARGE", 123456.789f);
            PlayerPrefs.SetString("PLAYER_PREFS_RUNTIME", "HELLO w@rлД?!");
            PlayerPrefs.SetString("PLAYER_PREFS_RU", "Привет, мир!");
            PlayerPrefs.SetString("PLAYER_PREFS_UA", "Вітаю, геймере!");
            PlayerPrefs.SetString("PLAYER_PREFS_JP", "こんにちはプレイヤー！");
            PlayerPrefs.SetString("PLAYER_PREFS_SHORT", "OK");
            PlayerPrefs.SetString("PLAYER_PREFS_MEDIUM", "Sample medium-length string value.");
            PlayerPrefs.SetString("PLAYER_PREFS_VERY_LONG", verylong);
            PlayerPrefs.SetString(TestValue, verylong);
            PlayerPrefs.SetString(
                "PLAYER_PREFS_LONG", TestValue + TestValue + TestValue + TestValue + TestValue + TestValue + TestValue + TestValue + TestValue + TestValue);
            PlayerPrefs.SetString(
                "PLAYER_PREFS_JSON",
                "{\"audio\":{\"master\":0.8,\"music\":0.65,\"sfx\":1.0},\"video\":{\"resolution\":\"2560x1440\",\"fullscreen\":true}}");

            PlayerPrefs.Save();
            Debug.Log("[PlayerPrefsRuntime] Added test PlayerPrefs data");
        }
    }
}
#endif
