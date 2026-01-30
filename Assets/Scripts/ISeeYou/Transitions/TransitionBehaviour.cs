using System.Collections;
using UnityEngine;

namespace ISeeYou
{
    /// <summary>
    /// Base class for a single animation sequence (Intro/Outro).
    /// </summary>
    public abstract class TransitionBehaviour<T> : MonoBehaviour
    {
        protected T Owner { get; private set; }

        public void Initialize(T owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// The main animation routine. 
        /// Yield returns until the animation is fully complete.
        /// </summary>
        public abstract IEnumerator Play();
    }
}