using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(TerracedNoiseNode))]
public class TerracedNoiseNodeEditor : NodeEditor
{
    private TerracedNoiseNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as TerracedNoiseNode;
        }
        serializedObject.Update();

        SerializedProperty numTerraces = serializedObject.FindProperty("numTerraces");
        NodeEditorGUILayout.PropertyField(numTerraces, true);

        SerializedProperty normalizeMode = serializedObject.FindProperty("normalizeMode");
        NodeEditorGUILayout.PropertyField(normalizeMode, true);

        SerializedProperty noiseMapSettings = serializedObject.FindProperty("noiseMapSettings");
        noiseMapSettings.isExpanded = EditorGUILayout.Foldout(noiseMapSettings.isExpanded, "Height Map Settings", true, EditorStyles.foldout);
        if (noiseMapSettings.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(noiseMapSettings);
            NodeEditorGUILayout.PropertyField(noiseMapSettings, true);
            EditorGUI.indentLevel--;
        }

        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        serializedObject.ApplyModifiedProperties();
    }
}