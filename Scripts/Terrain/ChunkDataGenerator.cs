using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Stella3D;
using Unity.Collections;

public static class ChunkDataGenerator
{
    public static ChunkData GenerateChunkData(TerrainSettings terrainSettings, Vector2 chunkCentre)
    {
        // Generate heightmap and biomestrength data
        BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(
            terrainSettings.resolution,
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
			chunkCentre,
            terrainSettings.scale
        );
        
        int width = biomeData.width;
        NativeArray<float> heightMapNat = new NativeArray<float>(biomeData.heightNoiseMap, Allocator.TempJob);
        SharedArray<float> anglesNat = new SharedArray<float>(width * width); // Use shared array so that we convert between native and non native with no cost

        Common.CalculateAnglesJob burstJob = new Common.CalculateAnglesJob {
            heightMap = heightMapNat,
            angles = anglesNat,
            width = width,
            scale = terrainSettings.scale
        };
        burstJob.Schedule().Complete();

        ChunkData chunkData = new ChunkData(biomeData, objects, anglesNat, roadData.roadStrengthMap);

        anglesNat.Dispose();
        heightMapNat.Dispose();

        return chunkData;
    }
}

public struct ChunkData
{
    public BiomeData biomeData;
    public List<ObjectSpawner> objects;
    public float[] roadStrengthMap;
    public float[] angles;

    public ChunkData(BiomeData biomeData, List<ObjectSpawner> objects, float[] angles, float[] roadStrengthMap)
    {
        this.biomeData = biomeData;
        this.objects = objects;
        this.roadStrengthMap = roadStrengthMap;
        this.angles = angles;
    }
}