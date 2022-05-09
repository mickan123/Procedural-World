using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

public static class Common
{
    // Thread safe random float in range [begin, end]
    public static float NextFloat(System.Random prng, float begin, float end)
    {
        float value = (float)prng.NextDouble();

        value = value * (end - begin) + begin;

        return value;
    }

    public static float HeightFromFloatCoord(Vector2 coord, float[] heightMap, int width)
    {
        return HeightFromFloatCoord(coord.x, coord.y, heightMap, width);
    }

    public static float HeightFromFloatCoord(TerrainData terrainData, float x, float y, int width)
    {
        float maxIndex = width - 1.001f;
        x = (x < maxIndex) ? x : maxIndex;
        y = (y < maxIndex) ? y : maxIndex;
        
        int indexX = (int)x;
        int indexY = (int)y;

        x = x - indexX;
        y = y - indexY;

        float heightNW = terrainData.GetHeight(indexX, indexY);
        float heightNE = terrainData.GetHeight(indexX + 1, indexY);
        float heightSW = terrainData.GetHeight(indexX, indexY + 1);
        float heightSE = terrainData.GetHeight(indexX + 1, indexY + 1);

        float height = heightNW * (1 - x) * (1 - y)
                     + heightNE * x * (1 - y)
                     + heightSW * (1 - x) * y
                     + heightSE * x * y;

        return height;
    }

    public static float HeightFromFloatCoord(float x, float y, float[] heightMap, int width)
    {
        // Technically subtracting 0.001f reduces slightly incorrect results however
        // this means we don't have to do any bounds checking in later steps so the
        // slight accuracy loss is worth it for the performance
        float maxIndex = width - 1.001f;
        x = (x < maxIndex) ? x : maxIndex;
        y = (y < maxIndex) ? y : maxIndex;
        
        int indexX = (int)x;
        int indexY = (int)y;

        x = x - indexX;
        y = y - indexY;

        float heightNW = heightMap[indexX * width + indexY];
        float heightNE = heightMap[(indexX + 1) * width + indexY];
        float heightSW = heightMap[indexX * width + indexY + 1];
        float heightSE = heightMap[(indexX + 1) * width + indexY + 1];

        float height = heightNW * (1 - x) * (1 - y)
                     + heightNE * x * (1 - y)
                     + heightSW * (1 - x) * y
                     + heightSE * x * y;

        return height;
    }

    public static float HeightFromFloatCoord(float x, float y, NativeArray<float> heightMap, int width)
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

        float heightNW = heightMap[indexX * width + indexY];
        float heightNE = heightMap[(indexX + 1) * width + indexY];
        float heightSW = heightMap[indexX * width + indexY + 1];
        float heightSE = heightMap[(indexX + 1) * width + indexY + 1];

        float height = heightNW * (1 - x) * (1 - y)
                     + heightNE * x * (1 - y)
                     + heightSW * (1 - x) * y
                     + heightSE * x * y;

        return height;
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct CalculateAnglesJob : IJob
    {
        [ReadOnly] public NativeArray<float> heightMap;
        [WriteOnly] public NativeArray<float> angles;

        public int width;
        public float scale;

        public void Execute()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    float height = heightMap[x * width + y];

                    int dxIdx1 = math.max((x - 1), 0) * width + y;
                    int dxIdx2 = math.min((x + 1), width - 1) * width + y;
                    float dx = math.abs(heightMap[dxIdx1] - heightMap[dxIdx2]);

                    int dyIdx1 = x * width + math.max(y - 1, 0);
                    int dyIdx2 = x * width + math.min(y + 1, width - 1);
                    float dy = math.abs(heightMap[dyIdx1] - heightMap[dyIdx2]);

                    float dMax = math.max(dx, dy);
                    angles[x * width + y] = math.degrees(math.atan2(
                        dMax, 
                        2 * scale
                    ));
                }
            }
        }
    }


    // Calculates slopes as opposed to angles, this is useful as angles
    // require many expensive Atan2 operations
    public static float[] CalculateSlopes(float[] heightMap, int width)
    {   
        // Create a padded heightmap so we don't have to check bounds
        // when calculating angles
        int paddedWidth = width + 1;
        float[] paddedHeightMap = new float[paddedWidth * paddedWidth];
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                paddedHeightMap[i * paddedWidth + j] = heightMap[i * width + j];
            }
        }
        for (int i = 0; i < width; i++)
        {
            paddedHeightMap[i * paddedWidth + width] = heightMap[i * width + width - 1];
            paddedHeightMap[width * paddedWidth + i] = heightMap[(width - 1) * width + i];
        }

        // Construct slopes array
        float[] slopes = new float[width * width];

        // Calculate slope at every element in heightmap
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                float height = paddedHeightMap[x * paddedWidth + y];

                // Compute the differentials by stepping over 1 in both directions.
                float dx = paddedHeightMap[(x + 1) * paddedWidth + y] - height;
                dx = dx > 0 ? dx : -dx;

                float dy = paddedHeightMap[x * paddedWidth + y + 1] - height;
                dy = dy > 0 ? dy : -dy;

                float dMax = dx > dy ? dx : dy;
                slopes[x * width + y] = dMax;
            }
        }

        return slopes;
    }

    private static readonly int[,] offsets = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

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

    public static void FadeEdgeHeightMap(float[] originalHeightMap, float[] finalHeightMap, int width, float blendDistance = 5f)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float nearDist = i < j ? i : j;
                float farDist = width - 1 - (i > j ? i : j);
                float distFromEdge = nearDist < farDist ? nearDist : farDist;
                distFromEdge = distFromEdge - 3f < 0f ? 0f : distFromEdge - 3f ;
                float edgeMultiplier = distFromEdge / blendDistance < 1f ? distFromEdge / blendDistance :1f;
                finalHeightMap[i * width + j] = edgeMultiplier * finalHeightMap[i * width + j] + (1f - edgeMultiplier) * originalHeightMap[i * width + j];
            }
        }
    }
}