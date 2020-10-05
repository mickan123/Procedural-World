using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class RidgedTurbulenceNode: Node
{
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port) {
        if (port.fieldName == "heightMap") {
            return GetRidgedturbulanceHeightMap();
        }
        else {
            return null;
        }
    }

    public float[,] GetRidgedturbulanceHeightMap() {
        var heightMapGraph = this.graph as HeightMapNodeGraph;
        float[,] heightMap =  HeightMapGenerator.GenerateRidgedTurbulenceMap(
            heightMapGraph.width,
            heightMapGraph.height,
            this.noiseMapSettings,
            heightMapGraph.terrainSettings,
            heightMapGraph.sampleCentre,
            normalizeMode,
            this.noiseMapSettings.seed
        );

        return heightMap;
    }
}
