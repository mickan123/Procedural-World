using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class TerracedNoiseNode : BiomeGraphNode
{
    [Range(1, 50)] public int numTerraces;
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "heightMap")
        {
            return GetTerracedHeightMap();
        }
        else
        {
            return null;
        }
    }

    public float[,] GetTerracedHeightMap()
    {
        var biomeGraph = this.graph as BiomeGraph;
        float[,] heightMap = HeightMapGenerator.GenerateTerracedNoiseMap(
            biomeGraph.width,
            biomeGraph.height,
            this.noiseMapSettings,
            biomeGraph.terrainSettings,
            biomeGraph.sampleCentre,
            normalizeMode,
            this.numTerraces,
            this.noiseMapSettings.seed
        );

        return heightMap;
    }
}
