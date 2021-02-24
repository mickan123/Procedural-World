using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/PoissonPoints")]
public class PoissonPointsNode : BiomeGraphNode
{
    [Output] public ObjectPositionData positionData;

    public bool isDetail = false;
    public GameObject[] terrainObjects;
    public Material[] detailMaterials;
    public ObjectSpawner.DetailMode detailMode;
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

        ObjectPositions positions = new ObjectPositions(points);

        if (this.isDetail)
        {
            return new ObjectPositionData(positions, biomeGraph.heightMap, detailMaterials, detailMode);
        }
        else
        {
            return new ObjectPositionData(positions, biomeGraph.heightMap, terrainObjects);
        }
    }
}