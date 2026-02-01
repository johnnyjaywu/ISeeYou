using System.IO;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Audio.Editor
{
    public static class SoundDataCreator
    {
        // 1. The Menu Item
        // This adds the entry to the right-click "Create" menu and the top bar "Assets" menu.
        [MenuItem("Assets/Create/Audio/Sound Data from Clip", false, 50)]
        public static void CreateSoundData()
        {
            // Track created objects to highlight them afterwards
            var createdAssets = new System.Collections.Generic.List<Object>();

            foreach (Object obj in Selection.objects)
            {
                if (obj is AudioClip clip)
                {
                    SoundData newData = CreateAssetForClip(clip);
                    if (newData != null) createdAssets.Add(newData);
                }
            }

            // Quality of Life: Select the newly created assets in the Project view
            if (createdAssets.Count > 0)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.objects = createdAssets.ToArray();
            }
        }

        // 2. The Validator
        // This grays out the menu item if no AudioClip is selected, keeping the menu clean.
        [MenuItem("Assets/Create/Audio/Sound Data from Clip", true)]
        public static bool ValidateCreateSoundData()
        {
            foreach (Object obj in Selection.objects)
            {
                if (obj is AudioClip) return true;
            }
            return false;
        }

        private static SoundData CreateAssetForClip(AudioClip clip)
        {
            // Create the instance
            SoundData asset = ScriptableObject.CreateInstance<SoundData>();
            
            // Assign the clip immediately
            asset.clips = new AudioClip[] { clip };

            // Apply defaults from your existing ResetSettings logic to ensure consistency
            asset.ResetSettings(); 

            // Construct Path: Same folder as the clip, same name, but .asset extension
            string clipPath = AssetDatabase.GetAssetPath(clip);
            string directory = Path.GetDirectoryName(clipPath);
            string assetPath = Path.Combine(directory, $"{clip.name}.asset");

            // Safety: Ensure we don't overwrite an existing SoundData if one already exists
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            // Write to disk
            AssetDatabase.CreateAsset(asset, assetPath);
            
            return asset;
        }
    }
}