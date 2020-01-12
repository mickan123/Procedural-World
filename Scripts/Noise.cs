using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

	public enum NormalizeMode { Local, Global };

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre, int seed) {

		float[,] noiseMap = new float[mapWidth, mapHeight];
		System.Random prng = new System.Random(seed);
		
		// Calculate octave offsets for max num of octaves and calculate max possible height at same time
		Vector2[] octaveOffsets = new Vector2[settings.octaves];
		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;
		for (int i = 0; i < settings.octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + sampleCentre.x;
			float offsetY = prng.Next(-100000, 100000) - sampleCentre.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);
			
			// Update max height
			maxPossibleHeight += amplitude; 
			amplitude *= settings.persistance;
		}

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

 		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < settings.octaves; i++) {
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;
					
					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude; 

					amplitude *= settings.persistance;
					frequency *= settings.lacunarity; 
				}

				noiseMap[x, y] = noiseHeight;
				
				// Normalise the height globally
				float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.5f); // MAGIC NUMBER
				noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);				
			}
		}

		return noiseMap;
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
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}
