using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseMapSettings))]
public class NoiseMapSettingsEditor : ScriptlessEditor
{
    private NoiseMapSettings myTarget;
    private SerializedObject soTarget;

    private SerializedProperty noiseType;
    private SerializedProperty perlinNoiseSettings;
    private SerializedProperty simplexNoiseSettings;

    private void OnEnable()
    {
        myTarget = (NoiseMapSettings)target;
        soTarget = new SerializedObject(target);

        noiseType = soTarget.FindProperty("noiseType");
        perlinNoiseSettings = soTarget.FindProperty("perlinNoiseSettings");
        simplexNoiseSettings = soTarget.FindProperty("simplexNoiseSettings");
    }

    public override void OnInspectorGUI()
    {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(noiseType);
        if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Perlin)
        {
            EditorGUILayout.PropertyField(perlinNoiseSettings);
        }
        else if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Simplex)
        {
            EditorGUILayout.PropertyField(simplexNoiseSettings);
        }
        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }
}
