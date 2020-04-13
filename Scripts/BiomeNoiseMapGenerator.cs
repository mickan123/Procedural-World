using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeHeightMapGenerator {

	public static BiomeData GenerateBiomeNoiseMaps(int width, int height, WorldSettings worldSettings, Vector2 sampleCentre) {
		
		HeightMap humidityNoiseMap = HeightMapGenerator.GenerateHeightMap(width,
																	height,
																	worldSettings.humidityMapSettings,
																	worldSettings,
																	sampleCentre,
																	HeightMapGenerator.NormalizeMode.Global,
																	worldSettings.humidityMapSettings.seed);
		HeightMap temperatureNoiseMap = HeightMapGenerator.GenerateHeightMap(width,
																		height,
																		worldSettings.temperatureMapSettings,
																		worldSettings,
																		sampleCentre,
																		HeightMapGenerator.NormalizeMode.Global,
																		worldSettings.temperatureMapSettings.seed);
		BiomeInfo biomeInfo = GenerateBiomeInfo(width,
												height,
												humidityNoiseMap,
												temperatureNoiseMap,
												worldSettings);
		HeightMap heightNoiseMap = GenerateBiomeHeightMap(width,
															height,
															worldSettings,
															humidityNoiseMap,
															temperatureNoiseMap,
															sampleCentre,
															biomeInfo);

		HydraulicErosion.Erode(heightNoiseMap.values, worldSettings, biomeInfo);
		ThermalErosion.Erode(heightNoiseMap.values, worldSettings, biomeInfo);

		return new BiomeData(heightNoiseMap, biomeInfo);
	}

	public static HeightMap GenerateBiomeHeightMap(int width, 
												 int height, 
												 WorldSettings worldSettings,
												 HeightMap humidityNoiseMap, 
												 HeightMap temperatureNoiseMap, 
												 Vector2 sampleCentre, 
												 BiomeInfo biomeInfo) {
		
		// Generate noise maps for all nearby and present biomes
		int numBiomes = worldSettings.biomes.Length;
		HeightMap[] biomeNoiseMaps = new HeightMap[numBiomes];
		for (int i = 0; i < numBiomes; i++) {
			biomeNoiseMaps[i] = HeightMapGenerator.GenerateHeightMap(width, 
													height, 
													worldSettings.biomes[i].heightMapSettings, 
													worldSettings,
													sampleCentre, 
													HeightMapGenerator.NormalizeMode.GlobalBiome,
													worldSettings.biomes[i].heightMapSettings.seed);
			}

		// Calculate final noise map values by blending where near another biome
		float[,] finalNoiseMapValues = new float[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int biome = 0; biome < numBiomes; biome++) {
					finalNoiseMapValues[x, y] += biomeNoiseMaps[biome].values[x, y] * biomeInfo.biomeStrengths[x, y, biome];
				}
			}
		}

		return new HeightMap(finalNoiseMapValues);
	}

	public static BiomeInfo GenerateBiomeInfo(int width, int height, HeightMap humidityNoiseMap, HeightMap temperatureNoiseMap, WorldSettings settings) {
		int numBiomes = settings.biomes.Length;
		int[,] biomeMap = new int[width, height];
		float[,,] biomeStrengths = new float[width, height, numBiomes];

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

				float humidity = humidityNoiseMap.values[i, j];
				float temperature = temperatureNoiseMap.values[i, j]; 

				// Get current biome
				for (int w = 0; w < numBiomes; w++) {
					BiomeSettings curBiome = settings.biomes[w]; 

					if (humidity > curBiome.startHumidity 
						&& humidity < curBiome.endHumidity
						&& temperature > curBiome.startTemperature 
						&& temperature < curBiome.endTemperature) {

						biomeMap[i, j] = w;
						biomeStrengths[i, j, w] = 1f;
						w = numBiomes;
					}
				}
				
				// Get strengths of all other biomes for blending
				int actualBiomeIndex = biomeMap[i, j];
				float actualBiomeTransitionDist = Mathf.Max(settings.sqrTransitionDistance, 0.00001f);
				float totalBiomeStrength = 1f; // Start at 1 for base biome

				for (int w = 0; w < numBiomes; w++) {
					if (w != actualBiomeIndex) {
						BiomeSettings curBiome = settings.biomes[w]; 
						float humidityDist = Mathf.Min(Mathf.Abs(humidity - curBiome.startHumidity), 
													   Mathf.Abs(humidity - curBiome.endHumidity));
						float tempDist = Mathf.Min(Mathf.Abs(temperature - curBiome.startTemperature), 
						                    	   Mathf.Abs(temperature - curBiome.endTemperature));
						
						if (humidity >= curBiome.startHumidity && humidity <= curBiome.endHumidity) {
							humidityDist = 0f;
						}
						if (temperature >= curBiome.startTemperature && temperature <= curBiome.endTemperature) {
							tempDist = 0f;
						}
						float distToBiome = humidityDist * humidityDist + tempDist * tempDist;

						if (distToBiome <= actualBiomeTransitionDist) {
							biomeStrengths[i, j, w] = (1f - (distToBiome / actualBiomeTransitionDist));
							biomeStrengths[i, j, w] *= biomeStrengths[i, j, w]; // Square values for smoother transition
							totalBiomeStrength += biomeStrengths[i, j, w];
						}
					}
				}

				// Normalize by biome strengths in range [0, 1]
				for (int w = 0; w < numBiomes; w++) {
					biomeStrengths[i, j, w] /= totalBiomeStrength;
				}
			}
		}

		// Calculate main Biome by summing strength at every location
		float[] totalBiomeStrenths = new float[numBiomes];
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				for (int w = 0; w < numBiomes; w++) {
					totalBiomeStrenths[w] += biomeStrengths[i, j, w];
				}	
			}
		}
		float maxValue = float.MinValue;
 		int maxIndex = 0;
		for (int i = 0; i < numBiomes; i++) {
			if (totalBiomeStrenths[i] > maxValue) {
				maxValue = totalBiomeStrenths[i];
				maxIndex = i;
			}
		}

		return new BiomeInfo(biomeMap, biomeStrengths, maxIndex);
	}
}

[System.Serializable]
public struct BiomeData {
	public readonly HeightMap heightNoiseMap;
	public readonly BiomeInfo biomeInfo;

	public BiomeData(HeightMap heightNoiseMap, BiomeInfo biomeInfo) {
		this.heightNoiseMap = heightNoiseMap;
		this.biomeInfo = biomeInfo;
	}
}