using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace ContentContent
{
    /// <summary>
    ///     Handles the low-level injection of the TimerManager into the Unity PlayerLoop.
    ///     This ensures timers update early in the frame regardless of scene composition.
    /// </summary>
    internal static class TimerBootstrapper
    {
        private static PlayerLoopSystem timerSystem;

        /// <summary>
        ///     Initializes the timer system before the first scene loads.
        ///     This timing is critical to ensure timers are available during Awake and OnEnable calls.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Initialize()
        {
            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            if (!InsertTimerManager<Update>(ref currentPlayerLoop, 0))
            {
                Debug.LogWarning("TimerManager failed to register. Timers will not tick.");
                return;
            }

            PlayerLoop.SetPlayerLoop(currentPlayerLoop);

#if UNITY_EDITOR
            // In the Editor, we must manually unhook the system when stopping play mode.
            // Failing to do so can cause the updateDelegate to point to dead memory on re-entry.
            EditorApplication.playModeStateChanged -= OnPlayModeState;
            EditorApplication.playModeStateChanged += OnPlayModeState;

            static void OnPlayModeState(PlayModeStateChange state)
            {
                if (state != PlayModeStateChange.ExitingPlayMode) return;

                PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
                RemoveTimerManager<Update>(ref loop);
                PlayerLoop.SetPlayerLoop(loop);

                TimerManager.Clear();
            }
#endif
        }

        private static void RemoveTimerManager<T>(ref PlayerLoopSystem loop)
        {
            PlayerLoopUtils.RemoveSystem<T>(ref loop, in timerSystem);
        }

        private static bool InsertTimerManager<T>(ref PlayerLoopSystem loop, int index)
        {
            timerSystem = new PlayerLoopSystem
            {
                type = typeof(TimerManager),
                updateDelegate = TimerManager.UpdateTimers,
                subSystemList = null
            };
            return PlayerLoopUtils.InsertSystem<T>(ref loop, in timerSystem, index);
        }
    }
}