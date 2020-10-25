using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(ObjectsOutputNode))]
public class ObjectsOutputNodeEditor : NodeEditor
{
    private ObjectsOutputNode node;

    public override void OnBodyGUI()
    {
        if (node == null)
        {
            node = target as ObjectsOutputNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("positionData"));

        SerializedProperty hide = serializedObject.FindProperty("hide");
        NodeEditorGUILayout.PropertyField(hide, true);

        serializedObject.ApplyModifiedProperties();
    }
}