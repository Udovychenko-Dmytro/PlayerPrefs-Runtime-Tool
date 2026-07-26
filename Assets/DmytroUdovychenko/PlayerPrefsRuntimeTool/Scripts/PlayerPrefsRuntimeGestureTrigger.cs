// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool
{
    /// <summary>
    /// Opt-in device gesture that opens the viewer: hold three fingers on the screen
    /// for two seconds. Disabled by default; enable via PlayerPrefsRuntime.EnableGestureTrigger().
    /// Supports both the legacy Input Manager and the new Input System.
    /// </summary>
    internal sealed class PlayerPrefsRuntimeGestureTrigger : MonoBehaviour
    {
        private static PlayerPrefsRuntimeGestureTrigger s_instance;

        private float m_holdTime;
        private bool m_fired;

        public static bool IsEnabled => s_instance != null && s_instance.enabled;

        public static void Enable()
        {
            if (s_instance == null)
            {
                GameObject triggerGo = new GameObject(PlayerPrefsRuntimeViewConstants.GestureTriggerObjectName);
                Object.DontDestroyOnLoad(triggerGo);
                s_instance = triggerGo.AddComponent<PlayerPrefsRuntimeGestureTrigger>();
            }

            s_instance.enabled = true;
        }

        public static void Disable()
        {
            if (s_instance == null)
            {
                return;
            }

            s_instance.enabled = false;
            s_instance.m_holdTime = 0f;
            s_instance.m_fired = false;
        }

        private void Update()
        {
            int touchCount = GetActiveTouchCount();

            if (touchCount < PlayerPrefsRuntimeViewConstants.GestureTouchCount)
            {
                m_holdTime = 0f;
                m_fired = false;
                return;
            }

            if (m_fired || PlayerPrefsRuntime.IsVisible)
            {
                m_holdTime = 0f;
                return;
            }

            // Unscaled time: the gesture must work while the game is paused (timeScale = 0).
            m_holdTime += Time.unscaledDeltaTime;

            if (m_holdTime >= PlayerPrefsRuntimeViewConstants.GestureHoldDuration)
            {
                m_fired = true; // Latched until the fingers lift below the threshold.
                m_holdTime = 0f;
                PlayerPrefsRuntime.ShowAllPlayerPrefs();
            }
        }

        private static int GetActiveTouchCount()
        {
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            int inputSystemTouches = GetInputSystemTouchCount();
            return inputSystemTouches > 0 ? inputSystemTouches : Input.touchCount;
#elif ENABLE_INPUT_SYSTEM
            return GetInputSystemTouchCount();
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.touchCount;
#else
            // Neither input backend define is set (only possible on Unity older than 2019.2,
            // where the legacy Input Manager is the sole backend). Fall back to it.
            return Input.touchCount;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static int GetInputSystemTouchCount()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return 0;
            }

            int activeTouches = 0;
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                if (touchscreen.touches[i].press.isPressed)
                {
                    activeTouches++;
                }
            }

            return activeTouches;
        }
#endif
    }
}
#endif
