using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(BiomeSettings))]
public class BiomeSettingsEditor : ScriptlessEditor
{
    private BiomeSettings myTarget;
    private SerializedObject soTarget;

    private SerializedProperty textureData;
    private SerializedProperty slopeTextureData;
    private SerializedProperty angleThreshold;
    private SerializedProperty angleBlendRange;
    private SerializedProperty heightMapSettings;
    private SerializedProperty biomeGraph;
    private ReorderableList textureDataList;
    private TextureDataEditor textureDataEditor;
    private TextureDataEditor slopeTextureDataEditor;

    private SerializedProperty hydraulicErosion;
    private SerializedProperty thermalErosion;
    private SerializedProperty allowRoads;

    private SerializedProperty startHumidity;
    private SerializedProperty endHumidity;
    private SerializedProperty startTemperature;
    private SerializedProperty endTemperature;

    private void OnEnable()
    {
        myTarget = (BiomeSettings)target;
        soTarget = new SerializedObject(target);

        textureData = soTarget.FindProperty("textureData");
        slopeTextureData = soTarget.FindProperty("slopeTextureData");
        angleThreshold = soTarget.FindProperty("angleThreshold");
        angleBlendRange = soTarget.FindProperty("angleBlendRange");
        heightMapSettings = soTarget.FindProperty("heightMapSettings");
        biomeGraph = soTarget.FindProperty("biomeGraph");
        textureDataEditor = null;
        slopeTextureDataEditor = null;

        hydraulicErosion = soTarget.FindProperty("hydraulicErosion");
        thermalErosion = soTarget.FindProperty("thermalErosion");
        allowRoads = soTarget.FindProperty("allowRoads");

        startHumidity = soTarget.FindProperty("startHumidity");
        endHumidity = soTarget.FindProperty("endHumidity");
        startTemperature = soTarget.FindProperty("startTemperature");
        endTemperature = soTarget.FindProperty("endTemperature");
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(hydraulicErosion, true);
        EditorGUILayout.PropertyField(thermalErosion, true);
        EditorGUILayout.PropertyField(allowRoads, true);
        EditorGUILayout.Space();

        textureDataEditor = (TextureDataEditor)Common.DisplayScriptableObjectEditor(textureData, myTarget.textureData, textureDataEditor);
        slopeTextureDataEditor = (TextureDataEditor)Common.DisplayScriptableObjectEditor(slopeTextureData, myTarget.slopeTextureData, slopeTextureDataEditor);
        EditorGUILayout.PropertyField(angleThreshold, true);
        EditorGUILayout.PropertyField(angleBlendRange, true);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(biomeGraph, true);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }
}
