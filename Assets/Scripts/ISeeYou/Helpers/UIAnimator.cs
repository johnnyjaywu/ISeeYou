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
        
        // Flag to track if we are in the middle of building a chain.
        private bool isBuildingSequence = false;

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
        /// Explicitly clears old sequences and starts a fresh, paused builder.
        /// </summary>
        public UIAnimator ResetChain()
        {
            Stop();
            EnsureSequence(); // Will create and pause
            return this;
        }

        /// <summary>
        /// Unpauses the sequence and begins execution.
        /// </summary>
        public void Play()
        {
            if (!currentSequence.isAlive)
            {
                // Edge case: Play called with no chain built. Create an empty one to satisfy logic.
                currentSequence = Sequence.Create();
            }

            // UNPAUSE the sequence to let it run
            currentSequence.isPaused = false;

            // Mark building as finished. The next method call will trigger a Stop() and new Sequence.
            isBuildingSequence = false;

            currentSequence.OnComplete(HandleInspectorEvent);
        }

        /// <summary>
        /// Immediately stops and completes any active sequence.
        /// </summary>
        public void Stop()
        {
            if (currentSequence.isAlive)
            {
                currentSequence.Complete();
            }
            isBuildingSequence = false;
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

        public UIAnimator OnFinish(Action callback)
        {
            EnsureSequence();
            currentSequence.ChainCallback(callback);
            return this;
        }

        // -----------------------
        // Helpers
        // -----------------------

        /// <summary>
        /// Ensures a sequence exists and is PAUSED so we can build onto it.
        /// </summary>
        private void EnsureSequence()
        {
            if (!isBuildingSequence)
            {
                // We are starting a brand new chain. 
                // Stop any old running sequence first.
                Stop();
                
                // Create a new sequence and PAUSE it immediately.
                // It will sit waiting for Play() to set isPaused = false.
                currentSequence = Sequence.Create();
                currentSequence.isPaused = true;
                
                isBuildingSequence = true;
            }
        }

        private void HandleInspectorEvent()
        {
            onSequenceComplete?.Invoke();
        }
    }
}