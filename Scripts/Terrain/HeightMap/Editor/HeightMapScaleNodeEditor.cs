using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HeightMapScaleNode))]
public class HeightMapScaleNodeEditor : NodeEditor {
    private HeightMapScaleNode node;

    public override void OnBodyGUI() {
        if (node == null) {
            node = target as HeightMapScaleNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMapIn"));

        SerializedProperty scale = serializedObject.FindProperty("scale");
        NodeEditorGUILayout.PropertyField(scale, true);
        SerializedProperty heightCurve = serializedObject.FindProperty("heightCurve");
        NodeEditorGUILayout.PropertyField(heightCurve, true);

        NodeEditorGUILayout.PortField(node.GetPort("heightMapOut"));

        serializedObject.ApplyModifiedProperties();
    }
} 