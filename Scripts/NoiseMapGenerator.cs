using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class NoiseMapGenerator {

	public static NoiseMap GenerateNoiseMap(int width, int height, NoiseMapSettings settings, Vector2 sampleCentre, int seed) {
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre, seed);
		
		AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

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
												 int seed) {

		// Calculate biome at each location of noise map
		int[,] biomeMap = GenerateBiomeMap(width, height, humidityNoiseMap, temperatureNoiseMap, biomeSettings);
		NearestBiomeInfo nearestBiomeInfo = GenerateNearestBiomeInfo(width, height, biomeMap, biomeSettings);

		int numBiomes = biomeSettings.biomes.Length;
		NoiseMap[] biomeNoiseMaps = new NoiseMap[numBiomes];

		
		AnimationCurve[] heightCurves_threadsafe = biomeSettings.biomes.Select(x => new AnimationCurve(x.heightMapSettings.heightCurve.keys)).ToArray();

		//TODO Only calculate for biomes that are present in chunk
		for (int biomeIndex = 0; biomeIndex < numBiomes; biomeIndex++) {
			float[,] noiseValues = Noise.GenerateNoiseMap(width, 	
														  height,
														  biomeSettings.biomes[biomeIndex].heightMapSettings.noiseSettings, 
														  sampleCentre, 
														  seed);

			float biomeMinValue = float.MaxValue;
			float biomeMaxValue = float.MinValue;

			for (int x = 0; x < width; x++) {
				for (int y = 0; y < width; y++) {
					noiseValues[x, y] *= heightCurves_threadsafe[biomeIndex].Evaluate(noiseValues[x, y]) 
									   * biomeSettings.biomes[biomeIndex].heightMapSettings.heightMultiplier;

					if (noiseValues[x, y] > biomeMaxValue) {
						biomeMaxValue = noiseValues[x, y];
					}
					if (noiseValues[x, y] < biomeMinValue) {
						biomeMinValue = noiseValues[x, y];
					}
				}
			}

			biomeNoiseMaps[biomeIndex] = new NoiseMap(noiseValues, biomeMinValue, biomeMaxValue);
		}

		float[,] finalNoiseMapValues = new float[width, height];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		// Interpeloate biome values
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int biomeIndex = biomeMap[x, y];
				int nearestBiomeIndex = nearestBiomeInfo.nearestBiomeMap[x, y];

				if (nearestBiomeInfo.nearestBiomeMap[x, y] != biomeIndex) { // Interpolate between biomes
					float maxInterpDist = biomeSettings.biomes[biomeIndex].sqrTransitionDistance;
					float nearestBiomeDist = nearestBiomeInfo.nearestBiomeDistance[x, y];
					float interpVal = 0.5f - nearestBiomeDist / maxInterpDist / 2; // Divide by 2 as other biome handles other half of transition

					float curBiomeVal = biomeNoiseMaps[biomeIndex].values[x, y];
					float nearestBiomeVal = biomeNoiseMaps[nearestBiomeIndex].values[x, y];

					finalNoiseMapValues[x, y] = Mathf.Lerp(curBiomeVal, nearestBiomeVal, interpVal);
					
					if (finalNoiseMapValues[x, y] > maxValue) {
						maxValue = finalNoiseMapValues[x, y];
					}
					if (finalNoiseMapValues[x, y] < minValue) {
						minValue = finalNoiseMapValues[x, y];
					}
				} else {
					finalNoiseMapValues[x, y] = biomeNoiseMaps[biomeIndex].values[x, y];
				}
				
			}
		}

		return new NoiseMap(finalNoiseMapValues, minValue, maxValue);
	}

	public static int[,] GenerateBiomeMap(int width, int height, NoiseMap humidityNoiseMap, NoiseMap temperatureNoiseMap, BiomeSettings settings) {
		int[,] biomeMap = new int[width, height];

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

				float humidity = (humidityNoiseMap.values[i, j] - humidityNoiseMap.minValue) / (humidityNoiseMap.maxValue  - humidityNoiseMap.minValue);
				float temperature = (temperatureNoiseMap.values[i, j] - temperatureNoiseMap.minValue) / (temperatureNoiseMap.maxValue  - temperatureNoiseMap.minValue);

				for (int w = 0; w < settings.biomes.Length; w++) {
					Biome curBiome = settings.biomes[w]; 

					if (humidity >= curBiome.startHumidity 
						&& humidity <= curBiome.endHumidity
						&& temperature >= curBiome.startTemperature 
						&& temperature <= curBiome.endTemperature) {
						
						biomeMap[i, j] = w;
						w = settings.biomes.Length;
					}
				}
			}
		}
		return biomeMap;
	}

	public static NearestBiomeInfo GenerateNearestBiomeInfo(int width, int height, int[,] biomeMap, BiomeSettings settings) {
		int[,] nearestBiomeMap = new int[width, height];
		float[,] nearestBiomeDistance = new float[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				
				int curBiomeIndex = biomeMap[x, y];
				int transitionDistance = settings.biomes[curBiomeIndex].transitionDistance;
				float sqrTransitionDistance = settings.biomes[curBiomeIndex].sqrTransitionDistance;

				float sqrMinBiomeDistance = float.MaxValue;
				int closestBiome = curBiomeIndex;

				for (int adjustedX = Mathf.Max(0, x - transitionDistance); adjustedX < Mathf.Min(x + transitionDistance, width); adjustedX++) {
					for (int adjustedY = Mathf.Max(0, y - transitionDistance); adjustedY < Mathf.Min(y + transitionDistance, height); adjustedY++) {
							
						if (biomeMap[adjustedX, adjustedY] != biomeMap[x, y]) {
							int xOffset = adjustedX - x;
							int yOffset = adjustedY - y;
							float sqrDistToAdjusted = xOffset * xOffset + yOffset * yOffset;

							if (sqrDistToAdjusted < sqrMinBiomeDistance && sqrDistToAdjusted < sqrTransitionDistance) {
								sqrMinBiomeDistance = sqrDistToAdjusted;
								closestBiome = biomeMap[adjustedX, adjustedY];
							}
						}
					}
				}

				nearestBiomeDistance[x, y] = sqrMinBiomeDistance;
				nearestBiomeMap[x, y] = closestBiome;
			}
		}

		return new NearestBiomeInfo(nearestBiomeMap, nearestBiomeDistance);
	}
}

public struct NearestBiomeInfo {
	public readonly int[,] nearestBiomeMap;
	public readonly float[,] nearestBiomeDistance; // Holds square value of nearest biome distance

	public NearestBiomeInfo(int[,] nearestBiomeMap, float[,] nearestBiomeDistance) {
		this.nearestBiomeMap = nearestBiomeMap;
		this.nearestBiomeDistance = nearestBiomeDistance;
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