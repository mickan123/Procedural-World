using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiskSampling
{
    public static List<Vector3> GeneratePoints(
        PoissonDiskSamplingSettings settings,
        Vector2 sampleCentre,
        float[][] heightMap,
        System.Random prng,
        float[] randomValues,
        TerrainSettings terrainSettings,
        int numSamplesBeforeRejection = 25
    )
    {
        int mapSize = heightMap.Length;

        float[][] spawnNoiseMap;
        if (settings.varyRadius)
        {
            spawnNoiseMap = Noise.GenerateNoiseMap(
                mapSize,
                mapSize,
                settings.noiseMapSettings.perlinNoiseSettings,
                sampleCentre,
                settings.noiseMapSettings.noiseType,
                settings.noiseMapSettings.seed
            );
        }
        else
        {
            spawnNoiseMap = null;
        }
        float spawnSize = mapSize - 1;

        float maxRadius = settings.varyRadius ? settings.maxRadius : settings.radius;
        float cellSize = maxRadius / Mathf.Sqrt(2);
        
        int maxPointsPerCell = Mathf.CeilToInt(cellSize / (float)(Mathf.Sqrt(settings.minRadius)));

        // Initialize 2d grid of lists
        int gridWidth = Mathf.CeilToInt(spawnSize / cellSize);
        List<int>[][] grid = new List<int>[gridWidth][];
        for (int x = 0; x < gridWidth; x++)
        {
            grid[x] = new List<int>[gridWidth];
            for (int y = 0; y < gridWidth; y++)
            {
                grid[x][y] = new List<int>(maxPointsPerCell);
            }
        }

        List<Vector2> points2d = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        int numPoints = 0;
        int randIdx = 0;
        spawnPoints.Add(new Vector2(spawnSize / 2, spawnSize / 2));

        Vector2 dir = new Vector2(); // Construct once and reuse
        while (spawnPoints.Count > 0)
        {
            numPoints++;
            int spawnIndex = prng.Next(0, spawnPoints.Count);
            Vector2 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float randomFloat = randomValues[randIdx];
                float angle = randomFloat * Mathf.PI * 2;
                float radius = settings.radius;
                if (settings.varyRadius)
                {
                    radius = spawnNoiseMap[(int)(spawnCentre.x)][(int)(spawnCentre.y)]
                                * (settings.maxRadius - settings.minRadius) + settings.minRadius;
                }
                dir.Set(Mathf.Sin(angle), Mathf.Cos(angle));

                Vector2 candidate = spawnCentre + dir * (randomValues[randIdx + 1] * radius + radius);

                // Check if the candidate we have randomly selected is valid
                if (IsValid(candidate, spawnSize, cellSize, radius, points2d, grid))
                {
                    points2d.Add(candidate);
                    spawnPoints.Add(candidate);

                    int cellX = (int)(candidate.x / cellSize);
                    int cellY = (int)(candidate.y / cellSize);
                    grid[cellX][cellY].Add(points2d.Count);
                    candidateAccepted = true;
                    break;
                }

                 // Update random index
                randIdx += 2;
                if (randIdx >= randomValues.Length - 1)
                {
                    randIdx = 0;
                }
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        List<Vector3> points3d = new List<Vector3>(points2d.Count);
        for (int point = 0; point < points2d.Count; point++)
        {
            float height = Common.HeightFromFloatCoord(points2d[point].x, points2d[point].y, heightMap);
            float offset = 1f; // Take into account offset due to extra points around edges

            Vector3 adjustedPoint = new Vector3((points2d[point].x) * terrainSettings.meshSettings.meshScale - offset,
                                                Common.HeightFromFloatCoord(points2d[point].x, points2d[point].y, heightMap),
                                                (points2d[point].y) * terrainSettings.meshSettings.meshScale - offset);
            if (adjustedPoint.x >= 0f && adjustedPoint.y >= 0f && adjustedPoint.x <= mapSize - 3 && adjustedPoint.y <= mapSize - 3)
            {
                points3d.Add(adjustedPoint);
            }
        }

        return points3d;
    }

    static bool IsValid(Vector2 candidate,
                        float spawnSize,
                        float cellSize,
                        float radius,
                        List<Vector2> points,
                        List<int>[][] grid)
    {
        if (candidate.x < 0 || candidate.x >= spawnSize || candidate.y < 0 || candidate.y >= spawnSize)
        {
            return false;
        }
        int cellX = (int)(candidate.x / cellSize);
        int cellY = (int)(candidate.y / cellSize);

        int maxIndex = grid.Length - 1;

        // Set range and clamp it inside grid indices
        int searchStartX = ((cellX - 1) > 0) ? (cellX - 1) : 0; 
        int searchEndX = ((cellX + 1) > maxIndex) ? maxIndex : (cellX + 1); 
        int searchStartY = ((cellY - 1) > 0) ? (cellY - 1) : 0; 
        int searchEndY = ((cellY + 1) > maxIndex) ? maxIndex : (cellY + 1); 

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                for (int i = 0; i < grid[x][y].Count; i++)
                {
                    int pointIndex = grid[x][y][i] - 1;
                    if (pointIndex != -1)
                    {
                        float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                        if (sqrDst < radius * radius)
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
