using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Constant")]
public class HeightMapConstantNode : BiomeGraphNode
{
    [Output] public float[] heightMapOut;

    public float value = 1f;

    public override object GetValue(NodePort port)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        int width = heightMapData.width;

        float[] result = new float[width * width];

        if (port.fieldName == "heightMapOut")
        {
            for (int i = 0; i < width * width; i++)
            {
                result[i] = value;
            }
        }
        return result;
    }
}
