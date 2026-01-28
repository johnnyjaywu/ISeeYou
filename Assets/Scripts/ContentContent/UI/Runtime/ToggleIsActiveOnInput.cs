using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.UI
{
    /// <summary>
    /// Toggles the active state of Target GameObject on input performed
    /// </summary>
    public class ToggleIsActiveOnInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference inputAction;
        [SerializeField] private GameObject target;

        private void Awake()
        {
            inputAction.action.Enable();
            inputAction.action.performed += OnInputActionPerformed;
        }

        private void OnDestroy()
        {
            inputAction.action.Disable();
            inputAction.action.performed -= OnInputActionPerformed;
        }

        private void OnInputActionPerformed(InputAction.CallbackContext context)
        {
            if (context.performed) target.SetActive(!target.activeSelf);
        }
    }
}