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

		float[,] noiseValues = Noise.GenerateNoiseMap(width,
													height,
													noiseSettings.perlinNoiseSettings,
													sampleCentre,
													NoiseMapSettings.NoiseType.Simplex,
													seed);

		int chunkIdxY = (int)sampleCentre.y / meshSettings.meshWorldSize;
		
		SandDuneSettings settings = noiseSettings.sandDuneSettings;

		float totalSequenceLength = 0f;
		for (int i = 0; i < settings.duneWidth.Length; i++) {
			totalSequenceLength += settings.duneWidth[i];
			totalSequenceLength += settings.duneGap;
		}

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				
				// Calculate dunewidth to use based on position in sequence
				float duneOffset = noiseValues[i, j] * settings.maxDuneVariation + settings.duneOffset;
				float posInSequence = (j + duneOffset + chunkIdxY * (3 - height)) % totalSequenceLength;
				posInSequence = (posInSequence < 0) ? posInSequence + totalSequenceLength : posInSequence;

				int duneWidthIndex = 0;
				float curSeqPos = 0f;
				while (true) {
					curSeqPos += settings.duneWidth[duneWidthIndex];
					curSeqPos += settings.duneGap;
					if (curSeqPos >= posInSequence) {
						break;
					}
					duneWidthIndex++;
				}
				float duneWidth = settings.duneWidth[duneWidthIndex];
				float x = posInSequence - (curSeqPos - duneWidth - settings.duneGap); // X = position along in current dune
				x = Mathf.Clamp(x / duneWidth, 0, 1);
				
				// Calculate dune height and equation according to Laurent Avenel (Wiwine) Terragen tutorial
				float side = (x > settings.xm) ? 1f : 0f;
				float cosTerm = 1f - Mathf.Cos((Mathf.PI / (settings.p * side + 1)) * ((x - side) / (settings.xm - side)));
				float constant = ((settings.p * side + 1) / 2f);
				
				// Calculate height multiplier with range[0, 1], perlin height value must be > duneThreshold or it is clamped to 0 
				float duneThresholdMultiplier = Mathf.Max(0, noiseValues[i, j] - settings.duneThreshold) / (Mathf.Max(1f - settings.duneThreshold, 0.01f));

				// Calculate dune height according to dune width and max repose angle of sand
				float duneHeight = ((2 * settings.sigma * duneWidth) / Mathf.PI) * Mathf.Max(1 - settings.xm, 0.01f);

				values[i, j] += (constant * cosTerm) * duneHeight * duneThresholdMultiplier;
			}
		}

		return values;
	}

	public static float CalculateDunePos(SandDuneSettings settings, float duneWidth,  int height, int chunkIdxY, int j, float noiseVal) {
		// Calculate where along sand dune we are:
		// [0, xm] = on the curve upwards to peak 
		// [xm, 1) = on the downwards curve
		// [1, inf] = gap between dunes
		// Random offset to generate wavey peaks
		float duneOffset = noiseVal * settings.maxDuneVariation + settings.duneOffset; // height - 1 is max value for height, subtract another 
		float duneLength = duneWidth + settings.duneGap;
		float x = (j + duneOffset + chunkIdxY * (3 - height)) % duneLength; 
		x = x < 0 ? x + duneLength : x;
		x = Mathf.Clamp(x / duneWidth, 0, 1);
		return x;
	}
}