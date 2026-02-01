using System;
using System.Collections.Generic;
using ContentContent;
using ContentContent.Audio;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ISeeYou
{
    public class ConversationController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;

        [SerializeField] private List<DialogLine> lines = new();

        private int currentLineIndex = -1;

        [ReadOnly, SerializeField] private DialogLine currentDialogLine;

        [Button]
        public void PlayNextLine()
        {
            int nextIndex = currentLineIndex + 1;

            if (!lines.IsNullOrEmpty() && nextIndex < lines.Count)
            {
                PlayLine(nextIndex);
            }
        }

        public void PlayLine(int index)
        {
            StopCurrentLine();
            
            // Sync internal state so PlayNextLine() works relative to this one
            currentLineIndex = index;
            currentDialogLine = lines[index];

            // Spawn Text
            currentDialogLine.Speak();
        }

        /// <summary>
        /// Stops audio and clears all visual text immediately.
        /// </summary>
        public void StopCurrentLine()
        {
            currentDialogLine.Stop();
        }
    }


    // Extension method helper for cleaner index checking
    public static class CollectionExtensions
    {
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return list != null && index >= 0 && index < list.Count;
        }
    }
}