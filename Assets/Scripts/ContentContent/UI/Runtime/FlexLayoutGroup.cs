using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.UI
{
    [AddComponentMenu("Layout/Flex Layout Group")]
    public class FlexLayoutGroup : LayoutGroup
    {
        public enum FlexDirection { Row, Column, RowReverse, ColumnReverse }
        public enum FlexWrap { NoWrap, Wrap, WrapReverse }
        public enum JustifyContent { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly }
        public enum AlignItems { FlexStart, Center, FlexEnd, Stretch }

        [SerializeField] private FlexDirection direction = FlexDirection.Row;
        [SerializeField] private FlexWrap wrap = FlexWrap.Wrap;
        [SerializeField] private JustifyContent justifyContent = JustifyContent.FlexStart;
        [SerializeField] private AlignItems alignItems = AlignItems.Center;

        [SerializeField] private Vector2 spacing = Vector2.zero;

        [Tooltip("The LayoutGroup will request at least this size from the layout system.")]
        [SerializeField] private Vector2 minSize = Vector2.zero;

        [Tooltip("If > 0, the layout will wrap when this size is reached. X = Width, Y = Height.")]
        [SerializeField] private Vector2 maxSize = Vector2.zero;

        [SerializeField] private bool forceExpandMainAxis = false;
        [SerializeField] private bool forceExpandCrossAxis = false;

        private readonly List<FlexLine> linesList = new List<FlexLine>();

        private class FlexLine
        {
            public int StartIndex;
            public int Count;
            public float MainSize;
            public float CrossSize;
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            // 1. Calculate the wrapping and line logic
            CalculateChildrenLayout();

            // 2. Determine Preferred Width
            float totalPreferredWidth = padding.horizontal;
            bool isColumn = direction == FlexDirection.Column || direction == FlexDirection.ColumnReverse;

            if (isColumn)
            {
                // In Column mode, Width is determined by the widest column (CrossSize)
                float maxLineWidth = 0;
                foreach (var line in linesList) maxLineWidth = Mathf.Max(maxLineWidth, line.CrossSize);
                totalPreferredWidth += maxLineWidth;
            }
            else
            {
                // In Row mode, Width is determined by the widest row (MainSize)
                float maxLineWidth = 0;
                foreach (var line in linesList) maxLineWidth = Mathf.Max(maxLineWidth, line.MainSize);
                totalPreferredWidth += maxLineWidth;
            }

            // 3. Apply Minimum Size Constraint
            totalPreferredWidth = Mathf.Max(totalPreferredWidth, minSize.x);

            // 4. Set Inputs (Min, Preferred, Flexible)
            // We use the same value for Min and Preferred to ensure stability with ContentSizeFitter
            SetLayoutInputForAxis(totalPreferredWidth, totalPreferredWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            // Ensure lines exist (sanity check)
            if (linesList.Count == 0) CalculateChildrenLayout();

            // 1. Determine Preferred Height
            float totalPreferredHeight = padding.vertical;
            bool isColumn = direction == FlexDirection.Column || direction == FlexDirection.ColumnReverse;

            if (isColumn)
            {
                // In Column mode, Height is the sum of main axis lengths
                float totalMainSize = 0;
                foreach (var line in linesList) totalMainSize += line.MainSize;

                // Add spacing between items in the column stack effectively
                // Note: Logic correction - If wrapping columns, we are stacking them horizontally.
                // But the HEIGHT of the container is defined by the tallest column.
                
                // Wait, standard Flexbox column wrap:
                // Items go down, then wrap to next column (right).
                // So Height = Max MainSize of any line.
                
                float maxLineHeight = 0;
                foreach (var line in linesList) maxLineHeight = Mathf.Max(maxLineHeight, line.MainSize);
                totalPreferredHeight += maxLineHeight;
            }
            else
            {
                // In Row mode, Height is the sum of line heights (CrossSize) plus spacing
                float totalCrossSize = 0;
                foreach (var line in linesList) totalCrossSize += line.CrossSize;

                if (linesList.Count > 1) totalPreferredHeight += (linesList.Count - 1) * spacing.y;
                totalPreferredHeight += totalCrossSize;
            }

            // 2. Apply Minimum Size Constraint
            totalPreferredHeight = Mathf.Max(totalPreferredHeight, minSize.y);

            SetLayoutInputForAxis(totalPreferredHeight, totalPreferredHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1);
        }

        private void CalculateChildrenLayout()
        {
            linesList.Clear();
            var activeChildren = rectChildren;

            bool isColumn = direction == FlexDirection.Column || direction == FlexDirection.ColumnReverse;
            int mainAxis = isColumn ? 1 : 0;
            int crossAxis = isColumn ? 0 : 1;

            float containerBoundary = isColumn ? rectTransform.rect.height : rectTransform.rect.width;
            
            // Resolve Constraint: Use MaxSize if set, otherwise use container size
            float maxConstraint = isColumn ? maxSize.y : maxSize.x;
            
            float availableMainSize = (maxConstraint > 0) ? maxConstraint : containerBoundary;
            availableMainSize -= isColumn ? padding.vertical : padding.horizontal;

            // Infinite layout handling
            if (availableMainSize <= 0) availableMainSize = float.MaxValue;

            float currentMainSize = 0;
            float maxCrossSizeInLine = 0;
            int startIndex = 0;
            float mainSpacing = isColumn ? spacing.y : spacing.x;

            for (int i = 0; i < activeChildren.Count; i++)
            {
                RectTransform child = activeChildren[i];
                float childMainSize = LayoutUtility.GetPreferredSize(child, mainAxis);
                float childCrossSize = LayoutUtility.GetPreferredSize(child, crossAxis);

                float itemSizeWithSpacing = childMainSize + (i > startIndex ? mainSpacing : 0);

                bool requiresWrap = false;
                if (wrap != FlexWrap.NoWrap)
                {
                    // Check against available size
                    if (currentMainSize + itemSizeWithSpacing > availableMainSize && i > startIndex)
                    {
                        requiresWrap = true;
                    }
                }

                if (requiresWrap)
                {
                    linesList.Add(new FlexLine
                    {
                        StartIndex = startIndex,
                        Count = i - startIndex,
                        MainSize = currentMainSize,
                        CrossSize = maxCrossSizeInLine
                    });

                    startIndex = i;
                    currentMainSize = 0;
                    maxCrossSizeInLine = 0;
                    itemSizeWithSpacing = childMainSize;
                }

                currentMainSize += itemSizeWithSpacing;
                maxCrossSizeInLine = Mathf.Max(maxCrossSizeInLine, childCrossSize);
            }

            if (startIndex < activeChildren.Count)
            {
                linesList.Add(new FlexLine
                {
                    StartIndex = startIndex,
                    Count = activeChildren.Count - startIndex,
                    MainSize = currentMainSize,
                    CrossSize = maxCrossSizeInLine
                });
            }
        }

        private void SetChildrenAlongAxis(int axis)
        {
            var activeChildren = rectChildren;
            bool isColumn = direction == FlexDirection.Column || direction == FlexDirection.ColumnReverse;
            int mainAxisIndex = isColumn ? 1 : 0;
            int crossAxisIndex = isColumn ? 0 : 1;

            if (axis != mainAxisIndex && axis != crossAxisIndex) return;

            float containerSizeMain = rectTransform.rect.size[mainAxisIndex];
            float startPaddingMain = isColumn ? padding.top : padding.left;
            float startPaddingCross = isColumn ? padding.left : padding.top;
            float currentCrossPos = startPaddingCross;
            float lineSpacing = isColumn ? spacing.x : spacing.y;

            foreach (var line in linesList)
            {
                float startOffset = startPaddingMain;
                float freeSpace = containerSizeMain - (isColumn ? padding.vertical : padding.horizontal) - line.MainSize;
                if (freeSpace < 0) freeSpace = 0;

                float extraSpacing = 0;
                float expansionPerChild = 0;

                // Force Expand Main Axis
                if (forceExpandMainAxis && line.Count > 0)
                {
                    expansionPerChild = freeSpace / line.Count;
                }
                else
                {
                    switch (justifyContent)
                    {
                        case JustifyContent.Center: startOffset += freeSpace * 0.5f; break;
                        case JustifyContent.FlexEnd: startOffset += freeSpace; break;
                        case JustifyContent.SpaceBetween: if (line.Count > 1) extraSpacing = freeSpace / (line.Count - 1); break;
                        case JustifyContent.SpaceAround: if (line.Count > 0) { extraSpacing = freeSpace / line.Count; startOffset += extraSpacing * 0.5f; } break;
                        case JustifyContent.SpaceEvenly: if (line.Count > 0) { extraSpacing = freeSpace / (line.Count + 1); startOffset += extraSpacing; } break;
                    }
                }

                float itemMainSpacing = isColumn ? spacing.y : spacing.x;

                for (int i = 0; i < line.Count; i++)
                {
                    int indexOffset = i;
                    if (direction == FlexDirection.RowReverse || direction == FlexDirection.ColumnReverse)
                        indexOffset = line.Count - 1 - i;

                    int childIndex = line.StartIndex + indexOffset;
                    if (childIndex >= activeChildren.Count) continue;

                    RectTransform child = activeChildren[childIndex];

                    if (axis == mainAxisIndex)
                    {
                        float childSize = LayoutUtility.GetPreferredSize(child, mainAxisIndex);
                        
                        // Apply expansion
                        if (forceExpandMainAxis) childSize += expansionPerChild;

                        SetChildAlongAxis(child, mainAxisIndex, startOffset, childSize);
                        
                        float space = (i < line.Count - 1) ? itemMainSpacing : 0;
                        startOffset += childSize + space + extraSpacing;
                    }
                    else
                    {
                        float childCrossSize = LayoutUtility.GetPreferredSize(child, crossAxisIndex);
                        float crossOffset = currentCrossPos;
                        
                        bool performStretch = alignItems == AlignItems.Stretch || forceExpandCrossAxis;

                        if (performStretch)
                        {
                            childCrossSize = line.CrossSize;
                        }
                        else
                        {
                            switch (alignItems)
                            {
                                case AlignItems.Center: crossOffset += (line.CrossSize - childCrossSize) * 0.5f; break;
                                case AlignItems.FlexEnd: crossOffset += line.CrossSize - childCrossSize; break;
                            }
                        }

                        SetChildAlongAxis(child, crossAxisIndex, crossOffset, childCrossSize);
                    }
                }

                if (axis == crossAxisIndex)
                {
                    currentCrossPos += line.CrossSize + lineSpacing;
                }
            }
        }
    }
}