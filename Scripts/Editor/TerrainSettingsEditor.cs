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
    private SerializedProperty humidityMapSettings;
    private SerializedProperty temperatureMapSettings;
    private SerializedProperty transitionDistance;
    private SerializedProperty biomeSettings;
    private ReorderableList biomeSettingsList;
    private Editor humidityMapSettingsEditor;
    private Editor temperatureMapSettingsEditor;
    private Dictionary<BiomeSettings, BiomeSettingsEditor> biomeSettingsEditors;

    // Erosion settings
    private SerializedProperty erosionSettings;
    private Editor erosionSettingsEditor;

    // Mesh settings
    private SerializedProperty meshSettings;
    private Editor meshSettingsEditor;

    // Road settings
    private SerializedProperty roadSettings;
    private Editor roadSettingsEditor;

    // Always display settings
    private SerializedProperty seed;

    // Editors are reset to ensure it is written to next time OnInspectorGUI is called
    private void OnEnable() {
        myTarget = (TerrainSettings)target;
        soTarget = new SerializedObject(target);

        // Biomes settings
        humidityMapSettings = soTarget.FindProperty("humidityMapSettings");
        temperatureMapSettings = soTarget.FindProperty("temperatureMapSettings");
        transitionDistance = soTarget.FindProperty("transitionDistance");
        humidityMapSettingsEditor = null;
        temperatureMapSettingsEditor = null;
        biomeSettingsEditors = new Dictionary<BiomeSettings, BiomeSettingsEditor>();
        CreateBiomeSettingsList();
        
        // Erosion settings
        erosionSettings = soTarget.FindProperty("erosionSettings");
        erosionSettingsEditor = null;

        // Mesh settings
        meshSettings = soTarget.FindProperty("meshSettings");
        meshSettingsEditor = null;

        // Road settings
        roadSettings = soTarget.FindProperty("roadSettings");
        roadSettingsEditor = null;

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
        
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property, true);
            rect.y += EditorGUIUtility.singleLineHeight;
            
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
            
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            myTarget.biomeSettings.Add(ScriptableObject.CreateInstance("BiomeSettings") as BiomeSettings);
        };
    }   

    public override void OnInspectorGUI() {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        // Common options
        EditorGUILayout.PropertyField(seed);

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

        myTarget.toolbarBottom = GUILayout.Toolbar(myTarget.toolbarBottom, new string[] { "Rivers" });
        switch (myTarget.toolbarBottom) {
            case 0:
                myTarget.toolbarTop = -1;
                myTarget.currentTab = "Rivers";
                break;
        }

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();

        // Tab options
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
        }

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
        }
    }

    private void BiomesTab() {
        
        EditorGUILayout.PropertyField(transitionDistance);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        Common.DisplayScriptableObjectEditor(humidityMapSettings, "Humidity Settings", myTarget.humidityMapSettings, humidityMapSettingsEditor);
        Common.DisplayScriptableObjectEditor(temperatureMapSettings, "Temperature Settings", myTarget.temperatureMapSettings, temperatureMapSettingsEditor);

        EditorGUILayout.Space();
        biomeSettingsList.DoLayoutList();
    }

    private void ErosionTab() {
        Common.DisplayScriptableObjectEditor(erosionSettings, "Erosion Settings", myTarget.erosionSettings, erosionSettingsEditor);
    }

    private void MeshTab() {
        Common.DisplayScriptableObjectEditor(meshSettings, "Mesh Settings", myTarget.meshSettings, meshSettingsEditor);
    }

    private void RoadsTab() {
        Common.DisplayScriptableObjectEditor(roadSettings, "Road Settings", myTarget.roadSettings, roadSettingsEditor);
    }

    private void RiversTab() {

    }
}