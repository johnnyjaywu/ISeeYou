using System.Collections;
using System.Collections.Generic;
using ContentContent; 
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class InspectionState : IState
    {
        private readonly ConversationManager manager;
        private readonly DropZone dropZone;
        private readonly ConversationData data;
        
        private CoroutineHandle sequenceHandle;
        private bool isComplete;
        
        private int totalMasks;
        private int revealedMasks;
        
        private List<Words> activeWords = new();

        public InspectionState(ConversationManager conversationManager)
        {
            manager = conversationManager;
            dropZone = manager.SpeechBubble;
            data = manager.CurrentData;
        }

        public void Enter()
        {
            Debug.Log("[InspectionState] Entering Phase 3: Inspection.");
            isComplete = false;
            revealedMasks = 0;

            sequenceHandle = EnterSequence().Run();
        }

        public void Exit()
        {
            foreach (var word in activeWords)
            {
                if (word != null) word.OnWordClicked -= OnWordSelected;
            }
            activeWords.Clear();

            if (sequenceHandle is { IsRunning: true }) sequenceHandle.Stop();
        }

        public void Update() { }

        private IEnumerator EnterSequence()
        {
            // Lock Input
            manager.SetInputActive(false);

            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<InspectionIntro>();
            }

            PrepareBoard();
            
            // Enable Input
            manager.SetInputActive(true);
        }

        private void PrepareBoard()
        {
            var words = dropZone.GetComponentsInChildren<Words>();
            
            totalMasks = 0;
            activeWords.Clear();

            foreach (var word in words)
            {
                if (word.TryGetComponent(out Draggable drag)) drag.enabled = false;
                
                word.SetInteractable(true);
                word.OnWordClicked += OnWordSelected;
                activeWords.Add(word);

                if (word.IsKey)
                {
                    totalMasks++;
                }
            }
        }

        private void OnWordSelected(Words word)
        {
            if (isComplete || word.IsRevealed) return;

            if (word.IsKey)
            {
                word.RevealSubtext();
                
                if (data.RevealSound != null) SoundManager.Instance.Play(data.RevealSound);

                revealedMasks++;
                
                if (revealedMasks >= totalMasks)
                {
                    isComplete = true;
                    sequenceHandle = ExitSequence().Run();
                }
            }
            else
            {
                if (data.DudSound != null) SoundManager.Instance.Play(data.DudSound);
            }
        }

        private IEnumerator ExitSequence()
        {
            Debug.Log("[InspectionState] All Masks Revealed! Truth Exposed.");

            // Lock Input
            manager.SetInputActive(false);

            if (data.PhaseCompleteStinger != null)
            {
                SoundManager.Instance.Play(data.PhaseCompleteStinger);
            }

            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<InspectionOutro>();
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            Debug.Log("Transitioning to Revelation (Phase 4)...");
            manager.StateMachine.ChangeState(new RevelationState(manager));
        }
    }
}