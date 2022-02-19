using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/PoissonPoints")]
public class PoissonPointsNode : BiomeGraphNode
{
    [Output] public ObjectPositionData positionData;

    
    public PoissonDiskSamplingSettings settings;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "positionData")
        {
            return GetPositionData();
        }
        else
        {
            return null;
        }
    }

    public ObjectPositionData GetPositionData()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];
        
        if (this.settings == null || this.settings.noiseMapSettings == null)
        {
            return null;
        }

        System.Random prng = new System.Random(this.seed);

        List<Vector3> points = PoissonDiskSampling.GeneratePoints(settings, heightMapData.sampleCentre, heightMapData.heightMap, prng, heightMapData.terrainSettings);

        float[] xCoords = new float[points.Count];
        float[] yCoords = new float[points.Count];
        float[] zCoords = new float[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            xCoords[i] = points[i].x;
            yCoords[i] = points[i].y;
            zCoords[i] = points[i].z;
        }

        ObjectPositions positions = new ObjectPositions(xCoords, yCoords, zCoords);

        return new ObjectPositionData(positions, heightMapData.heightMap);
    }
}