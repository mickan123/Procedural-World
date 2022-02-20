using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Output")]
public class HeightMapOutputNode : BiomeGraphNode
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public float[][] heightMap;

    public float[][] GetValue()
    {
        return GetInputValue<float[][]>("heightMap");
    }
}