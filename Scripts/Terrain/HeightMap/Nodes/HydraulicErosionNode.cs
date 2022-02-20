using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Linq;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/HydraulicErosion")]
public class HydraulicErosionNode : BiomeGraphNode
{
    public ErosionSettings erosionSettings;

    [Input] public float[][] heightMapIn;
    [Output] public float[][] heightMapOut;

    private static readonly int[,] neighBouroffsets = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "heightMapOut")
        {
            return ErodeHeightMap();
        }
        else
        {
            return null;
        }
    }

    public float[][] ErodeHeightMap()
    {
        float[][] heightMap = GetInputValue<float[][]>("heightMapIn", this.heightMapIn);

        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        int width = heightMap.Length;
        int height = heightMap[0].Length;  

        return HydraulicErosion.Erode(heightMap, heightMapData.terrainSettings, this.erosionSettings, heightMapData.biomeInfo);
    }
}
