using System.Collections;
using ContentContent;
using ContentContent.Audio;
using UnityEngine;

namespace ISeeYou
{
    public class InspectionIntro : TransitionBehaviour<ConversationManager>
    {
        [Header("Atmosphere")]
        [Tooltip("Optional: A UI Panel (black/dark) that fades in to dim the background.")]
        [SerializeField] private CanvasGroup backgroundDimmer;

        [SerializeField] private float dimDuration = 1.0f;
        [SerializeField] private float targetDimAlpha = 0.7f;

        [Header("Text Hint")]
        [Tooltip("Color to flash the text to indicate it is interactable.")]
        [SerializeField] private Color hintColor = new Color(1f, 1f, 0.8f); // Soft yellow

        [SerializeField] private float hintDuration = 0.5f;

        [Header("Audio")]
        [Tooltip("A quiet, intimate sound (e.g., a breath, a hum, or room tone shift).")]
        [SerializeField] private SoundData inspectionStartSound;

        public override IEnumerator Play()
        {
            Debug.Log($"[{GetType().Name}] Starting Inspection Intro...");

            // 1. Audio
            if (inspectionStartSound != null)
            {
                SoundManager.Instance.Play(inspectionStartSound);
            }

            // 2. Dim Lights (Run in parallel, but we wait for it manually)
            CoroutineHandle dimHandle = null;
            if (backgroundDimmer != null)
            {
                backgroundDimmer.gameObject.SetActive(true);
                backgroundDimmer.alpha = 0f;
                dimHandle = FadeRoutine(backgroundDimmer, 0f, targetDimAlpha, dimDuration).Run();
            }

            // 3. Flash Words (The Hint)
            // We flash all the words in the bubble to show they are active
            var words = Owner.SpeechBubble.GetComponentsInChildren<Words>();

            float timer = 0f;
            while (timer < hintDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / hintDuration;
                float pingPong = Mathf.PingPong(progress * 2, 1); // 0 -> 1 -> 0

                foreach (var word in words)
                {
                    // Access the TextMeshPro component via the Words script (assuming public or we get it)
                    // Since Words.cs doesn't expose the TMP component directly, we use SetVisualState hack 
                    // or GetComponent. For safety, let's assume we can GetComponent.
                    if (word.TryGetComponent(out TMPro.TextMeshProUGUI tmp))
                    {
                        tmp.color = Color.Lerp(Color.white, hintColor, pingPong);
                    }
                }

                yield return null;
            }

            // Ensure words are reset to white
            foreach (var word in words)
            {
                if (word.TryGetComponent(out TMPro.TextMeshProUGUI tmp)) tmp.color = Color.white;
            }

            // Wait for dimmer if it's still going
            if (dimHandle is { IsRunning: true }) yield return dimHandle;

            Debug.Log($"[{GetType().Name}] Intro Complete.");
        }

        private IEnumerator FadeRoutine(CanvasGroup group, float start, float end, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, end, t / duration);
                yield return null;
            }

            group.alpha = end;
        }
    }
}