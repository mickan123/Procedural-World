using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class HeightMapGenerator {

	public enum NormalizeMode { GlobalBiome, Global, Percentage };
	
	public static HeightMap GenerateHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											WorldSettings worldSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
												
		HeightMap heightMap;
		if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.Simplex ||
		    noiseSettings.noiseType == NoiseMapSettings.NoiseType.Perlin) {
			heightMap = GenerateDefaultHeightMap(width, height, noiseSettings, worldSettings, sampleCentre, normalizeMode, seed);
		} 
		else if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.SandDune) {
			heightMap = GenerateSandDuneHeightMap(width, height, noiseSettings, worldSettings.meshSettings, sampleCentre, seed);
		}
		else {
			heightMap = GenerateDefaultHeightMap(width, height, noiseSettings, worldSettings, sampleCentre, normalizeMode, seed);
		}
		
		AnimationCurve heightCurve_threadsafe = new AnimationCurve(noiseSettings.heightCurve.keys);

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				heightMap.values[i, j] *= heightCurve_threadsafe.Evaluate(heightMap.values[i, j]) * noiseSettings.heightMultiplier;
			}
		}

		return heightMap;
	}

	public static HeightMap GenerateDefaultHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											WorldSettings worldSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
		
		float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings.perlinNoiseSettings, sampleCentre, noiseSettings.noiseType, seed);

		if (normalizeMode == NormalizeMode.GlobalBiome) {
			values = Noise.normalizeGlobalBiomeValues(values, worldSettings);
		}
		else if (normalizeMode == NormalizeMode.Global) {
			values = Noise.normalizeGlobalValues(values, noiseSettings.perlinNoiseSettings);
		}
		else if (normalizeMode == NormalizeMode.Percentage) {
			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {
					values[i, j] = (values[i, j] + 1) / 2;
				}
			}
		}

		return new HeightMap(values);
	}

	public static HeightMap GenerateSandDuneHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											MeshSettings meshSettings,
											Vector2 sampleCentre, 
											int seed) {
		
		float[,] values = new float[width, height];

		float[,] noiseValues = Noise.GenerateNoiseMap(width,
													height,
													noiseSettings.perlinNoiseSettings,
													sampleCentre,
													NoiseMapSettings.NoiseType.Simplex,
													seed);

		int chunkIdxX = (int)sampleCentre.x / meshSettings.meshWorldSize;
		int chunkIdxY = (int)sampleCentre.y / meshSettings.meshWorldSize;
		
		for (int w = 0; w < noiseSettings.sandDuneSettings.Length; w++) {

			SandDuneSettings settings = noiseSettings.sandDuneSettings[w];
			float duneHeight = ((2 * settings.sigma * settings.duneWidth) / Mathf.PI) * Mathf.Max(1 - settings.xm, 0.01f);

			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {	

					float x = CalculateDunePos(settings, height, chunkIdxY, w, j, noiseValues[i, j]);

					// Calculate dune height and equation according to Laurent Avenel (Wiwine) Terragen tutorial
					float side = (x > settings.xm) ? 1f : 0f;
					float cosTerm = 1f - Mathf.Cos((Mathf.PI / (settings.p * side + 1)) * ((x - side) / (settings.xm - side)));
					float constant = ((settings.p * side + 1) / 2f);
					
					// Calculate height multiplier with range[0, 1], perlin height value must be > duneThreshold or it is clamped to 0 
					float duneThresholdMultiplier = Mathf.Max(0, noiseValues[i, j] - settings.duneThreshold) / (Mathf.Max(1f - settings.duneThreshold, 0.01f));

					values[i, j] += (constant * cosTerm) * duneHeight * duneThresholdMultiplier;
				}
			}
		}

		return new HeightMap(values);
	}

	public static float CalculateDunePos(SandDuneSettings settings, int height, int chunkIdxY, int dune, int j, float noiseVal) {
		// Calculate where along sand dune we are:
		// [0, xm] = on the curve upwards to peak 
		// [xm, 1) = on the downwards curve
		// [1, inf] = gap between dunes
		 // Random offset to generate wavey peaks
		float duneOffset = noiseVal * settings.maxDuneVariation + settings.duneOffset; // height - 1 is max value for height, subtract another 
		float duneLength = settings.duneWidth + settings.duneGap;
		float x = (j + duneOffset + chunkIdxY * (3 - height)) % duneLength; 
		x = x < 0 ? x + duneLength : x;
		x = Mathf.Clamp(x / settings.duneWidth, 0, 1);
		return x;
	}
}

public struct BiomeInfo {
	public readonly int[,] biomeMap; // Holds index of biome at each point
	public readonly float[,,] biomeStrengths; // E.g. 0.75 means 75-25 main biome nearest biome blend, has values in range [0, 1]
	public readonly int mainBiome;
	
	public BiomeInfo(int[,] biomeMap, float[,,] biomeStrengths, int mainBiome) {
		this.biomeMap = biomeMap;
		this.biomeStrengths = biomeStrengths;
		this.mainBiome = mainBiome;
	}
}

public struct HeightMap {
	public readonly float[,] values;

	public HeightMap(float[,] values) {
		this.values = values;
	}
}