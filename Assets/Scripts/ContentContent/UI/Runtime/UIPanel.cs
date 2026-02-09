using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ContentContent.UI
{
    /// <summary>
    ///     This component represents a panel where a group of UI elements' interaction is expected. Navigating panels is
    ///     controlled by <see cref="UIPanelManager" />.
    ///     UI selection is handled where Unity's default behaviour is lacking. Add to the root object of a group of UI
    ///     elements.
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [Tooltip("The first selected element for this menu, default to first item in the selectable list")]
        [SerializeField] protected Selectable firstSelected;

        private InputSystemUIInputModule inputModule;
        private Selectable lastSelected;
        private List<Selectable> selectables = new();

        #region Mono

        private void Awake()
        {
            // Get all selectables in children
            selectables = GetComponentsInChildren<Selectable>(true).ToList();
            foreach (Selectable selectable in selectables) AddSelectionListeners(selectable);

            firstSelected = selectables.Count > 0 ? selectables[0] : null;
            lastSelected = firstSelected;
            SelectDefault();

            // Get input module
            inputModule = FindFirstObjectByType<InputSystemUIInputModule>();
            if (!inputModule)
                Debug.LogWarning(
                    $"Missing object with component of type {typeof(InputSystemUIInputModule)}. Are you missing the {typeof(EventSystem)} object?");
        }

        private void OnEnable()
        {
            Enable();
        }

        private void OnDisable()
        {
            Disable();
        }

        #endregion Mono

        #region Public

        public void Enable()
        {
            inputModule.move.action.performed += OnMoveInputPerformed;
            SelectPrevious();
        }

        public void Disable()
        {
            inputModule.move.action.performed -= OnMoveInputPerformed;
        }

        public void Select(Selectable selectable)
        {
            SetSelectedDelayed(selectable);
        }

        public void SelectDefault()
        {
            if (!firstSelected)
                return;

            SetSelectedDelayed(firstSelected);
        }

        public void SelectPrevious()
        {
            if (!lastSelected)
                SelectDefault();
            else
                SetSelectedDelayed(lastSelected);
        }

        #endregion Public

        #region Helpers

        private void SetSelectedDelayed(Selectable selected)
        {
            StartCoroutine(SelectAfterDelay(selected.gameObject));
        }

        private IEnumerator SelectAfterDelay(GameObject targetSelectable)
        {
            yield return null;
            if (targetSelectable)
                EventSystem.current.SetSelectedGameObject(targetSelectable);
        }

        private void AddSelectionListeners(Selectable selectable)
        {
            // add listener
            var trigger = selectable.EnsureComponent<EventTrigger>();

            // add SELECT event
            var selectEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Select
            };
            selectEntry.callback.AddListener(OnSelect);
            trigger.triggers.Add(selectEntry);

            // add DESELECT event
            var deselectEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Deselect
            };
            deselectEntry.callback.AddListener(OnDeselect);
            trigger.triggers.Add(deselectEntry);

            // add POINTER ENTER event
            var pointerEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            pointerEnter.callback.AddListener(OnPointerEnter);
            trigger.triggers.Add(pointerEnter);

            // add POINTER EXIT event
            var pointerExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            pointerExit.callback.AddListener(OnPointerExit);
            trigger.triggers.Add(pointerExit);
        }

        #endregion Helpers

        #region Events

        private void OnSelect(BaseEventData eventData)
        {
            lastSelected = eventData.selectedObject.GetComponent<Selectable>();
        }

        private void OnDeselect(BaseEventData eventData)
        {
        }

        private void OnPointerEnter(BaseEventData eventData)
        {
            if (eventData is not PointerEventData pointerEventData) return;
            var selectable = pointerEventData.pointerEnter.GetComponentInParent<Selectable>();
            if (!selectable) selectable = pointerEventData.pointerEnter.GetComponentInChildren<Selectable>();

            // If there was already a selected object, deselect it first
            if (EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);

            // Select the new object
            pointerEventData.selectedObject = selectable.gameObject;
        }

        private void OnPointerExit(BaseEventData eventData)
        {
            if (eventData is PointerEventData pointerEventData) pointerEventData.selectedObject = null;
        }

        private void OnMoveInputPerformed(InputAction.CallbackContext context)
        {
            if (!EventSystem.current.currentSelectedGameObject && lastSelected)
                EventSystem.current.SetSelectedGameObject(lastSelected.gameObject);
        }

        #endregion Events
    }
}