using System;

namespace ContentContent
{
    /// <summary>
    ///     A generic, lazy, thread-safe singleton pattern for pure C# classes.
    /// </summary>
    /// <typeparam name="T">The type of the class inheriting from this singleton.</typeparam>
    public abstract class Singleton<T> where T : class
    {
        /// <summary>
        ///     The lazy-initialized, thread-safe instance.
        ///     The () => ... part is a factory method that creates the instance.
        ///     Activator.CreateInstance is used to call the private constructor.
        /// </summary>
        private static readonly Lazy<T> instance = new(
            () => (T)Activator.CreateInstance(typeof(T), true),
            true);

        /// <summary>
        ///     Protected constructor to prevent external instantiation.
        ///     Child classes should also have a private or protected constructor.
        /// </summary>
        protected Singleton()
        {
        }

        /// <summary>
        ///     Public accessor for the single instance.
        ///     The first time this is called, the instance will be created.
        /// </summary>
        public static T Instance => instance.Value;
    }
}