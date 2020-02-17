using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectGenerator {

	public static List<TerrainObject> GenerateBiomeObjects(NoiseMap noiseMap, BiomeInfo info, WorldSettings settings) {
		List<TerrainObject> biomeObjects = new List<TerrainObject>();
		
		int mapSize = noiseMap.values.GetLength(0);

		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {
				
			}
		}
		
		biomeObjects.Add(settings.biomes[0].terrainObjects[0]);

		return biomeObjects;
	}

}
