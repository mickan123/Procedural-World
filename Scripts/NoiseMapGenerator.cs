using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseMapGenerator {

	public static NoiseMap GenerateNoiseMap(int width, int height, NoiseMapSettings settings, Vector2 sampleCentre) {
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

		AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

				if (values[i, j] > maxValue) {
					maxValue = values[i, j];
				}
				if (values[i, j] < minValue) {
					minValue = values[i, j];
				}
			}
		}

		return new NoiseMap(values, minValue, maxValue);
	}
}


public struct NoiseMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public NoiseMap(float[,] values, float minValue, float maxValue) {
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}