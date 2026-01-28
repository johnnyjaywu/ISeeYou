using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ContentContent.UI
{
    /// <summary>
    /// Manages the lifecycle and navigation of UI panels in the scene. 
    /// It allows for opening, closing, and switching between panels, maintaining a history stack for navigation.
    /// </summary>
    public class UIPanelManager : MonoBehaviour
    {
        [SerializeField] private UIPanel firstActivePanel;
        [SerializeField] private bool autoEnableFirstPanel = true;

        private readonly Stack<UIPanel> panelStack = new();
        private List<UIPanel> panels = new();

        private void Awake()
        {
            // Find all panels in the scene, including inactive ones.
            panels = FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            // Ensure all panels are initially disabled.
            panels.ForEach(p => p.gameObject.SetActive(false));

            if (!firstActivePanel && panels.Count > 0)
            {
                firstActivePanel = panels[0];
            }
        }

        private void OnEnable()
        {
            if (autoEnableFirstPanel && firstActivePanel)
            {
                OpenPanel(firstActivePanel);
            }
        }

        /// <summary>
        /// Opens a new panel. If there's an existing panel open, it will be disabled.
        /// </summary>
        /// <param name="panel">The panel to open.</param>
        public void OpenPanel(UIPanel panel)
        {
            if (panel == null)
            {
                Debug.LogWarning("Attempted to open a null panel.");
                return;
            }

            // Deactivate the current top panel if one exists.
            if (panelStack.Count > 0)
            {
                UIPanel currentPanel = panelStack.Peek();
                currentPanel.gameObject.SetActive(false);
            }

            // Activate the new panel and push it to the stack.
            panel.gameObject.SetActive(true);
            panelStack.Push(panel);
        }

        /// <summary>
        /// Closes the currently active panel and reveals the previous one.
        /// </summary>
        public void ClosePanel()
        {
            if (panelStack.Count == 0)
            {
                Debug.LogWarning("No panels to close.");
                return;
            }

            // Deactivate and pop the current panel.
            UIPanel lastPanel = panelStack.Pop();
            lastPanel.gameObject.SetActive(false);

            // If there's a panel left in the stack, reactivate it.
            if (panelStack.Count > 0)
            {
                UIPanel previousPanel = panelStack.Peek();
                previousPanel.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Closes the current active panel and opens a new one.
        /// This is a convenience method for switching between panels.
        /// </summary>
        /// <param name="nextPanel">The new panel to open.</param>
        public void SwitchToPanel(UIPanel nextPanel)
        {
            if (panelStack.Count > 0)
            {
                ClosePanel();
            }
            OpenPanel(nextPanel);
        }
    }
}