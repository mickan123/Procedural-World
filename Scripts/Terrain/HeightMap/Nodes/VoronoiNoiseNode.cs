using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/VoronoiNoise")]
public class VoronoiNoiseNode : BiomeGraphNode
{
    public HeightMapGenerator.NormalizeMode normalizeMode;
    public HeightMapGenerator.VoronoiMode voronoiMode;
    [Range(1, 1000)] public int numVoronoiPolygons = 100;
    [Range(1, 1000)] public int numLloydsIterations = 5;
    [Range(0, 200)] public float voronoiCrackWidth = 4f;

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "heightMap")
        {
            return GetVoronoiNoiseHeightMap();
        }
        else
        {
            return null;
        }
    }

    public float[,] GetVoronoiNoiseHeightMap()
    {
        var biomeGraph = this.graph as BiomeGraph;
        float[,] heightMap = HeightMapGenerator.GenerateVeronoiMap(
            biomeGraph.width,
            biomeGraph.height,
            biomeGraph.terrainSettings,
            biomeGraph.sampleCentre,
            normalizeMode,
            voronoiMode,
            numVoronoiPolygons,
            numLloydsIterations,
            voronoiCrackWidth,
            this.seed
        );
        return heightMap;
    }
}
