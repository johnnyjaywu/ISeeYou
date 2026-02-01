using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ISeeYou
{
    [Serializable]
    public class WordsData
    {
        [Tooltip("The actual text displayed on screen.")]
        public string Text;

        [Tooltip("If true, this word is critical for the puzzle solution.")]
        public bool IsKey;

        [Tooltip("The hidden text revealed during Phase 3 (Inspection). Only relevant if IsKey is true.")]
        public string Subtext;
    }

    public static class WordsParser
    {
        /// <summary>
        /// Parses text using Regex. Prioritizes [...] groups, then standard words.
        /// Strips brackets and trims whitespace from all results.
        /// </summary>
        public static List<string> ParseGroupedString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();

            // Pattern: \[([^\]]*)\]  -> Matches content inside brackets (Group 1)
            //          |             -> OR
            //          (\S+)         -> Matches continuous non-whitespace characters (Group 2)
            var matches = Regex.Matches(input, @"\[([^\]]*)\]|(\S+)");

            return matches.Cast<Match>()
                // Pick Group 1 (bracket content) if matched, otherwise Group 2 (word)
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Select(text => text.Trim())          // Remove leading/trailing whitespace
                .Where(text => !string.IsNullOrEmpty(text)) // Discard empty entries
                .ToList();
        }
    }
}