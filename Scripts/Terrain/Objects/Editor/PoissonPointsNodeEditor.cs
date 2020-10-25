using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(PoissonPointsNode))]
public class PoissonPointsNodeEditor : NodeEditor
{
    private PoissonPointsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as PoissonPointsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        SerializedProperty varyRadius = serializedObject.FindProperty("settings.varyRadius");
        NodeEditorGUILayout.PropertyField(varyRadius, true);
        if (node.settings.varyRadius) {
            SerializedProperty minRadius = serializedObject.FindProperty("settings.minRadius");
            NodeEditorGUILayout.PropertyField(minRadius, true);
            SerializedProperty maxRadius = serializedObject.FindProperty("settings.maxRadius");
            NodeEditorGUILayout.PropertyField(maxRadius, true);
        }
        else {
            SerializedProperty radius = serializedObject.FindProperty("settings.radius");
            NodeEditorGUILayout.PropertyField(radius, true);
        }
        
        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        serializedObject.ApplyModifiedProperties();
    }
}