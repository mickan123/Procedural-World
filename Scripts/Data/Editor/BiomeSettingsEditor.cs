﻿using System.Collections;
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
    private SerializedProperty biomeGraph;
    private ReorderableList textureDataList;

    private SerializedProperty startHumidity;
    private SerializedProperty endHumidity;
    private SerializedProperty startTemperature;
    private SerializedProperty endTemperature;

    private void OnEnable()
    {
        myTarget = (BiomeSettings)target;
        soTarget = new SerializedObject(target);

        textureData = soTarget.FindProperty("textureData");
        biomeGraph = soTarget.FindProperty("biomeGraph");

        startHumidity = soTarget.FindProperty("startHumidity");
        endHumidity = soTarget.FindProperty("endHumidity");
        startTemperature = soTarget.FindProperty("startTemperature");
        endTemperature = soTarget.FindProperty("endTemperature");
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(startHumidity, true);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(endHumidity, true);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(startTemperature, true);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(endTemperature, true);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(biomeGraph, true);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        this.CreateTextureDataList();

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }

    private void CreateTextureDataList()
    {
        textureDataList = new ReorderableList(
            soTarget,
            textureData,
            true, // Draggable
            true, // Display header
            true, // Add button
            true  // Subtract butotn
        );

        textureDataList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Texture Data");
        };

        textureDataList.drawElementCallback = (Rect rect, int index, bool active, bool focused) =>
        {
            SerializedProperty property = textureDataList.serializedProperty.GetArrayElementAtIndex(index);
            Rect pos = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(pos, property);
            pos.y += EditorGUIUtility.singleLineHeight;

            if (active) 
            {
                EditorGUI.PropertyField(pos, property);
            }
        };

        textureDataList.onSelectCallback = (ReorderableList list) =>
        {
            SerializedProperty property = textureDataList.serializedProperty.GetArrayElementAtIndex(list.index);
        };

        textureDataList.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight * 2;
        };

        textureDataList.onAddCallback = (ReorderableList list) =>
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            TextureData settings = ScriptableObject.CreateInstance("TextureData") as TextureData;
            myTarget.textureData.Add(settings);
        };

        this.textureDataList.DoLayoutList();
    }
}
