using PrimeTween;
using UnityEngine;

namespace ISeeYou
{
    [RequireComponent(typeof(RectTransform))]
    public class AutoShake : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float startDelay = 0f; // New delay parameter

        [Header("Shiver Settings")]
        [SerializeField] private float strength = 3f;
        [SerializeField] private float frequency = 20f;
        [SerializeField] private float pulseDuration = 0.5f;

        [Header("Type")]
        [SerializeField] private bool shakePosition = true;
        [SerializeField] private bool shakeRotation = true;

        private RectTransform targetRect;
        private Vector3 originalPos;
        private Quaternion originalRot;

        // Tracks the initial start delay
        private Tween delayTween;
        // Tracks the active effects
        private Tween positionTween;
        private Tween rotationTween;

        private void Awake()
        {
            targetRect = GetComponent<RectTransform>();
            originalPos = targetRect.anchoredPosition3D;
            originalRot = targetRect.localRotation;
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
            // Reset everything before starting
            Stop();

            if (startDelay > 0f)
            {
                // Create a delay timer. If completed, start the actual effects.
                // We store this in 'delayTween' so Stop() can cancel the wait if needed.
                delayTween = Tween.Delay(startDelay, BeginShaking);
            }
            else
            {
                BeginShaking();
            }
        }

        public void Stop()
        {
            // 1. Kill the start delay if it's currently waiting
            if (delayTween.isAlive) 
            {
                delayTween.Stop();
            }

            // 2. Kill active effects
            if (positionTween.isAlive) 
            {
                positionTween.Stop();
            }

            if (rotationTween.isAlive) 
            {
                rotationTween.Stop();
            }

            // 3. Reset to original state
            targetRect.anchoredPosition3D = originalPos;
            targetRect.localRotation = originalRot;
        }

        // Internal method to kick off the loops after the delay (if any)
        private void BeginShaking()
        {
            if (shakePosition) 
            {
                StartPositionShake();
            }

            if (shakeRotation) 
            {
                StartRotationShake();
            }
        }

        private void StartPositionShake()
        {
            positionTween = Tween.ShakeLocalPosition(targetRect, Vector3.one * strength, pulseDuration, frequency);
            positionTween.OnComplete(StartPositionShake);
        }

        private void StartRotationShake()
        {
            rotationTween = Tween.ShakeLocalRotation(targetRect, Vector3.one * strength, pulseDuration, frequency);
            rotationTween.OnComplete(StartRotationShake);
        }
    }
}