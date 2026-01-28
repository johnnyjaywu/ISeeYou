using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContentContent.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownDrawer : PropertyDrawer
    {
        // Cache the derived types to avoid repetitive reflection if possible
        // (Key: BaseType Name, Value: List of Subtypes)
        private static readonly Dictionary<string, List<Type>> typeCache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // SAFETY CHECK: Ensure [SerializeReference] is present
            // fieldInfo is a built-in variable of PropertyDrawer
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(SerializeReference), false);
            if (attributes.Length == 0)
            {
                EditorGUI.HelpBox(position,
                    $"Field '{property.name}' is missing [SerializeReference] attribute! Data will not save.",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Draw Label
            var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            // Draw Dropdown Button
            var dropdownRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);

            // OPTIMIZATION: Do not use reflection here to get the name.
            // Use the raw object if available, otherwise "Null".
            var currentName = "Null (Assign)";
            if (property.managedReferenceValue != null)
                // Simple type retrieval is cheap
                currentName = property.managedReferenceValue.GetType().Name;

            if (GUI.Button(dropdownRect, currentName, EditorStyles.popup))
                // Only do the heavy lifting when the user clicks!
                ShowContextMenu(property);

            // Draw the actual fields of the selected object
            EditorGUI.PropertyField(position, property, GUIContent.none, true);

            EditorGUI.EndProperty();
        }

        private void ShowContextMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();

            // 1. Add Null Option
            menu.AddItem(new GUIContent("None"), property.managedReferenceValue == null, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            // 2. Find the Base Type of the field
            Type fieldType = GetTargetType(property);

            if (fieldType == null)
            {
                menu.AddDisabledItem(new GUIContent("Error: Could not resolve type"));
                menu.ShowAsContext();
                return;
            }

            // 3. Find all valid implementations
            List<Type> derivedTypes = GetDerivedTypes(fieldType);

            // 4. Populate Menu
            foreach (Type type in derivedTypes)
            {
                string menuLabel = type.Name;

                // Add tooltip or namespace if you have duplicate names
                // menuLabel = type.FullName.Replace('.', '/'); 

                menu.AddItem(new GUIContent(menuLabel), IsTypeSelected(property, type),
                    () => { ApplyType(property, type); });
            }

            menu.ShowAsContext();
        }

        private void ApplyType(SerializedProperty property, Type type)
        {
            // 1. Check if the type is actually instantiable
            if (typeof(Object).IsAssignableFrom(type))
            {
                Debug.LogError(
                    $"[SerializeReference] Error: '{type.Name}' inherits from UnityEngine.Object (MonoBehaviour/ScriptableObject). It must be a plain C# class.");
                return;
            }

            try
            {
                object newInstance = Activator.CreateInstance(type);

                // 2. Compatibility Check
                // We need to know what type the FIELD expects.
                Type fieldType = GetTargetType(property);
                if (fieldType != null && !fieldType.IsAssignableFrom(type))
                {
                    Debug.LogError(
                        $"[SerializeReference] Type Mismatch! Field '{property.name}' expects '{fieldType.Name}', but you are trying to assign '{type.Name}'.");
                    return;
                }

                // 3. Assign
                property.managedReferenceValue = newInstance;
                property.serializedObject.ApplyModifiedProperties();
            }
            catch (ArgumentException ex)
            {
                Debug.LogError(
                    $"[SerializeReference] Unity rejected the assignment. Make sure '{type.Name}' has [Serializable] attribute! \nDetails: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializeReference] Creation failed: {ex.Message}");
            }
        }

        private bool IsTypeSelected(SerializedProperty property, Type type)
        {
            return property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;
        }

        /// <summary>
        ///     Finds all classes that inherit from the base type, filtering out abstracts and interfaces.
        ///     Uses caching to speed up repeated clicks.
        /// </summary>
        private List<Type> GetDerivedTypes(Type baseType)
        {
            if (typeCache.TryGetValue(baseType.FullName, out List<Type> cached)) return cached;

            List<Type> types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t =>
                        !t.IsAbstract &&
                        !t.IsInterface &&
                        !t.IsGenericType && // Generic types usually can't be instantiated blindly
                        t.GetConstructor(Type.EmptyTypes) != null // Must have default constructor
                )
                .OrderBy(t => t.Name)
                .ToList();

            typeCache[baseType.FullName] = types;
            return types;
        }

        /// <summary>
        ///     Robustly finds the type of the field, handling Lists, Arrays, and nested classes.
        /// </summary>
        private Type GetTargetType(SerializedProperty property)
        {
            try
            {
                Type parentType = property.serializedObject.targetObject.GetType();
                string[] parts = property.propertyPath.Split('.');

                for (var i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];

                    if (part == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
                    {
                        if (parentType.IsArray) parentType = parentType.GetElementType();
                        else if (parentType.IsGenericType) parentType = parentType.GetGenericArguments()[0];
                        i++;
                    }
                    else
                    {
                        // FIX: Standard GetField doesn't find private fields in base classes.
                        // We must loop up the hierarchy.
                        FieldInfo fi = null;
                        Type currentReflectType = parentType;

                        while (currentReflectType != null && fi == null)
                        {
                            fi = currentReflectType.GetField(part,
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            currentReflectType = currentReflectType.BaseType;
                        }

                        if (fi == null) return null;
                        parentType = fi.FieldType;
                    }
                }

                return parentType;
            }
            catch
            {
                return null;
            }
        }
    }
}