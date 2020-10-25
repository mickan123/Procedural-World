using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ObjectFilters
{
    public static void FilterPointsByBiome(ref List<Vector3> points, int biome, BiomeInfo info, System.Random prng)
    {
        for (int i = 0; i < points.Count; i++)
        {
            float rand = (float)prng.NextDouble();

            int coordX = (int)points[i].x;
            int coordZ = (int)points[i].z;
            float biomeStrength = info.biomeStrengths[coordX, coordZ, biome];

            if (rand > biomeStrength * biomeStrength * biomeStrength)
            {
                points.RemoveAt(i);
                i--;
            }
        }
    }

    public static void FilterPointsBySlope(ref List<Vector3> points, float minAngle, float maxAngle, float[,] heightMap)
    {
        for (int i = 0; i < points.Count; i++)
        {
            float angle = Common.CalculateAngle(points[i].x, points[i].z, heightMap);
            if (angle > maxAngle || angle < minAngle)
            {
                points.RemoveAt(i);
                i--;
            }
        }
    }

    public static void FilterPointsByHeight(
        ref List<Vector3> points,
        float minHeight,
        float maxHeight,
        float[,] heightMap,
        AnimationCurve heightProbabilityCurve,
        System.Random prng
    )
    {
        for (int i = 0; i < points.Count; i++)
        {
            float height = heightMap[(int)points[i].x, (int)points[i].z];
            if (height > maxHeight || height < minHeight)
            {
                points.RemoveAt(i);
                i--;
            }
            else
            {
                float percentage = (height - minHeight) / (maxHeight - minHeight);
                float minProb = heightProbabilityCurve.Evaluate(percentage);
                if (Common.NextFloat(prng, 0f, 1f) > minProb)
                {
                    points.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public static void FilterPointsOnRoad(
        ref List<Vector3> points,
        float[,] roadStrengthMap,
        System.Random prng
    )
    {
        for (int i = 0; i < points.Count; i++)
        {
            float roadStrength = Common.HeightFromFloatCoord(points[i].x, points[i].z, roadStrengthMap);

            if (Common.NextFloat(prng, 0f, 0.5f) <= roadStrength)
            {
                points.RemoveAt(i);
                i--;
            }
        }
    }

}