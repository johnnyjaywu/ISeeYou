using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ISeeYou
{
    /// <summary>
    /// Manages word data and state.
    /// Uses OnValidate with delayCall to ensure Editor-time sizing matches TMP line height.
    /// Implements ILayoutSelfController for runtime layout precision.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(UIInputFeedback))]
    public class Words : MonoBehaviour, ILayoutElement, ILayoutSelfController, IPointerClickHandler
    {
        // -------------------------------------------------------------------------
        // 1. DATA & EVENTS
        // -------------------------------------------------------------------------

        public event Action<Words> OnWordClicked;
        
        public string Text => wordsData != null ? wordsData.Text : string.Empty;
        public bool IsKey => wordsData is { IsKey: true };
        public bool IsRevealed { get; private set; }

        private WordsData wordsData;
        
        [Header("References")]
        private RectTransform rectTransform;
        private TextMeshProUGUI textComponent;
        private UGUIAnimator animator;
        private UIInputFeedback inputFeedback;

        [Header("Text Appearance")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color revealedColor = new Color(1f, 0.92f, 0.016f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private bool interactionEnabled = true;
        private DrivenRectTransformTracker tracker;

        // -------------------------------------------------------------------------
        // 2. LIFECYCLE
        // -------------------------------------------------------------------------

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            textComponent = GetComponent<TextMeshProUGUI>();
            animator = GetComponent<UGUIAnimator>();
            inputFeedback = GetComponent<UIInputFeedback>();

            if (textComponent != null)
            {
                textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                textComponent.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private void OnEnable()
        {
            UpdateLayout();
        }

        private void OnDisable()
        {
            tracker.Clear();
        }

        private void Update()
        {
            // At runtime, we only need to pulse the layout if an animation is running
            // (e.g., Typewriter effect expanding the width)
            if (Application.isPlaying && animator != null)
            {
                UpdateLayout();
            }
        }

        /// <summary>
        /// Handles Editor-side changes to the component properties.
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            // We can't modify Transform inside OnValidate, so we queue it
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null) UpdateLayout();
            };
#endif
        }

        // -------------------------------------------------------------------------
        // 3. ILayoutSelfController Implementation
        // -------------------------------------------------------------------------

        public void SetLayoutHorizontal()
        {
            tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetPreferredWidth());
        }

        public void SetLayoutVertical()
        {
            tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetPreferredHeight());
        }

        // -------------------------------------------------------------------------
        // 4. SIZE CALCULATION
        // -------------------------------------------------------------------------

        public float GetPreferredWidth()
        {
            return textComponent != null ? textComponent.preferredWidth : 100f;
        }

        public float GetPreferredHeight()
        {
            // Strictly uses the line height of the text component for vertical sizing
            return textComponent != null ? textComponent.preferredHeight : 50f;
        }

        public void UpdateLayout()
        {
            if (rectTransform == null || textComponent == null) return;
            
            tracker.Clear();
            SetLayoutHorizontal();
            SetLayoutVertical();
            
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        // -------------------------------------------------------------------------
        // 5. INITIALIZATION & INTERACTION
        // -------------------------------------------------------------------------

        public void Initialize(WordsData data)
        {
            if (rectTransform == null) Awake();

            bool isTextUpdate = (wordsData != null) && (wordsData.Text != data.Text) && !string.IsNullOrEmpty(wordsData.Text);
            wordsData = data;
            
            textComponent.text = wordsData.Text;
            // if (isTextUpdate && animator != null)
            // {
            //     animator.PlayTypewriter(wordsData.Text);
            // }
            // else
            // {
            //     textComponent.text = wordsData.Text;
            //     textComponent.maxVisibleCharacters = 9999; // Ensure visible
            // }

            gameObject.name = $"Word_{wordsData.Text}";
            IsRevealed = false;
            SetVisualState(VisualState.Normal);
            UpdateLayout();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactionEnabled || eventData.dragging) return;

            // Trigger the animation and wait for it to finish
            animator.PlayClick(() => 
            {
                OnWordClicked?.Invoke(this);
            });
        }

        public void RevealSubtext(Action onComplete = null)
        {
            if (IsRevealed || string.IsNullOrEmpty(wordsData.Subtext))
            {
                onComplete?.Invoke();
                return;
            }

            IsRevealed = true;
    
            // Pass the callback to the animator
            // if (animator != null) 
            // {
            //     animator.PlayTypewriter(wordsData.Subtext, onComplete);
            // }
            // else 
            // {
            //     textComponent.text = wordsData.Subtext;
            //     onComplete?.Invoke();
            // }
            textComponent.text = wordsData.Subtext;
            onComplete?.Invoke();

            SetVisualState(VisualState.Revealed);
            UpdateLayout();
        }

        public void SetInteractable(bool active)
        {
            interactionEnabled = active;
            if (inputFeedback != null) inputFeedback.SetInteractable(active);
            
            if (!active) SetVisualState(VisualState.Disabled);
            else SetVisualState(IsRevealed ? VisualState.Revealed : VisualState.Normal);
        }

        public enum VisualState { Normal, Revealed, Disabled }
        public void SetVisualState(VisualState state)
        {
            if (textComponent == null) return;
            switch (state)
            {
                case VisualState.Normal: textComponent.color = normalColor; break;
                case VisualState.Revealed: textComponent.color = revealedColor; break;
                case VisualState.Disabled: textComponent.color = disabledColor; break;
            }
        }

        public void ResetVisuals()
        {
            IsRevealed = false;
            if (animator != null) animator.ForceReset();
            SetVisualState(VisualState.Normal);
            UpdateLayout();
        }

        // -------------------------------------------------------------------------
        // 6. ILayoutElement
        // -------------------------------------------------------------------------

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => -1;
        public float preferredWidth => GetPreferredWidth();
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => GetPreferredHeight();
        public float flexibleHeight => -1;
        public int layoutPriority => 1;
    }
}