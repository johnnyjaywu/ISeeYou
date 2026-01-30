using System.Collections;
using System.Collections.Generic;
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class FilteringIntro : TransitionBehaviour<ConversationManager>
    {
        [Header("Animation Settings")]
        [Tooltip("Time between each word appearing")]
        [SerializeField] private float spawnInterval = 0.1f;
        
        [Tooltip("Variance in the spawn interval (random +/-)")]
        [SerializeField] private float intervalJitter = 0.05f;

        [Tooltip("How long the scale-up animation takes for a single word")]
        [SerializeField] private float scaleDuration = 0.4f;

        [Tooltip("The animation curve for the 'Pop' effect. Recommended: Starts at 0, overshoots to 1.2, settles at 1.")]
        [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        [SerializeField] private SoundData popSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Starting Intro Sequence...");

            // 1. Grab the words that were just spawned by the Manager
            List<Words> words = Owner.WordsList;

            // 2. Immediately hide them all and reset scale to 0
            // Deactivating the gameObject ensures Physics2D doesn't run while hidden
            foreach (var word in words)
            {
                if (word != null)
                {
                    word.transform.localScale = Vector3.zero;
                    word.gameObject.SetActive(false);
                }
            }

            // 3. Reveal loop
            // We iterate through the list and bring them to life one by one
            foreach (var word in words)
            {
                if (word == null) continue;

                // A. Activate
                word.gameObject.SetActive(true);
                
                // B. Start the individual pop animation (fire and forget coroutine)
                StartCoroutine(AnimateWordPop(word.transform));

                // C. Play Sound
                if (popSound != null)
                {
                    SoundManager.Instance.Play(popSound, word.transform.position);
                }

                // D. Wait for next interval
                float waitTime = spawnInterval + Random.Range(-intervalJitter, intervalJitter);
                yield return new WaitForSeconds(Mathf.Max(0.01f, waitTime));
            }

            // 4. Buffer at the end to ensure the player catches up before control unlocks
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"[{GetType().Name}] Sequence Complete.");
        }

        private IEnumerator AnimateWordPop(Transform target)
        {
            float timer = 0f;
            
            while (timer < scaleDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / scaleDuration;
                
                // Evaluate the curve to get the "bouncy" scale value
                float scale = popCurve.Evaluate(progress);
                
                if (target != null)
                {
                    target.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }

            // Ensure we land perfectly on 1
            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        // Editor Helper: Set a nice default curve if none exists
#if UNITY_EDITOR
        private void Reset()
        {
            // Create a simple "Overshoot" curve (Elastic Out feel)
            popCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.7f, 1.1f), // Overshoot
                new Keyframe(1f, 1f)      // Settle
            );
        }
#endif
    }
}