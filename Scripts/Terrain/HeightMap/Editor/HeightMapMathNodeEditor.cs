using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(HeightMapMathNode))]
public class HeightMapMathNodeEditor : NodeEditor {
    private HeightMapMathNode node;

    public override void OnBodyGUI() {
        if (node == null) {
            node = target as HeightMapMathNode;
        }
        serializedObject.Update();

        NodeEditorGUILayout.PortField(node.GetPort("heightMapInA"));
        NodeEditorGUILayout.PortField(node.GetPort("heightMapInB"));
        
        SerializedProperty mathType = serializedObject.FindProperty("mathType");
        NodeEditorGUILayout.PropertyField(mathType, true);
        
        NodeEditorGUILayout.PortField(node.GetPort("heightMapOut"));

        serializedObject.ApplyModifiedProperties();
    }
} 