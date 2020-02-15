using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeNoiseMapGenerator {

	public static BiomeData GenerateBiomeNoiseMaps(int width, int height, WorldSettings worldSettings, Vector2 sampleCentre) {
		
		// TODO separate functions for temp/humidity noise generation which normalizes based on a separate global max
		NoiseMap humidityNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width,
																	height,
																	worldSettings.humidityMapSettings,
																	worldSettings,
																	sampleCentre,
																	NoiseMapGenerator.NormalizeMode.Global,
																	worldSettings.humidityMapSettings.seed);
		NoiseMap temperatureNoiseMap = NoiseMapGenerator.GenerateNoiseMap(width,
																		height,
																		worldSettings.temperatureMapSettings,
																		worldSettings,
																		sampleCentre,
																		NoiseMapGenerator.NormalizeMode.Global,
																		worldSettings.temperatureMapSettings.seed);
		BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width,
																height,
																humidityNoiseMap,
																temperatureNoiseMap,
																worldSettings);
		NoiseMap heightNoiseMap = NoiseMapGenerator.GenerateBiomeNoiseMap(width,
																		height,
																		worldSettings,
																		humidityNoiseMap,
																		temperatureNoiseMap,
																		sampleCentre,
																		biomeInfo);
																		
		List<TerrainObject> biomeObjects = ObjectGenerator.GenerateBiomeObjects(worldSettings, biomeInfo);																		

		float[,] values = HydraulicErosion.Erode(heightNoiseMap.values, worldSettings.erosionSettings);
		values = ThermalErosion.Erode(heightNoiseMap.values, worldSettings.erosionSettings);

		return new BiomeData(new NoiseMap(values, heightNoiseMap.minValue, heightNoiseMap.maxValue), temperatureNoiseMap, humidityNoiseMap, biomeInfo, biomeObjects);
	}
}

[System.Serializable]
public struct BiomeData {
	public readonly NoiseMap heightNoiseMap;
	public readonly NoiseMap humidityNoiseMap;
	public readonly NoiseMap temperatureNoiseMap;
	public readonly BiomeInfo biomeInfo;
	public readonly List<TerrainObject> biomeObjects;

	public BiomeData(NoiseMap heightNoiseMap, NoiseMap humidityNoiseMap, NoiseMap temperatureNoiseMap, BiomeInfo biomeInfo, List<TerrainObject> biomeObjects) {
		this.heightNoiseMap = heightNoiseMap;
		this.humidityNoiseMap = humidityNoiseMap;
		this.temperatureNoiseMap = temperatureNoiseMap;
		this.biomeInfo = biomeInfo;
		this.biomeObjects = biomeObjects;
	}
}