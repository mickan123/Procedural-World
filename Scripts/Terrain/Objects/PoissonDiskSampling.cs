using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiskSampling
{

    public static List<Vector3> GeneratePoints(TerrainObjectSettings settings,
                                                Vector2 sampleCentre,
                                                float[,] heightMap,
                                                System.Random prng,
                                                TerrainSettings terrainSettings,
                                                int biome,
                                                int numSamplesBeforeRejection = 35)
    {
        int mapSize = heightMap.GetLength(0);

        float[,] spawnNoiseMap;
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

        // Initialize 2d grid of lists
        List<int>[,] grid = new List<int>[Mathf.CeilToInt(spawnSize / cellSize), Mathf.CeilToInt(spawnSize / cellSize)];
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                grid[x, y] = new List<int>();
            }
        }

        List<Vector2> points2d = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        int numPoints = 0;
        spawnPoints.Add(new Vector2(spawnSize / 2, spawnSize / 2));
        while (spawnPoints.Count > 0)
        {
            numPoints++;
            int spawnIndex = prng.Next(0, spawnPoints.Count);
            Vector2 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float randomFloat = Common.NextFloat(prng, 0f, 1f);
                float angle = randomFloat * Mathf.PI * 2;
                float radius = settings.radius;
                if (settings.varyRadius)
                {
                    radius = spawnNoiseMap[Mathf.RoundToInt(spawnCentre.x), Mathf.RoundToInt(spawnCentre.y)]
                                * (settings.maxRadius - settings.minRadius) + settings.minRadius;
                }
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

                Vector2 candidate = spawnCentre + dir * Common.NextFloat(prng, radius, 2 * radius);
                if (IsValid(candidate, spawnSize, cellSize, radius, points2d, grid))
                {
                    points2d.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)].Add(points2d.Count);
                    candidateAccepted = true;
                    break;
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
                        List<int>[,] grid)
    {
        if (candidate.x >= 0 && candidate.x < spawnSize && candidate.y >= 0 && candidate.y < spawnSize)
        {

            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 1);
            int searchEndX = Mathf.Min(cellX + 1, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 1);
            int searchEndY = Mathf.Min(cellY + 1, grid.GetLength(1) - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    for (int i = 0; i < grid[x, y].Count; i++)
                    {
                        int pointIndex = grid[x, y][i] - 1;
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
        return false;
    }
}
