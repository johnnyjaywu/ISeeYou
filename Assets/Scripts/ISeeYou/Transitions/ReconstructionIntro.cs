using System.Collections;
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class ReconstructionIntro : TransitionBehaviour<ConversationManager>
    {
        [Header("Animation Settings")]
        [Tooltip("The curve for the bubble pulse animation.")]
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1f, 1, 1f);

        [Tooltip("How long the pulse takes.")]
        [SerializeField] private float duration = 0.5f;

        [Header("Audio")]
        [SerializeField] private SoundData phaseStartSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Starting Reconstruction Intro...");

            // 1. Setup
            RectTransform bubble = Owner.SpeechBubble.GetComponent<RectTransform>();
            Vector3 originalScale = Vector3.one;

            // 2. Play Audio
            if (phaseStartSound != null)
            {
                SoundManager.Instance.Play(phaseStartSound);
            }

            // 3. Pulse Animation
            // We scale the bubble slightly to draw the eye
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;

                // Evaluate curve (e.g., 1.0 -> 1.1 -> 1.0)
                float scaleMultiplier = pulseCurve.Evaluate(progress);
                bubble.localScale = originalScale * scaleMultiplier;

                yield return null;
            }

            // 4. Reset
            bubble.localScale = originalScale;
            
            Debug.Log($"[{GetType().Name}] Intro Complete.");
        }

        #if UNITY_EDITOR
        private void Reset()
        {
            // Default "Heartbeat" curve
            pulseCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 1.05f), // Slight expand
                new Keyframe(1f, 1f)
            );
        }
        #endif
    }
}