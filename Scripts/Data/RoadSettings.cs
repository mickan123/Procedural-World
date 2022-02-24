using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/RoadSettings")]
public class RoadSettings : ScriptableObject
{
    public static readonly int stepSize = 9;
    public static readonly int smoothness = 3; // How much to smooth path

    public TextureData roadTexture;
    public float width = 1f;
    
    public float maxAngle = 45; // Max allowed angle for road to be textured on
    
    // How much to blend road with existing terrain as distance increases from centre of road
    [Range(0, 1)] public float distanceBlendFactor = 0.2f; // How much to blend road with existing terrain

    // How much to blend road with existing terrain as distance increases from centre of road
    [Range(0, 1)] public float angleBlendFactor = 0.2f; 

#if UNITY_EDITOR

    public void OnValidate()
    {

    }

#endif
}

// Equivalent struct so that the settings can be used by burst compiler
public struct RoadSettingsStruct
{   
    public float width;
    public float maxAngle;
    public float distanceBlendFactor;
    public float angleBlendFactor;

    public RoadSettingsStruct(RoadSettings roadSettings)
    {
        this.width = roadSettings.width;
        this.maxAngle = roadSettings.maxAngle;
        this.distanceBlendFactor = roadSettings.distanceBlendFactor;
        this.angleBlendFactor = roadSettings.angleBlendFactor;
    }
}