using System.Collections;
using ContentContent; 
using UnityEngine;
using TMPro;

namespace ISeeYou
{
    public class RevelationState : IState
    {
        private readonly ConversationManager manager;
        private readonly ConversationData data;
        
        private CoroutineHandle sequenceHandle;

        public RevelationState(ConversationManager conversationManager)
        {
            manager = conversationManager;
            data = manager.CurrentData;
        }

        public void Enter()
        {
            Debug.Log("[RevelationState] Entering Phase 4: Revelation.");
            sequenceHandle = EnterSequence().Run();
        }

        public void Exit()
        {
            if (sequenceHandle is { IsRunning: true }) sequenceHandle.Stop();
        }

        public void Update() { }

        private IEnumerator EnterSequence()
        {
            // Lock Input
            manager.SetInputActive(false);

            // 1. Play Intro (The Bubble Fades Out)
            if (manager.Transitions != null)
            {
                yield return manager.Transitions.Play<RevelationIntro>();
            }

            // 2. Prepare Truth Text
            var finalUI = manager.TruthText;
            if (finalUI != null)
            {
                finalUI.text = data.Truth;
                finalUI.alpha = 0f;          // Ensure it starts invisible
                finalUI.gameObject.SetActive(true);

                // 3. Fade In Truth
                float duration = 2.0f;
                float timer = 0f;
                
                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    finalUI.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                    yield return null;
                }
                
                finalUI.alpha = 1f;
            }
            else
            {
                Debug.LogError("Truth Text reference missing in ConversationManager!");
            }

            // 4. Final Pause / Completion
            yield return new WaitForSeconds(3.0f);
            
            Debug.Log("Conversation Complete.");
        }
    }
}