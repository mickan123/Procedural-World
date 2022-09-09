using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/TerracedNoise")]
public class TerracedNoiseNode : BiomeGraphNode
{
    [Range(1, 50)] public int numTerraces;
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
        return GetTerracedHeightMap();
    }

    public float[] GetTerracedHeightMap()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        float[] heightMap = HeightMapGenerator.GenerateTerracedNoiseMap(
            heightMapData.width,
            this.noiseMapSettings,
            heightMapData.terrainSettings,
            heightMapData.sampleCentre,
            normalizeMode,
            this.numTerraces,
            this.noiseMapSettings.seed
        );

        return heightMap;
    }
}
