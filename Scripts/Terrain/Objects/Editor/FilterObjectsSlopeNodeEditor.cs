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

        SerializedProperty minSlope = serializedObject.FindProperty("minSlope");
        NodeEditorGUILayout.PropertyField(minSlope, true);
        SerializedProperty maxSlope = serializedObject.FindProperty("minSlope");
        NodeEditorGUILayout.PropertyField(maxSlope, true);

        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}