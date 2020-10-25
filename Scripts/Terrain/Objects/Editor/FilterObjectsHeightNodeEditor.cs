using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(FilterObjectsHeightNode))]
public class FilterObjectsHeightNodeEditor : NodeEditor
{
    private FilterObjectsHeightNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as FilterObjectsHeightNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionDataIn"));

        SerializedProperty minHeight = serializedObject.FindProperty("minHeight");
        NodeEditorGUILayout.PropertyField(minHeight, true);
        SerializedProperty maxHeight = serializedObject.FindProperty("maxHeight");
        NodeEditorGUILayout.PropertyField(maxHeight, true);
        SerializedProperty heightProbabilityCurve = serializedObject.FindProperty("heightProbabilityCurve");
        NodeEditorGUILayout.PropertyField(heightProbabilityCurve, true);

        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}