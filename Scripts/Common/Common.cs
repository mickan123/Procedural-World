using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class Common
{
    // Thread safe random float in range [begin, end]
    public static float NextFloat(System.Random prng, float begin, float end)
    {
        float value = (float)prng.NextDouble();

        value = value * (end - begin) + begin;

        return value;
    }


    public static float[,] CopyArray(float[,] reference)
    {
        float[,] array = new float[reference.GetLength(0), reference.GetLength(1)];

        for (int i = 0; i < reference.GetLength(0); i++)
        {
            for (int j = 0; j < reference.GetLength(1); j++)
            {
                array[i, j] = reference[i, j];
            }
        }

        return array;
    }

    // Copys array values from b into a
    public static void CopyArrayValues(float[,] src, float[,] dest)
    {
        for (int i = 0; i < src.GetLength(0); i++)
        {
            for (int j = 0; j < src.GetLength(1); j++)
            {
                dest[i, j] = src[i, j];
            }
        }
    }

    // Evenly smooths value from 0 to 1 in range [min, max]
    public static float SmoothRange(float value, float min, float max)
    {
        value = Mathf.Clamp(value, min, max);
        value = (value - min) / (max - min);
        return value;
    }

    public static float HeightFromFloatCoord(Vector2 coord, float[,] heightMap)
    {
        return HeightFromFloatCoord(coord.x, coord.y, heightMap);
    }

    public static float HeightFromFloatCoord(float x, float y, float[,] heightMap)
    {
        int maxIndex = heightMap.GetLength(0) - 1;
        x = Mathf.Clamp(x, 0, maxIndex);
        y = Mathf.Clamp(y, 0, maxIndex);

        int indexX = (int)x;
        int indexY = (int)y;

        x = x - indexX;
        y = y - indexY;

        float heightNW = heightMap[indexX, indexY];
        float heightNE = heightMap[Mathf.Min(indexX + 1, maxIndex), indexY];
        float heightSW = heightMap[indexX, Mathf.Min(indexY + 1, maxIndex)];
        float heightSE = heightMap[Mathf.Min(indexX + 1, maxIndex), Mathf.Min(indexY + 1, maxIndex)];

        float height = heightNW * (1 - x) * (1 - y)
                     + heightNE * x * (1 - y)
                     + heightSW * (1 - x) * y
                     + heightSE * x * y;

        return height;
    }

    public static float CalculateAngle(float xIn, float yIn, float[,] heightMap)
    {
        int coordX = (int)xIn;
        int coordZ = (int)yIn;

        int maxIndex = heightMap.GetLength(0) - 1;

        // Calculate offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = xIn - coordX;
        float y = yIn - coordZ;

        float heightNW = heightMap[coordX, coordZ];
        float heightNE = heightMap[Mathf.Min(coordX + 1, maxIndex), coordZ];
        float heightSW = heightMap[coordX, Mathf.Min(coordZ + 1, maxIndex)];
        float heightSE = heightMap[Mathf.Min(coordX + 1, maxIndex), Mathf.Min(coordZ + 1, maxIndex)];

        float heightE = heightNE * (1 - y) + heightSE * y;
        float heightW = heightNW * (1 - y) + heightSW * y;
        float heightN = heightNW * (1 - x) + heightNE * x;
        float heightS = heightSW * (1 - x) + heightSE * x;

        float height = heightNW * (1 - x) * (1 - y)
                     + heightNE * x * (1 - y)
                     + heightSW * (1 - x) * y
                     + heightSE * x * y;

        float angleN = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(
            heightN - height,
            (1 - y)
        ));
        float angleS = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(
            heightS - height,
            y
        ));
        float angleE = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(
            heightE - height,
            x
        ));
        float angleW = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(
            heightW - height,
            (1 - x)
        ));
        float averageAngle = (angleW + angleE + angleS + angleN) / 4f;

        return averageAngle;
    }

    private static readonly int[,] offsets = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

    public static float CalculateAngle(int xIn, int yIn, float[,] heightMap)
    {
        int maxIndex = heightMap.GetLength(0) - 1;

        float maxAngle = 0f;
        for (int i = 0; i < 4; i++)
        {
            float angle = AngleBetweenTwoPoints(
                xIn,
                yIn,
                Mathf.Min(Mathf.Max(xIn + offsets[i, 0], 0), maxIndex),
                Mathf.Min(Mathf.Max(yIn + offsets[i, 1], 0), maxIndex),
                heightMap
            );
            maxAngle = Mathf.Max(maxAngle, angle);
        }
        return maxAngle;
    }

    private static float AngleBetweenTwoPoints(int x1, int y1, int x2, int y2, float[,] heightMap)
    {
        float angle = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(
            heightMap[x1, y1] - heightMap[x2, y2],
            1f
        ));
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