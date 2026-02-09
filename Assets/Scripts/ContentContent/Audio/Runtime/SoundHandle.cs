using System;
using UnityEngine;

namespace ContentContent.Audio
{
    // A safe wrapper around an AudioEmitter.
    // If the internal emitter is recycled, this handle becomes "Invalid" automatically.
    public struct SoundHandle
    {
        private readonly SoundEmitter emitter;
        private readonly int generationId;

        // Constructor is internal so only AudioManager can create handles
        internal SoundHandle(SoundEmitter emitter)
        {
            this.emitter = emitter;
            // If emitter is null (creation failed), generationId is -1 (always invalid)
            generationId = emitter != null ? emitter.GenerationId : -1;
        }

        public bool IsValid =>
            emitter != null &&
            emitter.gameObject.activeInHierarchy &&
            emitter.GenerationId == generationId;

        public bool IsPlaying => IsValid && emitter.IsPlaying;
        public bool IsPaused => IsValid && emitter.IsPaused;

        public float Length => IsValid ? emitter.Length : 0f;

        public SoundHandle SetParent(Transform parent)
        {
            if (IsValid) emitter.transform.SetParent(parent);
            return this;
        }

        public SoundHandle SetPosition(Vector3 position)
        {
            if (IsValid) emitter.transform.position = position;
            return this;
        }

        public SoundHandle SetVolume(float volume)
        {
            if (IsValid) emitter.Volume = volume;
            return this;
        }

        public SoundHandle SetPitch(float pitch)
        {
            if (IsValid) emitter.Pitch = pitch;
            return this;
        }

        // --- CONTROLS ---

        public void Stop()
        {
            if (IsValid) emitter.Stop();
        }

        public void Pause()
        {
            if (IsValid) emitter.Pause();
        }

        public void Resume()
        {
            if (IsValid) emitter.Resume();
        }

        // --- EVENT CHAINING ---

        public SoundHandle OnStarted(Action callback)
        {
            if (IsValid) emitter.Started += callback;
            return this;
        }

        public SoundHandle OnFinished(Action<SoundData> callback)
        {
            if (IsValid) emitter.Finished += callback;
            return this;
        }

        // Expose the raw emitter only if absolutely necessary, but keep it hidden mostly
        public SoundEmitter GetEmitterUnsafe()
        {
            return IsValid ? emitter : null;
        }
    }
}