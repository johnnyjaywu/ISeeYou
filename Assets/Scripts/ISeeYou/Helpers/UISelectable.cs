using System;
using UnityEngine;
using UnityEngine.EventSystems;
using NaughtyAttributes;

namespace ISeeYou
{
    /// <summary>
    /// Responsibility: Manages the Logical Selection State (Selected/Deselected).
    /// Enforces "Single Selection" logic within the system.
    /// </summary>
    public class UISelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
    {
        // Global Tracker for Single Selection Logic
        private static UISelectable currentlySelected;
        public static UISelectable Active => currentlySelected;

        // Events
        public event Action<bool> OnHoverChanged;      // true = enter, false = exit
        public event Action<bool> OnSelectionChanged;  // true = selected, false = deselected
        public event Action OnConfirm;                 // Fired if clicked while ALREADY selected
        public event Action OnClickStarted; // TODO: TEMP

        [Header("Selection Logic")]
        [SerializeField] private bool allowDeselection = true;
        [Tooltip("If true, clicking immediately selects.")]
        [SerializeField] private bool autoSelectOnClick = true;
        
        [ReadOnly] [SerializeField] private bool isHovered;
        [ReadOnly] [SerializeField] private bool isSelected;

        public bool IsSelected => isSelected;

        // -------------------------------------------------------------------
        // Input Handling
        // -------------------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            OnHoverChanged?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            OnHoverChanged?.Invoke(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnClickStarted?.Invoke(); // TODO: TEMP
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            
            // Ignore clicks if we just finished dragging this item
            if (eventData.dragging) return;

            if (autoSelectOnClick)
            {
                if (isSelected)
                {
                    if (allowDeselection)
                    {
                        Deselect();
                    }
                    else
                    {
                        OnConfirm?.Invoke();
                    }
                }
                else
                {
                    Select();
                }
            }
        }

        // -------------------------------------------------------------------
        // State Management
        // -------------------------------------------------------------------

        [Button("Select")]
        public void Select()
        {
            if (isSelected) return;

            // Deselect the previous active item
            if (currentlySelected != null && currentlySelected != this)
            {
                currentlySelected.Deselect();
            }

            // Set New State
            currentlySelected = this;
            isSelected = true;
            OnSelectionChanged?.Invoke(true);
        }

        [Button("Deselect")]
        public void Deselect()
        {
            if (!isSelected) return;

            if (currentlySelected == this)
            {
                currentlySelected = null;
            }

            isSelected = false;
            OnSelectionChanged?.Invoke(false);
        }

        /// <summary>
        /// Forcefully deselects the current active item.
        /// </summary>
        public static void DeselectAll()
        {
            if (currentlySelected != null)
            {
                currentlySelected.Deselect();
            }
        }

        private void OnDisable()
        {
            if (isSelected)
            {
                Deselect();
            }
            
            isHovered = false;
        }
    }
}