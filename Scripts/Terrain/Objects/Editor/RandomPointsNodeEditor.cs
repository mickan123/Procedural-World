using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(RandomPointsNode))]
public class RandomPointsNodeEditor : NodeEditor
{
    private RandomPointsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as RandomPointsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        SerializedProperty numPoints = serializedObject.FindProperty("numPoints");
        NodeEditorGUILayout.PropertyField(numPoints, true);

        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        serializedObject.ApplyModifiedProperties();
    }
}