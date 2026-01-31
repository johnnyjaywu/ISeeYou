using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ISeeYou
{
    /// <summary>
    /// Responsibility: Handles direct user manipulation of the UI element.
    /// Allows the object to be dragged freely across the screen by temporarily 
    /// moving it to the root canvas hierarchy.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static Draggable Current { get; private set; }
        public Vector2 LastInputPosition { get; private set; }
        
        public event Action<PointerEventData> OnDragStarted;
        public event Action<PointerEventData> OnDragEnded;

        [Header("Interaction Settings")]
        [SerializeField] private float throwForceMultiplier = 1.0f; // Multiplier for the fling
        [SerializeField] private float velocitySmoothing = 15f;
        [SerializeField] private bool clampToScreen = true;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private UIPhysics physics;
        private Canvas rootCanvas;
        private RectTransform rootRect;
        private LayoutElement layoutElement;

        private Vector2 dragOffset;
        private Vector3 originalScale;
        private Vector2 lastPosition;
        private Vector2 smoothedVelocity;
        private bool wasLayoutIgnored;
        
        // Track dragging state to update velocity even when mouse is stationary
        private bool isDragging;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            physics = GetComponent<UIPhysics>();
            layoutElement = GetComponent<LayoutElement>();

            // Locate the top-most canvas to act as the drag plane
            Canvas foundCanvas = GetComponentInParent<Canvas>();
            if (foundCanvas != null)
            {
                rootCanvas = foundCanvas.rootCanvas;
                rootRect = rootCanvas.GetComponent<RectTransform>();
            }

            originalScale = transform.localScale;
        }

        private void Update()
        {
            // Calculate velocity every frame while dragging.
            // This ensures that if the user holds the mouse still, the velocity decays to zero.
            if (isDragging)
            {
                CalculateVelocity();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Current = this;
            LastInputPosition = eventData.position;
            isDragging = true;

            // Move the object to the Root Canvas so it is not restricted by 
            // any parent LayoutGroups (e.g., VerticalLayoutGroup, FlexLayoutGroup).
            if (rootCanvas != null)
            {
                // 1. Reparent to root (preserves world position automatically)
                transform.SetParent(rootCanvas.transform, true);
                
                // 2. Cache current World Position and Size before modifying Pivot/Anchors.
                // This is critical: changing the pivot/anchors usually shifts the object visually.
                // We cache where it *is* so we can put it back there after the change.
                Vector3 worldPos = rectTransform.position;
                Vector2 size = rectTransform.rect.size;

                // 3. Reset Anchors and Pivot to the center.
                // This simplifies the math when positioning the element relative to the 
                // center of the screen/mouse, ensuring dragging feels 1:1.
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                // 4. Restore World Position and Size.
                // This compensates for the pivot/anchor change, snapping the object back 
                // to where it visually was before we messed with the pivots.
                rectTransform.position = worldPos;
                rectTransform.sizeDelta = size;

                // Restore the original scale to prevent distortion if the previous 
                // parent (like a LayoutGroup) had forced a different scale factor.
                transform.localScale = originalScale;
            }

            // Ensure this object is completely ignored by any layout calculations 
            // while it is being dragged.
            if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
            wasLayoutIgnored = layoutElement.ignoreLayout;
            layoutElement.ignoreLayout = true;

            // Allow the pointer to pass through this object so it can detect 
            // DropZones underneath it.
            canvasGroup.blocksRaycasts = false;

            // Calculate the drag offset in the Root Canvas's coordinate space.
            // This ensures the object maintains its relative position to the mouse cursor,
            // regardless of the object's current scale or nested hierarchy.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootRect,
                eventData.position,
                rootCanvas.worldCamera,
                out Vector2 localMousePos
            );

            dragOffset = localMousePos - rectTransform.anchoredPosition;

            // Disable physics simulation during direct control (Kinematic mode)
            SetPhysicsState(isDynamic: false);
            smoothedVelocity = Vector2.zero;
            lastPosition = rectTransform.position;

            OnDragStarted?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rootCanvas == null) return;
            LastInputPosition = eventData.position;

            // Convert the current screen pointer position into the local coordinate 
            // space of the Root Canvas.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRect,
                    eventData.position,
                    rootCanvas.worldCamera,
                    out Vector2 localMousePos
                ))
            {
                // Apply the initial offset to keep the object "under" the mouse 
                // exactly where it was grabbed.
                Vector2 finalPos = localMousePos - dragOffset;

                if (clampToScreen) finalPos = ClampToScreen(finalPos);
                
                rectTransform.anchoredPosition = finalPos;
            }

            // Note: Velocity calculation removed from here and moved to Update()
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Current = null;
            LastInputPosition = eventData.position;
            canvasGroup.blocksRaycasts = true;
            isDragging = false;

            // Revert the layout ignore setting to its previous state.
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = wasLayoutIgnored;
            }

            // Restore physics simulation ONLY if we are still on the root canvas.
            // If we were dropped into a DropZone, the DropZone has already reparented us
            // and handled the physics state (likely setting it to static/docked).
            if (transform.parent == rootCanvas.transform)
            {
                SetPhysicsState(isDynamic: true);
                
                // FLING LOGIC: Apply the calculated momentum via the physics controller.
                // We multiply by throwForceMultiplier to allow tuning the "heft" of the throw.
                if (physics != null)
                {
                    physics.SetLinearVelocity(smoothedVelocity * throwForceMultiplier);
                }
            }

            OnDragEnded?.Invoke(eventData);
        }

        private void CalculateVelocity()
        {
            // Calculate velocity manually (pixels per second) to determine throw momentum.
            Vector2 currentPos = rectTransform.position;
            Vector2 rawVelocity = (currentPos - lastPosition) / Time.deltaTime;
            
            // Smooth the velocity to prevent erratic flinging if the mouse stops for 1 frame.
            smoothedVelocity = Vector2.Lerp(smoothedVelocity, rawVelocity, velocitySmoothing * Time.deltaTime);
            lastPosition = currentPos;
        }

        private void SetPhysicsState(bool isDynamic)
        {
            if (physics != null) physics.SetSimulationMode(isDynamic);
        }

        private Vector2 ClampToScreen(Vector2 proposedPos)
        {
            // Calculate the boundaries of the canvas and the object to keep it fully on screen.
            Vector2 canvasSize = rootRect.rect.size;
            Vector2 mySize = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;

            // Calculate min/max valid X and Y positions based on pivot offset.
            float minX = (mySize.x * pivot.x) - (canvasSize.x / 2);
            float maxX = (canvasSize.x / 2) - (mySize.x * (1 - pivot.x));
            float minY = (mySize.y * pivot.y) - (canvasSize.y / 2);
            float maxY = (canvasSize.y / 2) - (mySize.y * (1 - pivot.y));

            return new Vector2(Mathf.Clamp(proposedPos.x, minX, maxX), Mathf.Clamp(proposedPos.y, minY, maxY));
        }

        public RectTransform GetRectTransform() => rectTransform;
    }
}