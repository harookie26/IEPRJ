using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CutsceneSequence))]
public class CutsceneSequenceEditor : Editor
{
    private SerializedProperty actionsProperty;

    private void OnEnable()
    {
        actionsProperty = serializedObject.FindProperty("actions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(actionsProperty, true);

        // Add a button to let the user choose which action type to add
        if (GUILayout.Button("Add Action"))
        {
            ShowAddActionMenu();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowAddActionMenu()
    {
        GenericMenu menu = new GenericMenu();

        // Find all classes that inherit from CutsceneAction
        var actionTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(CutsceneAction)) && !type.IsAbstract);

        foreach (var type in actionTypes)
        {
            // Add an item to the menu for each action type
            menu.AddItem(new GUIContent(type.Name), false, () => {
                AddAction(type);
            });
        }

        menu.ShowAsContext();
    }

    private void AddAction(Type actionType)
    {
        var newAction = Activator.CreateInstance(actionType);
        
        actionsProperty.arraySize++;
        SerializedProperty newActionProperty = actionsProperty.GetArrayElementAtIndex(actionsProperty.arraySize - 1);
        newActionProperty.managedReferenceValue = newAction;

        serializedObject.ApplyModifiedProperties();
    }
}