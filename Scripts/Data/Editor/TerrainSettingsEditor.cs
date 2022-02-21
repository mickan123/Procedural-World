using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TerrainSettings))]
public class TerrainSettingsEditor : Editor
{
    private TerrainSettings myTarget;
    private SerializedObject soTarget;

    // Biomes settings
    private SerializedProperty biomeSettings;
    private ReorderableList biomeSettingsList;
    private Dictionary<BiomeSettings, BiomeSettingsEditor> biomeSettingsEditors;

    // Erosion settings
    private SerializedProperty erosionSettings;

    // Mesh settings
    private SerializedProperty meshSettings;

    // Preview settings
    private SerializedProperty previewMaterial;
    private SerializedProperty drawMode;
    private SerializedProperty offset;
    private SerializedProperty editorPreviewLOD;
    private SerializedProperty singleBiomeIndex;
    private SerializedProperty noiseMapBiomeIndex;

    // Always display settings
    private SerializedProperty seed;

    // Editors are reset to ensure it is written to next time OnInspectorGUI is called
    private void OnEnable()
    {
        myTarget = (TerrainSettings)target;
        soTarget = new SerializedObject(target);

        // Biomes settings
        biomeSettingsEditors = new Dictionary<BiomeSettings, BiomeSettingsEditor>();
        CreateBiomeSettingsList();

        // Erosion settings
        erosionSettings = soTarget.FindProperty("erosionSettings");

        // Mesh settings
        meshSettings = soTarget.FindProperty("meshSettings");

        // Preview settings
        previewMaterial = soTarget.FindProperty("previewMaterial");
        drawMode = soTarget.FindProperty("drawMode");
        offset = soTarget.FindProperty("offset");
        editorPreviewLOD = soTarget.FindProperty("editorPreviewLOD");
        singleBiomeIndex = soTarget.FindProperty("singleBiomeIndex");
        noiseMapBiomeIndex = soTarget.FindProperty("noiseMapBiomeIndex");

        // Always display settings
        seed = soTarget.FindProperty("seed");
    }

    private void CreateBiomeSettingsList()
    {

        biomeSettings = soTarget.FindProperty("biomeSettings");

        biomeSettingsList = new ReorderableList(
            soTarget,
            biomeSettings,
            true, // Draggable
            true, // Display header
            true, // Add button
            true  // Subtract butotn
        );

        biomeSettingsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Biome Settings");
        };

        biomeSettingsList.drawElementCallback = (Rect rect, int index, bool active, bool focused) =>
        {
            SerializedProperty property = biomeSettingsList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property);
        };

        biomeSettingsList.onSelectCallback = (ReorderableList list) =>
        {
            SerializedProperty property = biomeSettingsList.serializedProperty.GetArrayElementAtIndex(list.index);
            BiomeSettingsWindow window = BiomeSettingsWindow.Open(property.objectReferenceValue as BiomeSettings);
            window.Show();
        };

        biomeSettingsList.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight * 2;
        };

        biomeSettingsList.onAddCallback = (ReorderableList list) =>
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            BiomeSettings settings = ScriptableObject.CreateInstance("BiomeSettings") as BiomeSettings;
            settings.startHumidity = Mathf.Min(0.98f, settings.startHumidity);
            settings.endHumidity = Mathf.Max(0.02f, settings.endHumidity);
            settings.startTemperature = Mathf.Min(0.98f, settings.startTemperature);
            settings.endTemperature = Mathf.Max(0.02f, settings.endTemperature);
        };
    }

    public override void OnInspectorGUI()
    {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        CommonOptions();

        myTarget.toolbarTop = GUILayout.Toolbar(myTarget.toolbarTop, new string[] { "Biomes", "Mesh", "Rivers", "Preview" });
        switch (myTarget.toolbarTop)
        {
            case 0:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Biomes";
                break;

            case 1:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Mesh";
                break;
            case 2:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Rivers";
                break;
            case 3:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Preview";
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();

        switch (myTarget.currentTab)
        {
            case "Biomes":
                BiomesTab();
                break;
            case "Mesh":
                MeshTab();
                break;
            case "Rivers":
                RiversTab();
                break;
            case "Preview":
                PreviewTab();
                break;
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate"))
        {
            myTarget.DrawMapInEditor();
        }

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }

    private void CommonOptions()
    {
        EditorGUILayout.PropertyField(seed);
        EditorGUILayout.Space();
    }

    private void BiomesTab()
    {
        EditorGUILayout.LabelField("Biome Configuration Settings", EditorStyles.boldLabel);

        if (GUILayout.Button("Biome Spawn Settings Window"))
        {
            BiomeZoneWindow window = BiomeZoneWindow.Open(myTarget, soTarget);
            window.Show();
        }
        EditorGUILayout.Space();

        biomeSettingsList.DoLayoutList();
    }

    private void MeshTab()
    {
        EditorGUILayout.ObjectField(meshSettings);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(meshSettings);
    }

    private void RiversTab()
    {

    }

    private void PreviewTab()
    {
        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(previewMaterial);
        EditorGUILayout.PropertyField(drawMode, true);
        EditorGUILayout.PropertyField(offset, true);
        EditorGUILayout.PropertyField(editorPreviewLOD, true);
        if (drawMode.enumValueIndex == (int)TerrainSettings.DrawMode.SingleBiomeMesh)
        {
            EditorGUILayout.PropertyField(singleBiomeIndex, true);
        }
        if (drawMode.enumValueIndex == (int)TerrainSettings.DrawMode.NoiseMapTexture)
        {
            EditorGUILayout.PropertyField(noiseMapBiomeIndex, true);
        }
    }
}