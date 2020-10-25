using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomPoints
{
    public static List<Vector3> GeneratePoints(int numPoints, System.Random prng, float[,] heightMap)
    {
        List<Vector3> points = new List<Vector3>();

        int increment = 10;
        int mapSize = heightMap.GetLength(0);

        for (int x = 0; x < mapSize; x += increment)
        {
            for (int z = 0; z < mapSize; z += increment)
            {
                for (int spawn = 0; spawn < numPoints; spawn++)
                {
                    float xCoord = Common.NextFloat(prng, x, x + increment);
                    float zCoord = Common.NextFloat(prng, z, z + increment);

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
