using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace ContentContent
{
    internal interface IPool
    {
        int ActiveCount { get; }
        void ReturnAll();
        void ReturnObjectsByScene(Scene scene);
    }

    internal class Pool<T> : IPool where T : PoolableBehaviour
    {
        private readonly ObjectPool<T> internalPool;
        private readonly List<T> activeItems = new();
        private readonly Transform poolParent;

        public Pool(Func<T> factory, Transform parent, int capacity, int maxSize)
        {
            poolParent = parent;
            internalPool = new ObjectPool<T>(
                createFunc: () =>
                {
                    T item = factory();
                    item.transform.SetParent(poolParent);
                    return item;
                },
                actionOnGet: item =>
                {
                    IPoolable p = item;
                    p.IsActiveInternal = true; // Set flag on retrieval
                    p.OnGetFromPool();
                },
                actionOnRelease: item =>
                {
                    IPoolable p = item;
                    p.IsActiveInternal = false; // Reset flag on return
                    p.OnReleaseToPool();
                    item.gameObject.SetActive(false);
                    if (item.transform.parent != poolParent) item.transform.SetParent(poolParent);
                },
                actionOnDestroy: item => UnityEngine.Object.Destroy(item.gameObject),
                collectionCheck: true,
                defaultCapacity: capacity,
                maxSize: maxSize
            );
        }

        public T Get()
        {
            T item = internalPool.Get();
            activeItems.Add(item);
            return item;
        }

        public void Release(T item)
        {
            activeItems.Remove(item);
            internalPool.Release(item);
        }

        public void ReturnAll()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                T item = activeItems[i];
                IPoolable p = item;
                if (p is { IsActiveInternal: true })
                    internalPool.Release(activeItems[i]);
            }

            activeItems.Clear();
        }

        public void ReturnObjectsByScene(Scene scene)
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                T item = activeItems[i];
                IPoolable p = item;
                if (item != null && p.IsActiveInternal && item.gameObject.scene == scene)
                {
                    internalPool.Release(item);
                }
                else if (item == null)
                {
                    activeItems.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Returns the count of active items from Unity's internal ObjectPool.
        /// </summary>
        public int ActiveCount
        {
            get
            {
                // Clean up any potentially destroyed references before returning count
                activeItems.RemoveAll(item => item == null);
                return internalPool.CountActive;
            }
        }
    }
}