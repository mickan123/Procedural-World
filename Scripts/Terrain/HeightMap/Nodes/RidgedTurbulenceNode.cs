using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/RidgedTurbulence")]
public class RidgedTurbulenceNode : BiomeGraphNode
{
    public NoiseMapSettings noiseMapSettings;
    public HeightMapGenerator.NormalizeMode normalizeMode;

    [Output] public float[][] heightMap;

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

    public float[][] GetRidgedturbulanceHeightMap()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        float[][] heightMap = HeightMapGenerator.GenerateRidgedTurbulenceMap(
            heightMapData.width,
            heightMapData.height,
            this.noiseMapSettings,
            heightMapData.terrainSettings,
            heightMapData.sampleCentre,
            normalizeMode,
            this.noiseMapSettings.seed
        );

        return heightMap;
    }
}
