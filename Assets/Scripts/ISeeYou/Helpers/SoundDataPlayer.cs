using System;
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

        private SoundHandle soundHandle;

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
            soundHandle = sound.Play();
        }

        [Button]
        public void Stop()
        {
            if (!soundHandle.IsValid) return;
            soundHandle.Stop();
        }
    }
}