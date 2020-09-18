using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

    // Erosion settings
    private SerializedProperty erosionSettings;

    // Mesh settings
    private SerializedProperty meshSettings;

    // Road settings
    private SerializedProperty roadSettings;

    // Always display settings
    private SerializedProperty seed;

    private void OnEnable() {
        myTarget = (TerrainSettings)target;
        soTarget = new SerializedObject(target);

        // Biomes settings
        humidityMapSettings = soTarget.FindProperty("humidityMapSettings");
        temperatureMapSettings = soTarget.FindProperty("temperatureMapSettings");
        transitionDistance = soTarget.FindProperty("transitionDistance");
        biomeSettings = soTarget.FindProperty("biomeSettings");

        // Erosion settings
        erosionSettings = soTarget.FindProperty("erosionSettings");

        // Mesh settings
        meshSettings = soTarget.FindProperty("meshSettings");

        // Road settings
        roadSettings = soTarget.FindProperty("roadSettings");

        // Always display settings
        seed = soTarget.FindProperty("seed");
    }

    public override void OnInspectorGUI() {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        myTarget.toolbarTop = GUILayout.Toolbar(myTarget.toolbarTop, new string[] { "Biomes", "Erosion", "Mesh", "Road"});
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
                myTarget.currentTab = "Road";
                break;            
        }

        // myTarget.toolbarBottom = GUILayout.Toolbar(myTarget.toolbarBottom, new string[] { "Button5", "Button6", "Button7", "Button8"});
        // switch (myTarget.toolbarBottom) {
        //     case 0:
        //         myTarget.toolbarTop = -1;
        //         myTarget.currentTab = "e";
        //         break;
        //     case 1:
        //         myTarget.toolbarTop = -1;
        //         myTarget.currentTab = "f";
        //         break;
        //     case 2:
        //         myTarget.toolbarTop = -1;
        //         myTarget.currentTab = "g";
        //         break;
        //     case 3: 
        //         myTarget.toolbarTop = -1;
        //         myTarget.currentTab = "h";
        //         break;            
        // }

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(seed);
        switch (myTarget.currentTab) {
            case "Biomes":
                EditorGUILayout.PropertyField(transitionDistance);
                EditorGUILayout.PropertyField(humidityMapSettings);
                EditorGUILayout.PropertyField(temperatureMapSettings);
                EditorGUILayout.PropertyField(biomeSettings);
                break;
            case "Erosion":
                EditorGUILayout.PropertyField(erosionSettings);
                break;
            case "Mesh":
                EditorGUILayout.PropertyField(meshSettings);
                break;
            case "Road":
                EditorGUILayout.PropertyField(roadSettings);
                break;
        }

        if (EditorGUI.EndChangeCheck()) {
            soTarget.ApplyModifiedProperties();
        }
    }
}
