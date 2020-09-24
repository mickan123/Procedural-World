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
    private SerializedProperty heightMapSettings;
    private SerializedProperty terrainObjectSettings; 
    private ReorderableList textureDataList;
    private ReorderableList terrainObjectSettingsList;
    private TextureDataEditor textureDataEditor;
    private Dictionary<TerrainObjectSettings, TerrainObjectSettingsEditor> terrainObjectSettingsEditors;

    private SerializedProperty hydraulicErosion;
    private SerializedProperty thermalErosion;
    private SerializedProperty allowRoads; 

    private SerializedProperty startHumidity;
    private SerializedProperty endHumidity;
    private SerializedProperty startTemperature; 
    private SerializedProperty endTemperature;

    private void OnEnable() {
        myTarget = (BiomeSettings)target;
        soTarget = new SerializedObject(target);

        textureData = soTarget.FindProperty("textureData");
        heightMapSettings = soTarget.FindProperty("heightMapSettings");
        textureDataEditor = null;
        terrainObjectSettingsEditors = new Dictionary<TerrainObjectSettings, TerrainObjectSettingsEditor>();
        CreateTerrainObjectSettingsList();

        hydraulicErosion = soTarget.FindProperty("hydraulicErosion");
        thermalErosion = soTarget.FindProperty("thermalErosion");
        allowRoads = soTarget.FindProperty("allowRoads");

        startHumidity = soTarget.FindProperty("startHumidity");
        endHumidity = soTarget.FindProperty("endHumidity");
        startTemperature = soTarget.FindProperty("startTemperature");
        endTemperature = soTarget.FindProperty("endTemperature");
        
    }

    private void CreateTerrainObjectSettingsList() {
        terrainObjectSettings = soTarget.FindProperty("terrainObjectSettings");

        terrainObjectSettingsList = new ReorderableList(
            soTarget, 
            terrainObjectSettings,
            true, // Draggable
            true, // Display header
            true, // Add button
            true  // Subtract butotn
        );

        terrainObjectSettingsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Terrain Object Settings");
        };

        terrainObjectSettingsList.drawElementCallback = (Rect rect, int index, bool active, bool focused) => {
            SerializedProperty property = terrainObjectSettingsList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.indentLevel++;
            EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property);
            property.isExpanded = EditorGUI.Foldout(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property.isExpanded, GUIContent.none, true, EditorStyles.foldout);
            rect.y += EditorGUIUtility.singleLineHeight;
            if (property.isExpanded) {
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property, true);
            }
            EditorGUI.indentLevel--;
        };

        terrainObjectSettingsList.elementHeightCallback = (int index) => {
            SerializedProperty property = terrainObjectSettingsList.serializedProperty.GetArrayElementAtIndex(index);
            if (property.isExpanded) {
                return EditorGUI.GetPropertyHeight(property, true) + 2 * EditorGUIUtility.singleLineHeight;
            } else {
                return 2 * EditorGUIUtility.singleLineHeight;
            }
        };

        terrainObjectSettingsList.onAddCallback = (ReorderableList list) => {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            
            myTarget.terrainObjectSettings.Add(ScriptableObject.CreateInstance("TerrainObjectSettings") as TerrainObjectSettings);
        };
    }

    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(startHumidity, true);
        EditorGUILayout.PropertyField(endHumidity, true);
        EditorGUILayout.PropertyField(startTemperature, true);
        EditorGUILayout.PropertyField(endTemperature, true);

        EditorGUILayout.PropertyField(hydraulicErosion, true);
        EditorGUILayout.PropertyField(thermalErosion, true);
        EditorGUILayout.PropertyField(allowRoads, true);
        EditorGUILayout.Space();

        Common.DisplayScriptableObjectEditor(textureData, myTarget.textureData, textureDataEditor);

        heightMapSettings.isExpanded = EditorGUILayout.Foldout(heightMapSettings.isExpanded, "Height Map Settings", true, EditorStyles.foldout);
        if (heightMapSettings.isExpanded) {
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(heightMapSettings);
            EditorGUILayout.PropertyField(heightMapSettings, true);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        terrainObjectSettingsList.DoLayoutList();

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
        }
    }
}
