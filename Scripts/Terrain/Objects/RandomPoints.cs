using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomPoints
{
    public static List<Vector3> GeneratePoints(int numPoints, System.Random prng, float[,] heightMap)
    {
        int increment = 10;
        int mapSize = heightMap.GetLength(0);

        int totalRandomPoints = numPoints * (mapSize / increment + 1) * (mapSize / increment + 1);
        List<Vector3> points = new List<Vector3>(totalRandomPoints);

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
                    float yCoord = Common.HeightFromFloatCoord(xCoord + offset, zCoord + offset, heightMap);

                    points.Add(new Vector3(xCoord, yCoord, zCoord));
                }
            }
        }
        return points;
    }
}
