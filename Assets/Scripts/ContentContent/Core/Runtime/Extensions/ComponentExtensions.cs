using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ContentContent
{
    /// <summary>
    ///     Extensions methods for the Unity Component class.
    ///     This also includes some component-related extensions for the GameObject class.
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        ///     Ensure that a component of type <typeparamref name="T" /> exists on the game object.
        ///     If it doesn't exist, creates it.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="component">
        ///     A component on the game object for which a component of type <typeparamref name="T" /> should
        ///     exist.
        /// </param>
        /// <returns>The component that was retrieved or created.</returns>
        public static T EnsureComponent<T>(this Component component) where T : Component
        {
            return EnsureComponent<T>(component.gameObject);
        }

        /// <summary>
        ///     Find the first component of type <typeparamref name="T" /> in the ancestors of the game object of the specified
        ///     component.
        /// </summary>
        /// <typeparam name="T">Type of component to find.</typeparam>
        /// <param name="component">Component for which its game object's ancestors must be considered.</param>
        /// <param name="includeSelf">Indicates whether the specified game object should be included.</param>
        /// <returns>The component of type <typeparamref name="T" />. Null if none was found.</returns>
        public static T FindAncestorComponent<T>(this Component component, bool includeSelf = true) where T : Component
        {
            return component.transform.FindAncestorComponent<T>(includeSelf);
        }

        /// <summary>
        ///     Ensure that a component of type <typeparamref name="T" /> exists on the game object.
        ///     If it doesn't exist, creates it.
        /// </summary>
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <param name="gameObject">Game object on which component should be.</param>
        /// <returns>The component that was retrieved or created.</returns>
        /// <remarks>
        ///     This extension has to remain in this class as it is required by the <see cref="EnsureComponent{T}(Component)" />
        ///     method
        /// </remarks>
        public static T EnsureComponent<T>(this GameObject gameObject) where T : Component
        {
            var foundComponent = gameObject.GetComponent<T>();
            return foundComponent == null ? gameObject.AddComponent<T>() : foundComponent;
        }

        /// <summary>
        ///     Ensure that a component of type exists on the game object.
        ///     If it doesn't exist, creates it.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="component">A component on the game object for which a component of type should exist.</param>
        /// <returns>The component that was retrieved or created.</returns>
        public static Component EnsureComponent(this GameObject gameObject, Type component)
        {
            Component foundComponent = gameObject.GetComponent(component);
            return foundComponent == null ? gameObject.AddComponent(component) : foundComponent;
        }

        /// <summary>
        ///     Ensure that a component of type exists on the game object.
        ///     If it doesn't exist, creates it.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="type">the type of <see cref="Component" /> on the game object for which a component of type should exist.</param>
        /// <returns>The component that was retrieved or created.</returns>
        public static Component EnsureComponent(this Component component, Type type)
        {
            GameObject gameObject = component.gameObject;
            Component foundComponent = gameObject.GetComponent(type);
            return foundComponent == null ? gameObject.AddComponent(type) : foundComponent;
        }

        /// <summary>
        ///     Ensure that a component of type <typeparamref name="T" /> is removed and destroyed if it
        ///     exists on the game object.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject" /> to remove the component from.</param>
        public static void EnsureComponentDestroyed<T>(this GameObject gameObject) where T : Component
        {
            var foundComponent = gameObject.GetComponent<T>();

            if (foundComponent != null) foundComponent.Destroy();
        }

        /// <summary>
        ///     Ensure that a component of type <paramref name="component" /> is removed and destroyed if it
        ///     exists on the game object.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject" /> to remove the component from.</param>
        /// <param name="component">A component on the game object for which a component of type should be removed.</param>
        public static void EnsureComponentDestroyed(this GameObject gameObject, Type component)
        {
            Component foundComponent = gameObject.GetComponent(component);

            if (foundComponent != null) foundComponent.Destroy();
        }

        /// <summary>
        ///     Validates the <see cref="Component" /> reference.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Component" />.</typeparam>
        /// <param name="component">The target <see cref="Component" />.</param>
        /// <param name="callerName">The <see cref="CallerFilePathAttribute" /> fills in this information.</param>
        public static void Validate<T>(this T component, [CallerFilePath] string callerName = "") where T : Component
        {
            if (component == null)
                throw new MissingReferenceException(
                    $"{Path.GetFileNameWithoutExtension(callerName)} expected a {typeof(T).Name}");
        }

        /// <summary>
        ///     Sets the <see cref="GameObject" /> this <see cref="Component" /> is attached to, to the specified state.
        /// </summary>
        /// <param name="component">The target <see cref="Component" /></param>
        /// <param name="isActive">The <see cref="GameObject" />'s active state to set.</param>
        public static void SetActive(this Component component, bool isActive)
        {
            if (component.gameObject.activeSelf != isActive) component.gameObject.SetActive(isActive);
        }
        
        public static bool HasComponent<T>(this Component component) where T : class
        {
            return component.GetComponent<T>() != null;
        }

        public static bool TryGetComponent<T>(this Component component, out T result) where T : class
        {
            result = component.GetComponent<T>();
            return result != null;
        }

        /// <summary>
        /// Tries to find the component in children (including self) first. 
        /// If not found, searches in parents (including self).
        /// </summary>
        public static T GetComponentInParentOrChildren<T>(this Component source) where T : class
        {
            if (source == null) return null;

            // Check self and children first
            T result = source.GetComponentInChildren<T>();
            if (result != null) return result;

            // Fallback to parents
            return source.GetComponentInParent<T>();
        }

        /// <summary>
        /// Tries to find the component in children (including self) first. 
        /// If not found, searches in parents (including self).
        /// Includes inactive children in the search.
        /// </summary>
        public static T GetComponentInParentOrChildren<T>(this Component source, bool includeInactive) where T : class
        {
            if (source == null) return null;

            // Check self and children first
            T result = source.GetComponentInChildren<T>(includeInactive);
            if (result != null) return result;

            // Fallback to parents
            return source.GetComponentInParent<T>();
        }
    }
}