using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace ContentContent.Audio
{
    [CreateAssetMenu(fileName = "NewSoundData", menuName = "Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        public enum PlayOrder
        {
            Random,
            Sequential
        }

        [Header("Audio Config")]
        public AudioClip[] clips;

        public PlayOrder playOrder = PlayOrder.Random;
        public AudioMixerGroup outputGroup;

        [Tooltip("If set to false, it will play in sequence")]
        public bool playRandom = true;

        [Header("General Settings")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Randomly adds/subtracts this value from the Volume.")]
        [Range(0f, 0.5f)] public float randomVolume;

        [Range(0.1f, 3f)] public float pitch = 1f;

        [Tooltip("Randomly adds/subtracts this value from the Pitch.")]
        [Range(0f, 0.5f)] public float randomPitch;

        [Header("Looping & Fading")]
        public bool loop;

        public float fadeInTime;
        public float fadeOutTime = 0.5f;

        [Header("3D Settings")]
        [Range(0f, 1f)] public float spatialBlend; // 1 = 3D, 0 = 2D

        [Range(0f, 360f)] public float spread;
        [Range(0f, 5f)] public float dopplerLevel = 1f;

        [Header("Distance / Rolloff")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        public float minDistance = 1f;

        public float maxDistance = 500f;
        // Optional: If you really need custom curves
        // public AnimationCurve customRolloffCurve;

        [Header("Concurrency")]
        public int maxConcurrency = 3;

        public bool stealOldest;

        [Header("Pause Logic")]
        public bool bypassGlobalPause;

        // --- STATE TRACKING ---
        [NonSerialized] private int _playIndex;

        /// <summary>
        ///     Returns the duration range of the clips, accounting for the BASE pitch.
        ///     X = Min Duration, Y = Max Duration.
        ///     (Does not account for random pitch variance to keep values deterministic).
        /// </summary>
        public Vector2 GetDurationRange()
        {
            if (clips == null || clips.Length == 0) return Vector2.zero;

            var min = float.MaxValue;
            var max = float.MinValue;
            var foundClip = false;

            // Avoid divide by zero
            float basePitch = pitch == 0f ? 0.001f : Mathf.Abs(pitch);

            foreach (AudioClip clip in clips)
                if (clip != null)
                {
                    foundClip = true;
                    // Real Duration = Length / Pitch
                    float duration = clip.length / basePitch;

                    if (duration < min) min = duration;
                    if (duration > max) max = duration;
                }

            if (!foundClip) return Vector2.zero;

            // If only one clip, min and max are the same
            return new Vector2(min, max);
        }

        public AudioClip GetClip()
        {
            if (clips == null || clips.Length == 0) return null;

            switch (playOrder)
            {
                case PlayOrder.Sequential:
                    // Get current, then increment and wrap around
                    AudioClip clip = clips[_playIndex];
                    _playIndex = (_playIndex + 1) % clips.Length;
                    return clip;

                case PlayOrder.Random:
                default:
                    return clips[Random.Range(0, clips.Length)];
            }
        }

        public void ResetSettings()
        {
            // General
            volume = 1f;
            pitch = 1f;
            randomVolume = 0f;
            randomPitch = 0f;

            // Fading & Looping
            loop = false;
            fadeInTime = 0f;
            fadeOutTime = 0.5f;

            // 3D Settings
            spatialBlend = 0f; // Default to 3D
            spread = 0f;
            dopplerLevel = 1f;
            rolloffMode = AudioRolloffMode.Logarithmic;
            minDistance = 1f;
            maxDistance = 500f;

            // Concurrency
            maxConcurrency = 3;
            stealOldest = false;
            bypassGlobalPause = false;
        }

        public bool HasClips => clips is { Length: > 0 };

        public SoundHandle Play(Vector3 position = default)
        {
            return SoundManager.Instance.Play(this, position);
        }
    }
}