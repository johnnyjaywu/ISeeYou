using UnityEngine.LowLevel;

namespace ContentContent
{
    /// <summary>
    ///     Utility class for safely modifying the Unity PlayerLoop.
    ///     Provides methods to inject custom systems into specific loop phases.
    /// </summary>
    public static class PlayerLoopUtils
    {
        /// <summary>
        ///     Inserts a new system into a specific category of the PlayerLoop.
        ///     Reconstructs the array manually to avoid the overhead of wrapper collections.
        /// </summary>
        public static bool InsertSystem<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToInsert, int index)
        {
            // Recursive search: find the correct loop category (e.g., Update)
            if (loop.type != typeof(T)) return HandleSubSystemLoop<T>(ref loop, in systemToInsert, index);

            PlayerLoopSystem[] sourceList = loop.subSystemList;
            int oldLength = sourceList?.Length ?? 0;
            PlayerLoopSystem[] newList = new PlayerLoopSystem[oldLength + 1];

            // Manually shift elements to insert the new system at the target index
            for (var i = 0; i < newList.Length; i++)
                if (i < index) newList[i] = sourceList[i];
                else if (i == index) newList[i] = systemToInsert;
                else newList[i] = sourceList[i - 1];

            loop.subSystemList = newList;
            return true;
        }

        private static bool HandleSubSystemLoop<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToInsert,
            int index)
        {
            if (loop.subSystemList == null) return false;

            for (var i = 0; i < loop.subSystemList.Length; i++)
                if (InsertSystem<T>(ref loop.subSystemList[i], in systemToInsert, index))
                    return true;

            return false;
        }

        /// <summary>
        ///     Removes a custom system from the PlayerLoop. Essential for cleaning up the Editor environment.
        /// </summary>
        public static void RemoveSystem<T>(ref PlayerLoopSystem loop, in PlayerLoopSystem systemToRemove)
        {
            if (loop.subSystemList == null) return;

            int indexToRemove = -1;
            for (var i = 0; i < loop.subSystemList.Length; i++)
                if (loop.subSystemList[i].type == systemToRemove.type &&
                    loop.subSystemList[i].updateDelegate == systemToRemove.updateDelegate)
                {
                    indexToRemove = i;
                    break;
                }

            if (indexToRemove != -1)
            {
                PlayerLoopSystem[] oldList = loop.subSystemList;
                PlayerLoopSystem[] newList = new PlayerLoopSystem[oldList.Length - 1];

                for (int i = 0, j = 0; i < oldList.Length; i++)
                {
                    if (i == indexToRemove) continue;
                    newList[j++] = oldList[i];
                }

                loop.subSystemList = newList;
                return;
            }

            for (var i = 0; i < loop.subSystemList.Length; i++)
                RemoveSystem<T>(ref loop.subSystemList[i], systemToRemove);
        }
    }
}