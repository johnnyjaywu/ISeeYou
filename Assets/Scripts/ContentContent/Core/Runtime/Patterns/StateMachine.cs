using System;

namespace ContentContent
{
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }

    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        
        /// <summary>
        /// Fired whenever the state transitions. 
        /// Carries the new IState generic reference.
        /// </summary>
        public event Action<IState> StateChanged;
        
        public void ChangeState(IState newState)
        {
            if (CurrentState == newState) return;

            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Enter();
            
            // Invoke event AFTER entry logic completes
            StateChanged?.Invoke(CurrentState);
        }

        public void Update()
        {
            CurrentState?.Update();
        }
    }
}