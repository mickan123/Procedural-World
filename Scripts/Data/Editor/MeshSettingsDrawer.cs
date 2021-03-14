using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MeshSettings))]
public class MeshSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as MeshSettings);

        var meshScale = serializedObject.FindProperty("meshScale");
        var chunkSizeIndex = serializedObject.FindProperty("chunkSizeIndex");

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), meshScale, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), chunkSizeIndex, true);
        position.y += EditorGUIUtility.singleLineHeight;

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as MeshSettings);
    
        float height = 0f;

        height += 2 * EditorGUIUtility.singleLineHeight;

        return height;
    }
}
