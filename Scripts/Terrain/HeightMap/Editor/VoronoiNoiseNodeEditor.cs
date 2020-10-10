using UnityEngine;
using UnityEditor;
using XNodeEditor;

[CustomNodeEditor(typeof(VoronoiNoiseNode))]
public class VoronoiNoiseNodeEditor : NodeEditor {
    private VoronoiNoiseNode node;

    public override void OnBodyGUI() {
        if (node == null) {
            node = target as VoronoiNoiseNode;
        }
        serializedObject.Update();
        
        SerializedProperty normalizeMode = serializedObject.FindProperty("normalizeMode");
        NodeEditorGUILayout.PropertyField(normalizeMode, true);
        SerializedProperty voronoiMode = serializedObject.FindProperty("voronoiMode");
        NodeEditorGUILayout.PropertyField(voronoiMode, true);
        SerializedProperty numVoronoiPolygons = serializedObject.FindProperty("numVoronoiPolygons");
        NodeEditorGUILayout.PropertyField(numVoronoiPolygons, true);
        SerializedProperty numLloydsIterations = serializedObject.FindProperty("numLloydsIterations");
        NodeEditorGUILayout.PropertyField(numLloydsIterations, true);

        if (voronoiMode.enumValueIndex == (int)HeightMapGenerator.VoronoiMode.Cracks) {
            SerializedProperty voronoiCrackWidth = serializedObject.FindProperty("voronoiCrackWidth");
            NodeEditorGUILayout.PropertyField(voronoiCrackWidth, true);
        }
        
        NodeEditorGUILayout.PortField(node.GetPort("heightMap"));

        serializedObject.ApplyModifiedProperties();
    }
} 