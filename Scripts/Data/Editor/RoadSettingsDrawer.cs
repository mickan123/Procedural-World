using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(RoadSettings))]
public class RoadSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as RoadSettings);

        var roadTexture = serializedObject.FindProperty("roadTexture");
        var width = serializedObject.FindProperty("width");
        var distanceBlendFactor = serializedObject.FindProperty("distanceBlendFactor");
        var angleBlendFactor = serializedObject.FindProperty("angleBlendFactor");
        var maxAngle = serializedObject.FindProperty("maxAngle");

        EditorGUI.BeginChangeCheck();

        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Texture Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), roadTexture);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), roadTexture, true);
        position.y += EditorGUI.GetPropertyHeight(roadTexture, true);
        position.y += EditorGUIUtility.singleLineHeight;
        

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Other Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), width, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), distanceBlendFactor, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), angleBlendFactor, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxAngle, true);
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
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as RoadSettings);

        float height = 0f;

        var roadTexture = serializedObject.FindProperty("roadTexture");
        height += EditorGUI.GetPropertyHeight(roadTexture, true);

        height += 9 * EditorGUIUtility.singleLineHeight;

        return height;
    }
}
