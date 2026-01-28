using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ContentContent.UI
{
    /// <summary>
    ///     This component will emit an event when any Button is pressed.
    /// </summary>
    public class AnyButtonListener : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onPressedEvent;

        private IDisposable eventListener;

        private void OnEnable()
        {
            eventListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }

        private void OnDisable()
        {
            eventListener.Dispose();
        }

        private void OnAnyButtonPressed(InputControl button)
        {
            if (!button.IsPressed()) onPressedEvent?.Invoke();
            //Debug.Log($"Button {button.name} pressed!");
        }
    }
}