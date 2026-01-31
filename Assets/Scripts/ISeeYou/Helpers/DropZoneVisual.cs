using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou
{
    /// <summary>
    /// Responsibility: Manages visual feedback for DropZones.
    /// Listens to DropZone logic events to trigger highlights or spawn custom ghost visuals.
    /// </summary>
    [RequireComponent(typeof(DropZone))]
    public class DropZoneVisual : MonoBehaviour
    {
        [Header("Zone Feedback")]
        [SerializeField] private Image highlightImage;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.2f);
        
        private DropZone dropZone;
        private Color originalColor;

        private void Awake()
        {
            dropZone = GetComponent<DropZone>();
            
            if (highlightImage != null)
            {
                originalColor = highlightImage.color;
            }
        }

        private void OnEnable()
        {
            dropZone.OnZoneEnter += HandleZoneEnter;
            dropZone.OnZoneExit += HandleZoneExit;
        }

        private void OnDisable()
        {
            dropZone.OnZoneEnter -= HandleZoneEnter;
            dropZone.OnZoneExit -= HandleZoneExit;
        }

        private void HandleZoneEnter(Draggable item)
        {
            // 1. Highlight Zone
            if (highlightImage != null)
            {
                highlightImage.color = highlightColor;
            }
        }

        private void HandleZoneExit(Draggable item)
        {
            // 1. Restore Zone Color
            if (highlightImage != null)
            {
                highlightImage.color = originalColor;
            }
        }
    }
}