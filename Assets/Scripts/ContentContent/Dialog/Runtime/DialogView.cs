using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.Dialog
{
    [RequireComponent(typeof(DialogLinePresenter), typeof(CanvasGroup))]
    public class DialogView : PoolableBehaviour
    {
        [Tooltip("The unique ID for this UI element, leave blank for default.")]
        [SerializeField] private string viewID;

        [Header("UI")]
        [SerializeField] private TMP_Text dialogText;

        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject nameTextContainer;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private DialogStyle style;
        
        private CanvasGroup canvasGroup;
        private Vector2 originalAnchoredPosition;
        private RectTransform rectTransform;
        private Sequence activeSequence;

        public DialogStyle Style => style;
        public DialogLinePresenter Presenter { get; private set; }

        protected virtual void Awake()
        {
            // Make sure the line presenter is added
            Presenter = GetComponent<DialogLinePresenter>();
            rectTransform = GetComponent<RectTransform>();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        protected virtual void OnEnable()
        {
            Presenter.OnPresentingStarted += OnPresentingStarted;
            Presenter.OnPresentingFinished += OnPresentingFinished;
            DialogManager.Instance.RegisterView(viewID, this);
            DialogManager.Instance.RegisterSkipButton(skipButton);
            DialogManager.Instance.RegisterContinueButton(continueButton);
        }

        protected virtual void OnDisable()
        {
            Presenter.OnPresentingStarted -= OnPresentingStarted;
            Presenter.OnPresentingFinished -= OnPresentingFinished;
            DialogManager.Instance?.UnregisterView(viewID, this);
            DialogManager.Instance?.UnregisterSkipButton(skipButton);
            DialogManager.Instance?.UnregisterContinueButton(continueButton);
        }

        protected virtual void OnPresentingStarted(DialogLinePresenter presenter)
        {
            if (skipButton != null) skipButton.SetActive(Presenter.WaitForInput);
            if (continueButton != null) continueButton.SetActive(false);
        }

        protected virtual void OnPresentingFinished(DialogLinePresenter presenter)
        {
            if (skipButton != null) skipButton.SetActive(false);
            if (continueButton != null) continueButton.SetActive(Presenter.WaitForInput);
        }

        public void SetText(string text)
        {
            if (dialogText != null) dialogText.text = text;
        }

        public void SetVisibleCharacters(int count)
        {
            if (dialogText != null) dialogText.maxVisibleCharacters = count;
        }

        public void SetSpeakerName(string speakerName)
        {
            if (nameTextContainer != null) nameTextContainer.SetActive(!speakerName.IsNullOrEmpty());
            if (nameText != null) nameText.SetText(speakerName);
        }

        public void SetSpeakerPortrait(Sprite sprite)
        {
            if (portraitImage != null)
            {
                portraitImage.SetActive(sprite != null);
                portraitImage.sprite = sprite;
            }
        }

        public virtual Sequence Open(bool animateOpen = true)
        {
            StopActiveTweens();

            if (!animateOpen || !style.animateOpen)
            {
                SetStateInstant(true);
                return default;
            }

            ApplyInitialOpenState();
            activeSequence = Sequence.Create();

            if (style.useOpenAlpha)
                activeSequence.Group(Tween.Alpha(canvasGroup, style.openAlpha));

            if (style.useOpenScale)
                activeSequence.Group(Tween.Scale(rectTransform, style.openScale));

            if (style.useOpenTranslate)
                activeSequence.Group(Tween.UIAnchoredPosition(rectTransform, style.openTranslate));

            activeSequence.OnComplete(this, target => target.EnableInteraction(true));
            return activeSequence;
        }

        public virtual Sequence Close(bool animateClose = true, Action onClosed = null)
        {
            StopActiveTweens();
            EnableInteraction(false);

            if (!animateClose || !style.animateClose)
            {
                SetStateInstant(false);
                return default;
            }

            activeSequence = Sequence.Create();

            if (style.useCloseAlpha)
                activeSequence.Group(Tween.Alpha(canvasGroup, style.closeAlpha));

            if (style.useCloseScale)
                activeSequence.Group(Tween.Scale(rectTransform, style.closeScale));

            if (style.useCloseTranslate) activeSequence.Group(Tween.UIAnchoredPosition(rectTransform, style.closeTranslate));

            if (onClosed != null)
                activeSequence.ChainCallback(onClosed);
            return activeSequence;
        }

        private void ApplyInitialOpenState()
        {
            // We set the "From" state based on the Close settings values if they exist, 
            // otherwise we default to standard visible values.
            canvasGroup.alpha = style.useOpenAlpha ? style.closeAlpha.endValue : 1f;

            if (style.useOpenScale)
                rectTransform.localScale = style.closeScale.endValue;

            if (style.useOpenTranslate)
                rectTransform.anchoredPosition = style.closeTranslate.endValue;
        }

        private void SetStateInstant(bool isOpen)
        {
            canvasGroup.alpha = isOpen ? 1f : style.closeAlpha.endValue;
            rectTransform.localScale = isOpen ? Vector3.one : style.closeScale.endValue;
            rectTransform.anchoredPosition = isOpen
                ? originalAnchoredPosition
                : originalAnchoredPosition + style.closeTranslate.endValue;
            EnableInteraction(isOpen);
        }

        private void StopActiveTweens()
        {
            if (activeSequence.isAlive)
            {
                activeSequence.Stop();
            }

            Tween.StopAll(rectTransform);
            Tween.StopAll(canvasGroup);
        }

        private void EnableInteraction(bool isEnabled)
        {
            canvasGroup.blocksRaycasts = isEnabled;
            canvasGroup.interactable = isEnabled;
        }
    }
}