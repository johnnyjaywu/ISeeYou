using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentContent.Audio;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace ISeeYou
{
    [System.Serializable]
    public struct DialogLine
    {
        [TextArea(3, 5), ResizableTextArea]
        public string text;

        public string speakerID;
        public bool autoPlay;

        [TextArea(3, 5), ResizableTextArea]
        public List<string> maskingLines;

        public SoundData voiceLine;

        public UnityEvent triggerEvent;

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