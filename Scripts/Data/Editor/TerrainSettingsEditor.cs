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

    // Editors are reset to ensure it is written to next time OnInspectorGUI is called
    private void OnEnable()
    {
        myTarget = (TerrainSettings)target;
        soTarget = new SerializedObject(target);

        // Biomes settings
        biomeSettingsEditors = new Dictionary<BiomeSettings, BiomeSettingsEditor>();
        CreateBiomeSettingsList();
        
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

        myTarget.toolbarTop = GUILayout.Toolbar(myTarget.toolbarTop, new string[] { "Biomes", "Preview", "Details" });
        switch (myTarget.toolbarTop)
        {
            case 0:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Biomes";
                break;
            case 1:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Preview";
                break;
            case 2:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Details";
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
            myTarget.OnValidate();
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();

        switch (myTarget.currentTab)
        {
            case "Biomes":
                BiomesTab();
                break;
            case "Preview":
                PreviewTab();
                break;
            case "Details":
                DetailsTab();
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
        SerializedProperty seed = soTarget.FindProperty("seed");
        EditorGUILayout.PropertyField(seed);

        GUIContent widthList = new GUIContent("Width");
        myTarget.widthIdx = EditorGUILayout.Popup(widthList, myTarget.widthIdx, myTarget.validHeightMapWidths);

        GUIContent resolutionList = new GUIContent("Resolution");
        myTarget.resolutionIdx = EditorGUILayout.Popup(resolutionList, myTarget.resolutionIdx, myTarget.validHeightMapWidths);
        
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

    private void PreviewTab()
    {
        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
    
        SerializedProperty previewMaterial = soTarget.FindProperty("previewMaterial");
        EditorGUILayout.PropertyField(previewMaterial);
        SerializedProperty previewName = soTarget.FindProperty("previewName");
        EditorGUILayout.PropertyField(previewName);
        SerializedProperty drawMode = soTarget.FindProperty("drawMode");
        EditorGUILayout.PropertyField(drawMode, true);
        SerializedProperty chunkCoord = soTarget.FindProperty("chunkCoord");
        EditorGUILayout.PropertyField(chunkCoord, true);
        if (drawMode.enumValueIndex == (int)TerrainSettings.DrawMode.SingleBiomeMesh)
        {
            SerializedProperty singleBiomeIndex = soTarget.FindProperty("singleBiomeIndex");
            EditorGUILayout.PropertyField(singleBiomeIndex, true);
        }
    }

    private void DetailsTab()
    {
        SerializedProperty detailViewDistance = soTarget.FindProperty("detailViewDistance");
        EditorGUILayout.PropertyField(detailViewDistance);
        SerializedProperty detailResolutionPerPatch = soTarget.FindProperty("detailResolutionPerPatch");
        EditorGUILayout.PropertyField(detailResolutionPerPatch);
        SerializedProperty detailDensity = soTarget.FindProperty("detailDensity");
        EditorGUILayout.PropertyField(detailDensity);
        SerializedProperty wavingGrassAmount = soTarget.FindProperty("wavingGrassAmount");
        EditorGUILayout.PropertyField(wavingGrassAmount);
        SerializedProperty wavingGrassSpeed = soTarget.FindProperty("wavingGrassSpeed");
        EditorGUILayout.PropertyField(wavingGrassSpeed);
        SerializedProperty wavingGrassStrength = soTarget.FindProperty("wavingGrassStrength");
        EditorGUILayout.PropertyField(wavingGrassStrength);
        SerializedProperty wavingGrassTint = soTarget.FindProperty("wavingGrassTint");
        EditorGUILayout.PropertyField(wavingGrassTint);
    }
}