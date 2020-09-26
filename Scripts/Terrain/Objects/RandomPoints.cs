using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomPoints
{
    public static List<Vector3> GeneratePoints(TerrainObjectSettings settings, System.Random prng, float[,] heightMap) {

        float mapSize = heightMap.GetLength(0);
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < settings.numRandomSpawns; i++) {
            float x = Common.NextFloat(prng, 0, mapSize - 1);
            float z = Common.NextFloat(prng, 0, mapSize - 1);
            float offset = 1f;
            float y = Common.HeightFromFloatCoord(x + offset, z + offset, heightMap);

            if (x >= 0f && y >= 0f && x <= mapSize - 3 && y <= mapSize - 3) {
				points.Add(new Vector3(x, y, z));
			}
        }
    
        return points;
    }

}
