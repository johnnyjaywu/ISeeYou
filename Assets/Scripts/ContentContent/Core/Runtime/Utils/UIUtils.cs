using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ContentContent
{
    public static class UIUtils
    {
        // Cache the list to avoid GC allocation per frame
        private static readonly List<RaycastResult> raycastResults = new();

        /// <summary>
        ///     Checks if the current pointer (Mouse, Touch, or Pen) is over any UI object.
        ///     Safe to call from Input System callbacks (performed/started).
        /// </summary>
        public static bool IsPointerOverUI()
        {
            // 1. Get the current pointer (works for Mouse AND Touch Simulator)
            // Pointer.current automatically switches to the active device
            if (Pointer.current == null) return false;

            // 2. Get the screen position directly from the device
            Vector2 position = Pointer.current.position.ReadValue();

            // 3. Perform the check
            return IsScreenPosOverUI(position);
        }

        /// <summary>
        ///     Checks a specific screen coordinate (useful if you have the value from callback context)
        /// </summary>
        private static bool IsScreenPosOverUI(Vector2 screenPos)
        {
            // If the EventSystem doesn't exist, we can't check UI
            if (EventSystem.current == null) return false;

            // Create a pointer event at the screen position
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            // Raycast against the UI
            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            // If we hit any UI element that is a Raycast Target, return true
            return raycastResults.Count > 0;
        }
    }
}