using ContentContent.Audio;
using NaughtyAttributes;
using UnityEngine;

namespace ISeeYou
{
    [System.Serializable]
    public struct DialogLine
    {
        public string speakerID;
        [TextArea(3, 5), ResizableTextArea]
        public string text;
        public SoundData voiceLine;

        public void Speak()
        {
            var speaker = SpeakerManager.GetSpeaker(speakerID);
            if (speaker == null) return;
            speaker.Speak(this);
        }

        public void Stop()
        {
            var speaker = SpeakerManager.GetSpeaker(speakerID);
            if (speaker == null) return;
            speaker.Stop();
        }
    }
}