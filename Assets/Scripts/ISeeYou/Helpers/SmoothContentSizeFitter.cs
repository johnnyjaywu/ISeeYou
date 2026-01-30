using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace ISeeYou
{
    [RequireComponent(typeof(RectTransform))]
    public class SmoothContentSizeFitter : MonoBehaviour
    {
        [Header("Settings")]
        public bool horizontal = true;
        public bool vertical = true;
        public float duration = 0.3f;
        public Ease ease = Ease.OutQuad;

        private RectTransform rectTransform;
        private Vector2 lastTargetSize;
        private Tween currentTween;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            lastTargetSize = rectTransform.sizeDelta;
        }

        private void LateUpdate()
        {
            // 1. Calculate the size the Layout Group WANTS to be
            float targetWidth = horizontal 
                ? LayoutUtility.GetPreferredWidth(rectTransform) 
                : rectTransform.sizeDelta.x;
            
            float targetHeight = vertical 
                ? LayoutUtility.GetPreferredHeight(rectTransform) 
                : rectTransform.sizeDelta.y;

            Vector2 newTargetSize = new Vector2(targetWidth, targetHeight);

            // 2. If the target size has changed significantly, start animating
            if (Vector2.Distance(newTargetSize, lastTargetSize) > 0.5f)
            {
                lastTargetSize = newTargetSize;
                
                // Stop any existing tween to avoid conflicts
                if (currentTween.isAlive) currentTween.Stop();

                // 3. Tween to the new size using PrimeTween
                currentTween = Tween.UISizeDelta(
                    rectTransform, 
                    newTargetSize, 
                    duration, 
                    ease
                );
                
                // Optional: Notify the LayoutGroup to refresh as we resize
                // (Usually not needed if the LayoutGroup is driven by this rect, 
                // but ensures children stay anchored correctly during animation)
                currentTween.OnUpdate(rectTransform, (target, tweenVal) => 
                {
                    LayoutRebuilder.MarkLayoutForRebuild(target);
                });
            }
        }
        
        // Helper to force an instant update (e.g. on spawn)
        public void ForceUpdate()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            lastTargetSize = new Vector2(
                LayoutUtility.GetPreferredWidth(rectTransform), 
                LayoutUtility.GetPreferredHeight(rectTransform)
            );
            rectTransform.sizeDelta = lastTargetSize;
        }
    }
}