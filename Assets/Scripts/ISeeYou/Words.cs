using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou
{
    /// <summary>
    /// Manages word data and logic state.
    /// Acts as a purely reactive layout element that resizes itself to fit its TextMeshPro content.
    /// Integrates with UISelectable to handle selection state.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(UISelectable))]
    public class Words : MonoBehaviour, ILayoutElement, ILayoutSelfController
    {
        // -------------------------------------------------------------------------
        // 1. DATA & STATE
        // -------------------------------------------------------------------------

        public enum LogicState
        {
            Normal,
            Revealed,
            Disabled
        }

        public string Text => wordsData != null ? wordsData.Text : string.Empty;
        public bool IsKey => wordsData is { IsKey: true };
        public bool IsRevealed => CurrentState == LogicState.Revealed;

        // Public accessor for external feedback scripts to listen/poll
        public LogicState CurrentState { get; private set; } = LogicState.Normal;

        // Surfaced Selection State
        public bool IsSelected => selectable != null && selectable.IsSelected;

        // Surfaced Selection Events (passes 'this' for easy identification by managers)
        public event Action<Words, bool> OnSelectionChanged;
        public event Action<Words> OnWordConfirmed;

        private WordsData wordsData;
        private RectTransform rectTransform;
        private TextMeshProUGUI textComponent;
        private UISelectable selectable;

        // Tracker prevents the Inspector from allowing manual resize of driven properties
        private DrivenRectTransformTracker tracker;

        // -------------------------------------------------------------------------
        // 2. LIFECYCLE
        // -------------------------------------------------------------------------

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            textComponent = GetComponent<TextMeshProUGUI>();
            selectable = GetComponent<UISelectable>();

            if (textComponent != null)
            {
                // Ensure text settings support auto-sizing logic
                textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                textComponent.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private void OnEnable()
        {
            if (selectable != null)
            {
                selectable.OnSelectionChanged += HandleInternalSelection;
                selectable.OnConfirm += HandleInternalConfirm;
            }

            MarkLayoutDirty();
        }

        private void OnDisable()
        {
            if (selectable != null)
            {
                selectable.OnSelectionChanged -= HandleInternalSelection;
                selectable.OnConfirm -= HandleInternalConfirm;
            }

            tracker.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Delay call avoids "SendTransformChanged" errors during serialization
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject.activeInHierarchy) MarkLayoutDirty();
            };
        }
#endif

        // -------------------------------------------------------------------------
        // 3. EVENT HANDLERS
        // -------------------------------------------------------------------------

        private void HandleInternalSelection(bool selected)
        {
            OnSelectionChanged?.Invoke(this, selected);
        }

        private void HandleInternalConfirm()
        {
            OnWordConfirmed?.Invoke(this);
        }

        // -------------------------------------------------------------------------
        // 4. ILayoutSelfController Implementation
        // -------------------------------------------------------------------------

        public void SetLayoutHorizontal()
        {
            if (rectTransform == null || textComponent == null) return;

            // Drive the Width based on text content
            tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textComponent.preferredWidth);
        }

        public void SetLayoutVertical()
        {
            if (rectTransform == null || textComponent == null) return;

            // Drive the Height based on text content
            tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textComponent.preferredHeight);
        }

        // -------------------------------------------------------------------------
        // 5. LOGIC
        // -------------------------------------------------------------------------

        public void Initialize(WordsData data)
        {
            if (rectTransform == null) Awake();

            wordsData = data;
            textComponent.text = wordsData.Text;

            gameObject.name = $"Word_{wordsData.Text}";
            CurrentState = LogicState.Normal;

            // Ensure selection state is reset on re-initialization
            if (selectable.IsSelected) selectable.Deselect();

            MarkLayoutDirty();
        }

        public void RevealSubtext(Action onComplete = null)
        {
            if (CurrentState == LogicState.Revealed || string.IsNullOrEmpty(wordsData.Subtext))
            {
                onComplete?.Invoke();
                return;
            }

            CurrentState = LogicState.Revealed;
            textComponent.text = wordsData.Subtext;

            MarkLayoutDirty();
            onComplete?.Invoke();
        }

        public void SetInteractable(bool active)
        {
            if (!active)
            {
                CurrentState = LogicState.Disabled;

                // If disabled, we likely want to force deselect
                if (selectable.IsSelected) selectable.Deselect();
            }
            else
            {
                // Restore state based on whether we were previously revealed or not
                bool isShowingSubtext = textComponent.text == wordsData.Subtext;
                CurrentState = isShowingSubtext ? LogicState.Revealed : LogicState.Normal;
            }

            // Optional: Disable the UISelectable component itself if not interactable
            selectable.enabled = active;
        }

        public void ResetState()
        {
            CurrentState = LogicState.Normal;
            if (wordsData != null) textComponent.text = wordsData.Text;

            if (selectable.IsSelected) selectable.Deselect();
            selectable.enabled = true;

            MarkLayoutDirty();
        }

        private void MarkLayoutDirty()
        {
            if (rectTransform != null && gameObject.activeInHierarchy)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }
        }

        // -------------------------------------------------------------------------
        // 6. ILayoutElement
        // -------------------------------------------------------------------------

        public float minWidth => -1;
        public float preferredWidth => textComponent != null ? textComponent.preferredWidth : 0;
        public float flexibleWidth => -1;

        public float minHeight => -1;
        public float preferredHeight => textComponent != null ? textComponent.preferredHeight : 0;
        public float flexibleHeight => -1;

        public int layoutPriority => 1;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        /// <summary>
        /// Parses text using Regex. Prioritizes [...] groups, then standard words.
        /// Strips brackets and trims whitespace from all results.
        /// </summary>
        public static List<string> ParseGroupedString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();

            // Pattern: \[([^\]]*)\]  -> Matches content inside brackets (Group 1)
            //          |             -> OR
            //          (\S+)         -> Matches continuous non-whitespace characters (Group 2)
            var matches = Regex.Matches(input, @"\[([^\]]*)\]|(\S+)");

            return matches.Cast<Match>()
                // Pick Group 1 (bracket content) if matched, otherwise Group 2 (word)
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Select(text => text.Trim()) // Remove leading/trailing whitespace
                .Where(text => !string.IsNullOrEmpty(text)) // Discard empty entries
                .ToList();
        }
    }
}