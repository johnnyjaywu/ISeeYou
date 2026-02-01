using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using System.Collections.Generic;

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

            public TextMeshProUGUI text;
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

        /// <summary>
        /// Shakes the text (or the entire UI element) to draw attention. Uses PrimeTween's ShakeLocalPosition.
        /// </summary>
        [Button("Test Shake")]
        public void PlayShakeText(float strength = 5f, float duration = 0.5f, float frequency = 25f, bool useUnscaledTime = false, Action onComplete = null)
        {
            Transform target = textComponent != null ? textComponent.transform : transform;

            // Stop any current tween targeting this element
            if (activeTween.isAlive) activeTween.Stop();

            Vector3 strengthVec = new Vector3(strength, strength, 0f);
            Vector3 original = target.localPosition;

            // Tween.ShakeLocalPosition(target, strength, duration, frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1, float startDelay = 0f, float endDelay = 0f, bool useUnscaledTime = false)
            activeTween = Tween.ShakeLocalPosition(target, strengthVec, duration, frequency, enableFalloff: true, easeBetweenShakes: Ease.Linear, asymmetryFactor: 0f, cycles: 1, startDelay: 0f, endDelay: 0f, useUnscaledTime: useUnscaledTime)
                .OnComplete(() =>
                {
                    // Ensure we restore the original position in case of numerical drift
                    target.localPosition = original;
                    onComplete?.Invoke();
                });
        }

        [Header("Character Pulse Settings")]
        [SerializeField] private float charDelay = 0.2f;
        [SerializeField] private float pulseScale = 1.5f;
        [SerializeField] private Color pulseColor = Color.yellow;
        [SerializeField] private float charPulseDuration = 0.8f;
        
        private Vector3[][] originalVertices; // Store original positions

        private Dictionary<int, float> charProgress = new Dictionary<int, float>();

        [Button("Test Char Pulse")]
        public void PlayCharPulse()
        {
            string word = interaction.text.text;
            
            interaction.text.ForceMeshUpdate();
            TMP_TextInfo textInfo = interaction.text.textInfo;
            
            StoreOriginalVertices(textInfo);
            Color32 originalColor = interaction.text.color;
            
            charProgress.Clear();
            
            // Create tweens that update the progress dictionary
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;
                    
                int charIndex = i;
                charProgress[charIndex] = 0f;
                
                Tween.Custom(0f, 1f, charPulseDuration, onValueChange: progress =>
                {
                    charProgress[charIndex] = progress;
                    
                    // Update ALL characters every frame
                    UpdateAllChars(originalColor);
                    
                }, startDelay: i * charDelay, ease: Ease.OutBack);
            }
        }

        void UpdateAllChars(Color32 originalColor)
        {
            TMP_TextInfo textInfo = interaction.text.textInfo;
            
            // Update EVERY character based on its current progress
            foreach (var kvp in charProgress)
            {
                int charIndex = kvp.Key;
                float progress = kvp.Value;
                
                if (charIndex >= textInfo.characterCount) continue;
                
                TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
                if (!charInfo.isVisible) continue;
                
                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
                
                Vector3 center = (originalVertices[materialIndex][vertexIndex] + 
                                originalVertices[materialIndex][vertexIndex + 2]) / 2f;
                
                float t = Mathf.Sin(progress * Mathf.PI);
                float scale = 1f + (pulseScale - 1f) * t;
                Color lerpedColor = Color.Lerp(originalColor, pulseColor, t);
                
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = originalVertices[materialIndex][vertexIndex + i] - center;
                    vertices[vertexIndex + i] = center + offset * scale;
                    colors[vertexIndex + i] = lerpedColor;
                }
            }
            
            // Update mesh ONCE after all characters are processed
            interaction.text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }

        void StoreOriginalVertices(TMP_TextInfo textInfo)
        {
            originalVertices = new Vector3[textInfo.meshInfo.Length][];
            
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                Vector3[] sourceVertices = textInfo.meshInfo[i].vertices;
                originalVertices[i] = new Vector3[sourceVertices.Length];
                
                // Copy original positions
                for (int j = 0; j < sourceVertices.Length; j++)
                {
                    originalVertices[i][j] = sourceVertices[j];
                }
            }
        }

        [Button("Test RubberBand")]
        public void PlayRubberBand()
        {
            Transform target = textComponent != null ? textComponent.transform : transform;
            Tween.Scale(target, new Vector3(1.3f, 0.8f, 1f), 0.4f, ease: Ease.OutBack)
                .OnComplete(() => 
                    Tween.Scale(target, Vector3.one, 0.3f, ease: Ease.OutBack)
                );
        }
        
        // Jello wobble
        [Button("Test JelloWobble")]
        public void PlayJelloWobble()
        {
            Transform target = textComponent != null ? textComponent.transform : transform;
            Tween.PunchScale(target, new Vector3(0.3f, -0.2f, 0f), 0.6f, 10, false, Ease.OutElastic);
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