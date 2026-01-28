using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace ContentContent.UI
{
    [CustomEditor(typeof(BetterLayoutElement))]
    [CanEditMultipleObjects]
    public class BetterLayoutElementEditor : LayoutElementEditor
    {
        private SerializedProperty maxHeight;
        private SerializedProperty maxWidth;
        private SerializedProperty useMaxHeight;
        private SerializedProperty useMaxWidth;

        protected override void OnEnable()
        {
            base.OnEnable();

            useMaxWidth = serializedObject.FindProperty("useMaxWidth");
            maxWidth = serializedObject.FindProperty("maxWidth");
            useMaxHeight = serializedObject.FindProperty("useMaxHeight");
            maxHeight = serializedObject.FindProperty("maxHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Draw standard Min/Preferred/Flexible fields from base class
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            // 2. Draw Max Width and Max Height with Native alignment
            DrawLayoutProperty(useMaxWidth, maxWidth, "Max Width");
            DrawLayoutProperty(useMaxHeight, maxHeight, "Max Height");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayoutProperty(SerializedProperty toggle, SerializedProperty value, string label)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            float labelWidth = EditorGUIUtility.labelWidth;
            const float toggleWidth = 20f; // Width for the checkbox space

            // 1. Draw the Label in the left column
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), label);

            // 2. Draw the Toggle at the far left of the "Value" column
            var toggleRect = new Rect(rect.x + labelWidth, rect.y, toggleWidth, rect.height);
            toggle.boolValue = EditorGUI.Toggle(toggleRect, toggle.boolValue);

            // 3. Draw the Float Field to the right of the toggle
            using (new EditorGUI.DisabledGroupScope(!toggle.boolValue))
            {
                var fieldRect = new Rect(
                    rect.x + labelWidth + toggleWidth,
                    rect.y,
                    rect.width - labelWidth - toggleWidth,
                    rect.height
                );

                EditorGUI.PropertyField(fieldRect, value, GUIContent.none);
            }
        }
    }
}