using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HydraulicErosionNode : Node
{
    public ErosionSettings erosionSettings;

    [Input] public float[,] heightMapIn;
    [Output] public float[,] heightMapOut;

    public override object GetValue(NodePort port) {
        if (port.fieldName == "heightMapOut") {
            var temp = ErodeHeightMap();
            return temp;
        }
        else {
            return null;
        }
    }

    public float[,] ErodeHeightMap() {
        float[,] heightMapIn = GetInputValue<float[,]>("heightMapIn", this.heightMapIn);
        var heightMapGraph = this.graph as HeightMapNodeGraph;
        return null;
    }
}
