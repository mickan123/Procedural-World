using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class HeightMapGenerator {

	public enum NormalizeMode { GlobalBiome, Global };
	
	public static HeightMap GenerateHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											WorldSettings worldSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
		float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings.noiseSettings, sampleCentre, seed);

		if (normalizeMode == NormalizeMode.GlobalBiome) {
			values = Noise.normalizeGlobalBiomeValues(values, worldSettings);
		}
		else if (normalizeMode == NormalizeMode.Global) {
			values = Noise.normalizeGlobalValues(values, noiseSettings.noiseSettings);
		}

		AnimationCurve heightCurve_threadsafe = new AnimationCurve(noiseSettings.heightCurve.keys);

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * noiseSettings.heightMultiplier;
			}
		}
		
		return new HeightMap(values);
	}

	
}

public struct BiomeInfo {
	public readonly int[,] biomeMap; // Holds index of biome at each point
	public readonly float[,,] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0.5, 1]
	
	public BiomeInfo(int[,] biomeMap, float[,,] biomeStrengths) {
		this.biomeMap = biomeMap;
		this.biomeStrengths = biomeStrengths;
	}
}

public struct HeightMap {
	public readonly float[,] values;

	public HeightMap(float[,] values) {
		this.values = values;
	}
}