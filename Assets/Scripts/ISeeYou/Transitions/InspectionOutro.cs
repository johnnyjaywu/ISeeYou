using System.Collections;
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class InspectionOutro : TransitionBehaviour<ConversationManager>
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 1.5f;

        [Header("Audio")]
        [Tooltip("The final sound of the level (e.g., a long reverb tail or musical resolve).")]
        [SerializeField] private SoundData levelCompleteSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Starting Final Outro...");

            // 1. Audio
            if (levelCompleteSound != null)
            {
                SoundManager.Instance.Play(levelCompleteSound);
            }

            // 2. Prepare for Fade
            // We need a CanvasGroup on the SpeechBubble to fade it and its children efficiently.
            CanvasGroup bubbleGroup = Owner.SpeechBubble.GetComponent<CanvasGroup>();
            if (bubbleGroup == null)
            {
                bubbleGroup = Owner.SpeechBubble.gameObject.AddComponent<CanvasGroup>();
            }

            // 3. Fade Out
            float startAlpha = bubbleGroup.alpha;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeDuration;
                
                bubbleGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }

            bubbleGroup.alpha = 0f;
            
            // 4. Final Cleanup
            Owner.SpeechBubble.gameObject.SetActive(false);

            Debug.Log($"[{GetType().Name}] Level Complete.");
        }
    }
}