using System;
using System.Collections.Generic;

namespace ContentContent
{
    public static class ArrayExtensions
    {
        public static bool IsIndexInRange<T>(this T[] array, int index)
        {
            return array != null && index >= 0 && index < array.Length;
        }

        public static void Add<T>(this T[] array, T item)
        {
            if (array == null)
            {
                array = new[] { item };
                return;
            }

            Array.Resize(ref array, array.Length + 1);
            array[^1] = item;
        }

        public static void RemoveAt<T>(this T[] array, int index)
        {
            if (!array.IsIndexInRange(index))
                throw new ArgumentOutOfRangeException(
                    $"The passed index {index} is out of bounds of the array with length {array.Length}");

            T[] newArray = new T[array.Length - 1];

            var j = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (i == index)
                    continue;

                newArray[j] = array[i];
                j++;
            }

            array = newArray;
        }

        public static bool Remove<T>(this T[] array, T item)
        {
            int index = Array.IndexOf(array, item);

            if (index == -1)
                return false;

            array.RemoveAt(index);
            return true;
        }
    }

    /// <summary>
    ///     Generic EqualityComparer for an array of <typeparamref name="T" /> that allows for quick equality check of arrays
    ///     and enables using them as keys in collections (provides GetHashCode method).
    ///     Note that elements of the array must correctly pass equality check for this (override <see cref="object.Equals" />
    ///     ).
    /// </summary>
    /// <typeparam name="T">The type of array elements.</typeparam>
    public class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private readonly EqualityComparer<T> elementComparer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArrayEqualityComparer{T}" /> class.
        /// </summary>
        /// <param name="customElementComparer">
        ///     Custom element comparer. Uses <see cref="EqualityComparer&lt;T>.Default" /> by default.
        /// </param>
        public ArrayEqualityComparer(EqualityComparer<T> customElementComparer = null)
        {
            elementComparer = customElementComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(T[] first, T[] second)
        {
            if (first == second)
                return true;

            if (first == null || second == null)
                return false;

            if (first.Length != second.Length)
                return false;

            for (var i = 0; i < first.Length; i++)
                if (!elementComparer.Equals(first[i], second[i]))
                    return false;

            return true;
        }

        public int GetHashCode(T[] array)
        {
            unchecked
            {
                var hash = 19;

                foreach (T element in array) hash = hash * 37 + elementComparer.GetHashCode(element);

                return hash;
            }
        }
    }
}