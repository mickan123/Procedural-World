using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(RandomPointsNode))]
public class RandomPointsNodeEditor : NodeEditor
{
    private RandomPointsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as RandomPointsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        SerializedProperty numPoints = serializedObject.FindProperty("numPoints");
        NodeEditorGUILayout.PropertyField(numPoints, true);

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

        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        serializedObject.ApplyModifiedProperties();
    }
}