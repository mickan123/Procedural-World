using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HeightMapSquareNode))]
public class HeightMapSquareNodeEditor : NodeEditor
{
    private HeightMapSquareNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as HeightMapSquareNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMapIn"));

        NodeEditorGUILayout.PortField(node.GetPort("heightMapOut"));

        serializedObject.ApplyModifiedProperties();
    }
}