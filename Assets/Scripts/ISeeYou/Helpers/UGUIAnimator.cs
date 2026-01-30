using System;
using PrimeTween; 
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes; // Requires NaughtyAttributes for [Button] and [ShowIf]

namespace ISeeYou
{
    /// <summary>
    /// A modular animation component for UI elements.
    /// Handles Intro/Outro sequences, Interaction feedback (Hover/Click), and Typewriter effects.
    /// Uses PrimeTween for high-performance, allocation-free animations.
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
            public bool DestroyOnComplete = false;
        }

        [System.Serializable]
        public class InteractionSettings
        {
            [Header("Hover")]
            public bool EnableHover = true;
            public float HoverScale = 1.1f;
            public float HoverDuration = 0.2f;
            public Ease HoverEase = Ease.OutQuad;

            [Header("Click (Press)")]
            public bool EnableClick = true;
            public Vector3 ClickPunch = new Vector3(-0.1f, -0.1f, 0); // Slight shrink
            public float ClickDuration = 0.15f;
            public int ClickVibrato = 5; // 0 = simple press, >0 = wobble
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
        private RectTransform rectTransform;
        private TextMeshProUGUI textComponent;

        // Handles to track and stop running animations
        private Tween activeTween;
        private Sequence activeSequence;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            textComponent = GetComponent<TextMeshProUGUI>();
            
            // Fallback: look in children if not on root
            if (textComponent == null) textComponent = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (playIntroOnEnable)
            {
                PlayIntro();
            }
        }
        
        private void OnDisable()
        {
            StopAll();
        }

        // -------------------------------------------------------------------------
        // 4. PUBLIC API
        // -------------------------------------------------------------------------

        [Button("Test Intro")]
        public void PlayIntro()
        {
            StopAll();
            
            // A. Setup Initial State (Snap to start values)
            if (intro.SelectedAnimations.HasFlag(AnimationType.Scale)) 
                transform.localScale = Vector3.zero;
            
            if (intro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null) 
                canvasGroup.alpha = 0f;

            if (intro.SelectedAnimations.HasFlag(AnimationType.Typewriter) && textComponent != null)
            {
                textComponent.maxVisibleCharacters = 0;
                // If we are ONLY typing, ensure the container is visible
                if (!intro.SelectedAnimations.HasFlag(AnimationType.Scale)) transform.localScale = Vector3.one;
                if (!intro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null) canvasGroup.alpha = 1f;
            }

            // B. Build Parallel Sequence
            activeSequence = Sequence.Create();

            if (intro.Delay > 0) activeSequence.Group(Tween.Delay(intro.Delay));

            if (intro.SelectedAnimations.HasFlag(AnimationType.Scale))
            {
                activeSequence.Group(Tween.Scale(transform, Vector3.one, intro.Duration, intro.Easing));
            }

            if (intro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null)
            {
                activeSequence.Group(Tween.Alpha(canvasGroup, 1f, intro.Duration, intro.Easing));
            }

            if (intro.SelectedAnimations.HasFlag(AnimationType.Typewriter) && textComponent != null)
            {
                textComponent.ForceMeshUpdate();
                int totalChars = textComponent.textInfo.characterCount;
                float typeDuration = totalChars / Mathf.Max(1, intro.CharsPerSecond);

                activeSequence.Group(Tween.Custom(0, totalChars, typeDuration, onValueChange: (val) =>
                {
                    textComponent.maxVisibleCharacters = (int)val;
                }));
            }
        }

        [Button("Test Outro")]
        public void PlayOutro()
        {
            StopAll();

            activeSequence = Sequence.Create();

            if (outro.SelectedAnimations.HasFlag(AnimationType.Scale))
            {
                activeSequence.Group(Tween.Scale(transform, Vector3.zero, outro.Duration, outro.Easing));
            }

            if (outro.SelectedAnimations.HasFlag(AnimationType.Fade) && canvasGroup != null)
            {
                activeSequence.Group(Tween.Alpha(canvasGroup, 0f, outro.Duration, outro.Easing));
            }

            // Cleanup Callback
            activeSequence.OnComplete(() => 
            {
                if (outro.DestroyOnComplete) Destroy(gameObject);
                else gameObject.SetActive(false);
            });
        }

        public void PlayHover(bool isHovering)
        {
            if (!interaction.EnableHover) return;

            // Stop any conflicting scale animations
            if (activeSequence.isAlive) activeSequence.Stop();
            if (activeTween.isAlive) activeTween.Stop();

            float targetScale = isHovering ? interaction.HoverScale : 1.0f;
            
            activeTween = Tween.Scale(transform, Vector3.one * targetScale, interaction.HoverDuration, interaction.HoverEase);
        }

        public void PlayClick()
        {
            if (!interaction.EnableClick) return;

            // Stop hover momentarily so the click registers
            if (activeTween.isAlive) activeTween.Stop();

            // PunchScale: From current -> punch -> back to current
            if (interaction.ClickVibrato > 0)
            {
                // Punch requires vibrato > 0
                activeTween = Tween.PunchScale(transform, interaction.ClickPunch, interaction.ClickDuration, interaction.ClickVibrato);
            }
            else
            {
                // If vibrato is 0, do a simple "Press" and return to 1.0
                // Using cycles: 2 and CycleMode.Yoyo makes it go: 1.0 -> Punch -> 1.0
                Vector3 targetScale = Vector3.one + interaction.ClickPunch;
                activeTween = Tween.Scale(transform, targetScale, interaction.ClickDuration / 2f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
            }
        }

        [Button("Test Pulse")]
        public void PlayPulse()
        {
            StopAll();
            activeTween = Tween.Scale(transform, Vector3.one * pulseScaleAmount, pulseDuration / 2f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        public void PlayTypewriter(string newText)
        {
            if (textComponent != null) textComponent.text = newText;
            
            StopAll();
            
            textComponent.maxVisibleCharacters = 0;
            textComponent.ForceMeshUpdate();
            
            int totalChars = textComponent.textInfo.characterCount;
            float duration = totalChars / Mathf.Max(1, intro.CharsPerSecond);

            activeTween = Tween.Custom(0, totalChars, duration, onValueChange: (val) =>
            {
                textComponent.maxVisibleCharacters = (int)val;
            });
        }

        /// <summary>
        /// Instantly resets visual state to default (Scale 1, Alpha 1, Full Text).
        /// Use this when reusing pooled objects or hard-resetting UI.
        /// </summary>
        public void ForceReset()
        {
            StopAll();
            transform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (textComponent != null) textComponent.maxVisibleCharacters = 99999;
        }

        private void StopAll()
        {
            if (activeTween.isAlive) activeTween.Stop();
            if (activeSequence.isAlive) activeSequence.Stop();
        }
    }
}