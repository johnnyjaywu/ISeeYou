using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISeeYou
{
    /// <summary>
    /// Base class for managing a collection of TransitionBehaviours.
    /// Allows playing specific transitions by Type.
    /// </summary>
    /// <typeparam name="T">The type of the owner/manager (e.g., ConversationManager)</typeparam>
    public abstract class TransitionController<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Tooltip("The manager that these transitions control")]
        [SerializeField] protected T owner;
        
        [Tooltip("List of all available transition components on this object or children")]
        [SerializeField] protected List<TransitionBehaviour<T>> transitions = new();

        /// <summary>
        /// Plays the transition of the specified type TTransition.
        /// </summary>
        /// <typeparam name="TTransition">The specific transition class to play (e.g., FilteringIntro)</typeparam>
        public IEnumerator Play<TTransition>() where TTransition : TransitionBehaviour<T>
        {
            var transition = GetTransition<TTransition>();
            
            if (transition == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Could not find transition of type {typeof(TTransition).Name}");
                yield break;
            }

            // Inject dependency if needed
            transition.Initialize(owner);

            // Execute the animation coroutine
            yield return transition.Play();
        }

        /// <summary>
        /// Helper to retrieve a specific transition reference.
        /// </summary>
        public TTransition GetTransition<TTransition>() where TTransition : TransitionBehaviour<T>
        {
            return transitions.OfType<TTransition>().FirstOrDefault();
        }

        // Optional: Auto-discovery in Editor
#if UNITY_EDITOR
        protected virtual void Reset()
        {
            OnValidate();
        }
        
        protected virtual void OnValidate()
        {
            if (owner == null) owner = GetComponentInParent<T>();
            
            // Auto-populate list from children
            var found = GetComponentsInChildren<TransitionBehaviour<T>>(true);
            foreach (var t in found)
            {
                if (!transitions.Contains(t)) transitions.Add(t);
            }
        }
#endif
    }
}