using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ThermalErosion  {

	private static readonly int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1} };

	public static float[,] Erode(float[,] values, ErosionSettings settings) {
		
		int mapSize = values.GetLength(0);
		
		int numNeighbours = offsets.GetLength(0);

		for (int iter = 0; iter < settings.numThermalErosionIterations; iter++) {
			for (int x = 0; x < mapSize; x++) {
				for (int y = 0; y < mapSize; y++) {

					float sumHeightDifferences = 0;
					float[] heightDifferences = new float[numNeighbours];
					
					for (int i = 0; i < numNeighbours; i++) {
						int offsetX = x + offsets[i, 0];
						int offsetY = y + offsets[i, 1];
						if (offsetX >= 0 && offsetY >= 0 && offsetX < mapSize && offsetY < mapSize) {
							heightDifferences[i] = (values[offsetX, offsetY] - values[x, y]);
							sumHeightDifferences += Mathf.Max(0f, heightDifferences[i]);
						} else {
							heightDifferences[i] = 0f;
						}
					}
					

					for (int i = 0; i < numNeighbours; i++) {
						int offsetX = x + offsets[i, 0];
						int offsetY = y + offsets[i, 1];
						if (offsetX >= 0 && offsetY >= 0 && offsetX < mapSize && offsetY < mapSize 
						    && heightDifferences[i] > settings.talusAngle) {
							
							float volumeToBeMoved = (heightDifferences[i] / sumHeightDifferences) * settings.thermalErosionRate * settings.hardness * 0.5f;

							values[offsetX, offsetY] -= volumeToBeMoved;
							values[x, y] += volumeToBeMoved;
						}
					}
				}
			}
		}

		return values;
	}
}