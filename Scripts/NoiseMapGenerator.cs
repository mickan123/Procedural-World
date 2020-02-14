using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class NoiseMapGenerator {

	public enum NormalizeMode { GlobalBiome, Global };
	
	public static NoiseMap GenerateNoiseMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											WorldSettings worldSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
		float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings.noiseSettings, worldSettings, sampleCentre, seed);

		if (normalizeMode == NormalizeMode.GlobalBiome) {
			values = Noise.normalizeGlobalBiomeValues(values, worldSettings);
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
												 WorldSettings worldSettings,
												 NoiseMap humidityNoiseMap, 
												 NoiseMap temperatureNoiseMap, 
												 Vector2 sampleCentre, 
												 BiomeInfo biomeInfo) {
		
		// Generate noise maps for all nearby and present biomes
		int numBiomes = worldSettings.biomes.Length;
		NoiseMap[] biomeNoiseMaps = new NoiseMap[numBiomes];
		for (int i = 0; i < numBiomes; i++) {
			biomeNoiseMaps[i] = GenerateNoiseMap(width, 
												height, 
												worldSettings.biomes[i].heightMapSettings, 
												worldSettings,
												sampleCentre, 
												NormalizeMode.GlobalBiome,
												worldSettings.biomes[i].heightMapSettings.seed);
		}

		// Calculate final noise map values by blending where near another biome
		float[,] finalNoiseMapValues = new float[width, height];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int biome = 0; biome < numBiomes; biome++) {
					finalNoiseMapValues[x, y] += biomeNoiseMaps[biome].values[x, y] * biomeInfo.biomeStrengths[x, y, biome];
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

	public static BiomeInfo GenerateBiomeInfo(int width, int height, NoiseMap humidityNoiseMap, NoiseMap temperatureNoiseMap, WorldSettings settings) {
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
				float actualBiomeTransitionDist = settings.sqrTransitionDistance;
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
		return new BiomeInfo(biomeMap, biomeStrengths);
	}
}

public struct BiomeInfo {
	public readonly int[,] biomeMap; // Holds index of biome at each point
	public readonly float[,,] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0.5, 1]
	
	public BiomeInfo(int[,] biomeMap, float[,,] biomeStrengths) {
		this.biomeMap = biomeMap;
		this.biomeStrengths = biomeStrengths;
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