using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class TerracedNoiseNode: Node
{

    [Range(1, 50)] public int numTerraces;
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port) {
        if (port.fieldName == "heightMap") {
            return GetTerracedHeightMap();
        }
        else {
            return null;
        }
    }

    public float[,] GetTerracedHeightMap() {
        var heightMapGraph = this.graph as HeightMapNodeGraph;
        float[,] heightMap =  HeightMapGenerator.GenerateTerracedNoiseMap(
            heightMapGraph.width,
            heightMapGraph.height,
            this.noiseMapSettings,
            heightMapGraph.terrainSettings,
            heightMapGraph.sampleCentre,
            normalizeMode,
            this.numTerraces,
            this.noiseMapSettings.seed
        );

        return heightMap;
    }
}
