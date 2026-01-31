using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentContent.Audio;
using NaughtyAttributes;
using Swarmkeeper;
using UnityEngine;

namespace ISeeYou
{
    /// <summary>
    /// A ScriptableObject that defines the data for a single conversation puzzle.
    /// Includes text data for all phases and audio profiles for the filtering phase.
    /// </summary>
    [CreateAssetMenu(fileName = "Conversation_New", menuName = "I See You/Conversation Data")]
    public class ConversationData : ScriptableObject
    {
        [Header("Audio Data")]
        [Tooltip("The background noise loop during the Filtering phase.")]
        public SoundData Ambient;

        [InfoBox("Match the sound data name exactly to the words")]
        [Tooltip("The sound data associated with the noise words.")]
        public SoundBank NoiseVoices;
        // [Tooltip("Sound played when the filtering phase is completed.")]
        // public SoundData PhaseCompleteStinger;

        // [Tooltip("Sound when a Mask word is successfully cracked/revealed.")]
        // public SoundData RevealSound;
        
        // [Tooltip("Sound when clicking a normal word (no hidden meaning).")]
        // public SoundData DudSound;
        
        [Header("Phase 1: Filtering")]
        [InfoBox("Words or phrases separated by a comma. Surround with * to indicate the key words\n" +
                 "Key words order matter! They will be checked in game\n" +
                 "Example: Hello, how are you, don't, *worry*, *I'm fine*")]
        [TextArea(3, 5), ResizableTextArea]
        public string Noise;

        [Header("Phase 2 & 3: Reconstruction/Inspection")]
        [InfoBox("The reconstructed sentence(s) that appears after the words are arranged correctly.\n" +
                 "Surround words with [ ] to group them.\n" +
                 "Surround with * to indicate the masked words.\n" +
                 "Place subtext directly after masked words with ( ) to indicate the subtext to replace the masked words\n" +
                 "Example: *Don't worry*(I'm worried) [about me]. Honestly, *I can handle it* (I can't handle it)")]
        [TextArea(3, 5), ResizableTextArea]
        public string Monologue;
        public SoundBank MonologueVoices;
        
        [Header("Phase 4: Revelation")]
        [InfoBox("The final sentence(s) that is revealed at the end")]
        [TextArea(3, 5), ResizableTextArea]
        public string Truth;
        public SoundData TruthVoiceLine;
        
        /// <summary>
        /// Parses the Noise field and returns a list of WordsData.
        /// </summary>
        public List<WordsData> GetNoiseWords()
        {
            if (string.IsNullOrEmpty(Noise))
            {
                return null;
            }

            return Noise.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(word =>
                {
                    bool isKey = word.StartsWith("*") && word.EndsWith("*");
                    string text = isKey ? word.Substring(1, word.Length - 2) : word;
                    return new WordsData { Text = text, IsKey = isKey, Subtext = "" };
                })
                .ToList();
        }

        /// <summary>
        /// Parses the Monologue field and returns a list of WordsData.
        /// Handles [Groups], *Masks*, and (Subtext).
        /// </summary>
        public List<WordsData> GetMonologueWords()
        {
            if (string.IsNullOrEmpty(Monologue))
            {
                return null;
            }

            var wordsDataList = new List<WordsData>();
            
            // Regex Breakdown:
            // 1. (*word* (subtext)) -> Mask with subtext
            // 4. ([word])           -> Grouped word
            // 6. (*word*)           -> Key/Mask without subtext
            // 8. (word)             -> Normal word
            var regex = new Regex(@"(\*([^*]+?)\*\s*\(([^)]+?)\))|(\[(.*?)\])|(\*([^*]+?)\*)|(\S+)");
            
            var matches = regex.Matches(Monologue);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success) // Masked word with subtext
                {
                    wordsDataList.Add(new WordsData
                    {
                        Text = match.Groups[2].Value.Trim(),
                        IsKey = true,
                        Subtext = match.Groups[3].Value.Trim()
                    });
                }
                else if (match.Groups[4].Success) // Grouped word [Like This]
                {
                    // FIX: Removed the outer brackets from the Text string.
                    // We only want the content captured in Group 5.
                    wordsDataList.Add(new WordsData
                    {
                        Text = match.Groups[5].Value.Trim(), 
                        IsKey = false,
                        Subtext = ""
                    });
                }
                else if (match.Groups[6].Success) // Key word *Like This*
                {
                    wordsDataList.Add(new WordsData
                    {
                        Text = match.Groups[7].Value.Trim(),
                        IsKey = true,
                        Subtext = ""
                    });
                }
                else if (match.Groups[8].Success) // Normal word
                {
                    wordsDataList.Add(new WordsData
                    {
                        Text = match.Groups[8].Value.Trim(),
                        IsKey = false,
                        Subtext = ""
                    });
                }
            }
            return wordsDataList;
        }
    }
}