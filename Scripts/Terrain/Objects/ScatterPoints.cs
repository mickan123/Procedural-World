using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ScatterPoints
{
    public static List<Vector3> GeneratePoints(int numRandomSpawns, float[,] heightMap)
    {
        List<Vector3> points = new List<Vector3>();
        
        int increment = 10;
        int mapSize = heightMap.GetLength(0);

        HaltonSequence haltonSequence = new HaltonSequence();

        for (int x = 0; x < mapSize; x += increment)
        {
            for (int z = 0; z < mapSize; z += increment)
            {
                for (int spawn = 0; spawn < numRandomSpawns; spawn++)
                {
                    haltonSequence.Increment();
                    Vector3 curPos = haltonSequence.m_CurrentPos * mapSize; // Scale by map size

                    float xCoord = (float)(haltonSequence.m_CurrentPos.x * increment) + x;
                    float zCoord = (float)(haltonSequence.m_CurrentPos.z * increment) + z;

                    float offset = 1f;
                    float yCoord = Common.HeightFromFloatCoord(xCoord + offset, zCoord + offset, heightMap);
                    
                    if (xCoord >= 0f && zCoord >= 0f && xCoord <= mapSize - 3 && zCoord <= mapSize - 3)
                    {
                        points.Add(new Vector3(xCoord, yCoord, zCoord));
                    }
                }
            }
        }
        return points;
    }
}
