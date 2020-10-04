using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class SimplexNoiseNode : Node
{
    public NoiseMapSettings noiseMapSettings;

    [Output] public HeightMapWrapper heightMap;
    
    public override object GetValue(NodePort port) {
        if (port.fieldName == "heightMap") {
            var temp = GetHeightMap();
            return temp;
        }
        else {
            return null;
        }
    }

    public HeightMapWrapper GetHeightMap() {
        var heightMapGraph = this.graph as HeightMapNodeGraph;
        float[,] heightMap =  HeightMapGenerator.GenerateHeightMap(
            heightMapGraph.width,
            heightMapGraph.height,
            this.noiseMapSettings,
            heightMapGraph.terrainSettings,
            heightMapGraph.sampleCentre,
            HeightMapGenerator.NormalizeMode.GlobalBiome,
            this.noiseMapSettings.seed
        );
        return new HeightMapWrapper(heightMap);
    }
}