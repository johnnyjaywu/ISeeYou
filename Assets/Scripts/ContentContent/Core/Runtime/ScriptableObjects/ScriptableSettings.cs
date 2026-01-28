using System;
using System.Linq;
using UnityEngine;

namespace ContentContent
{
    public abstract class ScriptableSettings : ScriptableObject
    {
        protected static event Action OnReload;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadDomain()
        {
            OnReload?.Invoke();
        }
    }

    public abstract class ScriptableSettings<T> : ScriptableSettings where T : ScriptableSettings<T>
    {
        private static T _instance;

        static ScriptableSettings()
        {
            OnReload += ReloadDomain;
        }

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();

                // Note: If null here, the Editor script below handles the warning/creation
                return _instance;
            }
        }

        private static void ReloadDomain()
        {
            _instance = null;
            T found = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
            if (found != null) _instance = found;
        }
    }
}