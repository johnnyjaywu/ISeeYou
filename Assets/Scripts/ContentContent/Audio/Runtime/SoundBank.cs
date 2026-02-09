using System.Collections.Generic;
using ContentContent.Audio;
using UnityEngine;

namespace Swarmkeeper
{
    [CreateAssetMenu(fileName = "NewSoundBank", menuName = "Audio/Sound Bank")]
    public class SoundBank : ScriptableObject
    {
        public string bankName;
        public List<SoundData> sounds = new();

        // Optional: Helper to find sound by name if you prefer string-based lookup
        // though direct reference to SoundData is faster/safer.
        public SoundData GetSound(string name)
        {
            return sounds.Find(s => s.name == name);
        }
    }
}