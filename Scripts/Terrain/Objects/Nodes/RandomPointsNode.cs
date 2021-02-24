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
        
        if (biomeGraph.heightMap == null)
        {
            return null;
        }

        List<Vector3> points = RandomPoints.GeneratePoints(numPoints, prng, biomeGraph.heightMap);

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