using System.Collections.Generic;
using UnityEngine;

namespace ContentContent
{
    /// <summary>
    ///     Internal execution hub for all active timers.
    ///     Handles the per-frame dispatching of Tick calls.
    /// </summary>
    internal static class TimerManager
    {
        private static readonly List<Timer> activeTimers = new(64);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Config()
        {
            activeTimers.Clear();
        }

        internal static void RegisterTimer(Timer timer)
        {
            if (!activeTimers.Contains(timer)) activeTimers.Add(timer);
        }

        internal static void DeregisterTimer(Timer timer)
        {
            activeTimers.Remove(timer);
        }

        /// <summary> Iterates backwards to allow timers to safely remove themselves during Tick. </summary>
        internal static void UpdateTimers()
        {
            for (int i = activeTimers.Count - 1; i >= 0; i--) activeTimers[i].Tick();
        }

        /// <summary> Disposes all active timers. Called on scene cleanup or playmode exit. </summary>
        internal static void Clear()
        {
            for (int i = activeTimers.Count - 1; i >= 0; i--) activeTimers[i].Dispose();
            activeTimers.Clear();
        }
    }
}