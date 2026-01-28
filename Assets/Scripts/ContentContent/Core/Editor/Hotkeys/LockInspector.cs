using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContentContent.Editor
{
    public static class LockInspector
    {
        private const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly MethodInfo flipLocked;
        private static readonly PropertyInfo constrainProportions;

        static LockInspector()
        {
            // Cache static MethodInfo and PropertyInfo for performance
#if UNITY_2023_2_OR_NEWER
            Type editorLockTrackerType =
                typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.EditorGUIUtility+EditorLockTracker");
            flipLocked = editorLockTrackerType.GetMethod("FlipLocked", bindingFlags);
#endif
            constrainProportions = typeof(Transform).GetProperty("constrainProportionsScale", bindingFlags);
        }

        [MenuItem("Edit/Toggle Inspector Lock %l")]
        public static void Lock()
        {
#if UNITY_2023_2_OR_NEWER
            // New approach for Unity 2023.2 and above, including Unity 6
            Type inspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

            foreach (Object inspectorWindow in Resources.FindObjectsOfTypeAll(inspectorWindowType))
            {
                object lockTracker = inspectorWindowType.GetField("m_LockTracker", bindingFlags)
                    ?.GetValue(inspectorWindow);
                flipLocked?.Invoke(lockTracker, new object[] { });
            }
#else
        // Old approach for Unity versions before 2023.2
        ActiveEditorTracker.sharedTracker.isLocked = !ActiveEditorTracker.sharedTracker.isLocked;
#endif

            // Constrain Proportions lock for all versions including Unity 6
            foreach (UnityEditor.Editor activeEditor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                if (activeEditor.target is not Transform target) continue;

                var currentValue = (bool)constrainProportions.GetValue(target, null);
                constrainProportions.SetValue(target, !currentValue, null);
            }

            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        [MenuItem("Edit/Toggle Inspector Lock %l", true)]
        public static bool Valid()
        {
            return ActiveEditorTracker.sharedTracker.activeEditors.Length != 0;
        }
    }
}