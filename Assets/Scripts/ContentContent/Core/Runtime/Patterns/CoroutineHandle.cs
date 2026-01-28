using System.Collections;
using UnityEngine;

namespace ContentContent
{
    /// <summary>
    ///     A wrapper that tracks the state of a coroutine.
    ///     Allows checking .IsRunning and provides a convenient .Stop() method.
    /// </summary>
    public class CoroutineHandle : CustomYieldInstruction
    {
        private readonly Coroutine coroutine;
        private readonly MonoBehaviour owner;

        public CoroutineHandle(MonoBehaviour owner, IEnumerator routine)
        {
            this.owner = owner;
            IsRunning = true;
            coroutine = owner.StartCoroutine(Wrap(routine));
        }

        /// <summary>
        ///     Returns true if the coroutine is currently executing.
        /// </summary>
        public bool IsRunning { get; private set; }

        // Used for CustomYieldInstruction to allow "yield return handle"
        public override bool keepWaiting => IsRunning;

        private IEnumerator Wrap(IEnumerator originalRoutine)
        {
            // Run the actual user routine
            yield return originalRoutine;

            // When finished, update state
            IsRunning = false;
        }

        /// <summary>
        ///     Stops the coroutine immediately and updates the state.
        /// </summary>
        public void Stop()
        {
            if (IsRunning && owner != null && coroutine != null)
            {
                owner.StopCoroutine(coroutine);
                IsRunning = false;
            }
        }
    }

    /// <summary>
    ///     Extensions to enable .RunTracked() syntax
    /// </summary>
    public static class CoroutineHandleExtensions
    {
        /// <summary>
        ///     Starts the coroutine on the Dispatcher and returns a Handle
        ///     that can be checked for IsRunning status.
        /// </summary>
        public static CoroutineHandle Run(this IEnumerator enumerator)
        {
            // Ensure Dispatcher exists
            CoroutineManager manager = CoroutineManager.Instance;
            return manager == null ? null : new CoroutineHandle(manager, enumerator);
        }
    }
}