using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public static class BiomeHeightMapGenerator
{
    public static BiomeData GenerateBiomeNoiseMaps(
        int width, 
        int height, 
        TerrainSettings terrainSettings, 
        Vector2 chunkCentre
    )
    {
        Vector2 paddedChunkCentre = new Vector2(chunkCentre.x, chunkCentre.y);

        BiomeGraph humidityGraph = terrainSettings.humidityMapGraph;
        float[] humidityNoiseMap = humidityGraph.GetHeightMap(
            terrainSettings,
            paddedChunkCentre,
            width
        );

        BiomeGraph temperatureGraph = terrainSettings.temperatureMapGraph;
        float[] temperatureNoiseMap = temperatureGraph.GetHeightMap(
            terrainSettings,
            paddedChunkCentre,
            width
        );

        BiomeInfo biomeInfo = GenerateBiomeInfo(
            width,
            humidityNoiseMap,
            temperatureNoiseMap,
            terrainSettings
        );
        
        float[] heightNoiseMap = GenerateBiomeHeightMap(
            width,
            terrainSettings,
            paddedChunkCentre,
            biomeInfo
        );

        return new BiomeData(heightNoiseMap, biomeInfo, width);
    }

    public static float[] GenerateBiomeHeightMap(
        int width,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        BiomeInfo biomeInfo
    )
    {
        // Generate noise maps for all nearby and present biomes
        int numBiomes = terrainSettings.biomeSettings.Length;
        float[][] biomeNoiseMaps = new float[numBiomes][];
        for (int i = 0; i < numBiomes; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            biomeNoiseMaps[i] = graph.GetHeightMap(
                biomeInfo,
                terrainSettings,
                sampleCentre,
                i,
                width
            );
        }

        // Calculate final noise map values by blending where near another biome
        float[] finalNoiseMapValues = new float[width * width];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                for (int biome = 0; biome < numBiomes; biome++)
                {
                    finalNoiseMapValues[x * width + y] += biomeNoiseMaps[biome][x * width + y] * biomeInfo.GetBiomeStrength(x, y, biome);
                }
            }
        }

        return finalNoiseMapValues;
    }

    public static BiomeInfo GenerateBiomeInfo(int width, float[] humidityNoiseMap, float[] temperatureNoiseMap, TerrainSettings settings)
    {
        int numBiomes = settings.biomeSettings.Length;
        int[] biomeMap = new int[width * width];
        float[] biomeStrengths = new float[width * width * numBiomes];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float humidity = humidityNoiseMap[i * width + j];
                float temperature = temperatureNoiseMap[i * width + j];

                // Get current biome
                for (int k = 0; k < numBiomes; k++)
                {
                    BiomeSettings curBiome = settings.biomeSettings[k];

                    if (humidity > curBiome.startHumidity
                        && humidity < curBiome.endHumidity
                        && temperature > curBiome.startTemperature
                        && temperature < curBiome.endTemperature)
                    {

                        biomeMap[i * width + j] = k;
                        biomeStrengths[i * width * numBiomes + j * numBiomes + k] = 1f;
                        k = numBiomes;
                    }
                }

                // Get strengths of all other biomes for blending
                int actualBiomeIndex = biomeMap[i * width + j];
                float actualBiomeTransitionDist = settings.transitionDistance;
                if (actualBiomeTransitionDist <= 0)
                {
                    actualBiomeTransitionDist = 0.000001f;
                }
                float totalBiomeStrength = 1f; // Start at 1 for base biome

                for (int k = 0; k < numBiomes; k++)
                {
                    if (k != actualBiomeIndex)
                    {
                        BiomeSettings curBiome = settings.biomeSettings[k];
                        float humidityDistStart = humidity > curBiome.startHumidity ? humidity - curBiome.startHumidity : curBiome.startHumidity - humidity;
                        float humidityDistEnd = humidity > curBiome.startHumidity ? humidity - curBiome.startHumidity : curBiome.startHumidity - humidity;
                        float humidityDist = humidityDistStart > humidityDistEnd ? humidityDistStart : humidityDistEnd;

                        float tempDistStart = temperature > curBiome.startTemperature ? temperature - curBiome.startTemperature : curBiome.startTemperature - temperature;
                        float tempDistEnd = temperature > curBiome.endTemperature ? temperature - curBiome.endTemperature : curBiome.endTemperature - temperature;
                        float tempDist = tempDistStart > tempDistEnd ? tempDistStart : tempDistEnd;

                        if (humidity >= curBiome.startHumidity && humidity <= curBiome.endHumidity)
                        {
                            humidityDist = 0f;
                        }
                        if (temperature >= curBiome.startTemperature && temperature <= curBiome.endTemperature)
                        {
                            tempDist = 0f;
                        }
                        float distToBiome = humidityDist * humidityDist + tempDist * tempDist;

                        if (distToBiome <= actualBiomeTransitionDist)
                        {
                            biomeStrengths[i * width * numBiomes + j * numBiomes + k] = (1f - (distToBiome / actualBiomeTransitionDist));
                            totalBiomeStrength += biomeStrengths[i * width * numBiomes + j * numBiomes + k];
                        }
                    }
                }

                // Normalize by biome strengths in range [0, 1]
                for (int k = 0; k < numBiomes; k++)
                {
                    biomeStrengths[i * width * numBiomes + j * numBiomes + k] /= totalBiomeStrength;
                }
            }
        }

        return new BiomeInfo(
            biomeMap,
            biomeStrengths,
            width,
            numBiomes
        );
    }

    private static int CalculateMainBiomeIndex(float[] biomeStrengths, int width, int height, int numBiomes)
    {
        float[] totalBiomeStrenths = new float[numBiomes];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < numBiomes; k++)
                {
                    totalBiomeStrenths[k] += biomeStrengths[i * height * numBiomes + j * numBiomes + k];
                }
            }
        }
        float maxValue = float.MinValue;
        int maxIndex = 0;
        for (int i = 0; i < numBiomes; i++)
        {
            if (totalBiomeStrenths[i] > maxValue)
            {
                maxValue = totalBiomeStrenths[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}

[System.Serializable]
public struct BiomeData
{
    public float[] heightNoiseMap;
    public BiomeInfo biomeInfo;

    public int width;

    public BiomeData(float[] heightNoiseMap, BiomeInfo biomeInfo, int width)
    {
        this.heightNoiseMap = heightNoiseMap;
        this.biomeInfo = biomeInfo;
        this.width = width;
    }
}

public struct BiomeInfo
{
    public int[] biomeMap; // Holds index of biome at each point
    public float[] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0, 1]
    public int numBiomes;
    public int width;

    public BiomeInfo(int[] biomeMap, float[] biomeStrengths, int width, int numBiomes)
    {
        this.biomeMap = biomeMap;
        this.biomeStrengths = biomeStrengths;
        
        this.width = width;
        this.numBiomes = numBiomes;
    }

    public float GetBiomeStrength(int x, int y, int biome)
    {
        return this.biomeStrengths[x * width * numBiomes + y * numBiomes + biome];
    }
}
