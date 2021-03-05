using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(BiomeSettings))]
public class BiomeSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as BiomeSettings);

        var textureData = serializedObject.FindProperty("textureData");
        var slopeTextureData = serializedObject.FindProperty("slopeTextureData");
        var slopeThreshold = serializedObject.FindProperty("slopeThreshold");
        var slopeBlendRange = serializedObject.FindProperty("slopeBlendRange");
        var heightMapSettings = serializedObject.FindProperty("heightMapSettings");

        var startHumidity = serializedObject.FindProperty("startHumidity");
        var endHumidity = serializedObject.FindProperty("endHumidity");
        var startTemperature = serializedObject.FindProperty("startTemperature");
        var endTemperature = serializedObject.FindProperty("endTemperature");

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), startHumidity, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), endHumidity, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), startTemperature, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), endTemperature, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        float textureDataHeight = EditorGUI.GetPropertyHeight(textureData, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, textureDataHeight), textureData, true);
        position.y += textureDataHeight;

        float slopeTextureDataHeight = EditorGUI.GetPropertyHeight(slopeTextureData, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, slopeTextureDataHeight), slopeTextureData, true);
        position.y += slopeTextureDataHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), slopeThreshold, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), slopeBlendRange, true);
        position.y += EditorGUIUtility.singleLineHeight;

        float heightMapSettingsHeight = EditorGUI.GetPropertyHeight(textureData, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, heightMapSettingsHeight), heightMapSettings, true);
        position.y += heightMapSettingsHeight;

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
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as BiomeSettings);

        float height = 8 * EditorGUIUtility.singleLineHeight;

        var textureData = serializedObject.FindProperty("textureData");
        var slopeTextureData = serializedObject.FindProperty("slopeTextureData");
        var heightMapSettings = serializedObject.FindProperty("heightMapSettings");

        height += EditorGUI.GetPropertyHeight(textureData, true);
        height += EditorGUI.GetPropertyHeight(slopeTextureData, true);
        height += EditorGUI.GetPropertyHeight(heightMapSettings, true);

        return height;
    }
}
