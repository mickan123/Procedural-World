using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ObjectGenerator {

	public static List<SpawnObject> GenerateBiomeObjects(float[,] heightMap, BiomeInfo info, Road road, TerrainSettings settings, Vector2 sampleCentre) {
		List<SpawnObject> biomeObjects = new List<SpawnObject>();
		
		System.Random prng = new System.Random((int)(sampleCentre.x + sampleCentre.y));
		
		for (int biome = 0; biome < settings.biomeSettings.Count; biome++) {
			
			if (HeightMapContainesBiome(info, biome)) {
				for (int objectSetting = 0; objectSetting < settings.biomeSettings[biome].terrainObjectSettings.Count; objectSetting++) {
					biomeObjects.Add(GenerateTerrainObject(settings.biomeSettings[biome].terrainObjectSettings[objectSetting], 
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
													TerrainSettings terrainSettings, 
													Vector2 sampleCentre,
													System.Random prng) {

																	
		// Generate spawn points by selected algorithm
		List<Vector3> points;
		if (settings.spawnMode == TerrainObjectSettings.SpawnMode.PoissonDiskSampling) {
			points = PoissonDiskSampling.GeneratePoints(settings, sampleCentre, heightMap, prng, terrainSettings, biome);
		}
		else if (settings.spawnMode == TerrainObjectSettings.SpawnMode.Random) {
			points = RandomPoints.GeneratePoints(settings, prng, heightMap);
		}
		else {
			points = PoissonDiskSampling.GeneratePoints(settings, sampleCentre, heightMap,  prng, terrainSettings, biome);
		}

		// Filter points dependent on settings
		FilterSpawnPoints(ref points, settings, biome, info, heightMap, roadStrengthMap, prng);

		// Generate object positions
		List<ObjectPosition> spawnPositions = new List<ObjectPosition>();
		for (int point = 0; point < points.Count; point++) {
			spawnPositions.Add(new ObjectPosition(points[point] + settings.GetTranslation(prng), 
												  settings.GetScale(prng), 
												  settings.GetRotation(prng)));
		}

		return new SpawnObject(settings.terrainObjects, 
								spawnPositions, 
								prng,
								settings.hide);
	}

	public static void FilterSpawnPoints(ref List<Vector3> points, 
										 TerrainObjectSettings settings, 
										 int biome, 
										 BiomeInfo info, 
										 float[,] heightMap, 
										 float[,] roadStrengthMap,
										 System.Random prng) {

		FilterPointsByBiome(ref points, biome, info, prng);
		if (settings.constrainSlope) {
			FilterPointsBySlope(ref points, settings.minSlope, settings.maxSlope, heightMap);
		}
		if (settings.constrainHeight) {
			AnimationCurve threadSafeCurve = new AnimationCurve(settings.heightProbabilityCurve.keys);
			FilterPointsByHeight(ref points, settings.minHeight, settings.maxHeight, heightMap, settings.heightProbabilityCurve, prng);
		}
		if (!settings.spawnOnRoad) {
			FilterPointsOnRoad(ref points, roadStrengthMap);
		}
	}

	public static void FilterPointsByBiome(ref List<Vector3> points, int biome, BiomeInfo info, System.Random prng) {
		for (int i = 0; i < points.Count; i++) {
			float rand = (float)prng.NextDouble(); 

			int coordX = (int) points[i].x;
        	int coordZ = (int) points[i].z;

			if (rand > info.biomeStrengths[coordX, coordZ, biome] * info.biomeStrengths[coordX, coordZ, biome] * info.biomeStrengths[coordX, coordZ, biome]) {
				points.RemoveAt(i);
				i--;
			}
		}
	}

	public static void FilterPointsBySlope(ref List<Vector3> points, float minSlope, float maxSlope, float[,] heightMap) {
		for (int i = 0; i < points.Count; i++) {
			float slope = Common.CalculateSlope(points[i].x, points[i].z, heightMap);
			if (slope > maxSlope || slope < minSlope) {
				points.RemoveAt(i);
				i--;
			}
		}
	}

	public static void FilterPointsByHeight(ref List<Vector3> points, 
											float minHeight, 
											float maxHeight, 
											float[,] heightMap,
											AnimationCurve heightProbabilityCurve,
											System.Random prng) {
		
		for (int i = 0; i < points.Count; i++) {
			float height = heightMap[(int)points[i].x, (int)points[i].z];
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
	}

	public static void  FilterPointsOnRoad(ref List<Vector3> points, float[,] roadStrengthMap) {
		for (int i = 0; i < points.Count; i++) {
			float roadStrength = Common.HeightFromFloatCoord(points[i].x, points[i].z, roadStrengthMap);
			if (roadStrength > 0f) {
				points.RemoveAt(i);
				i--;
			}
		}
	}
}
