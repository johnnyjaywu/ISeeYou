using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.UI
{
    public class ActivateIfTouchSupport : MonoBehaviour
    {
        private void Awake()
        {
            // Check if a Touchscreen device is currently connected and recognized
            if (Touchscreen.current != null)
            {
                Debug.Log("Touch input is SUPPORTED on this device.");
            }
            else
            {
                Debug.Log("No Touchscreen detected.");
                gameObject.SetActive(false);
            }
        }
    }
}