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
        [ReadOnly]
        public NativeArray<float> heightMap;
        [ReadOnly]
        public NativeArray<float> spawnNoiseMap;

        [WriteOnly]
        public NativeList<float> xCoords;
        [WriteOnly]
        public NativeList<float> yCoords;
        [WriteOnly]
        public NativeList<float> zCoords;

        public Vector2 sampleCentre;

        public int meshScale;
        public int numSamplesBeforeRejection;

        // Poisson Disk Sampling settings
        public bool varyRadius;
        public float radius;
        public float minRadius;
        public float maxRadius;

        public int width;
        public int seed;

        public void Execute() 
        {            
            float spawnSize = width - 1;

            float maxRadiusLocal = varyRadius ? maxRadius : radius;
            float cellSize = maxRadiusLocal / Mathf.Sqrt(2);
            
            // Initialize 2d grid of lists
            int gridWidth = Mathf.CeilToInt(spawnSize / cellSize);
            int maxPointsPerCell = Mathf.CeilToInt(cellSize / (float)(Mathf.Sqrt(minRadius)));

            NativeArray<UnsafeList<int>> grid = new NativeArray<UnsafeList<int>>(gridWidth * gridWidth, Allocator.Temp);
            for (int i = 0; i < gridWidth * gridWidth; i++)
            {
                grid[i] = new UnsafeList<int>(maxPointsPerCell, Allocator.Temp);
            }
    
            Unity.Mathematics.Random prng = new Unity.Mathematics.Random((uint)seed);

            NativeList<float2> points2d = new NativeList<float2>(Allocator.Temp);
            NativeList<float2> spawnPoints = new NativeList<float2>(Allocator.Temp);

            grid[0].Add(0);
            spawnPoints.Add(new float2(spawnSize / 2, spawnSize / 2));

            while (spawnPoints.Length > 0)
            {
                int spawnIndex = prng.NextInt(0, spawnPoints.Length);
                float2 spawnCentre = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < numSamplesBeforeRejection; i++)
                {
                    float randomFloat = prng.NextFloat();
                    float angle = randomFloat * Mathf.PI * 2;
                    float localRadius = radius;
                    if (varyRadius)
                    {
                        localRadius = spawnNoiseMap[(int)(spawnCentre.x) * width + (int)(spawnCentre.y)]
                                    * (maxRadius - minRadius) + minRadius;
                    }
                    float2 dir = new float2(Mathf.Sin(angle), Mathf.Cos(angle));

                    float2 candidate = spawnCentre + dir * (prng.NextFloat() * localRadius + localRadius);

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
                float adjustedY = Common.HeightFromFloatCoord(points2d[point].x, points2d[point].y, heightMap, width);
                float adjustedZ = (points2d[point].y) * meshScale - offset;

                if (adjustedX >= 0f && adjustedZ >= 0f && adjustedX <= width - 3 && adjustedZ <= width - 3)
                {
                    xCoords.Add(adjustedX);
                    yCoords.Add(adjustedY);
                    zCoords.Add(adjustedZ);
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

        private bool IsValid(float2 candidate,
                            float spawnSize,
                            float cellSize,
                            float localRadius,
                            NativeList<float2> points,
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
                            float sqrDst = math.distancesq(candidate, points[pointIndex]);
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