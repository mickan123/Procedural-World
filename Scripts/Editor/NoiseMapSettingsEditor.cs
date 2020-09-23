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
    private SerializedProperty sandDuneSettings;

    private SerializedProperty heightMultiplier;
    private SerializedProperty heightCurve;

    private void OnEnable() {
        myTarget = (NoiseMapSettings)target;
        soTarget = new SerializedObject(target);

        noiseType = soTarget.FindProperty("noiseType");
        perlinNoiseSettings = soTarget.FindProperty("perlinNoiseSettings");
        simplexNoiseSettings = soTarget.FindProperty("simplexNoiseSettings");
        sandDuneSettings = soTarget.FindProperty("sandDuneSettings");

        heightMultiplier = soTarget.FindProperty("heightMultiplier");
        heightCurve = soTarget.FindProperty("heightCurve");
    }

    public override void OnInspectorGUI() {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(noiseType);
        if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Perlin) {
            EditorGUILayout.PropertyField(perlinNoiseSettings);
        }
        else if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Simplex) {
            EditorGUILayout.PropertyField(simplexNoiseSettings);
        }
        else if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.SandDune) {
            EditorGUILayout.PropertyField(sandDuneSettings);
        }
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(heightMultiplier);
        EditorGUILayout.PropertyField(heightCurve);

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
        }   
    }
}
