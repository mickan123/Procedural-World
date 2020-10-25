using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(TranslateObjectsNode))]
public class TranslateObjectsNodeEditor : NodeEditor
{
    private TranslateObjectsNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as TranslateObjectsNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionDataIn"));

        SerializedProperty randomTranslation = serializedObject.FindProperty("randomTranslation");
        NodeEditorGUILayout.PropertyField(randomTranslation, true);

        if (node.randomTranslation) {
            SerializedProperty minTranslation = serializedObject.FindProperty("minTranslation");
            NodeEditorGUILayout.PropertyField(minTranslation, true);
            SerializedProperty maxTranslation = serializedObject.FindProperty("maxTranslation");
            NodeEditorGUILayout.PropertyField(maxTranslation, true);
        }
        else {
            SerializedProperty translation = serializedObject.FindProperty("translation");
            NodeEditorGUILayout.PropertyField(translation, true);
        }
        
        NodeEditorGUILayout.PortField(node.GetPort("positionDataOut"));

        serializedObject.ApplyModifiedProperties();
    }
}