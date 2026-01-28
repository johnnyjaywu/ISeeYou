using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ContentContent.Dialog
{
    public class DialogManager : SingletonBehaviour<DialogManager>
    {
        private readonly Dictionary<string, PopupDialogView> popupRegistry = new();
        private readonly Dictionary<string, DialogView> staticViewRegistry = new();
        private DialogLinePresenter activeDialogPresenter;

        private CoroutineHandle activeDialogRoutine;
        private bool uiContinueTriggered;
        private bool uiSkipTriggered;
        private Canvas worldCanvas;

        public bool IsDialogActive => activeDialogRoutine is { IsRunning: true };

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public event Action DialogStarted;
        public event Action DialogFinished;

        public DialogLinePresenter StartDialog(Dialog dialog, string viewID = "", bool waitForInput = true,
            Action onFinished = null)
        {
            if (IsDialogActive)
                Debug.LogWarning(
                    $"Trying to start a new dialog {dialog} while a previous dialog is still active. Ignoring");
            else
                activeDialogRoutine = PlayDialog(dialog, viewID, waitForInput, onFinished).Run();

            return activeDialogPresenter;
        }

        public DialogLinePresenter StartDialog(List<DialogLine> lines, string viewID = "", bool waitForInput = true,
            Action onFinished = null)
        {
            return StartDialog(new Dialog().Add(lines), viewID, waitForInput, onFinished);
        }

        public DialogLinePresenter StartDialog(DialogLine line, string viewID = "", bool waitForInput = true,
            Action onFinished = null)
        {
            return StartDialog(new Dialog().Add(line), viewID, waitForInput, onFinished);
        }

        public DialogLinePresenter StartDialog(string text, string speakerID = "", string viewID = "",
            bool autoContinue = true,
            Action onFinished = null)
        {
            return StartDialog(new Dialog().Add(text, speakerID), viewID, autoContinue, onFinished);
        }

        public void StopDialog()
        {
            if (activeDialogRoutine != null)
            {
                activeDialogRoutine.Stop();
                activeDialogRoutine = null;
            }

            if (activeDialogPresenter != null)
            {
                activeDialogPresenter.Stop();
                activeDialogPresenter = null;
            }

            // Cleanup flags to prevent "ghost clicks" on next dialog
            uiSkipTriggered = false;
            uiContinueTriggered = false;

            DialogFinished?.Invoke();
        }

        private IEnumerator WaitToClose(DialogLinePresenter presenter, float duration, Action onClosed)
        {
            if (duration <= 0) yield break;

            yield return new WaitForSeconds(duration);
            presenter.Stop(onClosed: onClosed);
        }

        public DialogLinePresenter ShowPopup(DialogLine line, Transform target, Vector3 offset,
            float waitToCloseDuration, Action onClosed,
            PopupDialogView overridePrefab)
        {
            PopupDialogView prefab = overridePrefab ?? DialogSettings.DefaultPrefab;
            PopupDialogView popupDialogView = prefab.Spawn(Vector3.zero);
            popupDialogView.transform.SetParent(worldCanvas.transform, false);
            popupDialogView.Track(target, offset);
            popupDialogView.Presenter.Present(line, true,
                () => WaitToClose(popupDialogView.Presenter, waitToCloseDuration, onClosed).Run());
            return popupDialogView.Presenter;
        }

        public DialogLinePresenter ShowPopup(DialogLine line, Transform target = null,
            Vector3 offset = default, float waitToCloseDuration = 3, Action onClosed = null, string viewID = "")
        {
            return ShowPopup(line, target, offset, waitToCloseDuration, onClosed,
                popupRegistry.GetValueOrDefault(viewID));
        }
        
        public DialogLinePresenter ShowPopup(string line, Transform target = null,
            Vector3 offset = default, float waitToCloseDuration = 3, Action onClosed = null, string viewID = "")
        {
            return ShowPopup(new DialogLine(line), target, offset, waitToCloseDuration, onClosed,
                popupRegistry.GetValueOrDefault(viewID));
        }

        #region Registrations

        public void RegisterView(string id, DialogView view)
        {
            if (id.IsNullOrEmpty())
                id = "default";

            // Check if it's a popup
            if (view is PopupDialogView dialog)
                popupRegistry.TryAdd(id, dialog);
            else
                staticViewRegistry.TryAdd(id, view);
        }

        public void UnregisterView(string id, DialogView view)
        {
            if (id.IsNullOrEmpty())
                id = "default";

            // Check if it's a popup
            if (view is PopupDialogView)
                popupRegistry.Remove(id);
            else
                staticViewRegistry.Remove(id);
        }

        public void RegisterSkipButton(Button button)
        {
            if (button == null) return;
            button.onClick.AddListener(OnSkipButtonClicked);
        }

        public void UnregisterSkipButton(Button button)
        {
            if (button != null) button.onClick.RemoveListener(OnSkipButtonClicked);
        }

        public void RegisterContinueButton(Button button)
        {
            if (button == null) return;
            button.onClick.AddListener(OnContinueButtonClicked);
        }

        public void UnregisterContinueButton(Button button)
        {
            if (button != null) button.onClick.RemoveListener(OnContinueButtonClicked);
        }

        #endregion

        #region Helpers and Callbacks

        private void Initialize()
        {
            // Create the World Canvas for popups
            worldCanvas = new GameObject("PopupCanvas").AddComponent<Canvas>();
            worldCanvas.transform.SetParent(transform);
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.transform.localScale = Vector3.one * DialogSettings.WorldCanvasScale;
        }

        private IEnumerator PlayDialog(Dialog dialog, string viewID = "", bool waitForInput = true,
            Action onFinished = null)
        {
            if (waitForInput)
            {
                DialogSettings.Skip.Enable();
                DialogSettings.Continue.Enable();
            }

            uiSkipTriggered = false;
            uiContinueTriggered = false;

            DialogStarted?.Invoke();

            for (var i = 0; i < dialog.lines.Count; i++)
            {
                DialogLine line = dialog.lines[i];
                ShowLine(line, viewID, i == 0, waitForInput);
                if (activeDialogPresenter == null) yield break;
                yield return waitForInput
                    ? activeDialogPresenter.WaitForInput(GetSkipSignal, GetContinueSignal)
                    : activeDialogPresenter.WaitAutoContinue();
            }

            DialogSettings.Skip.Disable();
            DialogSettings.Continue.Disable();

            StopDialog();
            onFinished?.Invoke();
        }

        private IEnumerator WaitToClose(DialogLinePresenter presenter, float duration)
        {
            yield return new WaitForSeconds(duration);
            presenter.Stop();
        }

        private void ShowLine(DialogLine line, string viewID = "", bool animateOpen = true, bool waitForInput = true)
        {
            string targetView = viewID.IsNullOrEmpty() ? "default" : viewID;
            if (staticViewRegistry.TryGetValue(targetView, out DialogView view))
            {
                activeDialogPresenter = view.Presenter;
                activeDialogPresenter.WaitForInput = waitForInput;
                activeDialogPresenter.Present(line, animateOpen);
            }
            else
            {
                Debug.LogWarning(viewID.IsNullOrEmpty()
                    ? "[DialogManager] Logic fell back to 'default' view, but no DialogView with viewID='default' was found in the scene."
                    : $"[DialogManager] Could not find View with ID '{viewID}'.");
            }
        }

        private void OnSkipButtonClicked()
        {
            uiSkipTriggered = true;
        }

        private void OnContinueButtonClicked()
        {
            uiContinueTriggered = true;
        }

        private bool GetSkipSignal()
        {
            // Check Input System
            bool inputAction = DialogSettings.Skip != null && DialogSettings.Skip.WasPressedThisFrame();

            // Check UI (and consume the flag)
            bool uiAction = uiSkipTriggered;
            uiSkipTriggered = false;

            return inputAction || uiAction;
        }

        private bool GetContinueSignal()
        {
            // Check Input System
            bool inputAction = DialogSettings.Continue != null && DialogSettings.Continue.WasPressedThisFrame();

            // Check UI (and consume the flag)
            bool uiAction = uiContinueTriggered;
            uiContinueTriggered = false;

            return inputAction || uiAction;
        }

        #endregion
    }
}