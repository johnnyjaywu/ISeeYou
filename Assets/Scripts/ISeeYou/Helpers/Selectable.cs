using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ISeeYou
{
    /// <summary>
    /// Attach this to any object that needs Hover/Click feedback.
    /// </summary>
    public class Selectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public event Action OnHoverEnter;
        public event Action OnHoverExit;
        public event Action OnClick;

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }
    }
}