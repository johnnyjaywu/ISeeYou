using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace ISeeYou
{
    /// <summary>
    /// Core UI Animation engine using PrimeTween.
    /// Handles Intro/Outro sequences, Hover/Click feedback, and Typewriter effects.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UGUIAnimator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // 1. DATA STRUCTURES
        // -------------------------------------------------------------------------

        [System.Flags]
        public enum AnimationType
        {
            None = 0,
            Scale = 1 << 0,
            Fade = 1 << 1,
            Typewriter = 1 << 2
        }

        [System.Serializable]
        public class IntroSettings
        {
            [EnumFlags] public AnimationType SelectedAnimations = AnimationType.Scale | AnimationType.Fade;
            public float Duration = 0.4f;
            public Ease Easing = Ease.OutBack;
            public float Delay = 0f;

            [ShowIf("HasTypewriter")]
            public float CharsPerSecond = 30f;

            public bool HasTypewriter() => SelectedAnimations.HasFlag(AnimationType.Typewriter);
        }

        [System.Serializable]
        public class OutroSettings
        {
            [EnumFlags] public AnimationType SelectedAnimations = AnimationType.Scale | AnimationType.Fade;
            public float Duration = 0.3f;
            public Ease Easing = Ease.InBack;
            public bool DestroyOnComplete = true;
        }

        [System.Serializable]
        public class InteractionSettings
        {
            [Header("Hover")]
            public bool EnableHover = true;

            public float HoverScale = 1.05f;
            public float HoverDuration = 0.15f;
            public Ease HoverEase = Ease.OutQuad;

            [Header("Click (Press)")]
            public bool EnableClick = true;

            public Vector3 ClickPunch = new Vector3(-0.1f, -0.1f, 0);
            public float ClickDuration = 0.15f;

            [Tooltip("Set to 0 for a simple press/shrink. Higher values add a wobble.")]
            public int ClickVibrato = 0;
        }

        // -------------------------------------------------------------------------
        // 2. CONFIGURATION
        // -------------------------------------------------------------------------

        [Header("General")]
        [SerializeField] private bool playIntroOnEnable = true;

        [Header("Sequences")]
        [SerializeField] private IntroSettings intro;

        [SerializeField] private OutroSettings outro;

        [Header("Interaction")]
        [SerializeField] private InteractionSettings interaction;

        [Header("Attention (Pulse)")]
        [SerializeField] private float pulseDuration = 0.3f;

        [SerializeField] private float pulseScaleAmount = 1.1f;

        // -------------------------------------------------------------------------
        // 3. INTERNAL STATE
        // -------------------------------------------------------------------------

        private CanvasGroup canvasGroup;
        private TextMeshProUGUI textComponent;

        private Tween activeTween;
        private Sequence activeSequence;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>() ?? GetComponentInChildren<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (playIntroOnEnable && Application.isPlaying)
            {
                PlayIntro();
            }
        }

        private void OnDisable()
        {
            StopAll();
        }

        // -------------------------------------------------------------------------
        // 4. PUBLIC API - SEQUENCES
        // -------------------------------------------------------------------------

        [Button("Test Intro")]
        public void PlayIntro()
        {
            CompleteCurrent();

            // Setup Initial State
            if (intro.SelectedAnimations.HasFlag(AnimationType.Scale))
                transform.localScale = Vector3.zero;

            if (intro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (intro.SelectedAnimations.HasFlag(AnimationType.Typewriter) && textComponent != null)
                textComponent.maxVisibleCharacters = 0;

            activeSequence = Sequence.Create();

            if (intro.Delay > 0) activeSequence.Group(Tween.Delay(intro.Delay));

            if (intro.SelectedAnimations.HasFlag(AnimationType.Scale))
                activeSequence.Group(Tween.Scale(transform, Vector3.one, intro.Duration, intro.Easing));

            if (intro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null)
                activeSequence.Group(Tween.Alpha(canvasGroup, 1f, intro.Duration, intro.Easing));

            if (intro.SelectedAnimations.HasFlag(AnimationType.Typewriter) && textComponent != null)
            {
                textComponent.ForceMeshUpdate();
                int totalChars = textComponent.textInfo.characterCount;
                float typeDuration = totalChars / Mathf.Max(1, intro.CharsPerSecond);

                activeSequence.Group(Tween.Custom(0, totalChars, typeDuration,
                    onValueChange: (val) => { textComponent.maxVisibleCharacters = Mathf.FloorToInt(val); }));
            }
        }

        [Button("Test Outro")]
        public void PlayOutro()
        {
            CompleteCurrent();

            activeSequence = Sequence.Create();

            if (outro.SelectedAnimations.HasFlag(AnimationType.Scale))
                activeSequence.Group(Tween.Scale(transform, Vector3.zero, outro.Duration, outro.Easing));

            if (outro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null)
                activeSequence.Group(Tween.Alpha(canvasGroup, 0f, outro.Duration, outro.Easing));

            activeSequence.OnComplete(() =>
            {
                if (outro.DestroyOnComplete) Destroy(gameObject);
                else gameObject.SetActive(false);
            });
        }

        // -------------------------------------------------------------------------
        // 5. PUBLIC API - INTERACTION
        // -------------------------------------------------------------------------

        public void PlayHover(bool isHovering)
        {
            if (!interaction.EnableHover) return;

            // Stop hover tweens only to avoid breaking the Intro/Outro if they run simultaneously
            if (activeTween.isAlive) activeTween.Stop();

            float targetScale = isHovering ? interaction.HoverScale : 1.0f;
            activeTween = Tween.Scale(transform, Vector3.one * targetScale, interaction.HoverDuration,
                interaction.HoverEase);
        }

        /// <summary>
        /// Plays a click feedback effect and executes a callback when finished.
        /// </summary>
        public void PlayClick(System.Action onComplete = null)
        {
            if (!interaction.EnableClick)
            {
                onComplete?.Invoke();
                return;
            }

            if (activeTween.isAlive) activeTween.Stop();

            if (interaction.ClickVibrato > 0)
            {
                activeTween = Tween.PunchScale(transform, interaction.ClickPunch, interaction.ClickDuration,
                        interaction.ClickVibrato)
                    .OnComplete(onComplete);
            }
            else
            {
                Vector3 targetScale = Vector3.one + interaction.ClickPunch;
                activeTween = Tween.Scale(transform, targetScale, interaction.ClickDuration / 2f, Ease.OutQuad,
                        cycles: 2, cycleMode: CycleMode.Yoyo)
                    .OnComplete(onComplete);
            }
        }

        public void PlayTypewriter(string newText, System.Action onComplete = null)
        {
            if (textComponent == null) return;

            // Safety: If we are already typing this EXACT text, don't restart
            if (textComponent.text == newText && activeTween.isAlive) return;

            CompleteCurrent();

            textComponent.SetText(newText);
            textComponent.ForceMeshUpdate();
            textComponent.maxVisibleCharacters = 0;

            int totalChars = newText.Length;

            if (totalChars <= 0)
            {
                textComponent.maxVisibleCharacters = 9999;
                onComplete?.Invoke();
                return;
            }

            float duration = totalChars / Mathf.Max(1, intro.CharsPerSecond);

            activeTween = Tween.Custom(0, totalChars, duration,
                    onValueChange: (val) => { textComponent.maxVisibleCharacters = Mathf.FloorToInt(val); })
                .OnComplete(() =>
                {
                    textComponent.maxVisibleCharacters = 9999;
                    onComplete?.Invoke();
                });
        }

        [Button("Test Pulse")]
        public void PlayPulse()
        {
            CompleteCurrent();
            activeTween = Tween.Scale(transform, Vector3.one * pulseScaleAmount, pulseDuration / 2f, Ease.OutQuad,
                cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        // -------------------------------------------------------------------------
        // 6. UTILITIES
        // -------------------------------------------------------------------------

        public void ForceReset()
        {
            StopAll();
            transform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (textComponent != null) textComponent.maxVisibleCharacters = 9999;
        }

        public void CompleteCurrent()
        {
            if (activeTween.isAlive) activeTween.Complete();
            if (activeSequence.isAlive) activeSequence.Complete();
        }

        public void StopAll()
        {
            if (activeTween.isAlive) activeTween.Stop();
            if (activeSequence.isAlive) activeSequence.Stop();
        }
    }
}