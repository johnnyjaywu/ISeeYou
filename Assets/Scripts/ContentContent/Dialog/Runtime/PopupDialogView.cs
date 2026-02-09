using System;
using ContentContent.UI;
using PrimeTween;
using UnityEngine;

namespace ContentContent.Dialog
{
    [RequireComponent(typeof(WorldToScreenTracker))]
    public class PopupDialogView : DialogView
    {
        private WorldToScreenTracker tracker;

        protected override void Awake()
        {
            base.Awake();
            tracker = GetComponent<WorldToScreenTracker>();
        }

        protected override void OnSpawn()
        {
            tracker.Initialize(Camera.main);
            tracker.SetIndicatorActive(false);
        }

        protected override void OnDespawn()
        {
            // Stop tracking to prevent logic running while in pool
            tracker.Track(null);
        }

        public void Track(Transform target, Vector3 offset)
        {
            tracker.offset = offset;
            tracker.Track(target);
        }

        public override Sequence Close(bool animateClose = true, Action onClosed = null)
        {
            void ClosedCallback()
            {
                onClosed?.Invoke();
                this.Despawn();
            }

            Sequence closeSequence = base.Close(animateClose, ClosedCallback);
            return closeSequence;
        }
    }
}