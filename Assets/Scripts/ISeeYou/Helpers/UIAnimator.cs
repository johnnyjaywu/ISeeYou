using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;

namespace ISeeYou
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UIAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Header("Scale Settings")]
        [Tooltip("Multiplier for ScaleUp (e.g., 1.1 = 10% larger).")]
        [SerializeField] private float scaleUpMultiplier = 1.1f;

        [Header("Shake Settings")]
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private float shakeFrequency = 10f;

        [Header("Events")]
        [Tooltip("Called generically when the entire Sequence completes.")]
        [SerializeField] private UnityEvent onSequenceComplete;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Sequence currentSequence;
        private Vector3 originalScale;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>
        /// Resets the sequence builder. Call this to force a fresh start.
        /// </summary>
        public UIAnimator ResetChain()
        {
            Stop();
            currentSequence = Sequence.Create();
            return this;
        }

        /// <summary>
        /// Starts the built sequence.
        /// </summary>
        public void Play()
        {
            if (!currentSequence.isAlive)
            {
                currentSequence = Sequence.Create();
            }
            
            // Subscribe the inspector event to the end of the sequence
            currentSequence.OnComplete(HandleInspectorEvent);
        }

        public void Stop()
        {
            if (currentSequence.isAlive)
            {
                currentSequence.Complete();
            }
        }

        // -----------------------
        // Chained Methods
        // -----------------------

        public UIAnimator FadeIn()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.Alpha(canvasGroup, 1f, duration, ease));
            return this;
        }

        public UIAnimator FadeOut()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.Alpha(canvasGroup, 0f, duration, ease));
            return this;
        }

        public UIAnimator ScaleIn()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.Scale(rectTransform, Vector3.one, duration, ease));
            return this;
        }

        public UIAnimator ScaleOut()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.Scale(rectTransform, Vector3.zero, duration, ease));
            return this;
        }

        public UIAnimator ScaleUp()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.Scale(rectTransform, originalScale * scaleUpMultiplier, duration, ease));
            return this;
        }

        public UIAnimator ScaleBack()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.Scale(rectTransform, originalScale, duration, ease));
            return this;
        }

        public UIAnimator Shake()
        {
            EnsureSequence();
            currentSequence.Chain(Tween.ShakeLocalPosition(rectTransform, Vector3.one * shakeStrength, duration, shakeFrequency, true, ease));
            return this;
        }

        public UIAnimator Delay(float seconds)
        {
            EnsureSequence();
            currentSequence.ChainDelay(seconds);
            return this;
        }

        /// <summary>
        /// Adds a callback to the sequence at the current point in the chain.
        /// </summary>
        public UIAnimator OnFinish(Action callback)
        {
            EnsureSequence();
            currentSequence.ChainCallback(callback);
            return this;
        }

        // -----------------------
        // Helpers
        // -----------------------

        private void EnsureSequence()
        {
            if (!currentSequence.isAlive)
            {
                currentSequence = Sequence.Create();
            }
        }

        private void HandleInspectorEvent()
        {
            onSequenceComplete?.Invoke();
        }
    }
}