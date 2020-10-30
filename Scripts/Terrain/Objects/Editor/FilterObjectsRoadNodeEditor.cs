using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(FilterObjectsRoadNode))]
public class FilterObjectsRoadNodeEditor : NodeEditor
{
    private FilterObjectsRoadNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as FilterObjectsRoadNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionDataIn"));

        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}