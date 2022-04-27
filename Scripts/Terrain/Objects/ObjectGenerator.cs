using System.Collections.Generic;
using UnityEngine;

public static class ObjectGenerator
{
    public static List<ObjectSpawner> GenerateObjectSpawners(
        float[] heightMap, 
        BiomeInfo info, 
        float[] roadStrengthMap, 
        TerrainSettings settings, 
        Vector2 sampleCentre
    )
    {
        int width = info.width;
        List<ObjectSpawner> biomeObjectSpawners = new List<ObjectSpawner>();

        System.Random prng = new System.Random((int)(sampleCentre.x + sampleCentre.y));

        for (int biome = 0; biome < settings.biomeSettings.Length; biome++)
        {
            if (HeightMapContainesBiome(info, biome))
            {
                BiomeGraph graph = settings.biomeSettings[biome].biomeGraph;
                graph.heightMapData[System.Threading.Thread.CurrentThread] = new HeightMapGraphData(
                    settings, sampleCentre, width
                );
                List<ObjectSpawner> spawners = graph.GetObjectSpawners(settings, sampleCentre, info, biome, heightMap, roadStrengthMap);
                biomeObjectSpawners.AddRange(spawners);
            }
        }
        return biomeObjectSpawners;
    }

    private static bool HeightMapContainesBiome(BiomeInfo info, int biome)
    {
        int length = info.biomeStrengths.Length;
        for (int i = 0; i < length; i++)
        {
            for (int j = 0; j < length; j++)
            {
                if (info.GetBiomeStrength(i, j, biome) > 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
