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
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        float[,] heightMap = HeightMapGenerator.GenerateVeronoiMap(
            heightMapData.width,
            heightMapData.height,
            heightMapData.terrainSettings,
            heightMapData.sampleCentre,
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
