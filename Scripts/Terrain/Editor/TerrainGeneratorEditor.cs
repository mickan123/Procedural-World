using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    private TerrainGenerator myTarget;
    private SerializedObject soTarget;

    private SerializedProperty viewer;
    private SerializedProperty mapMaterial;

    private SerializedProperty detailLevels;
    private SerializedProperty colliderLODIndex;

    private SerializedProperty terrainSettings;
    private Editor terrainSettingsEditor;

    private void OnEnable()
    {
        myTarget = (TerrainGenerator)target;
        soTarget = new SerializedObject(target);

        viewer = soTarget.FindProperty("viewer");
        mapMaterial = soTarget.FindProperty("mapMaterial");

        detailLevels = soTarget.FindProperty("detailLevels");
        colliderLODIndex = soTarget.FindProperty("colliderLODIndex");

        terrainSettings = soTarget.FindProperty("terrainSettings");
        terrainSettingsEditor = null;
    }

    public override void OnInspectorGUI()
    {
        soTarget.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(viewer);
        EditorGUILayout.PropertyField(mapMaterial);
        EditorGUILayout.PropertyField(detailLevels);
        EditorGUILayout.PropertyField(colliderLODIndex);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Terrain Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(terrainSettings);
        
        if (terrainSettingsEditor == null)
        {
            terrainSettingsEditor = Editor.CreateEditor(terrainSettings.objectReferenceValue as TerrainSettings);
        }
        terrainSettingsEditor.OnInspectorGUI();


        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }

    public static Editor DisplayScriptableObjectEditor(SerializedProperty property, Object targetObject, Editor targetEditor)
    {
        EditorGUILayout.PropertyField(property);
        if (property.objectReferenceValue != null)
        {
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, GUIContent.none, true, EditorStyles.foldout);
        }

        if (property.isExpanded && targetObject != null)
        {
            if (targetEditor == null)
            {
                targetEditor = Editor.CreateEditor(targetObject);
            }
            EditorGUI.indentLevel++;

            targetEditor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        return targetEditor;
    }
}