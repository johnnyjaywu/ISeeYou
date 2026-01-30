using System.Collections;
using System.Collections.Generic;
using ContentContent.Audio;
using UnityEngine;
using UnityEngine.UI; 

namespace ISeeYou
{
    public class FilteringOutro : TransitionBehaviour<ConversationManager>
    {
        [Header("Animation Settings")]
        [Tooltip("How long it takes for the noise words to disappear")]
        [SerializeField] private float fadeDuration = 0.6f;

        [Tooltip("Curve for scaling down the noise words (starts at 1, ends at 0)")]
        [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Audio")]
        [Tooltip("Sound played when the noise clears away (e.g., a 'whoosh' or vacuum sound)")]
        [SerializeField] private SoundData noiseClearSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Starting Outro Sequence...");

            // 1. Identify Noise vs Truth
            // We iterate through all words tracked by the Manager.
            // If a word is NOT a child of the Speech Bubble, it is considered Noise.
            var allWords = Owner.WordsList;
            var noiseWords = new List<Words>();
            var keptWords = new List<Words>();

            Transform bubbleTransform = Owner.SpeechBubble.transform;

            foreach (var word in allWords)
            {
                if (word == null) continue;

                // Check hierarchy to see if the player successfully dropped it in the zone
                if (word.transform.IsChildOf(bubbleTransform))
                {
                    keptWords.Add(word);
                }
                else
                {
                    noiseWords.Add(word);
                }
            }

            // 2. Play Audio
            if (noiseClearSound != null)
            {
                SoundManager.Instance.Play(noiseClearSound);
            }

            // 3. Animate Noise Away
            // We run a single loop to animate all noise words simultaneously.
            // This is more performant than starting a coroutine for every single word.
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / fadeDuration);
                
                // Evaluate curves
                float scale = shrinkCurve.Evaluate(progress);
                Vector3 targetScale = Vector3.one * scale;

                foreach (var word in noiseWords)
                {
                    if (word != null)
                    {
                        word.transform.localScale = targetScale;
                        
                        // Fade Alpha if the component exists (It is required by Draggable)
                        var cg = word.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = scale;
                    }
                }

                yield return null;
            }

            // 4. Final Cleanup
            // Disable the noise objects completely so they don't interfere with raycasts/physics
            foreach (var word in noiseWords)
            {
                if (word != null)
                {
                    word.gameObject.SetActive(false);
                }
            }
            
            // 5. Stabilize the Bubble
            // Ensure the kept words are snapped nicely in the layout for Phase 2
            LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.SpeechBubble.GetComponent<RectTransform>());

            // Short pause for emphasis before state change
            yield return new WaitForSeconds(0.2f);

            Debug.Log($"[{GetType().Name}] Outro Complete. Noise cleared.");
        }
    }
}