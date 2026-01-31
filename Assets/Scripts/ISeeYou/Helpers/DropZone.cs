using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ISeeYou
{
    /// <summary>
    /// Responsibility: A simplified receiver for Draggable objects.
    /// Acts as a container that accepts valid drops and reparents them.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DropZone : MonoBehaviour, IDropHandler
    {
        [Header("Logic Settings")]
        [SerializeField] private bool isLocked = false;
        
        [Tooltip("How much of the item must overlap this zone to be accepted.")]
        [Range(0f, 1f)]
        [SerializeField] private float minOverlapPercent = 0.20f;

        [Header("Physics Settings")]
        [SerializeField] private bool disablePhysicsOnDrop = true;

        // Events for external feedback (e.g., highlighting the box when hovering)
        public event Action<Draggable> OnZoneEnter;
        public event Action<Draggable> OnZoneExit;
        public event Action OnContentChanged;

        public bool IsLocked => isLocked;
        public RectTransform RectTransform => myRect;

        private RectTransform myRect;
        private Canvas rootCanvas;
        private bool isHovering = false;

        // Cached arrays for overlap calculation to avoid GC allocations
        private readonly Vector3[] cornersSelf = new Vector3[4];
        private readonly Vector3[] cornersOther = new Vector3[4];

        private void Awake()
        {
            myRect = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) rootCanvas = canvas.rootCanvas;
        }

        private void Start()
        {
            // Auto-validate any items pre-placed in the Editor
            ValidateImmediateChildren();
        }

        // We use Update only to track Entrance/Exit events for feedback.
        // If you don't need visual highlights (changing color on hover), you can remove this.
        private void Update()
        {
            if (Draggable.Current == null)
            {
                if (isHovering) HandleExit(null);
                return;
            }

            bool isPointerInside = false;
            
            // Check if the mouse/finger is physically inside this rect
            if (rootCanvas != null)
            {
                Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
                isPointerInside = RectTransformUtility.RectangleContainsScreenPoint(myRect, Draggable.Current.LastInputPosition, cam);
            }

            // Verify if the Draggable actually overlaps the zone sufficiently
            bool overlaps = isPointerInside && CheckOverlap(Draggable.Current.GetRectTransform(), minOverlapPercent);

            if (overlaps)
            {
                if (!isHovering) HandleEnter(Draggable.Current);
            }
            else if (isHovering)
            {
                HandleExit(Draggable.Current);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Reset hover state immediately
            if (isHovering) HandleExit(Draggable.Current);
            
            if (isLocked || eventData.pointerDrag == null) return;

            Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();
            
            // Only accept the drop if we are the valid target
            if (draggable != null && CheckOverlap(draggable.GetRectTransform(), minOverlapPercent))
            {
                AcceptItem(draggable);
            }
        }

        /// <summary>
        /// Checks all immediate children. If they are Draggable, enforces the DropZone rules (Physics, Transform).
        /// Useful for initializing items pre-placed in the editor.
        /// </summary>
        public void ValidateImmediateChildren()
        {
            bool contentChanged = false;
            
            // Iterate over all children to ensure they conform to DropZone rules
            foreach (Transform child in transform)
            {
                Draggable draggable = child.GetComponent<Draggable>();
                if (draggable != null)
                {
                    ApplyDropLogic(draggable);
                    contentChanged = true;
                }
            }

            if (contentChanged)
            {
                OnContentChanged?.Invoke();
            }
        }

        private void AcceptItem(Draggable item)
        {
            // Adoption: Move the item from the Root Canvas into this container.
            item.transform.SetParent(transform);
            
            // Apply rules
            ApplyDropLogic(item);

            OnContentChanged?.Invoke();
        }

        private void ApplyDropLogic(Draggable item)
        {
            // Reset local transformation to ensure it snaps into the layout correctly.
            item.transform.localScale = Vector3.one;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localPosition = Vector3.zero;

            // Handle Physics state for the docked item
            UIPhysics physics = item.GetComponent<UIPhysics>();
            if (physics != null)
            {
                physics.SetSimulationMode(!disablePhysicsOnDrop);
            }
        }

        private void HandleEnter(Draggable item)
        {
            isHovering = true;
            OnZoneEnter?.Invoke(item);
        }

        private void HandleExit(Draggable item)
        {
            isHovering = false;
            OnZoneExit?.Invoke(item);
        }
        
        private bool CheckOverlap(RectTransform otherRect, float requiredPercent)
        {
            if (otherRect == null) return false;
            
            // Get world corners to calculate screen-space intersection
            myRect.GetWorldCorners(cornersSelf);
            otherRect.GetWorldCorners(cornersOther);

            Rect rect1 = GetScreenRect(cornersSelf);
            Rect rect2 = GetScreenRect(cornersOther);

            if (!rect1.Overlaps(rect2)) return false;

            Rect intersection = GetIntersection(rect1, rect2);
            float overlapArea = intersection.width * intersection.height;
            float objectArea = rect2.width * rect2.height;

            return (overlapArea / objectArea) >= requiredPercent;
        }

        private Rect GetScreenRect(Vector3[] corners)
        {
            Vector3 min = corners[0];
            Vector3 max = corners[2];
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private Rect GetIntersection(Rect r1, Rect r2)
        {
            float xMin = Mathf.Max(r1.x, r2.x);
            float xMax = Mathf.Min(r1.x + r1.width, r2.x + r2.width);
            float yMin = Mathf.Max(r1.y, r2.y);
            float yMax = Mathf.Min(r1.y + r1.height, r2.y + r2.height);
            return (xMax >= xMin && yMax >= yMin) ? new Rect(xMin, yMin, xMax - xMin, yMax - yMin) : Rect.zero;
        }
    }
}