using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContentContent.Editor
{
    public class SettingsPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom)
        {
            var changed = false;
            List<Object> preloaded = PlayerSettings.GetPreloadedAssets().ToList();

            if (preloaded.RemoveAll(x => x == null) > 0) changed = true;

            foreach (string path in imported)
            {
                if (!path.EndsWith(".asset")) continue;
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                if (IsScriptableSettings(asset.GetType()))
                {
                    Object existing = preloaded.FirstOrDefault(x => x != null && x.GetType() == asset.GetType());
                    if (existing == null)
                    {
                        preloaded.Add(asset);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                AssetDatabase.SaveAssets();
            }
        }

        public static bool IsScriptableSettings(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ScriptableSettings<>))
                    return true;
                type = type.BaseType;
            }

            return false;
        }
    }
}