using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using csDelaunay;

public static class HeightMapGenerator
{

    public enum NormalizeMode { GlobalBiome, Global, Local };
    public enum VoronoiMode { RandomFlat, ConvexPolygons, ClosestPoint, Cracks };

    private static readonly object VoronoiLock = new object();

    public static float[] GenerateHeightMap(
        int width,
        NoiseMapSettings noiseSettings,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        NormalizeMode normalizeMode,
        int seed
    )
    {
        float[] heightMap;
        if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.Perlin)
        {
            heightMap = GeneratePerlinHeightMap(width, noiseSettings, terrainSettings, sampleCentre, normalizeMode, seed);
        }
        else if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.Simplex)
        {
            heightMap = GenerateSimplexHeightMap(width, noiseSettings, terrainSettings, sampleCentre, normalizeMode, seed);
        }
        else if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.SandDune)
        {
            heightMap = GenerateSandDuneHeightMap(width, noiseSettings, terrainSettings.meshSettings, sampleCentre, seed);
        }
        else
        {
            heightMap = GeneratePerlinHeightMap(width, noiseSettings, terrainSettings, sampleCentre, normalizeMode, seed);
        }

        return heightMap;
    }

    public static float[] GeneratePerlinHeightMap(
        int width,
        NoiseMapSettings noiseSettings,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        NormalizeMode normalizeMode,
        int seed
    )
    {

        float[] values = Noise.GenerateNoiseMap(width, noiseSettings.perlinNoiseSettings, sampleCentre, noiseSettings.noiseType, seed);

        if (normalizeMode == NormalizeMode.GlobalBiome)
        {
            values = Noise.normalizeGlobalBiomeValues(values, terrainSettings);
        }
        else if (normalizeMode == NormalizeMode.Global)
        {
            values = Noise.normalizeGlobalValues(values, noiseSettings.perlinNoiseSettings);
        }
        else if (normalizeMode == NormalizeMode.Local)
        {
            values = Noise.normalizeLocal(values);
        }

        return values;
    }

    public static float[] GenerateSimplexHeightMap(
        int width,
        NoiseMapSettings noiseSettings,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        NormalizeMode normalizeMode,
        int seed
    )
    {
        float[] values = Noise.GenerateNoiseMap(width, noiseSettings.simplexNoiseSettings, sampleCentre, noiseSettings.noiseType, seed);

        if (normalizeMode == NormalizeMode.GlobalBiome)
        {
            values = Noise.normalizeGlobalBiomeValues(values, terrainSettings);
        }
        else if (normalizeMode == NormalizeMode.Global)
        {
            values = Noise.normalizeGlobalValues(values, noiseSettings.simplexNoiseSettings);
        }
        else if (normalizeMode == NormalizeMode.Local)
        {
            values = Noise.normalizeLocal(values);
        }

        return values;
    }

    public static float[] GenerateTerracedNoiseMap(
        int width,
        NoiseMapSettings noiseMapSettings,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        NormalizeMode normalizeMode,
        int numTerraces,
        int seed
    )
    {
        float[] heightMap = HeightMapGenerator.GenerateHeightMap(
            width,
            noiseMapSettings,
            terrainSettings,
            sampleCentre,
            normalizeMode,
            noiseMapSettings.seed
        );

        float terraceInterval = 1f / (float)numTerraces;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                heightMap[x * width + y] = Mathf.Floor(heightMap[x * width + y] / terraceInterval) * terraceInterval;
            }
        }

        return heightMap;
    }

    public static float[] GenerateRidgedTurbulenceMap(
        int width,
        NoiseMapSettings noiseSettings,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        NormalizeMode normalizeMode,
        int seed
    )
    {
        float[] heightMap = HeightMapGenerator.GenerateHeightMap(
            width,
            noiseSettings,
            terrainSettings,
            sampleCentre,
            normalizeMode,
            noiseSettings.seed
        );

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                heightMap[x * width + y] = (heightMap[x * width + y] * 2f) - 1; // Convert to range [-1, 1]
                heightMap[x * width + y] = Mathf.Abs(heightMap[x * width + y]);
            }
        }

        return heightMap;
    }

    public static float[] GenerateVeronoiMap(
        int width,
        TerrainSettings terrainSettings,
        NormalizeMode normalizeMode,
        VoronoiMode voronoiMode,
        int numPolygons,
        int numLloydsIterations,
        float voronoiCrackWidth,
        int seed
    )
    {
        List<Vector2> randomPoints = new List<Vector2>();
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < numPolygons; i++)
        {
            float x = Common.NextFloat(prng, 0, width);
            float y = Common.NextFloat(prng, 0, width);
            randomPoints.Add(new Vector2(x, y));
        }

        Rect bounds = new Rect(0, 0, width, width);

        Voronoi voronoi;
        lock (VoronoiLock)
        {
            voronoi = new Voronoi(randomPoints, bounds, numLloydsIterations);
        }

        float[] heightMap = new float[width * width];
        if (voronoiMode == VoronoiMode.RandomFlat)
        {
            FlatVoronoiHeightMap(ref heightMap, width, voronoi, prng);
        }
        else if (voronoiMode == VoronoiMode.ConvexPolygons)
        {
            ConvexPoloygonsVoronoiHeightMap(ref heightMap, width, voronoi);
        }
        else if (voronoiMode == VoronoiMode.ClosestPoint)
        {
            ClosestPointVoronoiHeightMap(ref heightMap, width, voronoi);
        }
        else if (voronoiMode == VoronoiMode.Cracks)
        {
            CracksVoronoiHeightMap(ref heightMap, width, voronoi, voronoiCrackWidth);
        }

        return heightMap;
    }

    private static void FlatVoronoiHeightMap(ref float[] heightMap, int width, Voronoi voronoi, System.Random prng)
    {
        int numPolygons = voronoi.SiteCoords().Count;
        float[] randomHeights = new float[numPolygons];
        for (int i = 0; i < numPolygons; i++)
        {
            randomHeights[i] = Common.NextFloat(prng, 0, 1);
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                Site closestSite = voronoi.ClosestSiteAtPoint(new Vector2(x, y));
                heightMap[x * width + y] = randomHeights[closestSite.SiteIndex];
            }
        }
    }

    private static void ConvexPoloygonsVoronoiHeightMap(ref float[] heightMap, int width, Voronoi voronoi)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                Vector2 pos = new Vector2(x, y);

                Site closestSite = voronoi.ClosestSiteAtPoint(pos);
                Edge closestEdge = closestSite.ClosestEdgeAtPoint(pos);

                float distToEdge = Common.DistanceFromLine(
                    pos,
                    closestEdge.ClippedEnds[LR.LEFT],
                    closestEdge.ClippedEnds[LR.LEFT] - closestEdge.ClippedEnds[LR.RIGHT]
                );
                float distToSite = Vector2.Distance(closestSite.Coord, pos);

                heightMap[x * width + y] = distToEdge;
            }
        }
    }

    private static void ClosestPointVoronoiHeightMap(ref float[] heightMap, int width, Voronoi voronoi)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Site closestSite = voronoi.ClosestSiteAtPoint(pos);

                float dist = Vector2.Distance(closestSite.Coord, pos);

                heightMap[x * width + y] = dist;
            }
        }
    }

    private static void CracksVoronoiHeightMap(ref float[] heightMap, int width, Voronoi voronoi, float voronoiCrackWidth)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {

                Vector2 pos = new Vector2(x, y);
                Site closestSite = voronoi.ClosestSiteAtPoint(pos);
                Site secondClosestSite = voronoi.SecondClosestSiteAtPoint(pos);

                float dist = Vector2.Distance(closestSite.Coord, pos);
                float maxDist = Vector2.Distance(secondClosestSite.Coord, pos);

                // Use half crack width as its calculated half from either side
                float halfCrackWidth = voronoiCrackWidth / 2f;
                heightMap[x * width + y] = (maxDist - dist > halfCrackWidth) ? 1f : 0f;
            }
        }
    }


    public static float[] GenerateSandDuneHeightMap(
        int width,
        NoiseMapSettings noiseSettings,
        MeshSettings meshSettings,
        Vector2 sampleCentre,
        int seed
    )
    {
        float[] values = new float[width * width];
        int padding = (width - meshSettings.meshWorldSize - 3) / 2;
        int chunkIdxY = (int)(sampleCentre.y + padding) / meshSettings.meshWorldSize;

        SandDuneSettings sandDuneSettings = noiseSettings.sandDuneSettings;

        int length = noiseSettings.sandDuneSettings.sandDunePeriods.Length;
        for (int k = 0; k < length; k++)
        {
            SandDunePeriod settings = noiseSettings.sandDuneSettings.sandDunePeriods[k];
            float duneHeight = ((2 * sandDuneSettings.sigma * settings.duneWidth) / Mathf.PI) * Mathf.Max(1 - sandDuneSettings.xm, 0.01f);

            float[] noiseValues = Noise.GenerateNoiseMap(
                width,
                noiseSettings.simplexNoiseSettings,
                sampleCentre,
                NoiseMapSettings.NoiseType.Simplex,
                seed + k
            );
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    float duneLength = settings.duneWidth + settings.duneGap;
                    float duneOffset = noiseValues[i * width + j] * settings.maxDuneVariation + settings.duneOffset;
                    float x = (j + duneOffset + chunkIdxY * (width - (2 * padding) - 3)) % duneLength;

                    x = (x < 0) ? x + duneLength : x;
                    x = Mathf.Clamp(x / settings.duneWidth, 0, 1);

                    // Calculate dune height and equation according to Laurent Avenel (Wiwine) Terragen tutorial
                    float side = (x > sandDuneSettings.xm) ? 1f : 0f;
                    float cosTerm = 1f - Mathf.Cos((Mathf.PI / (sandDuneSettings.p * side + 1)) * ((x - side) / (sandDuneSettings.xm - side)));
                    float constant = ((sandDuneSettings.p * side + 1) / 2f);

                    // Calculate height multiplier with range[0, 1], perlin height value must be > duneThreshold or it is clamped to 0 
                    float duneThresholdMultiplier = Mathf.Max(0, noiseValues[i * width + j] - settings.duneThreshold) / (Mathf.Max(1f - settings.duneThreshold, 0.01f));

                    values[i * width + j] += (constant * cosTerm) * duneHeight * duneThresholdMultiplier;
                }
            }

        }
        return values;
    }
}