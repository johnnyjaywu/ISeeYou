using UnityEngine;
using UnityEngine.EventSystems;

namespace ISeeYou
{
    /// <summary>
    /// Bridges Unity Event Systems (PointerEnter, Exit, Down) to the UGUIAnimator.
    /// Attach this to any object that needs Hover/Click feedback.
    /// </summary>
    [RequireComponent(typeof(UGUIAnimator))]
    public class UIInputFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private UGUIAnimator animator;
        private bool isInteractable = true;

        private void Awake()
        {
            animator = GetComponent<UGUIAnimator>();
        }

        /// <summary>
        /// Call this to mute feedback (e.g., during Dragging or cinematic sequences).
        /// Automatically resets the Hover state if disabled.
        /// </summary>
        public void SetInteractable(bool state)
        {
            isInteractable = state;
            
            // If we are disabling interaction, force the "Un-Hover" state immediately
            // so the object doesn't get stuck looking hovered.
            if (!state) 
            {
                animator.PlayHover(false); 
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            animator.PlayHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) return;
            animator.PlayHover(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable) return;
            animator.PlayClick();
        }
    }
}