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

            if (soundHandle is { IsPlaying: true })
                soundHandle.Resume();
            else
                soundHandle = sound.Play();
        }

        private void OnDisable()
        {
            if (!playOnEnable) return;
            if (soundHandle is { IsPlaying: true })
                soundHandle.Pause();
        }
    }
}