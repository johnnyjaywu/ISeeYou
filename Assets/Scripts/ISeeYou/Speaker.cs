using System.Collections.Generic;
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

        public string ID => id;

        // Dependencies
        private WordsSpawner spawner;
        private SoundHandle soundHandle;

        // State
        private List<Words> currentActiveWords = new List<Words>();

        private void Awake()
        {
            spawner = GetComponent<WordsSpawner>();
            SpeakerManager.Register(this);
        }

        public void Speak(DialogLine dialogLineToPlay)
        {
            currentDialogLine = dialogLineToPlay;
            currentActiveWords = spawner.SpawnWords(dialogLineToPlay.text, speechBubble);
            speechBubble.SetLock(true);

            // Play Audio
            if (dialogLineToPlay.voiceLine != null)
            {
                if (soundHandle is { IsPlaying: true })
                    soundHandle.Stop();
                soundHandle = dialogLineToPlay.voiceLine.Play();
            }
        }

        public void Think(DialogLine dialogLineToPlay)
        {
            currentDialogLine = dialogLineToPlay;
            currentActiveWords = spawner.SpawnWords(dialogLineToPlay.text, thoughtBubble);
            thoughtBubble.SetLock(true);

            // Play Audio
            if (dialogLineToPlay.voiceLine != null)
            {
                if (soundHandle is { IsPlaying: true })
                    soundHandle.Stop();
                soundHandle = dialogLineToPlay.voiceLine.Play();
            }
        }

        public void Stop()
        {
            ClearWords();

            if (soundHandle is { IsPlaying: true })
                soundHandle.Stop();
        }

        public void ClearWords()
        {
            currentActiveWords.Clear();
            if (spawner != null && speechBubble != null)
            {
                spawner.Clear(speechBubble.transform);
            }
        }

        [Button]
        public void Lock() => speechBubble?.SetLock(true);

        [Button]
        public void Unlock() => speechBubble?.SetLock(false);
    }
}