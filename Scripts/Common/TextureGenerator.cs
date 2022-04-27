using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width)
    {
        Texture2D texture = new Texture2D(width, width);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[] heightMap, int width)
    {
        Color[] colourMap = new Color[width * width];

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {

                if (heightMap[x * width + y] < minValue)
                {
                    minValue = heightMap[x * width + y];
                }
                if (heightMap[x * width + y] > maxValue)
                {
                    maxValue = heightMap[x * width + y];
                }
            }
        }

        for (int y = 0; y < width; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black,
                                                      Color.white,
                                                      Mathf.InverseLerp(minValue, maxValue, heightMap[x * width + y]));
            }
        }

        return TextureFromColourMap(colourMap, width);
    }
}
