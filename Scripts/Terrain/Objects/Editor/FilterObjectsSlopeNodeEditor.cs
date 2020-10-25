using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(FilterObjectsSlopeNode))]
public class FilterObjectsSlopeNodeEditor : NodeEditor
{
    private FilterObjectsSlopeNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as FilterObjectsSlopeNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionDataIn"));

        SerializedProperty minAngle = serializedObject.FindProperty("minAngle");
        NodeEditorGUILayout.PropertyField(minAngle, true);
        SerializedProperty maxAngle = serializedObject.FindProperty("maxAngle");
        NodeEditorGUILayout.PropertyField(maxAngle, true);

        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}