using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentContent;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou
{
    public enum GamePhase
    {
        Initial,
        Filtering,
        Revelation,
        End
    }

    public class ConversationManager : MonoBehaviour
    {
        [Header("Game Data")]
        [Expandable, Required]
        [SerializeField] private ConversationData conversation;

        [Header("Scene References")]
        [SerializeField] private Words wordsPrefab;

        [SerializeField] private SpeechLayoutController speechLayoutController;
        [SerializeField] private RectTransform focusZone;
        [SerializeField] private Button focusButton;
        [SerializeField] private TransitionController transitionController;

        [Header("Game Settings")]
        [SerializeField] private int maxTries = 3;

        private GamePhase currentPhase;
        private List<Words> wordsList = new();
        private int triesRemaining;
        private CoroutineHandle rebuildHandle;

        public List<Words> WordsList => wordsList;

        private void Start()
        {
            if (conversation == null)
            {
                Debug.LogError("No ConversationData assigned!");
                return;
            }

            triesRemaining = maxTries;
            transitionController.GoToNextPhase(currentPhase, OnTransitionFinished);
            // SpawnWordsForPhase();

            focusButton.onClick.AddListener(OnFocusButtonClicked);
            focusButton.SetActive(false);
        }

        private void OnTransitionFinished(GamePhase newPhase)
        {
            Debug.Log($"Transition finished! New phase: {newPhase}");
            currentPhase = newPhase;
            switch (currentPhase)
            {
                case GamePhase.Initial:
                    break;
                case GamePhase.Filtering:
                    break;
                case GamePhase.Revelation:
                    focusButton.SetActive(true);
                    break;
                case GamePhase.End:
                    rebuildHandle = RebuildWithDelay().Run();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SpawnWordsForPhase(GamePhase phase)
        {
            ClearWordsList();
            string sentence = phase == GamePhase.Filtering
                ? conversation.MaskSentence
                : conversation.FilteredSentence;
            List<string> parsedWords = ParseSentence(sentence);

            foreach (string wordText in parsedWords)
            {
                bool isKey = IsKeyWord(wordText, phase);
                string truth = phase == GamePhase.Revelation ? conversation.GetTruth(wordText) : "";
                Words words = Instantiate(wordsPrefab);
                words.Initialize(wordText, isKey, focusZone, phase == GamePhase.Filtering, truth);
                words.OnStateChanged += OnWordStateChanged;
                // words.SetVisible(false);
                wordsList.Add(words);
            }

            speechLayoutController.Rebuild(wordsList);
        }

        private void OnWordStateChanged()
        {
            if (currentPhase == GamePhase.Filtering)
            {
                if (CheckFilteredWords()) transitionController.GoToNextPhase(currentPhase, OnTransitionFinished);
            }
            else if (currentPhase == GamePhase.Revelation)
            {
                OnWordSelectedStateChanged();
            }
        }

        private bool CheckFilteredWords()
        {
            List<Words> wordsInZone = wordsList.Where(w => w.IsInZone).ToList();
            List<string> textInZone = wordsInZone.Select(w => w.Text).ToList();

            return new HashSet<string>(textInZone).SetEquals(new HashSet<string>(conversation.FilteredWords));
        }

        private void OnWordSelectedStateChanged()
        {
            focusButton.interactable = wordsList.Any(w => w.IsSelected);
        }

        private void OnFocusButtonClicked()
        {
            if (triesRemaining <= 0) return;

            var selectedWords = wordsList.Where(w => w.IsSelected).ToList();
            var selectedKeyWords = selectedWords.Where(w => w.IsKeyWord).ToList();
            var truthWords = new HashSet<string>(conversation.MaskedWords);

            bool foundAllKeys = selectedKeyWords.Count == truthWords.Count;
            bool noIncorrectSelected = selectedWords.Count == selectedKeyWords.Count;

            if (foundAllKeys && noIncorrectSelected)
            {
                // foreach (Words word in selectedKeyWords)
                // {
                //     word.RevealTruth();
                // }

                focusButton.SetActive(false);
                transitionController.GoToNextPhase(currentPhase, OnTransitionFinished);
            }
            else
            {
                triesRemaining--;
                Debug.Log($"Incorrect guess. Tries remaining: {triesRemaining}");

                foreach (Words word in selectedWords)
                {
                    word.ResetVisuals();
                }

                if (triesRemaining <= 0)
                {
                    Debug.Log("Out of tries! You failed.");
                }
                else
                {
                    ResetIncorrectGuessVisuals(selectedWords).Run();
                }
            }
        }

        private IEnumerator RebuildWithDelay()
        {
            yield return new WaitForEndOfFrame();
            speechLayoutController.Rebuild(wordsList);
        }

        private IEnumerator ResetIncorrectGuessVisuals(List<Words> wordsToReset)
        {
            yield return new WaitForSeconds(1);
            foreach (Words word in wordsToReset)
            {
                word.ResetVisuals();
            }

            focusButton.interactable = false;
        }

        private List<string> ParseSentence(string sentence)
        {
            var results = new List<string>();
            var regex = new Regex(@"\[[^\]]+\]|[\w'-]+");
            var matches = regex.Matches(sentence);

            foreach (Match match in matches)
            {
                string matchText = match.Value;
                if (match.Value.StartsWith("["))
                {
                    matchText = match.Value.Substring(1, match.Value.Length - 2);
                }

                //Trim
                string trimmed = matchText.TrimEnd('.', ',', '!', '?', '…', ' ');

                results.Add(trimmed);
            }

            return results;
        }

        private void ClearWordsList()
        {
            foreach (Words word in wordsList)
            {
                Destroy(word.gameObject);
            }

            wordsList.Clear();
        }

        private bool IsKeyWord(string word, GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Filtering:
                    return conversation.FilteredWords.Contains(word);
                case GamePhase.Revelation:
                    return conversation.MaskedWords.Contains(word);
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }
    }
}