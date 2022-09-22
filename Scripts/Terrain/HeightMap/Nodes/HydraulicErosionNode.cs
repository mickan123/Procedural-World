using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Linq;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/HydraulicErosion")]
public class HydraulicErosionNode : BiomeGraphNode
{
    public ErosionSettings erosionSettings;

    [Input] public float[] heightMapIn;
    [Output] public float[] heightMapOut;

    public override object GetValue(NodePort port)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        if (!biomeGraph.ContainsKey(System.Threading.Thread.CurrentThread) || port.fieldName != "heightMapOut" || this.heightMapIn == null)
        {
            return null;
        }
        return ErodeHeightMap();
    }

    public float[] ErodeHeightMap()
    {
        float[] heightMap = GetInputValue<float[]>("heightMapIn", this.heightMapIn);

        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        return HydraulicErosion.Erode(heightMap, heightMapData.terrainSettings, this.erosionSettings, heightMapData.biomeInfo);
    }
}
