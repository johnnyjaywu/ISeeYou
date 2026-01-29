using System;
using System.Collections;
using UnityEngine;

namespace ISeeYou
{
    public class RevealPhaseOutro : TransitionBehaviour
    {
        [SerializeField] private ConversationManager conversationManager;

        public override IEnumerator StartTransition()
        {
            var words = conversationManager.WordsList;
            if (words == null) yield return null;
            foreach (var word in words)
            {
                word.RevealTruth();
            }
            conversationManager.RebuildLayout();
            yield return new WaitForSeconds(3);
        }
    }
}