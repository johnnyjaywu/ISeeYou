using System;
using System.Collections.Generic;
using ContentContent;
using ContentContent.Audio;
using ContentContent.UI;
using NaughtyAttributes;
using UnityEngine;

namespace ISeeYou
{
    [RequireComponent(typeof(WordsSpawner))]
    public class Speaker : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private DropZone speechBubble;
        [SerializeField] private DropZone maskingBubble;
        [ReadOnly, SerializeField] private DialogLine currentDialogLine;
        [SerializeField] private ConversationController conversationController;

        public string ID => id;

        // Dependencies
        private WordsSpawner defaultSpawner;
        private SoundHandle soundHandle;

        // State
        private List<Words> activeWords = new List<Words>(); // the active words being displayed
        private List<Words> activeMaskWords = new List<Words>(); // The active thought words being displayed
        private Queue<string> currentMaskingLines = new Queue<string>();

        private bool HaveMaskRemaining => currentMaskingLines.Count > 0;

        private void Awake()
        {
            defaultSpawner = GetComponent<WordsSpawner>();
            SpeakerManager.Register(this);
            maskingBubble.OnContentCountChanged += MaskingBubbleOnOnContentCountChanged;

            speechBubble.SetLock(true);
        }

        private void MaskingBubbleOnOnContentCountChanged(int count)
        {
            if (count == 0)
            {
                ShowNextMask();
            }
            else
            {
                maskingBubble.CapCountToCurrent();
            }
        }

        private void OnDestroy()
        {
            maskingBubble.OnContentCountChanged -= MaskingBubbleOnOnContentCountChanged;
        }

        private void HandleMaskWordClicked(Words words)
        {
            if (activeMaskWords.Contains(words))
            {
                // maskingBubble.SetLock(true);
                words.FadeOut(1f, () =>
                {
                    words.OnWordClicked -= HandleMaskWordClicked;
                    Destroy(words.gameObject);
                    activeMaskWords.Remove(words);
                });
            }
        }

        public void Speak(DialogLine dialogLineToPlay)
        {
            if (HaveMaskRemaining)
                return; // If there are active mask, don't speak until my thoughts are "cleared"

            bool haveMasking = !dialogLineToPlay.maskingLines.IsNullOrEmpty();

            // Always disable input for a bit
            SetEnableContinueInput(false);
            currentDialogLine = dialogLineToPlay;

            // Display the "truth" underneath
            ShowCurrentLine();

            if (haveMasking)
            {
                // This should only be called once until currentThoughts have emptied
                currentMaskingLines = new Queue<string>(currentDialogLine.maskingLines);

                ShowNextMask();
                // if (currentDialogLine.autoPlay)
                // {
                //     DoAutoPlay();
                // }
                // else
                // {
                //     ShowNextMask();
                // }
            }
        }

        public void ShowNextMask()
        {
            // No more masking lines, finish
            if (currentDialogLine.maskingLines.IsNullOrEmpty() || currentMaskingLines.Count == 0)
            {
                // if (activeMaskWords.Count > 0)
                // {
                //     ClearMaskingWords();
                // }

                // var maskLayout = maskingBubble.GetComponent<FlexLayoutGroup>();
                // maskLayout.MinSize = Vector2.zero;
                RevealTruth();
                return;
            }

            Debug.Log("Showing Mask");
            // Grab the next masking Line
            string nextMaskingLine = currentMaskingLines.Dequeue();

            // Check if the maskingBubble has its own spawner
            var currentSpawner = defaultSpawner;
            var maskingWordsSpawner = maskingBubble.GetComponent<WordsSpawner>();
            if (maskingWordsSpawner != null)
                currentSpawner = maskingWordsSpawner;

            maskingBubble.SetLock(true);

            // Spawn mask words
            activeMaskWords = currentSpawner.SpawnWords(nextMaskingLine, maskingBubble.transform);
            currentSpawner.SpawnWithInterval(nextMaskingLine, maskingBubble.transform, OnFinishSpawningMaskWords);

            if (currentDialogLine.autoPlay)
            {
                DoAutoPlay();
            }
        }

        private void OnFinishSpawningMaskWords(List<Words> words)
        {
            activeMaskWords = words;
            foreach (Words word in activeMaskWords)
            {
                word.OnWordClicked += HandleMaskWordClicked;
            }

            maskingBubble.CapCountToCurrent();
            maskingBubble.SetLock(currentDialogLine.autoPlay);
        }

        public void ShowCurrentLine()
        {
            // if (currentDialogLine.autoPlay)
            // {
            //     activeWords = defaultSpawner.SpawnWords(currentDialogLine.text, speechBubble.transform, false);
            //     
            //     var speechLayout = speechBubble.GetComponent<FlexLayoutGroup>();
            //     speechLayout.CalculateLayoutInputHorizontal();
            //     var maskLayout = maskingBubble.GetComponent<FlexLayoutGroup>();
            //     maskLayout.MinSize = new Vector2(speechLayout.preferredWidth, speechLayout.preferredHeight);
            // }

            float interval = currentDialogLine.autoPlay ? 0 : 0.1f;
            defaultSpawner.SpawnWithInterval(currentDialogLine.text, speechBubble.transform, OnFinishSpawningSpeech,
                interval);
        }

        private void OnFinishSpawningSpeech(List<Words> words)
        {
            activeWords = words;

            if (!HaveMaskRemaining)
            {
                RevealTruth();
            }
            else
            {
                foreach (Words word in activeWords)
                {
                    word.SetVisible(false);
                    word.SetActive(false);
                }
            }
        }

        public void Stop()
        {
            ClearWords();
            ClearMaskingWords();

            if (soundHandle is { IsPlaying: true })
                soundHandle.Stop();
        }

        public void ClearWords()
        {
            activeWords.Clear();
            if (defaultSpawner != null && speechBubble != null)
            {
                defaultSpawner.Clear(speechBubble.transform);
            }
        }

        public void ClearMaskingWords()
        {
            activeMaskWords.Clear();
            if (defaultSpawner != null && maskingBubble != null)
            {
                defaultSpawner.Clear(maskingBubble.transform);
            }
        }

        private void SetEnableContinueInput(bool enableInput)
        {
            if (conversationController == null) return;
            if (enableInput) conversationController.EnableInput();
            else conversationController.DisableInput();
        }

        private void RevealTruth()
        {
            // We don't actually reveal if auto play
            if (currentDialogLine.autoPlay)
            {
                // Timer.Countdown(3f, this).OnFinish(() => conversationController.PlayNextLine());
                conversationController.PlayNextLine();
                return;
            }
         
            // Free the input
            Timer.Countdown(1f, this).OnFinish(() => SetEnableContinueInput(true));
   
            // Show the words 
            foreach (Words word in activeWords)
            {
                word.SetActive(true);
                word.SetVisible(true);
            }

            // Play Audio
            if (currentDialogLine.voiceLine != null)
            {
                if (soundHandle is { IsPlaying: true })
                    soundHandle.Stop();
                soundHandle = currentDialogLine.voiceLine.Play();
            }
        }

        private void DoAutoPlay()
        {
            maskingBubble.SetLock(true);
            Timer.Countdown(3f, this).OnFinish(() =>
            {
                ShowNextMask();
            });
        }
    }
}