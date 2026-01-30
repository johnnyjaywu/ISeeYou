using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ISeeYou
{
    /// <summary>
    /// Handles physical movement of UI elements.
    /// Integrates with DropZone for layout locking and UIInputFeedback for animation control.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static Draggable Current { get; private set; }

        public event Action<PointerEventData> OnDragStarted;
        public event Action<PointerEventData> OnDragEnded;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private RectTransform rootRect;
        
        // References
        private UIInputFeedback inputFeedback;

        // State
        private Vector2 dragOffset;
        private DropZone sourceZone;
        private int sourceSiblingIndex;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            inputFeedback = GetComponent<UIInputFeedback>(); // Optional dependency
            
            var foundCanvas = GetComponentInParent<Canvas>();
            if (foundCanvas != null) 
            {
                rootCanvas = foundCanvas.rootCanvas;
                rootRect = rootCanvas.GetComponent<RectTransform>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Current = this;

            // 1. Disable Hover/Click feedback while dragging
            if (inputFeedback != null) inputFeedback.SetInteractable(false);

            // 2. Identify Source
            sourceZone = GetComponentInParent<DropZone>();
            sourceSiblingIndex = transform.GetSiblingIndex();

            if (sourceZone != null)
            {
                sourceZone.NotifyItemRemoved();
            }

            // 3. Calculate Offset
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, 
                eventData.position, 
                rootCanvas.worldCamera, 
                out dragOffset
            );

            // 4. Lift to Root Canvas
            if (rootCanvas != null)
            {
                Vector3 worldPos = rectTransform.position;
                transform.SetParent(rootCanvas.transform, true);
                
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                
                rectTransform.position = worldPos;
            }

            canvasGroup.blocksRaycasts = false;
            OnDragStarted?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rootCanvas == null) return;

            Vector2 localCursor;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect, 
                eventData.position, 
                rootCanvas.worldCamera, 
                out localCursor
            ))
            {
                Vector2 finalPos = localCursor - (Vector2)(rootRect.rotation * dragOffset);
                finalPos = KeepWithinScreen(finalPos);
                rectTransform.anchoredPosition = finalPos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Current = null;
            canvasGroup.blocksRaycasts = true;

            // 1. Re-enable Hover/Click feedback
            if (inputFeedback != null) inputFeedback.SetInteractable(true);

            // 2. Validation Logic
            if (transform.parent == rootCanvas.transform)
            {
                if (sourceZone != null && sourceZone.IsLocked)
                {
                    sourceZone.ReturnItem(this, sourceSiblingIndex);
                }
                else
                {
                    var physics = GetComponent<UIPhysicsObject>();
                    if (physics != null)
                    {
                        physics.SetLayoutState(false);
                    }
                }
            }

            OnDragEnded?.Invoke(eventData);
        }
        
        public RectTransform GetRectTransform() => rectTransform;

        private Vector2 KeepWithinScreen(Vector2 proposedPos)
        {
            Vector2 canvasSize = rootRect.rect.size;
            Vector2 mySize = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;

            float minX = (mySize.x * pivot.x) - (canvasSize.x / 2);
            float maxX = (canvasSize.x / 2) - (mySize.x * (1 - pivot.x));
            
            float minY = (mySize.y * pivot.y) - (canvasSize.y / 2);
            float maxY = (canvasSize.y / 2) - (mySize.y * (1 - pivot.y));

            return new Vector2(
                Mathf.Clamp(proposedPos.x, minX, maxX),
                Mathf.Clamp(proposedPos.y, minY, maxY)
            );
        }
    }
}