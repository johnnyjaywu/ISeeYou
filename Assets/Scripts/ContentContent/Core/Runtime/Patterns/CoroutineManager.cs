using UnityEngine;

namespace ContentContent
{
    /// <summary>
    ///     A global bridge that allows POCOs (plain C# objects) or other objects to run Coroutines.
    ///     It automatically creates a hidden GameObject to handle the coroutine lifecycle.
    /// </summary>
    [AddComponentMenu("")]
    public class CoroutineManager : SingletonBehaviour<CoroutineManager>
    {
        /// <summary>
        ///     Static helper to stop all coroutines running on the manager.
        /// </summary>
        public static void StopAll()
        {
            if (Instance == null) return;
            Instance.StopAllCoroutines();
        }
    }
}