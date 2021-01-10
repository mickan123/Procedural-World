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

    private float minWidth = 0.02f;

#if UNITY_EDITOR

    public void OnValidate()
    {
        // Ensure values are in correct range
        this.startHumidity = Mathf.Clamp(this.startHumidity, 0f, 1f - minWidth);
        this.endHumidity = Mathf.Clamp(this.endHumidity, minWidth, 1f);
        this.startTemperature = Mathf.Clamp(this.startTemperature, 0f, 1f - minWidth);
        this.endTemperature = Mathf.Clamp(this.endTemperature, minWidth, 1f);

        // Ensure min < max
        this.startHumidity = Mathf.Min(this.startHumidity, this.endHumidity - minWidth);
        this.endHumidity = Mathf.Max(this.endHumidity, this.startHumidity + minWidth);
        this.startTemperature = Mathf.Min(this.startTemperature, this.endTemperature - minWidth);
        this.endTemperature = Mathf.Max(this.endTemperature, this.startTemperature + minWidth);
        
        if (textureData != null)
        {
            textureData.OnValidate();
        }
    }

#endif

}
