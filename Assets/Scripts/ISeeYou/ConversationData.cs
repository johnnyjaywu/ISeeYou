using System;
using System.Collections.Generic;
using ContentContent;
using NaughtyAttributes;
using UnityEngine;

namespace ISeeYou
{
    /// <summary>
    /// A ScriptableObject that defines the data for a single conversation puzzle.
    /// </summary>
    [CreateAssetMenu(fileName = "Conversation_New", menuName = "I See You/Conversation Data")]
    public class ConversationData : ScriptableObject
    {
        [Header("Phase 1: Deconstruction")]
        [Tooltip("The full sentence or phrase cloud that represents the character's mask.")]
        [TextArea(3, 5), ResizableTextArea]
        public string MaskSentence;

        [Tooltip("The specific words/phrases from the Mask that must be isolated.")]
        public List<string> FilteredWords;

        [Header("Phase 2: Revelation")]
        [Tooltip("The filtered sentence that appears after filtering out.")]
        [TextArea(3, 5), ResizableTextArea]
        public string FilteredSentence;

        [Tooltip("The list of words in the filtered sentence that are still not truthful.")]
        public List<string> MaskedWords;

        [Tooltip("The list of words that replace the hidden words. Must match the index and size of the hidden words.")]
        public List<string> Truths;

        private void OnValidate()
        {
            if (MaskedWords.Count != Truths.Count)
            {
                Debug.LogWarning($"[{name}] The number of Masked Words and Truths must be the same.", this);
            }
        }

        public string GetTruth(string word)
        {
            int index = MaskedWords.IndexOf(word);
            if (index == -1 || index >= Truths.Count) return "";
            return Truths[index];
        }
    }
}