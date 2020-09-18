using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class HeightMapGenerator {

	public enum NormalizeMode { GlobalBiome, Global, Percentage };
	
	public static float[,] GenerateHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											WorldSettings worldSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
												
		float[,] heightMap;
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
				heightMap[i, j] *= heightCurve_threadsafe.Evaluate(heightMap[i, j]) * noiseSettings.heightMultiplier;
			}
		}

		return heightMap;
	}

	public static float[,] GenerateDefaultHeightMap(int width, 
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

		return values;
	}

	public static float[,] GenerateSandDuneHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											MeshSettings meshSettings,
											Vector2 sampleCentre, 
											int seed) {
		
		float[,] values = new float[width, height];
		int padding = (height - meshSettings.meshWorldSize - 3) / 2;
		int chunkIdxY = (int)(sampleCentre.y + padding) / meshSettings.meshWorldSize;

		for (int w = 0; w < noiseSettings.sandDuneSettings.Length; w++) {

			SandDuneSettings settings = noiseSettings.sandDuneSettings[w];
			float duneHeight = ((2 * noiseSettings.sigma * settings.duneWidth) / Mathf.PI) * Mathf.Max(1 - noiseSettings.xm, 0.01f);

			float[,] noiseValues = Noise.GenerateNoiseMap(width,
														height,
														noiseSettings.perlinNoiseSettings,
														sampleCentre,
														NoiseMapSettings.NoiseType.Simplex,
														seed + w);
			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {
					float duneLength = settings.duneWidth + settings.duneGap;
					float duneOffset = noiseValues[i, j] * settings.maxDuneVariation + settings.duneOffset;
					float x = (j + duneOffset + chunkIdxY * (height - (2 * padding) - 3)) % duneLength;
					
					x = (x < 0) ? x + duneLength : x;
					x = Mathf.Clamp(x / settings.duneWidth, 0, 1);
					
					// Calculate dune height and equation according to Laurent Avenel (Wiwine) Terragen tutorial
					float side = (x > noiseSettings.xm) ? 1f : 0f;
					float cosTerm = 1f - Mathf.Cos((Mathf.PI / (noiseSettings.p * side + 1)) * ((x - side) / (noiseSettings.xm - side)));
					float constant = ((noiseSettings.p * side + 1) / 2f);
					
					// Calculate height multiplier with range[0, 1], perlin height value must be > duneThreshold or it is clamped to 0 
					float duneThresholdMultiplier = Mathf.Max(0, noiseValues[i, j] - settings.duneThreshold) / (Mathf.Max(1f - settings.duneThreshold, 0.01f));

					values[i, j] += (constant * cosTerm) * duneHeight * duneThresholdMultiplier;
				}
			}

		}

		return values;
	}
}