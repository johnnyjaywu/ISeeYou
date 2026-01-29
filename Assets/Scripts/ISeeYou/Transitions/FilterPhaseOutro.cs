using System.Collections;
using ContentContent;
using UnityEngine;

namespace ISeeYou
{
    public class FilterPhaseOutro : TransitionBehaviour
    {
        [SerializeField] private ConversationManager conversationManager;
        
        public override IEnumerator StartTransition()
        {
            var words = conversationManager.WordsList;
            if (words == null) yield return null;
            foreach (var word in words)
            {
                if (word.IsKeyWord)
                    word.SetVisualState(Words.VisualState.Revealed);
            }
            yield return new WaitForSeconds(3);
            foreach (var word in words)
            {
                word.SetVisible(false);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}