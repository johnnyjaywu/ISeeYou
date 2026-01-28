using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Audio
{
    public class SoundManager : SingletonBehaviour<SoundManager>
    {
        private readonly Dictionary<SoundData, List<SoundEmitter>> activeSounds = new();
        private readonly List<SoundEmitter> allActiveEmitters = new();
        private readonly object poolKey = new();
        private bool isGloballyPaused;

        public SoundHandle Play(SoundData data, Vector3 position = default)
        {
            if (data == null) return new SoundHandle(null); // Return empty valid handle

            // 1. Concurrency Check
            if (!activeSounds.ContainsKey(data)) activeSounds[data] = new List<SoundEmitter>();
            List<SoundEmitter> concurrencyList = activeSounds[data];

            if (concurrencyList.Count >= data.maxConcurrency)
            {
                if (data.stealOldest)
                    // Cleanly stop the oldest. 
                    // Note: accessing .Stop() on emitter triggers the fade out, 
                    // which eventually triggers Finished, which removes it from the list.
                    concurrencyList[0].Stop();
                else
                    return new SoundHandle(null); // Limit reached
            }

            // Get from pool
            Vector3 spawnPos = data.spatialBlend == 0 ? Vector3.zero : position;
            SoundEmitter emitter = ObjectPoolManager.Instance.Get(poolKey, CreateSoundEmitter, spawnPos, Quaternion.identity);

            // Parenting Logic
            if (data.spatialBlend < 0.1f)
            {
                emitter.transform.SetParent(transform);
                emitter.transform.localPosition = Vector3.zero;
            }
            else
            {
                emitter.transform.SetParent(null);
            }

            // Setup Callback
            emitter.Finished = finishedData =>
            {
                if (activeSounds.ContainsKey(finishedData))
                    activeSounds[finishedData].Remove(emitter);
                allActiveEmitters.Remove(emitter);
            };

            // Tracking
            concurrencyList.Add(emitter);
            allActiveEmitters.Add(emitter);

            // Play the audio
            emitter.Play(data);

            if (isGloballyPaused && !data.bypassGlobalPause) emitter.Pause();

            // Create and return the Safety Handle
            return new SoundHandle(emitter);
        }

        public void Pause()
        {
            isGloballyPaused = true;
            for (int i = allActiveEmitters.Count - 1; i >= 0; i--)
                if (allActiveEmitters[i] != null)
                    allActiveEmitters[i].Pause();
        }

        public void Unpause()
        {
            isGloballyPaused = false;
            for (int i = allActiveEmitters.Count - 1; i >= 0; i--)
                if (allActiveEmitters[i] != null)
                    allActiveEmitters[i].Resume();
        }

        private SoundEmitter CreateSoundEmitter()
        {
            // 1. Create empty GameObject
            // We give it a temporary name; the pool might rename it, or we name it here.
            var go = new GameObject($"SoundEmitter_{Guid.NewGuid().ToString().Substring(0, 4)}");

            // 2. Add AudioSource first
            // We configure defaults here to ensure a clean slate.
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f; // Default to 3D
            source.dopplerLevel = 0f; // Many games prefer 0 doppler by default

            // 3. Add SoundEmitter
            // Since AudioSource exists, Emitter.Awake() will successfully finding it via GetComponent.
            var emitter = go.AddComponent<SoundEmitter>();

            // 4. Optimization (Optional)
            // If you never need to find these by tag, this saves a tiny bit of overhead
            go.hideFlags = HideFlags.None;

            return emitter;
        }
    }
}