using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContentContent.Editor
{
    [CustomEditor(typeof(ScriptableSettings), true)]
    public class ScriptableSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 1. Determine Status
            AssetStatus status =
                GetStatus(target as ScriptableSettings, out string message, out Object activeAsset);

            // 2. Draw Status Box
            DrawStatusBox(status, message, activeAsset);

            // 3. Draw Default Inspector (The actual fields of your object)
            base.OnInspectorGUI();
        }

        private AssetStatus GetStatus(ScriptableSettings currentAsset, out string message,
            out Object activeAsset)
        {
            List<Object> preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            activeAsset = null;

            // Check if this specific asset is in the list
            if (preloadedAssets.Contains(currentAsset))
            {
                message = "ACTIVE SETTINGS\nThis settings is registered to load automatically at startup.";
                return AssetStatus.Active;
            }

            // Check if another asset of the same type is in the list
            Type type = currentAsset.GetType();
            Object existing = preloadedAssets.FirstOrDefault(x => x != null && x.GetType() == type);

            if (existing != null)
            {
                activeAsset = existing;
                message =
                    $"IGNORED (DUPLICATE)\nAnother settings '{existing.name}' is already the registered settings.";
                return AssetStatus.Duplicate;
            }

            message = "NOT REGISTERED\nThis settings is not in the Preloaded Assets list.";
            return AssetStatus.Unregistered;
        }

        private void DrawStatusBox(AssetStatus status, string message, Object activeAsset)
        {
            EditorGUILayout.Space(5);

            switch (status)
            {
                case AssetStatus.Active:
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                    break;

                case AssetStatus.Duplicate:
                    GUI.backgroundColor = Color.softRed;
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.BeginHorizontal();

                    // BUTTON 1: Swap Reference
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button($"Replace '{activeAsset.name}' with This", GUILayout.Height(30)))
                        if (EditorUtility.DisplayDialog("Replace Active Settings?",
                                $"Are you sure you want to make '{target.name}' the active settings?\n\nThis will remove '{activeAsset.name}' from the build startup list.",
                                "Yes, Replace", "Cancel"))
                            SwapActiveSettings(activeAsset, target);

                    GUI.backgroundColor = Color.white;

                    // BUTTON 2: Copy Values (Helper for workflows)
                    if (GUILayout.Button("Copy Values from Active", GUILayout.Height(30)))
                        if (EditorUtility.DisplayDialog("Overwrite Values?",
                                $"This will overwrite all fields in '{target.name}' with values from '{activeAsset.name}'.\n\nUndo is supported.",
                                "Overwrite", "Cancel"))
                            CopyValuesFrom(activeAsset, target);

                    EditorGUILayout.EndHorizontal();

                    // Helper to find the other one
                    if (GUILayout.Button($"Select Active Asset ({activeAsset.name})"))
                    {
                        Selection.activeObject = activeAsset;
                        EditorGUIUtility.PingObject(activeAsset);
                    }

                    break;

                case AssetStatus.Unregistered:
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                    if (GUILayout.Button("Register Now")) RegisterAsset(target);

                    break;
            }

            EditorGUILayout.Space(10);
        }

        private void SwapActiveSettings(Object oldAsset, Object newAsset)
        {
            // List<Object> list = PlayerSettings.GetPreloadedAssets().ToList();
            // list.TryRemove(oldAsset);
            // list.TryAdd(newAsset);
            // PlayerSettings.SetPreloadedAssets(list.ToArray());
            // AssetDatabase.SaveAssets();
            // AssetDatabase.RefreshSettings();
            ModifyPreloadedAssets(list =>
            {
                if (oldAsset != null) list.Remove(oldAsset);
                if (!list.Contains(newAsset)) list.Add(newAsset);
            });

            // Force the inspector to repaint immediately after registration
            GUIUtility.ExitGUI();
            Debug.Log($"[ScriptableSettings] Swapped active settings from {oldAsset.name} to {newAsset.name}");
        }

        private void RegisterAsset(Object asset)
        {
            // var list = PlayerSettings.GetPreloadedAssets().ToList();
            // list.Add(asset);
            // PlayerSettings.SetPreloadedAssets(list.ToArray());
            // AssetDatabase.SaveAssets();
            // AssetDatabase.RefreshSettings();
            ModifyPreloadedAssets(list =>
            {
                if (!list.Contains(asset)) list.Add(asset);
            });

            // Force the inspector to repaint immediately after registration
            GUIUtility.ExitGUI();
        }

        // Helper method to handle the SerializedObject logic safely
        private void ModifyPreloadedAssets(Action<List<Object>> modificationAction)
        {
            // 1. Load the actual PlayerSettings object from the AssetDatabase
            Object playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
                .FirstOrDefault();

            if (playerSettings == null)
            {
                Debug.LogError("Could not load PlayerSettings.");
                return;
            }

            // 2. Wrap it in a SerializedObject (This is the secret to updating the UI)
            var so = new SerializedObject(playerSettings);
            SerializedProperty preloadedAssetsProp = so.FindProperty("preloadedAssets");

            // 3. Read current list
            List<Object> currentList = new();
            for (var i = 0; i < preloadedAssetsProp.arraySize; i++)
                currentList.Add(preloadedAssetsProp.GetArrayElementAtIndex(i).objectReferenceValue);

            // 4. Modify list using your custom logic
            modificationAction(currentList);

            // 5. Write back to SerializedProperty
            preloadedAssetsProp.ClearArray();
            for (var i = 0; i < currentList.Count; i++)
            {
                preloadedAssetsProp.InsertArrayElementAtIndex(i);
                preloadedAssetsProp.GetArrayElementAtIndex(i).objectReferenceValue = currentList[i];
            }

            // 6. Apply changes (This triggers the UI refresh automatically)
            so.ApplyModifiedProperties();

            // Optional: Force a global repaint just to be absolutely sure
            InternalEditorUtility.RepaintAllViews();
        }

        private void CopyValuesFrom(Object source, Object destination)
        {
            Undo.RecordObject(destination, "Copy Settings Values");
            EditorUtility.CopySerialized(source, destination);

            // Warning: CopySerialized overwrites the "m_Name" (internal asset name) in some older Unity versions, 
            // but generally safe for ScriptableObjects data fields.
            // We force a dirty state to ensure it saves.
            EditorUtility.SetDirty(destination);
            Debug.Log($"Copied values from {source.name} to {destination.name}");
        }

        private enum AssetStatus
        {
            Active,
            Duplicate,
            Unregistered
        }
    }
}