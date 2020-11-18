using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Linq;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/HydraulicErosion")]
public class HydraulicErosionNode : BiomeGraphNode
{
    public ErosionSettings erosionSettings;

    [Input] public float[,] heightMapIn;
    [Output] public float[,] heightMapOut;

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

    public float[,] ErodeHeightMap()
    {
        float[,] heightMap = GetInputValue<float[,]>("heightMapIn", this.heightMapIn);

        var biomeGraph = this.graph as BiomeGraph;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        return HydraulicErosion.Erode(heightMap, biomeGraph.terrainSettings, biomeGraph.biomeInfo, biomeGraph.worldManager, biomeGraph.sampleCentre);
    }
}
