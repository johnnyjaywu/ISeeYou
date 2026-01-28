using System;

namespace ContentContent.Dialog
{
    public class DialogBuilder
    {
        private readonly Dialog dialog;

        private DialogBuilder()
        {
            dialog = new Dialog();
        }

        public static DialogBuilder Create()
        {
            return new DialogBuilder();
        }

        /// <summary>
        ///     Parses a block of text.
        ///     Format: "SpeakerID: This is the text."
        ///     If no colon is found, it uses the DefaultSpeakerID.
        /// </summary>
        public Dialog Parse(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return null;

            // Split by new line, handling different OS line endings
            string[] lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                ParseSingleLine(line);
            }

            return dialog;
        }

        private void ParseSingleLine(string line)
        {
            string cleanLine = line.Trim();

            // 1. Separate Metadata (Speaker/Portrait/Voice) from Content
            int colonIndex = cleanLine.IndexOf(':');
            var metaSection = "";
            string text = cleanLine;

            if (colonIndex > 0)
            {
                metaSection = cleanLine.Substring(0, colonIndex).Trim();
                text = cleanLine.Substring(colonIndex + 1).Trim();
            }

            // Parse Metadata
            // Expected format: "SpeakerName" OR "SpeakerName(PortraitID)"
            string speaker = metaSection;
            string portrait = "";
            string voice = "";

            // A. Extract Voice [Square Brackets]
            int openBracket = speaker.IndexOf('[');
            int closeBracket = speaker.IndexOf(']');
            if (openBracket >= 0 && closeBracket > openBracket)
            {
                voice = speaker.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
                // Remove the voice part from the string to clean up for the next step
                speaker = speaker.Remove(openBracket, closeBracket - openBracket + 1).Trim();
            }

            // B. Extract Portrait (Parentheses)
            int openParen = speaker.IndexOf('(');
            int closeParen = speaker.IndexOf(')');
            if (openParen >= 0 && closeParen > openParen)
            {
                portrait = speaker.Substring(openParen + 1, closeParen - openParen - 1).Trim();
                // Remove the portrait part
                speaker = speaker.Remove(openParen, closeParen - openParen + 1).Trim();
            }
            
            // C. Whatever is left is the Speaker Name
            speaker = speaker.Trim();

            dialog.Add(new DialogLine(text, speaker, portrait, voice));
        }

        /// <summary>
        ///     Manually add a specific line if parsing isn't enough.
        /// </summary>
        public DialogBuilder AddLine(string text, string speakerID = "", string portraitID = "")
        {
            dialog.Add(new DialogLine(text, speakerID, portraitID));
            return this;
        }
    }
}