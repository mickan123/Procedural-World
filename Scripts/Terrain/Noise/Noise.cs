﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using XNode;

public static class Noise
{

    public static float[,] GenerateNoiseMap(int mapWidth,
                                            int mapHeight,
                                            PerlinNoiseSettings noiseSettings,
                                            Vector2 sampleCentre,
                                            NoiseMapSettings.NoiseType noiseType,
                                            int seed)
    {

        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(seed);

        // Calculate octave offsets for max num of octaves and calculate max possible height at same time
        Vector2[] octaveOffsets = new Vector2[noiseSettings.octaves];

        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + sampleCentre.x;
            float offsetY = prng.Next(-100000, 100000) + sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float amplitude = 1;
        float frequency = 1;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < noiseSettings.octaves; i++)
                {
                    float sampleX = (x + octaveOffsets[i].x) / noiseSettings.scale * frequency;
                    float sampleY = (y + octaveOffsets[i].y) / noiseSettings.scale * frequency;

                    float noiseValue = 0f;
                    if (noiseType == NoiseMapSettings.NoiseType.Perlin)
                    {
                        noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                    }
                    else if (noiseType == NoiseMapSettings.NoiseType.Simplex)
                    {
                        float simplexNoiseVal = NoiseFunctions.Simplex2D(new Vector3(sampleX, sampleY, 0f), 1f).value;
                        noiseValue = (simplexNoiseVal + 1f) / 2; // Convert from [-1, 1] to [0, 1]
                    }

                    noiseHeight += noiseValue * amplitude;

                    amplitude *= noiseSettings.persistance;
                    frequency *= noiseSettings.lacunarity;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }
        return noiseMap;
    }

    public static float[,] normalizeGlobalBiomeValues(float[,] input, TerrainSettings terrainSettings)
    {

        float maxPossibleHeight = float.MinValue;

        for (int i = 0; i < terrainSettings.biomeSettings.Count; i++)
        {
            float height = terrainSettings.biomeSettings[i].biomeGraph.GetMaxPossibleHeight();
            if (height > maxPossibleHeight)
            {
                maxPossibleHeight = height;
            }
        }

        // Normalize by max possible height
        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                float normalizedHeight = input[i, j] / maxPossibleHeight;
                input[i, j] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);
            }
        }

        return input;
    }

    public static float[,] normalizeGlobalValues(float[,] input, PerlinNoiseSettings noiseSettings)
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
        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                input[i, j] = input[i, j] / maxPossibleHeight;
            }
        }

        return input;
    }

    public static float[,] normalizeLocal(float[,] input)
    {
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;
        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                if (input[i, j] > maxHeight)
                {
                    maxHeight = input[i, j];
                }
                if (input[i, j] < minHeight)
                {
                    minHeight = input[i, j];
                }
            }
        }

        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                input[i, j] = (input[i, j] - minHeight) / (maxHeight - minHeight);
            }
        }

        return input;
    }
}

