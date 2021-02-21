using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadSettings))]
public class RoadSettingsEditor : ScriptlessEditor
{
    private RoadSettings myTarget;
    private SerializedObject soTarget;

    private SerializedProperty roadTexture;
    private SerializedProperty width;
    private SerializedProperty roadMaterial;
    private SerializedProperty stepSize;
    private SerializedProperty smoothness;
    private SerializedProperty blendFactor;
    private SerializedProperty maxAngle;

    private void OnEnable()
    {
        myTarget = (RoadSettings)target;
        soTarget = new SerializedObject(target);

        roadTexture = soTarget.FindProperty("roadTexture");
        width = soTarget.FindProperty("width");
        roadMaterial = soTarget.FindProperty("roadMaterial");
        stepSize = soTarget.FindProperty("stepSize");
        smoothness = soTarget.FindProperty("smoothness");
        blendFactor = soTarget.FindProperty("blendFactor");
        maxAngle = soTarget.FindProperty("maxAngle");
    }

    public override void OnInspectorGUI()
    {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(roadMaterial);
        EditorGUILayout.PropertyField(stepSize);
        EditorGUILayout.PropertyField(smoothness);
        EditorGUILayout.PropertyField(blendFactor);
        EditorGUILayout.PropertyField(maxAngle);

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }
}
