using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(RidgedTurbulenceNode))]
public class RidgedTurbulenceNodeEditor : NodeEditor
{
    private RidgedTurbulenceNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as RidgedTurbulenceNode;
        }
        serializedObject.Update();

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