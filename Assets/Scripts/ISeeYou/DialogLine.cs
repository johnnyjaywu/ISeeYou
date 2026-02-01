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
        public string speakerID;
        [TextArea(3, 5), ResizableTextArea]
        public string text;

        [TextArea(3, 5), ResizableTextArea]
        public List<string> subTexts;
        
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