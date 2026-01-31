using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ContentContent; 
using ContentContent.Audio; 
using UnityEngine;

namespace ISeeYou
{
    public class ReconstructionState : IState
    {
        private readonly ConversationManager manager;
        private readonly DropZone dropZone;
        private readonly ConversationData data;
        
        private CoroutineHandle sequenceHandle;
        private bool isComplete;
        private List<WordsData> fullTargetSequence;
        private List<WordsData> skeletonSequence; 

        public ReconstructionState(ConversationManager conversationManager)
        {
            manager = conversationManager;
            dropZone = manager.SpeechBubble;
            data = manager.CurrentData;
        }

        public void Enter()
        {
            Debug.Log("[ReconstructionState] Entering Phase 2: Reconstruction.");
            isComplete = false;
            
            fullTargetSequence = data.GetMonologueWords();

            // Derive Skeleton from Monologue Keys ONLY
            skeletonSequence = fullTargetSequence.Where(w => w.IsKey).ToList();
            
            sequenceHandle = EnterSequence().Run();
        }

        public void Exit()
        {
            dropZone.OnContentChanged -= CheckWinCondition;
            // dropZone.IsLocked = false;
            if (sequenceHandle is { IsRunning: true }) sequenceHandle.Stop();
        }

        public void Update() { }

        private IEnumerator EnterSequence()
        {
            manager.SetInputActive(false);

            PrepareWordsForPhase();

            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<ReconstructionIntro>();
            }

            // dropZone.IsLocked = true;
            manager.SetInputActive(true);
            
            dropZone.OnContentChanged += CheckWinCondition;
            CheckWinCondition();
        }

        private void PrepareWordsForPhase()
        {
            var allWords = dropZone.GetComponentsInChildren<Words>();
            foreach (var word in allWords)
            {
                if (word.TryGetComponent(out Draggable drag)) drag.enabled = true;
                word.SetInteractable(false);
                word.ResetVisuals(); 
            }
        }

        private void CheckWinCondition()
        {
            if (isComplete) return;

            var currentOrder = dropZone.GetComponentsInChildren<Words>();
            if (currentOrder.Length < skeletonSequence.Count) return;

            var boardRelevantKeys = new List<string>();
            var targetKeysText = skeletonSequence.Select(k => k.Text.ToLowerInvariant()).ToList();

            foreach (var word in currentOrder)
            {
                string text = word.Text.ToLowerInvariant();
                if (targetKeysText.Contains(text))
                {
                    boardRelevantKeys.Add(text);
                }
            }

            if (boardRelevantKeys.Count != skeletonSequence.Count) return;

            for (int i = 0; i < skeletonSequence.Count; i++)
            {
                if (boardRelevantKeys[i] != skeletonSequence[i].Text.ToLowerInvariant())
                    return; 
            }

            isComplete = true;
            sequenceHandle = ExitSequence().Run();
        }

        private IEnumerator ExitSequence()
        {
            Debug.Log("[ReconstructionState] Keys Ordered Correctly! Rebuilding sentence...");
            
            manager.SetInputActive(false);
            dropZone.OnContentChanged -= CheckWinCondition;

            ConstructFullSentence();
            
            yield return new WaitForSeconds(0.5f);

            // if (data.PhaseCompleteStinger != null)
            // {
            //     SoundManager.Instance.Play(data.PhaseCompleteStinger);
            // }

            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<ReconstructionOutro>();
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            Debug.Log("Transitioning to Inspection (Phase 3)...");
            manager.StateMachine.ChangeState(new InspectionState(manager));
        }

        private void ConstructFullSentence()
        {
            var currentWords = dropZone.GetComponentsInChildren<Words>().ToList();
            
            for (int i = 0; i < fullTargetSequence.Count; i++)
            {
                WordsData targetData = fullTargetSequence[i];
                Words wordObject = null;

                // Match existing words
                var existingMatch = currentWords.FirstOrDefault(w => 
                    w.Text.Equals(targetData.Text, System.StringComparison.OrdinalIgnoreCase));

                if (existingMatch != null)
                {
                    wordObject = existingMatch;
                    currentWords.Remove(existingMatch);
                    wordObject.Initialize(targetData); 
                }
                else
                {
                    // Spawn missing fillers
                    wordObject = manager.SpawnWord(targetData, dropZone.transform);

                    // FIX: Ensure new words are set to "Layout Mode" immediately.
                    // Otherwise, their physics component will think they are in "Noise Mode" and float away.
                    var physics = wordObject.GetComponent<UIPhysics>();
                    if (physics != null)
                    {
                        // physics.SetLayoutState(true);
                    }
                }

                wordObject.transform.SetSiblingIndex(i);
                wordObject.ResetVisuals();
                wordObject.SetInteractable(false);
            }

            // Destroy leftovers
            foreach (var leftover in currentWords)
            {
                Object.Destroy(leftover.gameObject);
            }
        }
    }
}