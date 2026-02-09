using System;
using ContentContent;
using ContentContent.Audio;
using NaughtyAttributes;
using UnityEngine;

namespace ISeeYou
{
    public class SoundDataPlayer : MonoBehaviour
    {
        [Expandable]
        [SerializeField]
        private SoundData sound;

        [Tooltip("Will also stop on disable")]
        [SerializeField]
        public bool playOnEnable;

        public float delay;

        private SoundHandle soundHandle;
        private TimerHandle timerHandle;

        private void OnEnable()
        {
            if (!playOnEnable) return;

            Play();
        }

        private void OnDisable()
        {
            if (!playOnEnable) return;
            Stop();
        }

        [Button]
        public void Play()
        {
            if (soundHandle.IsPlaying)
                soundHandle.Stop();

            if (timerHandle.IsRunning)
                timerHandle.Stop();

            timerHandle = Timer.Countdown(delay, this).OnFinish(() => { soundHandle = sound.Play(); });
        }

        [Button]
        public void Stop()
        {
            if (!soundHandle.IsValid) return;
            soundHandle.Stop();
            
            if (timerHandle.IsRunning)
                timerHandle.Stop();
        }
    }
}