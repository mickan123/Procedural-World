﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        float[][] humidityNoiseMap = humidityGraph.GetHeightMap(
            terrainSettings,
            paddedChunkCentre,
            width,
            height
        );

        BiomeGraph temperatureGraph = terrainSettings.temperatureMapGraph;
        float[][] temperatureNoiseMap = temperatureGraph.GetHeightMap(
            terrainSettings,
            paddedChunkCentre,
            width,
            height
        );

        BiomeInfo biomeInfo = GenerateBiomeInfo(
            width,
            height,
            humidityNoiseMap,
            temperatureNoiseMap,
            terrainSettings
        );
        

#if UNITY_EDITOR
		float biomeNoiseMapStartTime = 0f;
		if (terrainSettings.IsMainThread()) {
        	biomeNoiseMapStartTime = Time.realtimeSinceStartup;
		}
#endif

        float[][] heightNoiseMap = GenerateBiomeHeightMap(
            width,
            height,
            terrainSettings,
            paddedChunkCentre,
            biomeInfo
        );

#if UNITY_EDITOR
		if (terrainSettings.IsMainThread()) {
			float biomeNoiseMapEndTime = Time.realtimeSinceStartup;
			float biomeNoiseMapTimeTaken = biomeNoiseMapEndTime - biomeNoiseMapStartTime;
			Debug.Log("    Biome Noise Map time taken: " + biomeNoiseMapTimeTaken + "s");
		}
#endif
        return new BiomeData(heightNoiseMap, biomeInfo);
    }

    public static float[][] GenerateBiomeHeightMap(
        int width,
        int height,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        BiomeInfo biomeInfo
    )
    {
        // Generate noise maps for all nearby and present biomes
        int numBiomes = terrainSettings.biomeSettings.Length;
        float[][][] biomeNoiseMaps = new float[numBiomes][][];
        for (int i = 0; i < numBiomes; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            biomeNoiseMaps[i] = graph.GetHeightMap(
                biomeInfo,
                terrainSettings,
                sampleCentre,
                i,
                width,
                height
            );
        }

        // Calculate final noise map values by blending where near another biome
        float[][] finalNoiseMapValues = new float[width][];
        for (int i = 0; i < height; i++)
        {
            finalNoiseMapValues[i] = new float[height];
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int biome = 0; biome < numBiomes; biome++)
                {
                    finalNoiseMapValues[x][y] += biomeNoiseMaps[biome][x][y] * biomeInfo.biomeStrengths[x][y][biome];
                }
            }
        }

        return finalNoiseMapValues;
    }

    public static BiomeInfo GenerateBiomeInfo(int width, int height, float[][] humidityNoiseMap, float[][] temperatureNoiseMap, TerrainSettings settings)
    {
        int numBiomes = settings.biomeSettings.Length;
        int[][] biomeMap = new int[width][];
        float[][][] biomeStrengths = new float[width][][];
        for (int i = 0; i < width; i++)
        {
            biomeMap[i] = new int[height];
            biomeStrengths[i] = new float[height][];
            for (int j = 0; j < height; j++)
            {
                biomeStrengths[i][j] = new float[numBiomes];
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float humidity = humidityNoiseMap[i][j];
                float temperature = temperatureNoiseMap[i][j];

                // Get current biome
                for (int k = 0; k < numBiomes; k++)
                {
                    BiomeSettings curBiome = settings.biomeSettings[k];

                    if (humidity > curBiome.startHumidity
                        && humidity < curBiome.endHumidity
                        && temperature > curBiome.startTemperature
                        && temperature < curBiome.endTemperature)
                    {

                        biomeMap[i][j] = k;
                        biomeStrengths[i][j][k] = 1f;
                        k = numBiomes;
                    }
                }

                // Get strengths of all other biomes for blending
                int actualBiomeIndex = biomeMap[i][j];
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
                            biomeStrengths[i][j][k] = (1f - (distToBiome / actualBiomeTransitionDist));
                            totalBiomeStrength += biomeStrengths[i][j][k];
                        }
                    }
                }

                // Normalize by biome strengths in range [0, 1]
                for (int k = 0; k < numBiomes; k++)
                {
                    biomeStrengths[i][j][k] /= totalBiomeStrength;
                }
            }
        }

        return new BiomeInfo(
            biomeMap,
            biomeStrengths,
            CalculateMainBiomeIndex(biomeStrengths)
        );
    }

    private static int CalculateMainBiomeIndex(float[][][] biomeStrengths)
    {
        int width = biomeStrengths.Length;
        int height = biomeStrengths[0].Length;
        int numBiomes = biomeStrengths[0][0].Length;

        float[] totalBiomeStrenths = new float[numBiomes];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < numBiomes; k++)
                {
                    totalBiomeStrenths[k] += biomeStrengths[i][j][k];
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
    public float[][] heightNoiseMap;
    public BiomeInfo biomeInfo;

    public BiomeData(float[][] heightNoiseMap, BiomeInfo biomeInfo)
    {
        this.heightNoiseMap = heightNoiseMap;
        this.biomeInfo = biomeInfo;
    }
}

public struct BiomeInfo
{
    public int[][] biomeMap; // Holds index of biome at each point
    public float[][][] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0, 1]
    public int mainBiome;

    public BiomeInfo(int[][] biomeMap, float[][][] biomeStrengths, int mainBiome)
    {
        this.biomeMap = biomeMap;
        this.biomeStrengths = biomeStrengths;
        this.mainBiome = mainBiome;
    }
}
