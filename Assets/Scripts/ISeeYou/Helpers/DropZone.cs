using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ISeeYou
{
    /// <summary>
    /// A UI container that accepts Draggable objects. 
    /// Manages visual feedback (ghosts) and sorting order.
    /// Now acts as the Authority for whether items can leave the layout via 'IsLocked'.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class DropZone : MonoBehaviour, IDropHandler
    {
        [Header("Feedback Settings")]
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [Range(0f, 1f)]
        [SerializeField] private float minOverlapPercent = 0.20f; 
        [SerializeField] private float overlapHysteresis = 0.5f; 

        // NEW: Controls whether items are allowed to be dragged out of this zone.
        // If True, items dropped outside will snap back via ReturnItem().
        public bool IsLocked { get; set; }

        // Event fired whenever an item is added to or removed from this zone.
        public event Action OnContentChanged; 

        private Color originalColor;
        private Image background;
        private GameObject layoutGhost;
        private RectTransform myRect;
        
        private readonly Vector3[] myCorners = new Vector3[4];
        private readonly Vector3[] otherCorners = new Vector3[4];

        private void Awake()
        {
            background = GetComponent<Image>();
            myRect = GetComponent<RectTransform>();
            originalColor = background.color;
        }

        private void Update()
        {
            // If nothing is being dragged, ensure we clean up any lingering visual feedback
            if (Draggable.Current == null) 
            {
                if (layoutGhost != null) ResetFeedback();
                return;
            }

            // Calculate overlap threshold with hysteresis to prevent flickering
            float threshold = minOverlapPercent;
            if (layoutGhost != null) threshold *= overlapHysteresis;

            if (CheckOverlap(Draggable.Current.GetRectTransform(), threshold))
            {
                // Create Ghost if it doesn't exist yet
                if (layoutGhost == null)
                {
                    // Don't show ghost if we are already inside this zone (prevent self-reaction)
                    if (Draggable.Current.transform.parent == transform) return;

                    var words = Draggable.Current.GetComponent<Words>();
                    if (words != null)
                    {
                        background.color = hoverColor;
                        CreateGhost(words);
                    }
                }

                // Update Ghost Position within the layout
                if (layoutGhost != null)
                {
                    int newIndex = GetInsertionIndex(Draggable.Current.transform.position);
                    
                    if (layoutGhost.transform.GetSiblingIndex() != newIndex)
                    {
                        layoutGhost.transform.SetSiblingIndex(newIndex);
                        LayoutRebuilder.MarkLayoutForRebuild(myRect);
                    }
                }
            }
            else
            {
                if (layoutGhost != null) ResetFeedback();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            int targetIndex = -1;
            if (layoutGhost != null) targetIndex = layoutGhost.transform.GetSiblingIndex();
            
            ResetFeedback(); 

            if (eventData.pointerDrag != null)
            {
                var draggable = eventData.pointerDrag.GetComponent<Draggable>();
                if (draggable != null)
                {
                    AcceptItem(draggable, targetIndex);
                }
            }
        }

        /// <summary>
        /// Shared logic to accept a draggable item into this zone.
        /// Handles reparenting, indexing, physics state, and notification.
        /// </summary>
        private void AcceptItem(Draggable draggable, int index)
        {
            draggable.transform.SetParent(transform);
            draggable.transform.localScale = Vector3.one;

            if (index != -1) draggable.transform.SetSiblingIndex(index);

            // Lock physics so it acts as a UI element
            var physics = draggable.GetComponent<UIPhysicsObject>();
            if (physics != null) physics.SetLayoutState(true);

            OnContentChanged?.Invoke(); 
        }

        /// <summary>
        /// Called by Draggable if this zone is Locked and the user tried to drop the item in empty space.
        /// Snaps the item back to its original position within this zone.
        /// </summary>
        public void ReturnItem(Draggable item, int originalIndex)
        {
            AcceptItem(item, originalIndex);
        }

        /// <summary>
        /// Called explicitly by Draggable.OnBeginDrag when an item is picked up.
        /// </summary>
        public void NotifyItemRemoved()
        {
            OnContentChanged?.Invoke();
        }

        private int GetInsertionIndex(Vector3 checkPos)
        {
            int childCount = transform.childCount;
            float closestDistance = float.MaxValue;
            
            int bestVisualIndex = 0; 
            int visibleChildCount = 0;
            
            RectTransform lastVisibleChild = null;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                
                if (child.gameObject == layoutGhost) continue;
                if (!child.gameObject.activeSelf) continue;

                visibleChildCount++;
                lastVisibleChild = child as RectTransform;

                float dist = Vector3.Distance(checkPos, child.position);
                
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    if (checkPos.x < child.position.x) bestVisualIndex = visibleChildCount - 1; 
                    else bestVisualIndex = visibleChildCount;
                }
            }

            // Jitter prevention logic
            if (visibleChildCount > 0)
            {
                Transform firstChild = null;
                for(int i=0; i<childCount; i++) 
                {
                    var c = transform.GetChild(i);
                    if(c.gameObject != layoutGhost && c.gameObject.activeSelf) { firstChild = c; break; }
                }

                if (firstChild != null && checkPos.x < firstChild.position.x && checkPos.y > firstChild.position.y - 50)
                {
                    return 0; 
                }
            }

            if (lastVisibleChild != null)
            {
                float halfHeight = lastVisibleChild.rect.height * 0.5f;
                bool isBelow = checkPos.y < lastVisibleChild.position.y - halfHeight;
                bool isSameLine = Mathf.Abs(checkPos.y - lastVisibleChild.position.y) < halfHeight;
                bool isRight = checkPos.x > lastVisibleChild.position.x;

                if (isBelow || (isSameLine && isRight))
                {
                    return childCount; 
                }
            }

            return Mathf.Clamp(bestVisualIndex, 0, childCount);
        }

        private bool CheckOverlap(RectTransform otherRect, float requiredPercent)
        {
            myRect.GetWorldCorners(myCorners);
            otherRect.GetWorldCorners(otherCorners);

            Rect rect1 = GetScreenRect(myCorners);
            Rect rect2 = GetScreenRect(otherCorners);

            if (!rect1.Overlaps(rect2)) return false;

            Rect intersection = GetIntersection(rect1, rect2);
            float intersectionArea = intersection.width * intersection.height;
            float draggedArea = rect2.width * rect2.height;

            return (intersectionArea / draggedArea) >= requiredPercent;
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

            if (xMax >= xMin && yMax >= yMin)
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            
            return Rect.zero;
        }

        private void CreateGhost(Words sourceWord)
        {
            if (layoutGhost != null) return;

            layoutGhost = new GameObject("LayoutGhost");

            var le = layoutGhost.AddComponent<LayoutElement>();
            le.preferredWidth = sourceWord.preferredWidth;
            le.preferredHeight = sourceWord.preferredHeight;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;

            layoutGhost.transform.SetParent(transform, false);
            LayoutRebuilder.MarkLayoutForRebuild(myRect);
        }

        private void ResetFeedback()
        {
            background.color = originalColor;
            if (layoutGhost != null)
            {
                Destroy(layoutGhost);
                layoutGhost = null;
                if (gameObject.activeInHierarchy)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(myRect);
                }
            }
        }
    }
}