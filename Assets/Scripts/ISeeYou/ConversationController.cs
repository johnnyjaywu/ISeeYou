using System.Collections.Generic;
using ContentContent;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ISeeYou
{
    public class ConversationController : MonoBehaviour
    {
        [SerializeField] private InputActionReference continueInput;

        [SerializeField] private List<DialogLine> lines = new();

        [ReadOnly, SerializeField] private DialogLine currentDialogLine;

        private int currentLineIndex = -1;
        private InputAction inputAction;

        private void Awake()
        {
            inputAction = continueInput.action;
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        public void EnableInput()
        {
            if (inputAction != null)
            {
                inputAction.Enable();
                inputAction.performed += OnInputPerformed;
            }
        }

        public void DisableInput()
        {
            if (inputAction != null)
            {
                inputAction.Disable();
                inputAction.performed -= OnInputPerformed;
            }
        }

        private void OnInputPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || !context.ReadValueAsButton()) return;
            PlayNextLine();
        }

        [Button]
        public void PlayNextLine()
        {
            int nextIndex = currentLineIndex + 1;

            if (!lines.IsNullOrEmpty() && nextIndex < lines.Count)
            {
                PlayLine(nextIndex);
            }
        }

        public void PlayLine(int index)
        {
            StopCurrentLine();

            // Sync internal state so PlayNextLine() works relative to this one
            currentLineIndex = index;
            currentDialogLine = lines[index];

            // Spawn Text
            currentDialogLine.Speak();
            currentDialogLine.triggerEvent?.Invoke();
        }

        /// <summary>
        /// Stops audio and clears all visual text immediately.
        /// </summary>
        public void StopCurrentLine()
        {
            currentDialogLine.Stop();
        }
    }


    // Extension method helper for cleaner index checking
    public static class CollectionExtensions
    {
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return list != null && index >= 0 && index < list.Count;
        }
    }
}