using UnityEngine;
using UnityEngine.Playables;

namespace ContentContent.Dialog
{
    public class DialogPlayableBehaviour : PlayableBehaviour
    {
        public string text;
        public string speakerID;
        public string portraitID;
        public string voiceLineID;
        public string viewID;
        public Transform targetTransform;
        public bool useClipDuration;

        private DialogLinePresenter activePresenter;
        private bool hasTriggered;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Trigger only once when the clip starts
            if (!hasTriggered && info.weight > 0f && Application.isPlaying)
            {
                TriggerDialog();
                hasTriggered = true;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // If the clip finishes (or timeline stops), cleanup.
            if (hasTriggered)
            {
                if (useClipDuration && activePresenter != null)
                {
                    // If we controlled duration, force stop now
                    activePresenter.Stop();
                }

                hasTriggered = false; // Reset for looping
                activePresenter = null;
            }
        }

        private void TriggerDialog()
        {
            // 1. Construct Line
            var line = new DialogLine(text, speakerID, portraitID, voiceLineID);

            if (useClipDuration)
            {
                // Disable auto-advance so it stays open until OnBehaviourPause calls Stop()
                // line.autoAdvance = false;
            }

            // Decide: World Space Popup or Screen Space Overlay?
            if (targetTransform != null)
            {
                // -- POPUP MODE --
                // We pass duration = 0 so it stays open indefinitely (until OnBehaviourPause closes it)
                activePresenter = DialogManager.Instance.ShowPopup(line, targetTransform, Vector3.zero,
                    0, null, viewID);
            }
            else
            {
                // -- STATIC VIEW MODE --
                // waitForInput: false, because Timeline controls the timing, not the player clicking "Continue"
                activePresenter = DialogManager.Instance.StartDialog(line, viewID, waitForInput: false);
            }
        }
    }
}