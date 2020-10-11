using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TerrainObjectSettings))]
public class TerrainObjectSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        
        if (property.objectReferenceValue == null) {
            return;
        }
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as TerrainObjectSettings);

        EditorGUI.BeginChangeCheck();

        var terrainObjects = serializedObject.FindProperty("terrainObjects");
        var spawnMode = serializedObject.FindProperty("spawnMode");
        var numRandomSpawns = serializedObject.FindProperty("numRandomSpawns");

        var varyRadius = serializedObject.FindProperty("varyRadius");
        var radius = serializedObject.FindProperty("radius");
        var minRadius = serializedObject.FindProperty("minRadius");
        var maxRadius = serializedObject.FindProperty("maxRadius");
        var noiseMapSettings = serializedObject.FindProperty("noiseMapSettings");

        var constrainHeight = serializedObject.FindProperty("constrainHeight");
        var minHeight = serializedObject.FindProperty("minHeight");
        var maxHeight = serializedObject.FindProperty("maxHeight");
        var heightProbabilityCurve = serializedObject.FindProperty("heightProbabilityCurve");

        var constrainSlope = serializedObject.FindProperty("constrainSlope");
        var minSlope = serializedObject.FindProperty("minSlope");
        var maxSlope = serializedObject.FindProperty("maxSlope");

        var uniformScale = serializedObject.FindProperty("uniformScale");
        var randomScale = serializedObject.FindProperty("randomScale");
        var scale = serializedObject.FindProperty("scale");
        var nonUniformScale = serializedObject.FindProperty("nonUniformScale");
        var minScaleNonUniform = serializedObject.FindProperty("minScaleNonUniform");
        var maxScaleNonUniform = serializedObject.FindProperty("maxScaleNonUniform");
        var minScaleUniform = serializedObject.FindProperty("minScaleUniform");
        var maxScaleUniform = serializedObject.FindProperty("maxScaleUniform");

        var randomTranslation = serializedObject.FindProperty("randomTranslation");
        var translation = serializedObject.FindProperty("translation");
        var minTranslation = serializedObject.FindProperty("minTranslation");
        var maxTranslation = serializedObject.FindProperty("maxTranslation");

        var randomRotation = serializedObject.FindProperty("randomRotation");
        var rotation = serializedObject.FindProperty("rotation");
        var minRotation = serializedObject.FindProperty("minRotation");
        var maxRotation = serializedObject.FindProperty("maxRotation");

        var hide = serializedObject.FindProperty("hide");
        var spawnOnRoad = serializedObject.FindProperty("spawnOnRoad");

        position.y += EditorGUIUtility.singleLineHeight;

        
        float terrainObjectsHeight = EditorGUI.GetPropertyHeight(terrainObjects, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, terrainObjectsHeight), terrainObjects, true);
        position.y += terrainObjectsHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, terrainObjectsHeight), spawnMode, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (spawnMode.enumValueIndex == (int)TerrainObjectSettings.SpawnMode.Random) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), numRandomSpawns, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
        
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Radius", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), varyRadius, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (varyRadius.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minRadius, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxRadius, true);
            position.y += EditorGUIUtility.singleLineHeight;
            
            EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), noiseMapSettings);
            noiseMapSettings.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), 
                                                            noiseMapSettings.isExpanded, 
                                                            GUIContent.none, 
                                                            true, 
                                                            EditorStyles.foldout);
            position.y += EditorGUIUtility.singleLineHeight;
            if (noiseMapSettings.isExpanded) {
                EditorGUI.indentLevel++;
                
                float noiseMapHeight = EditorGUI.GetPropertyHeight(noiseMapSettings, true);
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, noiseMapHeight), noiseMapSettings, true);
                position.y += noiseMapHeight;
                EditorGUI.indentLevel--;
            }
        }
        else {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), radius, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
        
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Height", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), constrainHeight, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (constrainHeight.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minHeight, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxHeight, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), heightProbabilityCurve, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Slope", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), constrainSlope, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (constrainSlope.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minSlope, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxSlope, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Scale", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), uniformScale, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), randomScale, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (uniformScale.boolValue && !randomScale.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), scale, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (uniformScale.boolValue && randomScale.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minScaleUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxScaleUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (!uniformScale.boolValue && randomScale.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minScaleNonUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxScaleNonUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (!uniformScale.boolValue && !randomScale.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), nonUniformScale, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Translation", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), randomTranslation, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (randomTranslation.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minTranslation, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxTranslation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), translation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Rotation", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), randomRotation, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (randomRotation.boolValue) {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minRotation, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxRotation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), rotation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Other", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), hide, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), spawnOnRoad, true);
        position.y += EditorGUIUtility.singleLineHeight;
        position.y += EditorGUIUtility.singleLineHeight;

        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (property.objectReferenceValue == null) {
            return EditorGUIUtility.singleLineHeight;
        }
        
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as TerrainObjectSettings);

        float height = 0;

        var terrainObjects = serializedObject.FindProperty("terrainObjects");
        var spawnMode = serializedObject.FindProperty("spawnMode");
        height += EditorGUI.GetPropertyHeight(terrainObjects, true) + 2 * EditorGUIUtility.singleLineHeight;
        if (spawnMode.enumValueIndex == (int)TerrainObjectSettings.SpawnMode.Random) {
            height += EditorGUIUtility.singleLineHeight;
        }

        var varyRadius = serializedObject.FindProperty("varyRadius");
        height += (varyRadius.boolValue) ? 6f * EditorGUIUtility.singleLineHeight : 4f * EditorGUIUtility.singleLineHeight;

        var constrainHeight = serializedObject.FindProperty("constrainHeight");
        height += (constrainHeight.boolValue) ? 6f * EditorGUIUtility.singleLineHeight : 3f * EditorGUIUtility.singleLineHeight;

        var constrainSlope = serializedObject.FindProperty("constrainSlope");
        height += (constrainSlope.boolValue) ? 5f * EditorGUIUtility.singleLineHeight : 3f * EditorGUIUtility.singleLineHeight;

        var randomScale = serializedObject.FindProperty("randomScale");
        height += (randomScale.boolValue) ? 6f * EditorGUIUtility.singleLineHeight : 5f * EditorGUIUtility.singleLineHeight;

        var randomTranslation = serializedObject.FindProperty("randomTranslation");
        height += (randomTranslation.boolValue) ? 5f * EditorGUIUtility.singleLineHeight : 4f * EditorGUIUtility.singleLineHeight;

        var randomRotation = serializedObject.FindProperty("randomRotation");
        height += (randomRotation.boolValue) ? 5f * EditorGUIUtility.singleLineHeight : 4f * EditorGUIUtility.singleLineHeight;

        height += 4f * EditorGUIUtility.singleLineHeight;

        var noiseMapSettings = serializedObject.FindProperty("noiseMapSettings");
        if (noiseMapSettings.isExpanded) {
            height += EditorGUI.GetPropertyHeight(noiseMapSettings, true);
        }

        return height;
    }
}   
