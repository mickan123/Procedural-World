using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TerrainObjectSettings))]
public class TerrainObjectSettingsDrawer : PropertyDrawer
{
    private SerializedProperty terrainObjects;
    private SerializedProperty spawnMode;
    private SerializedProperty numRandomSpawns;

    // Detail vars
    private SerializedProperty isDetail;
    private SerializedProperty detailMode;
    private SerializedProperty detailViewDistance;
    private SerializedProperty detailTexture;

    // Radius vars
    private SerializedProperty varyRadius;
    private SerializedProperty radius;
    private SerializedProperty minRadius;
    private SerializedProperty maxRadius;
    private SerializedProperty noiseMapSettings;

    // Height vars
    private SerializedProperty constrainHeight;
    private SerializedProperty minHeight;
    private SerializedProperty maxHeight;
    private SerializedProperty heightProbabilityCurve;

    // Slope vars
    private SerializedProperty constrainSlope;
    private SerializedProperty minSlope;
    private SerializedProperty maxSlope;

    // Scale vars
    private SerializedProperty uniformScale;
    private SerializedProperty randomScale;
    private SerializedProperty scale;
    private SerializedProperty nonUniformScale;
    private SerializedProperty minScaleNonUniform;
    private SerializedProperty maxScaleNonUniform;
    private SerializedProperty minScaleUniform;
    private SerializedProperty maxScaleUniform;

    // Translation vars
    private SerializedProperty randomTranslation;
    private SerializedProperty translation;
    private SerializedProperty minTranslation;
    private SerializedProperty maxTranslation;

    // Rotation vars
    private SerializedProperty randomRotation;
    private SerializedProperty rotation;
    private SerializedProperty minRotation;
    private SerializedProperty maxRotation;

    // Other vars
    private SerializedProperty spawnOnRoad;
    private SerializedProperty hide;


