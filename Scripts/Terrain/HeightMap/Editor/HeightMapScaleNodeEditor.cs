using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HeightMapScaleNode))]
public class HeightMapScaleNodeEditor : NodeEditor
{
    private HeightMapScaleNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as HeightMapScaleNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMapIn"));

        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("scale"), true);
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("heightCurve"), true);

        NodeEditorGUILayout.PortField(node.GetPort("heightMapOut"));

        serializedObject.ApplyModifiedProperties();
    }
}