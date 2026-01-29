using System;
using System.Collections;
using ContentContent;
using UnityEngine;

namespace ISeeYou
{
    public class TransitionController : MonoBehaviour
    {
        [Header("Transition Controllers")]
        [SerializeField] private TransitionBehaviour filterPhaseIntro;

        [SerializeField] private TransitionBehaviour filterPhaseOutro;
        [SerializeField] private TransitionBehaviour revealPhaseIntro;
        [SerializeField] private TransitionBehaviour revealPhaseOutro;

        private CoroutineHandle transitionRoutine;

        public void GoToNextPhase(GamePhase currentPhase, Action<GamePhase> onTransitionFinished = null)
        {
            if (transitionRoutine is { IsRunning: true })
                transitionRoutine.Stop();
            transitionRoutine = DoTransition(currentPhase, onTransitionFinished).Run();
        }

        private IEnumerator DoTransition(GamePhase currentPhase, Action<GamePhase> onTransitionFinished)
        {
            GamePhase nextPhase = GamePhase.Initial;
            switch (currentPhase)
            {
                case GamePhase.Initial:
                    nextPhase = GamePhase.Filtering;
                    yield return filterPhaseIntro.StartTransition();
                    break;
                case GamePhase.Filtering:
                    nextPhase = GamePhase.Revelation;
                    yield return filterPhaseOutro.StartTransition();
                    yield return revealPhaseIntro.StartTransition();
                    break;
                case GamePhase.Revelation:
                    nextPhase = GamePhase.End;
                    yield return revealPhaseOutro.StartTransition();
                    break;
            }

            onTransitionFinished?.Invoke(nextPhase);
        }
    }
}