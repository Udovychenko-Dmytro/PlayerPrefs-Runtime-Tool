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
using UnityEngine.UI;
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

            // Opt-in device gesture: hold three fingers for two seconds to open the viewer.
            PlayerPrefsRuntime.EnableGestureTrigger();

            CreateDemoButtons();

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
        /// Builds a small bottom bar with demo buttons so the tool can be driven
        /// on devices without a keyboard.
        /// </summary>
        private void CreateDemoButtons()
        {
            PlayerPrefsRuntimeUiFactory.EnsureEventSystem();
            Font font = PlayerPrefsRuntimeUiFactory.ResolveDefaultFont();

            GameObject canvasGo = new GameObject("PlayerPrefsRuntimeDemoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = PlayerPrefsRuntimeViewConstants.CanvasMatchWidthOrHeight;

            GameObject buttonBar = new GameObject("DemoButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonBar.transform.SetParent(canvasGo.transform, false);

            RectTransform buttonBarRT = buttonBar.GetComponent<RectTransform>();
            buttonBarRT.anchorMin = new Vector2(0.5f, 0f);
            buttonBarRT.anchorMax = new Vector2(0.5f, 0f);
            buttonBarRT.pivot = new Vector2(0.5f, 0f);
            buttonBarRT.anchoredPosition = new Vector2(0f, 30f);
            buttonBarRT.sizeDelta = new Vector2(900f, 90f);

            HorizontalLayoutGroup layoutGroup = buttonBar.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 16f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;

            Button showButton = PlayerPrefsRuntimeUiFactory.CreateActionButton("ShowViewerButton", buttonBar.transform, "Show Viewer", font, out _, out _);
            showButton.onClick.AddListener(ToggleViewer);

            Button logButton = PlayerPrefsRuntimeUiFactory.CreateActionButton("LogAllButton", buttonBar.transform, "Log All", font, out _, out _);
            logButton.onClick.AddListener(PlayerPrefsRuntime.LogAllPlayerPrefs);

            Button resetButton = PlayerPrefsRuntimeUiFactory.CreateActionButton("ResetDataButton", buttonBar.transform, "Reset Demo Data", font, out _, out _);
            resetButton.onClick.AddListener(AddTestPlayerPrefs);
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
