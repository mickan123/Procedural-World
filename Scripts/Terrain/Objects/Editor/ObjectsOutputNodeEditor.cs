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
            SerializedProperty renderMode = serializedObject.FindProperty("renderMode");
            NodeEditorGUILayout.PropertyField(renderMode, true);
            if (node.renderMode == DetailRenderMode.GrassBillboard)
            {
                SerializedProperty detailTexture = serializedObject.FindProperty("detailTexture");
                NodeEditorGUILayout.PropertyField(detailTexture, true);
            }
            else if (node.renderMode == DetailRenderMode.VertexLit)
            {
                SerializedProperty detailPrototype = serializedObject.FindProperty("detailPrototype");
                NodeEditorGUILayout.PropertyField(detailPrototype, true);
            }
            else if (node.renderMode == DetailRenderMode.Grass)
            {
                SerializedProperty usePrototypeMesh = serializedObject.FindProperty("usePrototypeMesh");
                NodeEditorGUILayout.PropertyField(usePrototypeMesh, true);
                if (node.usePrototypeMesh)
                {
                    SerializedProperty detailPrototype = serializedObject.FindProperty("detailPrototype");
                    NodeEditorGUILayout.PropertyField(detailPrototype, true);
                }
                else
                {
                    SerializedProperty detailTexture = serializedObject.FindProperty("detailTexture");
                    NodeEditorGUILayout.PropertyField(detailTexture, true);
                }
            }
            
            SerializedProperty dryColor = serializedObject.FindProperty("dryColor");
            NodeEditorGUILayout.PropertyField(dryColor, true);
            SerializedProperty healthyColor = serializedObject.FindProperty("healthyColor");
            NodeEditorGUILayout.PropertyField(healthyColor, true);
        }
        else
        {
            SerializedProperty terrainObjects = serializedObject.FindProperty("terrainObjects");
            NodeEditorGUILayout.PropertyField(terrainObjects, true);
        }


        serializedObject.ApplyModifiedProperties();
    }
}