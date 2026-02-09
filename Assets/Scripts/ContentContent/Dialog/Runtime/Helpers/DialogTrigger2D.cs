using NaughtyAttributes;
using UnityEngine;
using UnityEngine.XR;

namespace ContentContent.Dialog
{
    [RequireComponent(typeof(Collider2D))]
    public class DialogTrigger2D : MonoBehaviour
    {
        [SerializeField] private bool useDialogAsset;

        [ShowIf("useDialogAsset")]
        [SerializeField] private TextAsset dialogAsset;

        [HideIf("useDialogAsset")]
        [SerializeField] private Dialog dialog;

        [SerializeField] private string viewID = "";

        [Tooltip("Who can trigger?")]
        [SerializeField] private LayerMask triggerLayers;

        [SerializeField] private bool showOnAwake;

        [Tooltip("Disable the trigger after dialog is done?")]
        [SerializeField] private bool disableOnClose = true;

        // [Tooltip("How long to wait after the text is done showing before closing? 0 is infinite")]
        // [SerializeField] private float waitToCloseDuration = 0;

        // [Tooltip("Close the popup when target exits the collider?")]
        // [SerializeField] private bool closeOnExit;

        private Collider2D col;
        private DialogLinePresenter currentPresenter;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            if (useDialogAsset) dialog = DialogBuilder.Create().Parse(dialogAsset.text);

            if (showOnAwake) Show();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.IsInLayerMask(triggerLayers)) return;
            Show();
        }

        // private void OnTriggerExit2D(Collider2D other)
        // {
        //     if (!other.gameObject.IsInLayerMask(triggerLayers)) return;
        //     if (closeOnExit) Close();
        // }

        public void Show()
        {
            if (DialogManager.Instance.IsDialogActive) return;
            currentPresenter = DialogManager.Instance.StartDialog(dialog, viewID);
            currentPresenter.OnPresentingFinished += HandleDialogFinished;
        }

        public void Close()
        {
            DialogManager.Instance.StopDialog();
            HandleDialogFinished(currentPresenter);
        }

        private void HandleDialogFinished(DialogLinePresenter presenter)
        {
            if (currentPresenter != presenter)
            {
                // SOMETHING WENT WRONG HERE
                Debug.LogError("Woah, this shouldn't happen");
                return;
            }

            currentPresenter.OnPresentingFinished -= HandleDialogFinished;
            currentPresenter = null;
            if (disableOnClose) gameObject.SetActive(false);
        }
    }
}