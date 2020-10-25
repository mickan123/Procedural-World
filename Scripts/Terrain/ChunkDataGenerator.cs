using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChunkDataGenerator
{
    public static ChunkData GenerateChunkData(TerrainSettings terrainSettings, Vector2 chunkCentre, WorldManager worldGenerator)
    {
        // Generate heightmap and biomestrength data
#if (UNITY_EDITOR && PROFILE)
        float biomeDataStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            biomeDataStartTime = Time.realtimeSinceStartup;
        }
#endif
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(
            terrainSettings.meshSettings.numVerticesPerLine,
            terrainSettings.meshSettings.numVerticesPerLine,
            terrainSettings,
            chunkCentre,
            worldGenerator
        );
#if (UNITY_EDITOR && PROFILE)
        if (terrainSettings.IsMainThread()) {
            float biomeDataEndTime = Time.realtimeSinceStartup;
            float biomeDataGenTimeTaken = biomeDataEndTime - biomeDataStartTime;
            Debug.Log("Biome Data Generation time taken: " + biomeDataGenTimeTaken + "s");
        }
#endif

        // Generate roads
#if (UNITY_EDITOR && PROFILE)
        float roadStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            roadStartTime = Time.realtimeSinceStartup;
        }
#endif

        Road road = RoadGenerator.GenerateRoads(terrainSettings, chunkCentre, biomeData.heightNoiseMap, biomeData.biomeInfo);

#if (UNITY_EDITOR && PROFILE)
        if (terrainSettings.IsMainThread()) {
            float roadEndTime = Time.realtimeSinceStartup;
            float roadGenTimeTaken = roadEndTime - roadStartTime;
            Debug.Log("Road Generation time taken: " + roadGenTimeTaken + "s");
        }
#endif

        // Generate objects for chunk
#if (UNITY_EDITOR && PROFILE)
        float objectStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            objectStartTime = Time.realtimeSinceStartup;
        }
#endif

		terrainSettings.SetBiomeGraphHeightMap(biomeData.heightNoiseMap);
        List<ObjectSpawner> objects = ObjectGenerator.GenerateObjectSpawners(
			biomeData.heightNoiseMap,
			biomeData.biomeInfo,
			road,
			terrainSettings,
			chunkCentre
        );

#if UNITY_EDITOR && PROFILE
        if (terrainSettings.IsMainThread()) {
            float objectEndTime = Time.realtimeSinceStartup;
            float objectGenTimeTaken = objectEndTime - objectStartTime;
            Debug.Log("Object Generation time taken: " + objectGenTimeTaken + "s");
        }
#endif

        return new ChunkData(biomeData, objects, road);
    }
}

public class ChunkData
{
    public BiomeData biomeData;
    public List<ObjectSpawner> objects;
    public Road road;

    public ChunkData(BiomeData biomeData, List<ObjectSpawner> objects, Road road)
    {
        this.biomeData = biomeData;
        this.objects = objects;
        this.road = road;
    }
}