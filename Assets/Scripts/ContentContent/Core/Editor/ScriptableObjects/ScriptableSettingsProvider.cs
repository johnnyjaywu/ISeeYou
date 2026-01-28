using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Editor
{
    public static class ScriptableSettingsProvider
    {
        // This method is called by Unity to fetch all settings pages
        [SettingsProviderGroup]
        private static SettingsProvider[] CreateSettingProviders()
        {
            List<SettingsProvider> providers = new();

            // 1. Find all types in the assembly that inherit from ScriptableSettings<>
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(typeof(ScriptableSettings<>));

            foreach (Type type in types)
            {
                // Skip abstract classes (base classes)
                if (type.IsAbstract) continue;

                // 2. Find the actual asset file for this type
                // We use the same logic as the Registrar to find the one loaded in memory/project
                string guid = AssetDatabase.FindAssets($"t:{type.Name}").FirstOrDefault();

                if (string.IsNullOrEmpty(guid)) continue; // No asset created yet, skip UI

                string path = AssetDatabase.GUIDToAssetPath(guid);
                var settingsAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                // 3. Create the Provider
                // "Project/Game Settings/TypeName" defines the tree structure in the window
                var provider = new SettingsProvider($"Project/Game Settings/{type.Name}", SettingsScope.Project)
                {
                    label = ObjectNames.NicifyVariableName(type.Name), // e.g. "AudioSettings" -> "Audio Settings"

                    // This draws the UI
                    guiHandler = _ =>
                    {
                        // Draw the standard Inspector for the asset
                        var editor = UnityEditor.Editor.CreateEditor(settingsAsset);
                        editor.OnInspectorGUI();
                    },

                    // Search keywords (allows you to search "Volume" in Project Settings to find this page)
                    keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(
                        new SerializedObject(settingsAsset))
                };

                providers.Add(provider);
            }

            return providers.ToArray();
        }
    }
}