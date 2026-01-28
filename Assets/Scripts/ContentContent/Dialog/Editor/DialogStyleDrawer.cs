using UnityEditor;
using UnityEngine;

namespace ContentContent.Dialog.Editor
{
    [CustomPropertyDrawer(typeof(DialogStyle))]
    public class DialogStyleDrawer : PropertyDrawer
    {
        private const float kSpacing = 2f;
        private const float kToggleWidth = 18f;
        private const float kTogglePadding = 12f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float singleLine = EditorGUIUtility.singleLineHeight;
            var foldoutRect = new Rect(position.x, position.y, position.width, singleLine);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float currentY = position.y + singleLine + kSpacing;

                // --- 1. ANIMATION ---
                // Open Block
                currentY = DrawAnimationBlock(position, currentY, property, "Open", "animateOpen",
                    new[]
                    {
                        ("useOpenAlpha", "openAlpha", "Alpha"),
                        ("useOpenScale", "openScale", "Scale"),
                        ("useOpenTranslate", "openTranslate", "Translate")
                    });

                currentY += kSpacing;

                // Close Block
                currentY = DrawAnimationBlock(position, currentY, property, "Close", "animateClose",
                    new[]
                    {
                        ("useCloseAlpha", "closeAlpha", "Alpha"),
                        ("useCloseScale", "closeScale", "Scale"),
                        ("useCloseTranslate", "closeTranslate", "Translate")
                    });

                currentY += kSpacing;

                // --- 2. AUDIO ---
                currentY = DrawProperty(position, currentY, property.FindPropertyRelative("openingSound"));
                currentY = DrawProperty(position, currentY, property.FindPropertyRelative("closingSound"));
                currentY = DrawProperty(position, currentY, property.FindPropertyRelative("typingSound"));

                currentY += kSpacing;

                // --- 3. TYPEWRITER ---
                DrawProperty(position, currentY, property.FindPropertyRelative("typeSpeed"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                totalHeight += kSpacing;

                // Animation
                totalHeight += GetAnimationBlockHeight(property, "animateOpen",
                    new[] { "openAlpha", "openScale", "openTranslate" });
                totalHeight += kSpacing;

                totalHeight += GetAnimationBlockHeight(property, "animateClose",
                    new[] { "closeAlpha", "closeScale", "closeTranslate" });
                totalHeight += kSpacing;

                // Audio
                totalHeight += GetPropHeight(property, "openingSound");
                totalHeight += GetPropHeight(property, "closingSound");
                totalHeight += GetPropHeight(property, "typingSound");
                totalHeight += kSpacing;

                // Typewriter
                totalHeight += GetPropHeight(property, "typeSpeed");
            }

            return totalHeight;
        }

        // --- Helper Methods ---

        private float GetPropHeight(SerializedProperty root, string relName)
        {
            SerializedProperty prop = root.FindPropertyRelative(relName);
            return prop != null ? EditorGUI.GetPropertyHeight(prop) + kSpacing : 0f;
        }

        private float DrawProperty(Rect baseRect, float y, SerializedProperty prop)
        {
            if (prop == null) return y;
            float h = EditorGUI.GetPropertyHeight(prop);
            var rect = new Rect(baseRect.x, y, baseRect.width, h);
            EditorGUI.PropertyField(rect, prop);
            return y + h + kSpacing;
        }

        private float DrawAnimationBlock(Rect baseRect, float y, SerializedProperty root, string title,
            string animateBool, (string toggle, string settings, string label)[] items)
        {
            SerializedProperty animateProp = root.FindPropertyRelative(animateBool);
            if (animateProp == null) return y;

            float h = EditorGUI.GetPropertyHeight(animateProp);
            var animateRect = new Rect(baseRect.x, y, baseRect.width, h);

            EditorGUI.PropertyField(animateRect, animateProp, new GUIContent($"Animate {title}"));
            y += h + kSpacing;

            if (animateProp.boolValue)
            {
                // NOTE: Global Duration removed from here

                int originalIndent = EditorGUI.indentLevel;

                foreach ((string toggle, string settings, string label) item in items)
                {
                    SerializedProperty toggleProp = root.FindPropertyRelative(item.toggle);
                    SerializedProperty settingsProp = root.FindPropertyRelative(item.settings);

                    if (toggleProp != null && settingsProp != null)
                    {
                        float rowHeight;
                        if (!toggleProp.boolValue)
                            rowHeight = EditorGUIUtility.singleLineHeight;
                        else
                            rowHeight = EditorGUI.GetPropertyHeight(settingsProp, new GUIContent(item.label));

                        var rowRect = new Rect(baseRect.x, y, baseRect.width, rowHeight);
                        Rect indentedRect = EditorGUI.IndentedRect(rowRect);

                        EditorGUI.indentLevel = 0;

                        // 1. Draw Toggle
                        var toggleRect = new Rect(indentedRect.x, indentedRect.y, kToggleWidth,
                            EditorGUIUtility.singleLineHeight);

                        EditorGUI.BeginChangeCheck();
                        bool newToggle = EditorGUI.Toggle(toggleRect, toggleProp.boolValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            toggleProp.boolValue = newToggle;
                            if (newToggle) settingsProp.isExpanded = true;
                            else settingsProp.isExpanded = false;
                        }

                        // 2. Draw Property Field
                        float offset = kToggleWidth + kTogglePadding;
                        var propertyRect = new Rect(indentedRect.x + offset, indentedRect.y,
                            indentedRect.width - offset, rowHeight);

                        using (new EditorGUI.DisabledScope(!toggleProp.boolValue))
                        {
                            if (!toggleProp.boolValue && settingsProp.isExpanded)
                                settingsProp.isExpanded = false;

                            EditorGUI.PropertyField(propertyRect, settingsProp, new GUIContent(item.label), true);
                        }

                        EditorGUI.indentLevel = originalIndent;
                        y += rowHeight + kSpacing;
                    }
                }
            }

            return y;
        }

        private float GetAnimationBlockHeight(SerializedProperty root, string animateBool, string[] settingNames)
        {
            SerializedProperty animateProp = root.FindPropertyRelative(animateBool);
            if (animateProp == null) return 0f;

            float h = EditorGUI.GetPropertyHeight(animateProp) + kSpacing;

            if (animateProp.boolValue)
                // NOTE: Global Duration removed from here
                foreach (string name in settingNames)
                {
                    string toggleName = "use" + char.ToUpper(name[0]) + name.Substring(1);
                    SerializedProperty toggleProp = root.FindPropertyRelative(toggleName);
                    SerializedProperty settingsProp = root.FindPropertyRelative(name);

                    if (toggleProp != null && settingsProp != null)
                    {
                        if (toggleProp.boolValue)
                            h += EditorGUI.GetPropertyHeight(settingsProp, new GUIContent("Label")) + kSpacing;
                        else
                            h += EditorGUIUtility.singleLineHeight + kSpacing;
                    }
                }

            return h;
        }
    }
}