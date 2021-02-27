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
        var biomeGraph = this.graph as BiomeGraph;
        if (this.settings == null || this.settings.noiseMapSettings == null)
        {
            return null;
        }

        System.Random prng = new System.Random(this.seed);

        List<Vector3> points = PoissonDiskSampling.GeneratePoints(settings, biomeGraph.sampleCentre, biomeGraph.heightMap, prng, biomeGraph.terrainSettings);

        List<float> xCoords = new List<float>(points.Count);
        List<float> yCoords = new List<float>(points.Count);
        List<float> zCoords = new List<float>(points.Count);

        for (int i = 0; i < points.Count; i++)
        {
            xCoords.Add(points[i].x);
            yCoords.Add(points[i].y);
            zCoords.Add(points[i].z);
        }

        ObjectPositions positions = new ObjectPositions(xCoords, yCoords, zCoords);

        return new ObjectPositionData(positions, biomeGraph.heightMap);
    }
}