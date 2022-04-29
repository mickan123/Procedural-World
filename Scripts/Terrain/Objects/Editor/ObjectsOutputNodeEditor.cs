using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(ObjectsOutputNode))]
public class ObjectsOutputNodeEditor : NodeEditor
{
    private ObjectsOutputNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as ObjectsOutputNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        SerializedProperty hide = serializedObject.FindProperty("hide");
        NodeEditorGUILayout.PropertyField(hide, true);
        if (!node.isDetail) 
        {
            SerializedProperty generateCollider = serializedObject.FindProperty("generateCollider");
            NodeEditorGUILayout.PropertyField(generateCollider, true);
            SerializedProperty staticBatch = serializedObject.FindProperty("staticBatch");
            NodeEditorGUILayout.PropertyField(staticBatch, true);
        }
        SerializedProperty isDetail = serializedObject.FindProperty("isDetail");
        NodeEditorGUILayout.PropertyField(isDetail, true);
        if (node.isDetail)
        {
            SerializedProperty detailMode = serializedObject.FindProperty("detailMode");
            NodeEditorGUILayout.PropertyField(detailMode, true);
            SerializedProperty detailMaterials = serializedObject.FindProperty("detailMaterials");
            NodeEditorGUILayout.PropertyField(detailMaterials, true);
        }
        else
        {
            SerializedProperty terrainObjects = serializedObject.FindProperty("terrainObjects");
            NodeEditorGUILayout.PropertyField(terrainObjects, true);
        }


        serializedObject.ApplyModifiedProperties();
    }
}