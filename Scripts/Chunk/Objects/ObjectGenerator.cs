using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ObjectGenerator {

	public static List<SpawnObject> GenerateBiomeObjects(float[,] heightMap, BiomeInfo info, Road road, WorldSettings settings, Vector2 sampleCentre) {
		List<SpawnObject> biomeObjects = new List<SpawnObject>();
		
		System.Random prng = new System.Random((int)(sampleCentre.x + sampleCentre.y));
		
		for (int biome = 0; biome < settings.biomes.Length; biome++) {
			
			if (HeightMapContainesBiome(info, biome)) {
				for (int objectSetting = 0; objectSetting < settings.biomes[biome].terrainObjectSettings.Length; objectSetting++) {
					biomeObjects.Add(GenerateTerrainObject(settings.biomes[biome].terrainObjectSettings[objectSetting], 
															biome,
															heightMap,
															road.roadStrengthMap, 
															info, 
															settings, 
															sampleCentre,
															prng));    
				}
			}
		}

		return biomeObjects;
	}

	private static bool HeightMapContainesBiome(BiomeInfo info, int biome) {
		for (int i = 0; i < info.biomeStrengths.GetLength(0); i++) {
			for (int j = 0; j < info.biomeStrengths.GetLength(1); j++) {
				if (info.biomeStrengths[i, j, biome] > 0f) {
					return true;
				}
			}
		}
		return false;
	}

	public static SpawnObject GenerateTerrainObject(TerrainObjectSettings settings, 
													int biome,
													float[,] heightMap,
													float[,] roadStrengthMap, 
													BiomeInfo info, 
													WorldSettings worldSettings, 
													Vector2 sampleCentre,
													System.Random prng) {

															
		int mapSize = heightMap.GetLength(0);

		float[,] spawnNoiseMap = Noise.GenerateNoiseMap(mapSize,
														mapSize,
														settings.noiseMapSettings.perlinNoiseSettings,
														sampleCentre,
														worldSettings.biomes[biome].heightMapSettings.noiseType,
														settings.noiseMapSettings.seed);
				
		List<Vector2> points = PoissonDiskSampling.GeneratePoints(settings, mapSize - 1, sampleCentre, spawnNoiseMap, prng);

		points = FilterPointsByBiome(points, biome, info, prng);

		if (settings.constrainSlope) {
			points = FilterPointsBySlope(points, settings.minSlope, settings.maxSlope, heightMap);
		}
		if (settings.constrainHeight) {
			AnimationCurve threadSafeCurve = new AnimationCurve(settings.heightProbabilityCurve.keys);
			points = FilterPointsByHeight(points, settings.minHeight, settings.maxHeight, heightMap, settings.heightProbabilityCurve, prng);
		}
		if (!settings.spawnOnRoad) {
			points = FilterPointsOnRoad(points, roadStrengthMap);
		}

		List<ObjectPosition> spawnPositions = new List<ObjectPosition>();
		

		for (int point = 0; point < points.Count; point++) {
			Vector2 spawnPoint = points[point];

			Vector3 position = new Vector3(Mathf.FloorToInt(spawnPoint.x + sampleCentre.x) * worldSettings.meshSettings.meshScale,
											heightMap[Mathf.FloorToInt(spawnPoint.x), Mathf.FloorToInt(spawnPoint.y)], 
											Mathf.FloorToInt(spawnPoint.y + sampleCentre.y) * worldSettings.meshSettings.meshScale);

			Quaternion rotation = settings.GetRotation(prng);
			Vector3 scale = settings.GetScale(prng);
			Vector3 translation = settings.GetTranslation(prng);

			spawnPositions.Add(new ObjectPosition(position + translation, scale, rotation));
		}

		return new SpawnObject(settings.terrainObjects, 
								spawnPositions, 
								prng);
	}

	public static List<Vector2> FilterPointsByBiome(List<Vector2> points, int biome, BiomeInfo info, System.Random prng) {

		for (int i = 0; i < points.Count; i++) {
			float rand = (float)prng.NextDouble(); 

			int coordX = (int) points[i].x;
        	int coordY = (int) points[i].y;

			if (rand > info.biomeStrengths[coordX, coordY, biome] * info.biomeStrengths[coordX, coordY, biome] * info.biomeStrengths[coordX, coordY, biome]) {
				points.RemoveAt(i);
				i--;
			}
		}
		return points;
	}

	public static List<Vector2> FilterPointsBySlope(List<Vector2> points, float minSlope, float maxSlope, float[,] heightMap) {

		for (int i = 0; i < points.Count; i++) {
			
			int coordX = (int) points[i].x;
        	int coordY = (int) points[i].y;

			// Calculate offset inside the cell (0,0) = at NW node, (1,1) = at SE node
			float x = points[i].x - coordX;
			float y = points[i].y - coordY;

			float heightNW = heightMap[coordX, coordY];
			float heightNE = heightMap[coordX + 1, coordY];
			float heightSW = heightMap[coordX, coordY + 1];
			float heightSE = heightMap[coordX + 1, coordY + 1];

			float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        	float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

			float slope = Mathf.Sqrt(gradientX * gradientX + gradientY * gradientY);

			if (slope > maxSlope || slope < minSlope) {
				points.RemoveAt(i);
				i--;
			}
		}

		return points;
	}

	public static List<Vector2> FilterPointsByHeight(List<Vector2> points, 
														float minHeight, 
														float maxHeight, 
														float[,] heightMap,
														AnimationCurve heightProbabilityCurve,
														System.Random prng) {
		
		for (int i = 0; i < points.Count; i++) {
			float height = heightMap[(int)points[i].x, (int)points[i].y];
			if (height > maxHeight || height < minHeight) {
				points.RemoveAt(i);
				i--;
			}
			else {
				float percentage = (height - minHeight) / (maxHeight - minHeight);
				float minProb = heightProbabilityCurve.Evaluate(percentage);
				if (Common.NextFloat(prng, 0f, 1f) > minProb) {
					points.RemoveAt(i);
					i--;
				}
			}
		}

		return points;
	}

	public static List<Vector2> FilterPointsOnRoad(List<Vector2> points, float[,] roadStrengthMap) {
		for (int i = 0; i < points.Count; i++) {
			if (roadStrengthMap[(int)points[i].x, (int)points[i].y] != 0f ||
				roadStrengthMap[(int)points[i].x + 1, (int)points[i].y + 1] != 0f) {
				points.RemoveAt(i);
				i--;
			}
		}

		return points;
	}
}
