using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ISeeYou
{
    /// <summary>
    /// Manages a designated area where Draggable items can be dropped.
    /// Features:
    /// - Capacity Limits: Can restrict how many items it holds.
    /// - Locking: Can prevent items from being dragged out.
    /// - Auto-Tracking: Automatically updates its list of contents when hierarchy changes.
    /// - Physics Control: Can disable physics simulation on dropped items for UI stability.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DropZone : MonoBehaviour, IDropHandler
    {
        // -------------------------------------------------------------------------
        // 1. CONFIGURATION
        // -------------------------------------------------------------------------

        [Header("Logic Settings")]
        [Tooltip("If true, items inside cannot be dragged out, and new items cannot be dropped in.")]
        [SerializeField] private bool isLocked = false;
        
        [Tooltip("Maximum number of items allowed. Set to -1 for unlimited.")]
        [SerializeField] private int maxCapacity = -1; 
        
        [Tooltip("How much of the item's rect must overlap this zone to be accepted (0.0 to 1.0).")]
        [Range(0f, 1f)]
        [SerializeField] private float minOverlapPercent = 0.20f;

        [Header("Physics Settings")]
        [Tooltip("Should the UIPhysics component on the item be disabled when docked?")]
        [SerializeField] private bool disablePhysicsOnDrop = true;

        // -------------------------------------------------------------------------
        // 2. EVENTS & PROPERTIES
        // -------------------------------------------------------------------------

        // Feedback events for external UI (e.g., highlighting borders)
        public event Action<Draggable> OnZoneEnter;
        public event Action<Draggable> OnZoneExit;
        
        // Fired whenever the number of items inside changes
        public event Action<int> OnContentCountChanged;

        public bool IsLocked => isLocked;
        public bool IsFull => maxCapacity >= 0 && dockedItems.Count >= maxCapacity;
        public RectTransform RectTransform => myRect;
        
        // Public read-only access to the items currently in this zone
        public IReadOnlyList<Draggable> DockedItems => dockedItems;

        // -------------------------------------------------------------------------
        // 3. INTERNAL STATE
        // -------------------------------------------------------------------------

        private RectTransform myRect;
        private Canvas rootCanvas;
        private bool isHovering = false;

        // The authoritative list of what is currently inside this zone
        private List<Draggable> dockedItems = new List<Draggable>();

        // Pre-allocated arrays for intersection math to avoid GC allocations
        private readonly Vector3[] cornersSelf = new Vector3[4];
        private readonly Vector3[] cornersOther = new Vector3[4];

        // -------------------------------------------------------------------------
        // 4. LIFECYCLE
        // -------------------------------------------------------------------------

        private void Awake()
        {
            myRect = GetComponent<RectTransform>();
            
            // Cache the root canvas for screen-space calculations
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) rootCanvas = canvas.rootCanvas;
        }

        private void Start()
        {
            // Initialize list based on pre-placed children in the Editor
            RefreshDockedItems();
        }

        /// <summary>
        /// Unity Callback: Triggered whenever a child is parented, unparented, or destroyed.
        /// We use this to keep our internal list perfectly in sync with the hierarchy.
        /// </summary>
        private void OnTransformChildrenChanged()
        {
            RefreshDockedItems();
        }

        private void Update()
        {
            // If nothing is being dragged, we just handle cleanup of hover states
            if (Draggable.Current == null)
            {
                if (isHovering) HandleExit(null);
                return;
            }

            // 1. Capacity Check: If full, ignore hover logic entirely.
            //    This gives the user immediate visual feedback (no highlight) that they can't drop here.
            if (IsFull)
            {
                if (isHovering) HandleExit(Draggable.Current);
                return;
            }

            // 2. Overlap Check
            bool isPointerInside = false;
            if (rootCanvas != null)
            {
                // Check if mouse/finger is physically inside our rect
                Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
                isPointerInside = RectTransformUtility.RectangleContainsScreenPoint(myRect, Draggable.Current.LastInputPosition, cam);
            }

            // Verify if the Draggable rect actually overlaps us sufficiently
            bool overlaps = isPointerInside && CheckOverlap(Draggable.Current.GetRectTransform(), minOverlapPercent);

            // 3. Fire Events
            if (overlaps)
            {
                if (!isHovering) HandleEnter(Draggable.Current);
            }
            else if (isHovering)
            {
                HandleExit(Draggable.Current);
            }
        }

        // -------------------------------------------------------------------------
        // 5. DROP HANDLERS (IDropHandler)
        // -------------------------------------------------------------------------

        public void OnDrop(PointerEventData eventData)
        {
            // Always exit hover state on drop attempt
            if (isHovering) HandleExit(Draggable.Current);
            
            // REJECTION CRITERIA:
            // 1. Zone is Locked
            // 2. Zone is Full
            // 3. Data is invalid
            if (isLocked || IsFull || eventData.pointerDrag == null) return;

            Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();
            
            // Only accept if overlap logic passes
            if (draggable != null && CheckOverlap(draggable.GetRectTransform(), minOverlapPercent))
            {
                AcceptItem(draggable);
            }
        }

        private void AcceptItem(Draggable item)
        {
            // Reparenting triggers OnTransformChildrenChanged -> RefreshDockedItems
            item.transform.SetParent(transform);
            
            // Apply layout and physics rules
            ApplyDropLogic(item);
        }

        // -------------------------------------------------------------------------
        // 6. CONTROL LOGIC
        // -------------------------------------------------------------------------

        /// <summary>
        /// Scans immediate children to rebuild the dockedItems list.
        /// Also enforces the current Lock state on all found items.
        /// </summary>
        private void RefreshDockedItems()
        {
            dockedItems.Clear();

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out Draggable item))
                {
                    dockedItems.Add(item);
                    
                    // Enforce lock state: If we are locked, disable the draggable script
                    // so the user cannot drag the item out.
                    item.enabled = !isLocked;
                }
            }
            
            OnContentCountChanged?.Invoke(dockedItems.Count);
        }

        /// <summary>
        /// Locks or unlocks the zone.
        /// Locked = No new items in, no existing items out.
        /// </summary>
        public void SetLock(bool locked)
        {
            if (isLocked == locked) return;
            isLocked = locked;
            
            // Apply new state to all currently docked items
            foreach (var item in dockedItems)
            {
                if (item != null) item.enabled = !isLocked;
            }
        }

        private void ApplyDropLogic(Draggable item)
        {
            // Reset transforms to snap into the zone cleanly
            item.transform.localScale = Vector3.one;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localPosition = Vector3.zero;

            // Handle Physics state
            UIPhysics physics = item.GetComponent<UIPhysics>();
            if (physics != null)
            {
                physics.SetSimulationMode(!disablePhysicsOnDrop);
            }
        }

        // -------------------------------------------------------------------------
        // 7. MATH & UTILS
        // -------------------------------------------------------------------------

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
        
        /// <summary>
        /// Calculates if rectA overlaps this DropZone by at least the required percentage.
        /// </summary>
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

            // Prevent divide by zero
            if (objectArea <= 0) return false;

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
            
            return (xMax >= xMin && yMax >= yMin) 
                ? new Rect(xMin, yMin, xMax - xMin, yMax - yMin) 
                : Rect.zero;
        }
    }
}