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

	public static Texture2D TextureFromHeightMap(float[,] heightMap) {
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		Color[] colourMap = new Color[width * height];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int x = 0; x < height; x++) {
			for (int y = 0; y < width; y++) {

				if (heightMap[x, y] < minValue) {
					minValue = heightMap[x, y];
				}
				if (heightMap[x, y] > maxValue) {
					maxValue = heightMap[x, y];
				}
			}
		}

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap[y * width + x] = Color.Lerp(Color.black, 
													  Color.white, 
													  Mathf.InverseLerp(minValue, maxValue, heightMap[x, y]));
			}
		}

		return TextureFromColourMap(colourMap, width, height);
	}
}
