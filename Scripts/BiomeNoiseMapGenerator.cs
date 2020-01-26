using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeNoiseMapGenerator {

	public static float maxVal = float.MinValue;
	public static float minVal = float.MaxValue;

	public static BiomeData GenerateBiomeNoiseMaps(int width, int height, BiomeSettings biomeSettings, Vector2 sampleCentre) {
		
		// TODO separate functions for temp/humidity noise generation which normalizes based on a separate global max
		NoiseMap humidityNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width,
																	height,
																	biomeSettings.humidityMapSettings,
																	biomeSettings,
																	sampleCentre,
																	NoiseMapGenerator.NormalizeMode.Global,
																	biomeSettings.seed);
		NoiseMap temperatureNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width,
																		height,
																		biomeSettings.temperatureMapSettings,
																		biomeSettings,
																		sampleCentre,
																		NoiseMapGenerator.NormalizeMode.Global,
																		biomeSettings.seed);
		BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width,
																height,
																humidityNoiseMap,
																temperatureNoiseMap,
																biomeSettings);
		NoiseMap heightNoiseMap = NoiseMapGenerator.GenerateBiomeNoiseMap(width,
																		height,
																		biomeSettings,
																		humidityNoiseMap,
																		temperatureNoiseMap,
																		sampleCentre,
																		biomeInfo,
																		biomeSettings.seed);

		return new BiomeData(heightNoiseMap, temperatureNoiseMap, humidityNoiseMap, biomeInfo);
	}
}

[System.Serializable]
public struct BiomeData {
	public readonly NoiseMap heightNoiseMap;
	public readonly NoiseMap humidityNoiseMap;
	public readonly NoiseMap temperatureNoiseMap;
	public readonly BiomeInfo biomeInfo;

	public BiomeData(NoiseMap heightNoiseMap, NoiseMap humidityNoiseMap, NoiseMap temperatureNoiseMap, BiomeInfo biomeInfo) {
		this.heightNoiseMap = heightNoiseMap;
		this.humidityNoiseMap = humidityNoiseMap;
		this.temperatureNoiseMap = temperatureNoiseMap;
		this.biomeInfo = biomeInfo;
	}
}