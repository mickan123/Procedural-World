using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TerrainObjectSettings))]
public class TerrainObjectSettingsEditor : Editor
{
    private TerrainObjectSettings myTarget;
    private SerializedObject soTarget;

    // Radius vars
    private SerializedProperty varyRadius;
    private SerializedProperty radius;
    private SerializedProperty minRadius;
    private SerializedProperty maxRadius;
    private SerializedProperty noiseMapSettings;
    private Editor noiseMapSettingsEditor;

    // Height vars
    private SerializedProperty constrainHeight;
    private SerializedProperty minHeight;
    private SerializedProperty maxHeight;
    private SerializedProperty heightProbabilityCurve;

    // Slope vars
    private SerializedProperty constrainSlope;
    private SerializedProperty minSlope;
    private SerializedProperty maxSlope;

    // Scale vars
    private SerializedProperty uniformScale;
    private SerializedProperty randomScale;
    private SerializedProperty scale;
    private SerializedProperty nonUniformScale;
    private SerializedProperty minScaleNonUniform;
    private SerializedProperty maxScaleNonUniform;
    private SerializedProperty minScaleUniform;
    private SerializedProperty maxScaleUniform;

    // Translation vars
    private SerializedProperty randomTranslation;
    private SerializedProperty translation;
    private SerializedProperty minTranslation;
    private SerializedProperty maxTranslation;

    // Rotation vars
    private SerializedProperty randomRotation;
    private SerializedProperty rotation;
    private SerializedProperty minRotation;
    private SerializedProperty maxRotation;

    // Other vars
    private SerializedProperty spawnOnRoad;

    private void OnEnable() {
        myTarget = (TerrainObjectSettings)target;
        soTarget = new SerializedObject(target);

        varyRadius = soTarget.FindProperty("varyRadius");
        radius = soTarget.FindProperty("radius");
        minRadius = soTarget.FindProperty("minRadius");
        maxRadius = soTarget.FindProperty("maxRadius");
        noiseMapSettings = soTarget.FindProperty("noiseMapSettings");
        noiseMapSettingsEditor = null;

        constrainHeight = soTarget.FindProperty("constrainHeight");
        minHeight = soTarget.FindProperty("minHeight");
        maxHeight = soTarget.FindProperty("maxHeight");
        heightProbabilityCurve = soTarget.FindProperty("heightProbabilityCurve");

        constrainSlope = soTarget.FindProperty("constrainSlope");
        minSlope = soTarget.FindProperty("minSlope");
        maxSlope = soTarget.FindProperty("maxSlope");

        uniformScale = soTarget.FindProperty("uniformScale");
        randomScale = soTarget.FindProperty("randomScale");
        scale = soTarget.FindProperty("scale");
        nonUniformScale = soTarget.FindProperty("nonUniformScale");
        minScaleNonUniform = soTarget.FindProperty("minScaleNonUniform");
        maxScaleNonUniform = soTarget.FindProperty("maxScaleNonUniform");
        minScaleUniform = soTarget.FindProperty("minScaleUniform");
        maxScaleUniform = soTarget.FindProperty("maxScaleUniform");

        randomTranslation = soTarget.FindProperty("randomTranslation");
        translation = soTarget.FindProperty("translation");
        minTranslation = soTarget.FindProperty("minTranslation");
        maxTranslation = soTarget.FindProperty("maxTranslation");

        randomRotation = soTarget.FindProperty("randomRotation");
        rotation = soTarget.FindProperty("rotation");
        minRotation = soTarget.FindProperty("minRotation");
        maxRotation = soTarget.FindProperty("maxRotation");

        spawnOnRoad = soTarget.FindProperty("spawnOnRoad");
    }

    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Radius", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(varyRadius, true);
        if (myTarget.varyRadius) {
            EditorGUILayout.PropertyField(minRadius, true);
            EditorGUILayout.PropertyField(maxRadius, true);
            Common.DisplayScriptableObjectEditor(noiseMapSettings, myTarget.noiseMapSettings, noiseMapSettingsEditor);
        }
        else {
            EditorGUILayout.PropertyField(radius, true);
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.LabelField("Height", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(constrainHeight, true);
        if (myTarget.constrainHeight) {
            EditorGUILayout.PropertyField(minHeight, true);
            EditorGUILayout.PropertyField(maxHeight, true);
            EditorGUILayout.PropertyField(heightProbabilityCurve, true);
        }
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Slope", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(constrainSlope, true);
        if (myTarget.constrainSlope) {
            EditorGUILayout.PropertyField(minSlope, true);
            EditorGUILayout.PropertyField(maxSlope, true);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(uniformScale, true);
        EditorGUILayout.PropertyField(randomScale, true);
        if (myTarget.uniformScale && !myTarget.randomScale) {
            EditorGUILayout.PropertyField(scale, true);
        }
        else if (myTarget.uniformScale && myTarget.randomScale) {
            EditorGUILayout.PropertyField(minScaleUniform, true);
            EditorGUILayout.PropertyField(maxScaleUniform, true);
        }
        else if (!myTarget.uniformScale && myTarget.randomScale) {
            EditorGUILayout.PropertyField(minScaleNonUniform, true);
            EditorGUILayout.PropertyField(maxScaleNonUniform, true);
        }
        else if (!myTarget.uniformScale && !myTarget.randomScale) {
            EditorGUILayout.PropertyField(nonUniformScale, true);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Translation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(randomTranslation, true);
        if (myTarget.randomTranslation) {
            EditorGUILayout.PropertyField(minTranslation, true);
            EditorGUILayout.PropertyField(maxTranslation, true);
        }
        else {
            EditorGUILayout.PropertyField(translation, true);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(randomRotation, true);
        if (myTarget.randomRotation) {
            EditorGUILayout.PropertyField(minRotation, true);
            EditorGUILayout.PropertyField(maxRotation, true);
        }
        else {
            EditorGUILayout.PropertyField(rotation, true);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnOnRoad, true);
        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
        }
    }
}
