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
            if (currentDialogLine.autoPlay) return;
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

            currentDialogLine = dialogLineToPlay;
            bool haveMasking = !dialogLineToPlay.maskingLines.IsNullOrEmpty();
            if (haveMasking)
                currentMaskingLines = new Queue<string>(currentDialogLine.maskingLines);
            
            // Always disable input for a bit
            SetEnableContinueInput(false);

            // Spawn mask first
            if (haveMasking)
            {
                ShowNextMask();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        public void ShowNextMask()
        {
            // No more masking lines, show the real line underneath
            if (currentDialogLine.maskingLines.IsNullOrEmpty() || currentMaskingLines.Count == 0)
            {
                ShowCurrentLine();
                // RevealTruth();
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
                if (!currentDialogLine.autoPlay)
                    word.GetComponent<UIFlashColor>().Flash();
            }

            maskingBubble.CapCountToCurrent();
            maskingBubble.SetLock(currentDialogLine.autoPlay);
        }

        public void ShowCurrentLine()
        {
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
        }

        public void Stop()
        {
            if (autoPlayNextLineHandle.IsValid) autoPlayNextLineHandle.Stop();
            if (autoPlayHandle.IsValid) autoPlayHandle.Stop();
            
            currentDialogLine = default;
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

        private TimerHandle autoPlayNextLineHandle;

        private bool revealing = false;
        private void RevealTruth()
        {
            if (revealing) return;
            revealing = true;
            
            // We don't actually reveal if auto play
            if (currentDialogLine.autoPlay)
            {
                if (autoPlayNextLineHandle.IsValid) autoPlayNextLineHandle.Stop();

                autoPlayNextLineHandle =
                    Timer.Countdown(2f, this).OnFinish(() =>
                    {
                        revealing = false;
                        conversationController.PlayNextLine();
                    });
                // conversationController.PlayNextLine();
                return;
            }
            
            SetEnableContinueInput(true);
            
            // Play Audio
            if (currentDialogLine.voiceLine != null)
            {
                if (soundHandle is { IsPlaying: true })
                    soundHandle.Stop();
                soundHandle = currentDialogLine.voiceLine.Play();
            }

            revealing = false;
        }

        private TimerHandle autoPlayHandle;
        private void DoAutoPlay()
        {
            maskingBubble.SetLock(true);
            if (autoPlayHandle.IsValid) autoPlayHandle.Stop();
            autoPlayHandle = Timer.Countdown(2f, this).OnFinish(ShowNextMask);
        }
    }
}