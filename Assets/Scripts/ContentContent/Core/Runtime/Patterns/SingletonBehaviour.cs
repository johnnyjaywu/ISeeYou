using System;
using UnityEngine;

namespace ContentContent
{
    /// <summary>
    ///     A non-generic helper to track application state.
    ///     This fixes issues with Fast Enter Play Mode and Static Resets.
    /// </summary>
    internal static class SingletonState
    {
        public static bool IsQuitting { get; set; }
        public static event Action OnReset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            IsQuitting = false;
            OnReset?.Invoke();
        }
    }

    /// <summary>
    ///     A generic, robust MonoBehaviour singleton.
    ///     By default, it is persistent (DontDestroyOnLoad).
    ///     You can set 'isPersistent' to false in the Inspector
    ///     to create a scene-specific (non-persistent) singleton.
    /// </summary>
    /// <typeparam name="T">The type of the component inheriting from this class.</typeparam>
    public class SingletonBehaviour<T> : MonoBehaviour where T : Component
    {
        private static T instance;

        static SingletonBehaviour()
        {
            SingletonState.OnReset += ResetInstance;
        }

        /// <summary>
        ///     Controls whether this singleton will persist across scene loads.
        ///     Set this in the Inspector. It defaults to true.
        /// </summary>
        [Header("Singleton Settings")]
        [Tooltip("If true, this object will persist across scene loads. " +
                 "Set to false for scene-specific singletons.")]
        [SerializeField]
        private bool isPersistent = true;

        /// <summary>
        ///     The static accessor for the singleton instance.
        ///     It will find or create the instance if it doesn't exist.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (SingletonState.IsQuitting)
                    // Debug.LogWarning($"[Singleton] Instance of {typeof(T)} requested while quitting. Returning null.");
                    return null;

                // 1. Check if the instance is already set
                if (instance == null)
                {
                    // 2. If not, try to find an existing instance in the scene
                    instance = FindAnyObjectByType<T>();

                    // 3. If still not found, create a new one
                    if (instance == null)
                        // Debug.Log($"No instance of {typeof(T).Name} found. Auto-generating one.");
                        instance = new GameObject(typeof(T).Name + " Auto-Generated").AddComponent<T>();
                }

                // 4. Return the (now guaranteed) instance
                return instance;
            }
        }

        /// <summary>
        ///     Unity's Awake method. This is where the singleton pattern is enforced.
        /// </summary>
        protected virtual void Awake()
        {
            if (!Application.isPlaying) return;

            // This logic ensures that only one instance of this singleton ever exists.
            if (instance == null)
            {
                // This is the first instance.
                instance = this as T;

                // Apply persistence based on the Inspector setting
                if (isPersistent) DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                // This is a duplicate instance.
                Debug.LogWarning($"Duplicate instance of {typeof(T).Name} found on '{gameObject.name}'. " +
                                 "Destroying this duplicate.");

                // Destroy the duplicate.
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this as T) instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            SingletonState.IsQuitting = true;
        }

        // IMPORTANT: We need to reset the local static instance for this specific T
        private static void ResetInstance()
        {
            instance = null;
        }
    }
}