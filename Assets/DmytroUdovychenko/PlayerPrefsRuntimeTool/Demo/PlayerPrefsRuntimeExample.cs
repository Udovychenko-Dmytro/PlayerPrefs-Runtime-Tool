// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Example script demonstrating how to use the PlayerPrefsRuntime tool.
    /// </summary>
    public class PlayerPrefsRuntimeExample : MonoBehaviour
    {
        [SerializeField] private bool m_addTestValue = true;

        private PlayerPrefsRuntimeViewer m_viewer;

        private const string TestValue = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. HELLO w@rлД?! こんにちはプレイヤー！ [1,2,3,4,5,6,7,8,9,0]";

        /// <summary>
        /// Called when the script is started.
        /// Demonstrates adding test data and retrieving all PlayerPrefs.
        /// </summary>
        private void Start()
        {
            Debug.Log("[PlayerPrefsRuntimeExample] Starting example");

            if (m_addTestValue)
            {
                AddTestPlayerPrefs();
            }

            Invoke(nameof(ToggleViewer), 1f);
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame || Input.GetKeyDown(KeyCode.P))
#elif ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.P))
#endif
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
            StringBuilder verylong = new StringBuilder();

            for (int i = 0; i < 100; i++)
            {
                verylong.AppendLine(TestValue);
            }

            string longStringResult = verylong.ToString();

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
            PlayerPrefs.SetString("PLAYER_PREFS_VERY_LONG", longStringResult);
            PlayerPrefs.SetString(TestValue, longStringResult);
            PlayerPrefs.SetString(
                "PLAYER_PREFS_JSON",
                "{\"audio\":{\"master\":0.8,\"music\":0.65,\"sfx\":1.0},\"video\":{\"resolution\":\"2560x1440\",\"fullscreen\":true}}");

            PlayerPrefs.Save();
            Debug.Log("[PlayerPrefsRuntime] Added test PlayerPrefs data");
        }
    }
}
#endif
