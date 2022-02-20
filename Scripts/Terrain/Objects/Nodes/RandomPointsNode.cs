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
        int randIdx = prng.Next(0, this.numRandomValues);
        for (int x = 0; x <= mapSize - 3; x += increment)
        {
            for (int z = 0; z <= mapSize - 3; z += increment)
            {
                for (int spawn = 0; spawn < numPoints; spawn++)
                {   
                    // Get random x and y coords and update index
                    float xCoord = randomValues[randIdx] * increment + x;
                    float zCoord = randomValues[randIdx + 1] * increment + z;
                    randIdx += 2;
                    if (randIdx + 1 >= this.numRandomValues) 
                    {
                        randIdx = 0;
                    }

                    // Only get y coord for valid x and z coords and then calculate height
                    // from the two float coords
                    if (xCoord <= mapSize - 3 && zCoord <= mapSize - 3)
                    {
                        float offset = 1f;
                        int xIdx = (int)(xCoord + offset);
                        int zIdx = (int)(zCoord + offset);
                        float xOffset = xCoord + offset - xIdx;
                        float zOffset = zCoord + offset - zIdx;

                        float heightNW = heightMapData.heightMap[xIdx, zIdx];
                        float heightNE = heightMapData.heightMap[xIdx + 1, zIdx];
                        float heightSW = heightMapData.heightMap[xIdx, zIdx + 1];
                        float heightSE = heightMapData.heightMap[xIdx + 1, zIdx + 1];

                        int cornerHeightIdx = (xIdx * mapSize + zIdx) * 4;
                        float yCoord = heightNW * (1 - xOffset) * (1 - zOffset)
                            + heightNE * xOffset * (1 - zOffset)
                            + heightSW * (1 - xOffset) * zOffset
                            + heightSE * xOffset * zOffset;

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