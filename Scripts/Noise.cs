﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class Noise {


	public static float[,] GenerateNoiseMap(int mapWidth, 
											int mapHeight, 
											NoiseSettings noiseSettings, 
											Vector2 sampleCentre,
											int seed) {

		float[,] noiseMap = new float[mapWidth, mapHeight];
		System.Random prng = new System.Random(seed);

		// Calculate octave offsets for max num of octaves and calculate max possible height at same time
		Vector2[] octaveOffsets = new Vector2[noiseSettings.octaves];
		
		for (int i = 0; i < noiseSettings.octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + sampleCentre.x;
			float offsetY = prng.Next(-100000, 100000) - sampleCentre.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		float amplitude = 1;
		float frequency = 1;

 		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < noiseSettings.octaves; i++) {
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseSettings.scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseSettings.scale * frequency;
					
					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude; 

					amplitude *= noiseSettings.persistance;
					frequency *= noiseSettings.lacunarity; 
				}

				noiseMap[x, y] = noiseHeight;
			}
		}
		return noiseMap;
	}

	public static float[,] normalizeGlobalBiomeValues(float[,] input, WorldSettings worldSettings) {

		int maxNumOctaves = 1;
		float maxPersistance = 0;
		NoiseSettings[] noiseSettingArray = worldSettings.biomes.Select(x => x.heightMapSettings.noiseSettings).ToArray();
		for (int i = 0; i < noiseSettingArray.Length; i++) {
			if (noiseSettingArray[i].octaves > maxNumOctaves) {
				maxNumOctaves = noiseSettingArray[i].octaves;
			}
			if (noiseSettingArray[i].persistance > maxPersistance) {
				maxPersistance = noiseSettingArray[i].persistance;
			}
		}

		float maxPossibleHeight = 0;
		float amplitude = 1;
		for (int i = 0; i < maxNumOctaves; i++) {
			maxPossibleHeight += amplitude; 
			amplitude *= maxPersistance;
		}

		for (int i = 0; i < input.GetLength(0); i++) {
			for (int j = 0 ; j < input.GetLength(1); j++) {
				float normalizedHeight = (input[i, j] + 1) / (2f * maxPossibleHeight); // MAGIC NUMBER
				input[i, j] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);	
			}
		}

		return input;
	}

	public static float[,] normalizeGlobalValues(float[,] input, NoiseSettings noiseSettings) {
		float maxPossibleHeight = 0;
		float amplitude = 1;
		for (int i = 0; i < noiseSettings.octaves; i++) {
			maxPossibleHeight += amplitude; 
			amplitude *= noiseSettings.persistance;
		}

		for (int i = 0; i < input.GetLength(0); i++) {
			for (int j = 0 ; j < input.GetLength(1); j++) {
				input[i, j] = (input[i, j] + maxPossibleHeight) / (2 * maxPossibleHeight);
			}
		}

		return input;
	}
}

[System.Serializable]
public class NoiseSettings {
	public float scale = 50;

	public int octaves = 4;
	[Range(0, 1)]
	public float persistance = 0.3f;
	public float lacunarity = 2.8f;

	public void ValidateValues() {
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		octaves = Mathf.Min(octaves, 10);
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}
