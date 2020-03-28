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
		if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.Simplex) {
			return GeneratePerlinHeightMap(width, height, noiseSettings, worldSettings, sampleCentre, normalizeMode, seed);
		} 
		else if (noiseSettings.noiseType == NoiseMapSettings.NoiseType.SandDune) {
			return GenerateSandDuneHeightMap(width, height, noiseSettings, sampleCentre, seed);
		}
		else {
			return GeneratePerlinHeightMap(width, height, noiseSettings, worldSettings, sampleCentre, normalizeMode, seed);
		}
	}

	public static HeightMap GeneratePerlinHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											WorldSettings worldSettings,
											Vector2 sampleCentre, 
											NormalizeMode normalizeMode,
											int seed) {
		float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings.perlinNoiseSettings, sampleCentre, seed);

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

		AnimationCurve heightCurve_threadsafe = new AnimationCurve(noiseSettings.heightCurve.keys);

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * noiseSettings.heightMultiplier;
			}
		}

		return new HeightMap(values);
	}

	public static HeightMap GenerateSandDuneHeightMap(int width, 
											int height, 
											NoiseMapSettings noiseSettings, 
											Vector2 sampleCentre, 
											int seed) {
		
		float[,] values = new float[width, height];

		float[,] perlinValuesOffset = Noise.GenerateNoiseMap(width, height, noiseSettings.perlinNoiseSettings, sampleCentre, seed + 1);
		float[,] perlinValuesGap = Noise.GenerateNoiseMap(width, height, noiseSettings.perlinNoiseSettings, sampleCentre, seed + 2);
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				perlinValuesGap[i, j] = (perlinValuesGap[i, j] + 1) / 2;
				perlinValuesOffset[i, j] = (perlinValuesOffset[i, j] + 1) / 2;
			}
		}	

		SandDuneSettings settings = noiseSettings.sandDuneSettings;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				float duneGap = (settings.maxDuneGap - settings.minDuneGap) * perlinValuesGap[i, j] + settings.minDuneGap;
				float duneOffset = (settings.maxDuneOffset - settings.minDuneOffset) * perlinValuesOffset[i, j] + settings.minDuneOffset;
				float duneLength = settings.duneWidth + duneGap;
				float x = (j + duneOffset) % duneLength;
				float side = (x > settings.xm * settings.duneWidth) ? 1f : 0f;
				x = Mathf.Clamp(x / settings.duneWidth, 0, 1);

				float cosTerm = 1f - Mathf.Cos((Mathf.PI / (settings.p * side + 1)) * ((x - side) / (settings.xm - side)));
				float constant = ((settings.p * side + 1) / 2f);
				float duneHeight = ((2 * settings.sigma * settings.duneWidth) / Mathf.PI) * (1 - settings.xm);

				values[i, j] = (constant * cosTerm) * duneHeight;
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