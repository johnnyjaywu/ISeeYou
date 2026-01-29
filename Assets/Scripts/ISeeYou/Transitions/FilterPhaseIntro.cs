using System.Collections;
using ContentContent;
using UnityEngine;

namespace ISeeYou
{
    public class FilterPhaseIntro : TransitionBehaviour
    {
        [SerializeField] private ConversationManager conversationManager;

        public override IEnumerator StartTransition()
        {
            conversationManager.SpawnWordsForPhase(GamePhase.Filtering);
            var words = conversationManager.WordsList;
            if (words == null) yield return null;
            foreach (var word in words)
            {
                word.SetVisible(true);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}