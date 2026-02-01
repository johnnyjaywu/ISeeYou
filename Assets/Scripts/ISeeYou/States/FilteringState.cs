using System.Collections;
using System.Linq;
using ContentContent; 
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class FilteringState : IState
    {
        private readonly ConversationManager manager;
        private readonly DropZone dropZone;
        private readonly ConversationData data;
        
        private SoundHandle noiseLoopHandle;
        private bool isComplete;
        private int requiredKeyCount;
        private CoroutineHandle sequenceHandle;

        public FilteringState(ConversationManager conversationManager)
        {
            manager = conversationManager;
            dropZone = manager.SpeechBubble;
            data = manager.CurrentData;
        }

        public void Enter()
        {
            Debug.Log("[FilteringState] Entering Phase 1...");
            isComplete = false;
            var noiseWords = data.GetNoiseWords();
            requiredKeyCount = noiseWords.Count(w => w.IsKey);
            
            sequenceHandle = EnterSequence().Run();
        }

        public void Exit()
        {
            // dropZone.OnContentChanged -= CheckWinCondition;
            
            if (sequenceHandle is { IsRunning: true }) sequenceHandle.Stop();
            if (noiseLoopHandle is { IsValid: true }) noiseLoopHandle.Stop();
        }

        public void Update() { }

        private IEnumerator EnterSequence()
        {
            manager.SpawnNoisePhase();
            
            manager.SetInputActive(false); 
            
            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<FilteringIntro>();
            }
            
            // dropZone.OnContentChanged += CheckWinCondition;
            
            manager.SetInputActive(true);
            
            if (data.Ambient != null)
            {
                noiseLoopHandle = SoundManager.Instance.Play(data.Ambient);
            }
        }

        private void CheckWinCondition()
        {
            if (isComplete) return;

            var wordsInBubble = dropZone.GetComponentsInChildren<Words>();
            int foundKeys = 0;
            int wrongWords = 0;

            foreach (var word in wordsInBubble)
            {
                if (word.IsKey) foundKeys++;
                else wrongWords++;
            }

            // Audio Feedback Logic (Optional tweak)
            // Reduce volume as they get closer to the clean solution
            if (noiseLoopHandle is { IsValid: true })
            {
                float progress = (float)foundKeys / Mathf.Max(1, requiredKeyCount);
                // If there is junk in the bubble, keep noise volume slightly higher to indicate "not done"
                float targetVol = Mathf.Lerp(1.0f, 0.0f, progress);
                if (wrongWords > 0) targetVol = Mathf.Max(targetVol, 0.3f); 
                
                noiseLoopHandle.SetVolume(Mathf.Clamp01(targetVol));
            }

            // STRICT VALIDATION:
            // 1. Must have zero wrong words (Noise)
            if (wrongWords > 0) return;

            // 2. Must have all required keys
            if (foundKeys >= requiredKeyCount)
            {
                isComplete = true;
                sequenceHandle = ExitSequence().Run();
            }
        }

        private IEnumerator ExitSequence()
        {
            Debug.Log("[FilteringState] Puzzle Solved! Playing Outro...");

            manager.SetInputActive(false);
            
            // dropZone.OnContentChanged -= CheckWinCondition;

            // if (data.PhaseCompleteStinger != null)
            // {
            //     SoundManager.Instance.Play(data.PhaseCompleteStinger);
            // }

            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<FilteringOutro>();
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            Debug.Log("Transitioning to Reconstruction (Phase 2)...");
            manager.StateMachine.ChangeState(new ReconstructionState(manager));
        }
    }
}