using PrimeTween;
using UnityEngine;

namespace ISeeYou
{
    [RequireComponent(typeof(RectTransform))]
    public class AutoPulse : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float startDelay = 0f;

        [Header("Pulse Settings")]
        [SerializeField] private float scaleMultiplier = 1.02f;
        [SerializeField] private float oneBeatDuration = 1.2f;
        [SerializeField] private Ease pulseEase = Ease.InOutSine;

        private RectTransform targetRect;
        private Vector3 originalScale;
        private Tween pulseTween;

        private void Awake()
        {
            targetRect = GetComponent<RectTransform>();
            originalScale = targetRect.localScale;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Play()
        {
            // Always reset before starting to prevent compounding scales
            Stop();

            // PrimeTween Optimization: 
            // We pass 'cycles', 'cycleMode', and 'startDelay' directly as arguments.
            // This is cleaner and more performant than chaining .SetCycles().
            pulseTween = Tween.Scale(
                target: targetRect, 
                endValue: originalScale * scaleMultiplier, 
                duration: oneBeatDuration, 
                ease: pulseEase, 
                cycles: -1, 
                cycleMode: CycleMode.Yoyo, 
                startDelay: startDelay
            );
        }

        public void Stop()
        {
            if (pulseTween.isAlive)
            {
                pulseTween.Stop();
            }

            // Hard reset to ensure we don't get stuck in a scaled state
            targetRect.localScale = originalScale;
        }
        
        /// <summary>
        /// Updates the pulse settings live (useful if changed via script/inspector at runtime).
        /// </summary>
        public void Refresh()
        {
            if (pulseTween.isAlive)
            {
                Play();
            }
        }
    }
}