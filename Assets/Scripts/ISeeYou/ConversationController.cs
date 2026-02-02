using System;
using System.Collections.Generic;
using ContentContent;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ISeeYou
{
    public class ConversationController : MonoBehaviour
    {
        [SerializeField] private InputActionReference continueInput;

        [ReorderableList]
        [SerializeField] private List<DialogLine> lines = new();

        [ReadOnly, SerializeField] private DialogLine currentDialogLine;

        private int currentLineIndex = -1;
        private InputAction inputAction;

        private void Awake()
        {
            inputAction = continueInput.action;
            if (inputAction != null)
                inputAction.performed += OnInputPerformed;
        }

        private void OnDestroy()
        {
            if (inputAction != null)
                inputAction.performed -= OnInputPerformed;
        }

        private void OnEnable()
        {
            EnableInput();
            PlayNextLine();
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
            }
        }

        public void DisableInput()
        {
            if (inputAction != null)
            {
                inputAction.Disable();
            }
        }

        private void OnInputPerformed(InputAction.CallbackContext context)
        {
            // 1. Standard checks
            if (!context.performed || !context.ReadValueAsButton()) return;

            // 2. Stale Input Filter
            // Check if the physical button state actually CHANGED to pressed this frame.
            // If 'wasPressedThisFrame' is false, it means the user was already holding 
            // the button when input was enabled.
            if (context.control is ButtonControl btn && !btn.wasPressedThisFrame)
            {
                return;
            }
            
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