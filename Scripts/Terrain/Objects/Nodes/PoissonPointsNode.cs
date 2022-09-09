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
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        if (!biomeGraph.ContainsKey(System.Threading.Thread.CurrentThread) || port.fieldName != "positionData")
        {
            return null;
        }
        return GetPositionData();
    }

    public ObjectPositionData GetPositionData()
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        int width = heightMapData.width;

        // Copy heightmap into native array
        NativeArray<float> heightMapNat = new NativeArray<float>(heightMapData.heightMap, Allocator.TempJob);

        // Initialize spawnNoiseMap 
        float[] spawnNoiseMap = new float[width * width];
        if (settings.varyRadius)
        {
            spawnNoiseMap = Noise.GenerateNoiseMap(
                width,
                settings.noiseMapSettings.perlinNoiseSettings,
                heightMapData.sampleCentre,
                settings.noiseMapSettings.noiseType,
                settings.noiseMapSettings.seed
            );
        }
        NativeArray<float> spawnNoiseMapNat = new NativeArray<float>(spawnNoiseMap, Allocator.TempJob);

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
            numSamplesBeforeRejection = 25,
            varyRadius = settings.varyRadius,
            radius = settings.radius,
            minRadius = settings.minRadius,
            maxRadius = settings.maxRadius,
            width = width,
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

        return new ObjectPositionData(positions, heightMapData.heightMap, width);
    }
}