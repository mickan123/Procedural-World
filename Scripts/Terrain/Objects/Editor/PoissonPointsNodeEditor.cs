using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(PoissonPointsNode))]
public class PoissonPointsNodeEditor : NodeEditor
{
    private PoissonPointsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as PoissonPointsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        SerializedProperty isDetail = serializedObject.FindProperty("isDetail");
        NodeEditorGUILayout.PropertyField(isDetail, true);
        if (node.isDetail)
        {
            SerializedProperty detailMaterials = serializedObject.FindProperty("detailMaterials");
            NodeEditorGUILayout.PropertyField(detailMaterials, true);
            SerializedProperty detailMode = serializedObject.FindProperty("detailMode");
            NodeEditorGUILayout.PropertyField(detailMode, true);
        }
        else
        {
            SerializedProperty terrainObjects = serializedObject.FindProperty("terrainObjects");
            NodeEditorGUILayout.PropertyField(terrainObjects, true);
        }

        SerializedProperty varyRadius = serializedObject.FindProperty("settings.varyRadius");
        NodeEditorGUILayout.PropertyField(varyRadius, true);
        if (node.settings.varyRadius) {
            SerializedProperty minRadius = serializedObject.FindProperty("settings.minRadius");
            NodeEditorGUILayout.PropertyField(minRadius, true);
            SerializedProperty maxRadius = serializedObject.FindProperty("settings.maxRadius");
            NodeEditorGUILayout.PropertyField(maxRadius, true);
            SerializedProperty noiseMapSettings = serializedObject.FindProperty("settings.noiseMapSettings");
            EditorGUILayout.ObjectField(noiseMapSettings);
            NodeEditorGUILayout.PropertyField(noiseMapSettings, true);
        }
        else {
            SerializedProperty radius = serializedObject.FindProperty("settings.radius");
            NodeEditorGUILayout.PropertyField(radius, true);
        }
        
        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        serializedObject.ApplyModifiedProperties();
    }
}