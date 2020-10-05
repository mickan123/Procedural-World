using System.Collections;
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
    private SerializedProperty humidityMapGraph;
    private SerializedProperty temperatureMapGraph;
    private SerializedProperty transitionDistance;
    private SerializedProperty biomeSettings;
    private ReorderableList biomeSettingsList;
    private Dictionary<BiomeSettings, BiomeSettingsEditor> biomeSettingsEditors;

    // Erosion settings
    private SerializedProperty erosionSettings;

    // Mesh settings
    private SerializedProperty meshSettings;
    private Editor meshSettingsEditor;

    // Road settings
    private SerializedProperty roadSettings;
    private Editor roadSettingsEditor;

    // Preview settings
    private SerializedProperty previewMaterial;
    private SerializedProperty drawMode;
    private SerializedProperty centre;
    private SerializedProperty editorPreviewLOD;
    private SerializedProperty singleBiomeIndex;
    private SerializedProperty noiseMapBiomeIndex;

    // Always display settings
    private SerializedProperty seed;

    // Editors are reset to ensure it is written to next time OnInspectorGUI is called
    private void OnEnable() {
        myTarget = (TerrainSettings)target;
        soTarget = new SerializedObject(target);

        // Biomes settings
        humidityMapGraph = soTarget.FindProperty("humidityMapGraph");
        temperatureMapGraph = soTarget.FindProperty("temperatureMapGraph");
        transitionDistance = soTarget.FindProperty("transitionDistance");
        biomeSettingsEditors = new Dictionary<BiomeSettings, BiomeSettingsEditor>();
        CreateBiomeSettingsList();
        
        // Erosion settings
        erosionSettings = soTarget.FindProperty("erosionSettings");

        // Mesh settings
        meshSettings = soTarget.FindProperty("meshSettings");
        meshSettingsEditor = null;

        // Road settings
        roadSettings = soTarget.FindProperty("roadSettings");
        roadSettingsEditor = null;

        // Preview settings
        previewMaterial = soTarget.FindProperty("previewMaterial");
        drawMode = soTarget.FindProperty("drawMode");
        centre = soTarget.FindProperty("centre");
        editorPreviewLOD = soTarget.FindProperty("editorPreviewLOD");
        singleBiomeIndex = soTarget.FindProperty("singleBiomeIndex");
        noiseMapBiomeIndex = soTarget.FindProperty("noiseMapBiomeIndex");

        // Always display settings
        seed = soTarget.FindProperty("seed");
    }

    private void CreateBiomeSettingsList() {
        
        biomeSettings = soTarget.FindProperty("biomeSettings");

        biomeSettingsList = new ReorderableList(
            soTarget, 
            biomeSettings,
            true, // Draggable
            true, // Display header
            true, // Add button
            true  // Subtract butotn
        );

        biomeSettingsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Biome Settings");
        };

        biomeSettingsList.drawElementCallback = (Rect rect, int index, bool active, bool focused) => {
            SerializedProperty property = biomeSettingsList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property);
            
            if (active) {
                if (property.objectReferenceValue != null) {
                    BiomeSettings settings = property.objectReferenceValue as BiomeSettings;
                    if (!biomeSettingsEditors.ContainsKey(settings)) {
                        biomeSettingsEditors[settings] = CreateEditor(property.objectReferenceValue) as BiomeSettingsEditor;
                    }
                    
                    biomeSettingsEditors[settings].OnInspectorGUI();
                }
            }
        };

        biomeSettingsList.elementHeightCallback = (int index) => {
            return EditorGUIUtility.singleLineHeight * 2;
        };

        biomeSettingsList.onAddCallback = (ReorderableList list) => {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            
            myTarget.biomeSettings.Add(ScriptableObject.CreateInstance("BiomeSettings") as BiomeSettings);
        };
    }   

    public override void OnInspectorGUI() {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        CommonOptions();

        myTarget.toolbarTop = GUILayout.Toolbar(myTarget.toolbarTop, new string[] { "Biomes", "Erosion", "Mesh", "Roads"});
        switch (myTarget.toolbarTop) {
            case 0:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Biomes";
                break;
            case 1:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Erosion";
                break;
            case 2:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Mesh";
                break;
            case 3:
                myTarget.toolbarBottom = -1;
                myTarget.currentTab = "Roads";
                break;            
        }

        myTarget.toolbarBottom = GUILayout.Toolbar(myTarget.toolbarBottom, new string[] { "Rivers", "Preview" });
        switch (myTarget.toolbarBottom) {
            case 0:
                myTarget.toolbarTop = -1;
                myTarget.currentTab = "Rivers";
                break;
            case 1:
                myTarget.toolbarTop = -1;
                myTarget.currentTab = "Preview";
                break;
        }

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();

        switch (myTarget.currentTab) {
            case "Biomes":
                BiomesTab();
                break;
            case "Erosion":
                ErosionTab();
                break;
            case "Mesh":
                MeshTab();
                break;
            case "Roads":
                RoadsTab();
                break;
            case "Rivers":
                RiversTab();
                break;
            case "Preview":
                PreviewTab();
                break;
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate")) {
			myTarget.DrawMapInEditor();
		}

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
        }
    }

    private void CommonOptions() {
        EditorGUILayout.PropertyField(seed);
        EditorGUILayout.Space();
    }

    private void BiomesTab() {
        EditorGUILayout.PropertyField(transitionDistance);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Biome Spawn Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(humidityMapGraph, true);
        EditorGUILayout.PropertyField(temperatureMapGraph, true);
        EditorGUILayout.Space();

        for (int i = 0 ; i < biomeSettings.arraySize; i++) {
            SerializedProperty prop = biomeSettings.GetArrayElementAtIndex(i);
            BiomeSettings settings = prop.objectReferenceValue as BiomeSettings;
            if (settings != null) {
                EditorGUILayout.ObjectField(prop);
                EditorGUILayout.MinMaxSlider(ref settings.startTemperature, ref settings.endTemperature, 0f, 1f);
                EditorGUILayout.MinMaxSlider(ref settings.startHumidity, ref settings.endHumidity, 0f, 1f);
                EditorGUILayout.Space();
            }
        }
        myTarget.ValidateBiomeSpawnCriteria();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Biome Configuration Settings", EditorStyles.boldLabel);
        biomeSettingsList.DoLayoutList();
    }

    private void ErosionTab() {
        EditorGUILayout.PropertyField(erosionSettings, true);
    }

    private void MeshTab() {
        Common.DisplayScriptableObjectEditor(meshSettings, myTarget.meshSettings, meshSettingsEditor);
    }

    private void RoadsTab() {
        Common.DisplayScriptableObjectEditor(roadSettings, myTarget.roadSettings, roadSettingsEditor);
    }

    private void RiversTab() {

    }

    private void PreviewTab() {
        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(previewMaterial);
        EditorGUILayout.PropertyField(drawMode, true);
        EditorGUILayout.PropertyField(centre, true);
        EditorGUILayout.PropertyField(editorPreviewLOD, true);
        if (drawMode.enumValueIndex == (int)TerrainSettings.DrawMode.SingleBiomeMesh) {
            EditorGUILayout.PropertyField(singleBiomeIndex, true);
        }   
        if (drawMode.enumValueIndex == (int)TerrainSettings.DrawMode.NoiseMapTexture) {
            EditorGUILayout.PropertyField(noiseMapBiomeIndex, true);
        }
    }

    
}