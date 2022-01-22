using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ErosionSettings))]
public class ErosionSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        if (property.objectReferenceValue == null)
        {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as ErosionSettings);

        var erosionShader = serializedObject.FindProperty("erosionShader");
        var gravity = serializedObject.FindProperty("gravity");

        var numHydraulicErosionIterations = serializedObject.FindProperty("numHydraulicErosionIterations");

        var sedimentCapacityFactor = serializedObject.FindProperty("sedimentCapacityFactor");

        var evaporateSpeed = serializedObject.FindProperty("evaporateSpeed");

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), gravity, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Hydraulic Erosion Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), numHydraulicErosionIterations, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), sedimentCapacityFactor, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), evaporateSpeed, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as ErosionSettings);

        float height = 14 * EditorGUIUtility.singleLineHeight;

        return height;
    }
}
