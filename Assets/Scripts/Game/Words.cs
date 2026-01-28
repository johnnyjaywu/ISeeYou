using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ISeeYou
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(LayoutElement))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class Words : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public string Text { get; private set; }
        public bool IsKeyWord { get; private set; }
        public bool IsInFocusZone { get; private set; } = true;

        public event Action OnStateChanged;

        private RectTransform rectTransform;
        private LayoutElement layoutElement;
        private TextMeshProUGUI textComponent;
        private ContentSizeFitter sizeFitter;
        
        private RectTransform focusZone;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            layoutElement = GetComponent<LayoutElement>();
            textComponent = GetComponent<TextMeshProUGUI>();
            sizeFitter = GetComponent<ContentSizeFitter>();
        }

        public void Initialize(string text, bool isKey, RectTransform zone)
        {
            if (rectTransform == null) Awake();

            Text = text;
            IsKeyWord = isKey;
            focusZone = zone;
            
            textComponent.text = Text;
            textComponent.color = Color.white;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            layoutElement.preferredWidth = rectTransform.rect.width;
            layoutElement.preferredHeight = rectTransform.rect.height;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Application.isPlaying) return;
            rectTransform.anchoredPosition += eventData.delta / transform.lossyScale.x;
            
            UpdateFocusZoneStatus(eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Application.isPlaying) return;
            
            UpdateFocusZoneStatus(eventData.pressEventCamera);
        }

        private void UpdateFocusZoneStatus(Camera pressCamera)
        {
            if (focusZone == null) return;

            bool currentlyInZone = RectTransformUtility.RectangleContainsScreenPoint(focusZone, rectTransform.position, pressCamera);

            if (currentlyInZone != IsInFocusZone)
            {
                IsInFocusZone = currentlyInZone;
                OnStateChanged?.Invoke();
            }
        }

        public void SetVisualState(bool isPartOfCorrectSet)
        {
            if (IsInFocusZone && IsKeyWord)
            {
                if (IsKeyWord)
                {
                    // Only turn green if the entire set is correct. Otherwise, stay neutral.
                    textComponent.color = isPartOfCorrectSet ? Color.green : Color.white;
                }
            }
            else
            {
                // Words outside the zone are always neutral.
                textComponent.color = Color.white;
            }
        }

        private void OnValidate()
        {
            if (sizeFitter == null) sizeFitter = GetComponent<ContentSizeFitter>();
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();

            if (sizeFitter.horizontalFit != ContentSizeFitter.FitMode.PreferredSize)
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            if (sizeFitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}