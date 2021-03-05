using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/RoadSettings")]
public class RoadSettings : ScriptableObject
{
    public static readonly int stepSize = 3;
    public static readonly int smoothness = 3; // How much to smooth path

    public TextureData roadTexture;
    public float width = 1f;
    [Range(0, 1)] public float blendFactor = 0.2f; // How much to blend road with existing terrain
    public float maxAngle = 45; // Max allowed angle for road to be textured on

#if UNITY_EDITOR

    public void OnValidate()
    {

    }

#endif
}
