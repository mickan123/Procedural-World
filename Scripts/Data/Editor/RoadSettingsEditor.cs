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
    private SerializedProperty stepSize;
    private SerializedProperty smoothness;
    private SerializedProperty distanceBlendFactor;
    private SerializedProperty angleBlendFactor;
    private SerializedProperty maxAngle;

    private void OnEnable()
    {
        myTarget = (RoadSettings)target;
        soTarget = new SerializedObject(target);

        roadTexture = soTarget.FindProperty("roadTexture");
        width = soTarget.FindProperty("width");
        distanceBlendFactor = soTarget.FindProperty("distanceBlendFactor");
        angleBlendFactor = soTarget.FindProperty("angleBlendFactor");
        maxAngle = soTarget.FindProperty("maxAngle");
    }

    public override void OnInspectorGUI()
    {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(roadTexture);
        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(distanceBlendFactor);
        EditorGUILayout.PropertyField(angleBlendFactor);
        EditorGUILayout.PropertyField(maxAngle);

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }
}
