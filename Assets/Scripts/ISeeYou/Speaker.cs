using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace ISeeYou
{
    [RequireComponent(typeof(WordsSpawner))]
    public class Speaker : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private DropZone dropZone;
        
        [Header("Data")]
        [SerializeField] private LinesData linesData;

        // Dependencies
        private WordsSpawner spawner;
        
        // State
        private List<Words> currentActiveWords = new List<Words>();
        private int currentLineIndex = -1;

        private void Awake()
        {
            spawner = GetComponent<WordsSpawner>();
        }

        [Button]
        /// <summary>
        /// Advances to the next line in the sequence.
        /// </summary>
        /// <returns>True if a line was played, False if reached the end of the list.</returns>
        public bool PlayNextLine()
        {
            int nextIndex = currentLineIndex + 1;

            if (linesData != null && linesData.Lines.IsValidIndex(nextIndex))
            {
                PlayLine(nextIndex);
                return true;
            }

            Debug.Log("[Speaker] Reached end of lines.");
            return false;
        }
        
        /// <summary>
        /// Plays the specific line index and updates the internal state tracker.
        /// </summary>
        public void PlayLine(int index)
        {
            StopCurrentLine();

            if (linesData == null || !linesData.Lines.IsValidIndex(index))
            {
                Debug.LogWarning($"[Speaker] Cannot play line. Invalid index: {index}");
                return;
            }

            // Sync internal state so PlayNextLine() works relative to this one
            currentLineIndex = index;
            Line lineToPlay = linesData.Lines[index];

            // Play Audio
            if (lineToPlay.voiceLine != null)
            {
                Debug.Log($"[Speaker] Playing Voice: {lineToPlay.voiceLine.name}");
                // audioSource.PlayOneShot(lineToPlay.voiceLine.Clip); 
            }

            // Spawn Text
            if (spawner != null && dropZone != null)
            {
                currentActiveWords = spawner.SpawnPhrase(lineToPlay.text, dropZone);
                dropZone.SetLock(true);
            }
        }

        /// <summary>
        /// Stops audio and clears all visual text immediately.
        /// </summary>
        public void StopCurrentLine()
        {
            // Stop Audio
            // if (audioSource.isPlaying)
            // {
            //     audioSource.Stop();
            // }

            // Clear Lists
            currentActiveWords.Clear();

            // Clear Visuals
            if (spawner != null && dropZone != null)
            {
                spawner.Clear(dropZone.transform);
            }
        }
        
        [Button]
        public void Lock() => dropZone?.SetLock(true);
        [Button]
        public void Unlock() => dropZone?.SetLock(false);
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