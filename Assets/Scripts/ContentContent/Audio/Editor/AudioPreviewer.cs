using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ContentContent.Audio.Editor
{
    [InitializeOnLoad]
    public static class AudioPreviewer
    {
        private static AudioSource source;
        private static SoundData currentTarget;

        // Fading State
        private static double fadeStartTime;
        private static float fadeDuration;
        private static float fadeTargetVolume;

        static AudioPreviewer()
        {
            EditorApplication.playModeStateChanged += state => Stop();
        }

        public static bool IsPlaying => source != null && source.isPlaying;


        // --- VISUALIZER HELPERS ---
        // Helper to get the actual clip being played (or about to be played)
        public static AudioClip GetCurrentClip()
        {
            if (source != null) return source.clip;
            return null;
        }

        // 0.0 to 1.0 (Progress)
        public static float GetPlaybackProgress()
        {
            if (source == null || source.clip == null) return 0f;
            return source.time / source.clip.length;
        }

        public static void SetPlaybackPosition(float normalizedPosition)
        {
            if (source == null || source.clip == null) return;

            // Clamp between 0 and almost the end (to prevent loop restarting immediately)
            normalizedPosition = Mathf.Clamp01(normalizedPosition);
            float timeInSeconds = normalizedPosition * source.clip.length;

            // Safety clamp to ensure we don't hit exact end and stop
            source.time = Mathf.Clamp(timeInSeconds, 0f, source.clip.length - 0.01f);
        }

        // 0.0 to 1.0 (Current Amplitude)
        public static float GetCurrentVolumeAmplitude()
        {
            if (source == null || !source.isPlaying) return 0f;

            // Get a small sample block to calculate RMS (Root Mean Square) volume
            var samples = new float[64];
            source.GetOutputData(samples, 0);

            float sum = 0;
            for (var i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];

            return Mathf.Sqrt(sum / samples.Length);
        }

        // --- 1. DOUBLE CLICK HANDLER ---

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Object obj = EditorUtility.EntityIdToObject(instanceID);

            // ONLY handle SoundData. 
            // We ignore AudioClips so Unity's native inspector handles them.
            if (obj is SoundData data)
            {
                Play(data);
                Selection.activeObject = obj;
                return true;
            }

            return false;
        }

        // --- 2. PLAYBACK LOGIC ---

        public static void Play(SoundData data)
        {
            if (data == null) return;

            // Toggle functionality: If clicking the same data while playing, stop it.
            if (IsPlaying && currentTarget == data)
            {
                Stop();
                return;
            }

            // Reset Update Loop
            EditorApplication.update -= UpdateFade;
            CreateSourceIfNeeded();
            ResetSource(); // <--- IMPORTANT: Clear previous dirty state

            currentTarget = data;

            // Setup
            source.clip = data.GetClip();
            source.outputAudioMixerGroup = data.outputGroup;
            source.loop = data.loop;

            float targetPitch = data.pitch + Random.Range(-data.randomPitch, data.randomPitch);
            source.pitch = Mathf.Clamp(targetPitch, 0.1f, 3f);

            float targetVol = data.volume + Random.Range(-data.randomVolume, data.randomVolume);
            targetVol = Mathf.Clamp01(targetVol);

            // Fade Logic
            if (data.fadeInTime > 0f)
            {
                source.volume = 0f;
                fadeTargetVolume = targetVol;
                fadeDuration = data.fadeInTime;
                fadeStartTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += UpdateFade;
            }
            else
            {
                source.volume = targetVol;
            }

            source.Play();
        }

        public static void Stop()
        {
            EditorApplication.update -= UpdateFade;
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.outputAudioMixerGroup = null; // Clean up ref
                currentTarget = null;
            }
        }

        private static void ResetSource()
        {
            if (source == null) return;
            source.loop = false;
            source.pitch = 1f;
            source.volume = 1f;
            source.spatialBlend = 0f; // 2D
            source.mute = false;
            source.bypassEffects = false;
            source.bypassListenerEffects = false;
            source.bypassReverbZones = false;
        }

        private static void UpdateFade()
        {
            if (source == null || !source.isPlaying)
            {
                EditorApplication.update -= UpdateFade;
                return;
            }

            double timeElapsed = EditorApplication.timeSinceStartup - fadeStartTime;
            var progress = (float)(timeElapsed / fadeDuration);

            if (progress < 1.0f)
            {
                source.volume = Mathf.Lerp(0f, fadeTargetVolume, progress);
            }
            else
            {
                source.volume = fadeTargetVolume;
                EditorApplication.update -= UpdateFade;
            }
        }

        private static void CreateSourceIfNeeded()
        {
            if (source == null)
            {
                GameObject go = EditorUtility.CreateGameObjectWithHideFlags(
                    "AudioPreview_Global",
                    HideFlags.HideAndDontSave,
                    typeof(AudioSource)
                );
                source = go.GetComponent<AudioSource>();
                source.playOnAwake = false;
            }
        }

        [MenuItem("Tools/Audio/Stop Preview Audio %#k")]
        public static void StopAllPreviews()
        {
            Stop();
        }
    }
}