using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class VoronoiNoiseNode: Node
{
    public HeightMapGenerator.NormalizeMode normalizeMode;
    public HeightMapGenerator.VoronoiMode voronoiMode;
    [Range(1, 1000)] public int numVoronoiPolygons = 100;
    [Range(1, 1000)] public int numLloydsIterations = 5;
    [Range(0, 200)] public float voronoiCrackWidth = 4f;

    [HideInInspector] public int seed; // Initialised by terrain settings

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port) {
        if (port.fieldName == "heightMap") {
            return GetVoronoiNoiseHeightMap();
        }
        else {
            return null;
        }
    }

    public float[,] GetVoronoiNoiseHeightMap() {
        var heightMapGraph = this.graph as HeightMapNodeGraph;
        float[,] heightMap =  HeightMapGenerator.GenerateVeronoiMap(
            heightMapGraph.width,
            heightMapGraph.height,
            heightMapGraph.terrainSettings,
            heightMapGraph.sampleCentre,
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
