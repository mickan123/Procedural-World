using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/RidgedTurbulence")]
public class RidgedTurbulenceNode : BiomeGraphNode
{
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[] heightMap;

    public override object GetValue(NodePort port)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        if (!biomeGraph.ContainsKey(System.Threading.Thread.CurrentThread) || port.fieldName != "heightMap")
        {
            return null;
        }
        return GetRidgedturbulanceHeightMap();
    }

    public float[] GetRidgedturbulanceHeightMap()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        float[] heightMap = HeightMapGenerator.GenerateRidgedTurbulenceMap(
            heightMapData.width,
            this.noiseMapSettings,
            heightMapData.terrainSettings,
            heightMapData.sampleCentre,
            normalizeMode,
            this.noiseMapSettings.seed
        );

        return heightMap;
    }
}
