using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ObjectGenerator {

	public static List<TerrainObject> GenerateBiomeObjects(NoiseMap heightMap, BiomeInfo info, WorldSettings settings, Vector2 sampleCentre) {
		List<TerrainObject> biomeObjects = new List<TerrainObject>();
		
		for (int biome = 0; biome < settings.biomes.Length; biome++) {
			
			for (int objectSettings = 0; objectSettings < settings.biomes[biome].terrainObjectSettings.Length; objectSettings++) {
				biomeObjects.Add(GenerateTerrainObject(settings.biomes[biome].terrainObjectSettings[objectSettings], 
														biome,
														heightMap, 
														info, 
														settings, 
														sampleCentre));
			}
		}

		return biomeObjects;
	}

	public static TerrainObject GenerateTerrainObject(TerrainObjectSettings settings, 
														int biome,
														NoiseMap heightMap, 
														BiomeInfo info, 
														WorldSettings worldSettings, 
														Vector2 sampleCentre) {

		int mapSize = heightMap.values.GetLength(0);

		// float[,] spawnNoiseMap = Noise.GenerateNoiseMap(mapSize, mapSize, settings.noiseMapSettings.noiseSettings, sampleCentre, settings.noiseMapSettings.seed);
		
		List<Vector2> points = PoissonDiskSampling.GeneratePoints(settings.minRadius, worldSettings.meshSettings.meshWorldSize, biome, info, sampleCentre);
		List<Vector3> spawnPositions = new List<Vector3>();		

		for (int point = 0; point < points.Count; point++) {
			Vector2 spawnPoint = points[point];
			spawnPositions.Add(new Vector3(Mathf.FloorToInt(spawnPoint.x + sampleCentre.x) - (float)mapSize / 2f,
											heightMap.values[Mathf.FloorToInt(spawnPoint.x), Mathf.FloorToInt(spawnPoint.y)], 
											-Mathf.FloorToInt(spawnPoint.y - sampleCentre.y) + (float)mapSize / 2f));
		}

		return new TerrainObject(settings.terrainObject, spawnPositions);
	}

}
