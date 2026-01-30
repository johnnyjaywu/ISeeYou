using System.Collections;
using ContentContent.Audio; 
using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou
{
    public class ReconstructionOutro : TransitionBehaviour<ConversationManager>
    {
        [Header("Animation Settings")]
        [Tooltip("Color to flash the text background or text itself")]
        [SerializeField] private Color flashColor = Color.white;
        
        [Tooltip("How long the flash/confirmation lasts")]
        [SerializeField] private float duration = 0.5f;

        [Header("Audio")]
        [SerializeField] private SoundData sentenceLockedSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Starting Reconstruction Outro...");

            // 1. Play Audio
            if (sentenceLockedSound != null)
            {
                SoundManager.Instance.Play(sentenceLockedSound);
            }

            // 2. Lock Visuals
            // We want to make the words feel "solid" now.
            // Disable the Draggable components visually (logic is already handled by State)
            var draggables = Owner.SpeechBubble.GetComponentsInChildren<Draggable>();
            foreach (var drag in draggables)
            {
                // Optional: visual change to show they are locked
                if (drag.TryGetComponent(out Image img))
                {
                    img.color = new Color(0.95f, 0.95f, 0.95f); // Subtle grey lock
                }
            }

            // 3. Flash Effect
            // We flash the background of the speech bubble to confirm success
            Image bubbleBg = Owner.SpeechBubble.GetComponent<Image>();
            Color originalColor = bubbleBg.color;
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;

                // Ping-pong lerp: Normal -> Flash -> Normal
                // 0 -> 1 -> 0
                float t = Mathf.PingPong(progress * 2, 1);
                
                bubbleBg.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }

            bubbleBg.color = originalColor;

            Debug.Log($"[{GetType().Name}] Outro Complete.");
        }
    }
}