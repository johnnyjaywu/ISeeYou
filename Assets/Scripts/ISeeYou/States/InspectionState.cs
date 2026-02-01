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
                if (word != null) word.OnWordConfirmed -= OnWordSelected;
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
                word.OnWordConfirmed += OnWordSelected;
                activeWords.Add(word);

                if (word.IsKey)
                {
                    totalMasks++;
                }
            }
        }

        private void OnWordSelected(Words words)
        {
            if (isComplete || words.IsRevealed) return;

            HandleWordReveal(words).Run();
        }

        private IEnumerator HandleWordReveal(Words words)
        {
            if (words.IsKey)
            {
                // 1. Trigger the animation. 
                // We wait for the click animation + the typewriter to finish.
                bool animationFinished = false;
        
                // RevealSubtext should now accept an onComplete callback 
                // (Update your Words.cs RevealSubtext to pass this through to UGUIAnimator)
                words.RevealSubtext(() => animationFinished = true);

                // 2. Wait until the word says it is done
                // TODO: Fix animation issues
                // yield return new WaitUntil(() => animationFinished);
                yield return new WaitForSeconds(1f);

                // 3. Logic only happens AFTER the visual "Reveal" is complete
                // if (data.RevealSound != null) SoundManager.Instance.Play(data.RevealSound);

                revealedMasks++;
        
                if (revealedMasks >= totalMasks)
                {
                    isComplete = true;
                    sequenceHandle = ExitSequence().Run();
                }
            }
            else
            {
                // For duds, we still might want to wait for the "Click" punch animation
                // to finish so the player feels the impact before the dud sound.
                // if (data.DudSound != null) SoundManager.Instance.Play(data.DudSound);
            }
        }

        private IEnumerator ExitSequence()
        {
            Debug.Log("[InspectionState] All Masks Revealed! Truth Exposed.");
            
            // Lock Input
            manager.SetInputActive(false);
            

            // if (data.PhaseCompleteStinger != null)
            // {
            //     SoundManager.Instance.Play(data.PhaseCompleteStinger);
            // }

            yield return new WaitForSeconds(3f);
            
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