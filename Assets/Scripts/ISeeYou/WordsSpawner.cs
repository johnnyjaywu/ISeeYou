using System.Collections.Generic;
using UnityEngine;

namespace ISeeYou
{
    public class WordsSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Words wordPrefab;
        
        public List<Words> SpawnWords(string text, DropZone dropZone, bool splitWords = true)
        {
            // Auto-clear before spawning to ensure fresh state
            Clear(dropZone.transform);

            if (dropZone == null || wordPrefab == null)
            {
                Debug.LogError("[WordsSpawner] Missing Container or Prefab.");
                return null;
            }

            var parsedTokens = splitWords ? Words.ParseGroupedString(text) : new List<string> { text };
            var spawnedWords = new List<Words>(parsedTokens.Count);

            foreach (string token in parsedTokens)
            {
                Words newWord = Instantiate(wordPrefab, dropZone.transform);
                newWord.Initialize(new WordsData { Text = token });
                spawnedWords.Add(newWord);
            }

            return spawnedWords;
        }

        /// <summary>
        /// Destroys all child objects in the specified container.
        /// Publicly exposed so controllers (like Speaker) can manually reset the view.
        /// </summary>
        public void Clear(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject child = container.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                    continue;
                }
#endif
                Destroy(child);
            }
        }
    }
}