using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[] GenerateFalloffMap(int width)
    {
        float[] map = new float[width * width];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float x = i / (float)width * 2 - 1;
                float y = j / (float)width * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i * width + j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value)
    {
        float a = 3f;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow((b - b * value), a));
    }
}
