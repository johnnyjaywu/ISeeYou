using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.UI
{
    /// <summary>
    /// Responsibility: Provides a CSS Flexbox-style layout system for UGUI.
    /// 
    /// <para><b>Architecture:</b></para>
    /// <list type="bullet">
    /// <item><b>Axis Abstraction:</b> Abstracts logic into Main Axis (Flow) and Cross Axis (Perpendicular) to support both Row and Column layouts with a single logic path.</item>
    /// <item><b>Two-Pass Resolution:</b> Calculates line wrapping during the Input Calculation pass, then commits positions during the Set Layout pass.</item>
    /// <item><b>Allocation Free:</b> Uses a pooled struct-based line system to eliminate GC allocations during frequent layout rebuilds (e.g., dragging items).</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Layout/Flex Layout Group")]
    public class FlexLayoutGroup : LayoutGroup
    {
        // -------------------------------------------------------------------
        // Definitions
        // -------------------------------------------------------------------

        public enum FlexDirection { Row, Column, RowReverse, ColumnReverse }
        public enum FlexWrap { NoWrap, Wrap }
        public enum JustifyContent { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly }
        public enum AlignItems { FlexStart, Center, FlexEnd, Stretch }

        private struct FlexLine
        {
            public int StartIndex;
            public int Count;
            public float MainSize;
            public float CrossSize;
        }

        // -------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------

        [Header("Flex Settings")]
        [Tooltip("Direction items are laid out. Row = Horizontal, Column = Vertical.")]
        [SerializeField] private FlexDirection direction = FlexDirection.Row;
        
        [Tooltip("Controls wrapping behavior. 'NoWrap' forces single line, 'Wrap' allows multiple lines.")]
        [SerializeField] private FlexWrap wrap = FlexWrap.Wrap;
        
        [Tooltip("Alignment along the Main Axis (Horizontal for Row, Vertical for Column).")]
        [SerializeField] private JustifyContent justifyContent = JustifyContent.FlexStart;
        
        [Tooltip("Alignment along the Cross Axis (Vertical for Row, Horizontal for Column).")]
        [SerializeField] private AlignItems alignItems = AlignItems.Center;

        [Header("Spacing & Size")]
        [SerializeField] private Vector2 spacing = Vector2.zero;
        [SerializeField] private Vector2 minSize = Vector2.zero;
        [SerializeField] private Vector2 maxSize = Vector2.zero;

        public Vector2 MinSize
        {
            get => minSize;
            set => minSize = value;
        }
        
        [Header("Expansion")]
        [Tooltip("Force children to expand to fill empty space on the Main Axis.")]
        [SerializeField] private bool forceExpandMainAxis = false;
        
        [Tooltip("Force children to expand to fill empty space on the Cross Axis.")]
        [SerializeField] private bool forceExpandCrossAxis = false;

        // -------------------------------------------------------------------
        // State & Cache
        // -------------------------------------------------------------------

        // Optimization: List of structs avoids object allocation per line
        private readonly List<FlexLine> linesList = new List<FlexLine>();

        // Public Accessor for external logic (e.g. DropZones determining drag axis)
        public FlexDirection Direction => direction;

        // -------------------------------------------------------------------
        // LayoutGroup Overrides (Calculation Phase)
        // -------------------------------------------------------------------

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            
            // 1. Solve the flex logic (Wrapping, Line Sizes)
            // In UGUI, Horizontal is always called before Vertical. We do the heavy lifting here.
            CalculateChildrenLayout();

            // 2. Calculate Horizontal Bounds
            float totalWidth = padding.horizontal;
            bool isColumn = IsColumn();

            if (isColumn)
            {
                // Column: Width is determined by the widest column (Cross Axis)
                float maxCrossSize = 0;
                foreach (var line in linesList) 
                {
                    maxCrossSize = Mathf.Max(maxCrossSize, line.CrossSize);
                }
                totalWidth += maxCrossSize;
            }
            else
            {
                // Row: Width is determined by the longest row (Main Axis)
                float maxMainSize = 0;
                foreach (var line in linesList) 
                {
                    maxMainSize = Mathf.Max(maxMainSize, line.MainSize);
                }
                totalWidth += maxMainSize;
            }

            totalWidth = Mathf.Max(totalWidth, minSize.x);
            SetLayoutInputForAxis(totalWidth, totalWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            // Safety: If lines were not calculated in Horizontal pass (rare), calc them now.
            if (linesList.Count == 0) CalculateChildrenLayout();

            float totalHeight = padding.vertical;
            bool isColumn = IsColumn();

            if (isColumn)
            {
                // Column: Height is determined by the longest column (Main Axis)
                float maxMainSize = 0;
                foreach (var line in linesList) 
                {
                    maxMainSize = Mathf.Max(maxMainSize, line.MainSize);
                }
                totalHeight += maxMainSize;
            }
            else
            {
                // Row: Height is determined by sum of row heights (Cross Axis)
                float totalCrossSize = 0;
                foreach (var line in linesList) 
                {
                    totalCrossSize += line.CrossSize;
                }

                if (linesList.Count > 1) 
                {
                    totalHeight += (linesList.Count - 1) * spacing.y;
                }
                
                totalHeight += totalCrossSize;
            }

            totalHeight = Mathf.Max(totalHeight, minSize.y);
            SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
        }

        // -------------------------------------------------------------------
        // LayoutGroup Overrides (Application Phase)
        // -------------------------------------------------------------------

        public override void SetLayoutHorizontal() => SetChildrenAlongAxis(0);
        public override void SetLayoutVertical() => SetChildrenAlongAxis(1);

        // -------------------------------------------------------------------
        // Core Logic: Calculation
        // -------------------------------------------------------------------

        private void CalculateChildrenLayout()
        {
            linesList.Clear();
            
            // Note: 'rectChildren' automatically excludes inactive objects and those with ILayoutIgnorer.
            var activeChildren = rectChildren;
            bool isColumn = IsColumn();
            
            // Map Abstract Axes to Unity Axes: 0 = Horizontal (X), 1 = Vertical (Y)
            int mainAxis = isColumn ? 1 : 0; 
            int crossAxis = isColumn ? 0 : 1;

            // Determine Constraint Bounds
            float containerBoundary = isColumn ? rectTransform.rect.height : rectTransform.rect.width;
            float limit = isColumn ? maxSize.y : maxSize.x;
            
            float availableMainSize = (limit > 0) ? limit : containerBoundary;
            availableMainSize -= isColumn ? padding.vertical : padding.horizontal;

            // Infinite canvas handling (e.g. ScrollViews)
            if (availableMainSize <= 0) availableMainSize = float.MaxValue;

            // Line Calculation State
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

                // Wrapping Check
                bool requiresWrap = false;
                if (wrap != FlexWrap.NoWrap)
                {
                    if (currentMainSize + itemSizeWithSpacing > availableMainSize && i > startIndex)
                    {
                        requiresWrap = true;
                    }
                }

                if (requiresWrap)
                {
                    // Commit previous line
                    linesList.Add(new FlexLine
                    {
                        StartIndex = startIndex,
                        Count = i - startIndex,
                        MainSize = currentMainSize,
                        CrossSize = maxCrossSizeInLine
                    });

                    // Reset state for new line
                    startIndex = i;
                    currentMainSize = 0;
                    maxCrossSizeInLine = 0;
                    itemSizeWithSpacing = childMainSize;
                }

                currentMainSize += itemSizeWithSpacing;
                maxCrossSizeInLine = Mathf.Max(maxCrossSizeInLine, childCrossSize);
            }

            // Commit final line
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

        // -------------------------------------------------------------------
        // Core Logic: Positioning
        // -------------------------------------------------------------------

        private void SetChildrenAlongAxis(int axis)
        {
            var activeChildren = rectChildren;
            bool isColumn = IsColumn();
            int mainAxisIndex = isColumn ? 1 : 0;
            int crossAxisIndex = isColumn ? 0 : 1;

            // Only process valid axes
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

                // 1. Calculate Main Axis Positioning (Justification)
                if (forceExpandMainAxis && line.Count > 0)
                {
                    expansionPerChild = freeSpace / line.Count;
                }
                else
                {
                    switch (justifyContent)
                    {
                        case JustifyContent.Center: 
                            startOffset += freeSpace * 0.5f; 
                            break;
                        case JustifyContent.FlexEnd: 
                            startOffset += freeSpace; 
                            break;
                        case JustifyContent.SpaceBetween: 
                            if (line.Count > 1) extraSpacing = freeSpace / (line.Count - 1); 
                            break;
                        case JustifyContent.SpaceAround: 
                            if (line.Count > 0) 
                            { 
                                extraSpacing = freeSpace / line.Count; 
                                startOffset += extraSpacing * 0.5f; 
                            } 
                            break;
                        case JustifyContent.SpaceEvenly: 
                            if (line.Count > 0) 
                            { 
                                extraSpacing = freeSpace / (line.Count + 1); 
                                startOffset += extraSpacing; 
                            } 
                            break;
                    }
                }

                float itemMainSpacing = isColumn ? spacing.y : spacing.x;

                // 2. Iterate Children in Line
                for (int i = 0; i < line.Count; i++)
                {
                    // Support for Reverse Layouts (RowReverse / ColumnReverse)
                    int indexOffset = i;
                    if (direction == FlexDirection.RowReverse || direction == FlexDirection.ColumnReverse)
                        indexOffset = line.Count - 1 - i;

                    int childIndex = line.StartIndex + indexOffset;
                    if (childIndex >= activeChildren.Count) continue;

                    RectTransform child = activeChildren[childIndex];

                    if (axis == mainAxisIndex)
                    {
                        // Main Axis: Set position and advance offset
                        float childSize = LayoutUtility.GetPreferredSize(child, mainAxisIndex);
                        if (forceExpandMainAxis) childSize += expansionPerChild;

                        SetChildAlongAxis(child, mainAxisIndex, startOffset, childSize);
                        
                        float space = (i < line.Count - 1) ? itemMainSpacing : 0;
                        startOffset += childSize + space + extraSpacing;
                    }
                    else
                    {
                        // Cross Axis: Handle Alignment
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
                                case AlignItems.Center: 
                                    crossOffset += (line.CrossSize - childCrossSize) * 0.5f; 
                                    break;
                                case AlignItems.FlexEnd: 
                                    crossOffset += line.CrossSize - childCrossSize; 
                                    break;
                            }
                        }

                        SetChildAlongAxis(child, crossAxisIndex, crossOffset, childCrossSize);
                    }
                }

                // Advance Cross Axis position for the next line
                if (axis == crossAxisIndex)
                {
                    currentCrossPos += line.CrossSize + lineSpacing;
                }
            }
        }

        private bool IsColumn() => direction == FlexDirection.Column || direction == FlexDirection.ColumnReverse;
    }
}