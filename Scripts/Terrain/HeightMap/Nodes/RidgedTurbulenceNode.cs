using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/RidgedTurbulence")]
public class RidgedTurbulenceNode : BiomeGraphNode
{
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[,] heightMap;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "heightMap")
        {
            return GetRidgedturbulanceHeightMap();
        }
        else
        {
            return null;
        }
    }

    public float[,] GetRidgedturbulanceHeightMap()
    {
        var biomeGraph = this.graph as BiomeGraph;
        float[,] heightMap = HeightMapGenerator.GenerateRidgedTurbulenceMap(
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
