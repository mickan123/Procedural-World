using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HeightMapConstantNode))]
public class HeightMapConstantNodeEditor : NodeEditor
{
    private HeightMapConstantNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as HeightMapConstantNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("value"), true);

        NodeEditorGUILayout.PortField(node.GetPort("heightMapOut"));

        serializedObject.ApplyModifiedProperties();
    }
}