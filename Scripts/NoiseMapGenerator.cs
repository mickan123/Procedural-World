using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class NoiseMapGenerator {

	public static NoiseMap GenerateNoiseMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											BiomeSettings biomeSettings,
											Vector2 sampleCentre, 
											int seed) {
		float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings.noiseSettings, biomeSettings, sampleCentre, seed);

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
												 BiomeInfo biomeInfo,
												 int seed) {

		int[] uniqueBiomes = biomeInfo.biomeMap.Cast<int>().Distinct().ToArray(); // TODO try without this to see perf difference

		// Only 1 biome
		if (uniqueBiomes.Length == 1) {
			NoiseMapSettings settings = biomeSettings.biomes[uniqueBiomes[0]].heightMapSettings;
			return GenerateNoiseMap(width, height, settings, biomeSettings, sampleCentre, seed);
		}

		// Generate noise maps for all nearby and present biomes
		int numBiomes = biomeSettings.biomes.Length;
		NoiseMap[] biomeNoiseMaps = new NoiseMap[numBiomes];
		for (int i = 0; i < uniqueBiomes.Length; i++) {
			int biomeIndex = uniqueBiomes[i];
			biomeNoiseMaps[biomeIndex] = GenerateNoiseMap(width, 
														  height, 
														  biomeSettings.biomes[biomeIndex].heightMapSettings, 
														  biomeSettings,
														  sampleCentre, 
														  seed);
		}

		// Calculate final noise map values
		float[,] finalNoiseMapValues = new float[width, height];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int biomeIndex = biomeInfo.biomeMap[x, y];
				int nearestBiomeIndex = biomeInfo.nearestBiomeMap[x, y];

				if (nearestBiomeIndex != biomeIndex) { // Interpolate between biomes
					float maxInterpDist = biomeSettings.biomes[biomeIndex].sqrTransitionDistance;
					float nearestBiomeDist = biomeInfo.sqrDistanceToNearestBiome[x, y];
					float linearInterpVal = 1f - (nearestBiomeDist / maxInterpDist);
					float squareInterpVal = Mathf.Pow(linearInterpVal, 2) / 2f;  // Divide by 2 as at 0 distance we want 50-50 blend

					float curBiomeVal = biomeNoiseMaps[biomeIndex].values[x, y];
					float nearestBiomeVal = biomeNoiseMaps[nearestBiomeIndex].values[x, y];

					float noiseVal = Mathf.Lerp(curBiomeVal, nearestBiomeVal, squareInterpVal);

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
		float[,] sqrDistanceToNearestBiome = new float[width, height];

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

				float humidity = humidityNoiseMap.values[i, j];
				float temperature = temperatureNoiseMap.values[i, j];

				// Get current biome
				for (int w = 0; w < settings.biomes.Length; w++) {
					Biome curBiome = settings.biomes[w]; 

					if (humidity >= curBiome.startHumidity 
						&& humidity <= curBiome.endHumidity
						&& temperature >= curBiome.startTemperature 
						&& temperature <= curBiome.endTemperature) {

						biomeMap[i, j] = w;
					}
				}

				// Get distance to closest biome
				float minDistToClosestBiome = float.MaxValue;
				int closestBiomeIndex = -1;

				for (int w = 0; w < settings.biomes.Length; w++) {
					if (w != biomeMap[i, j]) {
						Biome curBiome = settings.biomes[w]; 
						float humidityDist = Mathf.Min(Mathf.Abs(humidity - curBiome.startHumidity), Mathf.Abs(humidity - curBiome.endHumidity));
						float tempDist = Mathf.Min(Mathf.Abs(temperature - curBiome.startTemperature), Mathf.Abs(temperature - curBiome.endTemperature));
						
						if (humidity >= curBiome.startHumidity && humidity <= curBiome.endHumidity) {
							humidityDist = 0f;
						}
						if (temperature >= curBiome.startTemperature && temperature <= curBiome.endTemperature) {
							tempDist = 0f;
						}

						float distToBiome = humidityDist + tempDist;

						if (distToBiome < minDistToClosestBiome) {
							minDistToClosestBiome = distToBiome;

							if (distToBiome < curBiome.sqrTransitionDistance) {
								closestBiomeIndex = w;
							}
						}
					}
				}

				// If we only have 1 biome we may never set closest biome index and distance so we have to check values
				sqrDistanceToNearestBiome[i, j] = (closestBiomeIndex != -1) ? minDistToClosestBiome : 0;
				nearestBiomeMap[i, j] = (closestBiomeIndex != -1) ? closestBiomeIndex : biomeMap[i, j];
				
			}
		}

		return new BiomeInfo(biomeMap, nearestBiomeMap, sqrDistanceToNearestBiome);
	}
}


public struct BiomeInfo {
	public readonly int[,] biomeMap; // Holds index of biome at each point
	public readonly int[,] nearestBiomeMap; // Holds index of closest biome at each point
	public readonly float[,] sqrDistanceToNearestBiome;
	
	public BiomeInfo(int[,] biomeMap, int[,] nearestBiomeMap, float[,] distanceToNearestBiome) {
		this.biomeMap = biomeMap;
		this.nearestBiomeMap = nearestBiomeMap;
		this.sqrDistanceToNearestBiome = distanceToNearestBiome;
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