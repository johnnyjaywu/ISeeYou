using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace ISeeYou
{
    /// <summary>
    /// A ScriptableObject that defines the data for a single conversation puzzle.
    /// Each instance represents one level of the game.
    /// </summary>
    [CreateAssetMenu(fileName = "Conversation_New", menuName = "I See You/Conversation Data")]
    public class ConversationData : ScriptableObject
    {
        [Header("Phase 1: Deconstruction")]
        [Tooltip("The full sentence or phrase cloud that represents the character's mask. Use [square brackets] to group words into a single draggable phrase.")]
        [ResizableTextArea]
        public string MaskSentence;

        [Header("Phase 2: Reconstruction")]
        [Tooltip("The specific words that form the hidden, true sentence. The player must isolate these from the Mask Words.")]
        public List<string> KeyWords;

        [Header("Phase 3: The Revelation")]
        [Tooltip("The single word in the reconstructed sentence that is the 'Keystone' to the final truth.")]
        public string KeystoneWord;

        [Tooltip("The vulnerable word that the Keystone Word transforms into upon being correctly identified.")]
        public string VulnerableWord;
    }
}