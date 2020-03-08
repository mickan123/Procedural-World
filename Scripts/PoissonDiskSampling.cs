using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiskSampling {

	private static System.Random prng = new System.Random(0);

	public static List<Vector2> GeneratePoints(float radius, int mapSize, int biome, BiomeInfo info, Vector2 sampleCentre, int numSamplesBeforeRejection = 200) {

		float cellSize = radius / Mathf.Sqrt(2);

		int[,] grid = new int[Mathf.CeilToInt((float)mapSize / cellSize), Mathf.CeilToInt((float)mapSize / cellSize)];
		List<Vector2> points = new List<Vector2>();
		List<Vector2> spawnPoints = new List<Vector2>();

		spawnPoints.Add(new Vector2((float)mapSize / 2, (float)mapSize / 2));
		while (spawnPoints.Count > 0) {
			int spawnIndex = prng.Next(0, spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];
			bool candidateAccepted = false;

			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{	
				float angle = NextFloat(prng, 0f, 1f) * Mathf.PI * 2;
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2 candidate = spawnCentre + dir * NextFloat(prng, radius, 2 * radius);
				if (IsValid(candidate, mapSize, cellSize, radius, points, biome, info, grid)) {
					points.Add(candidate);
					spawnPoints.Add(candidate);
					grid[(int)(candidate.x/cellSize),(int)(candidate.y/cellSize)] = points.Count;
					candidateAccepted = true;
					break;
				}
			}
			if (!candidateAccepted) {
				spawnPoints.RemoveAt(spawnIndex);
			}
		}

		// Filter points based on biome
		for (int i = 0; i < points.Count; i++) {
			if (info.biomeMap[Mathf.FloorToInt(points[i].x), Mathf.FloorToInt(points[i].y)] != biome) {
				points.RemoveAt(i);
				i--;
			}
		}

		return points;
	}

	static bool IsValid(Vector2 candidate, 
						int mapSize, 
						float cellSize,
						float radius, 
						List<Vector2> points, 
						int biome,
						BiomeInfo info,
						int[,] grid) {


		if (candidate.x >= 0 && candidate.x < (float)mapSize && candidate.y >= 0 && candidate.y < (float)mapSize) {

			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);
			int searchStartX = Mathf.Max(0, cellX - 2);
			int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
			int searchStartY = Mathf.Max(0, cellY -2);
			int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

			for (int x = searchStartX; x <= searchEndX; x++) {
				for (int y = searchStartY; y <= searchEndY; y++) {
					int pointIndex = grid[x, y] - 1;
					if (pointIndex != -1) {
						float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
						if (sqrDst < radius * radius) {
							return false;
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	static float NextFloat(System.Random random, float begin, float end)
	{
		double mantissa = (random.NextDouble() * 2.0) - 1.0;
		// choose -149 instead of -126 to also generate subnormal floats (*)
		double exponent = System.Math.Pow(2.0, random.Next(-126, 128));

		float value = (float)(mantissa * exponent);

		return value * (end - begin) + begin;
	}
}
