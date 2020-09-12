using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChunkDataGenerator
{

    public static ChunkData GenerateChunkData(WorldSettings worldSettings, Vector2 chunkCentre, WorldGenerator worldGenerator) {

        // Generate heightmap and biomestrength data
        #if (UNITY_EDITOR && PROFILE)
        float biomeDataStartTime = 0f;
        if (worldSettings.IsMainThread()) {
            biomeDataStartTime = Time.realtimeSinceStartup;
        }
        #endif
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(worldSettings.meshSettings.numVerticesPerLine,
                                                                            worldSettings.meshSettings.numVerticesPerLine,
                                                                            worldSettings,
                                                                            chunkCentre,
                                                                            worldGenerator);
        #if (UNITY_EDITOR && PROFILE)
        if (worldSettings.IsMainThread()) {
            float biomeDataEndTime = Time.realtimeSinceStartup;
            float biomeDataGenTimeTaken = biomeDataEndTime - biomeDataStartTime;
            Debug.Log("Biome Data Generation time taken: " + biomeDataGenTimeTaken + "s");
        }
        #endif

        // Generate roads
        #if (UNITY_EDITOR && PROFILE)
        float roadStartTime = 0f;
        if (worldSettings.IsMainThread()) {
            roadStartTime = Time.realtimeSinceStartup;
        }
        #endif

        Road road = RoadGenerator.GenerateRoads(worldSettings, chunkCentre, biomeData.heightNoiseMap, biomeData.biomeInfo); 
        
        #if (UNITY_EDITOR && PROFILE)
        if (worldSettings.IsMainThread()) {
            float roadEndTime = Time.realtimeSinceStartup;
            float roadGenTimeTaken = roadEndTime - roadStartTime;
            Debug.Log("Road Generation time taken: " + roadGenTimeTaken + "s");
        }
        #endif

        // Generate objects for chunk
        #if (UNITY_EDITOR && PROFILE)
        float objectStartTime = 0f;
        if (worldSettings.IsMainThread()) {
            objectStartTime = Time.realtimeSinceStartup;
        }
        #endif
        List<SpawnObject> objects = ObjectGenerator.GenerateBiomeObjects(biomeData.heightNoiseMap, 
                                                                        biomeData.biomeInfo, 
                                                                        road,
                                                                        worldSettings, 
                                                                        chunkCentre); 
        #if UNITY_EDITOR && PROFILE
        if (worldSettings.IsMainThread()) {
            float objectEndTime = Time.realtimeSinceStartup;
            float objectGenTimeTaken = objectEndTime - objectStartTime;
            Debug.Log("Object Generation time taken: " + objectGenTimeTaken + "s");
        }
        #endif      

        return new ChunkData(biomeData, objects, road);
    }
}

public class ChunkData {
    public BiomeData biomeData;
    public List<SpawnObject> objects;
    public Road road;

    public ChunkData(BiomeData biomeData, List<SpawnObject> objects, Road road) {
        this.biomeData = biomeData;
        this.objects = objects;
        this.road = road;
    }
}