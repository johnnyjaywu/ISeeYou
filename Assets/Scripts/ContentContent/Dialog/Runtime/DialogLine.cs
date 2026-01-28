using System;
using UnityEngine;

namespace ContentContent.Dialog
{
    [Serializable]
    public class DialogLine
    {
        [TextArea]
        public string text;

        public string speakerID;
        public string portraitID;
        public string voiceLineID;

        public DialogLine(string text, string speakerID = "", string portraitID = "", string voiceLineID = null)
        {
            this.text = text;
            this.speakerID = speakerID;
            this.portraitID = portraitID;
            this.voiceLineID = voiceLineID;
        }
    }
}