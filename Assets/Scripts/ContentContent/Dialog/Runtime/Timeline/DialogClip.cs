using System;
using ContentContent.Audio;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ContentContent.Dialog
{
    [Serializable]
    public class DialogClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Content")]
        [TextArea(3, 5)] public string text = "New Dialog Line";

        public string viewID;
        public string speakerID;
        public string portraitID;
        public string voiceLineID;

        [Header("Targeting")]
        public ExposedReference<Transform> barkTarget;

        [Header("Timing")]
        public bool useClipDuration = true;

        [Header("Info")]
        [Tooltip("The recommended duration for this clip based on text length and typing speed.")]
        [SerializeField] [ReadOnly] private double estimatedDuration;

        public override double duration => estimatedDuration;


        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<DialogPlayableBehaviour> playable = ScriptPlayable<DialogPlayableBehaviour>.Create(graph);
            DialogPlayableBehaviour behaviour = playable.GetBehaviour();

            behaviour.text = text;
            behaviour.speakerID = speakerID;
            behaviour.portraitID = portraitID;
            behaviour.voiceLineID = voiceLineID;
            behaviour.viewID = viewID;
            behaviour.useClipDuration = useClipDuration;
            behaviour.targetTransform = barkTarget.Resolve(graph.GetResolver());

            return playable;
        }

        private void OnValidate()
        {
            CalculateDuration();
        }

        public void CalculateDuration()
        {
            if (string.IsNullOrEmpty(text))
            {
                estimatedDuration = 1.0f;
                return;
            }

            // Typing Time
            float typingTime = text.Length * 0.05f;

            // Audio Time
            SoundData voiceLine = DialogSettings.GetVoiceLine(voiceLineID);
            float audioTime = voiceLine != null ? voiceLine.GetDurationRange().y : 0;

            // Take the longer of the two and pad it by 1 second
            estimatedDuration = Mathf.Max(typingTime, audioTime) + 1f;
        }
    }
}