using System.Collections.Generic;
using UnityEngine;

public static class ObjectGenerator
{
    public static List<ObjectSpawner> GenerateObjectSpawners(
        float[,] heightMap, 
        BiomeInfo info, 
        float[,] roadStrengthMap, 
        TerrainSettings settings, 
        Vector2 sampleCentre
    )
    {
        List<ObjectSpawner> biomeObjectSpawners = new List<ObjectSpawner>();

        System.Random prng = new System.Random((int)(sampleCentre.x + sampleCentre.y));

        for (int biome = 0; biome < settings.biomeSettings.Count; biome++)
        {
            if (HeightMapContainesBiome(info, biome))
            {
                BiomeGraph graph = settings.biomeSettings[biome].biomeGraph;
                List<ObjectSpawner> spawners = graph.GetObjectSpawners(heightMap, roadStrengthMap);
                biomeObjectSpawners.AddRange(spawners);
            }
        }
        return biomeObjectSpawners;
    }

    private static bool HeightMapContainesBiome(BiomeInfo info, int biome)
    {
        for (int i = 0; i < info.biomeStrengths.GetLength(0); i++)
        {
            for (int j = 0; j < info.biomeStrengths.GetLength(1); j++)
            {
                if (info.biomeStrengths[i, j, biome] > 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
