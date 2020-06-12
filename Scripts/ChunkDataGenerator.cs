using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChunkDataGenerator
{

    public static ChunkData GenerateChunkData(WorldSettings worldSettings, Vector2 chunkCentre, WorldGenerator worldGenerator) {

        // Generate heightmap and biomestrength data
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(worldSettings.meshSettings.numVerticesPerLine,
                                                                            worldSettings.meshSettings.numVerticesPerLine,
                                                                            worldSettings,
                                                                            chunkCentre,
                                                                            worldGenerator);
        // Generate roads
        Road road = RoadGenerator.GenerateRoads(worldSettings, chunkCentre, biomeData.heightNoiseMap, biomeData.biomeInfo); 

        // Generate objects for chunk
        List<SpawnObject> objects = ObjectGenerator.GenerateBiomeObjects(biomeData.heightNoiseMap, 
                                                                        biomeData.biomeInfo, 
                                                                        road,
                                                                        worldSettings, 
                                                                        chunkCentre);       

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