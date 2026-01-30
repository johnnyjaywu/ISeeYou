using System.Collections;
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class RevelationIntro : TransitionBehaviour<ConversationManager>
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeOutDuration = 1.5f;

        [Header("Audio")]
        [SerializeField] private SoundData truthRevealSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Fading out Speech Bubble...");

            // 1. Play Audio
            if (truthRevealSound != null)
            {
                SoundManager.Instance.Play(truthRevealSound);
            }

            // 2. Fade Out Bubble
            // We fade the entire CanvasGroup, which takes all words inside with it.
            CanvasGroup bubbleGroup = Owner.BubbleCanvasGroup;
            
            if (bubbleGroup != null)
            {
                float startAlpha = bubbleGroup.alpha;
                float timer = 0f;

                while (timer < fadeOutDuration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / fadeOutDuration;
                    
                    bubbleGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                    yield return null;
                }

                bubbleGroup.alpha = 0f;
                bubbleGroup.blocksRaycasts = false; // Ensure no accidental clicks
            }
            else
            {
                Debug.LogWarning("BubbleCanvasGroup is missing in ConversationManager!");
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}