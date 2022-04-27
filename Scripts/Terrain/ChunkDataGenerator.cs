using System.Collections.Generic;
using UnityEngine;

public static class ChunkDataGenerator
{
    public static ChunkData GenerateChunkData(TerrainSettings terrainSettings, Vector2 chunkCentre)
    {
        // Generate heightmap and biomestrength data
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(
            terrainSettings.meshSettings.numVerticesPerLine,
            terrainSettings.meshSettings.numVerticesPerLine,
            terrainSettings,
            chunkCentre
        );

        // Generate roads
        RoadData roadData = RoadGenerator.GenerateRoads(terrainSettings, chunkCentre, biomeData.heightNoiseMap, biomeData.biomeInfo);
        biomeData.heightNoiseMap = roadData.heightMap;

        // Generate objects for chunk
        List<ObjectSpawner> objects = ObjectGenerator.GenerateObjectSpawners(
			biomeData.heightNoiseMap,
			biomeData.biomeInfo,
			roadData.roadStrengthMap,
			terrainSettings,
			chunkCentre
        );

        return new ChunkData(biomeData, objects, roadData.roadStrengthMap);
    }
}

public struct ChunkData
{
    public BiomeData biomeData;
    public List<ObjectSpawner> objects;
    public float[] roadStrengthMap;

    public ChunkData(BiomeData biomeData, List<ObjectSpawner> objects, float[] roadStrengthMap)
    {
        this.biomeData = biomeData;
        this.objects = objects;
        this.roadStrengthMap = roadStrengthMap;
    }
}