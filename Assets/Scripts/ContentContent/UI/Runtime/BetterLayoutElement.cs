using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.UI
{
    public class BetterLayoutElement : LayoutElement
    {
        [SerializeField] private bool useMaxWidth;

        [ShowIf("useMaxWidth")]
        [SerializeField] private float maxWidth;

        [SerializeField] private bool useMaxHeight;

        [ShowIf("useMaxHeight")]
        [SerializeField] private float maxHeight;

        private readonly List<ILayoutElement> layoutComponents = new();

        public override float preferredWidth
        {
            get
            {
                float basePreferred = GetBasePreferredSize(0); // 0 = Width

                if (useMaxWidth) basePreferred = Mathf.Min(basePreferred, maxWidth);

                return Mathf.Max(basePreferred, minWidth);
            }
        }

        public override float preferredHeight
        {
            get
            {
                float basePreferred = GetBasePreferredSize(1); // 1 = Height

                if (useMaxHeight) basePreferred = Mathf.Min(basePreferred, maxHeight);

                return Mathf.Max(basePreferred, minHeight);
            }
        }

        private float GetBasePreferredSize(int axis)
        {
            GetComponents(layoutComponents);
            float largest = 0;
            foreach (ILayoutElement element in layoutComponents)
            {
                if (ReferenceEquals(element, this)) continue; // CRITICAL: Skip self
                float val = axis == 0 ? element.preferredWidth : element.preferredHeight;
                if (val > largest) largest = val;
            }

            return largest;
        }
    }
}