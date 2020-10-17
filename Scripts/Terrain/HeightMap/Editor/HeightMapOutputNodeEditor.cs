using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HeightMapOutputNode))]
public class HeightMapOutputNodeEditor : NodeEditor
{
    private HeightMapOutputNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as HeightMapOutputNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        serializedObject.ApplyModifiedProperties();
    }
}