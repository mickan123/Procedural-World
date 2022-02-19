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
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];
        
        if (heightMapData.heightMap == null)
        {
            return null;
        }

        int increment = 10;
        int mapSize = heightMapData.heightMap.GetLength(0);

        int totalRandomPoints = numPoints * (mapSize / increment + 1) * (mapSize / increment + 1);
        float[] xCoords = new float[totalRandomPoints];
        float[] yCoords = new float[totalRandomPoints];
        float[] zCoords = new float[totalRandomPoints];

        int index = 0;
        for (int x = 0; x <= mapSize - 3; x += increment)
        {
            for (int z = 0; z <= mapSize - 3; z += increment)
            {
                for (int spawn = 0; spawn < numPoints; spawn++)
                {
                    // Don't optimize this random value generation to ensure we don't have to check if its within
                    // range as otherwise we add too many points to edge of chunk
                    float xCoord = Common.NextFloat(prng, x, x + increment);
                    float zCoord = Common.NextFloat(prng, z, z + increment);

                    float offset = 1f;
                    float yCoord = Common.HeightFromFloatCoord(xCoord + offset, zCoord + offset, heightMapData.heightMap);

                    if (xCoord <= mapSize - 3 && zCoord <= mapSize - 3)
                    {
                        xCoords[index] = xCoord;
                        yCoords[index] = yCoord;
                        zCoords[index] = zCoord;
                        index += 1;
                    }
                }
            }
        }

        return new ObjectPositionData(new ObjectPositions(xCoords, yCoords, zCoords), heightMapData.heightMap);
    }
}