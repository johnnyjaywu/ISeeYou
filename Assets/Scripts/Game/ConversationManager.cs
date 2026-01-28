using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou
{
    public class ConversationManager : MonoBehaviour
    {
        [Header("Game Data")]
        [Expandable]
        [SerializeField] private ConversationData currentConversation;

        [Header("Scene References")]
        [SerializeField] private Words wordsPrefab;
        [SerializeField] private HorizontalLayoutGroup linePrefab;
        [SerializeField] private VerticalLayoutGroup linesContainer;
        [SerializeField] private RectTransform focusZone;

        public enum GamePhase { Deconstruction, Reconstruction, Revelation }
        public GamePhase CurrentPhase { get; private set; }

        private List<Words> allWords = new List<Words>();

        private void Start()
        {
            if (currentConversation == null)
            {
                Debug.LogError("No ConversationData assigned!");
                return;
            }

            CurrentPhase = GamePhase.Deconstruction;
            SpawnWords();
        }

        private void SpawnWords()
        {
            if (wordsPrefab == null || linePrefab == null || linesContainer == null)
            {
                Debug.LogError("Prefab or Container not assigned!");
                return;
            }

            foreach (Transform child in linesContainer.transform)
            {
                Destroy(child.gameObject);
            }
            allWords.Clear();

            float containerPadding = linesContainer.padding.horizontal;
            float linePadding = linePrefab.padding.horizontal;
            float usableWidth = ((RectTransform)linesContainer.transform).rect.width - containerPadding - linePadding;
            float lineSpacing = linePrefab.spacing;

            List<string> parsedWords = ParseMaskSentence(currentConversation.MaskSentence);
            
            HorizontalLayoutGroup currentLine = CreateNewLine();
            float currentLineWidth = 0f;

            foreach (string wordText in parsedWords)
            {
                Words words = Instantiate(wordsPrefab);
                
                string cleanedText = wordText.TrimEnd('.', ',', '!', '?', '…', ' ');
                words.Initialize(cleanedText, currentConversation.KeyWords.Contains(cleanedText), focusZone);
                words.OnStateChanged += OnWordStateChanged;
                allWords.Add(words);
                
                float wordWidth = words.GetComponent<LayoutElement>().preferredWidth;

                bool isFirstWord = currentLineWidth == 0f;
                float requiredWidth = isFirstWord ? wordWidth : lineSpacing + wordWidth;

                if (!isFirstWord && currentLineWidth + requiredWidth > usableWidth)
                {
                    currentLine = CreateNewLine();
                    words.transform.SetParent(currentLine.transform, false);
                    currentLineWidth = wordWidth;
                }
                else
                {
                    words.transform.SetParent(currentLine.transform, false);
                    currentLineWidth += requiredWidth;
                }
            }
        }

        private void OnWordStateChanged()
        {
            CheckPhaseCompletion();
        }

        private void CheckPhaseCompletion()
        {
            if (CurrentPhase != GamePhase.Deconstruction) return;

            List<Words> wordsInZone = allWords.Where(w => w.IsInFocusZone).ToList();
            List<string> textInZone = wordsInZone.Select(w => w.Text).ToList();
            
            bool isCorrectSet = new HashSet<string>(textInZone).SetEquals(new HashSet<string>(currentConversation.KeyWords));

            // Update visuals for all words based on the collective result.
            foreach (var word in allWords)
            {
                word.SetVisualState(isCorrectSet);
            }

            if (isCorrectSet)
            {
                CurrentPhase = GamePhase.Reconstruction;
                Debug.Log("Deconstruction Complete! The correct words are isolated. Transitioning to Reconstruction phase.");
            }
        }

        private HorizontalLayoutGroup CreateNewLine()
        {
            return Instantiate(linePrefab, linesContainer.transform);
        }

        private List<string> ParseMaskSentence(string sentence)
        {
            var results = new List<string>();
            var regex = new Regex(@"\[[^\]]+\]|[\w'-]+");
            var matches = regex.Matches(sentence);

            foreach (Match match in matches)
            {
                if (match.Value.StartsWith("["))
                {
                    results.Add(match.Value.Substring(1, match.Value.Length - 2));
                }
                else
                {
                    results.Add(match.Value);
                }
            }
            return results;
        }
    }
}