using System.Collections.Generic;
using UnityEngine;

public static class ChunkDataGenerator
{
    public static ChunkData GenerateChunkData(TerrainSettings terrainSettings, Vector2 chunkCentre)
    {
        // Generate heightmap and biomestrength data
#if UNITY_EDITOR
        float biomeDataStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            biomeDataStartTime = Time.realtimeSinceStartup;
        }
#endif
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(
            terrainSettings.meshSettings.numVerticesPerLine,
            terrainSettings.meshSettings.numVerticesPerLine,
            terrainSettings,
            chunkCentre
        );
#if UNITY_EDITOR
        if (terrainSettings.IsMainThread()) {
            float biomeDataEndTime = Time.realtimeSinceStartup;
            float biomeDataGenTimeTaken = biomeDataEndTime - biomeDataStartTime;
            Debug.Log("BiomeData Gen time taken: " + biomeDataGenTimeTaken + "s");
        }
#endif

        // Generate roads
#if UNITY_EDITOR
        float roadStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            roadStartTime = Time.realtimeSinceStartup;
        }
#endif

        RoadData roadData = RoadGenerator.GenerateRoads(terrainSettings, chunkCentre, biomeData.heightNoiseMap, biomeData.biomeInfo);
        biomeData.heightNoiseMap = roadData.heightMap;

#if UNITY_EDITOR
        if (terrainSettings.IsMainThread()) {
            float roadEndTime = Time.realtimeSinceStartup;
            float roadGenTimeTaken = roadEndTime - roadStartTime;
            Debug.Log("Road Generation time taken: " + roadGenTimeTaken + "s");
        }
#endif

        // Generate objects for chunk
#if UNITY_EDITOR
        float objectStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            objectStartTime = Time.realtimeSinceStartup;
        }
#endif

        List<ObjectSpawner> objects = ObjectGenerator.GenerateObjectSpawners(
			biomeData.heightNoiseMap,
			biomeData.biomeInfo,
			roadData.roadStrengthMap,
			terrainSettings,
			chunkCentre
        );

#if UNITY_EDITOR
        if (terrainSettings.IsMainThread()) {
            float objectEndTime = Time.realtimeSinceStartup;
            float objectGenTimeTaken = objectEndTime - objectStartTime;
            Debug.Log("Object Generation time taken: " + objectGenTimeTaken + "s");
        }
#endif

        return new ChunkData(biomeData, objects, roadData.roadStrengthMap);
    }
}

public class ChunkData
{
    public BiomeData biomeData;
    public List<ObjectSpawner> objects;
    public float[,] roadStrengthMap;

    public ChunkData(BiomeData biomeData, List<ObjectSpawner> objects, float[,] roadStrengthMap)
    {
        this.biomeData = biomeData;
        this.objects = objects;
        this.roadStrengthMap = roadStrengthMap;
    }
}