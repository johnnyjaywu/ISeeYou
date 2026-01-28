using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ContentContent.Audio
{
    /// <summary>
    ///     A wrapper around AudioSource that exposes events for playback lifecycle.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : PoolableBehaviour
    {
        private SoundData currentData;
        private CoroutineHandle fadeRoutine;
        public Action<SoundData> Finished; // Manager needs this direct ref
        private bool isPaused;
        private bool isReturning;
        private CoroutineHandle lifecycleRoutine;

        private AudioSource source;

        // Unique ID that increments every time this object is reused.
        // This allows Handles to know if they are holding an outdated reference.
        public int GenerationId { get; private set; }

        public bool IsPlaying => source != null && source.isPlaying;

        public float Volume
        {
            get => source.volume;
            set => source.volume = value;
        }

        public float Pitch
        {
            get => source.pitch;
            set => source.pitch = value;
        }

        public float Length => source.clip != null ? source.clip.length : 0f;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }


        protected override void OnSpawn()
        {
            // Any existing SoundHandles referring to the previous ID are now invalid.
            GenerationId++;

            isReturning = false;
            isPaused = false;
            source.Stop();
            if (source.clip != null)
                source.time = 0;
        }

        protected override void OnDespawn()
        {
            ResetCoroutines();
            source.Stop();
            source.clip = null;
            Started = null;
            Paused = null;
            Resumed = null;
            Finished?.Invoke(currentData);
            Finished = null;
        }

        // Events
        public event Action Started;
        public event Action Paused;
        public event Action Resumed;

        /// <summary>
        ///     Configures and plays the audio clip.
        /// </summary>
        public void Play(SoundData data)
        {
            // SAFETY: Hard Reset. 
            // Ensure no old coroutines are running from a previous lifecycle 
            // or if Play() was called accidentally on an active object.
            ResetCoroutines();

            currentData = data;
            isPaused = false;
            isReturning = false;

            // Apply Settings
            source.outputAudioMixerGroup = data.outputGroup;
            source.clip = data.GetClip();
            source.loop = data.loop;

            // Calculate Pitch (Clamped to prevent negative/zero pitch)
            float targetPitch = data.pitch + Random.Range(-data.randomPitch, data.randomPitch);
            source.pitch = Mathf.Clamp(targetPitch, 0.1f, 3f);

            // Calculate Volume (Clamped 0-1)
            float targetVolume = data.volume + Random.Range(-data.randomVolume, data.randomVolume);
            targetVolume = Mathf.Clamp01(targetVolume);

            // 3D Settings (The new stuff)
            source.spatialBlend = data.spatialBlend;
            source.spread = data.spread;
            source.dopplerLevel = data.dopplerLevel;
            source.rolloffMode = data.rolloffMode;
            source.minDistance = data.minDistance;
            source.maxDistance = data.maxDistance;

            // Optional: If using custom curves
            // if (data.rolloffMode == AudioRolloffMode.Custom && data.customRolloffCurve != null)
            //     source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, data.customRolloffCurve);

            // Volume & Fade In Logic
            if (data.fadeInTime > 0f)
            {
                source.volume = 0f;
                source.Play();
                fadeRoutine = FadeRoutine(0f, targetVolume, data.fadeInTime).Run();
            }
            else
            {
                source.volume = targetVolume;
                source.Play();
            }

            Started?.Invoke();

            // Start Lifecycle Monitor
            // Check activeInHierarchy to prevent Unity errors if the object was 
            // deactivated immediately after Get().
            if (gameObject.activeInHierarchy)
                lifecycleRoutine = MonitorPlayback().Run();
            else
                // Edge case: Object spawned but immediately disabled. Return to pool to avoid memory leak.
                Despawn();
        }

        /// <summary>
        ///     Stops playback immediately and fires the finish event.
        /// </summary>
        public void Stop()
        {
            // If we are already returning or stopping, ignore.
            if (isReturning) return;

            // Stop any active Fade-In immediately
            if (fadeRoutine is { IsRunning: true }) fadeRoutine.Stop();

            if (currentData.fadeOutTime > 0f && gameObject.activeInHierarchy)
                fadeRoutine = FadeRoutine(source.volume, 0f, currentData.fadeOutTime, true).Run();
            else
                source.Stop();
        }

        public void Pause()
        {
            if (isPaused || currentData.bypassGlobalPause) return;
            source.Pause();
            isPaused = true;
            Paused?.Invoke();
        }

        public void Resume()
        {
            if (!isPaused || currentData.bypassGlobalPause) return;
            source.UnPause();
            isPaused = false;
            Resumed?.Invoke();
        }

        #region Helpers

        private void ResetCoroutines()
        {
            if (lifecycleRoutine is { IsRunning: true }) lifecycleRoutine.Stop();
            lifecycleRoutine = null;

            if (fadeRoutine is { IsRunning: true }) fadeRoutine.Stop();
            fadeRoutine = null;
        }

        private IEnumerator FadeRoutine(float start, float end, float duration, bool stopOnComplete = false)
        {
            var timer = 0f;
            while (timer < duration)
            {
                // If the object is forcefully returned while fading, break.
                if (isReturning) yield break;
                if (!isPaused)
                {
                    timer += Time.deltaTime;
                    source.volume = Mathf.Lerp(start, end, timer / duration);
                }

                yield return null;
            }

            source.volume = end;
            if (stopOnComplete) source.Stop();
            fadeRoutine = null;
        }

        private IEnumerator MonitorPlayback()
        {
            yield return null; // Make sure the audio actually started playing

            // Wait while the audio is technically playing OR we are holding it in a paused state.
            //    - If playing normally: isPlaying=true, isPaused=false (Wait)
            //    - If paused: isPlaying=false, isPaused=true (Wait)
            //    - If finished: isPlaying=false, isPaused=false (Done)
            // AND we haven't flagged for return yet (Safety check)
            yield return new WaitWhile(() => (source.isPlaying || isPaused) && !isReturning);
            lifecycleRoutine = null;
            Despawn();
        }

        // private void ReleaseSelf()
        // {
        //     // SAFETY: Idempotency check. 
        //     // If multiple systems try to release this object in the same frame, 
        //     // only the first one succeeds.
        //     if (isReturning) return;
        //     isReturning = true;
        //
        //     // Notify Manager
        //     Finished?.Invoke(currentData);
        //     Despawn();
        // }

        #endregion
    }
}