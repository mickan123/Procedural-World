using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/SimplexNoise")]
public class SimplexNoiseNode : BiomeGraphNode
{
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[] heightMap;

    public override object GetValue(NodePort port)
    {
        var biomeGraph = this.graph as BiomeGraph;
        if (port.fieldName != "heightMap" || !biomeGraph.ContainsKey(System.Threading.Thread.CurrentThread))
        {
            return null;
        }
        return GetHeightMap();
    }

    public float[] GetHeightMap()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        float[] heightMap = HeightMapGenerator.GenerateHeightMap(
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