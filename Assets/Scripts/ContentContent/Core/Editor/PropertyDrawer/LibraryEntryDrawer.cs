using System;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Editor
{
    [CustomPropertyDrawer(typeof(LibraryEntry<>))]
    [CustomPropertyDrawer(typeof(LibraryEntryAttribute))]
    public class LibraryEntryDrawer : PropertyDrawer
    {
        private const float kVerticalPadding = 2f;
        private const float kHelpBoxHeight = 30f; // Height for the warning box

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty propID = property.FindPropertyRelative("id");
            SerializedProperty propValue = property.FindPropertyRelative("value");

            float idHeight = EditorGUI.GetPropertyHeight(propID);
            float valueHeight = EditorGUI.GetPropertyHeight(propValue);

            float totalHeight = idHeight + valueHeight + kVerticalPadding;

            // Add extra height if there is a validation error
            if (!string.IsNullOrEmpty(GetErrorMessage(property, propID)))
                totalHeight += kHelpBoxHeight + kVerticalPadding;

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect drawRect = EditorGUI.IndentedRect(position);

            SerializedProperty propID = property.FindPropertyRelative("id");
            SerializedProperty propValue = property.FindPropertyRelative("value");

            // --- Label Logic ---
            var idLabelText = "ID";
            var valueLabelText = "Value";

            if (attribute is LibraryEntryAttribute libraryAttribute)
            {
                idLabelText = libraryAttribute.IdLabel;
                valueLabelText = libraryAttribute.ValueLabel;
            }

            // --- Validation Logic ---
            string errorMessage = GetErrorMessage(property, propID);
            bool hasError = !string.IsNullOrEmpty(errorMessage);

            // --- Layout Calculation ---
            float idHeight = EditorGUI.GetPropertyHeight(propID);
            float valueHeight = EditorGUI.GetPropertyHeight(propValue);

            var idRect = new Rect(drawRect.x, drawRect.y, drawRect.width, idHeight);
            var valueRect = new Rect(drawRect.x, drawRect.y + idHeight + kVerticalPadding, drawRect.width, valueHeight);

            // --- Draw Fields ---
            EditorGUI.PropertyField(idRect, propID, new GUIContent(idLabelText));
            EditorGUI.PropertyField(valueRect, propValue, new GUIContent(valueLabelText));

            // --- Draw Error Box (if needed) ---
            if (hasError)
            {
                var helpBoxRect = new Rect(drawRect.x, valueRect.y + valueHeight + kVerticalPadding, drawRect.width,
                    kHelpBoxHeight);
                EditorGUI.HelpBox(helpBoxRect, errorMessage, MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        ///     Checks for Empty or Duplicate IDs and returns an error message, or null if valid.
        /// </summary>
        private string GetErrorMessage(SerializedProperty property, SerializedProperty idProp)
        {
            // 1. Check for Empty
            // Note: This assumes 'id' is a string. If using int/enum, remove this check.
            if (idProp.propertyType == SerializedPropertyType.String)
                if (string.IsNullOrEmpty(idProp.stringValue))
                    return "ID cannot be empty.";

            // 2. Check for Duplicates
            // We only check for duplicates if this property is part of an array/list
            if (IsDuplicate(property, idProp)) return $"Duplicate ID '{GetIdValue(idProp)}' found in list.";

            return null;
        }

        private bool IsDuplicate(SerializedProperty property, SerializedProperty idProp)
        {
            string currentId = GetIdValue(idProp);

            // Use property path parsing to find the parent list
            // Path format is usually: "variableName.Array.data[x]"
            string path = property.propertyPath;
            if (!path.Contains(".Array.data[")) return false; // Not in a list

            // Get the list property (remove the specific element index from path)
            int lastBracketIndex = path.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            string listPath = path[..lastBracketIndex]; // path.Substring(0, lastBracketIndex);
            SerializedProperty listProp = property.serializedObject.FindProperty(listPath);

            if (listProp == null || !listProp.isArray) return false;

            // Iterate through the list to compare IDs
            for (var i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty element = listProp.GetArrayElementAtIndex(i);

                // Skip checking against itself
                // We compare paths because element indices might shift during drawing but paths remain unique
                if (element.propertyPath == property.propertyPath) continue;

                SerializedProperty otherIdProp = element.FindPropertyRelative("id");
                if (otherIdProp != null)
                {
                    string otherId = GetIdValue(otherIdProp);
                    if (string.Equals(currentId, otherId)) return true;
                }
            }

            return false;
        }

        // Helper to handle String vs Int IDs generically
        private string GetIdValue(SerializedProperty idProp)
        {
            if (idProp.propertyType == SerializedPropertyType.String)
                return idProp.stringValue;
            if (idProp.propertyType == SerializedPropertyType.Integer)
                return idProp.intValue.ToString();

            return "Unknown";
        }
    }
}