using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace ISeeYou
{
    /// <summary>
    /// Responsibility: Manages visual feedback for DropZones.
    /// Listens to DropZone logic events to trigger highlights and manage Empty/Filled states.
    /// Updated: Added visual states for Empty vs. Filled content.
    /// </summary>
    [RequireComponent(typeof(DropZone))]
    public class DropZoneFeedback : MonoBehaviour
    {
        [Header("State Colors")]
        [Tooltip("Color when the zone has 0 items.")]
        [SerializeField] private Color emptyColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        
        [Tooltip("Color when the zone has at least 1 item.")]
        [SerializeField] private Color filledColor = new Color(1f, 1f, 1f, 1f);

        [Header("Interaction Feedback")]
        [SerializeField] private Image targetImage;
        [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.5f, 0.5f);
        
        [Header("Animation Settings")]
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private Ease easeType = Ease.OutQuad;
        [Tooltip("Optional: Scales the zone up slightly when hovering.")]
        [SerializeField] private float hoverScale = 1.05f;

        private DropZone dropZone;
        private Vector3 originalScale;
        private bool isHovering;

        private void Awake()
        {
            dropZone = GetComponent<DropZone>();
            originalScale = transform.localScale;
            
            // Initialize color immediately based on start state
            if (targetImage != null)
            {
                targetImage.color = GetTargetStateColor();
            }
        }

        private void OnEnable()
        {
            dropZone.OnZoneEnter += HandleZoneEnter;
            dropZone.OnZoneExit += HandleZoneExit;
            dropZone.OnContentChanged += HandleContentChanged;
        }

        private void OnDisable()
        {
            dropZone.OnZoneEnter -= HandleZoneEnter;
            dropZone.OnZoneExit -= HandleZoneExit;
            dropZone.OnContentChanged -= HandleContentChanged;
        }

        // -------------------------------------------------------------------
        // Event Handlers
        // -------------------------------------------------------------------

        private void HandleZoneEnter(Draggable item)
        {
            isHovering = true;
            ApplyVisuals(highlightColor, originalScale * hoverScale);
        }

        private void HandleZoneExit(Draggable item)
        {
            isHovering = false;
            // Revert to the current state color (Empty or Filled)
            ApplyVisuals(GetTargetStateColor(), originalScale);
        }

        private void HandleContentChanged()
        {
            // Only update the visual state if we aren't currently hovering.
            // If we are hovering, the "Highlight" color should persist until exit.
            // Note: DropZone calls HandleExit internally on drop, so this naturally transitions correctly.
            if (!isHovering)
            {
                ApplyVisuals(GetTargetStateColor(), originalScale);
            }
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private void ApplyVisuals(Color targetColor, Vector3 targetScale)
        {
            if (targetImage != null)
            {
                // PrimeTween handles interruption automatically
                Tween.Color(targetImage, targetColor, duration, easeType);
            }

            if (hoverScale > 0.01f)
            {
                Tween.Scale(transform, targetScale, duration, easeType);
            }
        }

        private Color GetTargetStateColor()
        {
            // Simple check: Does the transform have children?
            // Note: This assumes only "Items" are children. If you have background images
            // as children, you might need a more specific check (e.g., count Draggable components).
            bool hasItems = transform.childCount > 0;
            return hasItems ? filledColor : emptyColor;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
        }
#endif
    }
}