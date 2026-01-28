using System.Collections;
using UnityEngine;

namespace ContentContent.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioFader : MonoBehaviour
    {
        [Header("Fade In")]
        [SerializeField] private float fadeInDuration = 2f;

        [SerializeField] private float fadeInVolume = 1f;

        [Header("Fade Out")]
        [SerializeField] private float fadeOutDuration = 2f;

        [SerializeField] private float fadeOutVolume;
        private AudioSource audioSource;

        private Coroutine fadeInCoroutine;
        private Coroutine fadeOutCoroutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.volume = 0f;
        }

        private void Update()
        {
            // Check if audio started playing
            // *Note fadeInCoroutine should only be null when the audio reaches the end
            if (fadeInCoroutine == null && audioSource.isPlaying) FadeIn();

            // Check audio playback position for fade out
            // Gives a 0.5 second buffer so it starts fading out early
            if (fadeOutCoroutine == null &&
                audioSource.clip.length - audioSource.time <= fadeOutDuration)
                FadeOut();
        }

        public void FadeIn()
        {
            if (fadeInCoroutine != null)
            {
                Debug.LogWarning("Attempting to fade in when audio is already fading in");
                return;
            }

            fadeInCoroutine = StartCoroutine(FadeInCoroutine());
        }

        private IEnumerator FadeInCoroutine()
        {
            float startVolume = audioSource.volume;
            var timer = 0f;

            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, fadeInVolume, timer / fadeInDuration);
                yield return null;
            }

            audioSource.volume = fadeInVolume; // Ensure volume is exactly fadeInVolume at the end
        }

        public void FadeOut()
        {
            if (fadeOutCoroutine != null)
            {
                Debug.LogWarning("Attempting to fade out when audio is already fading out");
                return;
            }

            fadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            float startVolume = audioSource.volume;
            var timer = 0f;

            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, fadeOutVolume, timer / fadeOutDuration);
                yield return null; // Wait for the next frame
            }

            audioSource.volume = fadeOutVolume; // Ensure volume is exactly fadeOutVolume at the end
            if (!audioSource.loop)
                audioSource.Stop(); // Optionally stop the audio after fading out
            fadeOutCoroutine = null;
            fadeInCoroutine = null;
        }
    }
}