using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/MeshSettings")]
public class MeshSettings : ScriptableObject
{
    public const int numSupportedLODs = 5;
    public const int numSupportedChunkSizes = 11;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240, 480, 960 };

    public int meshScale = 1;

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;

#if UNITY_EDITOR

    public void OnValidate()
    {
        // Placeholder for custom validation for mesh settings
    }

#endif

    // Num vertices per line of a mesh rendered at LOD = 0. Includes the two extra vertices that are excluded from final mesh, but used for calculating normals. 
    public int numVerticesPerLine
    {
        get
        {
            return supportedChunkSizes[chunkSizeIndex] + 5; // +2 for correct normal +2 for high res border +1 as we dealing with squares
        }
    }

    public int meshWorldSize
    {
        get
        {
            return (numVerticesPerLine - 3) * meshScale;
        }
    }

}
