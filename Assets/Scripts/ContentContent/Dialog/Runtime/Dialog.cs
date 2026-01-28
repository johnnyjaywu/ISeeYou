using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Dialog
{
    [Serializable]
    public class Dialog
    {
        public string title;

        [Tooltip("The list of lines to play in order.")]
        public List<DialogLine> lines = new();

        public Dialog Add(List<DialogLine> lines)
        {
            this.lines.AddRange(lines);
            return this;
        }

        public Dialog Add(DialogLine line)
        {
            lines.Add(line);
            return this;
        }

        public Dialog Add(string text, string speakerID = "")
        {
            var line = new DialogLine(text, speakerID);
            lines.Add(line);
            return this;
        }
    }
}