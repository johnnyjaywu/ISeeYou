using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ISeeYou
{
    [RequireComponent(typeof(LayoutElement))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class Words : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler,
        IPointerExitHandler
    {
        public string Text { get; private set; }
        public bool IsKeyWord { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsInZone { get; private set; }

        public event Action OnStateChanged;

        private TextMeshProUGUI textComponent;
        private LayoutElement layoutElement;
        private RectTransform rectTransform;
        private ContentSizeFitter sizeFitter;

        private bool isDraggable;
        private RectTransform focusZone;
        private string truthText;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            layoutElement = GetComponent<LayoutElement>();
            textComponent = GetComponent<TextMeshProUGUI>();
            sizeFitter = GetComponent<ContentSizeFitter>();
        }

        public void Initialize(string text, bool isKey, RectTransform zone, bool draggable, string truth = "")
        {
            if (rectTransform == null) Awake();

            Text = text;
            IsKeyWord = isKey;
            focusZone = zone;
            isDraggable = draggable;
            truthText = truth;
            IsInZone = true;

            textComponent.text = Text;
            ResetVisuals();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            rectTransform.anchoredPosition += eventData.delta / transform.lossyScale.x;
            UpdateFocusZoneStatus(eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            UpdateFocusZoneStatus(eventData.pressEventCamera);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tween.Scale(transform, transform.localScale, Vector3.one * 1.1f, 0.1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tween.Scale(transform, transform.localScale, Vector3.one, 0.1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDraggable) return;
            IsSelected = !IsSelected;
            SetVisualState(IsSelected ? VisualState.Selected : VisualState.Default);
            OnStateChanged?.Invoke();
        }

        private void UpdateFocusZoneStatus(Camera pressCamera)
        {
            if (focusZone == null) return;
            bool inZone =
                RectTransformUtility.RectangleContainsScreenPoint(focusZone, rectTransform.position, pressCamera);
            if (inZone != IsInZone)
            {
                IsInZone = inZone;
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// TODO: Animate the word
        /// </summary>
        public void RevealTruth()
        {
            if (IsKeyWord && !string.IsNullOrEmpty(truthText))
            {
                Text = truthText;
                textComponent.text = Text;
                SetVisualState(VisualState.Revealed);
                // The manager is now responsible for calling RecalculateLayout after this.
            }
        }

        public void RecalculateLayout()
        {
            if (rectTransform == null) Awake();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            layoutElement.preferredWidth = rectTransform.rect.width;
            layoutElement.preferredHeight = rectTransform.rect.height;
        }

        public enum VisualState
        {
            Default,
            Selected,
            Revealed
        }

        public void SetVisualState(VisualState state)
        {
            switch (state)
            {
                case VisualState.Selected:
                    textComponent.color = Color.yellow;
                    break;
                case VisualState.Revealed:
                    textComponent.color = Color.green;
                    break;
                case VisualState.Default:
                default:
                    textComponent.color = Color.white;
                    break;
            }
        }

        public void ResetVisuals()
        {
            IsSelected = false;
            SetVisualState(VisualState.Default);
        }

        public void SetVisible(bool visible)
        {
            textComponent.enabled = visible;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sizeFitter == null) sizeFitter = GetComponent<ContentSizeFitter>();
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (sizeFitter.horizontalFit != ContentSizeFitter.FitMode.PreferredSize)
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (sizeFitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
#endif
    }
}