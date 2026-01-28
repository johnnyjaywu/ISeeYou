using UnityEditor;
using UnityEngine;

namespace ContentContent.Editor
{
    // % is Ctrl/Cmd, # is Shift, & is Alt.
    public static class Reimport
    {
        // F6 
        [MenuItem("File/Reimport Changed Assets _F6")]
        public static void ReimportChangedAssets()
        {
            // AssetDatabase.Refresh checks the filesystem for changes 
            // and imports any new or modified files.
            AssetDatabase.Refresh();
            Debug.Log("<color=cyan>[Unity]</color> Global Asset Refresh complete.");
        }

        // Ctrl + Shift + F6
        [MenuItem("File/Reimport Selected Folder %#F6")]
        public static void ReimportSelectedFolder()
        {
            // Get the currently selected object in the Project window
            Object selectedObject = Selection.activeObject;

            if (selectedObject == null)
            {
                Debug.LogWarning("No folder selected.");
                return;
            }

            // Convert the object to a file path
            string path = AssetDatabase.GetAssetPath(selectedObject);

            // Verify it is actually a folder
            if (AssetDatabase.IsValidFolder(path))
            {
                // ImportRecursive is critical here: 
                // It forces Unity to reprocess the folder AND all files inside it.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

                Debug.Log($"<color=green>[Unity]</color> Reimported folder: <b>{path}</b>");
            }
            else
            {
                Debug.LogWarning("The selected object is not a folder. Please select a folder to use this command.");
            }
        }
    }
}