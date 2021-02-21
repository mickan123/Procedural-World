using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TextureData))]
public class TextureDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as TextureData);
        
        var texture = serializedObject.FindProperty("texture");
        var tint = serializedObject.FindProperty("tint");

        var tintStrength = serializedObject.FindProperty("tintStrength");
        var blendStrength = serializedObject.FindProperty("blendStrength");

        var textureScale = serializedObject.FindProperty("textureScale");

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), texture, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), tint, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), tintStrength, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), blendStrength, true);
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), textureScale, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

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
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as TextureData);

        float height = 5 * EditorGUIUtility.singleLineHeight;

        return height;
    }
}
