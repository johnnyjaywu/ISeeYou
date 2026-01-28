using UnityEditor;
using UnityEngine;

namespace ContentContent.Editor
{
    public static class SceneAssetContextMenu
    {
        [MenuItem("Assets/Add to Build Settings", true)]
        private static bool AddToBuildSettingsValidation()
        {
            return Selection.activeObject is SceneAsset;
        }

        [MenuItem("Assets/Add to Build Settings")]
        private static void AddToBuildSettings()
        {
            var sceneAsset = Selection.activeObject as SceneAsset;
            if (sceneAsset == null) return;

            string path = AssetDatabase.GetAssetPath(sceneAsset);
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            foreach (EditorBuildSettingsScene scene in scenes)
                if (scene.path == path)
                {
                    Debug.LogWarning("Scene is already in Build Settings.");
                    return;
                }

            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = newScenes;

            Debug.Log("Scene added to Build Settings.");
        }
    }
}