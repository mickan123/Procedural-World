using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeNoiseMapGenerator {

	public static BiomeNoiseMaps GenerateBiomeNoiseMaps(int width, 
														int height, 
														NoiseMapSettings heightSettings, 
														NoiseMapSettings temperatureSettings, 
														NoiseMapSettings humiditySettings, 
														Vector2 sampleCentre) {
															
		NoiseMap heightNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width, height, heightSettings, sampleCentre);
		NoiseMap temperatureNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width, height, temperatureSettings, sampleCentre);
		NoiseMap humidityNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width, height, heightSettings, sampleCentre);

		return new BiomeNoiseMaps(heightNoiseMap, temperatureNoiseMap, humidityNoiseMap);
	}
}


public struct BiomeNoiseMaps {
	public readonly NoiseMap heightNoiseMap;
	public readonly NoiseMap temperatureNoiseMap;
	public readonly NoiseMap humidityNoiseMap;

	public BiomeNoiseMaps(NoiseMap heightNoiseMap, NoiseMap temperatureNoiseMap, NoiseMap humidityNoiseMap) {
		this.heightNoiseMap = heightNoiseMap;
		this.temperatureNoiseMap = temperatureNoiseMap;
		this.humidityNoiseMap = humidityNoiseMap;
	}
}