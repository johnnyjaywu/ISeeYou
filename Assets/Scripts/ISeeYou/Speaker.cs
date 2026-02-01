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
        [SerializeField] private DropZone thoughtBubble;
        [ReadOnly, SerializeField] private DialogLine currentDialogLine;
        [SerializeField] private ConversationController conversationController;

        public string ID => id;

        // Dependencies
        private WordsSpawner spawner;
        private SoundHandle soundHandle;

        // State
        private List<Words> activeWords = new List<Words>(); // the active words being displayed
        private List<Words> activeThoughtWords = new List<Words>(); // The active thought words being displayed
        private Queue<string> currentThoughts = new Queue<string>();
        private int currentThoughtIndex = 0;

        private void Awake()
        {
            spawner = GetComponent<WordsSpawner>();
            SpeakerManager.Register(this);
            thoughtBubble.OnZoneExit += OnZoneExit;
        }

        private void OnDestroy()
        {
            thoughtBubble.OnZoneExit -= OnZoneExit;
        }

        private void OnZoneExit(Draggable obj)
        {
            Words words = obj.GetComponent<Words>();
            if (words == null) return;
            if (activeThoughtWords.Contains(words))
            {
                thoughtBubble.SetLock(true);
                words.FadeOut(() =>
                {
                    Destroy(words.gameObject);
                    ShowNextThought();
                });
                activeThoughtWords.Remove(words);
            }
        }

        public void Speak(DialogLine dialogLineToPlay)
        {
            if (currentThoughts.Count > 0)
                return; // If there are active thoughts, don't speak until my thoughts are "cleared"

            bool haveThoughts = !dialogLineToPlay.thoughts.IsNullOrEmpty();

            // Force disable click to continue input
            if (haveThoughts && conversationController != null)
                conversationController.DisableInput();

            currentDialogLine = dialogLineToPlay;
            ShowCurrentLine();

            if (haveThoughts)
            {
                // This should only be called once until currentThoughts have emptied
                currentThoughts = new Queue<string>(currentDialogLine.thoughts);
                ShowNextThought();
            }
        }

        public void ShowNextThought()
        {
            if (currentDialogLine.thoughts.IsNullOrEmpty() || currentThoughts.Count == 0)
            {
                // No more thoughts, free the input
                if (conversationController != null)
                    conversationController.EnableInput();
                return;
            }

            string nextThought = currentThoughts.Dequeue();
            activeThoughtWords = spawner.SpawnWords(nextThought, thoughtBubble.transform, false);
            activeThoughtWords[0].ShakeInterval();
            thoughtBubble.SetLock(false);
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
            spawner.SpawnWithInterval(currentDialogLine.text, speechBubble.transform, true, OnFinishSpawningSpeech);
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
            if (spawner != null && speechBubble != null)
            {
                spawner.Clear(speechBubble.transform);
            }
        }

        public void ClearThoughts()
        {
            activeThoughtWords.Clear();
            if (spawner != null && thoughtBubble != null)
            {
                spawner.Clear(thoughtBubble.transform);
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