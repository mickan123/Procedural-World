using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiskSampling {

	public static List<Vector2> GeneratePoints(TerrainObjectSettings settings, 
												int mapSize, 
												Vector2 sampleCentre, 
												float[,] spawnNoiseMap, 
												System.Random prng,
												int numSamplesBeforeRejection = 20) {
		
		float cellSize = settings.maxRadius / Mathf.Sqrt(2);

		List<int>[,] grid = new List<int>[Mathf.CeilToInt((float)mapSize / cellSize), Mathf.CeilToInt((float)mapSize / cellSize)];
		for (int x = 0; x < grid.GetLength(0); x++) {
			for (int y = 0; y < grid.GetLength(1); y++) {
				grid[x, y] = new List<int>();
			}
		}
		List<Vector2> points = new List<Vector2>();
		List<Vector2> spawnPoints = new List<Vector2>();

		int numPoints = 0;
		spawnPoints.Add(new Vector2((float)mapSize / 2, (float)mapSize / 2));
		while (spawnPoints.Count > 0) {
			numPoints++;
			int spawnIndex = prng.Next(0, spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];
			bool candidateAccepted = false;

			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{	
				float randomFloat = Common.NextFloat(prng, 0f, 1f);
				float angle = randomFloat * Mathf.PI * 2;
				float radius = spawnNoiseMap[Mathf.RoundToInt(spawnCentre.x), Mathf.RoundToInt(spawnCentre.y)] * (settings.maxRadius - settings.minRadius) + settings.minRadius;
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				
				Vector2 candidate = spawnCentre + dir * Common.NextFloat(prng, radius, 2 * radius);
				if (IsValid(candidate, mapSize, cellSize, radius, points, grid)) {
					points.Add(candidate);
					spawnPoints.Add(candidate);
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)].Add(points.Count);
					candidateAccepted = true;
					break;
				}
			}
			if (!candidateAccepted) {
				spawnPoints.RemoveAt(spawnIndex);
			}
		}

		return points;
	}

	static bool IsValid(Vector2 candidate, 
						int mapSize, 
						float cellSize,
						float radius, 
						List<Vector2> points, 
						List<int>[,] grid) {


		if (candidate.x >= 0 && candidate.x < (float)mapSize && candidate.y >= 0 && candidate.y < (float)mapSize) {

			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);
			int searchStartX = Mathf.Max(0, cellX - 1);
			int searchEndX = Mathf.Min(cellX + 1, grid.GetLength(0) - 1);
			int searchStartY = Mathf.Max(0, cellY - 1);
			int searchEndY = Mathf.Min(cellY + 1, grid.GetLength(1) - 1);

			for (int x = searchStartX; x <= searchEndX; x++) {
				for (int y = searchStartY; y <= searchEndY; y++) {
					for (int i = 0; i < grid[x, y].Count; i++) {
						int pointIndex = grid[x, y][i] - 1;
						if (pointIndex != -1) {
							float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
							if (sqrDst < radius * radius) {
								return false;
							}
						}
					}
				}
			}
			return true;
		}
		return false;
	}
}
