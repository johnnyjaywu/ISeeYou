using System;
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
}