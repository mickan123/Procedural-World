using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Constant")]
public class HeightMapConstantNode : BiomeGraphNode
{
    [Output] public float[][] heightMapOut;

    public float value = 1f;

    public override object GetValue(NodePort port)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        int width = heightMapData.width;
        int height = heightMapData.height;

        float[][] result = new float[width][];
        for (int i = 0; i < width; i++)
        {
            result[i] = new float[height];
        }

        if (port.fieldName == "heightMapOut")
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    result[i][j] = value;
                }
            }
        }
        return result;
    }
}
