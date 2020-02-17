using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {

	public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D(width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colourMap);
		texture.Apply();
		return texture;
	}

	public static Texture2D TextureFromHeightMap(NoiseMap heightMap) {
		int width = heightMap.values.GetLength(0);
		int height = heightMap.values.GetLength(1);

		Color[] colourMap = new Color[width * height];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int x = 0; x < height; x++) {
			for (int y = 0; y < width; y++) {

				if (heightMap.values[x, y] < minValue) {
					minValue = heightMap.values[x, y];
				}
				if (heightMap.values[x, y] > maxValue) {
					maxValue = heightMap.values[x, y];
				}
			}
		}

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap[y * width + x] = Color.Lerp(Color.black, 
													  Color.white, 
													  Mathf.InverseLerp(minValue, maxValue, heightMap.values[x, y]));
			}
		}

		return TextureFromColourMap(colourMap, width, height);
	}
}
