using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TextureData))]
public class TextureDataEditor : ScriptlessEditor
{
    private TextureData myTarget;
    private SerializedObject soTarget;

    private SerializedProperty textureLayers;
    private ReorderableList textureLayersList;

    private void OnEnable()
    {
        myTarget = (TextureData)target;
        soTarget = new SerializedObject(target);

        CreateTextureLayersList();
    }

    private void CreateTextureLayersList()
    {
        textureLayers = soTarget.FindProperty("textureLayers");

        textureLayersList = new ReorderableList(
            soTarget,
            textureLayers,
            true, // Draggable
            true, // Display header
            true, // Add button
            true  // Subtract butotn
        );

        textureLayersList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Texture Layers Settings");
        };

        textureLayersList.drawElementCallback = (Rect rect, int index, bool active, bool focused) =>
        {
            SerializedProperty property = textureLayersList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property, true);
            EditorGUI.indentLevel--;
        };

        textureLayersList.elementHeightCallback = (int index) =>
        {
            SerializedProperty property = textureLayersList.serializedProperty.GetArrayElementAtIndex(index);
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * 8;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight * 2;
            }
        };

        textureLayersList.onAddCallback = (ReorderableList list) =>
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            // TODO add default texture
            // myTarget.terrainObjectSettings.Add(ScriptableObject.CreateInstance("TerrainObjectSettings") as TerrainObjectSettings);
        };
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        textureLayersList.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }

}
