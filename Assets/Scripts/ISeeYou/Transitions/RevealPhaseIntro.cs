using ContentContent;
using System.Collections;
using UnityEngine;

namespace ISeeYou
{
    public class RevealPhaseIntro : TransitionBehaviour
    {
        [SerializeField] private ConversationManager conversationManager;

        public override IEnumerator StartTransition()
        {
            conversationManager.SpawnWordsForPhase(GamePhase.Revelation);
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