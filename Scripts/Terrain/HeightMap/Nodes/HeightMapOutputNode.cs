using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapOutputNode : BiomeGraphNode
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public float[,] heightMap;

    public float[,] GetValue()
    {
        return GetInputValue<float[,]>("heightMap");
    }
}
