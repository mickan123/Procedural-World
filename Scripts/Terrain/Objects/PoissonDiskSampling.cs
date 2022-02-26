using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Collections.Generic;

public static class PoissonDiskSampling
{

    [BurstCompile]
    public struct PoissonDiskSamplingJob : IJob
    {
        public NativeArray<float> heightMap;
        public NativeArray<float> spawnNoiseMap;

        public NativeList<Vector3> points;

        public Vector2 sampleCentre;

        public int meshScale;
        public int numSamplesBeforeRejection;

        // Poisson Disk Sampling settings
        public bool varyRadius;
        public float radius;
        public float minRadius;
        public float maxRadius;

        public int mapSize;
        public int seed;

        public void Execute() 
        {            
            float spawnSize = mapSize - 1;

            float maxRadiusLocal = varyRadius ? maxRadius : radius;
            float cellSize = maxRadiusLocal / Mathf.Sqrt(2);
            
            // Initialize 2d grid of lists
            int gridWidth = Mathf.CeilToInt(spawnSize / cellSize);
            int maxPointsPerCell = Mathf.CeilToInt(cellSize / (float)(Mathf.Sqrt(minRadius)));

            NativeArray<UnsafeList<int>> grid = new NativeArray<UnsafeList<int>>(gridWidth * gridWidth, Allocator.Temp);
            for (int i = 0; i < gridWidth * gridWidth; i++)
            {
                grid[i] = new UnsafeList<int>(maxPointsPerCell * 5, Allocator.Temp);
            }
    
            Unity.Mathematics.Random prng = new Unity.Mathematics.Random((uint)seed);

            NativeList<Vector2> points2d = new NativeList<Vector2>(Allocator.Temp);
            NativeList<Vector2> spawnPoints = new NativeList<Vector2>(Allocator.Temp);

            grid[0].Add(0);
            spawnPoints.Add(new Vector2(spawnSize / 2, spawnSize / 2));

            while (spawnPoints.Length > 0)
            {
                int spawnIndex = prng.NextInt(0, spawnPoints.Length);
                Vector2 spawnCentre = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < numSamplesBeforeRejection; i++)
                {
                    float randomFloat = prng.NextFloat();
                    float angle = randomFloat * Mathf.PI * 2;
                    float localRadius = radius;
                    if (varyRadius)
                    {
                        localRadius = spawnNoiseMap[(int)(spawnCentre.x) * mapSize + (int)(spawnCentre.y)]
                                    * (maxRadius - minRadius) + minRadius;
                    }
                    Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

                    Vector2 candidate = spawnCentre + dir * (prng.NextFloat() * localRadius + localRadius);

                    // Check if the candidate we have randomly selected is valid
                    if (IsValid(candidate, spawnSize, cellSize, localRadius, points2d, grid, gridWidth))
                    {
                        points2d.Add(candidate);
                        spawnPoints.Add(candidate);

                        int cellX = (int)(candidate.x / cellSize);
                        int cellY = (int)(candidate.y / cellSize);

                        var list = grid[cellX * gridWidth + cellY];
                        list.Add(points2d.Length);
                        grid[cellX * gridWidth + cellY] = list;

                        candidateAccepted = true;
                        break;
                    }
                }
                if (!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex);
                }
            }

            for (int point = 0; point < points2d.Length; point++)
            {
                float offset = 1f; // Take into account offset due to extra points around edges

                float adjustedX = (points2d[point].x) * meshScale - offset;
                float adjustedY = Common.HeightFromFloatCoord(points2d[point].x, points2d[point].y, heightMap, mapSize);
                float adjustedZ = (points2d[point].y) * meshScale - offset;
                Vector3 adjustedPoint = new Vector3(adjustedX, adjustedY, adjustedZ);

                if (adjustedPoint.x >= 0f && adjustedPoint.y >= 0f && adjustedPoint.x <= mapSize - 3 && adjustedPoint.y <= mapSize - 3)
                {
                    points.Add(adjustedPoint);
                }
            }   

            // Dispose native arrays/lists
            for (int i = 0; i < gridWidth * gridWidth; i++)
            {
                grid[i].Dispose();
            }
            grid.Dispose();
            points2d.Dispose();
            spawnPoints.Dispose();
        }

        private bool IsValid(Vector2 candidate,
                            float spawnSize,
                            float cellSize,
                            float localRadius,
                            NativeList<Vector2> points,
                            NativeArray<UnsafeList<int>> grid,
                            int gridWidth)
        {
            if (candidate.x < 0 || candidate.x >= spawnSize || candidate.y < 0 || candidate.y >= spawnSize)
            {
                return false;
            }
            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);

            int maxIndex = gridWidth - 1;

            // Set range and clamp it inside grid indices
            int searchStartX = Mathf.Max(0, cellX - 1); 
            int searchEndX = Mathf.Min(cellX + 1, maxIndex);
            int searchStartY = Mathf.Max(0, cellY - 1);
            int searchEndY = Mathf.Min(cellY + 1, maxIndex);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    for (int i = 0; i < grid[x * gridWidth + y].Length; i++)
                    {
                        int pointIndex = grid[x * gridWidth + y][i] - 1;
                        if (pointIndex != -1)
                        {
                            float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                            if (sqrDst < localRadius * localRadius)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}

[System.Serializable]
public class PoissonDiskSamplingSettings
{
    public bool varyRadius = false;
    public float radius = 5f;
    public float minRadius = 5f;
    public float maxRadius = 50f;
    public NoiseMapSettings noiseMapSettings;

    PoissonDiskSamplingSettings(
        bool varyRadius,
        float radius,
        float minRadius,
        float maxRadius,
        NoiseMapSettings noiseMapSettings
    )
    {
        this.varyRadius = varyRadius;
        this.radius = radius;
        this.minRadius = minRadius;
        this.maxRadius = maxRadius;
        this.noiseMapSettings = noiseMapSettings;
    }

#if UNITY_EDITOR

    public void OnValidate()
    {
        if (noiseMapSettings != null)
        {
            noiseMapSettings.OnValidate();
        }

        minRadius = Mathf.Max(minRadius, 0f);
        maxRadius = Mathf.Max(maxRadius, minRadius);
    }

#endif
}