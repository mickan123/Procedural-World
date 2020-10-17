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

        var smoothFilterWidth = serializedObject.FindProperty("smoothFilterWidth");
        var smoothWidth = serializedObject.FindProperty("smoothWidth");

        var numHydraulicErosionIterations = serializedObject.FindProperty("numHydraulicErosionIterations");
        var erosionBrushRadius = serializedObject.FindProperty("erosionBrushRadius");

        var maxLifetime = serializedObject.FindProperty("maxLifetime");
        var sedimentCapacityFactor = serializedObject.FindProperty("sedimentCapacityFactor");
        var minSedimentCapacity = serializedObject.FindProperty("minSedimentCapacity");
        var depositSpeed = serializedObject.FindProperty("depositSpeed");
        var erodeSpeed = serializedObject.FindProperty("erodeSpeed");

        var evaporateSpeed = serializedObject.FindProperty("evaporateSpeed");

        var startSpeed = serializedObject.FindProperty("startSpeed");
        var startWater = serializedObject.FindProperty("startWater");

        var inertia = serializedObject.FindProperty("inertia");

        var numThermalErosionIterations = serializedObject.FindProperty("numThermalErosionIterations");
        var talusAngle = serializedObject.FindProperty("talusAngle");
        var thermalErosionRate = serializedObject.FindProperty("thermalErosionRate");
        var hardness = serializedObject.FindProperty("hardness");

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), erosionShader, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), gravity, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Smoothing Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), smoothFilterWidth, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), smoothWidth, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Hydraulic Erosion Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), numHydraulicErosionIterations, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), erosionBrushRadius, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxLifetime, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), sedimentCapacityFactor, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minSedimentCapacity, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), depositSpeed, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), erodeSpeed, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), evaporateSpeed, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), startSpeed, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), startWater, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), inertia, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Thermal Erosion Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), numThermalErosionIterations, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), talusAngle, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), thermalErosionRate, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), hardness, true);
        position.y += EditorGUIUtility.singleLineHeight;

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

        float height = 25 * EditorGUIUtility.singleLineHeight;

        return height;
    }
}
