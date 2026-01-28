using System.Collections.Generic;
using ContentContent.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentContent.Dialog
{
    [CreateAssetMenu(fileName = "DialogSettings", menuName = "ContentContent/Dialog/Settings")]
    public class DialogSettings : ScriptableSettings<DialogSettings>
    {
        [SerializeField] private InputActionReference skipAction;

        [SerializeField] private InputActionReference continueAction;

        [Tooltip("The default prefab used when spawning World Space popups.")]
        [SerializeField] private PopupDialogView defaultPrefab;

        [Tooltip("Scale for the World Space Canvas (usually 0.01).")]
        [SerializeField] private float worldCanvasScale = 0.01f;

        [SerializeField] private List<SpeakerProfile> speakerProfiles;

        [SerializeField] private List<LibraryEntry<SoundData>> voiceLines;

        public static InputAction Skip => Instance.skipAction.action;
        public static InputAction Continue => Instance.continueAction.action;
        public static PopupDialogView DefaultPrefab => Instance.defaultPrefab;
        public static float WorldCanvasScale => Instance.worldCanvasScale;


        public static SpeakerProfile GetProfile(string id)
        {
            return Instance.speakerProfiles.Find(profile => profile.speakerID == id);
        }

        public static SoundData GetVoiceLine(string id)
        {
            if (string.IsNullOrEmpty(id) || Instance.voiceLines == null) return null;

            return Instance.voiceLines.Find(entry => entry.id == id).value;
        }
    }
}