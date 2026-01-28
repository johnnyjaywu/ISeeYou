using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ContentContent
{
    /// <summary>
    /// Defines a contract for objects managed by the <see cref="ObjectPoolManager"/>.
    /// Allows the manager to reset state, handle lifecycle events, and optimize memory usage.
    /// </summary>
    internal interface IPoolable
    {
        /// <summary>
        /// Explicit flag to track if the object is currently "out" of the pool.
        /// Prevents double-release errors and provides a reliable state check.
        /// </summary>
        bool IsActiveInternal { get; set; }

        /// <summary>
        /// Stores a reference to the internal pool that spawned this instance.
        /// This reference allows for high-performance, dictionary-free release operations.
        /// </summary>
        object OriginPool { get; set; }

        /// <summary>
        /// Invoked by the pool manager immediately after an object is retrieved from the pool,
        /// but before it is repositioned or activated. Use this to reset health, timers, or velocity.
        /// </summary>
        void OnGetFromPool();

        /// <summary>
        /// Invoked by the pool manager immediately before an object is deactivated and returned to the pool.
        /// Use this to stop particles, cancel tweens, or unbind local events.
        /// </summary>
        void OnReleaseToPool();
    }

    /// <summary>
    /// Provides convenient extension methods to allow prefabs to spawn themselves via the <see cref="ObjectPoolManager"/>.
    /// </summary>
    public static class PoolableExtensions
    {
        /// <summary>
        /// Retrieves an instance from the pool at the specified position and rotation.
        /// </summary>
        public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation, bool setActive = true)
            where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.Get(prefab, position, rotation, setActive);
        }

        /// <summary>
        /// Retrieves an instance from the pool at the specified position with its default rotation.
        /// </summary>
        public static T Spawn<T>(this T prefab, Vector3 position, bool setActive = true)
            where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.Get(prefab, position,
                prefab.transform.rotation,
                setActive);
        }

        /// <summary>
        /// Retrieves an instance from the pool at the prefab's default position and rotation
        /// </summary>
        public static T Spawn<T>(this T prefab, bool setActive = true)
            where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.Get(prefab, prefab.transform.position,
                prefab.transform.rotation, setActive);
        }

        /// <summary>
        /// Retrieves multiple objects and returns them in a pooled List.
        /// This is the preferred method for high-performance bursts as it minimizes heap allocations.
        /// IMPORTANT: You must release the list back to <see cref="ListPool{T}"/> when finished using ObjectPoolManager.Instance.ReleaseAll().
        /// </summary>
        public static List<T> SpawnBurst<T>(this T prefab, int count, Vector3 position, Quaternion rotation, bool setActive = true)
            where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.GetBurst(prefab, count, position, rotation, setActive);
        }
        
        /// <summary>
        /// Retrieves multiple objects and returns them in a pooled List.
        /// This is the preferred method for high-performance bursts as it minimizes heap allocations.
        /// IMPORTANT: You must release the list back to <see cref="ListPool{T}"/> when finished using ObjectPoolManager.Instance.ReleaseAll().
        /// </summary>
        public static List<T> SpawnBurst<T>(this T prefab, int count, Vector3 position, bool setActive = true)
            where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.GetBurst(prefab, count, position, prefab.transform.rotation, setActive);
        }
        
        /// <summary>
        /// Retrieves multiple objects and returns them in a pooled List.
        /// This is the preferred method for high-performance bursts as it minimizes heap allocations.
        /// IMPORTANT: You must release the list back to <see cref="ListPool{T}"/> when finished using ObjectPoolManager.Instance.ReleaseAll().
        /// </summary>
        public static List<T> SpawnBurst<T>(this T prefab, int count, bool setActive = true)
            where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.GetBurst(prefab, count, prefab.transform.position, prefab.transform.rotation, setActive);
        }

        /// <summary>
        /// Retrieves multiple objects and returns them in a standard array.
        /// Useful for initialization logic where the collection will persist for a long time.
        /// </summary>
        public static T[] SpawnMultiple<T>(this T prefab, int count, Vector3 position, Quaternion rotation, bool setActive = true)
            where T : PoolableBehaviour
        {
            T[] results = new T[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = prefab.Spawn(position, rotation, setActive);
            }

            return results;
        }
        
        /// <summary>
        /// Returns the number of currently active instances in the pool associated with this prefab.
        /// </summary>
        /// <returns>The count of objects currently out of the pool.</returns>
        public static int GetActiveCount<T>(this T prefab) where T : PoolableBehaviour
        {
            return ObjectPoolManager.Instance.GetActiveCount(prefab);
        }
    }
}