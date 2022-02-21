using System.Collections.Generic;
using UnityEngine;

public static class Common
{
    // Thread safe random float in range [begin, end]
    public static float NextFloat(System.Random prng, float begin, float end)
    {
        float value = (float)prng.NextDouble();

        value = value * (end - begin) + begin;

        return value;
    }

    public static float HeightFromFloatCoord(Vector2 coord, float[][] heightMap)
    {
        return HeightFromFloatCoord(coord.x, coord.y, heightMap);
    }

    public static float HeightFromFloatCoord(float x, float y, float[][] heightMap)
    {
        // Technically subtracting 0.001f reduces slightly incorrect results however
        // this means we don't have to do any bounds checking in later steps so the
        // slight accuracy loss is worth it for the performance
        float maxIndex = heightMap.Length - 1.001f;
        x = (x < maxIndex) ? x : maxIndex;
        y = (y < maxIndex) ? y : maxIndex;
        
        int indexX = (int)x;
        int indexY = (int)y;

        x = x - indexX;
        y = y - indexY;

        float heightNW = heightMap[indexX][indexY];
        float heightNE = heightMap[indexX + 1][indexY];
        float heightSW = heightMap[indexX][indexY + 1];
        float heightSE = heightMap[indexX + 1][indexY + 1];

        float height = heightNW * (1 - x) * (1 - y)
                     + heightNE * x * (1 - y)
                     + heightSW * (1 - x) * y
                     + heightSE * x * y;

        return height;
    }

