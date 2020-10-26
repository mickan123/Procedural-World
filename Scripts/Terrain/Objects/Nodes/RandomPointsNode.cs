using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/RandomPoints")]
public class RandomPointsNode : BiomeGraphNode
{
    [Output] public ObjectPositionData positionData;

    public bool isDetail = false;
    public GameObject[] terrainObjects;
    public Material[] detailMaterials;
    public ObjectSpawner.DetailMode detailMode;
    public int numPoints; // TODO convert this to density

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
        System.Random prng = new System.Random(this.seed);
        var biomeGraph = this.graph as BiomeGraph;

        List<Vector3> points = RandomPoints.GeneratePoints(numPoints, prng, biomeGraph.heightMap);

        List<ObjectPosition> positions = new List<ObjectPosition>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            positions.Add(new ObjectPosition(points[i]));
        }

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