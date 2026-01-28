using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.UI
{
    public class PauseOnEnable : MonoBehaviour
    {
        [SerializeField] private UnityEvent onPause;
        [SerializeField] private UnityEvent onUnpause;

        private bool isPaused;

        private void OnEnable()
        {
            Pause();
        }

        private void OnDisable()
        {
            Unpause();
        }

        public void TogglePause()
        {
            if (isPaused)
                Unpause();
            else
                Pause();
        }

        public void Pause()
        {
            Time.timeScale = 0;
            isPaused = true;
            onPause?.Invoke();
        }

        public void Unpause()
        {
            Time.timeScale = 1;
            isPaused = false;
            onUnpause?.Invoke();
        }
    }
}