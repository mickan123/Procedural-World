using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeNoiseMapGenerator {

	public static BiomeNoiseMaps GenerateBiomeNoiseMaps(int width, 
														int height, 
														BiomeSettings biomeSettings, 
														Vector2 sampleCentre) {

		NoiseMap humidityNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width, height, biomeSettings.humidityMapSettings, biomeSettings, sampleCentre, biomeSettings.seed);
		NoiseMap temperatureNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width, height, biomeSettings.temperatureMapSettings, biomeSettings, sampleCentre, biomeSettings.seed);
		NoiseMap heightNoiseMap = NoiseMapGenerator.GenerateBiomeNoiseMap(width, height, biomeSettings, humidityNoiseMap, temperatureNoiseMap, sampleCentre, biomeSettings.seed);

		return new BiomeNoiseMaps(heightNoiseMap, temperatureNoiseMap, humidityNoiseMap);
	}
}

public struct BiomeNoiseMaps {
	public readonly NoiseMap heightNoiseMap;
	public readonly NoiseMap humidityNoiseMap;
	public readonly NoiseMap temperatureNoiseMap;

	public BiomeNoiseMaps(NoiseMap heightNoiseMap, NoiseMap humidityNoiseMap, NoiseMap temperatureNoiseMap) {
		this.heightNoiseMap = heightNoiseMap;
		this.humidityNoiseMap = humidityNoiseMap;
		this.temperatureNoiseMap = temperatureNoiseMap;
	}
}