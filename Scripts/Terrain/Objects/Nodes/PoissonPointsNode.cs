using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/PoissonPoints")]
public class PoissonPointsNode : BiomeGraphNode
{
    [Output] public ObjectPositionData positionData;

    
    public PoissonDiskSamplingSettings settings;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "positionData")
        {
            return GetPositionData();
        }
        else
        {
            return null;
        }
    }

    public ObjectPositionData GetPositionData()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        int mapSize = heightMapData.heightMap.Length;

        // Copy heightmap into native array
        NativeArray<float> heightMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        for (int i = 0; i < mapSize; i++)
        {
            int start = i * mapSize;
            heightMapNat.GetSubArray(start, mapSize).CopyFrom(heightMapData.heightMap[i]);
        }

        // Initialize spawnNoiseMap 
        NativeArray<float> spawnNoiseMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        float[][] spawnNoiseMap;
        if (settings.varyRadius)
        {
            spawnNoiseMap = Noise.GenerateNoiseMap(
                mapSize,
                mapSize,
                settings.noiseMapSettings.perlinNoiseSettings,
                heightMapData.sampleCentre,
                settings.noiseMapSettings.noiseType,
                settings.noiseMapSettings.seed
            );
            for (int i = 0; i < mapSize; i++)
            {
                int start = i * mapSize;
                spawnNoiseMapNat.GetSubArray(start, mapSize).CopyFrom(spawnNoiseMap[i]);
            }
        }

        // Create output native arrays
        NativeList<float> poissonPointsX = new NativeList<float>(Allocator.TempJob);
        NativeList<float> poissonPointsY = new NativeList<float>(Allocator.TempJob);
        NativeList<float> poissonPointsZ = new NativeList<float>(Allocator.TempJob);

        // Run poisson points job
        PoissonDiskSampling.PoissonDiskSamplingJob burstJob = new PoissonDiskSampling.PoissonDiskSamplingJob
        {
            heightMap = heightMapNat,
            spawnNoiseMap = spawnNoiseMapNat,
            sampleCentre = heightMapData.sampleCentre,
            xCoords = poissonPointsX,
            yCoords = poissonPointsY,
            zCoords = poissonPointsZ,
            meshScale = heightMapData.terrainSettings.meshSettings.meshScale,
            numSamplesBeforeRejection = 25,
            varyRadius = settings.varyRadius,
            radius = settings.radius,
            minRadius = settings.minRadius,
            maxRadius = settings.maxRadius,
            mapSize = mapSize,
            seed = this.seed
        };
        burstJob.Schedule().Complete();

        // Read outputs
        ObjectPositions positions = new ObjectPositions(poissonPointsX.ToArray(), poissonPointsY.ToArray(), poissonPointsZ.ToArray());

        // Cleanup native arrays
        heightMapNat.Dispose();
        spawnNoiseMapNat.Dispose();
        poissonPointsX.Dispose();
        poissonPointsY.Dispose();
        poissonPointsZ.Dispose();

        return new ObjectPositionData(positions, heightMapData.heightMap);
    }
}