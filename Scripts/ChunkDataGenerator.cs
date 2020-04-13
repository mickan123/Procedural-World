using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChunkDataGenerator
{

    public static ChunkData GenerateChunkData(WorldSettings worldSettings, Vector2 chunkCentre) {

        // Generate heightmap and biomestrength data
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(worldSettings.meshSettings.numVerticesPerLine,
                                                                            worldSettings.meshSettings.numVerticesPerLine,
                                                                            worldSettings,
                                                                            chunkCentre);
        
        // Generate objects for chunk
        List<SpawnObject> objects = ObjectGenerator.GenerateBiomeObjects(biomeData.heightNoiseMap, 
                                                                        biomeData.biomeInfo, 
                                                                        worldSettings, 
                                                                        chunkCentre);
        
        // Generate roads
        RoadGenerator.GenerateRoads(worldSettings, chunkCentre);


        return new ChunkData(biomeData, objects);
    }

    
}

public class ChunkData {
    public BiomeData biomeData;
    public List<SpawnObject> objects;

    public ChunkData(BiomeData biomeData, List<SpawnObject> objects) {
        this.biomeData = biomeData;
        this.objects = objects;
    }
}