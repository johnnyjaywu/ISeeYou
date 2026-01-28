using System;
using ContentContent.Audio;
using PrimeTween;
using UnityEngine;

namespace ContentContent.Dialog
{
    [Serializable]
    public class DialogStyle
    {
        [Header("Animation")]
        public bool animateOpen = true;

        public bool useOpenAlpha;
        public TweenSettings<float> openAlpha = new(1f, 0.3f, Ease.Linear);

        public bool useOpenScale;
        public TweenSettings<Vector3> openScale = new(Vector3.one, 0.3f, Ease.OutBack);

        public bool useOpenTranslate;
        public TweenSettings<Vector2> openTranslate = new(Vector2.zero, 0.3f, Ease.OutQuad);

        public bool animateClose = true;

        public bool useCloseAlpha;
        public TweenSettings<float> closeAlpha = new(0f, 0.2f, Ease.Linear);

        public bool useCloseScale;

        public TweenSettings<Vector3> closeScale = new(new Vector3(0.7f, 0.7f, 0.7f), 0.2f, Ease.InBack);

        public bool useCloseTranslate;

        public TweenSettings<Vector2> closeTranslate = new(new Vector2(0f, -100f), 0.2f, Ease.InQuad);

        [Header("Audio")]
        public SoundData openingSound;

        public SoundData closingSound;

        public SoundData typingSound;

        [Header("Typewriter")]
        [Tooltip("Seconds per character (0 = instant)")]
        public float typeSpeed = 0.05f;
    }
}