using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HydraulicErosionNode))]
public class HydraulicErosionNodeEditor : NodeEditor {
    private HydraulicErosionNode node;

    public override void OnBodyGUI() {
        if (node == null) {
            node = target as HydraulicErosionNode;
        }
        serializedObject.Update();

        SerializedProperty erosionSettings = serializedObject.FindProperty("erosionSettings");
        erosionSettings.isExpanded = EditorGUILayout.Foldout(erosionSettings.isExpanded, "Erosion Settings", true, EditorStyles.foldout);
        if (erosionSettings.isExpanded) {
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(erosionSettings);
            NodeEditorGUILayout.PropertyField(erosionSettings, true);
            EditorGUI.indentLevel--;
        }

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        serializedObject.ApplyModifiedProperties();
    }
} 