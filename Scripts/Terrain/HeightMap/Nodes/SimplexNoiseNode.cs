using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/SimplexNoise")]
public class SimplexNoiseNode : BiomeGraphNode
{
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port)
    {
        var biomeGraph = this.graph as BiomeGraph;
        if (!biomeGraph.initialized)
        {
            return null;
        }

        if (port.fieldName == "heightMap")
        {
            return GetHeightMap();
        }
        else
        {
            return null;
        }
    }

    public float[,] GetHeightMap()
    {
        var biomeGraph = this.graph as BiomeGraph;
        float[,] heightMap = HeightMapGenerator.GenerateHeightMap(
            biomeGraph.width,
            biomeGraph.height,
            this.noiseMapSettings,
            biomeGraph.terrainSettings,
            biomeGraph.sampleCentre,
            normalizeMode,
            this.noiseMapSettings.seed
        );
        return heightMap;
    }
}