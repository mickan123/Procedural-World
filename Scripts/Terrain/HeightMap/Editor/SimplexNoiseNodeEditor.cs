using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(SimplexNoiseNode))]
public class SimplexNoiseNodeEditor : NodeEditor {
    private SimplexNoiseNode node;

    public override void OnBodyGUI() {
        if (node == null) {
            node = target as SimplexNoiseNode;
        }
        serializedObject.Update();
        
        SerializedProperty noiseMapSettings = serializedObject.FindProperty("noiseMapSettings");
        noiseMapSettings.isExpanded = EditorGUILayout.Foldout(noiseMapSettings.isExpanded, "Height Map Settings", true, EditorStyles.foldout);
        if (noiseMapSettings.isExpanded) {
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(noiseMapSettings);
            NodeEditorGUILayout.PropertyField(noiseMapSettings, true);
            EditorGUI.indentLevel--;
        }

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        serializedObject.ApplyModifiedProperties();
    }
} 