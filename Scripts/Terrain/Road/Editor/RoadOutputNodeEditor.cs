using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(RoadOutputNode))]
public class RoadOutputNodeEditor : NodeEditor
{
    private RoadOutputNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as RoadOutputNode;
        }
        serializedObject.Update();

        SerializedProperty roadSettings = serializedObject.FindProperty("roadSettings");
        EditorGUILayout.ObjectField(roadSettings);
        NodeEditorGUILayout.PropertyField(roadSettings, true);

        serializedObject.ApplyModifiedProperties();
    }
}