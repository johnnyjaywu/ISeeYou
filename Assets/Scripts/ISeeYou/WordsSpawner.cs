using System;
using System.Collections;
using System.Collections.Generic;
using ContentContent;
using UnityEngine;

namespace ISeeYou
{
    public class WordsSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Words wordPrefab;

        private CoroutineHandle spawnRoutine;

        public List<Words> SpawnWords(string text, Transform parent, bool splitWords = true)
        {
            // Auto-clear before spawning to ensure fresh state
            Clear(parent);

            if (parent == null || wordPrefab == null)
            {
                Debug.LogError("[WordsSpawner] Missing Container or Prefab.");
                return null;
            }

            var parsedTokens = splitWords ? Words.ParseGroupedString(text) : new List<string> { text };
            var spawnedWords = new List<Words>(parsedTokens.Count);
            foreach (string token in parsedTokens)
            {
                Words newWord = Instantiate(wordPrefab, parent.transform);
                newWord.Initialize(new WordsData { Text = token });
                spawnedWords.Add(newWord);
            }

            return spawnedWords;
        }

        public void SpawnWithInterval(string text, Transform parent, Action<List<Words>> onFinish = null,
            float interval = 0)
        {
            // Auto-clear before spawning to ensure fresh state
            Clear(parent.transform);

            if (parent == null || wordPrefab == null)
            {
                Debug.LogError("[WordsSpawner] Missing Container or Prefab.");
                return;
            }

            var parsedTokens = Words.ParseGroupedString(text);
            if (spawnRoutine is { IsRunning: true })
                spawnRoutine.Stop();
            spawnRoutine = SpawnInterval(parsedTokens, parent, onFinish, interval).Run();
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

        private IEnumerator SpawnInterval(List<string> parsedTokens, Transform parent, Action<List<Words>> onFinish,
            float interval = 0f)
        {
            var spawnedWords = new List<Words>(parsedTokens.Count);
            if (interval == 0)
                interval = 1f / parsedTokens.Count;

            foreach (string token in parsedTokens)
            {
                Words newWord = Instantiate(wordPrefab, parent.transform);
                newWord.Initialize(new WordsData { Text = token });
                spawnedWords.Add(newWord);
                yield return new WaitForSeconds(interval);
            }

            onFinish?.Invoke(spawnedWords);
        }
    }
}