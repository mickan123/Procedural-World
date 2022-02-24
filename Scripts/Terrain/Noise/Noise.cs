﻿using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

public static class Noise
{

    [BurstCompile]
    struct CalculateNoiseJob : IJob
    {
        [WriteOnly]
        public NativeArray<float> noiseMap;

        [ReadOnly]
        public NativeArray<float> octaveOffsetsX;
        [ReadOnly]
        public NativeArray<float> octaveOffsetsY;

        public NoiseMapSettings.NoiseType noiseType;

        public int width;
        public int height;

        public float persistance;
        public float lacunarity;
        public float scale;
        public int octaves;

        public void Execute()
        {
            float amplitude = 1;
            float frequency = 1;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    amplitude = 1;
                    frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x + octaveOffsetsX[i]) / scale * frequency;
                        float sampleY = (y + octaveOffsetsY[i]) / scale * frequency;

                        float noiseValue = 0f;
                        if (noiseType == NoiseMapSettings.NoiseType.Perlin)
                        {
                            noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                        }
                        else if (noiseType == NoiseMapSettings.NoiseType.Simplex)
                        {
                            // TODO get simplex noise for burst compiler
                            noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                        }


                        noiseHeight += noiseValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }
                    noiseMap[x * width + y] = noiseHeight;
                }
            }
        }
    }

    public static float[][] GenerateNoiseMap(int width,
                                            int height,
                                            PerlinNoiseSettings noiseSettings,
                                            Vector2 sampleCentre,
                                            NoiseMapSettings.NoiseType noiseType,
                                            int seed)
    {

        float[][] noiseMap = new float[width][];
        for (int i = 0; i < width; i++)
        {
            noiseMap[i] = new float[height];
        }
        System.Random prng = new System.Random(seed);

        // Calculate octave offsets for max num of octaves and calculate max possible height at same time

        NativeArray<float> octaveOffsetsX = new NativeArray<float>(noiseSettings.octaves, Allocator.TempJob);
        NativeArray<float> octaveOffsetsY = new NativeArray<float>(noiseSettings.octaves, Allocator.TempJob);

        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            octaveOffsetsX[i] = prng.Next(-100000, 100000) + sampleCentre.x;
            octaveOffsetsY[i] = prng.Next(-100000, 100000) + sampleCentre.y;
        }

        NativeArray<float> noiseMapNat = new NativeArray<float>(width * height, Allocator.TempJob);

        CalculateNoiseJob burstJob = new CalculateNoiseJob
        {
            width = width,
            height = height,
            noiseMap = noiseMapNat,
            octaveOffsetsX = octaveOffsetsX,
            octaveOffsetsY = octaveOffsetsY,
            noiseType = noiseType,
            persistance = noiseSettings.persistance,
            lacunarity = noiseSettings.lacunarity,
            scale = noiseSettings.scale,
            octaves = noiseSettings.octaves
        };
        burstJob.Schedule().Complete();

        for (int i = 0; i < height; i++)
        {   
            int start = i * width;
            noiseMapNat.GetSubArray(start, width).CopyTo(noiseMap[i]);
        }

        noiseMapNat.Dispose();
        octaveOffsetsX.Dispose();
        octaveOffsetsY.Dispose();

        return noiseMap;
    }

    public static float[][] normalizeGlobalBiomeValues(float[][] input, TerrainSettings terrainSettings)
    {

        float maxPossibleHeight = float.MinValue;

        for (int i = 0; i < terrainSettings.biomeSettings.Length; i++)
        {
            float height = terrainSettings.biomeSettings[i].biomeGraph.GetMaxPossibleHeight();
            if (height > maxPossibleHeight)
            {
                maxPossibleHeight = height;
            }
        }

        // Normalize by max possible height
        int mapSize = input.Length;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                float normalizedHeight = input[i][j] / maxPossibleHeight;
                input[i][j] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);
            }
        }

        return input;
    }

    public static float[][] normalizeGlobalValues(float[][] input, PerlinNoiseSettings noiseSettings)
    {

        // Calculate max possible height
        float maxPossibleHeight = 0;
        float amplitude = 1;
        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            maxPossibleHeight += amplitude;
            amplitude *= noiseSettings.persistance;
        }

        // Normalize by max possible height
        int mapSize = input.Length;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                input[i][j] = input[i][j] / maxPossibleHeight;
            }
        }

        return input;
    }

    public static float[][] normalizeLocal(float[][] input)
    {
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;
        int mapSize = input.Length;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                if (input[i][j] > maxHeight)
                {
                    maxHeight = input[i][j];
                }
                if (input[i][j] < minHeight)
                {
                    minHeight = input[i][j];
                }
            }
        }

        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                input[i][j] = (input[i][j] - minHeight) / (maxHeight - minHeight);
            }
        }

        return input;
    }
}

