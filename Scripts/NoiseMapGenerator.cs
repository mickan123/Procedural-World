﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class NoiseMapGenerator {

	public enum NormalizeMode { GlobalBiome, Global };
	
	public static NoiseMap GenerateNoiseMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											BiomeSettings biomeSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
		float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings.noiseSettings, biomeSettings, sampleCentre, seed);

		if (normalizeMode == NormalizeMode.GlobalBiome) {
			values = Noise.normalizeGlobalBiomeValues(values, biomeSettings);
		}
		else if (normalizeMode == NormalizeMode.Global) {
			values = Noise.normalizeGlobalValues(values, noiseSettings.noiseSettings);
		}

		AnimationCurve heightCurve_threadsafe = new AnimationCurve(noiseSettings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * noiseSettings.heightMultiplier;

				if (values[i, j] > maxValue) {
					maxValue = values[i, j];
				}
				if (values[i, j] < minValue) {
					minValue = values[i, j];
				}
			}
		}

		return new NoiseMap(values, minValue, maxValue);
	}

	// Wrapper to generate noise
	public static NoiseMap GenerateBiomeNoiseMap(int width, 
												 int height, 
												 BiomeSettings biomeSettings,
												 NoiseMap humidityNoiseMap, 
												 NoiseMap temperatureNoiseMap, 
												 Vector2 sampleCentre, 
												 BiomeInfo biomeInfo) {
		
		// TODO: Test performance
		// Get unqiue biomes in biomeMap and nearestBiomeMap 
		List<int> uniqueBiomes = new List<int>();
		for (int x = 0; x < biomeInfo.biomeMap.GetLength(0); x++) {
			for (int y = 0; y < biomeInfo.biomeMap.GetLength(1); y++) {
				
				if (!uniqueBiomes.Contains(biomeInfo.biomeMap[x, y])) {
					uniqueBiomes.Add(biomeInfo.biomeMap[x, y]);
				}
				if (!uniqueBiomes.Contains(biomeInfo.nearestBiomeMap[x, y])) {
					uniqueBiomes.Add(biomeInfo.nearestBiomeMap[x, y]);
				}
			}
		}		

		// Only 1 biome so no blending between nearest biomes
		if (uniqueBiomes.Count == 1) {
			NoiseMapSettings settings = biomeSettings.biomes[uniqueBiomes[0]].heightMapSettings;
			return GenerateNoiseMap(width, height, settings, biomeSettings, sampleCentre, NormalizeMode.GlobalBiome, settings.seed);
		}

		// Generate noise maps for all nearby and present biomes
		int numBiomes = biomeSettings.biomes.Length;
		NoiseMap[] biomeNoiseMaps = new NoiseMap[numBiomes];
		for (int i = 0; i < numBiomes; i++) {
			biomeNoiseMaps[i] = GenerateNoiseMap(width, 
												height, 
												biomeSettings.biomes[i].heightMapSettings, 
												biomeSettings,
												sampleCentre, 
												NormalizeMode.GlobalBiome,
												biomeSettings.biomes[i].heightMapSettings.seed);
		}

		// Calculate final noise map values by blending where near another biome
		float[,] finalNoiseMapValues = new float[width, height];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int biomeIndex = biomeInfo.biomeMap[x, y];
				int nearestBiomeIndex = biomeInfo.nearestBiomeMap[x, y];

				if (nearestBiomeIndex != biomeIndex) { // Interpolate between biomes
					float biomeStrength = biomeInfo.mainBiomeStrength[x, y];

					float curBiomeVal = biomeNoiseMaps[biomeIndex].values[x, y];
					float nearestBiomeVal = biomeNoiseMaps[nearestBiomeIndex].values[x, y];

					float noiseVal = Mathf.SmoothStep(nearestBiomeVal, curBiomeVal, biomeStrength);
	
					finalNoiseMapValues[x, y] = noiseVal;
				} else {
					finalNoiseMapValues[x, y] = biomeNoiseMaps[biomeIndex].values[x, y];
				}

				if (finalNoiseMapValues[x, y] > maxValue) {
					maxValue = finalNoiseMapValues[x, y];
				}
				if (finalNoiseMapValues[x, y] < minValue) {
					minValue = finalNoiseMapValues[x, y];
				}
			}
		}

		return new NoiseMap(finalNoiseMapValues, minValue, maxValue);
	}

	public static BiomeInfo GenerateBiomeInfo(int width, int height, NoiseMap humidityNoiseMap, NoiseMap temperatureNoiseMap, BiomeSettings settings) {
		int[,] biomeMap = new int[width, height];
		int[,] nearestBiomeMap = new int[width, height];
		float[,] mainBiomeStrength = new float[width, height];
		float[,] distToNearestBiome = new float[width, height];

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

				float humidity = humidityNoiseMap.values[i, j];
				float temperature = temperatureNoiseMap.values[i, j]; 
				
				// Get current biome
				for (int w = 0; w < settings.biomes.Length; w++) {
					Biome curBiome = settings.biomes[w]; 

					if (humidity > curBiome.startHumidity 
						&& humidity < curBiome.endHumidity
						&& temperature > curBiome.startTemperature 
						&& temperature < curBiome.endTemperature) {

						biomeMap[i, j] = w;
						w = settings.biomes.Length;
					}
				}

				// Get distance to closest biome
				float minDistToClosestBiome = float.MaxValue;
				int closestBiomeIndex = biomeMap[i, j];
				int actualBiomeIndex = biomeMap[i, j];

				for (int w = 0; w < settings.biomes.Length; w++) {
					if (w != actualBiomeIndex) {
						Biome curBiome = settings.biomes[w]; 
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

						if (distToBiome < minDistToClosestBiome) {
							minDistToClosestBiome = distToBiome;

							if (distToBiome < settings.biomes[actualBiomeIndex].sqrTransitionDistance) {
								closestBiomeIndex = w;
							}
						}
					}
				}

				if (minDistToClosestBiome == float.MaxValue) {
					minDistToClosestBiome = 0f;
				}
				float maxDistToClosestBiome = settings.biomes[actualBiomeIndex].sqrTransitionDistance;
				float biomeStrength = (1f + (minDistToClosestBiome / maxDistToClosestBiome)) / 2; // 0.5 when mindist == 0, 1.0 when minDist == maxDist
				biomeStrength = Mathf.Min(biomeStrength, 1f);
				mainBiomeStrength[i, j] = biomeStrength;

				nearestBiomeMap[i, j] = closestBiomeIndex;
				distToNearestBiome[i, j] = minDistToClosestBiome;
			}
		}

		return new BiomeInfo(biomeMap, nearestBiomeMap, mainBiomeStrength, distToNearestBiome);
	}
}

public struct BiomeInfo {
	public readonly int[,] biomeMap; // Holds index of biome at each point
	public readonly int[,] nearestBiomeMap; // Holds index of closest biome at each point
	public readonly float[,] mainBiomeStrength; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0.5, 1]
	public readonly float[,] distToNearestBiome; // Used for debugging
	
	public BiomeInfo(int[,] biomeMap, int[,] nearestBiomeMap, float[,] mainBiomeStrength, float[,] distToNearestBiome) {
		this.biomeMap = biomeMap;
		this.nearestBiomeMap = nearestBiomeMap;
		this.mainBiomeStrength = mainBiomeStrength;
		this.distToNearestBiome = distToNearestBiome;
	}
}

public struct NoiseMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public NoiseMap(float[,] values, float minValue, float maxValue) {
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}