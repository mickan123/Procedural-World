using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(ScaleObjectsNode))]
public class ScaleObjectsNodeEditor : NodeEditor
{
    private ScaleObjectsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as ScaleObjectsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionDataIn"));

        SerializedProperty uniformScale = serializedObject.FindProperty("uniformScale");
        NodeEditorGUILayout.PropertyField(uniformScale, true);

        SerializedProperty randomScale = serializedObject.FindProperty("randomScale");
        NodeEditorGUILayout.PropertyField(randomScale, true);

        if (node.randomScale && node.uniformScale)
        {
            SerializedProperty minScaleUniform = serializedObject.FindProperty("minScaleUniform");
            NodeEditorGUILayout.PropertyField(minScaleUniform, true);
            SerializedProperty maxScaleUniform = serializedObject.FindProperty("maxScaleUniform");
            NodeEditorGUILayout.PropertyField(maxScaleUniform, true);
        }
        else if (node.randomScale && !node.uniformScale)
        {
            SerializedProperty minScaleNonUniform = serializedObject.FindProperty("minScaleNonUniform");
            NodeEditorGUILayout.PropertyField(minScaleNonUniform, true);
            SerializedProperty maxScaleNonUniform = serializedObject.FindProperty("maxScaleNonUniform");
            NodeEditorGUILayout.PropertyField(maxScaleNonUniform, true);
        }
        else if (!node.randomScale && node.uniformScale)
        {
            SerializedProperty scale = serializedObject.FindProperty("scale");
            NodeEditorGUILayout.PropertyField(scale, true);
        }
        else if (!node.randomScale && !node.uniformScale)
        {
            SerializedProperty nonUniformScale = serializedObject.FindProperty("nonUniformScale");
            NodeEditorGUILayout.PropertyField(nonUniformScale, true);
        }

        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}