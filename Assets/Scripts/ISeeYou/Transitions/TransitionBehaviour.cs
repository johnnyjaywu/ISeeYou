using System;
using System.Collections;
using UnityEngine;

namespace ISeeYou
{
    public abstract class TransitionBehaviour : MonoBehaviour
    {
        public abstract IEnumerator StartTransition();
    }
}