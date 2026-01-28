using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace ContentContent
{
    /// <summary>
    /// Centralized system for managing object recycling. Reduces Garbage Collector pressure
    /// by reusing objects instead of frequently instantiating and destroying them.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ObjectPoolManager : SingletonBehaviour<ObjectPoolManager>
    {
        [Header("Global Pool Constraints")]
        [SerializeField] [Tooltip("The initial number of objects created when a new pool is initialized.")]
        private int defaultPoolSize = 10;

        [SerializeField]
        [Tooltip("The maximum number of inactive objects the pool will store before destroying extras.")]
        private int maxPoolSize = 1000;

        private readonly Dictionary<object, IPool> pools = new();
        private bool isQuitting;

        protected override void Awake()
        {
            base.Awake();
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => SceneManager.sceneUnloaded += OnSceneUnloaded;
        private void OnDisable() => SceneManager.sceneUnloaded -= OnSceneUnloaded;

        /// <summary>
        /// Automatically cleans up pooled objects associated with an unloaded scene.
        /// This prevents "MissingReferenceException" errors in additive scene workflows.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            if (isQuitting) return;
            foreach (var pool in pools.Values) pool.ReturnObjectsByScene(scene);
        }

        /// <summary>
        /// Forces the creation of pool instances before they are needed.
        /// Call this during loading screens to prevent frame-rate spikes during combat.
        /// </summary>
        public void Preload<T>(T prefab, int count) where T : PoolableBehaviour
        {
            if (count <= 0) return;
            List<T> temp = ListPool<T>.Get();
            for (int i = 0; i < count; i++)
                temp.Add(Get(prefab, prefab.transform.position, prefab.transform.rotation, false));

            foreach (var item in temp) Release(item);
            ListPool<T>.Release(temp);
        }

        /// <summary>
        /// Retrieves a single object from the pool.
        /// </summary>
        public T Get<T>(T prefab, Vector3 position, Quaternion rotation, bool setActive = true)
            where T : PoolableBehaviour
        {
            return GetInternal(prefab, () => Instantiate(prefab), position, rotation, setActive);
        }

        public T Get<T>(object key, Func<T> createFunc, Vector3 position, Quaternion rotation, bool setActive = true)
            where T : PoolableBehaviour
        {
            return GetInternal(key, createFunc, position, rotation, setActive);
        }

        /// <summary>
        /// Retrieves multiple objects and returns them in a pooled List.
        /// IMPORTANT: Release the list back to <see cref="ListPool{T}"/> when finished.
        /// </summary>
        public List<T> GetBurst<T>(T prefab, int count, Vector3 position, Quaternion rotation, bool setActive)
            where T : PoolableBehaviour
        {
            List<T> list = ListPool<T>.Get();
            for (int i = 0; i < count; i++)
                list.Add(Get(prefab, position, rotation, setActive));
            return list;
        }

        /// <summary>
        /// Safely releases an object back to its pool. Uses the IsActive flag to prevent double-release.
        /// </summary>
        public void Release<T>(T instance) where T : PoolableBehaviour
        {
            if (isQuitting || instance == null) return;

            IPoolable p = instance;
            // The Gatekeeper: If it's not active, it's already in the pool. Stop here.
            if (!p.IsActiveInternal) return;

            if (p.OriginPool is Pool<T> pool)
            {
                pool.Release(instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        /// <summary>
        /// Safely releases a collection of objects and recycles the list container.
        /// </summary>
        public void ReleaseAll<T>(List<T> instances) where T : PoolableBehaviour
        {
            if (instances == null) return;

            for (int i = instances.Count - 1; i >= 0; i--)
            {
                // Internal Release() handles the IsActive check for each item
                Release(instances[i]);
            }

            ListPool<T>.Release(instances);
        }

        /// <summary>
        /// Returns the number of active objects for a generic key (e.g., a string or ID).
        /// </summary>
        public int GetActiveCount(object key) => pools.TryGetValue(key, out IPool p) ? p.ActiveCount : 0;

        #region Internal Logic

        // TODO: Add a public API for pooling an object from a factory method and not just a prefab
        private T GetInternal<T>(object key, Func<T> createFunc, Vector3 position, Quaternion rotation, bool setActive)
            where T : PoolableBehaviour
        {
            if (!pools.TryGetValue(key, out IPool poolInterface))
            {
                var container = new GameObject($"Pool_{key}");
                container.transform.SetParent(transform);
                poolInterface = new Pool<T>(createFunc, container.transform, defaultPoolSize, maxPoolSize);
                pools[key] = poolInterface;
            }

            if (poolInterface is Pool<T> poolObj)
            {
                T instance = poolObj.Get();
                IPoolable p = instance;
                p.OriginPool = poolObj;
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.gameObject.SetActive(setActive);
                return instance;
            }

            throw new InvalidCastException($"Pool Key Collision: {key} is already registered to a different type.");
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            isQuitting = true;
        }

        #endregion
    }
}