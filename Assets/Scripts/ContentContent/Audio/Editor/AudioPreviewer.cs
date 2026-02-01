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
        public static AudioClip GetCurrentClip()
        {
            return source != null ? source.clip : null;
        }

        public static float GetPlaybackProgress()
        {
            if (source == null || source.clip == null) return 0f;
            return source.time / source.clip.length;
        }

        public static void SetPlaybackPosition(float normalizedPosition)
        {
            if (source == null || source.clip == null) return;
            normalizedPosition = Mathf.Clamp01(normalizedPosition);
            float timeInSeconds = normalizedPosition * source.clip.length;
            source.time = Mathf.Clamp(timeInSeconds, 0f, source.clip.length - 0.01f);
        }

        public static float GetCurrentVolumeAmplitude()
        {
            if (source == null || !source.isPlaying) return 0f;
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

            EditorApplication.update -= UpdateFade;
            CreateSourceIfNeeded();
            ResetSource(); // <--- IMPORTANT: Clear previous dirty state

            currentTarget = data;

            // 1. Assign Clip
            source.clip = data.GetClip();
            if (source.clip == null) return;

            // 2. ROUTE TO MIXER
            // This ensures the preview goes through your Master/SFX groups with all effects applied.
            source.outputAudioMixerGroup = data.outputGroup;

            // 3. Apply Settings
            source.loop = data.loop;

            // Calculate Pitch
            float targetPitch = data.pitch + Random.Range(-data.randomPitch, data.randomPitch);
            source.pitch = Mathf.Clamp(targetPitch, 0.1f, 3f);

            // Calculate Volume
            float targetVol = data.volume + Random.Range(-data.randomVolume, data.randomVolume);
            targetVol = Mathf.Clamp01(targetVol);

            // 4. Force 2D for Preview
            // If we leave this at data.spatialBlend (which might be 1.0 for 3D), 
            // the audio will likely be silent because the Editor "Listener" is far away.
            // We force 2D (0f) to guarantee the signal reaches the Mixer.
            source.spatialBlend = 0f; 

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
                // Create a hidden GameObject in the Editor scene to host the AudioSource
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