using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ContentContent.UI
{
    [RequireComponent(typeof(Button))]
    public class LoadSceneButton : MonoBehaviour
    {
        [Scene]
        [SerializeField]
        private string scene;

        [SerializeField]
        private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        private Button button;

        private void OnEnable()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(LoadScene);
        }

        private void OnDisable()
        {
            if (button) button.onClick.RemoveListener(LoadScene);
        }

        private void LoadScene()
        {
            if (!string.IsNullOrEmpty(scene))
                SceneManager.LoadScene(scene, loadSceneMode);
            else
                Debug.LogError("Scene name is empty. Please assign a valid scene name.");
        }
    }
}