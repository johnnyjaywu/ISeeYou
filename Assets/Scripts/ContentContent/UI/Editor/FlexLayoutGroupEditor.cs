using UnityEditor;
using UnityEngine;
using ContentContent.UI;

namespace ContentContent.UI.Editor
{
    [CustomEditor(typeof(FlexLayoutGroup))]
    [CanEditMultipleObjects]
    public class FlexLayoutGroupEditor : UnityEditor.Editor
    {
        private SerializedProperty m_Padding;
        private SerializedProperty spacing;
        private SerializedProperty direction;
        private SerializedProperty wrap;
        private SerializedProperty justifyContent;
        private SerializedProperty alignItems;
        private SerializedProperty minSize;
        private SerializedProperty maxSize;
        private SerializedProperty forceExpandMain;
        private SerializedProperty forceExpandCross;

        private void OnEnable()
        {
            m_Padding = serializedObject.FindProperty("m_Padding");
            spacing = serializedObject.FindProperty("spacing");
            direction = serializedObject.FindProperty("direction");
            wrap = serializedObject.FindProperty("wrap");
            justifyContent = serializedObject.FindProperty("justifyContent");
            alignItems = serializedObject.FindProperty("alignItems");
            minSize = serializedObject.FindProperty("minSize");
            maxSize = serializedObject.FindProperty("maxSize");
            forceExpandMain = serializedObject.FindProperty("forceExpandMainAxis");
            forceExpandCross = serializedObject.FindProperty("forceExpandCrossAxis");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Flex Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(direction);
            EditorGUILayout.PropertyField(wrap);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(justifyContent);
            EditorGUILayout.PropertyField(alignItems);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sizing Constraints", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spacing);
            EditorGUILayout.PropertyField(minSize);
            EditorGUILayout.PropertyField(maxSize);
            
            EditorGUILayout.PropertyField(m_Padding, true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Child Controls", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(forceExpandMain);
            EditorGUILayout.PropertyField(forceExpandCross);

            serializedObject.ApplyModifiedProperties();
        }
    }
}