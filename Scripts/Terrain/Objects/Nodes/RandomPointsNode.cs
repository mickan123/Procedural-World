using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/RandomPoints")]
public class RandomPointsNode : BiomeGraphNode
{
    [Output] public ObjectPositionData positionData;

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

        int increment = 10;
        int mapSize = biomeGraph.heightMap.GetLength(0);

        int totalRandomPoints = numPoints * (mapSize / increment + 1) * (mapSize / increment + 1);
        List<float> xCoords = new List<float>(totalRandomPoints);
        List<float> yCoords = new List<float>(totalRandomPoints);
        List<float> zCoords = new List<float>(totalRandomPoints);

        for (int x = 0; x <= mapSize - 3; x += increment)
        {
            for (int z = 0; z <= mapSize - 3; z += increment)
            {
                float maxRandVal = Mathf.Min(x + increment, mapSize - 3);
                for (int spawn = 0; spawn < numPoints; spawn++)
                {
                    float xCoord = Common.NextFloat(prng, x, Mathf.Min(x + increment, mapSize - 3));
                    float zCoord = Common.NextFloat(prng, z, Mathf.Min(z + increment, mapSize - 3));

                    float offset = 1f;
                    float yCoord = Common.HeightFromFloatCoord(xCoord + offset, zCoord + offset, biomeGraph.heightMap);

                    xCoords.Add(xCoord);
                    yCoords.Add(yCoord);
                    zCoords.Add(zCoord);
                }
            }
        }

        return new ObjectPositionData(new ObjectPositions(xCoords, yCoords, zCoords), biomeGraph.heightMap);
    }
}