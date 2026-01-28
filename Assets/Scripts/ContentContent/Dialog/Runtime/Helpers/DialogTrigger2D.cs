using NaughtyAttributes;
using UnityEngine;

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

        // [Tooltip("How long to wait after the text is done showing before closing? 0 is infinite")]
        // [SerializeField] private float waitToCloseDuration = 0;

        // [Tooltip("Close the popup when target exits the collider?")]
        // [SerializeField] private bool closeOnExit;

        private Collider2D col;

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
            DialogManager.Instance.StartDialog(dialog, viewID);
        }

        public void Close()
        {
            DialogManager.Instance.StopDialog();
        }
    }
}