    public static float[][] CalculateAngles(float[][] heightMap)
    {   
        int mapSize = heightMap.Length;

        // Create a padded heightmap so we don't have to check bounds
        // when calculating angles
        float[][] paddedHeightMap = new float[mapSize + 1][];
        for (int i = 0; i < mapSize + 1; i++)
        {
            paddedHeightMap[i] = new float[mapSize + 1];
        }
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                paddedHeightMap[i][j] = heightMap[i][j];
            }
        }
        for (int i = 0; i < mapSize; i++)
        {
            paddedHeightMap[i][mapSize] = heightMap[i][mapSize - 1];
            paddedHeightMap[mapSize][i] = heightMap[mapSize - 1][i];
        }

        // Construct angles array
        float[][] angles = new float[mapSize][];
        for (int i = 0 ; i < mapSize; i++) 
        {
            angles[i] = new float[mapSize];
        }

        // Calculate angle at every element in heightmap
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                float height = heightMap[x][y];

                // Compute the differentials by stepping over 1 in both directions.
                float dx = paddedHeightMap[x + 1][y] - height;
                dx = dx > 0 ? dx : -dx;

                float dy = paddedHeightMap[x][y + 1] - height;
                dy = dy > 0 ? dy : -dy;

                float dMax = dx > dy ? dx : dy;
                angles[x][y] = Mathf.Rad2Deg * Mathf.Atan2(
                    dMax, 
                    1
                );
            }
        }

        return angles;
    }

    // Calculates slopes as opposed to angles, this is useful as angles
    // require many expensive Atan2 operations
    public static float[][] CalculateSlopes(float[][] heightMap)
    {   
        
        int mapSize = heightMap.Length;

        // Create a padded heightmap so we don't have to check bounds
        // when calculating angles
        float[][] paddedHeightMap = new float[mapSize + 1][];
        for (int i = 0; i < mapSize + 1; i++)
        {
            paddedHeightMap[i] = new float[mapSize + 1];
        }
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                paddedHeightMap[i][j] = heightMap[i][j];
            }
        }
        for (int i = 0; i < mapSize; i++)
        {
            paddedHeightMap[i][mapSize] = heightMap[i][mapSize - 1];
            paddedHeightMap[mapSize][i] = heightMap[mapSize - 1][i];
        }

        // Construct slopes array
        float[][] slopes = new float[mapSize][];
        for (int i = 0 ; i < mapSize; i++) 
        {
            slopes[i] = new float[mapSize];
        }

        // Calculate slope at every element in heightmap
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                float height = paddedHeightMap[x][y];

                // Compute the differentials by stepping over 1 in both directions.
                float dx = paddedHeightMap[x + 1][y] - height;
                dx = dx > 0 ? dx : -dx;

                float dy = paddedHeightMap[x][y + 1] - height;
                dy = dy > 0 ? dy : -dy;

                float dMax = dx > dy ? dx : dy;
                slopes[x][y] = dMax;
            }
        }

        return slopes;
    }

    private static readonly int[,] offsets = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

    public static float CalculateAngle(int xIn, int yIn, float[][] heightMap)
    {
        int maxIndex = heightMap.Length - 1;

        float maxAngle = 0f;
        for (int i = 0; i < 4; i++)
        {
            int x2 = xIn + offsets[i, 0];
            x2 = x2 >= 0 ? x2 : 0;
            x2 = x2 > maxIndex ? maxIndex : x2;

            int y2 = xIn + offsets[i, 0];
            y2 = y2 >= 0 ? y2 : 0;
            y2 = y2 > maxIndex ? maxIndex : y2;

            float angle = AngleBetweenTwoPoints(
                xIn,
                yIn,
                x2,
                y2,
                heightMap
            );
            maxAngle = maxAngle > angle ? maxAngle : angle;
        }
        return maxAngle;
    }

    private static float AngleBetweenTwoPoints(int x1, int y1, int x2, int y2, float[][] heightMap)
    {
        float angle = Mathf.Rad2Deg * Mathf.Atan2(
            heightMap[x1][y1] - heightMap[x2][y2],
            1f
        );
        angle = angle > 0 ? angle : -angle; // Get abs value
        return angle;
    }

    public static float DistanceFromLine(Vector2 point, Vector2 origin, Vector2 direction)
    {
        return DistanceFromLine(
            new Vector3(point.x, 0f, point.y),
            new Vector3(origin.x, 0f, origin.y),
            new Vector3(direction.x, 0f, direction.y)
        );
    }

    // Finds the distance of a point from a line of origin and direction
    public static float DistanceFromLine(Vector3 point, Vector3 origin, Vector3 direction)
    {
        Ray ray = new Ray(origin, direction);
        float distance = Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        return distance;
    }

    public static Vector2 GetClosestPoint(List<Vector2> points, Vector2 pos)
    {
        float minDist = float.MaxValue;
        Vector2 closestPoint = new Vector2(0f, 0f);

        for (int i = 0; i < points.Count; i++)
        {
            float dist = Vector2.Distance(points[i], pos);
            if (dist < minDist)
            {
                minDist = dist;
                closestPoint = points[i];
            }
        }

        return closestPoint;
    }

    public static Vector2 GetSecondClosestPoint(List<Vector2> points, Vector2 pos)
    {
        float minDist = float.MaxValue;
        float secondMinDist = float.MaxValue;
        Vector2 closestPoint = new Vector2(0f, 0f);
        Vector2 secondClosestPoint = new Vector2(0f, 0f);

        for (int i = 0; i < points.Count; i++)
        {
            float dist = Vector2.Distance(points[i], pos);
            if (dist < minDist)
            {
                secondMinDist = dist;
                minDist = dist;
                secondClosestPoint = closestPoint;
                closestPoint = points[i];
            }
        }

        return secondClosestPoint;
    }

    public static Vector2 GetCentreOfPoints(List<Vector2> points)
    {
        Vector2 centre = new Vector2(0f, 0f);
        for (int i = 0; i < points.Count; i++)
        {
            centre += points[i];
        }
        return centre /= points.Count;
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static void Shuffle<T>(this IList<T> list, IList<T> listb, IList<T> listc)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;

            value = listb[k];
            listb[k] = listb[n];
            listb[n] = value;

            value = listc[k];
            listc[k] = listc[n];
            listc[n] = value;
        }
    }
}