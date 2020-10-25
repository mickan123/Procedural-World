using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/BiomeSettings")]
public class BiomeSettings : ScriptableObject
{
    public TextureData textureData;
    public TextureData slopeTextureData;
    public float angleThreshold = 30;
    public float angleBlendRange = 15;

    public BiomeGraph biomeGraph;

    public bool hydraulicErosion = true;
    public bool thermalErosion = true;

    public bool allowRoads = true;

    [Range(0, 1)] public float startHumidity;
    [Range(0, 1)] public float endHumidity;
    [Range(0, 1)] public float startTemperature;
    [Range(0, 1)] public float endTemperature;

#if UNITY_EDITOR

    public void OnValidate()
    {
        if (textureData != null)
        {
            textureData.OnValidate();
        }
    }

#endif

}
