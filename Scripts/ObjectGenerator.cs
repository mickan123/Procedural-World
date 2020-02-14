using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectGenerator {

	public static List<TerrainObject> GenerateBiomeObjects(WorldSettings settings, BiomeInfo info) {
		List<TerrainObject> biomeObjects = new List<TerrainObject>();
		
		biomeObjects.Add(settings.biomes[0].terrainObjects[0]);

		return biomeObjects;
	}

}
