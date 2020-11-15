using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class BiomeHeightMapGenerator
{
    private static readonly int[,] neighBouroffsets = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

    public static BiomeData GenerateBiomeNoiseMaps(int width, int height, TerrainSettings terrainSettings, Vector2 chunkCentre, WorldManager worldManager)
    {
        int padding = terrainSettings.erosionSettings.maxLifetime;
        int paddedWidth = width + 2 * padding;
        int paddedHeight = height + 2 * padding;
        Vector2 paddedChunkCentre = new Vector2(chunkCentre.x - padding, chunkCentre.y - padding);

        
        BiomeGraph humidityGraph = terrainSettings.humidityMapGraph;
        // Dispatcher.RunOnMainThread(() => GPUErosion(settings, mapSize, map, randomIndices, ref gpuDone));
        float[,] humidityNoiseMap = humidityGraph.GetHeightMap(
            terrainSettings,
            paddedChunkCentre,
            paddedWidth,
            paddedHeight
        );

        BiomeGraph temperatureGraph = terrainSettings.temperatureMapGraph;
        float[,] temperatureNoiseMap = temperatureGraph.GetHeightMap(
            terrainSettings,
            paddedChunkCentre,
            paddedWidth,
            paddedHeight
        );

#if (PROFILE && UNITY_EDITOR)
		float biomeInfoStartTime = 0f;
		if (terrainSettings.IsMainThread()) {
        	biomeInfoStartTime = Time.realtimeSinceStartup;
		}
#endif

        BiomeInfo biomeInfo = GenerateBiomeInfo(
            paddedWidth,
            paddedHeight,
            humidityNoiseMap,
            temperatureNoiseMap,
            terrainSettings
        );
        
#if (PROFILE && UNITY_EDITOR)
		if (terrainSettings.IsMainThread()) {
			float biomeInfoEndTime = Time.realtimeSinceStartup;
			float biomeInfoTimeTaken = biomeInfoEndTime - biomeInfoStartTime;
			Debug.Log("BiomeInfo time taken: " + biomeInfoTimeTaken + "s");
		}
#endif

#if (PROFILE && UNITY_EDITOR)
		float biomeNoiseMapStartTime = 0f;
		if (terrainSettings.IsMainThread()) {
        	biomeNoiseMapStartTime = Time.realtimeSinceStartup;
		}
#endif

        float[,] heightNoiseMap = GenerateBiomeHeightMap(
            paddedWidth,
            paddedHeight,
            terrainSettings,
            humidityNoiseMap,
            temperatureNoiseMap,
            paddedChunkCentre,
            biomeInfo,
            worldManager
        );

#if (PROFILE && UNITY_EDITOR)
		if (terrainSettings.IsMainThread()) {
			float biomeNoiseMapEndTime = Time.realtimeSinceStartup;
			float biomeNoiseMapTimeTaken = biomeNoiseMapEndTime - biomeNoiseMapStartTime;
			Debug.Log("Biome Noise Map time taken: " + biomeNoiseMapTimeTaken + "s");
		}
#endif
        // Get rid of padding 
        float[,] actualHeightNoiseMap = new float[width, height];
        int[,] actualBiomeMap = new int[width, height];
        float[,,] actualBiomeStrengths = new float[width, height, terrainSettings.biomeSettings.Count];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                actualHeightNoiseMap[i, j] = heightNoiseMap[i + padding, j + padding];
                actualBiomeMap[i, j] = biomeInfo.biomeMap[i + padding, j + padding];
                for (int w = 0; w < terrainSettings.biomeSettings.Count; w++)
                {
                    actualBiomeStrengths[i, j, w] = biomeInfo.biomeStrengths[i + padding, j + padding, w];
                }
            }
        }

        BiomeInfo actualBiomeInfo = new BiomeInfo(actualBiomeMap, actualBiomeStrengths, biomeInfo.mainBiome);

        return new BiomeData(actualHeightNoiseMap, actualBiomeInfo);
    }

    public static float[,] GenerateBiomeHeightMap(
        int width,
        int height,
        TerrainSettings terrainSettings,
        float[,] humidityNoiseMap,
        float[,] temperatureNoiseMap,
        Vector2 sampleCentre,
        BiomeInfo biomeInfo,
        WorldManager manager
    )
    {
        // Generate noise maps for all nearby and present biomes
        int numBiomes = terrainSettings.biomeSettings.Count;
        List<float[,]> biomeNoiseMaps = new List<float[,]>();
        for (int i = 0; i < numBiomes; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            biomeNoiseMaps.Add(
                graph.GetHeightMap(
                    manager,
                    biomeInfo,
                    terrainSettings,
                    sampleCentre,
                    width,
                    height
                )
            );
        }

        // Calculate final noise map values by blending where near another biome
        float[,] finalNoiseMapValues = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int biome = 0; biome < numBiomes; biome++)
                {
                    finalNoiseMapValues[x, y] += biomeNoiseMaps[biome][x, y] * biomeInfo.biomeStrengths[x, y, biome];
                }
            }
        }

        return finalNoiseMapValues;
    }

    public static BiomeInfo GenerateBiomeInfo(int width, int height, float[,] humidityNoiseMap, float[,] temperatureNoiseMap, TerrainSettings settings)
    {
        int numBiomes = settings.biomeSettings.Count;
        int[,] biomeMap = new int[width, height];
        float[,,] biomeStrengths = new float[width, height, numBiomes];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float humidity = humidityNoiseMap[i, j];
                float temperature = temperatureNoiseMap[i, j];

                // Get current biome
                for (int w = 0; w < numBiomes; w++)
                {
                    BiomeSettings curBiome = settings.biomeSettings[w];

                    if (humidity > curBiome.startHumidity
                        && humidity < curBiome.endHumidity
                        && temperature > curBiome.startTemperature
                        && temperature < curBiome.endTemperature)
                    {

                        biomeMap[i, j] = w;
                        biomeStrengths[i, j, w] = 1f;
                        w = numBiomes;
                    }
                }

                // Get strengths of all other biomes for blending
                int actualBiomeIndex = biomeMap[i, j];
                float actualBiomeTransitionDist = Mathf.Max(settings.sqrTransitionDistance, 0.00001f);
                float totalBiomeStrength = 1f; // Start at 1 for base biome

                for (int w = 0; w < numBiomes; w++)
                {
                    if (w != actualBiomeIndex)
                    {
                        BiomeSettings curBiome = settings.biomeSettings[w];
                        float humidityDist = Mathf.Min(Mathf.Abs(humidity - curBiome.startHumidity),
                                                       Mathf.Abs(humidity - curBiome.endHumidity));
                        float tempDist = Mathf.Min(Mathf.Abs(temperature - curBiome.startTemperature),
                                                   Mathf.Abs(temperature - curBiome.endTemperature));

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
                            biomeStrengths[i, j, w] = (1f - (distToBiome / actualBiomeTransitionDist));
                            biomeStrengths[i, j, w] *= biomeStrengths[i, j, w]; // Square values for smoother transition
                            totalBiomeStrength += biomeStrengths[i, j, w];
                        }
                    }
                }

                // Normalize by biome strengths in range [0, 1]
                for (int w = 0; w < numBiomes; w++)
                {
                    biomeStrengths[i, j, w] /= totalBiomeStrength;
                }
            }
        }

        return new BiomeInfo(
            biomeMap,
            biomeStrengths,
            CalculateMainBiomeIndex(biomeStrengths)
        );
    }

    private static int CalculateMainBiomeIndex(float[,,] biomeStrengths)
    {
        int width = biomeStrengths.GetLength(0);
        int height = biomeStrengths.GetLength(1);
        int numBiomes = biomeStrengths.GetLength(2);

        float[] totalBiomeStrenths = new float[numBiomes];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int w = 0; w < numBiomes; w++)
                {
                    totalBiomeStrenths[w] += biomeStrengths[i, j, w];
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
    public readonly float[,] heightNoiseMap;
    public readonly BiomeInfo biomeInfo;

    public BiomeData(float[,] heightNoiseMap, BiomeInfo biomeInfo)
    {
        this.heightNoiseMap = heightNoiseMap;
        this.biomeInfo = biomeInfo;
    }
}

public struct BiomeInfo
{
    public readonly int[,] biomeMap; // Holds index of biome at each point
    public readonly float[,,] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0, 1]
    public readonly int mainBiome;

    public BiomeInfo(int[,] biomeMap, float[,,] biomeStrengths, int mainBiome)
    {
        this.biomeMap = biomeMap;
        this.biomeStrengths = biomeStrengths;
        this.mainBiome = mainBiome;
    }
}
