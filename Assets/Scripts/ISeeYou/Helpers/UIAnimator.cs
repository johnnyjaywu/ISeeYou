using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;

namespace ISeeYou
{
    public enum AnimLayer
    {
        Interaction = 0, // Hovers, Clicks, Shakes (Transient)
        Lifecycle = 1    // Show, Hide, Fade (State Changes)
    }

    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UIAnimator : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private float defaultDuration = 0.3f;
        [SerializeField] private Ease defaultEase = Ease.OutQuad;

        [Header("Scale Settings")]
        [SerializeField] private float scaleUpMultiplier = 1.1f;

        [Header("Shake Settings")]
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private float shakeFrequency = 10f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalScale;

        // TRACKING: We now keep a dictionary of sequences per Layer
        private Dictionary<AnimLayer, Sequence> activeSequences = new Dictionary<AnimLayer, Sequence>();
        
        // BUILDER STATE: Which layer are we currently building?
        private AnimLayer currentBuildLayer = AnimLayer.Interaction;
        private Sequence currentBuilder;
        private bool isBuilding = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
        }

        private void OnDisable()
        {
            StopAll();
        }

        // -------------------------------------------------------------------
        // 1. Channel Selection
        // -------------------------------------------------------------------

        /// <summary>
        /// Starts a command chain on a specific layer.
        /// Use 'Lifecycle' for Fades/Delays that shouldn't be interrupted by Hovers.
        /// Use 'Interaction' for Hovers/Clicks.
        /// </summary>
        public UIAnimator On(AnimLayer layer)
        {
            // If we were already building a different layer and didn't finish, clean it up
            if (isBuilding && currentBuildLayer != layer)
            {
                Debug.LogWarning($"[UIAnimator] Switched layers without playing previous chain on {currentBuildLayer}. Discarding.");
                currentBuilder.Stop(); 
            }

            currentBuildLayer = layer;
            
            // Stop ONLY the existing sequence on this specific layer
            Stop(layer);

            // Create new paused sequence for this layer
            currentBuilder = Sequence.Create();
            currentBuilder.isPaused = true;
            
            // Store it in our dictionary so we can manage it later
            activeSequences[layer] = currentBuilder;
            isBuilding = true;

            return this;
        }

        /// <summary>
        /// Default entry point (Uses Interaction Layer by default).
        /// </summary>
        public UIAnimator Begin() => On(AnimLayer.Interaction);

        // -------------------------------------------------------------------
        // 2. Commands (Builder Pattern)
        // -------------------------------------------------------------------

        public UIAnimator FadeIn(float? duration = null)
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.Alpha(canvasGroup, 1f, duration ?? defaultDuration, defaultEase));
            return this;
        }

        public UIAnimator FadeOut(float? duration = null)
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.Alpha(canvasGroup, 0f, duration ?? defaultDuration, defaultEase));
            return this;
        }

        public UIAnimator ScaleIn(float? duration = null)
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.Scale(rectTransform, originalScale, duration ?? defaultDuration, defaultEase));
            return this;
        }

        public UIAnimator ScaleOut(float? duration = null)
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.Scale(rectTransform, Vector3.zero, duration ?? defaultDuration, defaultEase));
            return this;
        }

        public UIAnimator ScaleUp(float? duration = null)
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.Scale(rectTransform, originalScale * scaleUpMultiplier, duration ?? defaultDuration, defaultEase));
            return this;
        }

        public UIAnimator ScaleBack(float? duration = null)
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.Scale(rectTransform, originalScale, duration ?? defaultDuration, defaultEase));
            return this;
        }

        public UIAnimator Shake()
        {
            ValidateBuilder();
            currentBuilder.Chain(Tween.ShakeLocalPosition(rectTransform, Vector3.one * shakeStrength, defaultDuration, shakeFrequency, true, defaultEase));
            return this;
        }

        public UIAnimator Delay(float seconds)
        {
            ValidateBuilder();
            currentBuilder.ChainDelay(seconds);
            return this;
        }

        public UIAnimator OnFinish(Action callback)
        {
            ValidateBuilder();
            currentBuilder.ChainCallback(callback);
            return this;
        }

        // -------------------------------------------------------------------
        // 3. Execution
        // -------------------------------------------------------------------

        public void Play()
        {
            if (!isBuilding) return;

            // Unpause the sequence we just built
            if (activeSequences.TryGetValue(currentBuildLayer, out var seq) && seq.isAlive)
            {
                seq.isPaused = false;
            }
            
            isBuilding = false;
        }

        public void Stop(AnimLayer layer)
        {
            if (activeSequences.TryGetValue(layer, out var seq))
            {
                if (seq.isAlive) seq.Stop();
                activeSequences.Remove(layer);
            }
        }

        public void StopAll()
        {
            foreach (var kvp in activeSequences)
            {
                if (kvp.Value.isAlive) kvp.Value.Stop();
            }
            activeSequences.Clear();
            isBuilding = false;
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private void ValidateBuilder()
        {
            // Auto-start a default builder if the user forgot to call On()
            if (!isBuilding)
            {
                On(AnimLayer.Interaction);
            }
        }
    }
}