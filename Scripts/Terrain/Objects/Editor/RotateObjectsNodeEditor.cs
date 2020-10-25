using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(RotateObjectsNode))]
public class RotateObjectsNodeEditor : NodeEditor
{
    private RotateObjectsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as RotateObjectsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionDataIn"));

        SerializedProperty randomRotation = serializedObject.FindProperty("randomRotation");
        NodeEditorGUILayout.PropertyField(randomRotation, true);

        if (node.randomRotation) {
            SerializedProperty minRotation = serializedObject.FindProperty("minRotation");
            NodeEditorGUILayout.PropertyField(minRotation, true);
            SerializedProperty maxRotation = serializedObject.FindProperty("maxRotation");
            NodeEditorGUILayout.PropertyField(maxRotation, true);
        }
        else {
            SerializedProperty rotation = serializedObject.FindProperty("rotation");
            NodeEditorGUILayout.PropertyField(rotation, true);
        }
        
        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}