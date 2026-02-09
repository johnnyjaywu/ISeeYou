using UnityEngine;

namespace ContentContent.Dialog
{
    [RequireComponent(typeof(Collider2D))]
    public class PopupTrigger2D : MonoBehaviour
    {
        [TextArea]
        [SerializeField] private string text = "";

        [Tooltip("Who can trigger?")]
        [SerializeField] private LayerMask triggerLayers;

        [SerializeField] private bool showOnAwake;

        [Tooltip("How long to wait after the text is done showing before closing? 0 is infinite")]
        [Min(0)]
        [SerializeField] private float waitToCloseDuration;

        [Tooltip("Close the popup when target exits the collider?")]
        [SerializeField] private bool closeOnExit;

        [Tooltip("Disable the trigger after dialog is done?")]
        [SerializeField] private bool disableOnClose = true;

        private Collider2D col;
        private DialogLinePresenter currentPresenter;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            if (showOnAwake) Show();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.IsInLayerMask(triggerLayers)) return;
            Show();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.IsInLayerMask(triggerLayers)) return;
            if (closeOnExit) Close();
        }

        public void Show()
        {
            if (currentPresenter) return;
            currentPresenter =
                DialogManager.Instance.ShowPopup(new DialogLine(text), transform, Vector3.zero, waitToCloseDuration);
            currentPresenter.OnPresentingFinished += HandleDialogFinished;
        }

        public void Close()
        {
            if (!currentPresenter) return;
            currentPresenter.Stop();
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