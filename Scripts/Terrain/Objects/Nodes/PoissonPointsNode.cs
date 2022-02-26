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
        System.Random prng = new System.Random(this.seed);

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

        NativeList<Vector3> pointsNat = new NativeList<Vector3>(Allocator.TempJob);

        PoissonDiskSampling.PoissonDiskSamplingJob burstJob = new PoissonDiskSampling.PoissonDiskSamplingJob
        {
            heightMap = heightMapNat,
            spawnNoiseMap = spawnNoiseMapNat,
            sampleCentre = heightMapData.sampleCentre,
            points = pointsNat,
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

        List<Vector3> points = new List<Vector3>(pointsNat.ToArray());

        heightMapNat.Dispose();
        spawnNoiseMapNat.Dispose();
        pointsNat.Dispose();

        float[] xCoords = new float[points.Count];
        float[] yCoords = new float[points.Count];
        float[] zCoords = new float[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            xCoords[i] = points[i].x;
            yCoords[i] = points[i].y;
            zCoords[i] = points[i].z;
        }

        ObjectPositions positions = new ObjectPositions(xCoords, yCoords, zCoords);

        return new ObjectPositionData(positions, heightMapData.heightMap);
    }
}