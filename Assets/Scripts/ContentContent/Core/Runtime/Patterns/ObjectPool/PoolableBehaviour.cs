using UnityEngine;

namespace ContentContent
{
    /// <summary>
    /// A standardized base class for poolable objects. 
    /// Handles the internal state tracking required by the ObjectPoolManager.
    /// </summary>
    public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
    {
        bool IPoolable.IsActiveInternal { get; set; }
        object IPoolable.OriginPool { get; set; }
        void IPoolable.OnGetFromPool() => OnSpawn();
        void IPoolable.OnReleaseToPool() => OnDespawn();

        /// <summary>
        /// A read-only check for derived classes.
        /// </summary>
        protected bool IsActive => ((IPoolable)this).IsActiveInternal;
        
        /// <summary>
        /// Called when the object is retrieved from the pool. Use this instead of OnEnable.
        /// </summary>
        protected virtual void OnSpawn() { }

        /// <summary>
        /// Called when the object is returned to the pool. Use this instead of OnDisable.
        /// </summary>
        protected virtual void OnDespawn() { }

        /// <summary>
        /// Convenience method for an object to return itself to the pool.
        /// </summary>
        public void Despawn() => ObjectPoolManager.Instance.Release(this);
    }
}