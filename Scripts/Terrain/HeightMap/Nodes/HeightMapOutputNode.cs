using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapOutputNode : Node
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public HeightMapWrapper heightMap;
    
    public float[,] GetValue() {
        HeightMapWrapper heightMapWrapper = GetInputValue<HeightMapWrapper>("heightMap");
        return heightMapWrapper.heightMap;
    }
}
