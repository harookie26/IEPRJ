using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generic foldable inspector. Opt-in by adding [FoldableInspector] to your MonoBehaviour.
/// Groups fields by the nearest [Header] attribute (falls back to "Settings") and shows tooltips.
/// Does not replace other editors unless the attribute is present.
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class FoldableComponentEditor : Editor
{
    // persistent per-type foldout state: key = type.FullName + "::" + header
    private static readonly Dictionary<string, bool> FoldoutState = new Dictionary<string, bool>();

    // cached grouping for the current inspected type to avoid repeated reflection
    private string cachedTypeName;
    private List<Group> groups;

    private class Group
    {
        public string Header;
        public List<SerializedProperty> Properties = new List<SerializedProperty>();
        // store FieldInfo for each property as well for quick tooltip/header detection
        public List<FieldInfo> BackingFields = new List<FieldInfo>();
    }

    public override void OnInspectorGUI()
    {
        var targetType = serializedObject.targetObject.GetType();

        // Only handle types explicitly opt-in via FoldableInspectorAttribute
        var foldAttr = targetType.GetCustomAttribute<FoldableInspectorAttribute>();
        if (foldAttr == null)
        {
            // Not opted-in: draw default inspector
            DrawDefaultInspector();
            return;
        }

        // Rebuild groups if different type (or first time)
        if (cachedTypeName != targetType.FullName || groups == null)
            BuildGroups(targetType);

        serializedObject.Update();

        // Script field (always show, match Unity's style)
        DrawScriptField();

        // Optional header message
        var title = string.IsNullOrEmpty(foldAttr.DisplayName) ? targetType.Name : foldAttr.DisplayName;
        EditorGUILayout.HelpBox($"Foldable inspector for {title}. Hover properties to see tooltips.", MessageType.None);

        // Draw groups as foldouts
        foreach (var g in groups)
        {
            var key = targetType.FullName + "::" + g.Header;
            if (!FoldoutState.TryGetValue(key, out var isOpen))
                FoldoutState[key] = isOpen = true;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            FoldoutState[key] = EditorGUILayout.Foldout(FoldoutState[key], g.Header, true);
            if (FoldoutState[key])
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < g.Properties.Count; ++i)
                {
                    var prop = g.Properties[i];
                    var field = g.BackingFields[i];
                    if (prop == null) continue;

                    var tooltip = GetTooltipForField(field);

                    // If the component attribute requested hiding the original field headers,
                    // and the field itself had a HeaderAttribute, draw the property manually using common controls.
                    if (foldAttr.HideFieldHeaders && field != null && field.GetCustomAttribute<HeaderAttribute>() != null)
                    {
                        DrawPropertyWithoutUnityHeader(prop, field, tooltip);
                    }
                    else
                    {
                        // Normal drawing: property label + tooltip
                        EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName, tooltip), true);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        // show disabled script field the same way Unity's inspector does
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript ms = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            EditorGUILayout.ObjectField("Script", ms, typeof(MonoScript), false);
        }
    }

    private void BuildGroups(Type inspectedType)
    {
        cachedTypeName = inspectedType.FullName;
        groups = new List<Group>();

        Group currentGroup = null; // create lazily

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        // Move to first property
        if (!prop.NextVisible(enterChildren))
            return;

        do
        {
            // Skip internal script reference (we draw it ourselves)
            if (prop.name == "m_Script")
                continue;

            var field = FindFieldInfo(inspectedType, prop.name);

            // If the field has a HeaderAttribute, start a new group using its header text.
            var hdr = field?.GetCustomAttribute<HeaderAttribute>();
            if (hdr != null && !string.IsNullOrEmpty(hdr.header))
            {
                currentGroup = new Group { Header = hdr.header };
                groups.Add(currentGroup);
            }

            // If there's no current group yet, lazily create the default "Settings" group.
            if (currentGroup == null)
            {
                currentGroup = new Group { Header = "Settings" };
                groups.Add(currentGroup);
            }

            // Skip fields explicitly hidden from inspector
            if (field != null && field.GetCustomAttribute<HideInInspector>() != null)
                continue;

            // Add property and its backing field for later drawing/tooltip lookup.
            currentGroup.Properties.Add(serializedObject.FindProperty(prop.propertyPath));
            currentGroup.BackingFields.Add(field);

        } while (prop.NextVisible(false));
    }

    private FieldInfo FindFieldInfo(Type type, string propertyName)
    {
        // walk inheritance chain
        Type t = type;
        while (t != null)
        {
            var field = t.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field;
            t = t.BaseType;
        }
        return null;
    }

    private string GetTooltipForField(FieldInfo field)
    {
        if (field == null) return string.Empty;
        var tp = field.GetCustomAttribute<TooltipAttribute>();
        if (tp != null) return tp.tooltip;
        return string.Empty;
    }

    // Manual drawing bypassing Unity's automatic Header rendering for common serialized property types.
    private void DrawPropertyWithoutUnityHeader(SerializedProperty prop, FieldInfo field, string tooltip)
    {
        EditorGUI.BeginChangeCheck();

        // Determine a UnityEngine.Object-compatible type for object references
        Type objectType = null;
        if (field != null && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            objectType = field.FieldType;

        var label = new GUIContent(prop.displayName, tooltip);

        switch (prop.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                {
                    var newObj = EditorGUILayout.ObjectField(label, prop.objectReferenceValue, objectType ?? typeof(UnityEngine.Object), true);
                    if (EditorGUI.EndChangeCheck())
                        prop.objectReferenceValue = newObj as UnityEngine.Object;
                    return;
                }
            case SerializedPropertyType.Float:
                {
                    var newVal = EditorGUILayout.FloatField(label, prop.floatValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.floatValue = newVal;
                    return;
                }
            case SerializedPropertyType.Integer:
                {
                    var newVal = EditorGUILayout.IntField(label, prop.intValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.intValue = newVal;
                    return;
                }
            case SerializedPropertyType.Boolean:
                {
                    var newVal = EditorGUILayout.Toggle(label, prop.boolValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.boolValue = newVal;
                    return;
                }
            case SerializedPropertyType.String:
                {
                    var newVal = EditorGUILayout.TextField(label, prop.stringValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.stringValue = newVal;
                    return;
                }
            case SerializedPropertyType.Vector2:
                {
                    var newVal = EditorGUILayout.Vector2Field(prop.displayName, prop.vector2Value);
                    if (EditorGUI.EndChangeCheck())
                        prop.vector2Value = newVal;
                    return;
                }
            case SerializedPropertyType.Vector3:
                {
                    var newVal = EditorGUILayout.Vector3Field(prop.displayName, prop.vector3Value);
                    if (EditorGUI.EndChangeCheck())
                        prop.vector3Value = newVal;
                    return;
                }
            case SerializedPropertyType.Color:
                {
                    var newVal = EditorGUILayout.ColorField(label, prop.colorValue);
                    if (EditorGUI.EndChangeCheck())
                        prop.colorValue = newVal;
                    return;
                }
            case SerializedPropertyType.Enum:
                {
                    // Try to get a proper enum type via FieldInfo if possible
                    if (field != null && field.FieldType.IsEnum)
                    {
                        var enumVal = (Enum)Enum.ToObject(field.FieldType, prop.enumValueIndex);
                        var newEnum = EditorGUILayout.EnumPopup(label, enumVal);
                        if (EditorGUI.EndChangeCheck())
                            prop.enumValueIndex = Convert.ToInt32(newEnum);
                        return;
                    }
                    break;
                }
                // Add more types if you need them
        }

        // Fallback: draw normally (may still show header for rare types)
        EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName, tooltip), true);
    }
}