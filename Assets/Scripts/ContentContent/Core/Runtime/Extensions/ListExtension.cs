using System.Collections.Generic;
using UnityEngine;

namespace ContentContent
{
    public static class ListExtension
    {
        public static bool IsNullOrEmpty<T>(this IList<T> list) => list == null || list.Count == 0;
        
        public static bool TryAdd<T>(this IList<T> list, T value)
        {
            if (list.Contains(value))
                return false;

            list.Add(value);
            return true;
        }

        public static bool TryRemove<T>(this IList<T> list, T value)
        {
            if (value == null || !list.Contains(value))
                return false;

            list.Remove(value);
            return true;
        }

        /// <summary>
        ///     Shuffles the elements of a list randomly using the Fisher-Yates algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1); // Use Unity's Random.Range for game-specific randomness
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static void RefreshWith<T>(this List<T> list, IEnumerable<T> items)
        {
            list.Clear();
            list.AddRange(items);
        }
    }
}