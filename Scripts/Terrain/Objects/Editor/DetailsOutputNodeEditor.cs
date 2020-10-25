using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(DetailsOutputNode))]
public class DetailsOutputNodeEditor : NodeEditor
{
    private DetailsOutputNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as DetailsOutputNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        SerializedProperty hide = serializedObject.FindProperty("hide");
        NodeEditorGUILayout.PropertyField(hide, true);

        serializedObject.ApplyModifiedProperties();
    }
}