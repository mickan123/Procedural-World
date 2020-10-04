using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HydraulicErosionNode : Node
{
    public ErosionSettings erosionSettings;

    [Input] public HeightMapWrapper heightMapIn;
    [Output] public HeightMapWrapper heightMapOut;

    public override object GetValue(NodePort port) {
        if (port.fieldName == "heightMapOut") {
            var temp = ErodeHeightMap();
            return temp;
        }
        else {
            return null;
        }
    }

    public HeightMapWrapper ErodeHeightMap() {
        HeightMapWrapper heightMapIn = GetInputValue<HeightMapWrapper>("heightMapIn", this.heightMapIn);
        var heightMapGraph = this.graph as HeightMapNodeGraph;
        return new HeightMapWrapper(null);
    }
}
