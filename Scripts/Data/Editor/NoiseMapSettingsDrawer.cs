using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NoiseMapSettings))]
public class NoiseMapSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as NoiseMapSettings);

        var noiseType = serializedObject.FindProperty("noiseType");
        var perlinNoiseSettings = serializedObject.FindProperty("perlinNoiseSettings");
        var simplexNoiseSettings = serializedObject.FindProperty("simplexNoiseSettings");

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), noiseType, true);
        position.y += EditorGUIUtility.singleLineHeight;

        if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Perlin)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), perlinNoiseSettings, true);
            position.y += EditorGUI.GetPropertyHeight(perlinNoiseSettings, true);
        }
        else if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Simplex)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), simplexNoiseSettings, true);
            position.y += EditorGUI.GetPropertyHeight(simplexNoiseSettings, true);
        }

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
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as NoiseMapSettings);

        var noiseType = serializedObject.FindProperty("noiseType");
        var perlinNoiseSettings = serializedObject.FindProperty("perlinNoiseSettings");
        var simplexNoiseSettings = serializedObject.FindProperty("simplexNoiseSettings");
        var heightMultiplier = serializedObject.FindProperty("heightMultiplier");
        var heightCurve = serializedObject.FindProperty("heightCurve");

        float height = 0f;
        if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Perlin)
        {
            height += EditorGUI.GetPropertyHeight(perlinNoiseSettings, true);
        }
        else if (noiseType.enumValueIndex == (int)NoiseMapSettings.NoiseType.Simplex)
        {
            height += EditorGUI.GetPropertyHeight(simplexNoiseSettings, true);
        }
        height += EditorGUIUtility.singleLineHeight;

        return height;
    }
}