    private void InitVars(SerializedObject soTarget)
    {
        terrainObjects = soTarget.FindProperty("terrainObjects");
        spawnMode = soTarget.FindProperty("spawnMode");
        numRandomSpawns = soTarget.FindProperty("numRandomSpawns");

        isDetail = soTarget.FindProperty("isDetail");
        detailMode = soTarget.FindProperty("detailMode");
        detailViewDistance = soTarget.FindProperty("detailViewDistance");
        detailTexture = soTarget.FindProperty("detailTexture");

        varyRadius = soTarget.FindProperty("varyRadius");
        radius = soTarget.FindProperty("radius");
        minRadius = soTarget.FindProperty("minRadius");
        maxRadius = soTarget.FindProperty("maxRadius");
        noiseMapSettings = soTarget.FindProperty("noiseMapSettings");

        constrainHeight = soTarget.FindProperty("constrainHeight");
        minHeight = soTarget.FindProperty("minHeight");
        maxHeight = soTarget.FindProperty("maxHeight");
        heightProbabilityCurve = soTarget.FindProperty("heightProbabilityCurve");

        constrainSlope = soTarget.FindProperty("constrainSlope");
        minSlope = soTarget.FindProperty("minSlope");
        maxSlope = soTarget.FindProperty("maxSlope");

        uniformScale = soTarget.FindProperty("uniformScale");
        randomScale = soTarget.FindProperty("randomScale");
        scale = soTarget.FindProperty("scale");
        nonUniformScale = soTarget.FindProperty("nonUniformScale");
        minScaleNonUniform = soTarget.FindProperty("minScaleNonUniform");
        maxScaleNonUniform = soTarget.FindProperty("maxScaleNonUniform");
        minScaleUniform = soTarget.FindProperty("minScaleUniform");
        maxScaleUniform = soTarget.FindProperty("maxScaleUniform");

        randomTranslation = soTarget.FindProperty("randomTranslation");
        translation = soTarget.FindProperty("translation");
        minTranslation = soTarget.FindProperty("minTranslation");
        maxTranslation = soTarget.FindProperty("maxTranslation");

        randomRotation = soTarget.FindProperty("randomRotation");
        rotation = soTarget.FindProperty("rotation");
        minRotation = soTarget.FindProperty("minRotation");
        maxRotation = soTarget.FindProperty("maxRotation");

        hide = soTarget.FindProperty("hide");
        spawnOnRoad = soTarget.FindProperty("spawnOnRoad");
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return;
        }
        TerrainObjectSettings terainObjectSettings = property.objectReferenceValue as TerrainObjectSettings;
        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as TerrainObjectSettings);

        this.InitVars(serializedObject);

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), isDetail, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;

        if (terainObjectSettings.isDetail)
        {
            DetailObjectSettings(ref position);
        }
        else
        {
            MeshObjectSettings(ref position);
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void MeshObjectSettings(ref Rect position)
    {
        this.GameObjectSettings(ref position);
        this.SpawnRadiusSettings(ref position);
        this.HeightSettings(ref position);
        this.SlopeSettings(ref position);
        this.ScaleSettings(ref position);
        this.TranslationSettings(ref position);
        this.RotationSettings(ref position);
        this.OtherSettings(ref position);
    }

    private void DetailObjectSettings(ref Rect position)
    {
        this.DetailOnlySettings(ref position);
        this.GameObjectSettings(ref position);
        this.HeightSettings(ref position);
        this.SlopeSettings(ref position);
        this.ScaleSettings(ref position);
        this.OtherSettings(ref position);
    }

    private void DetailOnlySettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Detail Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        float detailTextureHeight = EditorGUI.GetPropertyHeight(detailTexture, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, detailTextureHeight), detailTexture, true);
        position.y += detailTextureHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), detailMode, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;
    }

    private void GameObjectSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Object Settings", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        float terrainObjectsHeight = EditorGUI.GetPropertyHeight(terrainObjects, true);
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, terrainObjectsHeight), terrainObjects, true);
        position.y += terrainObjectsHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), spawnMode, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (spawnMode.enumValueIndex == (int)TerrainObjectSettings.SpawnMode.Random)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), numRandomSpawns, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
    }

    private void SpawnRadiusSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Radius", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), varyRadius, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (varyRadius.boolValue)
        {
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
            if (noiseMapSettings.isExpanded)
            {
                EditorGUI.indentLevel++;

                float noiseMapHeight = EditorGUI.GetPropertyHeight(noiseMapSettings, true);
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, noiseMapHeight), noiseMapSettings, true);
                position.y += noiseMapHeight;
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), radius, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
    }

    private void HeightSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Height", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), constrainHeight, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (constrainHeight.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minHeight, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxHeight, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), heightProbabilityCurve, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
    }

    private void SlopeSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Slope", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), constrainSlope, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (constrainSlope.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minSlope, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxSlope, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
    }

    private void ScaleSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Scale", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), uniformScale, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), randomScale, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (uniformScale.boolValue && !randomScale.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), scale, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (uniformScale.boolValue && randomScale.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minScaleUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxScaleUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (!uniformScale.boolValue && randomScale.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minScaleNonUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxScaleNonUniform, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (!uniformScale.boolValue && !randomScale.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), nonUniformScale, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += EditorGUIUtility.singleLineHeight;
    }

    private void TranslationSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Translation", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), randomTranslation, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (randomTranslation.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minTranslation, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxTranslation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), translation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += 2 * EditorGUIUtility.singleLineHeight;
    }

    private void RotationSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Rotation", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), randomRotation, true);
        position.y += EditorGUIUtility.singleLineHeight;
        if (randomRotation.boolValue)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), minRotation, true);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), maxRotation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), rotation, true);
            position.y += EditorGUIUtility.singleLineHeight;
        }
        position.y += 2 * EditorGUIUtility.singleLineHeight;
    }

    private void OtherSettings(ref Rect position)
    {
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Other", EditorStyles.boldLabel);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), hide, true);
        position.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), spawnOnRoad, true);
        position.y += 2 * EditorGUIUtility.singleLineHeight;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.objectReferenceValue == null)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue as TerrainObjectSettings);
        TerrainObjectSettings terrainObjectSettings = property.objectReferenceValue as TerrainObjectSettings;

        float detailSettingsHeight = (terrainObjectSettings.isDetail) ? 5 * EditorGUIUtility.singleLineHeight : 0f;
        float varyRadiusSettingsHeight = ((terrainObjectSettings.varyRadius) ? 6f : 4f) * EditorGUIUtility.singleLineHeight;
        float constrainHeightSettingsHeight = ((terrainObjectSettings.constrainHeight) ? 6f : 3f) * EditorGUIUtility.singleLineHeight;
        float constrainSlopeSettingsHeight = ((terrainObjectSettings.constrainSlope) ? 5f : 3f) * EditorGUIUtility.singleLineHeight;
        float scaleSettingsHeight = ((terrainObjectSettings.randomScale) ? 6f : 5f) * EditorGUIUtility.singleLineHeight;
        float translationSettingsHeight = ((terrainObjectSettings.randomTranslation) ? 6f : 5f) * EditorGUIUtility.singleLineHeight;
        float rotationSettingsHeight = ((terrainObjectSettings.randomRotation) ? 6f : 5f) * EditorGUIUtility.singleLineHeight;
        float otherSettingsHeight = 4f * EditorGUIUtility.singleLineHeight;

        var terrainObjects = serializedObject.FindProperty("terrainObjects");
        float terrainObjectsSettingsHeight = EditorGUI.GetPropertyHeight(terrainObjects, true) + 3 * EditorGUIUtility.singleLineHeight;

        var noiseMapSettings = serializedObject.FindProperty("noiseMapSettings");
        float noiseMapSettingsHeight = (noiseMapSettings.isExpanded) ? EditorGUI.GetPropertyHeight(noiseMapSettings, true) : 0f;

        var spawnMode = serializedObject.FindProperty("spawnMode");
        float spawnModeSettingsHeight = 0f;
        if (spawnMode.enumValueIndex == (int)TerrainObjectSettings.SpawnMode.Random)
        {
            spawnModeSettingsHeight += EditorGUIUtility.singleLineHeight;
        }

        float height = 2 * EditorGUIUtility.singleLineHeight;
        if (terrainObjectSettings.isDetail)
        {
            height += detailSettingsHeight
                   + terrainObjectsSettingsHeight
                   + constrainHeightSettingsHeight
                   + constrainSlopeSettingsHeight
                   + scaleSettingsHeight
                   + otherSettingsHeight;
        }
        else
        {
            height += varyRadiusSettingsHeight
                   + constrainHeightSettingsHeight
                   + constrainSlopeSettingsHeight
                   + scaleSettingsHeight
                   + translationSettingsHeight
                   + rotationSettingsHeight
                   + otherSettingsHeight
                   + terrainObjectsSettingsHeight
                   + noiseMapSettingsHeight
                   + spawnModeSettingsHeight;
        }

        return height;
    }
}
