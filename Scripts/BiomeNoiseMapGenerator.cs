using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class BiomeHeightMapGenerator {

	private static readonly int[,] neighBouroffsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1} };
	public static BiomeData GenerateBiomeNoiseMaps(int width, int height, WorldSettings worldSettings, Vector2 chunkCentre, WorldGenerator worldGenerator) {
		
		int padding = worldSettings.erosionSettings.maxLifetime;
		int paddedWidth = width + 2 * padding;
		int paddedHeight = height + 2 * padding;
		Vector2 paddedChunkCentre = new Vector2(chunkCentre.x - padding, chunkCentre.y - padding);

		float[,] humidityNoiseMap = HeightMapGenerator.GenerateHeightMap(paddedWidth,
																	paddedHeight,
																	worldSettings.humidityMapSettings,
																	worldSettings,
																	paddedChunkCentre,
																	HeightMapGenerator.NormalizeMode.Global,
																	worldSettings.humidityMapSettings.seed);
		float[,] temperatureNoiseMap = HeightMapGenerator.GenerateHeightMap(paddedWidth,
																		paddedHeight,
																		worldSettings.temperatureMapSettings,
																		worldSettings,
																		paddedChunkCentre,
																		HeightMapGenerator.NormalizeMode.Global,
																		worldSettings.temperatureMapSettings.seed);
		BiomeInfo biomeInfo = GenerateBiomeInfo(paddedWidth,
												paddedHeight,
												humidityNoiseMap,
												temperatureNoiseMap,
												worldSettings);
		float[,] heightNoiseMap = GenerateBiomeHeightMap(paddedWidth,
														paddedHeight,
														worldSettings,
														humidityNoiseMap,
														temperatureNoiseMap,
														paddedChunkCentre,
														biomeInfo);

		
		ApplyErosion(heightNoiseMap, biomeInfo, worldSettings, chunkCentre, worldGenerator);

		// Get rid of padding 
		float[,] actualHeightNoiseMap = new float[width, height];
		int[,] actualBiomeMap = new int[width, height];
		float[,,] actualBiomeStrengths = new float[width, height, worldSettings.biomes.Length];
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				actualHeightNoiseMap[i, j] = heightNoiseMap[i + padding, j + padding];
				actualBiomeMap[i, j] = biomeInfo.biomeMap[i + padding, j + padding];
				for (int w = 0; w < worldSettings.biomes.Length; w++) {
					actualBiomeStrengths[i, j, w] = biomeInfo.biomeStrengths[i + padding, j + padding, w];
				}
			}
		}

		BiomeInfo actualBiomeInfo = new BiomeInfo(actualBiomeMap, actualBiomeStrengths, biomeInfo.mainBiome);

		return new BiomeData(actualHeightNoiseMap, actualBiomeInfo);
	}

	public static void ApplyErosion(float[,] heightNoiseMap, BiomeInfo biomeInfo, WorldSettings worldSettings, Vector2 chunkCentre, WorldGenerator worldGenerator) {

		int width = heightNoiseMap.GetLength(0);
		int height = heightNoiseMap.GetLength(1);
		int padding = worldSettings.erosionSettings.maxLifetime;

		float chunkWidth = worldSettings.meshSettings.numVerticesPerLine;
		ChunkCoord curChunkCoord = new ChunkCoord(Mathf.RoundToInt(chunkCentre.x / chunkWidth), Mathf.RoundToInt(chunkCentre.y / chunkWidth));

		if (worldGenerator != null) {

			// Generate list of chunk coords for current chunk and neighbouring chunks
			List<ChunkCoord> adjacentChunkCoords = new List<ChunkCoord>();
			adjacentChunkCoords.Add(curChunkCoord);
			for (int i = 0; i < neighBouroffsets.GetLength(0); i++) {
				ChunkCoord adjacentVector = new ChunkCoord(Mathf.RoundToInt(curChunkCoord.x + neighBouroffsets[i, 0]),
													 	   Mathf.RoundToInt(curChunkCoord.y + neighBouroffsets[i, 1]));

				adjacentChunkCoords.Add(adjacentVector);							
			}

			adjacentChunkCoords = adjacentChunkCoords.OrderBy(v => Mathf.Abs(v.x) + Mathf.Abs(v.y)).ToList();
			float[,] erosionMask = new float[width, height];

			for (int i = 0; i < adjacentChunkCoords.Count; i++) {
				if (adjacentChunkCoords[i] == curChunkCoord) {
					erosionMask = CalculateBiomeBlendingMask(erosionMask, padding);
					HydraulicErosion.Erode(heightNoiseMap, erosionMask, worldSettings, biomeInfo, worldGenerator);
					worldGenerator.DoneErosion(curChunkCoord, heightNoiseMap);
					break;
				}
				else {
					worldGenerator.UpdateChunkBorder(curChunkCoord, adjacentChunkCoords[i], heightNoiseMap, erosionMask);
				}	
			}
		}
		else {
			float[,] erosionMask = new float[width, height];
			CalculateBiomeBlendingMask(erosionMask, padding);
			HydraulicErosion.Erode(heightNoiseMap, new float[width, height], worldSettings, biomeInfo, worldGenerator);
		}
	}

	// This creates a mask of weights that blend the edges of biomes as they have some overlap due to padding
	public static float[,] CalculateBiomeBlendingMask(float[,] mask, int padding) {		
		int mapSize = mask.GetLength(0);
		for (int i = 0; i < mask.GetLength(0); i++) {
			for (int j = 0; j < mask.GetLength(1); j++) {
				if (mask[i, j] == 0) {
					mask[i, j] = 1f;
				}
				else {
					int xDistFromEdge = Mathf.Min(i, Mathf.Abs(i - mapSize));
					int yDistFromEdge = Mathf.Min(j, Mathf.Abs(j - mapSize));
					int distFromEdge = Mathf.Min(xDistFromEdge, yDistFromEdge);
					mask[i, j] = Mathf.Max(distFromEdge - padding - 3, 0)  / (float)(padding);
				}
			}
		}

		return mask;
	}
	

	public static float[,] GenerateBiomeHeightMap(int width, 
												 int height, 
												 WorldSettings worldSettings,
												 float[,] humidityNoiseMap, 
												 float[,] temperatureNoiseMap, 
												 Vector2 sampleCentre, 
												 BiomeInfo biomeInfo) {
		
		// Generate noise maps for all nearby and present biomes
		int numBiomes = worldSettings.biomes.Length;
		List<float[,]> biomeNoiseMaps = new List<float[,]>();
		for (int i = 0; i < numBiomes; i++) {
			biomeNoiseMaps.Add(HeightMapGenerator.GenerateHeightMap(width, 
								height, 
								worldSettings.biomes[i].heightMapSettings, 
								worldSettings,
								sampleCentre, 
								HeightMapGenerator.NormalizeMode.GlobalBiome,
								worldSettings.biomes[i].heightMapSettings.seed));
		}

		// Calculate final noise map values by blending where near another biome
		float[,] finalNoiseMapValues = new float[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int biome = 0; biome < numBiomes; biome++) {
					finalNoiseMapValues[x, y] += biomeNoiseMaps[biome][x, y] * biomeInfo.biomeStrengths[x, y, biome];
				}
			}
		}

		return finalNoiseMapValues;
	}

	public static BiomeInfo GenerateBiomeInfo(int width, int height, float[,] humidityNoiseMap, float[,] temperatureNoiseMap, WorldSettings settings) {
		int numBiomes = settings.biomes.Length;
		int[,] biomeMap = new int[width, height];
		float[,,] biomeStrengths = new float[width, height, numBiomes];

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

				float humidity = humidityNoiseMap[i, j];
				float temperature = temperatureNoiseMap[i, j]; 

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
	public readonly float[,] heightNoiseMap;
	public readonly BiomeInfo biomeInfo;

	public BiomeData(float[,] heightNoiseMap, BiomeInfo biomeInfo) {
		this.heightNoiseMap = heightNoiseMap;
		this.biomeInfo = biomeInfo;
	}
}

public struct BiomeInfo {
	public readonly int[,] biomeMap; // Holds index of biome at each point
	public readonly float[,,] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0, 1]
	public readonly int mainBiome;
	
	public BiomeInfo(int[,] biomeMap, float[,,] biomeStrengths, int mainBiome) {
		this.biomeMap = biomeMap;
		this.biomeStrengths = biomeStrengths;
		this.mainBiome = mainBiome;
	}
}
