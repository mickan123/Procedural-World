using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/RandomPoints")]
public class RandomPointsNode : BiomeGraphNode
{
    [Output] public ObjectPositionData positionData;

    public int pointsPerSquare; // TODO convert this to density

    public override object GetValue(NodePort port)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        if (!biomeGraph.ContainsKey(System.Threading.Thread.CurrentThread) || port.fieldName != "positionData") 
        {
            return null;
        }
        return GetPositionData();
    }

    public ObjectPositionData GetPositionData()
    {
        System.Random prng = new System.Random(this.seed);
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        int width = heightMapData.width;

        int totalRandomPoints = pointsPerSquare * (width - 2) * (width - 2);
        float[] xCoords = new float[totalRandomPoints];
        float[] yCoords = new float[totalRandomPoints];
        float[] zCoords = new float[totalRandomPoints];

        int index = 0;
        int randIdx = prng.Next(0, this.numRandomValues);
        for (int x = 0; x <= width - 3; x++)
        {
            for (int z = 0; z <= width - 3; z++)
            {
                for (int spawn = 0; spawn < pointsPerSquare; spawn++)
                {   
                    // Get random x and y coords and update index
                    float xCoord = randomValues[randIdx] + x;
                    float zCoord = randomValues[randIdx + 1] + z;
                    randIdx += 2;
                    if (randIdx + 1 >= this.numRandomValues) 
                    {
                        randIdx = 0;
                    }

                    // Only get y coord for valid x and z coords and then calculate height
                    // from the two float coords
                    if (xCoord <= width - 3 && zCoord <= width - 3)
                    {
                        float offset = 1f;
                        int xIdx = (int)(xCoord + offset);
                        int zIdx = (int)(zCoord + offset);
                        float xOffset = xCoord + offset - xIdx;
                        float zOffset = zCoord + offset - zIdx;

                        float heightNW = heightMapData.heightMap[xIdx * width + zIdx];
                        float heightNE = heightMapData.heightMap[(xIdx + 1) * width + zIdx];
                        float heightSW = heightMapData.heightMap[xIdx * width + zIdx + 1];
                        float heightSE = heightMapData.heightMap[(xIdx + 1) * width + zIdx + 1];

                        int cornerHeightIdx = (xIdx * width + zIdx) * 4;
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
        

        ObjectPositionData positionData = new ObjectPositionData(new ObjectPositions(xCoords, yCoords, zCoords), heightMapData.heightMap, width);

        // Filter all coords that are centred at 0,0 this is necessary as in the above
        // random point generation loop we only add points according to a condition which
        // isn't always true resulting in us not filling up the arrays
        for (int i = 0; i < xCoords.Length; i++)
        {
            if (xCoords[i] == 0 && zCoords[i] == 0)
            {
                positionData.positions.filtered[i] = true;
            }
        }

        return positionData;
    }
}