using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou
{
    /// <summary> 
    /// Controls the layout of speech bubbles, arranging words into lines within a given container.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class SpeechLayoutController : MonoBehaviour
    {
        [SerializeField] private LineLayoutGroup linePrefab;

        private RectTransform containerRect;
        private VerticalLayoutGroup layoutGroup;

        private void Awake()
        {
            containerRect = GetComponent<RectTransform>();
            layoutGroup = GetComponent<VerticalLayoutGroup>();
        }

        public void Rebuild(List<Words> wordsToLayout)
        {
            // Clear existing content
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            if (wordsToLayout.Count == 0) return;

            // Calculate usable width
            float horizontalPadding = layoutGroup ? (float)layoutGroup.padding.horizontal : 0f;
            float linePadding = linePrefab.padding.horizontal;
            float usableWidth = containerRect.rect.width - horizontalPadding - linePadding;
            float lineSpacing = linePrefab.spacing;

            HorizontalLayoutGroup currentLine = CreateNewLine();
            float currentLineWidth = 0f;

            foreach (Words word in wordsToLayout)
            {
                word.RecalculateLayout();
                float wordWidth = word.GetComponent<LayoutElement>().preferredWidth;

                bool isFirstWord = currentLineWidth == 0f;
                float requiredWidth = isFirstWord ? wordWidth : lineSpacing + wordWidth;

                if (!isFirstWord && currentLineWidth + requiredWidth > usableWidth)
                {
                    currentLine = CreateNewLine();
                    currentLineWidth = 0f;
                    isFirstWord = true;
                }

                word.transform.SetParent(currentLine.transform, false);
                currentLineWidth += isFirstWord ? wordWidth : lineSpacing + wordWidth;
            }
        }

        private HorizontalLayoutGroup CreateNewLine()
        {
            return Instantiate(linePrefab, transform);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (layoutGroup == null) layoutGroup = GetComponent<VerticalLayoutGroup>();
            // layoutGroup.childControlWidth = true;
            // layoutGroup.childControlHeight = true;
            // layoutGroup.childForceExpandWidth = true;
        }
#endif
    }
}