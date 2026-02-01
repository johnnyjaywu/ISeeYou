using System;
using System.Collections.Generic;
using ContentContent;
using ContentContent.Audio;
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
        private Queue<string> currentMaskingWords = new Queue<string>();
        private int currentMaskingIndex = 0;

        private bool HaveMaskRemaining => currentMaskingWords.Count > 0;
        
        private void Awake()
        {
            defaultSpawner = GetComponent<WordsSpawner>();
            SpeakerManager.Register(this);
            maskingBubble.OnZoneExit += OnZoneExit;
        }

        private void OnDestroy()
        {
            maskingBubble.OnZoneExit -= OnZoneExit;
        }

        private void OnZoneExit(Draggable obj)
        {
            if (obj == null) return;
            
            Words words = obj.GetComponent<Words>();
            if (words == null) return;
            if (activeMaskWords.Contains(words))
            {
                maskingBubble.SetLock(true);
                words.FadeOut(() =>
                {
                    maskingBubble.SetLock(false);
                    Destroy(words.gameObject);
                    activeMaskWords.Remove(words);
                    if (activeMaskWords.Count == 0)
                        ShowNextMask();
                });
            }
        }

        public void Speak(DialogLine dialogLineToPlay)
        {
            if (HaveMaskRemaining)
                return; // If there are active thoughts, don't speak until my thoughts are "cleared"

            bool haveMasking = !dialogLineToPlay.maskingWords.IsNullOrEmpty();
            
            // Always disable input for a bit
            SetEnableContinueInput(false);
            currentDialogLine = dialogLineToPlay;
            ShowCurrentLine();

            if (haveMasking)
            {
                // This should only be called once until currentThoughts have emptied
                currentMaskingWords = new Queue<string>(currentDialogLine.maskingWords);
                ShowNextMask();
            }
        }

        public void ShowNextMask()
        {
            if (currentDialogLine.maskingWords.IsNullOrEmpty() || currentMaskingWords.Count == 0)
            {
                // No more thoughts, free the input
                SetEnableContinueInput(true);
                return;
            }

            Debug.Log("Showing Mask");
            string nextMaskingWords = currentMaskingWords.Dequeue();
            // Check if the maskingBubble has its own spawner
            var currentSpawner = defaultSpawner;
            var maskingWordsSpawner = maskingBubble.GetComponent<WordsSpawner>();
            if (maskingWordsSpawner != null)
                currentSpawner = maskingWordsSpawner;
            
            activeMaskWords = currentSpawner.SpawnWords(nextMaskingWords, maskingBubble.transform);
            // foreach(Words words in activeMaskWords)
            //     words.ShakeInterval();
            maskingBubble.SetLock(false);
        }
        
        // private void OnFinishSpawningThought(List<Words> words)
        // {
        //     activeWords = words;
        //     SetEnableContinueInput(true);
        // }
        
        public void ShowCurrentLine()
        {
            // activeWords = spawner.SpawnWords(currentDialogLine.text, speechBubble);
            SetEnableContinueInput(false);
            defaultSpawner.SpawnWithInterval(currentDialogLine.text, speechBubble.transform, true, OnFinishSpawningSpeech);
            speechBubble.SetLock(true);

            // Play Audio
            if (currentDialogLine.voiceLine != null)
            {
                if (soundHandle is { IsPlaying: true })
                    soundHandle.Stop();
                soundHandle = currentDialogLine.voiceLine.Play();
            }
        }

        private void OnFinishSpawningSpeech(List<Words> words)
        {
            activeWords = words;
            if (!HaveMaskRemaining)
                SetEnableContinueInput(true);
        }

        public void Stop()
        {
            ClearWords();
            ClearThoughts();

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

        public void ClearThoughts()
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
    }
}