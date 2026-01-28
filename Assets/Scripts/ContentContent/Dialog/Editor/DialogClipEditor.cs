using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace ContentContent.Dialog.Editor
{
    [CustomEditor(typeof(DialogClip))]
    public class DialogClipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector (Text fields, etc)
            base.OnInspectorGUI();

            DialogClip clip = (DialogClip)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Resize to Fit Content"))
            {
                // Recalculate based on current text
                clip.CalculateDuration();

                // We need to find the TimelineClip wrapper to actually resize it in the UI
                UpdateTimelineClipDuration(clip);
            }
        }

        private void UpdateTimelineClipDuration(DialogClip asset)
        {
            // This is a bit of Editor magic to find the Clip wrapper for this Asset
            // because PlayableAsset doesn't know about its TimelineClip parent.

            // 1. Get the current Timeline Director
            var director = UnityEditor.Timeline.TimelineEditor.inspectedDirector;
            if (director == null) return;

            var timelineAsset = director.playableAsset as TimelineAsset;
            if (timelineAsset == null) return;

            // 2. Search all tracks to find the clip that references our Asset
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset == asset)
                    {
                        // 3. Apply the duration
                        Undo.RecordObject(timelineAsset, "Resize Dialog Clip");
                        clip.duration = asset.duration;

                        // Force repaint
                        UnityEditor.Timeline.TimelineEditor.Refresh(
                            UnityEditor.Timeline.RefreshReason.WindowNeedsRedraw);
                        return;
                    }
                }
            }
        }
    }
}