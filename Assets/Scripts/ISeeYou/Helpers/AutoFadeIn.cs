using PrimeTween;
using UnityEngine;

namespace ISeeYou
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AutoFadeIn : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float startDelay = 0f;

        [Header("Fade Settings")]
        [SerializeField, Range(0f, 1f)] private float startAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float targetAlpha = 1f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        private CanvasGroup canvasGroup;
        private Tween delayTween;
        private Tween fadeTween;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
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
            Stop();

            // Instant setup to prevent visual glitching (flash of 100% opacity)
            canvasGroup.alpha = startAlpha;

            if (startDelay > 0f)
            {
                delayTween = Tween.Delay(startDelay, BeginFadeIn);
            }
            else
            {
                BeginFadeIn();
            }
        }

        public void Stop()
        {
            if (delayTween.isAlive) delayTween.Stop();
            if (fadeTween.isAlive) fadeTween.Stop();

            // Ensure we end up at the target visibility if interrupted, 
            // or reset to start if you prefer. Here, we snap to target.
            canvasGroup.alpha = targetAlpha;
        }

        private void BeginFadeIn()
        {
            // Simple linear tween from current (startAlpha) to targetAlpha
            fadeTween = Tween.Alpha(canvasGroup, targetAlpha, duration, ease);
        }
    }
}