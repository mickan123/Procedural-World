using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(BiomeSettings))]
public class BiomeSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        
        if (property.objectReferenceValue == null) {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as BiomeSettings);

        var textureData = serializedObject.FindProperty("textureData");
        var heightMapSettings = serializedObject.FindProperty("heightMapSettings");

        var hydraulicErosion = serializedObject.FindProperty("hydraulicErosion");
        var thermalErosion = serializedObject.FindProperty("thermalErosion");
        var allowRoads = serializedObject.FindProperty("allowRoads");

        var startHumidity = serializedObject.FindProperty("startHumidity");
        var endHumidity = serializedObject.FindProperty("endHumidity");
        var startTemperature = serializedObject.FindProperty("startTemperature");
        var endTemperature = serializedObject.FindProperty("endTemperature");

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), startHumidity, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), endHumidity, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), startTemperature, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), endTemperature, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), hydraulicErosion, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), thermalErosion, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), allowRoads, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        float textureDataHeight = EditorGUI.GetPropertyHeight(textureData, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, textureDataHeight), textureData, true);
        position.y += textureDataHeight;

        float heightMapSettingsHeight = EditorGUI.GetPropertyHeight(textureData, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, heightMapSettingsHeight), heightMapSettings, true);
        position.y += heightMapSettingsHeight;

        EditorGUI.BeginChangeCheck();

    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (property.objectReferenceValue == null) {
            return EditorGUIUtility.singleLineHeight;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as BiomeSettings);

        float height = 9 * EditorGUIUtility.singleLineHeight;

        var textureData = serializedObject.FindProperty("textureData");
        var heightMapSettings = serializedObject.FindProperty("heightMapSettings");
        
        height += EditorGUI.GetPropertyHeight(textureData, true);
        height += EditorGUI.GetPropertyHeight(heightMapSettings, true);

        return height;
    }

}
